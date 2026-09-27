const fs = require('fs');
let n = 0;

/* 在 VesselTag 里记录上一次 Sync 的关键中间量，供审计打印 */
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
let v = fs.readFileSync(V, 'utf8');

if (!v.includes('public string LastSyncTrace')) {
  v = v.replace('    public float ExtraLift;',
`    public float ExtraLift;
    /// <summary>上一次 Sync 走到哪一步、关键量是多少（诊断用）</summary>
    public string LastSyncTrace = "(还没跑过)";`);
  fs.writeFileSync(V, v, 'utf8'); n++;
  console.log('  ✓ LastSyncTrace 已加');
}

/* Sync 里逐段记录 */
v = fs.readFileSync(V, 'utf8');
const marks = [
  ['        if (Model == null) return;',
   '        if (Model == null) { LastSyncTrace = "Model 为空"; return; }'],
  ['        if (!show) return;',
   '        if (!show) { LastSyncTrace = "ShowTags 关了"; return; }'],
  ['        if (_cam == null) _cam = Camera.main;',
   '        if (_cam == null) _cam = Camera.main;\n        LastSyncTrace = "Pos=" + Model.Pos + " LiftY=" + Model.LiftY + " cam=" + (_cam == null ? "null" : "有");'],
  ['        if (!vis) return;',
   '        if (!vis) { LastSyncTrace += " → Visible 返回 false，没定位"; return; }'],
];
for (const [a, b] of marks) {
  if (v.includes(a)) { v = v.replace(a, b); n++; console.log('  ✓ 标记:', a.trim().substring(0, 40)); }
  else console.log('  ✗ 找不到:', a.trim().substring(0, 40));
}
/* 定位成功后补一句 */
if (v.includes('_panel.localPosition = Vector3.zero;')) {
  v = v.replace('        _panel.localPosition = Vector3.zero;',
                '        _panel.localPosition = Vector3.zero;\n        LastSyncTrace += " → anchor=" + anchor;');
  n++;
  console.log('  ✓ 成功标记已加');
}
fs.writeFileSync(V, v, 'utf8');

/* 审计里打印 */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('LastSyncTrace')) {
  b = b.replace('            sb.AppendLine("[TagAudit]   " + t.gameObject.name + " ExtraLift=" + t.ExtraLift.ToString("F3")\n                        + " 屏幕矩形=" + t.ScreenRect);',
`            sb.AppendLine("[TagAudit]   " + t.gameObject.name + " ExtraLift=" + t.ExtraLift.ToString("F3")
                        + " 屏幕矩形=" + t.ScreenRect);
            sb.AppendLine("[TagAudit]      " + t.LastSyncTrace);`);
  fs.writeFileSync(B, b, 'utf8');
  n++;
  console.log('  ✓ 审计已打印 trace');
}
console.log('改动处数:', n);
console.log('  VesselTag 括号差:', (fs.readFileSync(V,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(V,'utf8').match(/\}/g)||[]).length);
console.log('  LabBootstrap 括号差:', (fs.readFileSync(B,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(B,'utf8').match(/\}/g)||[]).length);
