const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 删掉悬空 foreach（第 256 行，它的循环体被 PetView.Show 顶替了） */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].trim() === 'foreach (var arg in System.Environment.GetCommandLineArgs())'
      && lines[i + 1] && lines[i + 1].includes('PetView.Show')) {
    lines.splice(i, 1);
    console.log('✓ 删掉第', i + 1, '行的悬空 foreach');
    break;
  }
}
fs.writeFileSync(B, lines.join('\n'), 'utf8');

/* 自检里的焓变场景失败：无头环境下 EnthalpyScene.Show 会调 SetLabVisible,
   而 LabBootstrap.Instance 为 null —— 已经用了 ?. 保护，
   但 SetLabVisible 里还碰了 LabBootstrap.CalculatorActive（静态字段，安全）。
   真正炸的是 Build() 里的 MkText 用 LabUI.UiFont 为 null。加个保护。 */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let e = fs.readFileSync(E, 'utf8');
if (!e.includes('// 无头测试时 UiFont 可能还没设')) {
  e = e.replace('    void Build(Font font) {',
`    void Build(Font font) {
        /* 无头测试时 UiFont 可能还没设 —— MkText 全都用它，为 null 会直接空引用 */
        if (LabUI.UiFont == null) LabUI.UiFont = font;
`);
  fs.writeFileSync(E, e, 'utf8');
  console.log('✓ EnthalpyScene.Build 加了 UiFont 保护');
}
const chk = fs.readFileSync(B, 'utf8');
console.log('  Build() 次数:', (chk.match(/_ui\.Build\(\);/g) || []).length);
console.log('  悬空 foreach 残留:', chk.includes('foreach (var arg in System.Environment.GetCommandLineArgs())\n        /*'));
