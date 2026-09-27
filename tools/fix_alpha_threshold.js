const fs = require('fs');
let n = 0;

/* =====================================================================
   根治：把 `col.a > 0.001f` 改成 `col.a > 0f`
     「要不要建 Image」的条件原本是 alpha > 0.001，
     但代码里到处用 0.001f 表示「几乎全透明但可点」——
     0.001 > 0.001 是 false，于是压根不建 Image，
     没有 Graphic 就没有 raycast 目标，控件点不到。
     已经因此栽过两次：选单挡板（选单关不掉）、连线热区（连线点不到）。
     阈值改成 > 0，只要有一点 alpha 就建 Image，语义才对。
   ===================================================================== */
const files = [
  'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs',
  'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs',
];
for (const f of files) {
  let c = fs.readFileSync(f, 'utf8');
  const before = (c.match(/col\.a > 0\.001f/g) || []).length;
  if (before > 0) {
    c = c.replace(/col\.a > 0\.001f/g, 'col.a > 0f');
    fs.writeFileSync(f, c, 'utf8');
    n += before;
    console.log('  ✓', f.split('/').pop(), '改了', before, '处');
  } else console.log('  -', f.split('/').pop(), '没有该模式');
}

/* 连线热区本身也补一句注释 */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let e = fs.readFileSync(E, 'utf8');
if (e.includes('var hit = MkNode("Hit", go, new Color(1, 1, 1, 0.001f));')) {
  e = e.replace('var hit = MkNode("Hit", go, new Color(1, 1, 1, 0.001f));',
    '/* 连线的点击热区：几乎全透明但必须可点（MkNode 现在只要 alpha > 0 就会建 Image） */\n        var hit = MkNode("Hit", go, new Color(1, 1, 1, 0.02f));');
  fs.writeFileSync(E, e, 'utf8');
  n++;
  console.log('  ✓ 连线热区 alpha 提到 0.02');
}
console.log('改动处数:', n);
