const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let c = fs.readFileSync(p, 'utf8');
if (c.includes('方程式格式化')) { console.log('已存在'); process.exit(0); }

const anchor = `            /* ---------- 爆鸣音：必须真的有声音输出 ---------- */`;
const block = `            /* ================================================================
               反应方程式的规范化输出
                 中文：无机用「=」、有机用「→」、可逆用「⇌」，条件在等式上方，
                       气体标 ↑、沉淀标 ↓，**不标状态符号**
                 英文：一律「→」、可逆「⇌」，条件在箭头上方，**必须标 (s)/(l)/(g)/(aq)**
               最强的一条验证是：格式化前后**元素与电荷都守恒**，
               离子方程式消去旁观离子后同样守恒。
               ================================================================ */
            {
                Rules.Init();
                int shown = 0;
                foreach (var r in Rules.All) {
                    if (!r.Parsed || shown >= 6) break;
                    if (r.Reactants.Count == 0 || r.Products.Count == 0) continue;
                    string cn = EqFormat.FormatFull(r, false, true);
                    string en = EqFormat.FormatFull(r, true, true);
                    Say($"   中：{cn}");
                    Say($"   英：{en}");

                    /* ---- 连接符规范 ---- */
                    bool rev = r.Reversible;
                    string cnConn = rev ? "⇌" : (EqFormat.IsOrganic(r) ? "→" : "=");
                    TCheck($"中文连接符正确（{(rev ? "可逆" : EqFormat.IsOrganic(r) ? "有机" : "无机")}）",
                           cn.Contains(" " + cnConn + " "));
                    TCheck($"英文一律用 → 或 ⇌", en.Contains(" → ") || en.Contains(" ⇌ "));
                    TCheck($"可逆反应中英都用 ⇌",
                           !rev || (cn.Contains(" ⇌ ") && en.Contains(" ⇌ ")));
                    /* ---- 状态符号只在英文出现 ---- */
                    TCheck($"英文标了状态符号（{r.Products[0].Id}）", en.Contains("("));
                    TCheck($"中文不标状态符号", !cn.Contains("(aq)") && !cn.Contains("(s)") && !cn.Contains("(g)"));
                    /* ---- 条件 —— 有条件的必须能翻出来 ---- */
                    string cl = EqFormat.CondLabel(r.Cond, false);
                    string ce = EqFormat.CondLabel(r.Cond, true);
                    if (!string.IsNullOrEmpty(cl)) {
                        TCheck($"条件已译出（{r.Cond} → {cl} / {ce}）", ce.Length > 0);
                    }

                    /* ---- 元素守恒：把格式化后的文本拆回去数原子 ---- */
                    bool okCn = AtomsConserved(cn, out string dCn);
                    if (!okCn) Say("      中文拆解：" + dCn);
                    TCheck("格式化后元素守恒（中文）", okCn);

                    /* ---- 离子方程式：写出后元素与电荷都要守恒 ---- */
                    string ion = EqFormat.IonicEquation(r, true, true);
                    if (!string.IsNullOrEmpty(ion)) {
                        Say($"   离子式：{ion}");
                        TCheck("离子式元素守恒", AtomsConserved(ion, out _));
                        TCheck("离子式电荷守恒", ChargeConserved(ion, out string dq), dq);
                        TCheck("离子式已消去旁观离子（不含未变化的离子对）", !SpectatorLeft(r, ion));
                    }
                    shown++;
                }
            }

`
if (!c.includes(anchor)) { console.log('✗ 锚点'); process.exit(1); }
c = c.replace(anchor, block + anchor);

/* 辅助函数 */
const helper = `        /// <summary>把方程式文本拆回物种，核对两边的原子总数</summary>
        static bool AtomsConserved(string eq, out string detail) {
            detail = "";
            string conn = eq.Contains(" ⇌ ") ? " ⇌ " : eq.Contains(" = ") ? " = " : " → ";
            int k = eq.IndexOf(conn);
            if (k < 0) { detail = "找不到连接符"; return false; }
            var L = EqFormat.ParseSide(eq.Substring(0, k));
            var R = EqFormat.ParseSide(eq.Substring(k + conn.Length));
            if (L.Count == 0 || R.Count == 0) { detail = "拆解为空"; return false; }
            var a = CountAtoms2(L); var b = CountAtoms2(R);
            var els = new System.Collections.Generic.HashSet<string>(a.Keys);
            foreach (var x in b.Keys) els.Add(x);
            var bad = new System.Collections.Generic.List<string>();
            foreach (var e in els) {
                double va, vb; a.TryGetValue(e, out va); b.TryGetValue(e, out vb);
                if (Math.Abs(va - vb) > 1e-6 * Math.Max(1.0, Math.Max(va, vb))) bad.Add($"{e}:{va:F4}≠{vb:F4}");
            }
            detail = bad.Count == 0 ? "" : string.Join(", ", bad);
            return bad.Count == 0;
        }
        static System.Collections.Generic.Dictionary<string, double> CountAtoms2(System.Collections.Generic.List<Rules.Species> list) {
            var tot = new System.Collections.Generic.Dictionary<string, double>();
            foreach (var sp in list) {
                ChemData.Sub.TryGetValue(sp.Id, out var s);
                if (s == null) continue;
                foreach (var kv in ChemLab.Chem.Formula.Parse(s.Formula)) {
                    double cur; tot.TryGetValue(kv.Key, out cur);
                    tot[kv.Key] = cur + kv.Value * sp.Coef;
                }
            }
            return tot;
        }
        /// <summary>离子方程式的两边电荷必须相等</summary>
        static bool ChargeConserved(string eq, out string detail) {
            string conn = eq.Contains(" ⇌ ") ? " ⇌ " : eq.Contains(" = ") ? " = " : " → ";
            int k = eq.IndexOf(conn);
            var L = EqFormat.ParseSide(eq.Substring(0, k));
            var R = EqFormat.ParseSide(eq.Substring(k + conn.Length));
            double cl = 0, cr = 0;
            foreach (var sp in L) cl += Charge(sp) * sp.Coef;
            foreach (var sp in R) cr += Charge(sp) * sp.Coef;
            detail = $"左 {cl} / 右 {cr}";
            return Math.Abs(cl - cr) < 1e-6;
        }
        static double Charge(Rules.Species sp) {
            ChemData.Sub.TryGetValue(sp.Id, out var s);
            if (s == null) return 0;
            string f = s.Formula;
            int caret = f.IndexOf('^');
            if (caret >= 0) {
                string ch = f.Substring(caret + 1);
                double n = 1; int d = 0;
                while (d < ch.Length && char.IsDigit(ch[d])) d++;
                if (d > 0) double.TryParse(ch.Substring(0, d), out n);
                return ch.EndsWith("-") ? -n : n;
            }
            var m = System.Text.RegularExpressions.Regex.Match(f, @"(\\d*)([+-])$");
            if (!m.Success) return 0;
            double num = 1;
            if (m.Groups[1].Value.Length > 0) double.TryParse(m.Groups[1].Value, out num);
            return m.Groups[2].Value == "-" ? -num : num;
        }
        /// <summary>离子式里不应再出现「两边同量出现的离子」</summary>
        static bool SpectatorLeft(Rules.Rule r, string ion) {
            var net = new System.Collections.Generic.Dictionary<string, double>();
            foreach (var sp in r.Reactants) { double a; net.TryGetValue(sp.Id, out a); net[sp.Id] = a - sp.Coef; }
            foreach (var sp in r.Products)  { double a; net.TryGetValue(sp.Id, out a); net[sp.Id] = a + sp.Coef; }
            foreach (var kv in net) {
                if (Math.Abs(kv.Value) > 1e-9) continue;      // 不是旁观离子
                ChemData.Sub.TryGetValue(kv.Key, out var s);
                if (s == null || !s.IsIon) continue;
                string pretty = EqFormat.Pretty(s.Formula);
                if (ion.Contains(pretty)) return true;         // 旁观离子还留在式子里
            }
            return false;
        }
        static bool Charge(int _) => false;

`
c = c.replace('        /// <summary>两种视图的原子总数必须一致', helper + '        /// <summary>两种视图的原子总数必须一致');
fs.writeFileSync(p, c, 'utf8');
console.log('✓ 方程式格式化自检已加入');
