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

## Color Management
In deciding how to manage the delta between rendering a "presentation-free" buffer in the library
and leacving presentation to the application it made sense to support the "xterm palette." A user
can provide a palette to the buffer, which will be used when characters are rendered to set the
appropriate RGB values for either "classic" (16 color) terminals or 256 color palette xterm values.
This means that the RGB values are always set by the library for the rendering library to use as
desired. Given that more than one escape sequence can reference color palettes in a plethora of
ways this felt like the most appropriate choice.

As a consequence of this decision, however, runtime palette changes won't be reflected in text
that has already been rendered. This can be considered a feature or a bug, depending on your mood.

## Dev notes

Some cool sites I've found while putting this together.
- VT codes doc: https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences
- xterm: http://invisible-island.net/xterm/ctlseqs/ctlseqs.html
- This Honeywell (???) document: https://country.honeywellaidc.com/CatalogDocuments/RFTERM-UG%20Rev%20D.pdf
- Sample for ConPTY: https://github.com/Microsoft/console/tree/master/samples/ConPTY/EchoCon
- a random fun site: http://wiki.bash-hackers.org/scripting/terminalcodes