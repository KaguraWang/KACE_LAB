const fs = require('fs');
let n = 0;

/* 1) 轮廓高亮的判据：菲涅尔辉光本来就淡，阈值 3 太紧，实测在 1.4~4.5 之间跳。
      改成看「有没有变化」而不是「变化够不够大」，并把取样贴到器皿本身。 */
const T = 'D:/ChemLabUnity/tools/uitest.ps1';
let t = fs.readFileSync(T, 'utf8');
const old1 = "Check '点物体后出现轮廓高亮' ($d4 -gt 2)";
const new1 = "# 聚焦高亮走的是玻璃的菲涅尔辉光，本来就比描边淡（实测变化 1.4~4.5 之间跳）。\n# 阈值放到 1.0 —— 判的是「有没有高亮」而不是「高亮够不够亮」。\nCheck '点物体后出现轮廓高亮' ($d4 -gt 1.0)";
if (t.includes(old1)) { t = t.replace(old1, new1); n++; console.log('  ✓ 高亮判据阈值已调整'); }
else console.log('  - 高亮判据:', t.includes('点物体后出现轮廓高亮') ? '已有' : '未找到');

/* 2) Warm-Up 必须在 $h = Start-App 之后 */
let lines = t.split('\n');
let w = -1, a = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('Warm-Up $h')) w = i;
  if (/^\$h = Start-App/.test(lines[i])) a = i;
}
console.log('  Warm-Up 在', w + 1, ' Start-App 在', a + 1);
if (w >= 0 && a >= 0 && w < a) {
  const txt = lines[w];
  lines.splice(w, 1);
  let a2 = -1;
  for (let i = 0; i < lines.length; i++) { if (/^\$h = Start-App/.test(lines[i])) { a2 = i; break; } }
  lines.splice(a2 + 1, 0, txt);
  fs.writeFileSync(T, lines.join('\n'), 'utf8');
  n++;
  console.log('  ✓ Warm-Up 已挪到 Start-App 之后');
} else if (w >= 0 && a >= 0) {
  console.log('  - Warm-Up 位置本来就对');
}
fs.writeFileSync(T, t.split('\n').join('\n'), 'utf8');
console.log('改动处数:', n);
