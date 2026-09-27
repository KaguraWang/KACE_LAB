const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/dlgtest.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('function Refresh-Coords {')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到'); process.exit(1); }
console.log('Refresh-Coords 在第', at + 1, '行:');
for (let i = at; i < at + 6; i++) console.log('   ', lines[i]);

lines.splice(at + 1, 0,
  '    # 先清空 —— 不然旧的选单项会一直留着，看不出菜单换了没有',
  '    $script:Coords = @{}');
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 已改为先清空');
