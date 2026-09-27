const fs = require('fs');
const L = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(L, 'utf8').split('\n');

/* 1) 抬高固体的比例下限：0.03 → 0.06，少量固体也要看得见 */
let a = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('float solidFrac = solidVol > 0 ? Mathf.Clamp(0.03f + solidVol / 60f, 0.02f, 0.34f) : 0;')) { a = i; break; }
}
if (a < 0) { console.log('✗ 找不到 solidFrac 那行'); process.exit(1); }
console.log('第', a + 1, '行:', lines[a].trim());
lines[a] = '        /* 下限从 0.03 抬到 0.06 —— 少量固体在白台面上原来几乎看不见 */\n' +
           '        float solidFrac = solidVol > 0 ? Mathf.Clamp(0.06f + solidVol / 55f, 0.05f, 0.40f) : 0;';

/* 2) SedimentColor 改成按量加权 + 提饱和 */
let b = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('static Color SedimentColor(Vessel m, Color fallback) {')) { b = i; break; }
}
if (b < 0) { console.log('✗ 找不到 SedimentColor'); process.exit(1); }
let e = -1;
for (let i = b; i < b + 30; i++) {
  if (lines[i] !== undefined && lines[i].includes('return new Color(r / n, g / n, b / n);')) { e = i; break; }
}
console.log('SedimentColor:', b + 1, '..', e + 1);

const nw = [
  '    static Color SedimentColor(Vessel m, Color fallback) {',
  '        /* 按**物质的量**加权，不是简单平均 ——',
  '           平均的话，白色沉淀 + 白色固体混出来几乎全白，在白台面上根本看不见。 */',
  '        float r = 0, g = 0, b2 = 0, wsum = 0;',
  '        void Acc(string id, double w) {',
  '            var s = ChemData.Sub[id];',
  '            if (s == null) return;',
  '            string hex = ChemData.Precip.TryGetValue(s.Formula, out var pi) && !string.IsNullOrEmpty(pi.Color)',
  '                       ? pi.Color : s.SolidColor;',
  '            if (string.IsNullOrEmpty(hex) || !ColorUtility.TryParseHtmlString(hex, out Color c)) return;',
  '            float wf = (float)System.Math.Max(0.0001, w);',
  '            r += c.r * wf; g += c.g * wf; b2 += c.b * wf; wsum += wf;',
  '        }',
  '        foreach (var kv in m.Undissolved) Acc(kv.Key, kv.Value);',
  '        foreach (var kv in m.Precipitates) Acc(kv.Key, kv.Value);',
  '        if (wsum <= 0f) return fallback;',
  '        var col = new Color(r / wsum, g / wsum, b2 / wsum);',
  '        /* 提一点饱和、压一点亮度：白粉放在白台面上要有对比才看得见 */',
  '        float mx = Mathf.Max(col.r, Mathf.Max(col.g, col.b));',
  '        float mn = Mathf.Min(col.r, Mathf.Min(col.g, col.b));',
  '        if (mx - mn < 0.12f) {',
  '            /* 近乎无彩色的固体：整体压暗一档，别和台面糊在一起 */',
  '            col = new Color(col.r * 0.86f, col.g * 0.86f, col.b * 0.88f);',
  '        }',
  '        return col;',
  '    }',
];
lines.splice(b, e - b + 1, ...nw);
fs.writeFileSync(L, lines.join('\n'), 'utf8');
console.log('✓ solidFrac 下限与 SedimentColor 已改');
console.log('  括号差:', (fs.readFileSync(L,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(L,'utf8').match(/\}/g)||[]).length);
