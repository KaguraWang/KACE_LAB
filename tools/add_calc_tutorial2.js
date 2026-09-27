const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

if (fs.readFileSync(E, 'utf8').includes('首次打开计算器')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].trim() === 'Instance = s;') { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 Instance = s;'); process.exit(1); }
console.log('在第', at + 1, '行:');
for (let i = at - 2; i <= at + 4; i++) console.log('   ', lines[i]);

const blk = [
  '        /* 首次打开计算器 -> 教一遍怎么用（约 15 步）。',
  '           PetTutorial 的注释里写着这条，但之前没有任何代码触发它 ——',
  '           BornHaber 教程只能从右键选单手动开，',
  '           而且 PetProgress.FinishCalc() 永远不会被调用，calcDone 一直是 no。 */',
  '        if (PetProgress.LabDone && !PetProgress.CalcDone && !PetTutorial.Running)',
  '            PetTutorial.StartDelayed(TutorialKind.BornHaber, 1.5f);',
];
lines.splice(at + 1, 0, ...blk);
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ 触发已加');
const c = fs.readFileSync(E, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);

/* -calcTut 也要能强制演示 */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('-calcTut')) {
  const a5 = '            if (a5 == "-petAudit") StartCoroutine(PetTick());';
  if (b.includes(a5)) {
    b = b.replace(a5, a5 + '\n        /* -calcTut：强制走一遍计算器教程，便于验证 */\n        foreach (var a6 in System.Environment.GetCommandLineArgs())\n            if (a6 == "-calcTut") Invoke(nameof(ForceCalcTut), 2f);');
    b = b.replace('    void DumpTagAudit() {',
      '    void ForceCalcTut() {\n        PetProgress.FinishLab(true);          /* 先让实验教程算完，否则起不来 */\n        if (!PetTutorial.Running) PetTutorial.Start(TutorialKind.BornHaber);\n    }\n\n    void DumpTagAudit() {');
    fs.writeFileSync(B, b, 'utf8');
    console.log('  ✓ -calcTut 已加');
  } else console.log('  ✗ -petAudit 锚点未命中');
}
const c2 = fs.readFileSync(B, 'utf8');
console.log('  括号差:', (c2.match(/\{/g) || []).length - (c2.match(/\}/g) || []).length);
