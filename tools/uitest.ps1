# =====================================================================
#  真实窗口操作测试
#    注入真实鼠标事件逐条验证功能，每步截图取证。
#
#  坐标不写死：先用 -dumpLayout 让**游戏自己报出**每个控件的
#  「游戏内屏幕坐标」，再换算到客户区像素。
#  为什么这么做：窗口分辨率每次都不一样（1066x600 / 2560x1600 都出现过），
#  而且宽高比不同会让 CanvasScaler 产生不同的比例布局，
#  写死比例坐标必然点偏。让游戏报坐标就一劳永逸。
# =====================================================================
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
. D:\ChemLabUnity\tools\ui.ps1

$out = 'D:\ChemLabUnity\Logs\uitest'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Get-ChildItem $out -File -EA SilentlyContinue | Remove-Item -Force -EA SilentlyContinue

$reg = 'HKCU:\Software\DefaultCompany\ChemLabUnity'
New-Item -Path $reg -Force | Out-Null
Set-ItemProperty -Path $reg -Name 'anon.prog.lab' -Value 1
Set-ItemProperty -Path $reg -Name 'anon.prog.calc' -Value 1

$script:R = New-Object System.Collections.Generic.List[object]
function Check($n, $ok, $d) {
    $script:R.Add([pscustomobject]@{ name=$n; ok=$ok; detail=$d })
    Write-Host "  $(if ($ok) {'[OK ]'} else {'[BAD]'}) $n  --  $d"
}

# ---------- 启动并收集控件坐标 ----------
$h = Start-App -ExtraArgs @('-dumpLayout','-petAudit')
Warm-Up $h    # 第一次点击会被 Windows 拿去激活窗口，先消耗掉（必须在拿到句柄之后调）
$cr = New-Object Win32UI+RECT
[Win32UI]::GetClientRect($h, [ref]$cr) | Out-Null
$script:CW = $cr.R; $script:CH = $cr.B
Write-Host "客户区 $($script:CW)x$($script:CH)"

$log = "$env:USERPROFILE\AppData\LocalLow\DefaultCompany\ChemLabUnity\Player.log"
$txt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
$i = $txt.IndexOf('DUMPSTART'); $j = $txt.IndexOf('DUMPEND')
if ($i -lt 0) { throw "没拿到 -dumpLayout 输出" }
$lines = ($txt.Substring($i+10, $j-$i-10) -split "`n") | Where-Object { $_.Trim() }
$gw = 0; $gh = 0
$T = @{}
foreach ($ln in $lines) {
    $p = $ln.Trim().Split('|')
    if ($p[0] -eq 'SCREEN') { $gw = [int]$p[1]; $gh = [int]$p[2] }
    elseif ($p[0] -eq 'CLICK') { $T[$p[1]] = @([int]$p[2], [int]$p[3]) }
}
Write-Host "游戏内屏幕 $gw x $gh，收到 $($T.Count) 个控件坐标"
if ($gw -le 0) { throw "SCREEN 行没解析到" }

# 游戏屏幕坐标 → 客户区像素（注意 Y 轴要翻转）
function TX([string]$name) { [int]($T[$name][0] * $script:CW / $gw) }
function TY([string]$name) { [int](($gh - $T[$name][1]) * $script:CH / $gh) }
function GX([int]$x) { [int]($x * $script:CW / $gw) }
function GY([int]$y) { [int](($gh - $y) * $script:CH / $gh) }
function ClickT($name, $btn = 'Left', $settle = 600) {
    if (-not $T.ContainsKey($name)) { throw "没有控件 $name" }
    Click-At $h (TX $name) (TY $name) $btn $settle
}

$base = Shot $h "$out\00-launch.png"

Write-Host ''
Write-Host '=== 1. 试剂架分类过滤 ==='
# 改用游戏自报的状态判定。像素取样在这个用例上一直不可靠 ——
# 列表行与行长得像、取样区域难对准，调了四轮反而越调越差。
function Rg([string]$key) {
    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    $m = [regex]::Matches($t, "REAGENT\|$key\|([^\r\n|]*)")
    if ($m.Count -eq 0) { return '' }
    return $m[$m.Count - 1].Groups[1].Value
}
$cat0 = Rg 'cat'; $vis0 = Rg 'visible'
Write-Host "  初始: cat=$cat0 visible=$vis0"
# 用游戏自报的坐标，别写死 —— 试剂架布局一改，写死的坐标就点不准了。
function Ctrl([string]$n) {
    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    $mm = [regex]::Matches($tt, "CLICK\|" + [regex]::Escape($n) + "\|(\d+)\|(\d+)")
    if ($mm.Count -eq 0) { return $null }
    return @([int]$mm[$mm.Count-1].Groups[1].Value, [int]$mm[$mm.Count-1].Groups[2].Value)
}
Write-Host "  「氧化物」按钮屏幕坐标: $($T['cat_氧化物'] -join ',')"
ClickT 'cat_氧化物' 'Left' 1500
Start-Sleep -Milliseconds 2500
$cat1 = Rg 'cat'; $vis1 = Rg 'visible'
Check '点分类「氧化物」后分类切换' ($cat1 -eq 'oxide') "cat $cat0 -> $cat1"
Check '切换分类后列表条目数变化' ($vis1 -ne $vis0) "visible $vis0 -> $vis1"
Shot $h "$out\11-cat-oxide.png" | Out-Null

Write-Host ''
Write-Host '=== 2. 搜索框 ==='
# 搜索框实测客户区中心 (2266, 131)
ClickT 'search' 'Left' 1200
Type-Text 'CuSO4' 1200
Start-Sleep -Milliseconds 2000
$q = Rg 'query'; $vis2 = Rg 'visible'
Check '输入 CuSO4 后搜索词进入' ($q -eq 'CuSO4') "query = $q"
Check '搜索后列表被过滤' ([int]$vis2 -lt [int]$vis1) "visible $vis1 -> $vis2"
Shot $h "$out\12-search.png" | Out-Null
# 清空搜索
for ($i = 0; $i -lt 6; $i++) { Key-Press 'Back' 80 }
Start-Sleep -Milliseconds 1500

Write-Host '=== 3. 器材架点「烧杯」 ==='
# 数器皿个数，不采样像素 —— 新器皿的位置改过之后，
# 原来固定的采样区域就套不中了（像素判据太脆）。
function VesselCount {
    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    $mm = [regex]::Matches($tt, "STATE\|vessel\|(v\d+)\|")
    $ids = @{}
    foreach ($x in $mm) { $ids[$x.Groups[1].Value] = 1 }
    return $ids.Count
}
$n0 = VesselCount
Write-Host "  点之前器皿数 = $n0"
ClickT 'vessel_烧杯' 'Left' 2000
Start-Sleep -Seconds 3
$n1 = VesselCount
Write-Host "  点之后器皿数 = $n1"
Check '点「烧杯」后器皿数增加' ($n1 -gt $n0) "器皿 $n0 -> $n1"
Shot $h "$out\21-after-beaker.png" | Out-Null

Write-Host ''
Write-Host '=== 4. 聚焦 / 轮廓高亮 ==='
# 用状态判定：读 PET|glow（聚焦高亮的强度）和 STATE|vessel（器皿的屏幕坐标）。
# 不猜比例坐标 —— 器皿每次生成的位置都不同，猜出来的坐标时灵时不灵。
# 也不用像素判据 —— 菲涅尔辉光本来就淡，实测在 1.4~4.5 之间跳。
function GlowCount {
    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    $mm = [regex]::Matches($tt, 'PET\|glow\|(\d+)\|')
    if ($mm.Count -eq 0) { return -1 }
    return [int]$mm[$mm.Count - 1].Groups[1].Value
}
# 所有器皿的屏幕坐标。注意坐标列表里可能混着已经在左面板下面、点不中的，
# 所以下面要逐个试。
function VesselPositions {
    $tt = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue
    $mm = [regex]::Matches($tt, "STATE\|vessel\|[^|]+\|(\d+),(\d+)")
    $o = @()
    for ($i = 0; $i -lt $mm.Count; $i++) {
        $o += ,@([int]$mm[$i].Groups[1].Value, [int]$mm[$i].Groups[2].Value)
    }
    return $o
}
Write-Host "  点之前 glow 数 = $(GlowCount)"
$vps = VesselPositions
Write-Host "  拿到 $($vps.Count) 个器皿坐标"
$g1 = 0
# 逐个试：坐标列表里可能混着已经在左面板下面、点不中的
foreach ($vp in ($vps | Select-Object -First 4)) {
    Write-Host "  点 ($($vp[0]), $($gh - $vp[1]))"
    Click-At $h (GX $vp[0]) (GY $vp[1]) "Left" 1200
    Start-Sleep -Milliseconds 1800
    $g1 = GlowCount
    Write-Host "      glow = $g1"
    if ($g1 -ge 1) { break }
}
Check '点器皿后出现高亮' ($g1 -ge 1) "glow 数 = $g1"
Shot $h "$out\31-focused.png" | Out-Null
Click-At $h 1600 1440 "Left" 900
Start-Sleep -Milliseconds 2500
$g2 = GlowCount
Write-Host "  点空白后 glow 数 = $g2"
Check '点空白后高亮消失' ($g2 -eq 0) "glow 数 = $g2"
Shot $h "$out\33-after-unfocus.png" | Out-Null
Write-Host "  聚焦高亮：见 31/32/33 三张截图（-focusDemo 下已多次肉眼确认）"
Shot $h "$out\32-unfocused.png" | Out-Null

Write-Host '=== 5. 打开焓变计算器：实验台 UI 应隐藏 ==='
$pnR = @([int]($CW*0.02), [int]($CH*0.25), [int]($CW*0.12), [int]($CH*0.30))
$lab = RegionColor $base $pnR[0] $pnR[1] $pnR[2] $pnR[3]
ClickT 'opencalc' 'Left' 1500
Start-Sleep -Seconds 1
$f5 = Shot $h "$out\51-calculator.png"
$calc = RegionColor $f5 $pnR[0] $pnR[1] $pnR[2] $pnR[3]
$d6 = ColorDist $lab $calc
Check '计算器打开后实验台左栏消失' ($d6 -gt 20) "左栏色差 $([math]::Round($d6,1))  实验台=$($lab -join ',')  计算器=$($calc -join ',')"

Write-Host ''
Write-Host '=== 6. 计算器：拖动节点，连线应实时跟随 ==='
$nodeR = @([int]($CW*0.22), [int]($CH*0.28), [int]($CW*0.22), [int]($CH*0.22))
$n0 = RegionColor $f5 $nodeR[0] $nodeR[1] $nodeR[2] $nodeR[3]
Drag-FromTo $h ([int]($CW*0.30)) ([int]($CH*0.40)) ([int]($CW*0.20)) ([int]($CH*0.62))
Start-Sleep -Milliseconds 900
$f5c = Shot $h "$out\53-after-drag.png"
$n1 = RegionColor $f5c $nodeR[0] $nodeR[1] $nodeR[2] $nodeR[3]
$d7 = ColorDist $n0 $n1
Check '拖动节点后画面变化（节点动+连线重画）' ($d7 -gt 5) "节点区色差 $([math]::Round($d7,1))"

Write-Host ''
Write-Host '=== 7. 返回实验台 ==='
Click-At $h ([int]($CW*0.94)) ([int]($CH*0.018))
Start-Sleep -Seconds 1.5
$f6 = Shot $h "$out\60-back.png"
$back = RegionColor $f6 $pnR[0] $pnR[1] $pnR[2] $pnR[3]
$d8 = ColorDist $lab $back
Check '点「返回实验台」后左栏恢复' ($d8 -lt 14) "与实验台原色差 $([math]::Round($d8,1))"

Write-Host ''
$pass = ($script:R | Where-Object { $_.ok }).Count
Write-Host "=== 通过 $pass / $($script:R.Count)，失败 $($script:R.Count - $pass) ==="
$script:R | Where-Object { -not $_.ok } | ForEach-Object { Write-Host "   BAD $($_.name)  $($_.detail)" }
$script:R | ConvertTo-Json -Depth 3 | Set-Content "$out\results.json" -Encoding UTF8
Stop-App $h
