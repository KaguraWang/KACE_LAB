# =====================================================================
#  窗口级 UI 自动化（整份重写 —— 之前被补丁打烂过一次，重写比继续补更稳）
#
#  找窗口按**进程**找，不能按标题 —— FindWindow(null,"ChemLab")
#  会匹配到标题里含 ChemLab 的浏览器窗口。
#  截图用 PrintWindow 并裁到客户区 —— CopyFromScreen 抓的是屏幕上
#  视觉最上层的东西，播放器容易被浏览器盖住。
#  必须调 SetProcessDpiAwareness —— 否则坐标被虚拟化，
#  GetClientRect 报虚拟值、截图是真实像素，两套坐标系对不上。
# =====================================================================

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -Path 'D:\ChemLabUnity\tools\Win32UI.cs'

$script:DpiAware = [Win32UI]::MakeDpiAware()

function Find-AppWindow {
    $p = Get-Process -Name 'ChemLab' -EA SilentlyContinue |
         Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if (-not $p) { return [IntPtr]::Zero }
    $script:AppProc = $p
    return $p.MainWindowHandle
}

function Start-App {
    param([string]$Exe = 'D:\ChemLabUnity\Build\ChemLab.exe',
          [string[]]$ExtraArgs = @(), [int]$WaitSec = 15)
    Get-Process -Name 'ChemLab' -EA SilentlyContinue | ForEach-Object { try { $_.Kill() } catch {} }
    Start-Sleep -Milliseconds 1500
    if ($ExtraArgs.Count -gt 0) { Start-Process -FilePath $Exe -ArgumentList $ExtraArgs }
    else { Start-Process -FilePath $Exe }
    for ($i = 0; $i -lt $WaitSec * 2; $i++) {
        Start-Sleep -Milliseconds 500
        $h = Find-AppWindow
        if ($h -ne [IntPtr]::Zero) { Start-Sleep -Seconds 4; return $h }
    }
    throw "窗口没起来"
}

function Stop-App {
    Get-Process -Name 'ChemLab' -EA SilentlyContinue | ForEach-Object { try { $_.Kill() } catch {} }
    Start-Sleep -Milliseconds 1000
}

function Focus-App {
    param($h)
    # 单靠 SetForegroundWindow 常常静默失败，之后所有键盘注入都到不了程序。
    # ForceForeground 会先把本线程的输入挂到目标窗口线程上再抢前台。
    [Win32UI]::ForceForeground($h) | Out-Null
    Start-Sleep -Milliseconds 300
}

function To-Screen {
    param($h, [int]$x, [int]$y)
    $p = New-Object Win32UI+POINT
    $p.X = $x; $p.Y = $y
    [Win32UI]::ClientToScreen($h, [ref]$p) | Out-Null
    return @($p.X, $p.Y)
}

function Move-Client {
    param($h, [int]$x, [int]$y)
    $s = To-Screen $h $x $y
    [Win32UI]::SetCursorPos($s[0], $s[1]) | Out-Null
    Start-Sleep -Milliseconds 50
}

function Warm-Up {
    param($h)
    [Win32UI]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 500
    $n = To-Screen $h 8 8
    [Win32UI]::SetCursorPos($n[0], $n[1]) | Out-Null
    Start-Sleep -Milliseconds 200
    [Win32UI]::mouse_event([Win32UI]::LEFTDOWN, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 70
    [Win32UI]::mouse_event([Win32UI]::LEFTUP, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 800
}

function Click-At {
    param($h, [int]$x, [int]$y, [string]$Button = 'Left', [int]$SettleMs = 500)
    Focus-App $h
    Move-Client $h $x $y
    Start-Sleep -Milliseconds 120
    if ($Button -eq 'Right') {
        [Win32UI]::mouse_event([Win32UI]::RIGHTDOWN, 0, 0, 0, [IntPtr]::Zero)
        Start-Sleep -Milliseconds 70
        [Win32UI]::mouse_event([Win32UI]::RIGHTUP, 0, 0, 0, [IntPtr]::Zero)
    } else {
        [Win32UI]::mouse_event([Win32UI]::LEFTDOWN, 0, 0, 0, [IntPtr]::Zero)
        Start-Sleep -Milliseconds 70
        [Win32UI]::mouse_event([Win32UI]::LEFTUP, 0, 0, 0, [IntPtr]::Zero)
    }
    Start-Sleep -Milliseconds $SettleMs
}

function Drag-FromTo {
    param($h, [int]$x0, [int]$y0, [int]$x1, [int]$y1, [string]$Button = 'Left',
          [int]$Steps = 16, [int]$SettleMs = 700)
    Focus-App $h
    Move-Client $h $x0 $y0
    Start-Sleep -Milliseconds 150
    if ($Button -eq 'Right') { $dn = [Win32UI]::RIGHTDOWN; $up = [Win32UI]::RIGHTUP }
    else { $dn = [Win32UI]::LEFTDOWN; $up = [Win32UI]::LEFTUP }
    [Win32UI]::mouse_event($dn, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 100
    for ($i = 1; $i -le $Steps; $i++) {
        $x = [int]($x0 + ($x1 - $x0) * $i / $Steps)
        $y = [int]($y0 + ($y1 - $y0) * $i / $Steps)
        Move-Client $h $x $y
        Start-Sleep -Milliseconds 25
    }
    Start-Sleep -Milliseconds 150
    [Win32UI]::mouse_event($up, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds $SettleMs
}

function Scroll-At {
    param($h, [int]$x, [int]$y, [int]$Notches = 1)
    Focus-App $h
    Move-Client $h $x $y
    for ($i = 0; $i -lt [Math]::Abs($Notches); $i++) {
        $d = if ($Notches -gt 0) { [uint32]120 } else { [uint32](-120) }
        [Win32UI]::mouse_event([Win32UI]::WHEEL, 0, 0, $d, [IntPtr]::Zero)
        Start-Sleep -Milliseconds 90
    }
    Start-Sleep -Milliseconds 350
}

$script:VK = @{ 'Enter'=13; 'Esc'=27; 'Space'=32; 'Tab'=9; 'Back'=8;
                'W'=87; 'A'=65; 'S'=83; 'D'=68; 'E'=69; 'Q'=81; 'Shift'=16;
                'Left'=37; 'Up'=38; 'Right'=39; 'Down'=40 }

function Key-Press {
    param([string]$Name, [int]$SettleMs = 200)
    $vk = $script:VK[$Name]
    if (-not $vk) { throw "未知按键 $Name" }
    [Win32UI]::keybd_event([byte]$vk, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [Win32UI]::keybd_event([byte]$vk, 0, [Win32UI]::KEYUP, [IntPtr]::Zero)
    Start-Sleep -Milliseconds $SettleMs
}

$script:VKCHAR = 'abcdefghijklmnopqrstuvwxyz0123456789 -./'
$script:VKCODE = @(0x41,0x42,0x43,0x44,0x45,0x46,0x47,0x48,0x49,0x4A,0x4B,0x4C,0x4D,
                   0x4E,0x4F,0x50,0x51,0x52,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5A,
                   0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,
                   0x20,0xBD,0xBE,0xBF)

function Type-Text {
    param([string]$Text, [int]$SettleMs = 500)
    # 用 SendInput 的 Unicode 路径逐字发送。
    # 试过 SendKeys.SendWait 和 keybd_event，两者在游戏里一个字符都收不到；
    # Unicode 路径不依赖键盘布局，也不用手工处理 Shift。
    foreach ($ch in $Text.ToCharArray()) {
        [Win32UI]::TypeChar($ch) | Out-Null
        Start-Sleep -Milliseconds 90
    }
    Start-Sleep -Milliseconds $SettleMs
}

function Shot {
    param($h, [string]$Path)
    [Win32UI]::ShowWindow($h, [Win32UI]::SW_RESTORE) | Out-Null
    [Win32UI]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 350
    $wr = New-Object Win32UI+RECT
    [Win32UI]::GetWindowRect($h, [ref]$wr) | Out-Null
    $ww = $wr.R - $wr.L; $wh = $wr.B - $wr.T
    if ($ww -le 0 -or $wh -le 0) { throw "窗口尺寸异常 $ww x $wh" }
    $org = New-Object Win32UI+POINT
    $org.X = 0; $org.Y = 0
    [Win32UI]::ClientToScreen($h, [ref]$org) | Out-Null
    $bx = $org.X - $wr.L
    $by = $org.Y - $wr.T
    $cr = New-Object Win32UI+RECT
    [Win32UI]::GetClientRect($h, [ref]$cr) | Out-Null
    $cw = $cr.R; $chh = $cr.B
    $full = New-Object System.Drawing.Bitmap($ww, $wh)
    $g = [System.Drawing.Graphics]::FromImage($full)
    $hdc = $g.GetHdc()
    $ok = [Win32UI]::PrintWindow($h, $hdc, [Win32UI]::PW_RENDERFULLCONTENT)
    $g.ReleaseHdc($hdc); $g.Dispose()
    if (-not $ok) {
        $g2 = [System.Drawing.Graphics]::FromImage($full)
        $g2.CopyFromScreen($wr.L, $wr.T, 0, 0, (New-Object System.Drawing.Size($ww, $wh)))
        $g2.Dispose()
    }
    $out = New-Object System.Drawing.Bitmap($cw, $chh)
    $g3 = [System.Drawing.Graphics]::FromImage($out)
    $g3.DrawImage($full, (New-Object System.Drawing.Rectangle(0, 0, $cw, $chh)),
                  (New-Object System.Drawing.Rectangle($bx, $by, $cw, $chh)),
                  [System.Drawing.GraphicsUnit]::Pixel)
    $g3.Dispose()
    $out.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose(); $full.Dispose()
    return $Path
}

function RegionColor {
    param([string]$Path, [int]$X, [int]$Y, [int]$W, [int]$H)
    $img = [System.Drawing.Image]::FromFile($Path)
    $bmp = New-Object System.Drawing.Bitmap($W, $H)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.DrawImage($img, (New-Object System.Drawing.Rectangle(0,0,$W,$H)),
                 (New-Object System.Drawing.Rectangle($X,$Y,$W,$H)), [System.Drawing.GraphicsUnit]::Pixel)
    $r=0;$gg=0;$b=0;$n=0
    for ($y = 0; $y -lt $H; $y += 3) {
        for ($x = 0; $x -lt $W; $x += 3) {
            $c = $bmp.GetPixel($x,$y); $r += $c.R; $gg += $c.G; $b += $c.B; $n++
        }
    }
    $g.Dispose(); $bmp.Dispose(); $img.Dispose()
    return @([int]($r/$n), [int]($gg/$n), [int]($b/$n))
}

function ColorDist {
    param($A, $B)
    return [Math]::Sqrt([Math]::Pow($A[0]-$B[0],2) + [Math]::Pow($A[1]-$B[1],2) + [Math]::Pow($A[2]-$B[2],2))
}

function DiffRatio {
    param([string]$A, [string]$B, [int]$X, [int]$Y, [int]$W, [int]$H, [int]$Thresh = 40, [int]$Step = 2)
    $ia = [System.Drawing.Image]::FromFile($A)
    $ib = [System.Drawing.Image]::FromFile($B)
    $ba = New-Object System.Drawing.Bitmap($W, $H)
    $bb = New-Object System.Drawing.Bitmap($W, $H)
    $ga = [System.Drawing.Graphics]::FromImage($ba)
    $gb = [System.Drawing.Graphics]::FromImage($bb)
    $rect = New-Object System.Drawing.Rectangle(0,0,$W,$H)
    $src  = New-Object System.Drawing.Rectangle($X,$Y,$W,$H)
    $ga.DrawImage($ia, $rect, $src, [System.Drawing.GraphicsUnit]::Pixel)
    $gb.DrawImage($ib, $rect, $src, [System.Drawing.GraphicsUnit]::Pixel)
    $chg = 0; $tot = 0
    for ($y = 0; $y -lt $H; $y += $Step) {
        for ($x = 0; $x -lt $W; $x += $Step) {
            $ca = $ba.GetPixel($x,$y); $cb = $bb.GetPixel($x,$y)
            $d = [Math]::Abs($ca.R-$cb.R) + [Math]::Abs($ca.G-$cb.G) + [Math]::Abs($ca.B-$cb.B)
            if ($d -gt $Thresh) { $chg++ }
            $tot++
        }
    }
    $ga.Dispose(); $gb.Dispose(); $ba.Dispose(); $bb.Dispose(); $ia.Dispose(); $ib.Dispose()
    return [double]$chg / [Math]::Max(1, $tot)
}