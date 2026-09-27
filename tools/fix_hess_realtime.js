const fs = require('fs');
let n = 0;

const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

/* 1) NodeDrag.OnDrag 加 NodeMoved —— 这是「曲线不实时」的根因 */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('node.Rt.anchoredPosition = node.Pos;')
      && lines[i - 1] && lines[i - 1].includes('node.Pos += e.delta;')) {
    lines.splice(i + 1, 0,
      '        /* 关键：节点一动，连到它上面的线必须立刻重画。',
      '           少了这一句，拖节点时线就停在原地 —— 看起来就是「曲线不是实时计算的」。 */',
      '        scene.NodeMoved();');
    n++;
    console.log('  ✓ NodeDrag.OnDrag 已补 NodeMoved（第', i + 2, '行）');
    break;
  }
}

/* 2) 示例场景：系数参数传成了分子式，改成 "1" */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('AddNode(new Vector2(-340, 60), "C", "C")')) {
    lines[i] = lines[i].replace('AddNode(new Vector2(-340, 60), "C", "C")', 'AddNode(new Vector2(-340, 60), "1", "C")');
    n++; console.log('  ✓ 石墨节点系数 → "1"');
  }
  if (lines[i].includes('AddNode(new Vector2(40, 250), "CO2", "CO2")')) {
    lines[i] = lines[i].replace('AddNode(new Vector2(40, 250), "CO2", "CO2")', 'AddNode(new Vector2(40, 250), "1", "CO2")');
    n++; console.log('  ✓ CO2 节点系数 → "1"');
  }
  if (lines[i].includes('AddNode(new Vector2(40, -140), "CO", "CO")')) {
    lines[i] = lines[i].replace('AddNode(new Vector2(40, -140), "CO", "CO")', 'AddNode(new Vector2(40, -140), "1", "CO")');
    n++; console.log('  ✓ CO 节点系数 → "1"');
  }
}

/* 3) Born-Haber 示例同样是这个问题 */
for (let i = 0; i < lines.length; i++) {
  const m = lines[i].match(/AddNode\(new Vector2\(([-\d.]+), ([-\d.]+)\),\s*"([^"]*)",\s*"([^"]*)"\)/);
  if (m && m[3] === m[4]) {
    lines[i] = lines[i].replace('"' + m[3] + '", "' + m[4] + '"', '"1", "' + m[4] + '"');
    n++;
    console.log('  ✓ 修正系数:', m[4]);
  }
}

/* 4) 解析失败时不要静默变 0 */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('double.TryParse(coef, out n.Coef)')) {
    lines[i] = '        /* TryParse 失败会把 out 参数置 0 —— 那会让标注显示成「0CO₂」。'
      + '\n        解析不了就当 1（默认系数）。 */'
      + '\n        if (!double.TryParse(coef, out n.Coef)) n.Coef = 1;';
    /* 原来的 if 行要去掉 */
    lines[i - 1] = '';
    n++;
    console.log('  ✓ TryParse 失败回退为 1');
  }
}

fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('改动处数:', n);

/* 查验 */
const chk = fs.readFileSync(E, 'utf8');
console.log('  NodeMoved 在 OnDrag 里:', /node\.Rt\.anchoredPosition = node\.Pos;[\s\S]{0,200}scene\.NodeMoved\(\)/.test(chk));
console.log('  示例系数为 1:', chk.includes('AddNode(new Vector2(-340, 60), "1", "C")'));
