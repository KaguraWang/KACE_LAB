/* =====================================================================
   左栏：器材架与容器卡片不得重叠
     病灶：器材架按钮 y = -58 - row*50、高 42，第 4 行占 -208..-250；
     而容器卡片顶在 -220 —— 正好压住「移液管 / 蒸发皿」，点不到。

     做法：
       ① 器材架压缩一点（行高 42→36，间距 50→42），四行到 -214 结束
       ② 卡片下移到 -224
       ③ 自检的重叠检查从「只看卡片内部」扩到「整个左栏」
   ===================================================================== */
const fs = require('fs');
let n = 0;

/* 1) 器材架压缩 */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
const oldRack = `            rt.anchoredPosition = new Vector2(16 + col * 186, -58 - row * 50);
            rt.sizeDelta = new Vector2(178, 42);`;
const newRack = `            /* 行高 36、间距 42 —— 四行到 -214 为止，给下面的容器卡片让出位置。
               以前是 42/50，第四行到 -250，会被卡片（-220 起）盖住。 */
            rt.anchoredPosition = new Vector2(16 + col * 186, -52 - row * 42);
            rt.sizeDelta = new Vector2(178, 36);`;
if (u.includes(oldRack)) { u = u.replace(oldRack, newRack); n++; console.log('  ✓ 器材架已压缩'); }
else console.log('  ✗ 器材架锚点未命中');

/* 2) 卡片下移 */
if (u.includes('        float y = -220;')) {
  u = u.replace('        float y = -220;', '        float y = -224;   /* 器材架四行到 -214 结束，卡片从 -224 开始 */');
  n++; console.log('  ✓ 卡片下移到 -224');
} else console.log('  ✗ 卡片 y 锚点未命中');

fs.writeFileSync(U, u, 'utf8');

/* 3) 自检：整个左栏的控件不得重叠（含器材架 vs 卡片） */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
if (!sc.includes('左栏所有可点控件')) {
  sc = sc.replace('                    TCheck("左栏卡片内没有区段互相重叠", ov == 0);',
`                    TCheck("左栏卡片内没有区段互相重叠", ov == 0);

                    /* 只看卡片内部是不够的 —— 器材架第 4 行被卡片盖住过。
                       这里查整个左栏里所有「可点图形」两两之间有没有叠。 */
                    var all = lui.TestPanelClickables();
                    Say("   左栏可点控件 " + all.Count + " 个");
                    int hit = 0;
                    for (int a = 0; a < all.Count; a++)
                        for (int b = a + 1; b < all.Count; b++) {
                            var ra = all[a].r; var rb = all[b].r;
                            bool lap = ra.xMin < rb.xMax - 1 && rb.xMin < ra.xMax - 1
                                    && ra.yMin < rb.yMax - 1 && rb.yMin < ra.yMax - 1;
                            /* 父子关系不算重叠 */
                            if (lap && !all[a].name.StartsWith(all[b].name) && !all[b].name.StartsWith(all[a].name)) {
                                hit++;
                                if (hit <= 6) Say($"   ✗ 「{all[a].name}」与「{all[b].name}」重叠");
                            }
                        }
                    TCheck("左栏所有可点控件互不重叠", hit == 0);`);
  fs.writeFileSync(S, sc, 'utf8'); n++;
  console.log('  ✓ 自检扩到整个左栏');
}

/* 4) LabUI 暴露 TestPanelClickables */
u = fs.readFileSync(U, 'utf8');
if (!u.includes('TestPanelClickables')) {
  u = u.replace('    public string TestCat => _cat;',
`    /// <summary>左栏里所有「可点图形」的世界矩形。
    /// 用来看器材架按钮有没有被容器卡片盖住之类的问题。 </summary>
    public List<(string name, Rect2 r)> TestPanelClickables() {
        var res = new List<(string, Rect2)>();
        var panel = _canvas.transform.Find("LeftPanel") as RectTransform;
        if (panel == null) return res;
        Canvas.ForceUpdateCanvases();
        foreach (var g in panel.GetComponentsInChildren<Graphic>(true)) {
            if (g == null || !g.raycastTarget) continue;
            if (!g.gameObject.activeInHierarchy) continue;
            var rt = g.rectTransform;
            if (rt == null) continue;
            var a = rt.TransformPoint(rt.rect.min);
            var b = rt.TransformPoint(rt.rect.max);
            res.Add((Path(g.transform), new Rect2 {
                xMin = Mathf.Min(a.x, b.x), xMax = Mathf.Max(a.x, b.x),
                yMin = Mathf.Min(a.y, b.y), yMax = Mathf.Max(a.y, b.y),
            }));
        }
        return res;
    }

    public string TestCat => _cat;`);
  fs.writeFileSync(U, u, 'utf8'); n++;
  console.log('  ✓ TestPanelClickables 已加');
}

console.log('改动处数:', n);
