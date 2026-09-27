const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

const src = fs.readFileSync(U, 'utf8');
const oldCat = 'rt.anchoredPosition = new Vector2(12 + col * 60, -140 - row * 36); rt.sizeDelta = new Vector2(56, 30);';
const newCat = 'rt.anchoredPosition = new Vector2(12 + col * 60, -182 - row * 38); rt.sizeDelta = new Vector2(56, 30);';

if (!src.includes(oldCat)) { console.log('✗ 分类行锚点未命中'); process.exit(1); }

let out = src.replace(oldCat, newCat);
out = out.replace('new Vector2(12, 12), new Vector2(-12, -272), new Color(0,0,0,0.001f));',
                  'new Vector2(12, 12), new Vector2(-12, -296), new Color(0,0,0,0.001f));');
fs.writeFileSync(U, out, 'utf8');
console.log('✓ 分类行 -140 → -182（行距 36 → 38）');
console.log('✓ 滚动列表顶 -272 → -296');

/* 核对 */
const fl = out.split('\n');
for (let i = 0; i < fl.length; i++) {
  if (fl[i].includes('col * 60, -')) console.log('  分类:', fl[i].trim());
  if (fl[i].includes('rowX[i], -134')) console.log('  精确行:', fl[i].trim());
  if (fl[i].includes('new Vector2(12, 12), new Vector2(-12, -')) console.log('  滚动列表:', fl[i].trim());
  if (fl[i].includes('12 + i * 96, -96')) console.log('  预设:', fl[i].trim());
}
console.log('  括号差:', (out.match(/\{/g) || []).length - (out.match(/\}/g) || []).length);
