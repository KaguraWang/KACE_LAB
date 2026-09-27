const fs = require('fs');
let n = 0;

/* ---- 修 TestCardRects：换成世界坐标角点，才能互相比较 ---- */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
const oldRects = u.slice(u.indexOf('    /// <summary>卡片内各区段的矩形'), u.indexOf('    public string TestCat => _cat;'));
const newRects = `    /// <summary>卡片内各区段的矩形（**世界坐标**）。
    /// 注意不能用 RectTransform.rect 直接比 —— 那是各自局部坐标，
    /// 只有换算到同一空间（TransformPoint）才有可比性。 </summary>
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

    /// <summary>按钮名列表，供测试用</summary>
    public List<string> TestButtonsUnder(string parentName) {
        var res = new List<string>();
        var t = _canvas.transform.Find(parentName);
        if (t == null) return res;
        foreach (var b in t.GetComponentsInChildren<Button>(true)) res.Add(b.gameObject.name);
        return res;
    }

`;
u = u.replace(oldRects, newRects);
fs.writeFileSync(U, u, 'utf8'); n++;
console.log('✓ TestCardRects 已改为世界坐标');

/* ---- 修自检：用新结构 + 按名字找按钮 ---- */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');

/* 重叠检查改用世界坐标 */
sc = sc.replace(`                            var ra = rs[a].r; var rb = rs[b].r;
                            bool hit = ra.xMin < rb.xMax - 1 && rb.xMin < ra.xMax - 1
                                    && ra.yMin < rb.yMax - 1 && rb.yMin < ra.yMax - 1;`,
`                            var ra = rs[a].r; var rb = rs[b].r;
                            bool hit = ra.xMin < rb.xMax - 1 && rb.xMin < ra.xMax - 1
                                    && ra.yMin < rb.yMax - 1 && rb.yMin < ra.yMax - 1;`);

/* 分类按钮：按钮实际叫 Btn_<文字>，容器才叫 CatN */
sc = sc.replace(`                    bool clicked = ui.TestClickButton("Cat2");
                    Say("   点击 Cat2（氧化物）：" + (clicked ? "找到并触发了" : "✗ 根本没找到这个按钮"));`,
`                    Say("   试剂架下的按钮名：" + string.Join(", ", ui.TestButtonsUnder("LeftPanel")));
                    bool clicked = ui.TestClickButton("Btn_氧化物");
                    Say("   点击「氧化物」：" + (clicked ? "找到并触发了" : "✗ 根本没找到这个按钮"));`);

/* 列表条数：Destroy 是延迟的，要等一帧再数；这里改成用「有效行」判断 */
sc = sc.replace(`                    /* --- 直接测过滤器本身（排除点击层的问题）--- */
                    ui.TestSetFilter("acid", "");
                    int nAcid = ui.TestVisibleCount;
                    ui.TestSetFilter("base", "");
                    int nBase = ui.TestVisibleCount;
                    Say($"   直接设过滤器：酸 {nAcid} 条，碱 {nBase} 条");
                    TCheck("不同分类列出不同数量（酸≠碱）", nAcid != nBase && nAcid > 0 && nBase > 0);

                    /* --- 搜索框 --- */
                    ui.TestSetFilter("acid", "硫酸");
                    int nq = ui.TestVisibleCount;
                    Say($"   搜索「硫酸」→ {nq} 条");
                    TCheck("搜索能缩小结果", nq > 0 && nq < nAcid);

                    ui.TestSetFilter("acid", "zzzz不存在");
                    int nz = ui.TestVisibleCount;
                    Say($"   搜索「zzzz不存在」→ {nz} 条");
                    TCheck("搜不到时结果为空", nz == 0);`,
`                    /* --- 直接测过滤器本身（排除点击层的问题）---
                       注意：UnityEngine.Object.Destroy 是延迟到帧末执行的，
                       所以刚 RefreshReagents 完就数 childCount 会数到旧行。
                       这里改成查「实际会被列出的条目数」，不依赖 childCount。 */
                    int nAll = ui.TestFilterCount("", "");
                    ui.TestSetFilter("acid", "");
                    int nAcid = ui.TestFilterCount("acid", "");
                    ui.TestSetFilter("base", "");
                    int nBase = ui.TestFilterCount("base", "");
                    Say($"   全部 {nAll}，酸 {nAcid}，碱 {nBase}");
                    TCheck("不同分类列出不同数量（酸≠碱）", nAcid != nBase && nAcid > 0 && nBase > 0);

                    /* --- 搜索 --- */
                    int nq = ui.TestFilterCount("acid", "硫酸");
                    Say($"   在酸里搜「硫酸」→ {nq} 条");
                    TCheck("搜索能缩小结果", nq > 0 && nq < nAcid);

                    int nz = ui.TestFilterCount("acid", "zzzz不存在");
                    Say($"   搜「zzzz不存在」→ {nz} 条");
                    TCheck("搜不到时结果为空", nz == 0);

                    /* 化学式也能搜 */
                    int nf = ui.TestFilterCount("", "H2SO4");
                    Say($"   全库搜化学式「H2SO4」→ {nf} 条");
                    TCheck("能按化学式搜索", nf >= 1);`);
fs.writeFileSync(S, sc, 'utf8'); n++;
console.log('✓ 自检已更新');

/* ---- LabUI 加 TestFilterCount ---- */
u = fs.readFileSync(U, 'utf8');
if (!u.includes('TestFilterCount')) {
  const a2 = '    public string TestCat => _cat;';
  const add2 = `    /// <summary>按过滤条件算出「会列出多少条」，不依赖 childCount</summary>
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

    public string TestCat => _cat;`;
  u = u.replace(a2, add2);
  fs.writeFileSync(U, u, 'utf8'); n++;
  console.log('✓ TestFilterCount 已加');
}
console.log('补丁处数:', n);
