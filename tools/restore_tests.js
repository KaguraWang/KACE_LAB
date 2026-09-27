const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');

const blk = `    /* ================================================================
       测试接口
         这些不是给游戏用的，是给自动化自检用的。
         靠读代码猜「按钮为什么点不动」不可靠 —— 有了这些接口，
         测试能真的点按钮、真的做射线检测、真的比矩形。
       ================================================================ */
    public string TestCat => _cat;
    public string TestQuery => _query;
    public string TestAmount => _amount;
    public int TestVisibleCount => _contentList != null ? _contentList.childCount : -1;

    /// <summary>按过滤条件算出「会列出多少条」，不依赖 childCount。
    /// （UnityEngine.Object.Destroy 是帧末执行的，刚刷新完就数 childCount 会数到旧行）</summary>
    public int TestFilterCount(string cat, string query) {
        IEnumerable<(string id, string name, string formula, string state, char sol, string cls, string color)> list;
        if (!string.IsNullOrEmpty(query)) {
            var q = query.ToLowerInvariant();
            list = _reagents.Where(r => r.name.ToLowerInvariant().Contains(q)
                                     || r.formula.ToLowerInvariant().Contains(q)
                                     || r.id.Contains(q));
        } else {
            list = _reagents.Where(r => r.cls == cat);
        }
        return list.Count();
    }

    /// <summary>真的触发某个按钮的 onClick</summary>
    public bool TestClickButton(string namePart) {
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true)) {
            if (b.gameObject.name.Contains(namePart)) { b.onClick.Invoke(); return true; }
        }
        return false;
    }

    public List<string> TestAllButtons() {
        var res = new List<string>();
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true)) res.Add(b.gameObject.name);
        return res;
    }
    public List<string> TestButtonsUnder(string path) {
        var res = new List<string>();
        var t = _canvas.transform.Find(path);
        if (t == null) return res;
        foreach (var b in t.GetComponentsInChildren<Button>(true)) res.Add(b.gameObject.name);
        return res;
    }

    /// <summary>射线检测：模拟 EventSystem 在按钮中心会命中谁。
    /// 程序化 onClick.Invoke() 绕过了射线，测不出「被别的控件挡住」这类问题。 </summary>
    public string TestRaycastAt(string buttonNamePart) {
        var gr = _canvas.GetComponent<GraphicRaycaster>();
        if (gr == null) return "✗ 画布上没有 GraphicRaycaster";
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return "✗ 场景里没有 EventSystem";
        Button target = null;
        foreach (var b in _canvas.GetComponentsInChildren<Button>(true))
            if (b.gameObject.name == buttonNamePart) { target = b; break; }
        if (target == null) return "✗ 找不到按钮 " + buttonNamePart;

        Canvas.ForceUpdateCanvases();
        var rt = (RectTransform)target.transform;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
        var ped = new UnityEngine.EventSystems.PointerEventData(es);
        ped.position = screen;
        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        gr.Raycast(ped, results);
        if (results.Count == 0)
            return "✗ 按钮「" + target.name + "」在屏幕 " + screen + " 处射线打不到任何东西";
        var top = results[0].gameObject;
        bool ok = top == target.gameObject || top.transform.IsChildOf(target.transform);
        return ok ? "✓ 按钮「" + target.name + "」可点"
                  : "✗ 按钮「" + target.name + "」被「" + top.name + "」挡住（路径 " + Path(top.transform) + "）";
    }
    static string Path(Transform t) {
        var s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    /// <summary>卡片内各区段的矩形（**世界坐标**）。
    /// 不能用 RectTransform.rect 直接比 —— 那是各自局部坐标，只有换算到同一空间才有可比性。</summary>
    public List<(string name, Rect2 r)> TestCardRects() {
        var res = new List<(string, Rect2)>();
        var card = _canvas.transform.Find("LeftPanel/VesselCard") as RectTransform;
        if (card == null) return res;
        var list = new List<RectTransform>();
        string[] want = { "Contents", "PourBar", "BurnerBar", "TestBar", "TagBar", "EqBar" };
        foreach (var w in want) {
            var t = card.Find(w) as RectTransform;
            if (t != null && t.gameObject.activeSelf) list.Add(t);
        }
        for (int i = 0; i < 6; i++) {
            var t = card.Find("Act" + i) as RectTransform;
            if (t != null) list.Add(t);
        }
        foreach (var t in list) {
            var a = t.TransformPoint(t.rect.min);
            var b = t.TransformPoint(t.rect.max);
            res.Add((t.name, new Rect2 {
                xMin = Mathf.Min(a.x, b.x), xMax = Mathf.Max(a.x, b.x),
                yMin = Mathf.Min(a.y, b.y), yMax = Mathf.Max(a.y, b.y),
            }));
        }
        return res;
    }
    public struct Rect2 { public float xMin, xMax, yMin, yMax; }

    /// <summary>直接设过滤器并刷新（绕开输入框，单测过滤逻辑本身）</summary>
    public void TestSetFilter(string cat, string query) {
        _cat = cat; _query = query ?? "";
        RefreshReagents();
    }

`;

/* 插在左栏分节注释之前，或者文件里第一个 "/* --- 右：试剂架" 之前 */
let idx = u.indexOf('    /* ------------------------------------------------------------ 右：试剂架 */');
if (idx < 0) idx = u.indexOf('    void BuildRight()');
if (idx < 0) { console.log('✗ 找不到锚点'); process.exit(1); }
u = u.slice(0, idx) + blk + u.slice(idx);
fs.writeFileSync(U, u, 'utf8');
console.log('✓ 测试接口已补齐（插在', idx, '字符处）');

/* 清理可能残留的旧测试接口重复定义 */
const dup = (u.match(/public string TestCat => _cat;/g) || []).length;
console.log('  TestCat 定义次数:', dup);
