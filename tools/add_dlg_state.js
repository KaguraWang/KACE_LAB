const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

if (fs.readFileSync(E, 'utf8').includes('DLG|')) { console.log('- 已存在'); process.exit(0); }

/* 在 StateAudit 里报出「输入对话框开着没有」——
   外部脚本靠它判断点了「手动输入…」之后对话框有没有真的出现，
   比从控件名列表里猜靠谱（控件名会残留在历史日志里）。 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('sb.AppendLine("STATE|solved|"')) { at = i + 1; break; }
}
if (at < 0) { console.log('✗ 找不到 solved 那行'); process.exit(1); }
console.log('在第', at, '行前插入');

lines.splice(at, 0,
  '        /* 手动输入对话框开着没有 */',
  '        bool dlgOpen = false;',
  '        if (_menuLayer != null) {',
  '            foreach (var ch in _menuLayer.GetComponentsInChildren<RectTransform>(true)) {',
  '                if (ch.name == "ValueDlg" && ch.gameObject.activeInHierarchy) { dlgOpen = true; break; }',
  '            }',
  '        }',
  '        sb.AppendLine("STATE|dlg|" + (dlgOpen ? "open" : "closed"));');
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ STATE|dlg 已加');
const c = fs.readFileSync(E, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
