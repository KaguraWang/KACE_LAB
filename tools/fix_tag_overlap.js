const fs = require('fs');
let n = 0;

/* ============ 1) VesselTag 加 ExtraLift + 面板信息查询 ============ */
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
let v = fs.readFileSync(V, 'utf8');

if (!v.includes('public float ExtraLift')) {
  v = v.replace('    public int Slot;',
`    public int Slot;
    /// <summary>被重叠消解顶起来的额外世界高度（由 LabBootstrap 每帧写）</summary>
    public float ExtraLift;

    /// <summary>面板在屏幕上的矩形，供重叠消解用</summary>
    public Rect ScreenRect {
        get {
            if (_panel == null || _cam == null) return new Rect(0, 0, 0, 0);
            var c = _panel.transform.position;
            var sc = _cam.WorldToScreenPoint(c);
            if (sc.z <= 0.01f) return new Rect(0, 0, 0, 0);
            /* 面板的世界尺寸换算成屏幕像素 */
            float perWorld = 1f;
            var p0 = _cam.WorldToScreenPoint(c);
            var p1 = _cam.WorldToScreenPoint(c + _cam.transform.up * 0.10f);
            perWorld = Vector3.Distance(p0, p1) / 0.10f;
            float wpx = PanelW / CanvasUnitsPerWorld * perWorld;
            float hpx = _panel.rect.height / CanvasUnitsPerWorld * perWorld;
            return new Rect(sc.x - wpx * 0.5f, sc.y - hpx * 0.5f, wpx, hpx);
        }
    }

    /// <summary>每世界单位对应多少屏幕像素（消解时把像素位移换回世界单位）</summary>
    public float PixelsPerWorld {
        get {
            if (_cam == null) return 100f;
            var o = transform.position;
            var a = _cam.WorldToScreenPoint(o);
            var b = _cam.WorldToScreenPoint(o + Vector3.up * 0.10f);
            return Mathf.Max(1f, Vector3.Distance(a, b) / 0.10f);
        }
    }`);
  fs.writeFileSync(V, v, 'utf8'); n++;
  console.log('  ✓ ExtraLift / ScreenRect / PixelsPerWorld 已加');
}

/* 定位时把 ExtraLift 算进去 */
v = fs.readFileSync(V, 'utf8');
const oldA = 'Vector3 anchor = basePt + Vector3.up * (Slot * (worldH + 0.07f) + worldH * 0.5f + 0.06f);';
const newA = 'Vector3 anchor = basePt + Vector3.up * (Slot * (worldH + 0.07f) + worldH * 0.5f + 0.06f + ExtraLift);';
if (v.includes(oldA)) { v = v.replace(oldA, newA); fs.writeFileSync(V, v, 'utf8'); n++; console.log('  ✓ 定位计入 ExtraLift'); }
else console.log('  ✗ 定位锚点');

/* ============ 2) LabBootstrap 里做重叠消解 ============ */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('ResolveTagOverlaps')) {
  b = b.replace('            foreach (var tg in _tags) if (tg != null) tg.Sync();',
`            foreach (var tg in _tags) if (tg != null) tg.Sync();
            ResolveTagOverlaps();`);
  b = b.replace('    void DumpInputAudit() {',
`    /// <summary>标签之间不许压在一起。
    /// 器皿在世界里离得远，可标签本身很宽（0.92 世界单位），
    /// 屏幕上照样会叠上 —— 所以定位完之后按屏幕矩形做一次「往上顶」的消解。
    /// 每个标签最多被顶几次，避免来回振荡。</summary>
    void ResolveTagOverlaps() {
        var live = new List<VesselTag>();
        foreach (var t in _tags) if (t != null && t.gameObject.activeSelf) live.Add(t);
        if (live.Count < 2) { foreach (var t in live) t.ExtraLift = 0f; return; }

        /* 按屏幕 X 排序，从左往右处理，结果稳定 */
        live.Sort((x, y) => x.ScreenRect.center.x.CompareTo(y.ScreenRect.center.x));
        for (int i = 0; i < live.Count; i++) live[i].ExtraLift = 0f;

        for (int pass = 0; pass < 6; pass++) {
            bool any = false;
            for (int i = 0; i < live.Count; i++) {
                for (int j = i + 1; j < live.Count; j++) {
                    var ra = live[i].ScreenRect;
                    var rb = live[j].ScreenRect;
                    if (ra.width <= 0f || rb.width <= 0f) continue;
                    /* 留 6px 间隙 */
                    if (ra.xMin < rb.xMax + 6f && rb.xMin < ra.xMax + 6f
                        && ra.yMin < rb.yMax + 6f && rb.yMin < ra.yMax + 6f) {
                        /* 把屏幕位置低的那个往下、高的往上？不行 ——
                           统一往上顶更直观：把 j 抬高到 i 之上。 */
                        float need = (ra.yMax + 6f) - rb.yMin;      // 像素
                        if (need > 0f) {
                            live[j].ExtraLift += need / Mathf.Max(1f, live[j].PixelsPerWorld);
                            any = true;
                        }
                    }
                }
            }
            if (!any) break;
            /* 重新定位一轮再判 */
            foreach (var t in live) t.Sync();
        }
    }

    void DumpInputAudit() {`);
  fs.writeFileSync(B, b, 'utf8');
  n++;
  console.log('  ✓ ResolveTagOverlaps 已加');
}

console.log('改动处数:', n);
const cb = fs.readFileSync(B, 'utf8');
console.log('  LabBootstrap 大括号差:', (cb.match(/\{/g) || []).length - (cb.match(/\}/g) || []).length);
const cv = fs.readFileSync(V, 'utf8');
console.log('  VesselTag 大括号差:', (cv.match(/\{/g) || []).length - (cv.match(/\}/g) || []).length);
