const fs = require('fs');
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
let c = fs.readFileSync(V, 'utf8');
let n = 0;

/* 槽位间距改成「按面板实际高度分层」——
   固定 0.30 世界单位不够：面板本身就有 h/340 高（约 0.235），
   两个标签一叠就压在一起了。 */
const oldA = 'Vector3 anchor = basePt + Vector3.up * (Slot * 0.30f + worldH * 0.5f + 0.06f);';
const newA = '/* 按实际高度分层：面板高 worldH，槽位间距取 worldH + 0.07，两个标签才不会压在一起 */\n        Vector3 anchor = basePt + Vector3.up * (Slot * (worldH + 0.07f) + worldH * 0.5f + 0.06f);';
if (c.includes(oldA)) { c = c.replace(oldA, newA); n++; console.log('  ✓ 槽位间距改为按面板高度'); }
else console.log('  ✗ 锚点 A 未命中');

/* TopWorldY 也要跟着改，否则方程式标签会让位不足 */
const oldB = 'return baseY + worldH + 0.055f + Slot * 0.30f;';
const newB = '/* 要加上「被后面几个槽位顶上去」的高度，否则方程式标签会压在容器标签上 */\n            return baseY + worldH + 0.055f + Slot * (worldH + 0.07f);';
if (c.includes(oldB)) { c = c.replace(oldB, newB); n++; console.log('  ✓ TopWorldY 同步'); }
else console.log('  ✗ 锚点 B 未命中');

fs.writeFileSync(V, c, 'utf8');
const chk = fs.readFileSync(V, 'utf8');
console.log('  大括号差:', (chk.match(/\{/g) || []).length - (chk.match(/\}/g) || []).length);
console.log('改动处数:', n);
