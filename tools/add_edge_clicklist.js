const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

if (fs.readFileSync(E, 'utf8').includes('edge_')) { console.log('- 已存在'); process.exit(0); }

/* 在节点清单之后追加连线清单（名字用方程，外部脚本按方程点） */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('CLICK|node_" + f + "|"')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到节点清单'); process.exit(1); }
/* 找到该 for 循环的收尾 */
let close = at;
for (let i = at; i < at + 12; i++) {
  if (lines[i] && lines[i].trim() === '}') { close = i; break; }
}
console.log('节点清单在第', at + 1, '行，收尾在', close + 1, '行');
for (let i = at - 2; i <= close; i++) console.log('   ', lines[i]);

const blk = [
  '',
  '        /* 连线清单：名字用方程，外部脚本按方程点，编号靠不住 */',
  '        for (int ei = 0; ei < _edges.Count; ei++) {',
  '            var ed = _edges[ei];',
  '            if (ed == null || ed.Rt == null) continue;',
  '            var et = ed.Rt.GetComponentInChildren<Text>();',
  '            if (et == null) continue;',
  '            var cc2 = ScreenOf((RectTransform)et.transform);',
  '            string eq2 = EdgeEquation(ed);',
  '            if (eq2.Length == 0) eq2 = "edge" + ei;',
  '            sb.AppendLine("[CalcAudit] CLICK|edge_" + eq2 + "|" + Mathf.RoundToInt(cc2.x) + "|" + Mathf.RoundToInt(cc2.y) + "|0|0");',
  '        }',
];
lines.splice(close + 1, 0, ...blk);
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ 连线清单已加');
const c = fs.readFileSync(E, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
