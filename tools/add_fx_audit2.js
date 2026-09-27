const fs = require('fs');
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(L, 'utf8').split('\n');

if (fs.readFileSync(L, 'utf8').includes('"FX|"')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('sb.AppendLine("STATE|vessel|"')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 STATE|vessel'); process.exit(1); }
/* 找这一段的结尾（STATE|vessel 那两行） */
let end = at;
for (let i = at; i < at + 4; i++) {
  if (lines[i] !== undefined && lines[i].includes('Mathf.RoundToInt(ssp.x)')) { end = i + 1; break; }
}
console.log('STATE|vessel 在', at + 1, '..', end + 1);
for (let i = at - 1; i <= end; i++) console.log('   ', lines[i]);

lines.splice(end + 1, 0,
  '',
  '        /* 视觉特效强度：验证「容器空了特效必须停」 */',
  '        foreach (var vv3 in _views) {',
  '            if (vv3 == null || vv3.Model == null) continue;',
  '            var fx = vv3.Model.Fx;',
  '            if (fx.Bubble > 0.01f || fx.Smoke > 0.01f || fx.Glow > 0.01f || fx.Flame)',
  '                sb.AppendLine("FX|" + vv3.Model.Id + "|泡" + fx.Bubble.ToString("F2")',
  '                            + " 烟" + fx.Smoke.ToString("F2") + " 光" + fx.Glow.ToString("F2")',
  '                            + " 焰" + (fx.Flame ? "开" : "关")',
  '                            + " 液" + vv3.Model.Volume().ToString("F0"));',
  '        }');
fs.writeFileSync(L, lines.join('\n'), 'utf8');
console.log('✓ FX 诊断已加');
console.log('  括号差:', (fs.readFileSync(L,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(L,'utf8').match(/\}/g)||[]).length);
