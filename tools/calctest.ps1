# =====================================================================
#  焓变计算器的真实窗口测试
#    坐标由游戏用 [CalcAudit] CLICK| 报出（节点名直接用化学式），
#    状态读 [CalcState] STATE|。全部是客观证据。
#
#  两个已踩过的坑：
#   1. 游戏每 1.2 秒才打一次状态 —— 点完必须等 ≥1.9 秒再读，否则读到旧值
#   2. 右键在「拉线中」是**取消拉线**（设计如此），不会弹选单 ——
#      测选单必须先在非拉线状态下右键
# =====================================================================
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
. D:\ChemLabUnity\tools\ui.ps1

$out = 'D:\ChemLabUnity\Logs\calctest'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Get-ChildItem $out -File -EA SilentlyContinue | Remove-Item -Force -EA SilentlyContinue

$reg = 'HKCU:\Software\DefaultCompany\ChemLabUnity'
New-Item -Path $reg -Force | Out-Null
Set-ItemProperty -Path $reg -Name 'anon.prog.lab' -Value 1
Set-ItemProperty -Path $reg -Name 'anon.prog.calc' -Value 1

$script:PASS = 0; $script:FAIL = 0; $script:BAD = @()
function Check($n, $ok, $d) {
    if ($ok) { $script:PASS++ } else { $script:FAIL++; $script:BAD += "$n  --  $d" }
    $m = if ($ok) { '[OK ]' } else { '[BAD]' }
    Write-Host "  $m $n  --  $d"
}

$log = "$env:USERPROFILE\AppData\LocalLow\DefaultCompany\ChemLabUnity\Player.log"
function St([string]$key) {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    $m = [regex]::Matches($t, "STATE\|$key\|([^\r\n|]+)")
    if ($m.Count -eq 0) { return "" }
    return $m[$m.Count - 1].Groups[1].Value
}
function EdgeLines {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    return [regex]::Matches($t, "STATE\|edge\|[^\r\n]*") | Select-Object -Last 4 | ForEach-Object { $_.Value }
}

$h = Start-App -ExtraArgs @('-calcDemo')

# 预热点击：程序刚起来时的第一次点击会被 Windows 拿去激活窗口，传不到程序里。
# 先点一下无害的角落把它消耗掉，否则测试的第一步会被吞（第一次跑就是这么被骗的）。
[Win32UI]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 500
$w0 = To-Screen $h 8 8
[Win32UI]::SetCursorPos($w0[0], $w0[1]) | Out-Null
Start-Sleep -Milliseconds 200
[Win32UI]::mouse_event([Win32UI]::LEFTDOWN, 0, 0, 0, [IntPtr]::Zero)
Start-Sleep -Milliseconds 70
[Win32UI]::mouse_event([Win32UI]::LEFTUP, 0, 0, 0, [IntPtr]::Zero)
Start-Sleep -Milliseconds 800
$cr = New-Object Win32UI+RECT
[Win32UI]::GetClientRect($h, [ref]$cr) | Out-Null
$script:CW = $cr.R; $script:CH = $cr.B
Write-Host "客户区 $($cr.R)x$($cr.B)"

# 从日志里取控件坐标（[CalcAudit] CLICK|名字|x|y）
$Coords = @{}
for ($k = 0; $k -lt 3; $k++) {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    foreach ($m in [regex]::Matches($t, "CLICK\|([^|]+)\|(\d+)\|(\d+)\|")) {
        $Coords[$m.Groups[1].Value] = @([int]$m.Groups[2].Value, [int]$m.Groups[3].Value)
    }
    if ($Coords.ContainsKey('node_C')) { break }
    Start-Sleep -Seconds 2
}
Write-Host "控件: $($Coords.Keys -join ', ')"
if (-not $Coords.ContainsKey('node_C')) { throw "没拿到控件坐标" }

# 控件坐标是启动时收一次的，但选单项要等选单打开后才存在。
# 每次开完选单必须重新收一遍，否则菜单项永远是「没有控件」。
function Refresh-Coords {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    foreach ($m in [regex]::Matches($t, "CLICK\|([^|]+)\|(\d+)\|(\d+)\|")) {
        $Coords[$m.Groups[1].Value] = @([int]$m.Groups[2].Value, [int]$m.Groups[3].Value)
    }
}

function CXY([string]$k, [string]$btn = 'Left') {
    if (-not $Coords.ContainsKey($k)) { throw "没有控件 $k" }
    $x = [int]($Coords[$k][0] * $script:CW / 2560)
    $y = [int](($script:CH - $Coords[$k][1]) * $script:CH / 1600)
    # 节点中心落在公式 InputField 上；右键会被输入框截获而不会传给 NodeDrag。
    # 点节点正文区域，才能验证节点本身的右键菜单。
    if ($k.StartsWith('node_')) { $y += [int](24 * $script:CH / 1600) }
    Click-At $h $x $y $btn 800
    Start-Sleep -Milliseconds 4500       # 等状态刷新（游戏 1.2 秒一次，留足余量）
}

Write-Host "初始: nodes=$(St 'nodes') edges=$(St 'edges') pending=$(St 'pending') menu=$(St 'menu')"
Shot $h "$out\00-open.png" | Out-Null

Write-Host ''
Write-Host '=== 1. 求解未知焓变（先做，此时回路干净）==='
CXY 'btn_求解未知焓变'
Start-Sleep -Seconds 2
Shot $h "$out\01-solved.png" | Out-Null
$t1 = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
$mm = [regex]::Match($t1, "\[SolveResult\][^\r\n]*")
$line = if ($mm.Success) { $mm.Value } else { "(没有求解日志)" }
$nm = [regex]::Match($line, "(-?[0-9.]+) kJ/mol")
$got = if ($nm.Success) { [double]$nm.Groups[1].Value } else { [double]::NaN }
Write-Host "  求解日志: $line"
# 回路是 C -> CO₂ -> CO -> C：-394 + x + 111 = 0  =>  x = +283
Check '求解得出正确数值 +283' ([Math]::Abs($got - 283.0) -lt 0.5) "得到 $got"

Write-Host ''
Write-Host '=== 2. 非拉线状态下右键节点 -> 弹出选单 ==='
CXY 'node_C' 'Right'
Refresh-Coords
Check '右键节点弹出选单' ((St 'menu') -eq 'open') "menu = $(St 'menu')"
Shot $h "$out\02-menu.png" | Out-Null

Write-Host ''
Write-Host '=== 3. 左键点别处 -> 选单关闭 ==='
Click-At $h ([int]($script:CW*0.5)) ([int]($script:CH*0.85))
Start-Sleep -Milliseconds 3000
Check '左键点别处后选单关闭' ((St 'menu') -eq 'closed') "menu = $(St 'menu')"
Shot $h "$out\03-menu-closed.png" | Out-Null

Write-Host ''
Write-Host '=== 4. 单击节点 -> 进入拉线状态 ==='
CXY 'node_C'
Check '单击节点后进入拉线状态' ((St 'pending') -eq 'yes') "pending = $(St 'pending')"
Shot $h "$out\04-pending.png" | Out-Null

Write-Host ''
Write-Host '=== 5. 再点另一个节点 -> 连成新连线 ==='
$e0 = [int](St 'edges')
CXY 'node_CO₂'
$e1 = [int](St 'edges')
Check '点另一个节点后新增连线' ($e1 -eq $e0 + 1) "edges $e0 -> $e1"
Check '连完后退出拉线状态' ((St 'pending') -eq 'no') "pending = $(St 'pending')"
Shot $h "$out\05-connected.png" | Out-Null

Write-Host ''
# 连完一条线会弹出「焓变从哪来」的**选单**（查表 / 手动输入 / 设为未知量 / 取消），
# 它一直开着，会把下一步的点击吃掉用来关自己 —— 这就是之前两轮里
# 「先 no 后 yes」的真正原因（不是状态滞后，等待加长也没用）。
# 先点别处把它关掉再继续。
Write-Host "  连完后 menu = $(St 'menu')（预期 open）"
Click-At $h ([int]($script:CW*0.5)) ([int]($script:CH*0.85))
Start-Sleep -Milliseconds 3000
Write-Host "  关掉后 menu = $(St 'menu')（预期 closed）"
Start-Sleep -Seconds 1

Write-Host '=== 6. 拉线中点回自己 -> 取消 ==='
CXY 'node_CO'
$mid = St 'pending'
CXY 'node_CO'
Check '拉线中点回同一节点取消' ($mid -eq 'yes' -and (St 'pending') -eq 'no') "先 $mid 后 $(St 'pending')"

Write-Host ''
Start-Sleep -Seconds 3
Write-Host '=== 7. 拉线中右键 -> 取消拉线 ==='
CXY 'node_C'
$mid2 = St 'pending'
CXY 'node_C' 'Right'
Check '拉线中右键取消拉线' ($mid2 -eq 'yes' -and (St 'pending') -eq 'no') "先 $mid2 后 $(St 'pending')"

Write-Host ''
Write-Host '=== 8. 右键连线 -> 弹出「编辑焓变值 / 删除连线」 ==='
# 连线热区以前是个 100x100 的默认方块、停在原点，线根本点不到，
# 右键线会弹成空白处的「新建节点」。已修，这里就是回归验证。
CXY 'edge_C → CO₂' 'Right'
Refresh-Coords
Check '右键连线弹出选单' ((St 'menu') -eq 'open') "menu = $(St 'menu')"
Shot $h "$out\08-edge-menu.png" | Out-Null

Write-Host ''
Write-Host '=== 9. 点「删除连线」-> 连线数减一 ==='
$e2 = [int](St 'edges')
CXY 'menu_删除连线'
Start-Sleep -Milliseconds 1500
$e3 = [int](St 'edges')
Check '点删除连线后连线数减少' ($e3 -eq $e2 - 1) "edges $e2 -> $e3"
Shot $h "$out\09-edge-deleted.png" | Out-Null



Write-Host "=== 通过 $script:PASS / $($script:PASS + $script:FAIL)，失败 $script:FAIL ==="
$script:BAD | ForEach-Object { Write-Host "   BAD $_" }
Write-Host "截图在 $out"
