const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

if (fs.readFileSync(U, 'utf8').includes('精确模式：按用户输入的量加')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('if (id == "h2o") { v.AddWater(amt.water);')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到加药分支'); process.exit(1); }
console.log('加药分支在第', at + 1, '行:');
for (let i = at - 3; i <= at + 8; i++) console.log('   ', lines[i]);

/* 找到这一段的收尾（else 块的 }） */
let e = at;
for (let i = at; i < at + 12; i++) {
  if (lines[i] !== undefined && lines[i].trim() === '}' && lines[i - 1] && lines[i - 1].includes('AddReagent(v, id, g);')) { e = i; break; }
}
console.log('收尾在第', e + 1, '行');

const nw = [
  '        /* ---- 精确模式：按用户输入的量加 ---- */',
  '        if (precise) {',
  '            if (id == "h2o") {',
  '                double w = _preciseUnit == 3 ? _preciseVal : pg;',
  '                if (w > 0) { v.AddWater(w); Lab.Note(v, $"加入 {w:0.###} mL 水", "add"); Lab.Settle(v); }',
  '            } else if (_preciseUnit == 3) {',
  '                /* 单位是 mL：只能用于液体或溶液 */',
  '                if (state == "l" || state == "aq") {',
  '                    Lab.AddLiquid(v, id, _preciseVal);',
  '                } else {',
  '                    Hint($"{id} 是固体，不能用 mL —— 请把单位切成 mg / g / mmol");',
  '                }',
  '            } else {',
  '                /* mg / g / mmol：统一换算成克；mL 单位的液体按 1 g/mL 近似 */',
  '                double g2 = pg;',
  '                if (state == "l" || state == "aq") {',
  '                    /* 液体用 AddLiquid 才走体积模型；克换算成毫升按 1:1 近似 */',
  '                    Lab.AddLiquid(v, id, g2);',
  '                } else {',
  '                    Lab.AddReagent(v, id, g2);',
  '                }',
  '            }',
  '            Lab.RxnCount += Math.Max(0, Lab.Log.Count(l => l.Kind == "rxn") - before);',
  '            RefreshVesselPanel(); RefreshLog();',
  '            return;',
  '        }',
  '',
  '        /* ---- 预设档位 ---- */',
];
lines.splice(at, e - at + 1, ...nw);
fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('✓ 精确模式分支已加');

const c = fs.readFileSync(U, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
/* 打印改动后的完整方法 */
const fl = fs.readFileSync(U, 'utf8').split('\n');
let a2 = -1;
for (let i = 0; i < fl.length; i++) { if (fl[i].includes('void AddReagentTo(string id, string state, char sol) {')) { a2 = i; break; } }
console.log('--- 改后的 AddReagentTo ---');
for (let i = a2; i < a2 + 48; i++) console.log('  ', fl[i]);
