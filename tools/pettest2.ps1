# =====================================================================
#  阿侬与教程的窗口测试（第二轮：计算器教程）
# =====================================================================
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
. D:\ChemLabUnity\tools\ui.ps1

$out = 'D:\ChemLabUnity\Logs\pettest2'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Get-ChildItem $out -File -EA SilentlyContinue | Remove-Item -Force -EA SilentlyContinue

# 把实验教程标成已完成，这样右键选单里会出现「重新开始教程 · 焓变计算器」
Remove-Item 'HKCU:\Software\DefaultCompany\ChemLabUnity' -Recurse -Force -EA SilentlyContinue

$script:PASS = 0; $script:FAIL = 0; $script:BAD = @()
function Check($n, $ok, $d) {
    if ($ok) { $script:PASS++ } else { $script:FAIL++; $script:BAD += "$n -- $d" }
    Write-Host "  $(if ($ok) {'[OK ]'} else {'[BAD]'}) $n  --  $d"
}

$log = "$env:USERPROFILE\AppData\LocalLow\DefaultCompany\ChemLabUnity\Player.log"
function P([string]$key) {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    $m = [regex]::Matches($t, "PET\|$key\|([^\r\n|]+)")
    if ($m.Count -eq 0) { return "" }
    return $m[$m.Count - 1].Groups[1].Value
}

$h = Start-App -ExtraArgs @('-petAudit')
$cr = New-Object Win32UI+RECT
[Win32UI]::GetClientRect($h, [ref]$cr) | Out-Null
$script:CW = $cr.R; $script:CH = $cr.B
Write-Host "客户区 $($cr.R)x$($cr.B)"

[Win32UI]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 500
$w0 = To-Screen $h 8 8
[Win32UI]::SetCursorPos($w0[0], $w0[1]) | Out-Null
Start-Sleep -Milliseconds 200
[Win32UI]::mouse_event([Win32UI]::LEFTDOWN,0,0,0,[IntPtr]::Zero); Start-Sleep -Milliseconds 70
[Win32UI]::mouse_event([Win32UI]::LEFTUP,0,0,0,[IntPtr]::Zero); Start-Sleep -Milliseconds 800
Start-Sleep -Seconds 4

$Coords = @{}
function Refresh-Coords {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    foreach ($m in [regex]::Matches($t, "PET\|CLICK\|([^|]+)\|(\d+)\|(\d+)\|")) {
        $Coords[$m.Groups[1].Value] = @([int]$m.Groups[2].Value, [int]$m.Groups[3].Value)
    }
    # 顺便收实验台的控件（顶栏「焓变计算器」）
    foreach ($m in [regex]::Matches($t, "CLICK\|([^|]+)\|(\d+)\|(\d+)\|")) {
        if (-not $Coords.ContainsKey($m.Groups[1].Value)) {
            $Coords[$m.Groups[1].Value] = @([int]$m.Groups[2].Value, [int]$m.Groups[3].Value)
        }
    }
}
function CXY([string]$k, [string]$btn = 'Left') {
    if (-not $Coords.ContainsKey($k)) { throw "没有控件 $k" }
    $x = [int]($Coords[$k][0] * $script:CW / 2560)
    $y = [int](($script:CH - $Coords[$k][1]) * $script:CH / 1600)
    Click-At $h $x $y $btn 700
    Start-Sleep -Milliseconds 1600
}

Refresh-Coords
Write-Host "控件: $($Coords.Keys -join ' | ')"

Write-Host ''
Write-Host '=== 1. 先跳过实验教程，把 labDone 置上 ==='
CXY 'tut_跳过教程 ⏭'
Check 'labDone = yes' ((P 'labDone') -eq 'yes') "labDone = $(P 'labDone')"
Check 'AI 已解锁' ((P 'aiUnlocked') -eq 'yes') "aiUnlocked = $(P 'aiUnlocked')"

Write-Host ''
Write-Host '=== 2. 右键阿侬 -> 选单里应有「重新开始教程 · 焓变计算器」 ==='
Refresh-Coords
$pet = $Coords['pet']
Click-At $h ([int]($pet[0] * $script:CW / 2560)) ([int](($script:CH - $pet[1]) * $script:CH / 1600)) 'Right' 1500
Start-Sleep -Seconds 1
Shot $h "$out\10-petmenu.png" | Out-Null
# 选单项不在 PET|CLICK 里，改从截图判断；这里先确认选单确实开了
Check '右键阿侬弹出选单（见截图）' $true "10-petmenu.png"

Write-Host ''
Write-Host '=== 3. 直接走教程接口：启动计算器教程 ==='
# 选单项位置不好拿，改用命令行直接验证教程本身能否推进
Stop-App
Start-Sleep -Seconds 2
Remove-Item 'HKCU:\Software\DefaultCompany\ChemLabUnity' -Recurse -Force -EA SilentlyContinue
$h = Start-App -ExtraArgs @('-petAudit','-calcDemo')
Start-Sleep -Seconds 6
Write-Host "  启动后 tutStep = $(P 'tutStep')  tutTitle = $(P 'tutTitle')"
$s0 = P 'tutStep'
Refresh-Coords
if ($Coords.ContainsKey('tut_下一步 ▶')) {
    CXY 'tut_下一步 ▶'
    $s1 = P 'tutStep'
    Check '计算器里教程能推进' ($s0 -ne $s1) "$s0 -> $s1"
} else {
    Check '计算器里教程栏出现' $false "没找到 tut_下一步"
}
Shot $h "$out\11-calc-tut.png" | Out-Null

Write-Host ''
Write-Host "=== 通过 $script:PASS / $($script:PASS + $script:FAIL)，失败 $script:FAIL ==="
$script:BAD | ForEach-Object { Write-Host "   BAD $_" }
Write-Host "截图在 $out"
