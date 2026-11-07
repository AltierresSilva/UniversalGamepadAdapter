$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$assetRoot=Join-Path $PSScriptRoot '..\src\UGA.App\Assets'
$source=[Drawing.Image]::FromFile((Join-Path $assetRoot 'UGA-symbol.png'))
$images=@()
try {
    foreach($size in @(16,24,32,48,64,128,256)) {
        $bitmap=[Drawing.Bitmap]::new($size,$size,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g=[Drawing.Graphics]::FromImage($bitmap)
        try {
            $g.Clear([Drawing.Color]::Transparent)
            $g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
            $g.CompositingQuality=[Drawing.Drawing2D.CompositingQuality]::HighQuality
            $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            # The compact central emblem fills the Windows icon slot without a wide gamepad or empty margins.
            $padding=0
            $scale=[Math]::Min(($size-2*$padding)/$source.Width,($size-2*$padding)/$source.Height)
            $width=[int][Math]::Round($source.Width*$scale)
            $height=[int][Math]::Round($source.Height*$scale)
            $target=[Drawing.Rectangle]::new([int](($size-$width)/2),[int](($size-$height)/2),$width,$height)
            $g.DrawImage($source,$target,0,0,$source.Width,$source.Height,[Drawing.GraphicsUnit]::Pixel)
        } finally {$g.Dispose()}
        try {
            $memory=[IO.MemoryStream]::new();$bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
            $images+=,@{Size=$size;Bytes=$memory.ToArray()};$memory.Dispose()
            if($size -eq 256) {$bitmap.Save((Join-Path $assetRoot 'UGA.png'),[Drawing.Imaging.ImageFormat]::Png)}
        } finally {$bitmap.Dispose()}
    }
} finally {$source.Dispose()}
$stream=[IO.File]::Open((Join-Path $assetRoot 'UGA.ico'),[IO.FileMode]::Create)
$writer=[IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$images.Count)
    $offset=6+16*$images.Count
    foreach($image in $images) {
        $dim=if($image.Size -eq 256){0}else{$image.Size}
        $writer.Write([byte]$dim);$writer.Write([byte]$dim);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$image.Bytes.Length);$writer.Write([uint32]$offset)
        $offset+=$image.Bytes.Length
    }
    foreach($image in $images){$writer.Write([byte[]]$image.Bytes)}
} finally {$writer.Dispose();$stream.Dispose()}
