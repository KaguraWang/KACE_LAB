const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 第 927 行少一个右括号与分号；Select 改成 _ui.SelectExternal */
const fixes = [
  ['items.Add(("脱离吸附", () => { SnapController.Detach(v); v.Burner = null; v.TripodRef = null; Select(v); })',
   '            items.Add(("脱离吸附", () => { SnapController.Detach(v); v.Burner = null; v.TripodRef = null; if (_ui != null) _ui.SelectExternal(v); }));'],
];
for (let i = 920; i < 934 && i < lines.length; i++) {
  if (lines[i].includes('脱离吸附')) {
    lines[i] = '            items.Add(("脱离吸附", () => { SnapController.Detach(v); v.Burner = null; v.TripodRef = null; if (_ui != null) _ui.SelectExternal(v); }));';
    console.log('  修第', i + 1, '行');
  }
  /* Select(v) → _ui.SelectExternal(v)，并补上 if 的花括号 */
  lines[i] = lines[i].replace(/Select\(v\);/, 'if (_ui != null) _ui.SelectExternal(v);');
}
/* if 没花括号的话补上 */
for (let i = 924; i < 930 && i < lines.length; i++) {
  if (lines[i].trim().startsWith('if (v.Burner != null') && !lines[i].includes('{')) {
    lines[i] = lines[i].replace(/\s*$/, ' {');
    lines[i + 1] = '            ' + lines[i + 1].trim();
    lines.splice(i + 2, 0, '            }');
    console.log('  给 if 补了花括号');
    break;
  }
}
fs.writeFileSync(B, lines.join('\n'), 'utf8');

const c = fs.readFileSync(B, 'utf8');
console.log('  剩余裸 Select(v):', (c.match(/[^.]Select\(v\)/g) || []).length);
