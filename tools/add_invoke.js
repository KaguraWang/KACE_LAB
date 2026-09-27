const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');
let done = false;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('var cs = EnthalpyScene.Show(gameObject, LabUI.UiFont);')
      && lines[i + 1] && lines[i + 1].includes('AuditLayout')) {
    lines.splice(i + 1, 0,
      '                Invoke(nameof(DumpInputAudit), 3f);   // 3 秒后把输入诊断打出来');
    done = true;
    console.log('✓ Invoke 已插在第', i + 2, '行');
    break;
  }
}
if (!done) {
  for (let i = 0; i < lines.length; i++) {
    if (lines[i].includes('var cs = EnthalpyScene.Show(gameObject, LabUI.UiFont);')) {
      lines.splice(i + 1, 0, '                if (cs != null) Invoke(nameof(DumpInputAudit), 3f);');
      done = true; console.log('✓ Invoke 已插在第', i + 2, '行（宽松匹配）');
      break;
    }
  }
}
if (done) fs.writeFileSync(B, lines.join('\n'), 'utf8');
else console.log('✗ 找不到锚点');
