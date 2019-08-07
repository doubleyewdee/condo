[![Build Status](https://doubleyewdee.visualstudio.com/condo/_apis/build/status/doubleyewdee.condo?branchName=master)](https://doubleyewdee.visualstudio.com/condo/_build/latest?definitionId=4?branchName=master)

# WPF command line shell using Windows ConPTY

This is a WPF wrapper on Windows command line applications invoked using the new and very cool
ConPTY functionality in Windows 10 RS4+. The code is pretty raw and multiple things are missing,
but for a light overview:
- condo is the actual WPF application. I'm building a custom rendering surface for high throughput
  and potential customizability down the road.
- ConsoleBuffer is the combination ConPTY invoker and vt100 emulator.
- condo.uwp was my experiment with a UWP version. It's not wired to build and it doesn't work
  really. The UWP platform has some issues with UWP apps being able to invoke external processes.
  This needs some work from the console/Windows/UWP teams (I guess?) to address this for now.

## Dev notes

Prequisites to build:
- .NET Core 3 preview 7 (it's what is currently in use to build).
- Visual Studio 2019 16.3 preview 1 (see above)

Some cool sites I've found while putting this together.
- VT codes doc: https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences
- xterm: http://invisible-island.net/xterm/ctlseqs/ctlseqs.html
- This Honeywell (???) document: https://country.honeywellaidc.com/CatalogDocuments/RFTERM-UG%20Rev%20D.pdf
- Sample for ConPTY: https://github.com/Microsoft/console/tree/master/samples/ConPTY/EchoCon
- a random fun site: http://wiki.bash-hackers.org/scripting/terminalcodes
