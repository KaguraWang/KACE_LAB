const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

if (fs.readFileSync(E, 'utf8').includes('STATE|dlgOK|')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('sb.AppendLine("STATE|dlg|"')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 STATE|dlg'); process.exit(1); }
console.log('第', at + 1, '行:', lines[at].trim());

/* 紧跟着把对话框里「确定」「取消」以及输入框的屏幕坐标也报出来 ——
   外部脚本直接点这些坐标，不用从控件名列表里猜（同名旧项会残留在历史日志里）。 */
lines.splice(at + 1, 0,
  '        /* 对话框里各控件的屏幕坐标：外部脚本直接点，不猜 */',
  '        if (dlgOpen && _menuLayer != null) {',
  '            foreach (var ch in _menuLayer.GetComponentsInChildren<RectTransform>(true)) {',
  '                if (!ch.gameObject.activeInHierarchy) continue;',
  '                if (ch.name == "ValueDlg") {',
  '                    var dc = ScreenOf(ch);',
  '                    sb.AppendLine("STATE|dlgBox|" + Mathf.RoundToInt(dc.x) + "," + Mathf.RoundToInt(dc.y));',
  '                    continue;',
  '                }',
  '                var bt = ch.GetComponentInChildren<Text>();',
  '                if (bt == null || ch.name != "B") continue;',
  '                var bc = ScreenOf(ch);',
  '                if (bt.text == "确定")',
  '                    sb.AppendLine("STATE|dlgOK|" + Mathf.RoundToInt(bc.x) + "," + Mathf.RoundToInt(bc.y));',
  '                else if (bt.text == "取消")',
  '                    sb.AppendLine("STATE|dlgCancel|" + Mathf.RoundToInt(bc.x) + "," + Mathf.RoundToInt(bc.y));',
  '            }',
  '        }');
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ 对话框各控件的屏幕坐标已加');
const c = fs.readFileSync(E, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
