# =====================================================================
#  手动输入焓变对话框的窗口测试（重写）
#
#  坐标系说明：游戏报的是屏幕坐标（Y 从下往上），
#  换到客户区要翻 Y（clientY = 屏高 - screenY）。
#
#  关键教训：控件坐标**只能取最后一块审计**。
#  从整个日志正则抓会把历史记录里的过期位置一起收进来 ——
#  上一次就这样拿到一个 y=1907（屏幕只有 1600 高），
#  换算后变成 -307，点到了窗口外面。
# =====================================================================
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
. D:\ChemLabUnity\tools\ui.ps1

$out = 'D:\ChemLabUnity\Logs\dlgtest'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Get-ChildItem $out -File -EA SilentlyContinue | Remove-Item -Force -EA SilentlyContinue
Remove-Item 'HKCU:\Software\DefaultCompany\ChemLabUnity' -Recurse -Force -EA SilentlyContinue

$script:PASS = 0; $script:FAIL = 0; $script:BAD = @()
function Check($n, $ok, $d) {
    if ($ok) { $script:PASS++ } else { $script:FAIL++; $script:BAD += "$n -- $d" }
    Write-Host "  $(if ($ok) {'[OK ]'} else {'[BAD]'}) $n  --  $d"
}

$log = "$env:USERPROFILE\AppData\LocalLow\DefaultCompany\ChemLabUnity\Player.log"

# ---- 只取最后一块 [CalcAudit] ----
# 只取最后一块 [CalcAudit]：从最后一个 "[CalcAudit]" 到下一个 "[CalcAudit]" 之前。
# 不能靠 DUMPEND —— -calcDemo 的审计没有那个标记（只有 -dumpLayout 才有）。
# 只取最后一块审计。用 AUDITSTART / AUDITEND 标记 ——
# 不能靠 "[CalcAudit]" 定位：审计每行都以它开头，LastIndexOf 找到的是最后一行。
function LastAudit {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    if (-not $t) { return "" }
    $i = $t.LastIndexOf("AUDITSTART")
    if ($i -lt 0) { return "" }
    $j = $t.IndexOf("AUDITEND", $i)
    if ($j -lt 0) { $j = [Math]::Min($t.Length, $i + 6000) }
    return $t.Substring($i, $j - $i)
}
function Ctrl([string]$name) {
    # 审计每 1.2 秒才打一次，控件刚出现时要等它刷新 —— 直接读会拿到上一块
    for ($try = 0; $try -lt 6; $try++) {
        $blk = LastAudit
        $m = [regex]::Matches($blk, "CLICK\|" + [regex]::Escape($name) + "\|(\d+)\|(\d+)\|")
        if ($m.Count -gt 0) {
            $x = [int]$m[$m.Count - 1].Groups[1].Value
            $y = [int]$m[$m.Count - 1].Groups[2].Value
            if ($x -ge 0 -and $x -le 2560 -and $y -ge 0 -and $y -le 1600) { return @($x, $y) }
        }
        Start-Sleep -Milliseconds 900
    }
    return $null
}
function St([string]$key) {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    $m = [regex]::Matches($t, "STATE\|$key\|([^\r\n|]+)")
    if ($m.Count -eq 0) { return "" }
    return $m[$m.Count - 1].Groups[1].Value
}
function EdgeDH([string]$eq) {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    $m = [regex]::Matches($t, "STATE\|edge\|([^|]*)\|(-?[0-9.]+)\|")
    $last = ''
    foreach ($x in $m) { if ($x.Groups[1].Value -eq $eq) { $last = $x.Groups[2].Value } }
    return $last
}

$h = Start-App -ExtraArgs @('-calcDemo','-petAudit','-noTut')
$cr = New-Object Win32UI+RECT
[Win32UI]::GetClientRect($h, [ref]$cr) | Out-Null
$script:CW = $cr.R; $script:CH = $cr.B
Write-Host "客户区 $($cr.R)x$($cr.B)"
Warm-Up $h
Start-Sleep -Seconds 2

# 屏幕坐标 -> 客户区坐标（Y 要翻）
function ClickScreen($p, [string]$btn = 'Left', [int]$wait = 2500) {
    if ($p -eq $null) { throw "坐标为空" }
    $x = [int]($p[0] * $script:CW / 2560)
    $y = [int](($script:CH - $p[1]) * $script:CH / 1600)
    Write-Host "    点在客户区 ($x, $y)"
    Click-At $h $x $y $btn 800
    Start-Sleep -Milliseconds $wait
}

Write-Host ''
Write-Host '=== 1. 右键连线 C → CO₂ ==='
$ep = Ctrl 'edge_C → CO₂'
Write-Host "  连线标注屏幕坐标: $($ep -join ',')"
ClickScreen $ep 'Right'
Check '右键连线后选单打开' ((St 'menu') -eq 'open') "menu = $(St 'menu')"
Shot $h "$out\01-edgemenu.png" | Out-Null

Write-Host ''
Write-Host '=== 2. 点「编辑焓变值」 ==='
$p2 = Ctrl 'menu_编辑焓变值'
Write-Host "  「编辑焓变值」屏幕坐标: $($p2 -join ',')"
ClickScreen $p2 'Left'
$src = Ctrl 'menu_手动输入…'
Check '出现焓变来源选单（有「手动输入…」）' ($src -ne $null) "手动输入… 在 $($src -join ',')"
Shot $h "$out\02-sourcemenu.png" | Out-Null

Write-Host ''
Write-Host '=== 3. 点「手动输入…」，应弹出对话框 ==='
$before = EdgeDH 'C->CO₂'
Write-Host "  改之前 C->CO₂ 的 ΔH = $before"
ClickScreen $src 'Left'
$dlg = St 'dlg'
Check '点手动输入后对话框出现' ($dlg -eq 'open') "dlg = $dlg"
Shot $h "$out\03-dialog.png" | Out-Null

$ok = St 'dlgOK'
Write-Host "  对话框「确定」屏幕坐标: $ok"
Check '对话框里有「确定」按钮且坐标有效' ($ok -ne '') "dlgOK = $ok"

Write-Host ''
Write-Host '=== 4. 输入 777 并点确定 ==='
Type-Text '777' 1200
if ($ok -ne '') {
    $pp = $ok.Split(',')
    ClickScreen @([int]$pp[0], [int]$pp[1]) 'Left' 2500
    Start-Sleep -Seconds 1
    $after = EdgeDH 'C->CO₂'
    Write-Host "  改之后 C->CO₂ 的 ΔH = $after"
    Shot $h "$out\04-committed.png" | Out-Null
    Check '点确定后对话框关闭' ((St 'dlg') -eq 'closed') "dlg = $(St 'dlg')"
    Check '连线的 ΔH 变成手动输入的值' ($after -ne $before) "ΔH $before -> $after"
} else {
    Check '点确定' $false "没有 dlgOK 坐标"
}

Write-Host ''
Write-Host "=== 通过 $script:PASS / $($script:PASS + $script:FAIL)，失败 $script:FAIL ==="
$script:BAD | ForEach-Object { Write-Host "   BAD $_" }
Write-Host "截图在 $out"
Stop-App
