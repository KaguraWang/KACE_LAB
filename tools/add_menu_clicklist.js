const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

if (fs.readFileSync(E, 'utf8').includes('menu_')) { console.log('- 已存在'); process.exit(0); }

/* 在连线清单之后追加选单项清单 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('CLICK|edge_" + eq2')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到连线清单'); process.exit(1); }
let close = at;
for (let i = at; i < at + 12; i++) {
  if (lines[i] && lines[i].trim() === '}') { close = i; break; }
}
console.log('连线清单收尾在第', close + 1, '行');

const blk = [
  '',
  '        /* 选单项清单：右键弹出菜单后才知道有哪些项，名字就是按钮文字 */',
  '        if (_menuLayer != null) {',
  '            foreach (var mrt in _menuLayer.GetComponentsInChildren<RectTransform>(true)) {',
  '                if (mrt.name != "B") continue;',
  '                var mt = mrt.GetComponentInChildren<Text>();',
  '                if (mt == null || mt.text.Length == 0) continue;',
  '                var mc = ScreenOf(mrt);',
  '                sb.AppendLine("[CalcAudit] CLICK|menu_" + mt.text + "|" + Mathf.RoundToInt(mc.x) + "|" + Mathf.RoundToInt(mc.y) + "|0|0");',
  '            }',
  '        }',
];
lines.splice(close + 1, 0, ...blk);
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ 选单项清单已加');
const c = fs.readFileSync(E, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);

/* 审计由每秒的 StateTick 触发，但选单是临时出现的 —— 让 -calcDemo 每秒重打一次控件清单 */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('cs.AuditLayout()')) {
  b = b.replace('            if (cs != null) Debug.Log("[CalcState]\\n" + cs.StateAudit());',
                '            if (cs != null) {\n                Debug.Log("[CalcState]\\n" + cs.StateAudit());\n                Debug.Log(cs.AuditLayout());   /* 选单是临时出现的，每秒重打一次控件清单 */\n            }');
  fs.writeFileSync(B, b, 'utf8');
  console.log('  ✓ 控件清单改为每秒重打');
}
