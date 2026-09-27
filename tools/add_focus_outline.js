const fs = require('fs');
const P = 'D:/ChemLabUnity/Assets/Scripts/Game/OutlineFx.cs';
let c = fs.readFileSync(P, 'utf8');
let n = 0;

/* 1) 记录「源 → 壳」配对，每帧同步网格 —— 器皿的网格是每帧重建的，
      外壳如果只在 Attach 时复制一次，形状就会过期。 */
c = c.replace('    readonly List<MeshRenderer> _shells = new List<MeshRenderer>();',
`    readonly List<MeshRenderer> _shells = new List<MeshRenderer>();
    /* 源 → 壳 的配对：器皿的网格每帧重建，外壳必须跟着换网格，
       否则描边会停留在第一次复制时的形状。 */
    readonly List<(MeshFilter src, MeshFilter dst, Renderer sr, Renderer dr, Transform st, Transform dt)> _pairs
        = new List<(MeshFilter, MeshFilter, Renderer, Renderer, Transform, Transform)>();`);
n++;

c = c.replace('        _root.gameObject.SetActive(false);',
`        _root.gameObject.SetActive(false);
        _pairs.Clear();`);
n++;

c = c.replace('        _shells.Add(mr);\n    }\n\n    void BuildShellSkinned',
`        _shells.Add(mr);
        _pairs.Add((src, mf, src.GetComponent<Renderer>(), mr, src.transform, go.transform));
    }

    void BuildShellSkinned`);
n++;

/* 2) LateUpdate 里同步网格与变换 */
c = c.replace('    public void SetOn(bool on) {',
`    void LateUpdate() {
        if (!_on) return;
        for (int i = 0; i < _pairs.Count; i++) {
            var p = _pairs[i];
            if (p.src == null || p.dst == null) continue;
            /* 网格换了就换过去（器皿的液体/玻璃网格是动态生成的） */
            if (p.dst.sharedMesh != p.src.sharedMesh) p.dst.sharedMesh = p.src.sharedMesh;
            /* 变换也跟着走 —— 源物体的局部变换可能被重建过程改掉 */
            if (p.dt != null && p.st != null) {
                p.dt.localPosition = transform.InverseTransformPoint(p.st.position);
                p.dt.localRotation = Quaternion.Inverse(transform.rotation) * p.st.rotation;
                p.dt.localScale = p.st.lossyScale * 1.22f;
                var rd = p.dr as MeshRenderer;
                if (rd != null && p.sr != null) rd.enabled = p.sr.enabled;
            }
        }
    }

    public void SetOn(bool on) {`);
n++;

fs.writeFileSync(P, c, 'utf8');
console.log('OutlineFx 改动处数:', n);

/* 3) 加回聚焦：点任意物体聚焦 + 描边；点空白取消 */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('_focused')) {
  b = b.replace('    void HandleInput() {\n        /* 计算器开着时实验台不接收输入（那边有自己的交互） */\n        if (CalculatorActive) return;',
`    /* ================================================================
       聚焦与轮廓高亮
         · 左键点任何东西（器皿 / 器材 / 检验道具）→ 聚焦它并高亮轮廓
         · 点空白处 → 取消聚焦、取消高亮
         · 拖动中的器皿不算聚焦，松手后若几乎没动才当点击
       ================================================================ */
    object _focused;                       // 当前聚焦的对象（Vessel / Apparatus / TestItem）
    OutlineFx _focusOutline;

    /// <summary>设聚焦对象；传 null 表示取消聚焦</summary>
    public void SetFocus(object target) {
        if (ReferenceEquals(_focused, target)) return;
        if (_focusOutline != null) { _focusOutline.SetOn(false); _focusOutline = null; }
        _focused = target;
        if (target == null) return;

        Transform tr = null;
        if (target is Vessel v) {
            var vv = _views.Find(x => x != null && x.Model == v);
            tr = vv != null ? vv.transform : null;
        } else if (target is Apparatus a) {
            var av = _apViews.Find(x => x != null && x.Model == a);
            tr = av != null ? av.transform : null;
        } else if (target is TestItem t) {
            var tv = _testViews.Find(x => x != null && x.Model == t);
            tr = tv != null ? tv.transform : null;
        }
        if (tr == null) return;
        _focusOutline = OutlineFx.Attach(tr, new Color(0.35f, 0.85f, 1f), 0.02f);
        if (_focusOutline != null) _focusOutline.SetOn(true);
    }
    public object Focused => _focused;

    void HandleInput() {
        /* 计算器开着时实验台不接收输入（那边有自己的交互） */
        if (CalculatorActive) return;`);
  n++;
  console.log('  ✓ 聚焦与描边已加');
}

/* 4) 点击时设置聚焦 */
b = fs.readFileSync(B, 'utf8');
if (!b.includes('_clickStart') ) {
  b = b.replace('        if (Input.GetMouseButtonDown(0)) {\n            if (overUI) { _orbiting = false; return; }\n            var v = PickVessel(m);',
`        if (Input.GetMouseButtonDown(0)) {
            _clickStart = m; _clickTime = Time.unscaledTimeAsDouble;
            if (overUI) { _orbiting = false; return; }
            var v = PickVessel(m);`);
  b = b.replace('        if (Input.GetMouseButtonUp(0)) {\n            if (_dragging != null) CommitSnap(_dragging);\n            _dragging = null; _orbiting = false;\n            ClearHighlights();\n        }',
`        if (Input.GetMouseButtonUp(0)) {
            if (_dragging != null) CommitSnap(_dragging);
            _dragging = null; _orbiting = false;
            ClearHighlights();
            /* 几乎没动、按得也短 → 算一次点击：点中东西就聚焦它，点空白就取消聚焦 */
            if (!overUI && Vector2.Distance(m, _clickStart) < 6f
                && Time.unscaledTimeAsDouble - _clickTime < 0.45) {
                var hv = PickVessel(m);
                if (hv != null) SetFocus(hv);
                else if (PickAnyApparatus(m, out var ha)) SetFocus(ha);
                else if (PickAnyTestItem(m, out var ht)) SetFocus(ht);
                else SetFocus(null);
            }
        }`);
  b = b.replace('    Vector3 _grabOffset;', '    Vector3 _grabOffset;\n    Vector2 _clickStart;\n    double _clickTime;');
  n++;
  console.log('  ✓ 点击设置聚焦');
}

/* 5) 器材与道具的点选 */
if (!b.includes('bool PickAnyApparatus')) {
  b = b.replace('    Vector3 PickPlane(Vector3 screen) {',
`    bool PickAnyApparatus(Vector3 screen, out Apparatus found) {
        found = null; float best = float.MaxValue;
        foreach (var a in ApparatusSet.All) {
            var sc = Cam.WorldToScreenPoint(a.Pos + Vector3.up * 0.14f);
            if (sc.z <= 0) continue;
            float d = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(sc.x, sc.y));
            if (d < Screen.height * 0.09f && d < best) { best = d; found = a; }
        }
        return found != null;
    }

    bool PickAnyTestItem(Vector3 screen, out TestItem found) {
        found = null; float best = float.MaxValue;
        foreach (var t in TestSet.All) {
            var sc = Cam.WorldToScreenPoint(t.Pos + Vector3.up * (t.LiftY + 0.08f));
            if (sc.z <= 0) continue;
            float d = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(sc.x, sc.y));
            if (d < Screen.height * 0.07f && d < best) { best = d; found = t; }
        }
        return found != null;
    }

    Vector3 PickPlane(Vector3 screen) {`);
  n++;
  console.log('  ✓ 器材/道具点选已加');
}

fs.writeFileSync(B, b, 'utf8');
console.log('总改动处数:', n);
