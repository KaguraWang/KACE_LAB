const fs = require('fs');
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let lines = fs.readFileSync(T, 'utf8').split('\n');

let hit = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('(PX 0.868)')) { hit = i; break; }
}
if (hit < 0) { console.log('✗ 找不到'); process.exit(1); }
console.log('第', hit + 1, '行:', lines[hit]);
lines[hit] = 'Click-At $h 2223 260 "Left" 1500      # 「氧化物」按钮（实测客户区坐标）';
fs.writeFileSync(T, lines.join('\n'), 'utf8');
console.log('✓ 已改成绝对坐标');

/* 再确认 PX/PY 的定义在文件里存在（后面几步还在用） */
const c = fs.readFileSync(T, 'utf8');
console.log('  PX 定义在吗:', c.includes('function PX('));
console.log('  PY 定义在吗:', c.includes('function PY('));
