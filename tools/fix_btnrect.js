const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

/* Btn(...) 建出来的按钮锚点是 0..1（撑满父节点），
   要自由摆放必须先断开拉伸。这两处用的是「{ var br = ...; }」内联块。 */
let n = 0;
for (let i = 0; i < lines.length; i++) {
  const L = lines[i];
  if (L.includes('_pourBtn = Btn(_pourBar') && lines[i + 1] && lines[i + 1].includes('var br =')) {
    /* 吃掉后面的内联块（可能是 1~2 行） */
    let extra = 1;
    if (lines[i + 2] && lines[i + 2].includes('sizeDelta')) extra = 2;
    lines.splice(i + 1, extra,
      '        FixBtnRect(_pourBtn, new Vector2(236, 0), new Vector2(52, 24));');
    n++; console.log('  ✓ 倾倒按钮'); continue;
  }
  if (L.includes('_burnerBtn = Btn(_burnerBar') && lines[i + 1] && lines[i + 1].includes('var br =')) {
    let extra = 1;
    if (lines[i + 2] && lines[i + 2].includes('sizeDelta')) extra = 2;
    lines.splice(i + 1, extra,
      '        FixBtnRect(_burnerBtn, new Vector2(228, 0), new Vector2(60, 24));');
    n++; console.log('  ✓ 火力按钮'); continue;
  }
}
fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('替换处数:', n);
