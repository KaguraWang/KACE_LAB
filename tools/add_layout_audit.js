const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(p, 'utf8');
if (c.includes('AuditLayout')) { console.log('已存在'); process.exit(0); }

/* 在 Back() 之前插入审计方法 */
const anchor = '    void Back() { gameObject.SetActive(false); }';
const add = `    void Back() { gameObject.SetActive(false); }

    /* ================================================================
       布局审计：把所有控件的屏幕矩形打出来。
       用它来判断按钮到底在不在画面里、有没有互相压住 ——
       比对着截图猜可靠得多。
       ================================================================ */
    public string AuditLayout() {
        var sb = new System.Text.StringBuilder();
        var canvasRt = (RectTransform)transform;
        sb.AppendLine($"[CalcAudit] 画布 {canvasRt.rect.width:F0}x{canvasRt.rect.height:F0}  屏幕 {Screen.width}x{Screen.height}");
        int bad = 0;
        foreach (var rt in GetComponentsInChildren<RectTransform>(true)) {
            if (rt == canvasRt) continue;
            var t = rt.GetComponent<Text>();
            var img = rt.GetComponent<Image>();
            string label = t != null && !string.IsNullOrEmpty(t.text) ? t.text
                         : (img != null ? "[图]" : "");
            if (label.Length == 0) continue;
            if (rt.name == "T" || rt.name == "BG" || rt.name == "Seg" || rt.name == "Head") continue;
            /* 世界坐标（ScreenSpaceOverlay 下就是屏幕像素） */
            var c0 = rt.TransformPoint(rt.rect.min);
            var c1 = rt.TransformPoint(rt.rect.max);
            float x0 = Mathf.Min(c0.x, c1.x), x1 = Mathf.Max(c0.x, c1.x);
            float y0 = Mathf.Min(c0.y, c1.y), y1 = Mathf.Max(c0.y, c1.y);
            bool offscreen = x1 < 0 || y1 < 0 || x0 > Screen.width || y0 > Screen.height;
            if (offscreen) bad++;
            sb.AppendLine($"[CalcAudit] {rt.name,-12} 「{Trim(label),-16}」 x {x0:F0}..{x1:F0}  y {y0:F0}..{y1:F0}"
                        + (offscreen ? "   ← 在画面外！" : ""));
        }
        sb.AppendLine($"[CalcAudit] 画面外的控件数 = {bad}");
        return sb.ToString();
    }
    static string Trim(string s) => s.Length > 14 ? s.Substring(0, 14) : s;
`;
if (!c.includes(anchor)) { console.log('✗ 锚点'); process.exit(1); }
c = c.replace(anchor, add);
fs.writeFileSync(p, c, 'utf8');
console.log('✓ AuditLayout 已加');
