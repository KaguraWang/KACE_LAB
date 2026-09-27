const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* -focusDemo：启动几秒后自动聚焦第一个器皿，用于截图验证轮廓 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('if (arg == "-hessDemo") {') || lines[i].includes('"-hessDemo"')) { at = i; }
}
if (at < 0) {
  for (let i = 0; i < lines.length; i++) {
    if (lines[i].includes('foreach (var a in System.Environment.GetCommandLineArgs()) {')) { at = i - 1; break; }
  }
}
console.log('插入点在第', at + 1, '行');

if (!lines.some(l => l.includes('"-focusDemo"'))) {
  lines.splice(at + 1, 0,
    '        /* -focusDemo：启动后自动聚焦第一个器皿，用于截图验证轮廓高亮 */',
    '        foreach (var a in System.Environment.GetCommandLineArgs())',
    '            if (a == "-focusDemo") Invoke(nameof(FocusDemo), 2.5f);');
  fs.writeFileSync(B, lines.join('\n'), 'utf8');
  console.log('✓ -focusDemo 已加');
}

let b = fs.readFileSync(B, 'utf8');
if (!b.includes('void FocusDemo()')) {
  b = b.replace('    void DumpInputAudit() {',
`    void FocusDemo() {
        if (Lab.Vessels.Count == 0) { var v0 = Lab.AddVessel(VesselKind.Beaker); v0.AddWater(60); }
        SetFocus(Lab.Vessels[0]);
        Debug.Log("[ChemLab] focusDemo 聚焦 = " + (_focused != null ? _focused.ToString() : "null")
                + "  轮廓 = " + (_focusOutline != null ? (_focusOutline.IsOn ? "已开" : "未开") : "未建"));
    }

    void DumpInputAudit() {`);
  fs.writeFileSync(B, b, 'utf8');
  console.log('✓ FocusDemo 方法已加');
}
