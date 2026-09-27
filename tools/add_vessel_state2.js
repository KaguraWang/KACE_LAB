const fs = require('fs');
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(L, 'utf8').split('\n');

if (fs.readFileSync(L, 'utf8').includes('STATE|vessel|')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('sb.AppendLine("PET|glow|"')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 PET|glow 那行'); process.exit(1); }
console.log('PET|glow 在第', at + 1, '行:', lines[at].trim());

const blk = [
  '        /* 器皿在屏幕上的位置 —— 测试要点器皿时直接用，不要猜比例坐标',
  '           （器皿每次生成的位置都不同，猜出来的坐标时灵时不灵）。 */',
  '        foreach (var vv2 in _views) {',
  '            if (vv2 == null || vv2.Model == null) continue;',
  '            if (Cam == null) continue;',
  '            var wsp = vv2.transform.position + Vector3.up * 0.25f;',
  '            var ssp = Cam.WorldToScreenPoint(wsp);',
  '            if (ssp.z <= 0.01f) continue;',
  '            sb.AppendLine("STATE|vessel|" + vv2.Model.Id + "|"',
  '                        + Mathf.RoundToInt(ssp.x) + "," + Mathf.RoundToInt(ssp.y));',
  '        }',
];
lines.splice(at + 1, 0, ...blk);
fs.writeFileSync(L, lines.join('\n'), 'utf8');

/* 当场核对：打印插入后的实际内容 */
const after = fs.readFileSync(L, 'utf8').split('\n');
console.log('--- 插入后（第', at + 1, '行起）---');
for (let i = at; i < at + 14; i++) console.log('  ', (i + 1) + ':', after[i]);
console.log('  含 STATE|vessel| :', fs.readFileSync(L, 'utf8').includes('STATE|vessel|'));
console.log('  括号差:', (fs.readFileSync(L,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(L,'utf8').match(/\}/g)||[]).length);
