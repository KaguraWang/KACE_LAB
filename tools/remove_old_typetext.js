const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/ui.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

/* 找出所有 Type-Text 定义 */
const starts = [];
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('function Type-Text {')) starts.push(i);
}
console.log('找到', starts.length, '个 Type-Text 定义，行号:', starts.map(i => i + 1).join(', '));

if (starts.length < 2) { console.log('- 只有一个，不用删'); process.exit(0); }

/* 删掉第一个（旧的，用 $VK 的那个） */
const s = starts[0];
let e = -1;
for (let i = s; i < s + 30; i++) {
  if (lines[i] !== undefined && lines[i] === '}') { e = i; break; }
}
console.log('旧的从', s + 1, '到', e + 1, '行');
for (let i = s; i <= e; i++) console.log('  删:', lines[i]);

lines.splice(s, e - s + 1);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 旧定义已删');

const still = fs.readFileSync(P, 'utf8');
console.log('  还剩 Type-Text 定义:', (still.match(/function Type-Text \{/g) || []).length, '个');
console.log('  还有 $VK[ 吗:', still.includes('$VK['));
