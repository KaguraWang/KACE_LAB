const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');

if (b.includes('KeyDbg')) { console.log('- 已存在'); process.exit(0); }

/* 在 Update 里抓键盘输入 —— Input.inputString 每帧清空，
   只有当场打出来才看得到。 */
let at = -1;
for (let i = 0; i < b.length; i++) {
  if (b.startsWith('    void Update() {', i)) { at = i; break; }
}
if (at < 0) {
  for (let i = 0; i < b.length; i++) {
    if (b.startsWith('    void Update(', i)) { at = i; break; }
  }
}
if (at < 0) { console.log('✗ 找不到 Update'); process.exit(1); }
console.log('Update 在第', b.substring(0, at).split('\n').length, '行');

const lines = b.split('\n');
let li = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].startsWith('    void Update(')) { li = i; break; }
}
/* 找到 Update 的第一行之后插入 */
lines.splice(li + 1, 0,
  '        /* 键盘诊断：Input.inputString 每帧清空，只有当场打出来才看得到 */',
  '        if (!string.IsNullOrEmpty(Input.inputString)) {',
  '            var es0 = UnityEngine.EventSystems.EventSystem.current;',
  '            Debug.Log("[KeyDbg] 收到按键 \\"" + Input.inputString + "\\"  当前选中="',
  '                    + (es0 == null || es0.currentSelectedGameObject == null ? "(无)" : es0.currentSelectedGameObject.name) + ")");',
  '        }');
fs.writeFileSync(B, lines.join('\n'), 'utf8');
console.log('✓ KeyDbg 已加');
console.log('  括号差:', (fs.readFileSync(B,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(B,'utf8').match(/\}/g)||[]).length);
