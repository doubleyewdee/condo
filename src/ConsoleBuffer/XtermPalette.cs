namespace ConsoleBuffer
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the xterm palette for 256 color rendering.
    /// </summary>
    public sealed class XtermPalette
    {
        private struct XtermColor
        {
            public int Id;
            public byte R;
            public byte G;
            public byte B;
            public string Name;
        }

        private readonly XtermColor[] DefaultPalette = new[]
        {
            new XtermColor { Id = 0, R = 0x00, G = 0x00, B = 0x00, Name = "Black" },
            new XtermColor { Id = 1, R = 0x80, G = 0x00, B = 0x00, Name = "Maroon" },
            new XtermColor { Id = 2, R = 0x00, G = 0x80, B = 0x00, Name = "Green" },
            new XtermColor { Id = 3, R = 0x80, G = 0x80, B = 0x00, Name = "Olive" },
            new XtermColor { Id = 4, R = 0x00, G = 0x00, B = 0x80, Name = "Navy" },
            new XtermColor { Id = 5, R = 0x80, G = 0x00, B = 0x80, Name = "Purple" },
            new XtermColor { Id = 6, R = 0x00, G = 0x80, B = 0x80, Name = "Teal" },
            new XtermColor { Id = 7, R = 0xc0, G = 0xc0, B = 0xc0, Name = "Silver" },
            new XtermColor { Id = 8, R = 0x80, G = 0x80, B = 0x80, Name = "Grey" },
            new XtermColor { Id = 9, R = 0xff, G = 0x00, B = 0x00, Name = "Red" },
            new XtermColor { Id = 10, R = 0x00, G = 0xff, B = 0x00, Name = "Lime" },
            new XtermColor { Id = 11, R = 0xff, G = 0xff, B = 0x00, Name = "Yellow" },
            new XtermColor { Id = 12, R = 0x00, G = 0x00, B = 0xff, Name = "Blue" },
            new XtermColor { Id = 13, R = 0xff, G = 0x00, B = 0xff, Name = "Fuchsia" },
            new XtermColor { Id = 14, R = 0x00, G = 0xff, B = 0xff, Name = "Aqua" },
            new XtermColor { Id = 15, R = 0xff, G = 0xff, B = 0xff, Name = "White" },
            new XtermColor { Id = 16, R = 0x00, G = 0x00, B = 0x00, Name = "Grey0" },
            new XtermColor { Id = 17, R = 0x00, G = 0x00, B = 0x5f, Name = "NavyBlue" },
            new XtermColor { Id = 18, R = 0x00, G = 0x00, B = 0x87, Name = "DarkBlue" },
            new XtermColor { Id = 19, R = 0x00, G = 0x00, B = 0xaf, Name = "Blue3" },
            new XtermColor { Id = 20, R = 0x00, G = 0x00, B = 0xd7, Name = "Blue3" },
            new XtermColor { Id = 21, R = 0x00, G = 0x00, B = 0xff, Name = "Blue1" },
            new XtermColor { Id = 22, R = 0x00, G = 0x5f, B = 0x00, Name = "DarkGreen" },
            new XtermColor { Id = 23, R = 0x00, G = 0x5f, B = 0x5f, Name = "DeepSkyBlue4" },
            new XtermColor { Id = 24, R = 0x00, G = 0x5f, B = 0x87, Name = "DeepSkyBlue4" },
            new XtermColor { Id = 25, R = 0x00, G = 0x5f, B = 0xaf, Name = "DeepSkyBlue4" },
            new XtermColor { Id = 26, R = 0x00, G = 0x5f, B = 0xd7, Name = "DodgerBlue3" },
            new XtermColor { Id = 27, R = 0x00, G = 0x5f, B = 0xff, Name = "DodgerBlue2" },
            new XtermColor { Id = 28, R = 0x00, G = 0x87, B = 0x00, Name = "Green4" },
            new XtermColor { Id = 29, R = 0x00, G = 0x87, B = 0x5f, Name = "SpringGreen4" },
            new XtermColor { Id = 30, R = 0x00, G = 0x87, B = 0x87, Name = "Turquoise4" },
            new XtermColor { Id = 31, R = 0x00, G = 0x87, B = 0xaf, Name = "DeepSkyBlue3" },
            new XtermColor { Id = 32, R = 0x00, G = 0x87, B = 0xd7, Name = "DeepSkyBlue3" },
            new XtermColor { Id = 33, R = 0x00, G = 0x87, B = 0xff, Name = "DodgerBlue1" },
            new XtermColor { Id = 34, R = 0x00, G = 0xaf, B = 0x00, Name = "Green3" },
            new XtermColor { Id = 35, R = 0x00, G = 0xaf, B = 0x5f, Name = "SpringGreen3" },
            new XtermColor { Id = 36, R = 0x00, G = 0xaf, B = 0x87, Name = "DarkCyan" },
            new XtermColor { Id = 37, R = 0x00, G = 0xaf, B = 0xaf, Name = "LightSeaGreen" },
            new XtermColor { Id = 38, R = 0x00, G = 0xaf, B = 0xd7, Name = "DeepSkyBlue2" },
            new XtermColor { Id = 39, R = 0x00, G = 0xaf, B = 0xff, Name = "DeepSkyBlue1" },
            new XtermColor { Id = 40, R = 0x00, G = 0xd7, B = 0x00, Name = "Green3" },
            new XtermColor { Id = 41, R = 0x00, G = 0xd7, B = 0x5f, Name = "SpringGreen3" },
            new XtermColor { Id = 42, R = 0x00, G = 0xd7, B = 0x87, Name = "SpringGreen2" },
            new XtermColor { Id = 43, R = 0x00, G = 0xd7, B = 0xaf, Name = "Cyan3" },
            new XtermColor { Id = 44, R = 0x00, G = 0xd7, B = 0xd7, Name = "DarkTurquoise" },
            new XtermColor { Id = 45, R = 0x00, G = 0xd7, B = 0xff, Name = "Turquoise2" },
            new XtermColor { Id = 46, R = 0x00, G = 0xff, B = 0x00, Name = "Green1" },
            new XtermColor { Id = 47, R = 0x00, G = 0xff, B = 0x5f, Name = "SpringGreen2" },
            new XtermColor { Id = 48, R = 0x00, G = 0xff, B = 0x87, Name = "SpringGreen1" },
            new XtermColor { Id = 49, R = 0x00, G = 0xff, B = 0xaf, Name = "MediumSpringGreen" },
            new XtermColor { Id = 50, R = 0x00, G = 0xff, B = 0xd7, Name = "Cyan2" },
            new XtermColor { Id = 51, R = 0x00, G = 0xff, B = 0xff, Name = "Cyan1" },
            new XtermColor { Id = 52, R = 0x5f, G = 0x00, B = 0x00, Name = "DarkRed" },
            new XtermColor { Id = 53, R = 0x5f, G = 0x00, B = 0x5f, Name = "DeepPink4" },
            new XtermColor { Id = 54, R = 0x5f, G = 0x00, B = 0x87, Name = "Purple4" },
            new XtermColor { Id = 55, R = 0x5f, G = 0x00, B = 0xaf, Name = "Purple4" },
            new XtermColor { Id = 56, R = 0x5f, G = 0x00, B = 0xd7, Name = "Purple3" },
            new XtermColor { Id = 57, R = 0x5f, G = 0x00, B = 0xff, Name = "BlueViolet" },
            new XtermColor { Id = 58, R = 0x5f, G = 0x5f, B = 0x00, Name = "Orange4" },
            new XtermColor { Id = 59, R = 0x5f, G = 0x5f, B = 0x5f, Name = "Grey37" },
            new XtermColor { Id = 60, R = 0x5f, G = 0x5f, B = 0x87, Name = "MediumPurple4" },
            new XtermColor { Id = 61, R = 0x5f, G = 0x5f, B = 0xaf, Name = "SlateBlue3" },
            new XtermColor { Id = 62, R = 0x5f, G = 0x5f, B = 0xd7, Name = "SlateBlue3" },
            new XtermColor { Id = 63, R = 0x5f, G = 0x5f, B = 0xff, Name = "RoyalBlue1" },
            new XtermColor { Id = 64, R = 0x5f, G = 0x87, B = 0x00, Name = "Chartreuse4" },
            new XtermColor { Id = 65, R = 0x5f, G = 0x87, B = 0x5f, Name = "DarkSeaGreen4" },
            new XtermColor { Id = 66, R = 0x5f, G = 0x87, B = 0x87, Name = "PaleTurquoise4" },
            new XtermColor { Id = 67, R = 0x5f, G = 0x87, B = 0xaf, Name = "SteelBlue" },
            new XtermColor { Id = 68, R = 0x5f, G = 0x87, B = 0xd7, Name = "SteelBlue3" },
            new XtermColor { Id = 69, R = 0x5f, G = 0x87, B = 0xff, Name = "CornflowerBlue" },
            new XtermColor { Id = 70, R = 0x5f, G = 0xaf, B = 0x00, Name = "Chartreuse3" },
            new XtermColor { Id = 71, R = 0x5f, G = 0xaf, B = 0x5f, Name = "DarkSeaGreen4" },
            new XtermColor { Id = 72, R = 0x5f, G = 0xaf, B = 0x87, Name = "CadetBlue" },
            new XtermColor { Id = 73, R = 0x5f, G = 0xaf, B = 0xaf, Name = "CadetBlue" },
            new XtermColor { Id = 74, R = 0x5f, G = 0xaf, B = 0xd7, Name = "SkyBlue3" },
            new XtermColor { Id = 75, R = 0x5f, G = 0xaf, B = 0xff, Name = "SteelBlue1" },
            new XtermColor { Id = 76, R = 0x5f, G = 0xd7, B = 0x00, Name = "Chartreuse3" },
            new XtermColor { Id = 77, R = 0x5f, G = 0xd7, B = 0x5f, Name = "PaleGreen3" },
            new XtermColor { Id = 78, R = 0x5f, G = 0xd7, B = 0x87, Name = "SeaGreen3" },
            new XtermColor { Id = 79, R = 0x5f, G = 0xd7, B = 0xaf, Name = "Aquamarine3" },
            new XtermColor { Id = 80, R = 0x5f, G = 0xd7, B = 0xd7, Name = "MediumTurquoise" },
            new XtermColor { Id = 81, R = 0x5f, G = 0xd7, B = 0xff, Name = "SteelBlue1" },
            new XtermColor { Id = 82, R = 0x5f, G = 0xff, B = 0x00, Name = "Chartreuse2" },
            new XtermColor { Id = 83, R = 0x5f, G = 0xff, B = 0x5f, Name = "SeaGreen2" },
            new XtermColor { Id = 84, R = 0x5f, G = 0xff, B = 0x87, Name = "SeaGreen1" },
            new XtermColor { Id = 85, R = 0x5f, G = 0xff, B = 0xaf, Name = "SeaGreen1" },
            new XtermColor { Id = 86, R = 0x5f, G = 0xff, B = 0xd7, Name = "Aquamarine1" },
            new XtermColor { Id = 87, R = 0x5f, G = 0xff, B = 0xff, Name = "DarkSlateGray2" },
            new XtermColor { Id = 88, R = 0x87, G = 0x00, B = 0x00, Name = "DarkRed" },
            new XtermColor { Id = 89, R = 0x87, G = 0x00, B = 0x5f, Name = "DeepPink4" },
            new XtermColor { Id = 90, R = 0x87, G = 0x00, B = 0x87, Name = "DarkMagenta" },
            new XtermColor { Id = 91, R = 0x87, G = 0x00, B = 0xaf, Name = "DarkMagenta" },
            new XtermColor { Id = 92, R = 0x87, G = 0x00, B = 0xd7, Name = "DarkViolet" },
            new XtermColor { Id = 93, R = 0x87, G = 0x00, B = 0xff, Name = "Purple" },
            new XtermColor { Id = 94, R = 0x87, G = 0x5f, B = 0x00, Name = "Orange4" },
            new XtermColor { Id = 95, R = 0x87, G = 0x5f, B = 0x5f, Name = "LightPink4" },
            new XtermColor { Id = 96, R = 0x87, G = 0x5f, B = 0x87, Name = "Plum4" },
            new XtermColor { Id = 97, R = 0x87, G = 0x5f, B = 0xaf, Name = "MediumPurple3" },
            new XtermColor { Id = 98, R = 0x87, G = 0x5f, B = 0xd7, Name = "MediumPurple3" },
            new XtermColor { Id = 99, R = 0x87, G = 0x5f, B = 0xff, Name = "SlateBlue1" },
            new XtermColor { Id = 100, R = 0x87, G = 0x87, B = 0x00, Name = "Yellow4" },
            new XtermColor { Id = 101, R = 0x87, G = 0x87, B = 0x5f, Name = "Wheat4" },
            new XtermColor { Id = 102, R = 0x87, G = 0x87, B = 0x87, Name = "Grey53" },
            new XtermColor { Id = 103, R = 0x87, G = 0x87, B = 0xaf, Name = "LightSlateGrey" },
            new XtermColor { Id = 104, R = 0x87, G = 0x87, B = 0xd7, Name = "MediumPurple" },
            new XtermColor { Id = 105, R = 0x87, G = 0x87, B = 0xff, Name = "LightSlateBlue" },
            new XtermColor { Id = 106, R = 0x87, G = 0xaf, B = 0x00, Name = "Yellow4" },
            new XtermColor { Id = 107, R = 0x87, G = 0xaf, B = 0x5f, Name = "DarkOliveGreen3" },
            new XtermColor { Id = 108, R = 0x87, G = 0xaf, B = 0x87, Name = "DarkSeaGreen" },
            new XtermColor { Id = 109, R = 0x87, G = 0xaf, B = 0xaf, Name = "LightSkyBlue3" },
            new XtermColor { Id = 110, R = 0x87, G = 0xaf, B = 0xd7, Name = "LightSkyBlue3" },
            new XtermColor { Id = 111, R = 0x87, G = 0xaf, B = 0xff, Name = "SkyBlue2" },
            new XtermColor { Id = 112, R = 0x87, G = 0xd7, B = 0x00, Name = "Chartreuse2" },
            new XtermColor { Id = 113, R = 0x87, G = 0xd7, B = 0x5f, Name = "DarkOliveGreen3" },
            new XtermColor { Id = 114, R = 0x87, G = 0xd7, B = 0x87, Name = "PaleGreen3" },
            new XtermColor { Id = 115, R = 0x87, G = 0xd7, B = 0xaf, Name = "DarkSeaGreen3" },
            new XtermColor { Id = 116, R = 0x87, G = 0xd7, B = 0xd7, Name = "DarkSlateGray3" },
            new XtermColor { Id = 117, R = 0x87, G = 0xd7, B = 0xff, Name = "SkyBlue1" },
            new XtermColor { Id = 118, R = 0x87, G = 0xff, B = 0x00, Name = "Chartreuse1" },
            new XtermColor { Id = 119, R = 0x87, G = 0xff, B = 0x5f, Name = "LightGreen" },
            new XtermColor { Id = 120, R = 0x87, G = 0xff, B = 0x87, Name = "LightGreen" },
            new XtermColor { Id = 121, R = 0x87, G = 0xff, B = 0xaf, Name = "PaleGreen1" },
            new XtermColor { Id = 122, R = 0x87, G = 0xff, B = 0xd7, Name = "Aquamarine1" },
            new XtermColor { Id = 123, R = 0x87, G = 0xff, B = 0xff, Name = "DarkSlateGray1" },
            new XtermColor { Id = 124, R = 0xaf, G = 0x00, B = 0x00, Name = "Red3" },
            new XtermColor { Id = 125, R = 0xaf, G = 0x00, B = 0x5f, Name = "DeepPink4" },
            new XtermColor { Id = 126, R = 0xaf, G = 0x00, B = 0x87, Name = "MediumVioletRed" },
            new XtermColor { Id = 127, R = 0xaf, G = 0x00, B = 0xaf, Name = "Magenta3" },
            new XtermColor { Id = 128, R = 0xaf, G = 0x00, B = 0xd7, Name = "DarkViolet" },
            new XtermColor { Id = 129, R = 0xaf, G = 0x00, B = 0xff, Name = "Purple" },
            new XtermColor { Id = 130, R = 0xaf, G = 0x5f, B = 0x00, Name = "DarkOrange3" },
            new XtermColor { Id = 131, R = 0xaf, G = 0x5f, B = 0x5f, Name = "IndianRed" },
            new XtermColor { Id = 132, R = 0xaf, G = 0x5f, B = 0x87, Name = "HotPink3" },
            new XtermColor { Id = 133, R = 0xaf, G = 0x5f, B = 0xaf, Name = "MediumOrchid3" },
            new XtermColor { Id = 134, R = 0xaf, G = 0x5f, B = 0xd7, Name = "MediumOrchid" },
            new XtermColor { Id = 135, R = 0xaf, G = 0x5f, B = 0xff, Name = "MediumPurple2" },
            new XtermColor { Id = 136, R = 0xaf, G = 0x87, B = 0x00, Name = "DarkGoldenrod" },
            new XtermColor { Id = 137, R = 0xaf, G = 0x87, B = 0x5f, Name = "LightSalmon3" },
            new XtermColor { Id = 138, R = 0xaf, G = 0x87, B = 0x87, Name = "RosyBrown" },
            new XtermColor { Id = 139, R = 0xaf, G = 0x87, B = 0xaf, Name = "Grey63" },
            new XtermColor { Id = 140, R = 0xaf, G = 0x87, B = 0xd7, Name = "MediumPurple2" },
            new XtermColor { Id = 141, R = 0xaf, G = 0x87, B = 0xff, Name = "MediumPurple1" },
            new XtermColor { Id = 142, R = 0xaf, G = 0xaf, B = 0x00, Name = "Gold3" },
            new XtermColor { Id = 143, R = 0xaf, G = 0xaf, B = 0x5f, Name = "DarkKhaki" },
            new XtermColor { Id = 144, R = 0xaf, G = 0xaf, B = 0x87, Name = "NavajoWhite3" },
            new XtermColor { Id = 145, R = 0xaf, G = 0xaf, B = 0xaf, Name = "Grey69" },
            new XtermColor { Id = 146, R = 0xaf, G = 0xaf, B = 0xd7, Name = "LightSteelBlue3" },
            new XtermColor { Id = 147, R = 0xaf, G = 0xaf, B = 0xff, Name = "LightSteelBlue" },
            new XtermColor { Id = 148, R = 0xaf, G = 0xd7, B = 0x00, Name = "Yellow3" },
            new XtermColor { Id = 149, R = 0xaf, G = 0xd7, B = 0x5f, Name = "DarkOliveGreen3" },
            new XtermColor { Id = 150, R = 0xaf, G = 0xd7, B = 0x87, Name = "DarkSeaGreen3" },
            new XtermColor { Id = 151, R = 0xaf, G = 0xd7, B = 0xaf, Name = "DarkSeaGreen2" },
            new XtermColor { Id = 152, R = 0xaf, G = 0xd7, B = 0xd7, Name = "LightCyan3" },
            new XtermColor { Id = 153, R = 0xaf, G = 0xd7, B = 0xff, Name = "LightSkyBlue1" },
            new XtermColor { Id = 154, R = 0xaf, G = 0xff, B = 0x00, Name = "GreenYellow" },
            new XtermColor { Id = 155, R = 0xaf, G = 0xff, B = 0x5f, Name = "DarkOliveGreen2" },
            new XtermColor { Id = 156, R = 0xaf, G = 0xff, B = 0x87, Name = "PaleGreen1" },
            new XtermColor { Id = 157, R = 0xaf, G = 0xff, B = 0xaf, Name = "DarkSeaGreen2" },
            new XtermColor { Id = 158, R = 0xaf, G = 0xff, B = 0xd7, Name = "DarkSeaGreen1" },
            new XtermColor { Id = 159, R = 0xaf, G = 0xff, B = 0xff, Name = "PaleTurquoise1" },
            new XtermColor { Id = 160, R = 0xd7, G = 0x00, B = 0x00, Name = "Red3" },
            new XtermColor { Id = 161, R = 0xd7, G = 0x00, B = 0x5f, Name = "DeepPink3" },
            new XtermColor { Id = 162, R = 0xd7, G = 0x00, B = 0x87, Name = "DeepPink3" },
            new XtermColor { Id = 163, R = 0xd7, G = 0x00, B = 0xaf, Name = "Magenta3" },
            new XtermColor { Id = 164, R = 0xd7, G = 0x00, B = 0xd7, Name = "Magenta3" },
            new XtermColor { Id = 165, R = 0xd7, G = 0x00, B = 0xff, Name = "Magenta2" },
            new XtermColor { Id = 166, R = 0xd7, G = 0x5f, B = 0x00, Name = "DarkOrange3" },
            new XtermColor { Id = 167, R = 0xd7, G = 0x5f, B = 0x5f, Name = "IndianRed" },
            new XtermColor { Id = 168, R = 0xd7, G = 0x5f, B = 0x87, Name = "HotPink3" },
            new XtermColor { Id = 169, R = 0xd7, G = 0x5f, B = 0xaf, Name = "HotPink2" },
            new XtermColor { Id = 170, R = 0xd7, G = 0x5f, B = 0xd7, Name = "Orchid" },
            new XtermColor { Id = 171, R = 0xd7, G = 0x5f, B = 0xff, Name = "MediumOrchid1" },
            new XtermColor { Id = 172, R = 0xd7, G = 0x87, B = 0x00, Name = "Orange3" },
            new XtermColor { Id = 173, R = 0xd7, G = 0x87, B = 0x5f, Name = "LightSalmon3" },
            new XtermColor { Id = 174, R = 0xd7, G = 0x87, B = 0x87, Name = "LightPink3" },
            new XtermColor { Id = 175, R = 0xd7, G = 0x87, B = 0xaf, Name = "Pink3" },
            new XtermColor { Id = 176, R = 0xd7, G = 0x87, B = 0xd7, Name = "Plum3" },
            new XtermColor { Id = 177, R = 0xd7, G = 0x87, B = 0xff, Name = "Violet" },
            new XtermColor { Id = 178, R = 0xd7, G = 0xaf, B = 0x00, Name = "Gold3" },
            new XtermColor { Id = 179, R = 0xd7, G = 0xaf, B = 0x5f, Name = "LightGoldenrod3" },
            new XtermColor { Id = 180, R = 0xd7, G = 0xaf, B = 0x87, Name = "Tan" },
            new XtermColor { Id = 181, R = 0xd7, G = 0xaf, B = 0xaf, Name = "MistyRose3" },
            new XtermColor { Id = 182, R = 0xd7, G = 0xaf, B = 0xd7, Name = "Thistle3" },
            new XtermColor { Id = 183, R = 0xd7, G = 0xaf, B = 0xff, Name = "Plum2" },
            new XtermColor { Id = 184, R = 0xd7, G = 0xd7, B = 0x00, Name = "Yellow3" },
            new XtermColor { Id = 185, R = 0xd7, G = 0xd7, B = 0x5f, Name = "Khaki3" },
            new XtermColor { Id = 186, R = 0xd7, G = 0xd7, B = 0x87, Name = "LightGoldenrod2" },
            new XtermColor { Id = 187, R = 0xd7, G = 0xd7, B = 0xaf, Name = "LightYellow3" },
            new XtermColor { Id = 188, R = 0xd7, G = 0xd7, B = 0xd7, Name = "Grey84" },
            new XtermColor { Id = 189, R = 0xd7, G = 0xd7, B = 0xff, Name = "LightSteelBlue1" },
            new XtermColor { Id = 190, R = 0xd7, G = 0xff, B = 0x00, Name = "Yellow2" },
            new XtermColor { Id = 191, R = 0xd7, G = 0xff, B = 0x5f, Name = "DarkOliveGreen1" },
            new XtermColor { Id = 192, R = 0xd7, G = 0xff, B = 0x87, Name = "DarkOliveGreen1" },
            new XtermColor { Id = 193, R = 0xd7, G = 0xff, B = 0xaf, Name = "DarkSeaGreen1" },
            new XtermColor { Id = 194, R = 0xd7, G = 0xff, B = 0xd7, Name = "Honeydew2" },
            new XtermColor { Id = 195, R = 0xd7, G = 0xff, B = 0xff, Name = "LightCyan1" },
            new XtermColor { Id = 196, R = 0xff, G = 0x00, B = 0x00, Name = "Red1" },
            new XtermColor { Id = 197, R = 0xff, G = 0x00, B = 0x5f, Name = "DeepPink2" },
            new XtermColor { Id = 198, R = 0xff, G = 0x00, B = 0x87, Name = "DeepPink1" },
            new XtermColor { Id = 199, R = 0xff, G = 0x00, B = 0xaf, Name = "DeepPink1" },
            new XtermColor { Id = 200, R = 0xff, G = 0x00, B = 0xd7, Name = "Magenta2" },
            new XtermColor { Id = 201, R = 0xff, G = 0x00, B = 0xff, Name = "Magenta1" },
            new XtermColor { Id = 202, R = 0xff, G = 0x5f, B = 0x00, Name = "OrangeRed1" },
            new XtermColor { Id = 203, R = 0xff, G = 0x5f, B = 0x5f, Name = "IndianRed1" },
            new XtermColor { Id = 204, R = 0xff, G = 0x5f, B = 0x87, Name = "IndianRed1" },
            new XtermColor { Id = 205, R = 0xff, G = 0x5f, B = 0xaf, Name = "HotPink" },
            new XtermColor { Id = 206, R = 0xff, G = 0x5f, B = 0xd7, Name = "HotPink" },
            new XtermColor { Id = 207, R = 0xff, G = 0x5f, B = 0xff, Name = "MediumOrchid1" },
            new XtermColor { Id = 208, R = 0xff, G = 0x87, B = 0x00, Name = "DarkOrange" },
            new XtermColor { Id = 209, R = 0xff, G = 0x87, B = 0x5f, Name = "Salmon1" },
            new XtermColor { Id = 210, R = 0xff, G = 0x87, B = 0x87, Name = "LightCoral" },
            new XtermColor { Id = 211, R = 0xff, G = 0x87, B = 0xaf, Name = "PaleVioletRed1" },
            new XtermColor { Id = 212, R = 0xff, G = 0x87, B = 0xd7, Name = "Orchid2" },
            new XtermColor { Id = 213, R = 0xff, G = 0x87, B = 0xff, Name = "Orchid1" },
            new XtermColor { Id = 214, R = 0xff, G = 0xaf, B = 0x00, Name = "Orange1" },
            new XtermColor { Id = 215, R = 0xff, G = 0xaf, B = 0x5f, Name = "SandyBrown" },
            new XtermColor { Id = 216, R = 0xff, G = 0xaf, B = 0x87, Name = "LightSalmon1" },
            new XtermColor { Id = 217, R = 0xff, G = 0xaf, B = 0xaf, Name = "LightPink1" },
            new XtermColor { Id = 218, R = 0xff, G = 0xaf, B = 0xd7, Name = "Pink1" },
            new XtermColor { Id = 219, R = 0xff, G = 0xaf, B = 0xff, Name = "Plum1" },
            new XtermColor { Id = 220, R = 0xff, G = 0xd7, B = 0x00, Name = "Gold1" },
            new XtermColor { Id = 221, R = 0xff, G = 0xd7, B = 0x5f, Name = "LightGoldenrod2" },
            new XtermColor { Id = 222, R = 0xff, G = 0xd7, B = 0x87, Name = "LightGoldenrod2" },
            new XtermColor { Id = 223, R = 0xff, G = 0xd7, B = 0xaf, Name = "NavajoWhite1" },
            new XtermColor { Id = 224, R = 0xff, G = 0xd7, B = 0xd7, Name = "MistyRose1" },
            new XtermColor { Id = 225, R = 0xff, G = 0xd7, B = 0xff, Name = "Thistle1" },
            new XtermColor { Id = 226, R = 0xff, G = 0xff, B = 0x00, Name = "Yellow1" },
            new XtermColor { Id = 227, R = 0xff, G = 0xff, B = 0x5f, Name = "LightGoldenrod1" },
            new XtermColor { Id = 228, R = 0xff, G = 0xff, B = 0x87, Name = "Khaki1" },
            new XtermColor { Id = 229, R = 0xff, G = 0xff, B = 0xaf, Name = "Wheat1" },
            new XtermColor { Id = 230, R = 0xff, G = 0xff, B = 0xd7, Name = "Cornsilk1" },
            new XtermColor { Id = 231, R = 0xff, G = 0xff, B = 0xff, Name = "Grey100" },
            new XtermColor { Id = 232, R = 0x08, G = 0x08, B = 0x08, Name = "Grey3" },
            new XtermColor { Id = 233, R = 0x12, G = 0x12, B = 0x12, Name = "Grey7" },
            new XtermColor { Id = 234, R = 0x1c, G = 0x1c, B = 0x1c, Name = "Grey11" },
            new XtermColor { Id = 235, R = 0x26, G = 0x26, B = 0x26, Name = "Grey15" },
            new XtermColor { Id = 236, R = 0x30, G = 0x30, B = 0x30, Name = "Grey19" },
            new XtermColor { Id = 237, R = 0x3a, G = 0x3a, B = 0x3a, Name = "Grey23" },
            new XtermColor { Id = 238, R = 0x44, G = 0x44, B = 0x44, Name = "Grey27" },
            new XtermColor { Id = 239, R = 0x4e, G = 0x4e, B = 0x4e, Name = "Grey30" },
            new XtermColor { Id = 240, R = 0x58, G = 0x58, B = 0x58, Name = "Grey35" },
            new XtermColor { Id = 241, R = 0x62, G = 0x62, B = 0x62, Name = "Grey39" },
            new XtermColor { Id = 242, R = 0x6c, G = 0x6c, B = 0x6c, Name = "Grey42" },
            new XtermColor { Id = 243, R = 0x76, G = 0x76, B = 0x76, Name = "Grey46" },
            new XtermColor { Id = 244, R = 0x80, G = 0x80, B = 0x80, Name = "Grey50" },
            new XtermColor { Id = 245, R = 0x8a, G = 0x8a, B = 0x8a, Name = "Grey54" },
            new XtermColor { Id = 246, R = 0x94, G = 0x94, B = 0x94, Name = "Grey58" },
            new XtermColor { Id = 247, R = 0x9e, G = 0x9e, B = 0x9e, Name = "Grey62" },
            new XtermColor { Id = 248, R = 0xa8, G = 0xa8, B = 0xa8, Name = "Grey66" },
            new XtermColor { Id = 249, R = 0xb2, G = 0xb2, B = 0xb2, Name = "Grey70" },
            new XtermColor { Id = 250, R = 0xbc, G = 0xbc, B = 0xbc, Name = "Grey74" },
            new XtermColor { Id = 251, R = 0xc6, G = 0xc6, B = 0xc6, Name = "Grey78" },
            new XtermColor { Id = 252, R = 0xd0, G = 0xd0, B = 0xd0, Name = "Grey82" },
            new XtermColor { Id = 253, R = 0xda, G = 0xda, B = 0xda, Name = "Grey85" },
            new XtermColor { Id = 254, R = 0xe4, G = 0xe4, B = 0xe4, Name = "Grey89" },
            new XtermColor { Id = 255, R = 0xee, G = 0xee, B = 0xee, Name = "Grey93" },
        };

        private readonly Dictionary<string, int> colorNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Character.ColorInfo[] colorPalette;

        public Character.ColorInfo this[int id]
        {
            get
            {
                return this.colorPalette[id];
            }
        }

        public Character.ColorInfo this[string name]
        {
            get
            {
                return this.colorPalette[this.colorNames[name]];
            }
        }

        public XtermPalette()
        {
            this.colorPalette = new Character.ColorInfo[this.DefaultPalette.Length];
            foreach (var c in this.DefaultPalette)
            {
                var colorInfo = new Character.ColorInfo { R = c.R, G = c.G, B = c.B }; 
                this.colorPalette[c.Id] = colorInfo;
                this.colorNames[c.Name] = c.Id;
            }
        }
    }
}
