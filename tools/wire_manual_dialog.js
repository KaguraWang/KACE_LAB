const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

if (fs.readFileSync(E, 'utf8').includes('AskManualValue(e, !editing)')) { console.log('- 已接上'); process.exit(0); }

/* 选单里的「手动输入…」改成打开真正的输入对话框。
   原来只是把值设成 -100 —— AskManualValue 定义了却从来没被调用过。 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('items.Add(("手动输入…"')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到手动输入选单项'); process.exit(1); }
console.log('原来的「手动输入…」在第', at + 1, '行:');
for (let i = at - 1; i <= at + 7; i++) console.log('   ', lines[i]);

/* 找到这个 items.Add 的收尾 )); */
let end = at;
for (let i = at; i < at + 12; i++) {
  if (lines[i] && lines[i].trim().endsWith('}));')) { end = i; break; }
}
console.log('收尾在第', end + 1, '行:', lines[end]);

const nw = [
  '        items.Add(("手动输入…", () => AskManualValue(e, !editing)));',
];
lines.splice(at, end - at + 1, ...nw);
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ 「手动输入…」已改为打开输入对话框');
const c = fs.readFileSync(E, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
