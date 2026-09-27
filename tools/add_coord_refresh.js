const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/calctest.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

if (fs.readFileSync(P, 'utf8').includes('function Refresh-Coords')) { console.log('- 已存在'); process.exit(0); }

/* 在 CXY 之前加 Refresh-Coords */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('function CXY(')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 CXY'); process.exit(1); }
console.log('CXY 在第', at + 1, '行');

const blk = [
  '# 控件坐标是启动时收一次的，但选单项要等选单打开后才存在。',
  '# 每次开完选单必须重新收一遍，否则菜单项永远是「没有控件」。',
  'function Refresh-Coords {',
  '    $t = Get-Content $log -Encoding UTF8 -Raw -EA SilentlyContinue',
  '    foreach ($m in [regex]::Matches($t, "CLICK\\|([^|]+)\\|(\\d+)\\|(\\d+)\\|")) {',
  '        $Coords[$m.Groups[1].Value] = @([int]$m.Groups[2].Value, [int]$m.Groups[3].Value)',
  '    }',
  '}',
  '',
];
lines.splice(at, 0, ...blk);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ Refresh-Coords 已加');

/* 每次右键后刷新坐标 */
let c = fs.readFileSync(P, 'utf8');
const pairs = [
  ["CXY 'node_C' 'Right'\nCheck '右键节点弹出选单'", "CXY 'node_C' 'Right'\nRefresh-Coords\nCheck '右键节点弹出选单'"],
  ["CXY 'edge_C → CO₂' 'Right'\nCheck '右键连线弹出选单'", "CXY 'edge_C → CO₂' 'Right'\nRefresh-Coords\nCheck '右键连线弹出选单'"],
];
for (const [a, b] of pairs) {
  if (c.includes(a)) { c = c.replace(a, b); console.log('  ✓ 刷新点已插'); }
  else console.log('  ✗ 锚点:', a.split('\n')[0]);
}
fs.writeFileSync(P, c, 'utf8');
