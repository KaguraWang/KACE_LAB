const fs = require('fs');
let n = 0;

/* 1) LabUI：把关键控件的中心点按游戏内屏幕坐标打出来 */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
if (!u.includes('TestClickTargets')) {
  const a = '    public string TestCat => _cat;';
  const add = `    /// <summary>把关键控件的中心点按「游戏内屏幕坐标」打出来，供外部自动化点击。
    /// 这样测试脚本就不用写死比例坐标 —— 分辨率一变比例布局就不一样了。
    /// 输出格式：CLICK|名字|x|y</summary>
    public string TestClickTargets() {
        Canvas.ForceUpdateCanvases();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("SCREEN|" + Screen.width + "|" + Screen.height);
        void Emit(string name, RectTransform rt) {
            if (rt == null) return;
            var c = rt.TransformPoint(rt.rect.center);
            sb.AppendLine("CLICK|" + name + "|" + Mathf.RoundToInt(c.x) + "|" + Mathf.RoundToInt(c.y));
        }
        var panel = _canvas.transform.Find("LeftPanel");
        if (panel != null) {
            foreach (var rt in panel.GetComponentsInChildren<RectTransform>(true)) {
                if (rt.name.StartsWith("Ap_")) Emit("vessel_" + rt.name.Substring(3), rt);
            }
        }
        var right = _canvas.transform.Find("RightPanel");
        if (right != null) {
            foreach (var rt in right.GetComponentsInChildren<RectTransform>(true)) {
                if (rt.name.StartsWith("Cat")) {
                    var txt = rt.GetComponentInChildren<Text>();
                    if (txt != null) Emit("cat_" + txt.text, rt);
                }
                if (rt.name == "Search") Emit("search", rt);
            }
        }
        foreach (var rt in _canvas.GetComponentsInChildren<RectTransform>(true)) {
            if (rt.name == "CalcBtn") Emit("opencalc", rt);
        }
        return sb.ToString();
    }

    public string TestCat => _cat;`;
  if (u.includes(a)) { u = u.replace(a, add); fs.writeFileSync(U, u, 'utf8'); n++; console.log('  ✓ TestClickTargets 已加'); }
  else console.log('  ✗ 锚点');
}

/* 2) 接到 -dumpLayout */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('TestClickTargets')) {
  const old = 'Debug.Log("DUMPSTART\\n" + _ui.TestDumpBars() + _ui.TestDumpLayout() + "\\nDUMPEND")';
  const nw  = 'Debug.Log("DUMPSTART\\n" + _ui.TestClickTargets() + _ui.TestDumpBars() + _ui.TestDumpLayout() + "\\nDUMPEND")';
  if (b.includes(old)) { b = b.replace(old, nw); fs.writeFileSync(B, b, 'utf8'); n++; console.log('  ✓ 已接到 -dumpLayout'); }
  else console.log('  ✗ dump 锚点未命中');
}
console.log('改动处数:', n);
