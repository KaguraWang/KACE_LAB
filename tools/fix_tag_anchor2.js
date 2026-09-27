const fs = require('fs');
let n = 0;

/* 1) 删掉 anchoredPosition 清零 —— 它和 position = anchor 冲突 */
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
let v = fs.readFileSync(V, 'utf8');
const line = '        _panel.anchoredPosition = Vector2.zero;';
if (v.includes(line)) {
  v = v.replace(line, '        /* 这里绝不能再动 _panel 的位置属性（anchoredPosition / localPosition）——\n           _panel 就是 ct 本身，任何「归零」都会把上面设好的世界位置撤销掉。 */');
  fs.writeFileSync(V, v, 'utf8'); n++;
  console.log('  ✓ anchoredPosition 清零已删');
} else console.log('  ✗ anchoredPosition 行未命中');

/* 2) 消解加上限，避免发散 */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (b.includes('live[j].ExtraLift += need')) {
  b = b.replace('                            live[j].ExtraLift += need / Mathf.Max(1f, live[j].PixelsPerWorld);',
`                            live[j].ExtraLift += need / Mathf.Max(1f, live[j].PixelsPerWorld);
                            /* 加上限：万一 ScreenRect 失真，也不至于把标签顶到天上去 */
                            live[j].ExtraLift = Mathf.Min(live[j].ExtraLift, 1.6f);`);
  fs.writeFileSync(B, b, 'utf8'); n++;
  console.log('  ✓ ExtraLift 加 1.6 上限');
}
console.log('改动处数:', n);
console.log('  括号差 V:', (fs.readFileSync(V,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(V,'utf8').match(/\}/g)||[]).length);
console.log('  括号差 B:', (fs.readFileSync(B,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(B,'utf8').match(/\}/g)||[]).length);
