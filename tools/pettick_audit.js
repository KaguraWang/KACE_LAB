const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

if (fs.readFileSync(B, 'utf8').includes('PetTick 也打')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('Debug.Log("[PetState]') && lines[i].includes('PetAudit()')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 PetState 那行'); process.exit(1); }
console.log('第', at + 1, '行:', lines[at].trim());

lines.splice(at + 1, 0,
  '            /* 控件清单也要周期重打：试剂列表项会随搜索/分类变，',
  '               只在启动时打一次的话，搜出来的试剂拿不到坐标 */',
  '            if (_ui != null) Debug.Log("AUDITSTART\\n" + _ui.TestClickTargets() + "AUDITEND\\n");');
fs.writeFileSync(B, lines.join('\n'), 'utf8');
console.log('✓ PetTick 也打控件清单');
console.log('  括号差:', (fs.readFileSync(B,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(B,'utf8').match(/\}/g)||[]).length);
