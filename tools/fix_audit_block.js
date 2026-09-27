const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 找到「存活标签」那段，整段重写成干净的代码 */
let from = -1, to = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('存活标签') && lines[i].includes('AppendLine')) { from = i - 6; break; }
}
if (from < 0) { console.log('✗ 找不到'); process.exit(1); }
/* 从「标签重叠检查」注释开始 */
for (let i = from; i < from + 20; i++) {
  if (lines[i] && lines[i].includes('标签重叠检查')) { from = i; break; }
}
for (let i = from; i < from + 25; i++) {
  if (lines[i] && lines[i].includes('方程式标签数')) { to = i - 1; break; }
}
console.log('重写范围', from + 1, '..', to + 1);
for (let i = from; i <= to; i++) console.log('  旧:', lines[i]);

const nw = [
  '        /* 标签重叠检查 */',
  '        var liveTags = new List<VesselTag>();',
  '        foreach (var lt in _tags) if (lt != null && lt.gameObject.activeSelf) liveTags.Add(lt);',
  '        int lap = 0;',
  '        for (int i = 0; i < liveTags.Count; i++)',
  '            for (int j = i + 1; j < liveTags.Count; j++) {',
  '                var ra = liveTags[i].ScreenRect; var rb = liveTags[j].ScreenRect;',
  '                if (ra.width <= 0f || rb.width <= 0f) continue;',
  '                if (ra.xMin < rb.xMax && rb.xMin < ra.xMax && ra.yMin < rb.yMax && rb.yMin < ra.yMax) lap++;',
  '            }',
  '        sb.AppendLine("[TagAudit] 存活标签 " + liveTags.Count + " 个，重叠对数 = " + lap);',
  '        foreach (var lt in liveTags) {',
  '            sb.AppendLine("[TagAudit]   " + lt.gameObject.name',
  '                        + " ExtraLift=" + lt.ExtraLift.ToString("F3")',
  '                        + " 屏幕矩形=" + lt.ScreenRect);',
  '            sb.AppendLine("[TagAudit]      " + lt.LastSyncTrace);',
  '        }',
];
lines.splice(from, to - from + 1, ...nw);
fs.writeFileSync(B, lines.join('\n'), 'utf8');
const c = fs.readFileSync(B, 'utf8');
console.log('✓ 已重写');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);

/* 检查缩进后的上下文 */
const l2 = fs.readFileSync(B, 'utf8').split('\n');
console.log('  上下文:');
for (let i = from - 2; i < from + 20; i++) console.log('   ', (i + 1) + ':', l2[i]);
