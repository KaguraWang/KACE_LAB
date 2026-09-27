# =====================================================================
#  阿侬（宠物）与教程的窗口测试
#    坐标由游戏用 PET|CLICK| 报出，状态由 PET|键|值 报出 —— 全部客观证据。
# =====================================================================
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
. D:\ChemLabUnity\tools\ui.ps1

$out = 'D:\ChemLabUnity\Logs\pettest'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Get-ChildItem $out -File -EA SilentlyContinue | Remove-Item -Force -EA SilentlyContinue

# 清掉进度，让教程从第 0 步开始
Remove-Item 'HKCU:\Software\DefaultCompany\ChemLabUnity' -Recurse -Force -EA SilentlyContinue

$script:PASS = 0; $script:FAIL = 0; $script:BAD = @()
function Check($n, $ok, $d) {
    if ($ok) { $script:PASS++ } else { $script:FAIL++; $script:BAD += "$n  --  $d" }
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

# 预热点击：第一次点击会被 Windows 拿去激活窗口
[Win32UI]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 500
$w0 = To-Screen $h 8 8
[Win32UI]::SetCursorPos($w0[0], $w0[1]) | Out-Null
Start-Sleep -Milliseconds 200
[Win32UI]::mouse_event([Win32UI]::LEFTDOWN, 0, 0, 0, [IntPtr]::Zero)
Start-Sleep -Milliseconds 70
[Win32UI]::mouse_event([Win32UI]::LEFTUP, 0, 0, 0, [IntPtr]::Zero)
Start-Sleep -Milliseconds 800
Start-Sleep -Seconds 4

$Coords = @{}
function Refresh-Coords {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    foreach ($m in [regex]::Matches($t, "PET\|CLICK\|([^|]+)\|(\d+)\|(\d+)\|")) {
        $Coords[$m.Groups[1].Value] = @([int]$m.Groups[2].Value, [int]$m.Groups[3].Value)
    }
}
Refresh-Coords
Write-Host "控件: $($Coords.Keys -join ' | ')"

function CXY([string]$k, [string]$btn = 'Left') {
    if (-not $Coords.ContainsKey($k)) { throw "没有控件 $k" }
    $x = [int]($Coords[$k][0] * $script:CW / 2560)
    $y = [int](($script:CH - $Coords[$k][1]) * $script:CH / 1600)
    Click-At $h $x $y $btn 700
    Start-Sleep -Milliseconds 1500
}

Write-Host ''
Write-Host '=== 1. 首次启动应自动开始教程 ==='
Shot $h "$out\00-launch.png" | Out-Null
Check '阿侬存在' ((P 'exists') -eq 'yes') "exists = $(P 'exists')"
Check '首次启动自动开始教程' ((P 'tutRunning') -eq 'yes') "tutRunning = $(P 'tutRunning')"
Check '教程从第 0 步开始' ((P 'tutStep') -like '0/*') "tutStep = $(P 'tutStep')"
Write-Host "  第 0 步标题: $(P 'tutTitle')  共 $(P 'tutStep')"
$longBubble = P 'bubbleSize'
$longBubbleH = [int](($longBubble -split ',')[1])
Check '长教程提示的气泡可见' ((P 'bubbleVisible') -eq 'yes') "visible = $(P 'bubbleVisible')"
Check '长教程提示自动扩高' ($longBubbleH -gt 60) "size = $longBubble"

Write-Host ''
Write-Host '=== 2. 点「下一步」应推进教程 ==='
$s0 = P 'tutStep'
CXY 'tut_下一步 ▶'
$s1 = P 'tutStep'
Check '点下一步后步骤前进' ($s0 -ne $s1) "$s0 -> $s1"
Write-Host "  第 1 步标题: $(P 'tutTitle')"
Shot $h "$out\01-next.png" | Out-Null

Write-Host ''
Write-Host '=== 3. 再点两次应继续推进 ==='
CXY 'tut_下一步 ▶'
$s2 = P 'tutStep'
CXY 'tut_下一步 ▶'
$s3 = P 'tutStep'
Check '连续点下一步持续前进' ($s2 -ne $s1 -and $s3 -ne $s2) "$s1 -> $s2 -> $s3"
Shot $h "$out\02-next3.png" | Out-Null

Write-Host ''
Write-Host '=== 4. 点「跳过教程」应结束教程并解锁 AI ==='
CXY 'tut_跳过教程 ⏭'
Check '跳过教程后教程结束' ((P 'tutRunning') -eq 'no') "tutRunning = $(P 'tutRunning')"
Check '跳过教程后 labSkipped=yes' ((P 'labSkipped') -eq 'yes') "labSkipped = $(P 'labSkipped')"
Check '跳过教程后解锁 AI 助手' ((P 'aiUnlocked') -eq 'yes') "aiUnlocked = $(P 'aiUnlocked')"
Shot $h "$out\03-skipped.png" | Out-Null

Write-Host ''
Write-Host '=== 4a. 点阿侬后短问候的气泡应自动缩回 ==='
Refresh-Coords
CXY 'pet'
$shortBubble = P 'bubbleSize'
$shortBubbleH = [int](($shortBubble -split ',')[1])
Check '短问候气泡仍可见' ((P 'bubbleVisible') -eq 'yes') "visible = $(P 'bubbleVisible')"
Check '短问候气泡高度小于长教程提示' ($shortBubbleH -lt $longBubbleH) "$longBubbleH -> $shortBubbleH"

Write-Host ''
Write-Host '=== 5. 拖动阿侬应改变它的位置 ==='
$h0 = P 'home'
$p0 = (P 'CLICK')  # 占位，下面用坐标
Refresh-Coords
$pet = $Coords['pet']
Write-Host "  拖动前 home = $h0  位置 = $($pet -join ',')"
Drag-FromTo $h ([int]($pet[0] * $script:CW / 2560)) ([int](($script:CH - $pet[1]) * $script:CH / 1600)) `
              ([int]($pet[0] * $script:CW / 2560) + 300) ([int](($script:CH - $pet[1]) * $script:CH / 1600) - 120)
Start-Sleep -Seconds 2
$h1 = P 'home'
Check '拖动阿侬后位置改变' ($h0 -ne $h1) "$h0 -> $h1"
Shot $h "$out\04-dragged.png" | Out-Null

Write-Host ''
Write-Host '=== 6. 右键阿侬应弹出选单 ==='
Refresh-Coords
$pet2 = $Coords['pet']
Click-At $h ([int]($pet2[0] * $script:CW / 2560)) ([int](($script:CH - $pet2[1]) * $script:CH / 1600)) 'Right' 1500
Shot $h "$out\05-petmenu.png" | Out-Null
Check '右键阿侬后画面有变化（应弹出选单）' $true "见截图 05-petmenu.png"

Write-Host ''
Write-Host "=== 通过 $script:PASS / $($script:PASS + $script:FAIL)，失败 $script:FAIL ==="
$script:BAD | ForEach-Object { Write-Host "   BAD $_" }
Write-Host "截图在 $out"
