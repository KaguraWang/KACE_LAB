const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);')
      && lines[i - 1] && lines[i - 1].includes('顶栏是 (0,1)')) { at = i; break; }
}
if (at < 0) {
  for (let i = 0; i < lines.length; i++) {
    if (lines[i].includes('rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);')) { at = i; break; }
  }
}
if (at < 0) { console.log('✗ 找不到锚点行'); process.exit(1); }
console.log('第', at + 1, '行:', lines[at].trim());
console.log('上下文:');
for (let i = at - 3; i <= at + 3; i++) console.log('   ', lines[i]);

/* 锚点改用调用方传的 pivot。
   原来硬编码 (1,1)，那是顶栏的用法；对话框传 (1,0) 期望右下角，
   结果按钮被摆到了面板上方 12px 的外面。 */
lines[at] = '        rt.anchorMin = rt.anchorMax = pivot;';
lines[at - 1] = '        /* 锚点用调用方传的 pivot —— 顶栏传 (1,1) 从右上角往左排，';
lines[at - 2] = '           对话框传 (1,0) 从右下角往左排。硬编码 (1,1) 会把';
lines[at - 3] = '           对话框的按钮摆到面板外面去。 */';

fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ MkBtn 锚点已改为用 pivot');
for (let i = at - 3; i <= at + 2; i++) console.log('   新:', lines[i]);
const c = fs.readFileSync(E, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
