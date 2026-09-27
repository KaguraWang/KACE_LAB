const fs = require('fs');
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
let lines = fs.readFileSync(V, 'utf8').split('\n');
let n = 0;

for (let i = 0; i < lines.length; i++) {
  /* 1) Fmt() 已经返回 "15.00 mmol"，这里不能再拼一次 " mol" */
  if (lines[i].includes('_rowAmt[i].text = $"{Fmt(s.Mol)} mol   {s.Conc:F3} M";')) {
    lines[i] = '                _rowAmt[i].text = $"{Fmt(s.Mol)}   {s.Conc:F3} M";   /* Fmt 已带单位，不要再拼 mol */';
    n++; console.log('  ✓ 去掉重复的 mol（第', i + 1, '行）');
  }
  /* 2) 名字列和数量列会撞：名字列 0..0.62、数量列 0.60..1 有重叠 */
  if (lines[i].includes('ar.anchorMax = new Vector2(0.62f, 1);')) {
    lines[i] = '            ar.anchorMax = new Vector2(0.54f, 1);   /* 名字列收窄，别压到数量列 */';
    n++; console.log('  ✓ 名字列 0.62 → 0.54');
  }
  if (lines[i].includes('br.anchorMin = new Vector2(0.60f, 1);')) {
    lines[i] = '            br.anchorMin = new Vector2(0.54f, 1);   /* 和名字列对齐，中间不留缝也不重叠 */';
    n++; console.log('  ✓ 数量列起点 0.60 → 0.54');
  }
}
fs.writeFileSync(V, lines.join('\n'), 'utf8');
const c = fs.readFileSync(V, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
console.log('  还有重复 mol 吗:', c.includes('{Fmt(s.Mol)} mol'));
console.log('改动处数:', n);
