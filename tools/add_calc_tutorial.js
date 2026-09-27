const fs = require('fs');
let n = 0;

/* =====================================================================
   补上「首次打开计算器时启动 15 步计算器教程」
     PetTutorial 的注释里写着这条，但从来没有任何代码触发它 ——
     TutorialKind.BornHaber 只在右键选单里手动可点，
     PetProgress.FinishCalc() 也永远不会被调用，calcDone 一直是 no。
     这里在 EnthalpyScene.Show 首次创建时触发，并在完成时打标记。
   ===================================================================== */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let e = fs.readFileSync(E, 'utf8');

const old = `        Instance = s;
        SetLabVisible(false);      // 新建路径也要藏 —— 教程第一次打开走的就是这条
        return s;`;
const nw = `        Instance = s;
        SetLabVisible(false);      // 新建路径也要藏 —— 教程第一次打开走的就是这条
        /* 首次打开计算器 -> 教一遍怎么用（约 15 步）。
           之前这里什么都没做，那条教程只能从右键选单里手动开，
           而且 PetProgress.FinishCalc() 永远不会被调用。 */
        if (PetProgress.LabDone && !PetProgress.CalcDone && !PetTutorial.Running)
            PetTutorial.StartDelayed(TutorialKind.BornHaber, 1.5f);
        return s;`;
if (e.includes(old)) { e = e.replace(old, nw); fs.writeFileSync(E, e, 'utf8'); n++; console.log('  ✓ 首次打开触发已加'); }
else console.log('  ✗ EnthalpyScene 锚点未命中');

/* -calcDemo 时也要能演示这条教程：让它先 Start 一次（教程自己会延迟启动） */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('StartCoroutine(PetTick())')) {
  b = b.replace('            if (a5 == "-petAudit") StartCoroutine(PetTick());',
                '            if (a5 == "-petAudit") StartCoroutine(PetTick());\n        /* -calcTut：强制走一遍计算器教程，便于验证 */\n        foreach (var a6 in System.Environment.GetCommandLineArgs())\n            if (a6 == "-calcTut") Invoke(nameof(ForceCalcTut), 2f);');
  b = b.replace('    void DumpTagAudit() {',
`    void ForceCalcTut() {
        if (!PetTutorial.Running) PetTutorial.Start(TutorialKind.BornHaber);
    }

    void DumpTagAudit() {`);
  fs.writeFileSync(B, b, 'utf8');
  n++;
  console.log('  ✓ -calcTut 已加');
}
const c = fs.readFileSync(B, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
console.log('改动处数:', n);
