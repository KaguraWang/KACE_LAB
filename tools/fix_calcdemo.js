const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 找 -calcDemo 那段，把它往前提到「教程是否自动开始」的判断之前，
   并顺手把教程进度标记为已完成，避免两者抢场景 */
let calcIdx = -1, tutIdx = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('"-calcDemo"')) calcIdx = i;
  if (lines[i].includes('if (!PetProgress.LabDone) PetTutorial.StartDelayed')) tutIdx = i;
}
console.log('-calcDemo 在第', calcIdx + 1, '行；教程自动开始在第', tutIdx + 1, '行');

/* 在教程判断之前插一段：有 -calcDemo 就先把教程标记为已完成 */
if (tutIdx >= 0 && !lines.slice(0, tutIdx).some(l => l.includes('// -calcDemo 时不要自动开教程'))) {
  lines.splice(tutIdx, 0,
    '        /* -calcDemo 时不要自动开教程 —— 教程第一步会 SwitchScene(false)，',
    '           会把刚打开的计算器又藏起来，两者互相抢场景。 */',
    '        bool calcDemo = false;',
    '        foreach (var arg in System.Environment.GetCommandLineArgs())',
    '            if (arg == "-calcDemo") calcDemo = true;',
    '        if (calcDemo) PetProgress.FinishLab(true);');
  fs.writeFileSync(B, lines.join('\n'), 'utf8');
  console.log('✓ 已插入');
} else console.log('- 已存在或锚点失效');
