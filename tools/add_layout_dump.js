const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
if (!u.includes('TestDumpLayout')) {
  const a = '    public string TestCat => _cat;';
  const add = `    /// <summary>把左栏所有控件的位置打出来（世界坐标 = 屏幕坐标）。
    /// 靠肉眼看截图找排版问题太慢，让它自己报。 </summary>
    public string TestDumpLayout() {
        Canvas.ForceUpdateCanvases();
        var sb = new System.Text.StringBuilder();
        var panel = _canvas.transform.Find("LeftPanel") as RectTransform;
        if (panel == null) return "[Layout] 找不到 LeftPanel";
        var p0 = panel.TransformPoint(panel.rect.min);
        var p1 = panel.TransformPoint(panel.rect.max);
        float px0 = Mathf.Min(p0.x, p1.x), px1 = Mathf.Max(p0.x, p1.x);
        float py0 = Mathf.Min(p0.y, p1.y), py1 = Mathf.Max(p0.y, p1.y);
        sb.AppendLine("[Layout] 面板 x " + px0.ToString("F0") + ".." + px1.ToString("F0")
                    + "  y " + py0.ToString("F0") + ".." + py1.ToString("F0"));
        foreach (var g in panel.GetComponentsInChildren<Graphic>(true)) {
            if (g == null) continue;
            var t = g as Text;
            string label = t != null ? t.text : "";
            if (string.IsNullOrEmpty(label)) continue;
            if (label.Length > 12) label = label.Substring(0, 12) + "…";
            var rt = g.rectTransform;
            if (rt == null) continue;
            var a2 = rt.TransformPoint(rt.rect.min);
            var b2 = rt.TransformPoint(rt.rect.max);
            float x0 = Mathf.Min(a2.x, b2.x), x1 = Mathf.Max(a2.x, b2.x);
            float y0 = Mathf.Min(a2.y, b2.y), y1 = Mathf.Max(a2.y, b2.y);
            /* 越界的标出来 */
            bool outside = x0 < px0 - 1 || x1 > px1 + 1 || y0 < py0 - 1 || y1 > py1 + 1;
            sb.AppendLine("[Layout] " + (outside ? "✗越界 " : "      ")
                + Path(g.transform).Replace("LeftPanel/", "") + "  「" + label + "」"
                + " x " + x0.ToString("F0") + ".." + x1.ToString("F0")
                + " y " + y0.ToString("F0") + ".." + y1.ToString("F0"));
        }
        return sb.ToString();
    }

    public string TestCat => _cat;`;
  if (u.includes(a)) { u = u.replace(a, add); fs.writeFileSync(U, u, 'utf8'); console.log('✓ TestDumpLayout 已加'); }
  else console.log('✗ 锚点');
} else console.log('- 已存在');

/* 加一个命令行开关，直接 dump 布局 */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('-dumpLayout')) {
  b = b.replace('        Instance = this;',
`        Instance = this;
        foreach (var arg in System.Environment.GetCommandLineArgs())
            if (arg == "-dumpLayout") Debug.Log("DUMPSTART\\n" + LabUI.TestDumpLayoutStatic() + "DUMPEND");`);
  fs.writeFileSync(B, b, 'utf8');
  console.log('✓ -dumpLayout 开关已加');
}
