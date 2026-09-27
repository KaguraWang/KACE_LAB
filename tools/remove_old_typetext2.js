const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/ui.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

/* 找出用 $VK[ 的那个（旧版），整段删掉 */
let bad = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('$vk = $VK[$s.ToLower()]')) { bad = i; break; }
}
if (bad < 0) { console.log('- 旧版已经不在了'); process.exit(0); }

/* 往上找 function 头 */
let s = bad;
while (s > 0 && !lines[s].includes('function Type-Text {')) s--;
/* 往下找收尾 } */
let e = bad;
while (e < lines.length && lines[e] !== '}') e++;
console.log('旧版 Type-Text 在', s + 1, '..', e + 1, '行');
for (let i = s; i <= e; i++) console.log('  删:', lines[i]);

lines.splice(s, e - s + 1);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 旧版已删');
const c = fs.readFileSync(P, 'utf8');
console.log('  还剩 Type-Text:', (c.match(/function Type-Text \{/g) || []).length, '个');
console.log('  还有 $VK[ 吗:', c.includes('$VK['));
console.log('  VKCHAR 在吗:', c.includes('VKCHAR'));
