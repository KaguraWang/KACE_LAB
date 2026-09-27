const fs = require('fs');
let n = 0;
const P = (p, a, b, label) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 46).replace(/\n/g,'⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ============ A) LabBootstrap：标签层初始化改用 TagLayer.Ensure ============ */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
P(B, `        if (_tagRoot == null) {
            var tg = new GameObject("Tags");
            tg.transform.SetParent(transform, false);
            _tagRoot = tg.transform;
        }`,
`        /* 所有悬浮标签共用一块 ScreenSpaceOverlay 画布 */
        _tagRoot = TagLayer.Ensure(transform);`);

/* ============ B) VesselTag：改成 Overlay + 世界坐标投影 ============ */
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
`        /* 直接挂在共用的标签层下；用 overrideSorting 让每个标签能独立排层 */
        var cgo = new GameObject("Canvas", typeof(RectTransform));
        cgo.transform.SetParent(go.transform, false);
        t._canvas = cgo.AddComponent<Canvas>();
        t._canvas.overrideSorting = true;
        t._canvas.sortingOrder = 10;               // 低于方程式标签
        var crt = (RectTransform)cgo.transform;
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(PanelW, 100f);`);

P(V, `        /* ---- 位置：容器上方 ---- */
        var g = VesselMesh.Get(Model.Kind);
        Vector3 basePt = Model.Pos + Vector3.up * (Model.LiftY + g.H + 0.10f);   // 连线起点（容器口）
        float worldH = h / CanvasUnitsPerWorld;
        Vector3 center = basePt + Vector3.up * (worldH * 0.5f + 0.055f);        // 面板中心（贴近容器，避免被画面上缘裁掉）
        /* 按槽位抬高，避免相邻容器的标签重叠 */
        center += Vector3.up * (Slot * 0.30f);
        transform.position = center;

        /* ---- 永远面朝镜头 ---- */
        if (_cam == null) _cam = Camera.main;
        if (_cam != null) transform.rotation = _cam.transform.rotation;

        /* ---- 白线：从面板底边连到容器口 ---- */
        bool showLine = TagSettings.ShowLine;
        if (_line != null) {
            _line.enabled = showLine;
            if (showLine) {
                Vector3 bottom = center - Vector3.up * (worldH * 0.5f);
                _line.SetPosition(0, bottom);
                _line.SetPosition(1, basePt);
            }
        }`,
`        /* ---- 位置：把世界坐标投影到屏幕（Overlay 画布天然正对镜头） ---- */
        var g = VesselMesh.Get(Model.Kind);
        Vector3 basePt = Model.Pos + Vector3.up * (Model.LiftY + g.H + 0.10f);   // 容器口
        if (_cam == null) _cam = Camera.main;
        bool vis = TagLayer.Visible(basePt, _cam);
        if (_canvas != null) _canvas.enabled = vis;
        if (!vis) return;

        /* 容器口与面板底边是两个世界点，投影到屏幕后直接得到像素高度差 */
        Vector2 pBottom = TagLayer.Project(basePt + Vector3.up * (Slot * 0.30f), _cam);
        Vector2 pTop    = TagLayer.Project(basePt + Vector3.up * (Slot * 0.30f + 0.06f), _cam);
        float perWorld = Mathf.Max(1f, Mathf.Abs(pTop.y - pBottom.y) / 0.06f);   // 每世界单位多少像素
        float pxH = h * perWorld / CanvasUnitsPerWorld * 0.42f;                  // 面板的屏幕高度
        _panel.anchoredPosition = pBottom + new Vector2(0f, pxH * 0.5f + 8f);

        /* ---- 白线：从面板底边连到容器口 ---- */
        bool showLine = TagSettings.ShowLine;
        if (_line != null) {
            _line.enabled = showLine;
            if (showLine) {
                Vector3 bottomWorld = basePt + Vector3.up * (Slot * 0.30f + (pxH * 0.5f) / perWorld);
                _line.SetPosition(0, bottomWorld);
                _line.SetPosition(1, basePt);
            }
        }`);

/* 面板尺寸要在投影之前算好 */
P(V, `        float h = 6 + 28 + 4 + Mathf.Max(1, lines.Count) * RowH + 8;
        var crt = (RectTransform)_canvas.transform;
        crt.sizeDelta = new Vector2(PanelW, h);
        _panel = crt;`,
`        float h = 6 + 28 + 4 + Mathf.Max(1, lines.Count) * RowH + 8;
        _panel = (RectTransform)_canvas.transform;
        _panel.sizeDelta = new Vector2(PanelW, h);`);

fs.writeFileSync(V, fs.readFileSync(V, 'utf8'));
console.log('补丁处数:', n);
