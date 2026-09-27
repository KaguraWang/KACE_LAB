const fs = require('fs');
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(L, 'utf8').split('\n');

if (fs.readFileSync(L, 'utf8').includes('PARTS|')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('sb.AppendLine("FX|"')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 FX 那行'); process.exit(1); }
/* 找到 FX 那个 foreach 的结尾 */
let end = at;
for (let i = at; i < at + 14; i++) {
  if (lines[i] !== undefined && lines[i].trim() === '}') { end = i; break; }
}
console.log('FX 块:', at + 1, '..', end + 1);

lines.splice(end + 1, 0,
  '',
  '        /* 器皿下面挂了哪些渲染物：排查「试管里那根竖条是什么」 */',
  '        {',
  '            var anyV = _views.Count > 0 ? _views[_views.Count - 1] : null;',
  '            if (anyV != null) {',
  '                var names = new List<string>();',
  '                foreach (var mr in anyV.GetComponentsInChildren<MeshRenderer>(true)) {',
  '                    if (!mr.enabled || !mr.gameObject.activeInHierarchy) continue;',
  '                    var bnd = mr.bounds;',
  '                    names.Add(mr.gameObject.name + "(h" + bnd.size.y.ToString("F2") + ")");',
  '                }',
  '                sb.AppendLine("PARTS|" + anyV.Model.Id + "|" + string.Join(" ", names));',
  '            }',
  '        }');
fs.writeFileSync(L, lines.join('\n'), 'utf8');
console.log('✓ PARTS 诊断已加');
console.log('  括号差:', (fs.readFileSync(L,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(L,'utf8').match(/\}/g)||[]).length);
