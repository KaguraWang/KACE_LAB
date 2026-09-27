# =====================================================================
#  手动输入焓变对话框的窗口测试
#    右键连线 -> 「手动输入…」-> 对话框 -> 输数值 -> 确定 -> 值生效
#
#  注意：首次打开计算器会自动跑 15 步教程，其中一步会 ClearAll。
#  画布空了之后再右键连线只会点到空白（弹出「在此新建节点」）——
#  第一次跑就是这么被骗的，所以必须先跳过教程。
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

$script:Coords = @{}
function Refresh-Coords {
    # 先清空 —— 不然旧的选单项会一直留着，看不出菜单换了没有
    $script:Coords = @{}
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    foreach ($m in [regex]::Matches($t, "CLICK\|([^|]+)\|(\d+)\|(\d+)\|")) {
        $script:Coords[$m.Groups[1].Value] = @([int]$m.Groups[2].Value, [int]$m.Groups[3].Value)
    }
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

function CXY([string]$k, [string]$btn = 'Left', [int]$wait = 2500) {
    if (-not $script:Coords.ContainsKey($k)) {
        throw "没有控件 $k（现有: $(($script:Coords.Keys) -join ', ')）"
    }
    $x = [int]($script:Coords[$k][0] * $script:CW / 2560)
    $y = [int](($script:CH - $script:Coords[$k][1]) * $script:CH / 1600)
    Click-At $h $x $y $btn 800
    Start-Sleep -Milliseconds $wait
}

Write-Host ''
Write-Host '=== 0. 先跳过计算器教程（它会清空画布）==='
Refresh-Coords
if ($script:Coords.ContainsKey('tut_跳过教程 ⏭')) {
    CXY 'tut_跳过教程 ⏭' 'Left' 3500
    Write-Host "  跳过教程后: tutRunning=$(St 'tutRunning')  calcDone=$(St 'calcDone')"
} else {
    Write-Host '  （教程栏不在，可能已经跳过）'
}
Start-Sleep -Seconds 2
Shot $h "$out\00-after-skip.png" | Out-Null

Write-Host ''
Write-Host '=== 1. 右键连线 C → CO₂，弹出选单 ==='
Refresh-Coords
CXY 'edge_C → CO₂' 'Right'
Refresh-Coords
$menuItems = ($script:Coords.Keys | Where-Object { $_ -like 'menu_*' }) -join ' | '
Write-Host "  选单项: $menuItems"
Check '右键连线弹出连线选单' ($menuItems -like '*编辑焓变值*') "items = $menuItems"
Shot $h "$out\01-edgemenu.png" | Out-Null


Write-Host ''
Write-Host '=== 1b. 点「编辑焓变值」，弹出焓变来源选单 ==='
# 右键连线 -> 「编辑焓变值」-> 才是「查表 / 手动输入… / 设为未知量 / 取消」那个选单
if ($script:Coords.ContainsKey('menu_编辑焓变值')) {
    CXY 'menu_编辑焓变值' 'Left' 2500
    Refresh-Coords
    $srcItems = ($script:Coords.Keys | Where-Object { $_ -like 'menu_*' }) -join ' | '
    Write-Host "  焓变来源选单项: $srcItems"
    Check '出现焓变来源选单（含「手动输入…」）' ($srcItems -like '*手动输入*') "items = $srcItems"
    Shot $h "$out\01b-sourcemenu.png" | Out-Null
} else {
    Check '连线选单里有「编辑焓变值」' $false "没有"
}
Write-Host ''
Write-Host '=== 2. 点「手动输入…」，应弹出输入对话框 ==='
if ($script:Coords.ContainsKey('menu_手动输入…')) {
    CXY 'menu_手动输入…' 'Left' 2500
    Refresh-Coords
    $dlgItems = ($script:Coords.Keys | Where-Object { $_ -like 'menu_*' }) -join ' | '
    Write-Host "  对话框按钮: $dlgItems"
    Check '对话框出现（有「确定」按钮）' ($dlgItems -like '*确定*') "items = $dlgItems"
    Shot $h "$out\02-dialog.png" | Out-Null

    Write-Host ''
    Write-Host '=== 3. 输入数值并点确定 ==='
    $before = EdgeDH 'C->CO₂'
    Write-Host "  改之前 C->CO₂ 的 ΔH = $before"
    Type-Text '7' 400
    Type-Text '7' 400
    Type-Text '7' 700
    if ($script:Coords.ContainsKey('menu_确定')) {
        CXY 'menu_确定' 'Left' 2500
        Start-Sleep -Seconds 1
        $after = EdgeDH 'C->CO₂'
        Write-Host "  改之后 C->CO₂ 的 ΔH = $after"
        Shot $h "$out\03-committed.png" | Out-Null
        Check '点确定后对话框关闭' ((St 'menu') -eq 'closed') "menu = $(St 'menu')"
        Check '连线的 ΔH 变成了手动输入的值' ($after -ne $before) "ΔH $before -> $after"
    } else {
        Check '对话框里有「确定」按钮' $false "没找到（现有: $($script:Coords.Keys -join ', ')）"
    }
} else {
    Check '选单里有「手动输入…」' $false "没有"
}

Write-Host ''
Write-Host "=== 通过 $script:PASS / $($script:PASS + $script:FAIL)，失败 $script:FAIL ==="
$script:BAD | ForEach-Object { Write-Host "   BAD $_" }
Write-Host "截图在 $out"
Stop-App