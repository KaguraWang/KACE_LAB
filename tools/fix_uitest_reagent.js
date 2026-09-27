const fs = require('fs');
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let lines = fs.readFileSync(T, 'utf8').split('\n');

/* 把第 1、2 步改成读 REAGENT 状态，不再靠像素取样 */
let from = -1, to = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes("=== 1. 试剂架分类过滤 ===")) { from = i - 1; break; }
}
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes("=== 3. 器材架点「烧杯」 ===")) { to = i - 1; break; }
}
if (from < 0 || to < 0) { console.log('✗ 定位失败', from, to); process.exit(1); }
console.log('替换', from + 1, '..', to + 1, '行');

const nw = [
  "Write-Host ''",
  "Write-Host '=== 1. 试剂架分类过滤 ==='",
  "# 改用游戏自报的状态判定。像素取样在这个用例上一直不可靠 ——",
  "# 列表行与行长得像、取样区域难对准，调了四轮反而越调越差。",
  "function Rg([string]$key) {",
  "    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue",
  "    $m = [regex]::Matches($t, \"REAGENT\\|$key\\|([^\\r\\n|]*)\")",
  "    if ($m.Count -eq 0) { return '' }",
  "    return $m[$m.Count - 1].Groups[1].Value",
  "}",
  "$cat0 = Rg 'cat'; $vis0 = Rg 'visible'",
  "Write-Host \"  初始: cat=$cat0 visible=$vis0\"",
  "Click-At $h (PX 0.868) (PY 0.163)      # 「氧化物」按钮（实测坐标）",
  "Start-Sleep -Milliseconds 2500",
  "$cat1 = Rg 'cat'; $vis1 = Rg 'visible'",
  "Check '点分类「氧化物」后分类切换' ($cat1 -eq 'oxide') \"cat $cat0 -> $cat1\"",
  "Check '切换分类后列表条目数变化' ($vis1 -ne $vis0) \"visible $vis0 -> $vis1\"",
  "Shot $h \"$out\\11-cat-oxide.png\" | Out-Null",
  '',
  "Write-Host ''",
  "Write-Host '=== 2. 搜索框 ==='",
  "# 搜索框实测客户区中心 (2266, 131)",
  "Click-At $h 2266 131 'Left' 1200",
  "Type-Text 'CuSO4' 1200",
  "Start-Sleep -Milliseconds 2000",
  "$q = Rg 'query'; $vis2 = Rg 'visible'",
  "Check '输入 CuSO4 后搜索词进入' ($q -eq 'CuSO4') \"query = $q\"",
  "Check '搜索后列表被过滤' ([int]$vis2 -lt [int]$vis1) \"visible $vis1 -> $vis2\"",
  "Shot $h \"$out\\12-search.png\" | Out-Null",
  "# 清空搜索",
  "for ($i = 0; $i -lt 6; $i++) { Key-Press 'Back' 80 }",
  "Start-Sleep -Milliseconds 1500",
  '',
];
lines.splice(from, to - from + 1, ...nw);
fs.writeFileSync(T, lines.join('\n'), 'utf8');
console.log('✓ 前两步已改为状态判定');
