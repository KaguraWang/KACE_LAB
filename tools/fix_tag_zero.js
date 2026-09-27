const fs = require('fs');
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
let c = fs.readFileSync(V, 'utf8');

const oldBlock = `        _panel.anchoredPosition = Vector2.zero;
        _panel.localPosition = Vector3.zero;`;
const newBlock = `        /* 注意：_panel 就是 ct（同一个 transform），
           这里千万不能再写 _panel.localPosition = Vector3.zero ——
           那会把上面刚设好的世界位置立刻清零，
           四个标签全被打回父节点原点、叠在一起。 */`;

if (c.includes(oldBlock)) {
  c = c.replace(oldBlock, newBlock);
  fs.writeFileSync(V, c, 'utf8');
  console.log('✓ 已删掉把位置清零的两行');
} else {
  console.log('✗ 锚点未命中，实际内容：');
  const i = c.indexOf('_panel.anchoredPosition');
  console.log(c.substring(i - 200, i + 200));
}
const chk = fs.readFileSync(V, 'utf8');
console.log('  localPosition = Vector3.zero 还在吗:', chk.includes('_panel.localPosition = Vector3.zero'));
console.log('  括号差:', (chk.match(/\{/g) || []).length - (chk.match(/\}/g) || []).length);
