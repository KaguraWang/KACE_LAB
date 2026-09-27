const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/calctest.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

if (fs.readFileSync(P, 'utf8').includes('右键连线')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes("Write-Host '=== 7. 拉线中右键 -> 取消拉线 ==='")) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到第 7 步'); process.exit(1); }
/* 找到第 7 步的 Check 行 */
let end = at;
for (let i = at; i < at + 10; i++) {
  if (lines[i] && lines[i].includes("Check '拉线中右键取消拉线'")) { end = i; break; }
}
console.log('第 7 步', at + 1, '..', end + 1);

const blk = [
  '',
  "Write-Host ''",
  "Write-Host '=== 8. 右键连线 -> 弹出「编辑焓变值 / 删除连线」 ==='",
  '# 连线热区以前是个 100x100 的默认方块、停在原点，线根本点不到，',
  '# 右键线会弹成空白处的「新建节点」。已修，这里就是回归验证。',
  "CXY 'edge_C → CO₂' 'Right'",
  "Check '右键连线弹出选单' ((St 'menu') -eq 'open') \"menu = $(St 'menu')\"",
  "Shot $h \"$out\\08-edge-menu.png\" | Out-Null",
  '',
  "Write-Host ''",
  "Write-Host '=== 9. 点「删除连线」-> 连线数减一 ==='",
  '$e2 = [int](St \'edges\')',
  "CXY 'menu_删除连线'",
  "Start-Sleep -Milliseconds 1500",
  '$e3 = [int](St \'edges\')',
  "Check '点删除连线后连线数减少' ($e3 -eq $e2 - 1) \"edges $e2 -> $e3\"",
  "Shot $h \"$out\\09-edge-deleted.png\" | Out-Null",
  '',
];
lines.splice(end + 1, 0, ...blk);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 第 8、9 步已加');
