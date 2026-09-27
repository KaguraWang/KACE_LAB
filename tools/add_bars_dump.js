const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
if (!u.includes('TestDumpBars')) {
  u = u.replace('    public string TestCat => _cat;',
`    /// <summary>把两个工具栏的直接子节点连同 anchoredPosition / pivot / rect 打出来。
    /// 光看屏幕坐标对不上号，这样能看到真实的摆放参数。</summary>
    public string TestDumpBars() {
        Canvas.ForceUpdateCanvases();
        var sb = new System.Text.StringBuilder();
        foreach (var barName in new[] { "VesselCard/TagBar", "VesselCard/EqBar" }) {
            var bar = _canvas.transform.Find("LeftPanel/" + barName) as RectTransform;
            if (bar == null) { sb.AppendLine("[Bars] 找不到 " + barName); continue; }
            sb.AppendLine("[Bars] " + barName + "  anchorMin=" + bar.anchorMin + " anchorMax=" + bar.anchorMax
                        + " pivot=" + bar.pivot + " rect=" + bar.rect + " anchoredPos=" + bar.anchoredPosition);
            foreach (var ch in bar.GetComponentsInChildren<RectTransform>(true)) {
                if (ch.parent != bar) continue;
                sb.AppendLine("[Bars]    " + ch.name + "  pivot=" + ch.pivot
                            + "  anchoredPos=" + ch.anchoredPosition
                            + "  sizeDelta=" + ch.sizeDelta
                            + "  rect=" + ch.rect);
            }
        }
        return sb.ToString();
    }

    public string TestCat => _cat;`);
  fs.writeFileSync(U, u, 'utf8');
  console.log('✓ TestDumpBars 已加');
} else console.log('- 已存在');

const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('TestDumpBars')) {
  b = b.replace('if (arg == "-dumpLayout" && _ui != null) Debug.Log("DUMPSTART\\n" + _ui.TestDumpLayout() + "\\nDUMPEND");',
                'if (arg == "-dumpLayout" && _ui != null) Debug.Log("DUMPSTART\\n" + _ui.TestDumpBars() + _ui.TestDumpLayout() + "\\nDUMPEND");');
  fs.writeFileSync(B, b, 'utf8');
  console.log('✓ dump 已含 Bars');
}
