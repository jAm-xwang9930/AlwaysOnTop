param([string]$Color = "#64748B", [string]$Out = "AlwaysOnTop.ico")
Add-Type -AssemblyName System.Drawing
$hex = $Color.TrimStart("#")
$r = [Convert]::ToInt32($hex.Substring(0,2),16)
$g = [Convert]::ToInt32($hex.Substring(2,2),16)
$b = [Convert]::ToInt32($hex.Substring(4,2),16)
$accent = [System.Drawing.Color]::FromArgb(255,$r,$g,$b)
$sizes = @(16,20,24,32,40,48,64,256)
$pf = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
function RR([single]$x,[single]$y,[single]$w,[single]$h,[single]$rad){
  $p = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = [single]($rad*2)
  $p.AddArc($x,$y,$d,$d,180,90); $p.AddArc($x+$w-$d,$y,$d,$d,270,90)
  $p.AddArc($x+$w-$d,$y+$h-$d,$d,$d,0,90); $p.AddArc($x,$y+$h-$d,$d,$d,90,90)
  $p.CloseFigure(); return $p }
function DrawC([int]$size){
  $bmp = New-Object System.Drawing.Bitmap -ArgumentList @($size,$size,$pf)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  $g.Clear([System.Drawing.Color]::Transparent)
  $s = [single]$size
  $small = $size -le 24
  $stroke = [single][Math]::Max(1.25, $s*0.048)
  $pen = New-Object System.Drawing.Pen -ArgumentList @($accent,$stroke)
  $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
  $g.DrawPath($pen, (RR ([single]($s*0.10)) ([single]($s*0.10)) ([single]($s*0.60)) ([single]($s*0.46)) ([single]($s*0.10))))
  $fp = RR ([single]($s*0.30)) ([single]($s*0.38)) ([single]($s*0.60)) ([single]($s*0.50)) ([single]($s*0.09))
  $g.FillPath((New-Object System.Drawing.SolidBrush($accent)), $fp)
  if(-not $small){
    $bp = RR ([single]($s*0.37)) ([single]($s*0.455)) ([single]($s*0.34)) ([single]($s*0.075)) ([single]($s*0.035))
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)), $bp)
    $bp.Dispose()
  }
  $fp.Dispose(); $pen.Dispose(); $g.Dispose()
  return $bmp }
$frames = @()
foreach($sz in $sizes){
  $bmp = DrawC $sz
  $rect = New-Object System.Drawing.Rectangle -ArgumentList @(0,0,$sz,$sz)
  $bd = $bmp.LockBits($rect,[System.Drawing.Imaging.ImageLockMode]::ReadOnly,$pf)
  $stride = $bd.Stride; $bytes = New-Object byte[] ($stride*$sz)
  [System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0,$bytes,0,$bytes.Length)
  $bmp.UnlockBits($bd); $bmp.Dispose()
  if($sz -le 64){
    $xor = New-Object byte[] ($sz*$sz*4)
    for($row=0;$row -lt $sz;$row++){ [Array]::Copy($bytes,(($sz-1-$row)*$stride),$xor,($row*$sz*4),($sz*4)) }
    $maskStride = [int]([Math]::Floor(($sz+31)/32)*4)
    $and = New-Object byte[] ($maskStride*$sz)
    $frames += ,@($sz,$xor,$and)
  } else {
    $msPng = New-Object System.IO.MemoryStream
    $bmp2 = DrawC $sz
    $bmp2.Save($msPng,[System.Drawing.Imaging.ImageFormat]::Png)
    $bmp2.Dispose()
    $frames += ,@($sz,$msPng.ToArray(),$null)
    $msPng.Dispose()
  }
}
$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$frames.Count)
$offset = 6 + 16*$frames.Count
$dirs = @()
foreach($f in $frames){
  $len = if($f[2] -eq $null){ $f[1].Length } else { $f[1].Length + $f[2].Length + 40 }
  $dirs += ,@($f[0],$len,$offset); $offset += $len }
foreach($d in $dirs){
  $sz = $d[0]
  $bw.Write([byte]$(if($sz -ge 256){0}else{$sz})); $bw.Write([byte]$(if($sz -ge 256){0}else{$sz}))
  $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([uint16]1); $bw.Write([uint16]32)
  $bw.Write([uint32]$d[1]); $bw.Write([uint32]$d[2]) }
foreach($f in $frames){
  $sz = $f[0]
  if($f[2] -ne $null){
    $bw.Write([uint32]40); $bw.Write([int32]$sz); $bw.Write([int32]($sz*2))
    $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]0)
    $bw.Write([uint32]($f[1].Length+$f[2].Length))
    $bw.Write([int32]0); $bw.Write([int32]0); $bw.Write([uint32]0); $bw.Write([uint32]0)
    $bw.Write($f[1]); $bw.Write($f[2])
  } else { $bw.Write($f[1]) } }
$bw.Flush()
$target = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) $Out
[System.IO.File]::WriteAllBytes($target, $ms.ToArray())
$bw.Dispose(); $ms.Dispose()
Write-Output ("ico written: " + $target + " (" + (Get-Item $target).Length + " bytes, " + $frames.Count + " sizes)")