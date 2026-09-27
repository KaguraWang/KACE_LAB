const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');

/* 在 SyncViews 末尾加一条标签状态日志（用 -tagAudit 开启） */
if (!b.includes('TagAudit')) {
  b = b.replace(`    void DumpInputAudit() {`,
`    /// <summary>容器标签状态诊断：为什么标签没显示出来</summary>
    public string TagAudit() {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[TagAudit] ShowTags = " + TagSettings.ShowTags);
        sb.AppendLine("[TagAudit] _tagRoot = " + (_tagRoot == null ? "null！" : _tagRoot.name
                    + " active=" + _tagRoot.gameObject.activeInHierarchy));
        sb.AppendLine("[TagAudit] UiFont = " + (LabUI.UiFont == null ? "null！" : LabUI.UiFont.name));
        sb.AppendLine("[TagAudit] 容器数 = " + Lab.Vessels.Count + "  标签视图数 = " + _tags.Count);
        for (int i = 0; i < _tags.Count; i++) {
            var t = _tags[i];
            if (t == null) { sb.AppendLine("[TagAudit]   " + i + ": null"); continue; }
            var canvas = t.GetComponentInChildren<Canvas>(true);
            sb.AppendLine("[TagAudit]   " + i + ": " + t.gameObject.name
                        + " active=" + t.gameObject.activeInHierarchy
                        + " canvas=" + (canvas == null ? "无" : ("enabled=" + canvas.enabled + " order=" + canvas.sortingOrder))
                        + " 世界坐标=" + t.transform.position);
        }
        sb.AppendLine("[TagAudit] 方程式标签数 = " + _eqTags.Count
                    + "  Active = " + EquationTag.Active.Count);
        return sb.ToString();
    }

    void DumpInputAudit() {`);
  fs.writeFileSync(B, b, 'utf8');
  console.log('✓ TagAudit 已加');
}

/* -focusDemo 那种延迟回调里顺便打一份 */
let b2 = fs.readFileSync(B, 'utf8');
if (!b2.includes('TagAudit()')) { console.log('✗ 没写进去'); process.exit(1); }
if (!b2.includes('Invoke(nameof(DumpTagAudit)')) {
  b2 = b2.replace('        /* -focusDemo：启动后自动聚焦第一个器皿，用于截图验证轮廓高亮 */',
`        /* -tagAudit：把容器标签的状态打出来 */
        foreach (var a4 in System.Environment.GetCommandLineArgs())
            if (a4 == "-tagAudit") Invoke(nameof(DumpTagAudit), 4f);
        /* -focusDemo：启动后自动聚焦第一个器皿，用于截图验证轮廓高亮 */`);
  b2 = b2.replace('    void DumpInputAudit() {',
`    void DumpTagAudit() { Debug.Log(TagAudit()); }

    void DumpInputAudit() {`);
  fs.writeFileSync(B, b2, 'utf8');
  console.log('✓ -tagAudit 已接');
}
