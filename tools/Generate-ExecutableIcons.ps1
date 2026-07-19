param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Add-Type -AssemblyName System.Drawing

function New-IconPng([int]$Size, [scriptblock]$Draw) {
    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        & $Draw $graphics $Size
        $stream = [System.IO.MemoryStream]::new()
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return (, $stream.ToArray())
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Write-Ico([string]$Path, [scriptblock]$Draw) {
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $images = @($sizes | ForEach-Object { New-IconPng $_ $Draw })
    $stream = [System.IO.File]::Create($Path)
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
            $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
            $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32)
            $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
            $offset += $images[$i].Length
        }
        foreach ($image in $images) { $writer.Write($image) }
    }
    finally {
        $writer.Dispose()
    }
}

$guiDraw = {
    param($g, $s)
    $m = $s * 0.08
    $board = [System.Drawing.RectangleF]::new($m, $m, $s - 2 * $m, $s - 2 * $m)
    $g.FillRectangle([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 210, 140, 45)), $board)
    $g.DrawRectangle([System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 73, 43, 18), [Math]::Max(1, $s * 0.055)), $board.X, $board.Y, $board.Width, $board.Height)
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(220, 70, 42, 18), [Math]::Max(1, $s * 0.025))
    foreach ($i in 1..3) {
        $p = $m + ($board.Width * $i / 4)
        $g.DrawLine($pen, $p, $m + $board.Height * 0.12, $p, $m + $board.Height * 0.88)
        $g.DrawLine($pen, $m + $board.Width * 0.12, $p, $m + $board.Width * 0.88, $p)
    }
    $r = $s * 0.18
    $g.FillEllipse([System.Drawing.Brushes]::Black, $s * 0.26 - $r / 2, $s * 0.26 - $r / 2, $r, $r)
    $g.FillEllipse([System.Drawing.Brushes]::White, $s * 0.74 - $r / 2, $s * 0.74 - $r / 2, $r, $r)
    $g.DrawEllipse([System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 45, 45, 45), [Math]::Max(1, $s * 0.018)), $s * 0.74 - $r / 2, $s * 0.74 - $r / 2, $r, $r)
}

$engineDraw = {
    param($g, $s)
    $dark = [System.Drawing.Color]::FromArgb(255, 31, 45, 61)
    $blue = [System.Drawing.Color]::FromArgb(255, 64, 177, 232)
    $face = [System.Drawing.RectangleF]::new($s * 0.12, $s * 0.20, $s * 0.76, $s * 0.66)
    $g.FillRectangle([System.Drawing.SolidBrush]::new($dark), $face)
    $g.DrawRectangle([System.Drawing.Pen]::new($blue, [Math]::Max(1, $s * 0.055)), $face.X, $face.Y, $face.Width, $face.Height)
    $g.DrawLine([System.Drawing.Pen]::new($dark, [Math]::Max(1, $s * 0.055)), $s * 0.5, $s * 0.20, $s * 0.5, $s * 0.08)
    $g.FillEllipse([System.Drawing.SolidBrush]::new($blue), $s * 0.44, $s * 0.035, $s * 0.12, $s * 0.12)
    $eye = $s * 0.15
    $g.FillEllipse([System.Drawing.SolidBrush]::new($blue), $s * 0.27 - $eye / 2, $s * 0.43 - $eye / 2, $eye, $eye)
    $g.FillEllipse([System.Drawing.SolidBrush]::new($blue), $s * 0.73 - $eye / 2, $s * 0.43 - $eye / 2, $eye, $eye)
    $mouthPen = [System.Drawing.Pen]::new($blue, [Math]::Max(1, $s * 0.045))
    $g.DrawLine($mouthPen, $s * 0.29, $s * 0.68, $s * 0.71, $s * 0.68)
    foreach ($x in @(0.40, 0.50, 0.60)) { $g.DrawLine($mouthPen, $s * $x, $s * 0.62, $s * $x, $s * 0.74) }
}

Write-Ico (Join-Path $RepositoryRoot 'KifuwarabeGo2026\GuiIcon.ico') $guiDraw
Write-Ico (Join-Path $RepositoryRoot 'KifuwarabeGo2026.Engine\EngineIcon.ico') $engineDraw
