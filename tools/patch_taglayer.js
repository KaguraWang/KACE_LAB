const fs = require('fs');
let n = 0;
const P = (p, a, b, label) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 40)); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ============ 1) LabBootstrap：建标签层，标签挂到它下面 ============ */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
P(B, `        if (_tagRoot == null) {
            var tg = new GameObject("Tags");
            tg.transform.SetParent(transform, false);
            _tagRoot = tg.transform;
        }`,
`        /* 标签层：一块 ScreenSpaceOverlay 画布承载所有悬浮标签。
           用世界空间画布的话，多个标签之间的遮挡关系完全不可控。 */
        if (TagLayer.Canvas == null) {
            var tg = new GameObject("TagLayer");
            tg.transform.SetParent(transform, false);
            TagLayer.Canvas = tg.AddComponent<Canvas>();
            TagLayer.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            TagLayer.Canvas.sortingOrder = 5;
            var sc = tg.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(LabUI.DesignW, LabUI.DesignH);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 0.5f;
            var cg = tg.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            cg.enabled = false;                       // 标签不接收点击
            TagLayer.Root = (RectTransform)tg.transform;
            _tagRoot = TagLayer.Root;
        }`);

/* ============ 2) VesselTag：改成 Overlay 定位 ============ */
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
P(V, `        var cgo = new GameObject("Canvas");
        cgo.transform.SetParent(go.transform, false);
        t._canvas = cgo.AddComponent<Canvas>();
        t._canvas.renderMode = RenderMode.WorldSpace;
        t._canvas.sortingOrder = 10;   /* 低于方程式标签，让方程式永远看得见 */
        var crt = (RectTransform)cgo.transform;
        crt.sizeDelta = new Vector2(PanelW, 100f);
        crt.localScale = Vector3.one * (1f / CanvasUnitsPerWorld);
        cgo.layer = LabBootstrap.UiLayerConst;`,
`        /* 直接挂到共用的标签层下（不再各自建世界空间画布） */
        var cgo = new GameObject("Canvas");
        cgo.transform.SetParent(go.transform, false);
        t._canvas = cgo.AddComponent<Canvas>();
        t._canvas.overrideSorting = true;             // 让每个标签能独立排层
        t._canvas.sortingOrder = 10;                  // 低于方程式标签
        var crt = (RectTransform)cgo.transform;
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(PanelW, 100f);`);

P(V, `        var cgo = new GameObject("Canvas");
        cgo.transform.SetParent(go.transform, false);`,
`        var cgo = new GameObject("Canvas");
        cgo.transform.SetParent(go.transform, false);`);

/* Sync 里的定位改成投影 */
P(V, `        /* ---- 位置：容器上方 ---- */
        var g = VesselMesh.Get(Model.Kind);
        Vector3 basePt = Model.Pos + Vector3.up * (Model.LiftY + g.H + 0.10f);   // 连线起点（容器口）
        float worldH = h / CanvasUnitsPerWorld;
        Vector3 center = basePt + Vector3.up * (worldH * 0.5f + 0.055f);        // 面板中心
        transform.position = center;

        /* ---- 永远面朝镜头 ---- */
        if (_cam == null) _cam = Camera.main;
        if (_cam != null) transform.rotation = _cam.transform.rotation;`,
`        /* ---- 位置：投影到屏幕（Overlay 画布，天然正对镜头） ---- */
        var g = VesselMesh.Get(Model.Kind);
        Vector3 basePt = Model.Pos + Vector3.up * (Model.LiftY + g.H + 0.10f);   // 容器口
        if (_cam == null) _cam = Camera.main;
        bool vis = TagLayer.Visible(basePt, _cam);
        foreach (var ch in GetComponentsInChildren<Renderer>(true)) ch.enabled = vis;
        if (!vis) return;
        /* 面板整体比容器口高一点；高度就是画布本身的高度（屏幕像素） */
        Vector2 scrBase = TagLayer.Project(basePt, _cam);
        Vector2 scrTop  = TagLayer.Project(basePt + Vector3.up * ((h / CanvasUnitsPerWorld) + 0.10f), _cam);
        float pxH = Mathf.Abs(scrTop.y - scrBase.y);
        _panel.anchoredPosition = scrBase + new Vector2(0f, pxH * 0.5f + 6f);`);

/* 连线改到屏幕空间 */
P(V, `        /* ---- 白线：连到容器口 ---- */`,
`        /* ---- 白线：连到容器口 ---- */`);

P(V, `        float h = 6 + 28 + 4 + Mathf.Max(1, lines.Count) * RowH + 8;
        var crt = (RectTransform)_canvas.transform;
        crt.sizeDelta = new Vector2(PanelW, h);
        _panel = crt;`,
`        float h = 6 + 28 + 4 + Mathf.Max(1, lines.Count) * RowH + 8;
        _panel = (RectTransform)_canvas.transform;
        _panel.sizeDelta = new Vector2(PanelW, h);`);

fs.writeFileSync(V, fs.readFileSync(V, 'utf8').replace(
`        Vector3 lp = basePt;
        if (_cam != null) {`, `        Vector3 lp = basePt;
        if (_cam != null) {`));
console.log('补丁处数:', n);
