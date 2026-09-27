const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

if (fs.readFileSync(B, 'utf8').includes('-noTut')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('if (calcDemo) PetProgress.FinishLab(true);')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 calcDemo 那行'); process.exit(1); }
console.log('第', at + 1, '行:', lines[at]);

lines.splice(at + 1, 0,
  '        /* -noTut：跳过自动教程。首次打开计算器会自动跑 15 步教程，',
  '           其中一步会 ClearAll —— 测试要右键连线时画布已经空了。 */',
  '        foreach (var a7 in System.Environment.GetCommandLineArgs())',
  '            if (a7 == "-noTut") PetProgress.FinishCalc();');
fs.writeFileSync(B, lines.join('\n'), 'utf8');
console.log('✓ -noTut 已加');
const c = fs.readFileSync(B, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
