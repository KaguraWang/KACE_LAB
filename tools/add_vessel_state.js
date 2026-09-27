const fs = require('fs');
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let l = fs.readFileSync(L, 'utf8');

if (l.includes('STATE|vessel|')) { console.log('- 已存在'); process.exit(0); }

const anchor = '        sb.AppendLine("PET|glow|" + glowCount + "|" + maxGlow.ToString("F2"));';
if (!l.includes(anchor)) { console.log('✗ 锚点未命中'); process.exit(1); }

/* 器皿在屏幕上的位置 —— 测试要点器皿时直接用这个，不要猜比例坐标
   （器皿每次生成的位置都不同，猜出来的坐标时灵时不灵）。 */
const add = [anchor,
  '        /* 器皿的屏幕位置：测试要点器皿时直接用，别猜 */',
  '        foreach (var vv2 in _views) {',
  '            if (vv2 == null || vv2.Model == null || Cam == null) continue;',
  '            var wp = vv2.transform.position + Vector3.up * 0.25f;',
  '            var sp = Cam.WorldToScreenPoint(wp);',
  '            if (sp.z <= 0.01f) continue;',
  '            sb.AppendLine("STATE|vessel|" + vv2.Model.Id + "|" + Mathf.RoundToInt(sp.x) + "," + Mathf.RoundToInt(sp.y));',
  '        }',
].join('\n');

l = l.replace(anchor, add);
fs.writeFileSync(L, l, 'utf8');
console.log('✓ STATE|vessel 已加');
console.log('  括号差:', (l.match(/\{/g) || []).length - (l.match(/\}/g) || []).length);
