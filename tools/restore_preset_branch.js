const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

if (fs.readFileSync(U, 'utf8').includes('加入约 {amt.water:F0} mL 水')) { console.log('- 预设分支已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('/* ---- 预设档位 ---- */')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到预设档位注释'); process.exit(1); }
console.log('在第', at + 1, '行后插入预设分支');

const blk = [
  '        if (id == "h2o") { v.AddWater(amt.water); Lab.Note(v, $"加入约 {amt.water:F0} mL 水", "add"); Lab.Settle(v); }',
  '        else if (state == "l" || state == "aq") {',
  '            double ml = v.Kind == VesselKind.Tube ? Math.Min(amt.l, 8) : amt.l;',
  '            Lab.AddLiquid(v, id, ml);',
  '        } else {',
  '            double g = v.Kind == VesselKind.Tube ? Math.Min(amt.s, 1.0) : amt.s;',
  '            Lab.AddReagent(v, id, g);',
  '        }',
];
lines.splice(at + 1, 0, ...blk);
fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('✓ 已插入');
const c = fs.readFileSync(U, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
const fl = c.split('\n');
let a2 = -1;
for (let i = 0; i < fl.length; i++) { if (fl[i].includes('void AddReagentTo(string id, string state, char sol) {')) { a2 = i; break; } }
console.log('--- AddReagentTo 结尾 ---');
for (let i = a2 + 30; i < a2 + 48; i++) console.log('  ', fl[i]);
