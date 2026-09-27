const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

if (fs.readFileSync(E, 'utf8').includes('热区跟着标注走')) { console.log('- 已存在'); process.exit(0); }

/* 在 RedrawEdges 里给标签定位之后，把 Heat 热区也贴过去 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('trt.anchoredPosition = PickLabelPos(e, p0, ctrl, p1);')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到标签定位行'); process.exit(1); }
console.log('标签定位在第', at + 1, '行:', lines[at].trim());

const blk = [
  '            /* 热区跟着标注走 ——',
  '               原来 Hit 只在 Connect 里建了一次、从没重新定位，',
  '               于是它一直停在父节点原点、尺寸也是 MkNode 的默认 100x100，',
  '               连线根本点不到（右键连线弹出的会是空白处的「新建节点」选单）。 */',
  '            if (e.Hit != null) {',
  '                var hrt = (RectTransform)e.Hit.transform;',
  '                hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);',
  '                hrt.pivot = new Vector2(0.5f, 0.5f);',
  '                hrt.anchoredPosition = trt.anchoredPosition;',
  '                hrt.sizeDelta = new Vector2(LW, LH);',
  '            }',
];
lines.splice(at + 1, 0, ...blk);
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ 热区跟随已加');
const c = fs.readFileSync(E, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
