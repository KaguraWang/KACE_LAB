const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');

/* 把射线检测换成几何遮挡检测：
   在无头编辑器里 GraphicRaycaster 依赖画布尺寸与屏幕尺寸，
   拿到的结果不可靠。改成直接找所有 raycastTarget 为真的图形，
   看谁的矩形盖住了按钮中心、且在层级上排在按钮后面（后画=在上）。 */
const s = u.indexOf('    /// <summary>射线检测：模拟 EventSystem 在按钮中心会命中谁。');
const e = u.indexOf('    static string Path(Transform t) {');
if (s < 0 || e < 0) { console.log('✗ 锚点'); process.exit(1); }

const nw = `    /// <summary>几何遮挡检测：按钮中心有没有被别的可点图形盖住。
    /// 不用 GraphicRaycaster —— 无头编辑器里它依赖画布/屏幕尺寸，结果不可靠。
    /// 判据：所有 raycastTarget 为真的图形里，矩形包含按钮中心、
    ///       且在兄弟顺序上排在按钮之后的（后画的在上面）就算遮挡。 </summary>
    public string TestOcclusionAt(string buttonNamePart) {
        Button target = null;
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true))
            if (b.gameObject.name == buttonNamePart) { target = b; break; }
        if (target == null) return "✗ 找不到按钮 " + buttonNamePart;

        Canvas.ForceUpdateCanvases();
        var trt = (RectTransform)target.transform;
        Vector3 c = trt.TransformPoint(trt.rect.center);

        var hits = new List<string>();
        foreach (var g in _canvas.GetComponentsInChildren<Graphic>(true)) {
            if (g == null || !g.raycastTarget) continue;
            if (!g.gameObject.activeInHierarchy) continue;
            if (g.transform == target.transform || g.transform.IsChildOf(target.transform)) continue;
            if (target.transform.IsChildOf(g.transform)) continue;      // 自己的父节点不算
            var rt = g.rectTransform;
            if (rt == null) continue;
            Vector3 a = rt.TransformPoint(rt.rect.min), b2 = rt.TransformPoint(rt.rect.max);
            bool inside = c.x >= Mathf.Min(a.x, b2.x) && c.x <= Mathf.Max(a.x, b2.x)
                       && c.y >= Mathf.Min(a.y, b2.y) && c.y <= Mathf.Max(a.y, b2.y);
            if (!inside) continue;
            /* 后画的在上面：比较在画布下的渲染顺序 */
            if (RenderOrder(g.transform) > RenderOrder(target.transform))
                hits.Add(g.gameObject.name + "(" + Path(g.transform) + ")");
        }
        return hits.Count == 0
            ? "✓ 按钮「" + target.name + "」没有被遮挡"
            : "✗ 按钮「" + target.name + "」被 " + string.Join(" / ", hits) + " 盖住";
    }
    static int RenderOrder(Transform t) {
        int ord = 0, mul = 1;
        while (t != null) { ord += t.GetSiblingIndex() * mul; mul *= 1000; t = t.parent; }
        return ord;
    }

`;
u = u.slice(0, s) + nw + u.slice(e);
fs.writeFileSync(U, u, 'utf8');
console.log('✓ TestOcclusionAt 已替换 TestRaycastAt');

/* 自检改用它 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
sc = sc.replace(/lui\.TestRaycastAt\(tn\)/g, 'lui.TestOcclusionAt(tn)');
sc = sc.replace('string[] targets = { "Btn_金属", "Btn_氧化物", "Btn_少量", "Btn_适量", "Btn_过量" };',
                'string[] targets = { "Btn_金属", "Btn_氧化物", "Btn_酸", "Btn_碱", "Btn_适量" };');
sc = sc.replace('TCheck("试剂架上的按钮都能被射线命中（没被别的控件挡住）", ov == 0);',
                'TCheck("试剂架上的按钮没有被别的控件盖住", ov == 0);');
fs.writeFileSync(S, sc, 'utf8');
console.log('✓ 自检已改用几何检测');
