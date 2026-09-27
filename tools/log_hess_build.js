const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

/* 在 Build 的关键步骤之间插日志 */
const marks = [
  ['        _canvas = gameObject.AddComponent<Canvas>();', '        Debug.Log("[HessBuild] 1 加 Canvas 前");'],
  ['        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;', '        Debug.Log("[HessBuild] 2 Canvas 已加=" + (_canvas != null));'],
  ['        gameObject.AddComponent<GraphicRaycaster>();', '        Debug.Log("[HessBuild] 3 Scaler 完成");'],
  ['        _root = (RectTransform)transform;', '        Debug.Log("[HessBuild] 4 Raycaster 完成");'],
];
for (const [target, log] of marks) {
  for (let i = lines.length - 1; i >= 0; i--) {
    if (lines[i] === target) { lines.splice(i, 0, log); break; }
  }
}
/* 末尾再加一条 */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('SeedExample();')) { lines.splice(i + 1, 0, '        Debug.Log("[HessBuild] 9 Build 完成");'); break; }
}
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ 分段日志已插');
