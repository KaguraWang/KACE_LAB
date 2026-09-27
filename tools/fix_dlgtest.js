const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/dlgtest.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

/* 第 1 步的判据改成「有编辑焓变值」，并在中间加一步点它 */
let hit = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes("Check '右键连线弹出的是连线选单")) { hit = i; break; }
}
if (hit < 0) { console.log('✗ 找不到判据'); process.exit(1); }
console.log('第', hit + 1, '行:', lines[hit].trim());
lines[hit] = "Check '右键连线弹出连线选单' ($menuItems -like '*编辑焓变值*') \"items = $menuItems\"";

/* 在第 2 步之前插入「点编辑焓变值」 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes("=== 2. 点「手动输入…」")) { at = i - 1; break; }
}
if (at < 0) { console.log('✗ 找不到第 2 步'); process.exit(1); }
console.log('第 2 步在第', at + 1, '行');

const blk = [
  '',
  "Write-Host ''",
  "Write-Host '=== 1b. 点「编辑焓变值」，弹出焓变来源选单 ==='",
  "# 右键连线 -> 「编辑焓变值」-> 才是「查表 / 手动输入… / 设为未知量 / 取消」那个选单",
  "if ($script:Coords.ContainsKey('menu_编辑焓变值')) {",
  "    CXY 'menu_编辑焓变值' 'Left' 2500",
  "    Refresh-Coords",
  "    $srcItems = ($script:Coords.Keys | Where-Object { $_ -like 'menu_*' }) -join ' | '",
  "    Write-Host \"  焓变来源选单项: $srcItems\"",
  "    Check '出现焓变来源选单（含「手动输入…」）' ($srcItems -like '*手动输入*') \"items = $srcItems\"",
  "    Shot $h \"$out\\01b-sourcemenu.png\" | Out-Null",
  "} else {",
  "    Check '连线选单里有「编辑焓变值」' $false \"没有\"",
  "}",
];
lines.splice(at, 0, ...blk);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 已插入「点编辑焓变值」这一步');
