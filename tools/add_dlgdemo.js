const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

if (fs.readFileSync(B, 'utf8').includes('-dlgDemo')) { console.log('- 已存在'); process.exit(0); }

/* -dlgDemo：直接打开手动输入对话框。
   菜单链路（连线选单 -> 编辑焓变值 -> 来源选单含「手动输入…」）已经用
   窗口测试验过了；这里单独验证对话框本身能不能用。 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes("if (a7 == \"-noTut\") PetProgress.FinishCalc();")) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 -noTut'); process.exit(1); }
console.log('第', at + 1, '行:', lines[at].trim());

lines.splice(at + 1, 0,
  '        /* -dlgDemo：直接打开手动输入焓变对话框，单独验证对话框本身 */',
  '        foreach (var a8 in System.Environment.GetCommandLineArgs())',
  '            if (a8 == "-dlgDemo") Invoke(nameof(OpenValueDlg), 3f);');
fs.writeFileSync(B, lines.join('\n'), 'utf8');
console.log('✓ -dlgDemo 已加');

let b = fs.readFileSync(B, 'utf8');
if (!b.includes('void OpenValueDlg()')) {
  b = b.replace('    void ForceCalcTut() {',
`    void OpenValueDlg() {
        var s = EnthalpyScene.Instance;
        if (s == null || s.TestEdgeCount == 0) { Debug.Log("[DlgDemo] 没有连线"); return; }
        s.AskManualValue(s.TestFirstEdge(), true);
        Debug.Log("[DlgDemo] 已调用 AskManualValue");
    }

    void ForceCalcTut() {`);
  fs.writeFileSync(B, b, 'utf8');
  console.log('  ✓ OpenValueDlg 已加');
}
console.log('  括号差:', (fs.readFileSync(B,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(B,'utf8').match(/\}/g)||[]).length);

/* EnthalpyScene 暴露测试用的访问点 */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let e = fs.readFileSync(E, 'utf8');
if (!e.includes('public Edge TestFirstEdge')) {
  e = e.replace('    public string StateAudit() {',
`    /// <summary>给测试用的访问点</summary>
    public int TestEdgeCount => _edges.Count;
    public Edge TestFirstEdge() => _edges.Count > 0 ? _edges[0] : null;

    public string StateAudit() {`);
  fs.writeFileSync(E, e, 'utf8');
  console.log('  ✓ TestEdgeCount / TestFirstEdge 已加');
}
console.log('  EnthalpyScene 括号差:', (fs.readFileSync(E,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(E,'utf8').match(/\}/g)||[]).length);
