$colorsFile = join-path $PSScriptRoot "xterm-colors.json"
$colorData = get-content $colorsFIle | convertfrom-json
foreach ($c in $colorData)
{
    $id = $c.colorId
    $r = $c.rgb.r.ToString("x2")
    $g = $c.rgb.g.ToString("x2")
    $b = $c.rgb.b.ToString("x2")
    $name = $c.name
    write-host "new XtermColor { Id = ${id}, R = 0x$r, G = 0x$g, B = 0x$b, Name = `"${name}`" },"
}