namespace ConsoleBuffer
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.Win32.SafeHandles;

    public sealed class ConsoleWrapper : IDisposable, INotifyPropertyChanged
    {
        private string contents;
        public string Contents
        {
            get
            {
                return this.contents;
            }
            set
            {
                this.contents = value;
                this.OnPropertyChanged(nameof(Contents));
            }
        }

        public string Command { get; private set; }

        private NativeMethods.COORD consoleSize = new NativeMethods.COORD { X = 25, Y = 80 };

        /// <summary>
        /// The handle from which we read data from the console.
        /// </summary>
        private SafeFileHandle readHandle;

        /// <summary>
        /// The handle into which we write data to the console.
        /// </summary>
        private SafeFileHandle writeHandle;

        /// <summary>
        /// Handle to the console PTY.
        /// </summary>
        private IntPtr consoleHandle;

        // used for booting / running the underlying process.
        private NativeMethods.STARTUPINFOEX startupInfo;
        private NativeMethods.PROCESS_INFORMATION processInfo;

        public ConsoleWrapper(string command)
        {
            using (var sr = new StreamWriter(new FileStream(@"c:\users\wd\source\repos\wincon\fuckme.log", FileMode.Create)))
            {
                sr.WriteLine($"let's start this shit");

                this.Contents = string.Empty;

                if (string.IsNullOrWhiteSpace(command))
                {
                    throw new ArgumentException("No command specified.", nameof(command));
                }

                this.Command = command;
                sr.WriteLine($"running {command}");

                this.CreatePTY();
                sr.WriteLine($"created PTY");
                this.InitializeStartupInfo();
                sr.WriteLine($"initialized startup shit");
                this.StartProcess();
                sr.WriteLine($"started the fucking process");

                Task.Run(() => this.ReadConsoleTask());
                sr.WriteLine($"fuck ME");
            }
        }

        private void CreatePTY()
        {
            SafeFileHandle pipeTTYin, pipeTTYout;

            if (   NativeMethods.CreatePipe(out pipeTTYin, out this.writeHandle, IntPtr.Zero, 0)
                && NativeMethods.CreatePipe(out this.readHandle, out pipeTTYout, IntPtr.Zero, 0))
            {
                ThrowForHResult(NativeMethods.CreatePseudoConsole(this.consoleSize, pipeTTYin, pipeTTYout, 0, out this.consoleHandle),
                                "Failed to create PTY");

                // It is safe to close these as they have been duped to the child console host and will be closed on that end.
                if (!pipeTTYin.IsInvalid) pipeTTYin.Dispose();
                if (!pipeTTYout.IsInvalid) pipeTTYout.Dispose();

                return;
            }

            throw new InvalidOperationException("Unable to create pipe(s) for console.");
        }

        private void InitializeStartupInfo()
        {
            IntPtr allocSize = IntPtr.Zero;
            // yes this method really returns true *if it fails to get the size as requested*
            // ... fuckin' Windows.
            if (NativeMethods.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref allocSize) || allocSize == IntPtr.Zero)
            {
                throw new InvalidOperationException("Unable to get size of process startup attribute storage.");
            }

            this.startupInfo = new NativeMethods.STARTUPINFOEX();
            this.startupInfo.StartupInfo.cb = Marshal.SizeOf<NativeMethods.STARTUPINFOEX>();
            this.startupInfo.lpAttributeList = Marshal.AllocHGlobal(allocSize);
            if (!NativeMethods.InitializeProcThreadAttributeList(this.startupInfo.lpAttributeList, 1, 0, ref allocSize))
            {
                ThrowForHResult(Marshal.GetLastWin32Error(), "Unable to initialze process startup info.");
            }

            if (!NativeMethods.UpdateProcThreadAttribute(this.startupInfo.lpAttributeList, 0, (IntPtr)NativeMethods.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, this.consoleHandle,
                                                         (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
            {
                ThrowForHResult(Marshal.GetLastWin32Error(), "Unable to update process startup info.");
            }
        }

        private void StartProcess()
        {
            var processSecurityAttr = new NativeMethods.SECURITY_ATTRIBUTES();
            var threadSecurityAttr = new NativeMethods.SECURITY_ATTRIBUTES();
            processSecurityAttr.nLength = threadSecurityAttr.nLength = Marshal.SizeOf<NativeMethods.SECURITY_ATTRIBUTES>();

            if (!NativeMethods.CreateProcess(null, this.Command, ref processSecurityAttr, ref threadSecurityAttr, false,
                                             NativeMethods.EXTENDED_STARTUPINFO_PRESENT, IntPtr.Zero, null, ref this.startupInfo, out this.processInfo))
            {
                ThrowForHResult(Marshal.GetLastWin32Error(), "Unable to start process.");
            }
        }

        private void ReadConsoleTask()
        {
            using (var ptyOutput = new FileStream(this.readHandle, FileAccess.Read))
            {
                var input = new byte[32];

                while (true)
                {
                    if (this.disposed)
                    {
                        return;
                    }

                    var read = ptyOutput.Read(input, 0, input.Length);
                    this.Contents += System.Text.Encoding.UTF8.GetString(input, 0, read);
                }
            }
        }

        private static void ThrowForHResult(int hr, string exceptionMessage)
        {
            if (hr != 0)
                throw new InvalidOperationException($"{exceptionMessage}: {Marshal.GetHRForLastWin32Error()}");
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        #region IDisposable
        private bool disposed = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.readHandle?.Dispose();
                    this.readHandle = null;
                    this.writeHandle?.Dispose();
                    this.writeHandle = null;
                }

                if (this.processInfo.hProcess != IntPtr.Zero)
                {
                    // XXX: maybe don't?
                    NativeMethods.TerminateProcess(this.processInfo.hProcess, uint.MaxValue);

                    NativeMethods.CloseHandle(this.processInfo.hProcess);
                    this.processInfo.hProcess = IntPtr.Zero;
                }
                if (this.processInfo.hThread != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(this.processInfo.hThread);
                    this.processInfo.hThread = IntPtr.Zero;
                }

                if (this.consoleHandle != IntPtr.Zero)
                {
                    NativeMethods.ClosePseudoConsole(this.consoleHandle);
                    this.consoleHandle = IntPtr.Zero;
                }

                if (this.startupInfo.lpAttributeList != IntPtr.Zero)
                {
                    NativeMethods.DeleteProcThreadAttributeList(this.startupInfo.lpAttributeList);
                    Marshal.FreeHGlobal(this.startupInfo.lpAttributeList);
                    this.startupInfo.lpAttributeList = IntPtr.Zero;
                }

                this.disposed = true;
            }
        }

        ~ConsoleWrapper()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
