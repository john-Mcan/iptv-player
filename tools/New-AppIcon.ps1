<#
.SYNOPSIS
    Genera src/IPTVPlayer/Assets/chucao.ico a partir del ícono vectorial del chucao.

.DESCRIPTION
    Dibuja con WPF el DrawingImage «ChucaoIcon» de Themes/Icon.xaml (la versión XAML de
    Assets/chucao.svg) en varios tamaños y los guarda como PNG dentro de un único .ico.
    No necesita herramientas externas: solo Windows PowerShell 5.1, o pwsh en Windows con -STA.

.EXAMPLE
    powershell -NoProfile -File tools/New-AppIcon.ps1
#>
param(
    [string]$Source = (Join-Path $PSScriptRoot '..\src\IPTVPlayer\Themes\Icon.xaml'),
    [string]$Output = (Join-Path $PSScriptRoot '..\src\IPTVPlayer\Assets\chucao.ico'),
    [int[]]$Sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
)

$ErrorActionPreference = 'Stop'

if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA') {
    throw 'WPF necesita un hilo STA. Ejecuta el script con Windows PowerShell o con «pwsh -STA».'
}

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

$xaml = [System.IO.File]::ReadAllText((Resolve-Path $Source), [System.Text.Encoding]::UTF8)
$dictionary = [System.Windows.Markup.XamlReader]::Parse($xaml)
$drawing = $dictionary['ChucaoIcon'].Drawing
$bounds = $drawing.Bounds

$images = foreach ($size in $Sizes) {
    # Centra el dibujo en un cuadrado con un margen del 3 %.
    $margin = $size * 0.03
    $scale = ($size - 2 * $margin) / [Math]::Max($bounds.Width, $bounds.Height)
    $offsetX = ($size - $bounds.Width * $scale) / 2 - $bounds.X * $scale
    $offsetY = ($size - $bounds.Height * $scale) / 2 - $bounds.Y * $scale

    $visual = New-Object System.Windows.Media.DrawingVisual
    $context = $visual.RenderOpen()
    $context.PushTransform((New-Object System.Windows.Media.MatrixTransform($scale, 0, 0, $scale, $offsetX, $offsetY)))
    $context.DrawDrawing($drawing)
    $context.Pop()
    $context.Close()

    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap($size, $size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)

    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $buffer = New-Object System.IO.MemoryStream
    $encoder.Save($buffer)

    [pscustomobject]@{ Size = $size; Data = $buffer.ToArray() }
}

# Formato ICO: cabecera de 6 bytes, una entrada de 16 bytes por imagen y después los PNG.
$file = [System.IO.File]::Create([System.IO.Path]::GetFullPath($Output))
$writer = New-Object System.IO.BinaryWriter($file)
try {
    $writer.Write([uint16]0)              # reservado
    $writer.Write([uint16]1)              # tipo: ícono
    $writer.Write([uint16]$images.Count)

    $offset = 6 + 16 * $images.Count
    foreach ($image in $images) {
        $dimension = if ($image.Size -ge 256) { 0 } else { $image.Size }   # 0 significa 256
        $writer.Write([byte]$dimension)   # ancho
        $writer.Write([byte]$dimension)   # alto
        $writer.Write([byte]0)            # colores de paleta
        $writer.Write([byte]0)            # reservado
        $writer.Write([uint16]1)          # planos
        $writer.Write([uint16]32)         # bits por píxel
        $writer.Write([uint32]$image.Data.Length)
        $writer.Write([uint32]$offset)
        $offset += $image.Data.Length
    }

    foreach ($image in $images) {
        $writer.Write($image.Data)
    }
}
finally {
    $writer.Dispose()
}

Write-Host "Ícono generado: $Output ($($Sizes -join ', ') px)"
