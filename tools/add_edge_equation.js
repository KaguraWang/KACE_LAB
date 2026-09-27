const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(p, 'utf8');
if (c.includes('EdgeEquation')) { console.log('已存在'); process.exit(0); }

/* 1) 连线标注改成两行：第一行规范化反应式，第二行 ΔH */
const old = `            /* 焓变标注 */
            string label = e.Source == EnthalpySource.Unknown ? "ΔH = ?"
                         : $"ΔH = {e.DH:F0} kJ/mol";
            var t = EnsureLabel(e);
            t.text = label;
            var trt = (RectTransform)t.transform;
            trt.anchoredPosition = Quadratic(p0, ctrl, p1, 0.5f) + new Vector2(0, 16);
            t.color = e.Source == EnthalpySource.Unknown ? new Color(1f, 0.8f, 0.35f)
                                                         : new Color(0.85f, 0.94f, 1f);`;

const nw = `            /* 标注两行：规范化反应式 + 焓变值。
               方程式用 EqFormat 走一遍，上下标与离子电荷才正确
               （CO2 → CO₂、H+ → H⁺、SO4^2- → SO₄²⁻）。 */
            var t = EnsureLabel(e);
            string eq = EdgeEquation(e);
            string dh = e.Source == EnthalpySource.Unknown ? "ΔH = ?"
                      : $"ΔH = {e.DH:F0} kJ/mol";
            t.text = eq.Length > 0 ? eq + "\\n" + dh : dh;
            t.fontSize = 13;
            var trt = (RectTransform)t.transform;
            trt.anchoredPosition = Quadratic(p0, ctrl, p1, 0.5f) + new Vector2(0, 22);
            t.color = e.Source == EnthalpySource.Unknown ? new Color(1f, 0.8f, 0.35f)
                                                         : new Color(0.85f, 0.94f, 1f);`;

if (!c.includes(old)) { console.log('✗ 标注锚点'); process.exit(1); }
c = c.replace(old, nw);

/* 2) 加 EdgeEquation 方法 */
const anchor2 = `    /* ---------------- 手动输入数值对话框 ---------------- */`;
const add = `    /// <summary>把这条连线两端的节点写成一条规范化化学方程式。
    /// 离子会显示成 H⁺ / SO₄²⁻ 这种正确形式，系数也带上。</summary>
    public string EdgeEquation(Edge e) {
        if (e.A == null || e.B == null) return "";
        string L = Term(e.A), R = Term(e.B);
        if (L.Length == 0 || R.Length == 0) return "";
        return L + " → " + R;
    }
    string Term(Node n) {
        string f = n.Formula.Length > 0 ? n.Formula : n.Raw;
        if (f.Length == 0) return "";
        /* 规范化：优先用匹配到的物种分子式，再走 EqFormat 做上下标 */
        string raw = n.SpeciesId.Length > 0 ? FormulaOf(n.SpeciesId) : f;
        string pretty = EqFormat.Pretty(raw);
        if (n.Coef == 1) return pretty;
        return (n.Coef == System.Math.Floor(n.Coef) ? ((int)n.Coef).ToString() : n.Coef.ToString("0.#")) + pretty;
    }
    static string FormulaOf(string id) {
        ChemData.Sub.TryGetValue(id, out var s);
        return s != null ? s.Formula : id;
    }

    /* ---------------- 手动输入数值对话框 ---------------- */`;
if (!c.includes(anchor2)) { console.log('✗ 方法锚点'); process.exit(1); }
c = c.replace(anchor2, add);

fs.writeFileSync(p, c, 'utf8');
console.log('✓ 连线标注改为显示规范化反应式');
