/* =====================================================================
   SUT 接口：读 JSON 用例 → 驱动化学引擎 → 写 JSON 结果
   命令行：
     -executeMethod ChemLab.Editor.SpecRunner.Run -specIn=<cases.json> -specOut=<results.json>
   用例格式见 caie/spec/case_schema.md
   ===================================================================== */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
using ChemLab.Spec;
using ChemLab.Game;

namespace ChemLab.Editor {

public static class SpecRunner {

    /* ================= 方程式守恒校验 ================= */

    /// <summary>解析一个物种：系数 + 原子组成 + 电荷 + 电子数</summary>
    public class Species {
        public int Coef = 1;
        public Dictionary<string,int> Atoms = new Dictionary<string,int>();
        public int Charge;
        public int Electrons;
        public string Raw = "";
    }

    public static Species ParseTerm(string raw) {
        var sp = new Species { Raw = raw };
        string s = raw.Trim();
        if (s.Length == 0) return sp;

        /* 先剥前导系数。注意 '2H2O' 的 2 是系数，'H2O' 的 2 不是 */
        int k = 0;
        while (k < s.Length && char.IsDigit(s[k])) k++;
        if (k > 0) {
            sp.Coef = int.Parse(s.Substring(0, k));
            s = s.Substring(k);
        }

        /* 电子 */
        if (s == "e" || s == "e-" || s == "e^-") { sp.Electrons = 1; sp.Charge = -1; return sp; }

        /* 电荷识别。
           不能用贪婪正则 (\d*)([+-])$ —— 它会把 MnO4- 读成 MnO + 电荷 -4。
           也不能一律把数字当电荷 —— 那会把 NH4+ 读成 NH + 电荷 +4。
           判据：把结尾的数字剥掉后，若剩下的是**单个元素符号**，数字就是电荷；
                 否则数字是下标、电荷为 ±1。
             Fe2+ → "Fe" 是元素 → 电荷 +2       ✅
             NH4+ → "NH" 不是元素 → 下标, +1    ✅
             MnO4- → "MnO" 不是元素 → 下标, -1  ✅
             O2-  → "O" 是元素 → 电荷 -2        ✅
             [..]n± 与 ^n± 直接按显式电荷处理。 */
        string chargePart = null;
        int caret = s.IndexOf('^');
        if (caret >= 0) {
            chargePart = s.Substring(caret + 1);
            s = s.Substring(0, caret);
        } else if (s.EndsWith("+") || s.EndsWith("-")) {
            char sign = s[s.Length - 1];
            string body = s.Substring(0, s.Length - 1);
            int d = 0;
            while (d < body.Length && char.IsDigit(body[body.Length - 1 - d])) d++;
            string digits = d > 0 ? body.Substring(body.Length - d) : "";
            string rest = d > 0 ? body.Substring(0, body.Length - d) : body;
            char lastOfRest = rest.Length > 0 ? rest[rest.Length - 1] : ' ';
            bool explicitBracket = lastOfRest == ']';
            bool restIsElement = System.Text.RegularExpressions.Regex.IsMatch(rest, @"^[A-Z][a-z]?$");
            if (digits.Length > 0 && (explicitBracket || restIsElement)) {
                chargePart = digits + sign;          /* Fe2+ / [CuCl4]2- / O2- */
                s = rest;
            } else if (digits.Length > 0) {
                chargePart = sign.ToString();        /* NH4+ / MnO4- ：数字是下标 */
                s = body;
            } else {
                chargePart = sign.ToString();        /* H+ / OH- / Cl- */
                s = body;
            }
        }
        if (chargePart != null && chargePart.Length > 0) {
            char sign = chargePart[chargePart.Length - 1];
            string digits = chargePart.Substring(0, chargePart.Length - 1);
            int mag = digits.Length == 0 ? 1 : int.Parse(digits);
            sp.Charge = sign == '-' ? -mag : mag;
        }

        /* 去括号（只处理最外层包裹整个式子的那种） */
        if (s.StartsWith("[") && s.EndsWith("]")) s = s.Substring(1, s.Length - 2);

        // Formula.Parse handles hydrate segment multipliers (e.g. ·5H2O).
        foreach(var atom in ChemLab.Chem.Formula.Parse(s)) sp.Atoms[atom.Key]=atom.Value;
        return sp;
    }

    /// <summary>支持嵌套括号的原子计数</summary>
    static void CountAtoms(string s, Dictionary<string,int> atoms) {
        int i = 0;
        var stack = new List<Dictionary<string,int>> { atoms };
        var mult = new List<int> { 1 };
        while (i < s.Length) {
            char c = s[i];
            if (c == '(' || c == '[') {
                stack.Add(new Dictionary<string,int>());
                mult.Add(1);
                i++;
            } else if (c == ')' || c == ']') {
                i++;
                int n = 0;
                while (i < s.Length && char.IsDigit(s[i])) { n = n * 10 + (s[i] - '0'); i++; }
                if (n == 0) n = 1;
                var inner = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                mult.RemoveAt(mult.Count - 1);
                foreach (var kv in inner) {
                    var target = stack[stack.Count - 1];
                    int cur; target.TryGetValue(kv.Key, out cur);
                    target[kv.Key] = cur + kv.Value * n;
                }
            } else if (char.IsUpper(c)) {
                int st = i; i++;
                while (i < s.Length && char.IsLower(s[i])) i++;
                string el = s.Substring(st, i - st);
                int n = 0;
                while (i < s.Length && char.IsDigit(s[i])) { n = n * 10 + (s[i] - '0'); i++; }
                if (n == 0) n = 1;
                var target = stack[stack.Count - 1];
                int cur; target.TryGetValue(el, out cur);
                target[el] = cur + n;
            } else i++;   // 忽略未知字符（如 ·）
        }
    }

    /// <summary>按「空格 + 空格」切分左右两边的项。
    /// 绝不能直接按 '+' 切 —— 那会把 Zn2+ 拆成 Zn2，把 H+ 拆掉，
    /// 电荷信息就丢了（这是本项目已经踩过一次的坑）。</summary>
    static string[] SplitTerms(string side) {
        var rx = new System.Text.RegularExpressions.Regex(@"\s+\+\s+");
        var parts = rx.Split(side.Trim());
        var outp = new List<string>();
        foreach (var p in parts) {
            var q = p.Trim();
            if (q.Length > 0) outp.Add(q);
        }
        return outp.ToArray();
    }

    public static bool CheckEquation(string eq, out string detail) {
        detail = "";
        string arrow = null;
        foreach (var a in new[] { "->", "→", "⟶", "=" })
            if (eq.Contains(a)) { arrow = a; break; }
        if (arrow == null) { detail = "没有找到反应箭头"; return false; }
        var parts = eq.Split(new[] { arrow }, StringSplitOptions.None);
        if (parts.Length != 2) { detail = "箭头数量不是 1"; return false; }

        var left = new Dictionary<string,int>();
        var right = new Dictionary<string,int>();
        int ql = 0, qr = 0, el = 0, er = 0;
        foreach (var term in SplitTerms(parts[0])) {
            var sp = ParseTerm(term);
            foreach (var kv in sp.Atoms) { int c; left.TryGetValue(kv.Key, out c); left[kv.Key] = c + kv.Value * sp.Coef; }
            ql += sp.Charge * sp.Coef; el += sp.Electrons * sp.Coef;
        }
        foreach (var term in SplitTerms(parts[1])) {
            var sp = ParseTerm(term);
            foreach (var kv in sp.Atoms) { int c; right.TryGetValue(kv.Key, out c); right[kv.Key] = c + kv.Value * sp.Coef; }
            qr += sp.Charge * sp.Coef; er += sp.Electrons * sp.Coef;
        }

        var probs = new List<string>();
        foreach (var el2 in left.Keys.Union(right.Keys))
            if (!left.ContainsKey(el2) || !right.ContainsKey(el2) ||
                left[el2] != right[el2]) {
                int l, r; left.TryGetValue(el2, out l); right.TryGetValue(el2, out r);
                probs.Add($"{el2}: 左{l} 右{r}");
            }
        if (ql != qr) probs.Add($"电荷: 左{ql} 右{qr}");
        /* 不单独查电子数：电荷守恒已经涵盖电子（e- 带 -1 电荷）。
           半反应里电子本来就只出现在一侧，把它当守恒量会误判。 */
        detail = probs.Count == 0
            ? $"原子/电荷守恒（电荷 {ql}）"
            : "不守恒 → " + string.Join("; ", probs);
        return probs.Count == 0;
    }

    /* ================= 定量计算内核 =================
       滴定读数处理与量纲换算属于「实验数据处理」，是仿真器该有的能力，
       也正好是规范 R02 / R05 要测的东西。 */

    public static double DoCalc(string calc, JVal a, Dictionary<string,double> vars, out string err) {
        err = null;
        switch (calc) {
            case "titre":
                return Math.Round((a["final"].Num() - a["initial"].Num()) * 100) / 100.0;

            case "n":      /* n = c × V(cm3)/1000 */
                return a["c"].Num() * a["v_cm3"].Num() / 1000.0;

            case "v":      /* V(cm3) = n / c × 1000 */
                return a["c"].Num() == 0 ? double.NaN : a["n"].Num() / a["c"].Num() * 1000.0;

            case "scale":
                return a["n"].Num() * a["factor"].Num();

            case "dilute":  /* 稀释后浓度 = 原浓度 × (取液 / 定容) */
                return a["from_cm3"].Num() == 0 ? double.NaN
                     : a["c1"].Num() * a["from_cm3"].Num() / a["to_cm3"].Num();

            case "unit": {
                string f = a["from"].Str(), t = a["to"].Str();
                double v = a["value"].Num();
                if (f == "cm3" && t == "dm3") return v / 1000.0;
                if (f == "dm3" && t == "cm3") return v * 1000.0;
                if (f == "g"   && t == "mg")  return v * 1000.0;
                if (f == "mg"  && t == "g")   return v / 1000.0;
                if (f == "mol" && t == "mmol") return v * 1000.0;
                err = "未知换算 " + f + "->" + t;
                return double.NaN;
            }

            case "mean": {
                var xs = new List<double>();
                foreach (var e in a["values"].A ?? new List<JVal>()) xs.Add(e.Num());
                if (xs.Count == 0) { err = "没有数值"; return double.NaN; }
                double s = 0; foreach (var x in xs) s += x;
                return s / xs.Count;
            }

            case "batch_eval": {
                /* 按 CAIE MS 的规则处理一整批滴定读数：
                   1) 排除标为 rough 的
                   2) 优先选完全相同的一组；否则选极差 ≤0.05 的一组；再否则 ≤0.10
                   3) 用于平均的数据总极差必须 ≤ 0.20
                   输出：mean / spread / count / rejected / concordant */
                var rows = new List<(string label, double titre)>();
                foreach (var r in a["readings"].A ?? new List<JVal>()) {
                    double t = Math.Round((r["final"].Num() - r["initial"].Num()) * 100) / 100.0;
                    rows.Add((r["label"].Str("?"), t));
                }
                var acc = rows.Where(x => x.label.ToLowerInvariant() != "rough")
                              .OrderBy(x => x.titre).ToList();
                /* 统计量无论如何都要给出，便于负向用例检查「数据不一致」 */
                vars["sel_count"] = acc.Count;
                vars["sel_spread"] = acc.Count >= 2 ? acc.Max(x => x.titre) - acc.Min(x => x.titre) : 0;
                vars["all_spread"] = acc.Count >= 2 ? acc.Max(x => x.titre) - acc.Min(x => x.titre) : 0;
                vars["rejected"] = acc.Count;
                vars["concordant"] = 0;
                if (acc.Count < 2) { err = "准确滴定不足两组"; return double.NaN; }

                List<(string label, double titre)> sel = null;
                foreach (var tol in new[] { 0.0, 0.05, 0.10, 0.20 }) {
                    for (int i = 0; i + 1 < acc.Count; i++) {
                        var grp = acc.Where(x => x.titre >= acc[i].titre - 1e-9 &&
                                                 x.titre <= acc[i].titre + tol + 1e-9).ToList();
                        if (grp.Count >= 2 && (sel == null || grp.Count > sel.Count)) sel = grp;
                    }
                    if (sel != null && sel.Count >= 2) break;
                }
                if (sel == null) { err = "找不到一致的两组数据（不 concordant）"; return double.NaN; }

                double spread = sel.Max(x => x.titre) - sel.Min(x => x.titre);
                double mean = sel.Average(x => x.titre);
                vars["concordant"] = 1;
                vars["sel_count"] = sel.Count;
                vars["sel_spread"] = spread;
                vars["rejected"] = rows.Count - sel.Count;
                vars["mean_raw"] = mean;
                vars["mean_2dp"] = Math.Round(mean, 2);
                return Math.Round(mean, 2);
            }

            case "round":
                return Math.Round(a["value"].Num(), (int)a["dp"].Num(2));

            case "sf": {
                /* 保留 n 位有效数字 */
                double v = a["value"].Num();
                int n = (int)a["sf"].Num(3);
                if (v == 0) return 0;
                double mag = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(v))) - (n - 1));
                return Math.Round(v / mag) * mag;
            }
        }
        err = "未知计算 " + calc;
        return double.NaN;
    }

    static VesselKind KindOf(string s) {
        switch ((s ?? "").ToLowerInvariant()) {
            case "beaker":   return VesselKind.Beaker;
            case "flask":    return VesselKind.Flask;
            case "tube":     return VesselKind.Tube;
            case "cylinder": return VesselKind.Cylinder;
            case "round":    return VesselKind.RoundBottom;
            case "crucible": return VesselKind.Crucible;
            case "dish":     return VesselKind.Dish;
            case "bottle":   return VesselKind.Bottle;
            default:         return VesselKind.Beaker;
        }
    }

    static HeatMode ModeOf(string s) {
        switch ((s ?? "").ToLowerInvariant()) {
            case "heat":   return HeatMode.Heat;
            case "reflux": return HeatMode.Reflux;
            case "distil": return HeatMode.Distil;
            default:       return HeatMode.None;
        }
    }

    static JVal ObjState(Vessel v) {
        var o = MiniJson.Obj();
        var sol = MiniJson.Obj();
        foreach (var kv in v.Solutes.OrderByDescending(x => x.Value))
            if (kv.Value > 1e-12) sol.Put(kv.Key, MiniJson.Num(kv.Value));
        o.Put("solutes", sol);
        var pre = MiniJson.Obj();
        foreach (var kv in v.Precipitates) if (kv.Value > 1e-12) pre.Put(kv.Key, MiniJson.Num(kv.Value));
        o.Put("precipitates", pre);
        var gas = MiniJson.Obj();
        foreach (var kv in v.Gases) if (kv.Value > 1e-12) gas.Put(kv.Key, MiniJson.Num(kv.Value));
        o.Put("gases", gas);
        var und = MiniJson.Obj();
        foreach (var kv in v.Undissolved) if (kv.Value > 1e-12) und.Put(kv.Key, MiniJson.Num(kv.Value));
        o.Put("undissolved_detail", und);
        o.Put("undissolved", MiniJson.Num(v.Undissolved.Count));
        o.Put("water_ml", MiniJson.Num(v.Water));
        o.Put("temp_c", MiniJson.Num(v.Temp));
        o.Put("ph", MiniJson.Num(v.Ph));
        Color c; string cname = "无色";
        if (Lab.TrySolutionColor(v, out c)) cname = Lab.ColorName(c);
        o.Put("colour", MiniJson.Str(cname));
        if (Lab.TrySolutionColor(v, out c)) {
            o.Put("rgb", MiniJson.Arr().Push(MiniJson.Num(Math.Round(c.r * 255)))
                                 .Push(MiniJson.Num(Math.Round(c.g * 255)))
                                 .Push(MiniJson.Num(Math.Round(c.b * 255))));
        }
        var tarr = MiniJson.Arr();
        foreach (var ti in TestSet.All) {
            var td = MiniJson.Obj();
            td.Put("id", MiniJson.Str(ti.Id));
            td.Put("kind", MiniJson.Str(ti.Kind.ToString()));
            td.Put("name", MiniJson.Str(ti.Name));
            td.Put("moist", MiniJson.Num(ti.Moist ? 1 : 0));
            td.Put("splint", MiniJson.Str(ti.Splint.ToString()));
            td.Put("tint", MiniJson.Str(ti.Tint ?? ""));
            td.Put("result", MiniJson.Str(ti.LastResult ?? ""));
            tarr.Push(td);
        }
        o.Put("tests", tarr);
        o.Put("pop", MiniJson.Num(v.Fx.Pop));

        /* 标签两视图的实际内容 —— 便于精确核对，不用去数截图上的小字 */
        var molArr = MiniJson.Arr();
        foreach (var s in Chem.VesselView.Lines(v, true))
            molArr.Push(MiniJson.Str($"{s.Formula}|{s.Mol:G6}|{s.Conc:G6}"));
        o.Put("tag_molecular", molArr);
        var ionArr = MiniJson.Arr();
        foreach (var s in Chem.VesselView.Lines(v, false))
            ionArr.Push(MiniJson.Str($"{s.Formula}|{s.Mol:G6}|{s.Conc:G6}"));
        o.Put("tag_ionic", ionArr);
        return o;
    }

    public static JVal RunCase(JVal c) {
        var res = MiniJson.Obj();
        string caseId = c["case_id"].Str("(unnamed)");
        res.Put("case_id", MiniJson.Str(caseId));
        res.Put("mode", MiniJson.Str(c["mode"].Str("simulate")));
        var asserts = MiniJson.Arr();
        var events = MiniJson.Arr();
        var probesOut = MiniJson.Obj();

        try {
            Lab.Reset();
            Lab.EnsureReady();

            var vs = new Dictionary<string, Vessel>();
            var vars = new Dictionary<string, double>();
            var testVars = new Dictionary<string, TestItem>();
            var strVars = new Dictionary<string, string>();
            TestItem lastTest = null;
            string lastTestResult = "";
            foreach (var vd in c["vessels"].A ?? new List<JVal>()) {
                var v = Lab.AddVessel(KindOf(vd["kind"].Str("beaker")));
                if (vd["water_ml"].Num(0) > 0) v.AddWater(vd["water_ml"].Num());
                vs[vd["id"].Str("v")] = v;
            }
            if (vs.Count == 0) vs["v"] = Lab.AddVessel(VesselKind.Beaker);

            TestItem FindTest(JVal a) {
                string n2 = a["test"].Str("");
                if (n2.Length > 0 && testVars.ContainsKey(n2)) return testVars[n2];
                return lastTest;
            }

            Vessel pick(JVal a) {
                string id = a["vessel"].Str("");
                if (id.Length > 0 && vs.ContainsKey(id)) return vs[id];
                return vs.Values.First();
            }

            /* ---- 动作序列 ---- */
            foreach (var a in c["actions"].A ?? new List<JVal>()) {
                string op = a["op"].Str("");
                var v = pick(a);
                switch (op) {
                    case "add_mol":
                        v.Introduce(a["id"].Str(), a["mol"].Num(), v.Water > 1);
                        break;
                    case "add_ml":
                        Lab.AddLiquid(v, a["id"].Str(), a["ml"].Num());
                        break;
                    case "add_g":
                        Lab.AddReagent(v, a["id"].Str(), a["g"].Num());
                        break;
                    case "water":
                        v.AddWater(a["ml"].Num());
                        break;
                    case "set_temp":
                        v.Temp = (float)a["c"].Num(25);
                        break;
                    case "heat":
                        v.Mode = ModeOf(a["mode"].Str("heat"));
                        break;
                    case "stir":   Lab.SetStir(v, a["on"].Bool(true)); break;
                    case "ignite": Lab.Ignite(v); break;
                    case "settle": Lab.Settle(v); break;
                    /* ---- 离子检验器材 ---- */
                    case "add_test": {
                        var kind = a["kind"].Str("red_litmus") switch {
                            "blue_litmus" => TestItemKind.BlueLitmus,
                            "splint"      => TestItemKind.Splint,
                            _             => TestItemKind.RedLitmus,
                        };
                        var ti = TestSet.Add(kind, new Vector3(0f, 0f, 0f));
                        lastTest = ti;
                        string tn2 = a["as"].Str("t");
                        if (!string.IsNullOrEmpty(tn2)) testVars[tn2] = ti;
                        break;
                    }
                    case "moisten": {
                        var ti = FindTest(a);
                        GasTest.Moisten(ti);
                        break;
                    }
                    case "light_splint": {
                        var ti = FindTest(a);
                        GasTest.Light(ti);
                        break;
                    }
                    case "extinguish_splint": {
                        var ti = FindTest(a);
                        GasTest.Extinguish(ti);
                        break;
                    }
                    case "insert": {
                        /* 把检验器材伸到容器口（对准气相） */
                        var ti = FindTest(a);
                        string r = GasTest.Insert(v, ti);
                        string vn = a["as"].Str("res");
                        if (!string.IsNullOrEmpty(vn)) strVars[vn] = r;
                        lastTestResult = r;
                        break;
                    }
                    case "transfer_gas": {
                        /* 把一只容器里产生的气体通入另一只容器 ——
                           石灰水检验必须让 CO2 进到**独立的**石灰水里，
                           直接倒在同一个容器里会被溶液里的其它离子（如 Zn2+）抢走 OH-。 */
                        string toId = a["to"].Str("");
                        if (!vs.ContainsKey(toId)) break;
                        var dst = vs[toId];
                        string only = a["gas"].Str("");
                        double move = a["mol"].Num(0);
                        var keys = new List<string>(v.Gases.Keys);
                        foreach (var gk in keys) {
                            if (only.Length > 0 && gk != only) continue;
                            double amt = move > 0 ? Math.Min(v.Gases[gk], move) : v.Gases[gk];
                            if (amt <= 1e-12) continue;
                            v.Gases[gk] -= amt;
                            if (v.Gases[gk] <= 1e-12) v.Gases.Remove(gk);
                            dst.Gases.TryGetValue(gk, out double cur);
                            dst.Gases[gk] = cur + amt;
                        }
                        Lab.Note(dst, "将生成的气体通入该容器", "add");
                        Lab.Settle(dst);
                        break;
                    }
                    case "advance": {
                        /* 无头环境手动推进器材状态（木条烧尽、余烬熄灭） */
                        double secs2 = a["seconds"].Num(1.0);
                        const double dt2 = 0.05;
                        for (double t2 = 0; t2 < secs2; t2 += dt2) TestSet.Tick((float)dt2);
                        break;
                    }
                    /* ---- 器材交互：吸附 / 本生灯 / 按比例倾倒 ---- */
                    case "burner": {
                        /* 把容器吸附到本生灯上，设定火力并点燃，再推进若干秒 */
                        if (ApparatusSet.All.Count == 0)
                            ApparatusSet.Add(ApparatusKind.BunsenBurner, new Vector3(-2.3f, 0f, 1.25f));
                        var burner = ApparatusSet.All.First(x => x.Kind == ApparatusKind.BunsenBurner);
                        if (v.Burner != null && v.Burner != burner) v.Burner.Holds = null;
                        v.Burner = burner; burner.Holds = v;
                        burner.GasFlow = a["gas"].Num(0.5);
                        burner.Lit = a["lit"].Bool(true) && burner.CanLight;
                        v.Pos = new Vector3(burner.Pos.x, 0f, burner.Pos.z);
                        v.LiftY = SnapController.LiftOntoApparatus(burner);
                        double secs = a["seconds"].Num(0);
                        if (secs > 0) {
                            /* 无头环境没有帧循环，按步长手动推进 */
                            const double dt = 0.05;
                            for (double t = 0; t < secs; t += dt) {
                                ApparatusSet.Tick((float)dt);
                                Lab.Tick((float)dt);
                            }
                        }
                        break;
                    }
                    case "burner_off": {
                        if (v.Burner != null) { v.Burner.Lit = false; v.Burner.Holds = null; v.Burner = null; }
                        v.LiftY = 0f;
                        break;
                    }
                    case "snap": {
                        /* 把容器悬停到另一个容器上方 */
                        string onto = a["onto"].Str("");
                        if (vs.ContainsKey(onto)) {
                            v.AttachedTo = vs[onto];
                            v.Pos = new Vector3(vs[onto].Pos.x, 0f, vs[onto].Pos.z);
                            v.LiftY = SnapController.LiftOnto(vs[onto], v);
                        }
                        break;
                    }
                    case "pour_frac": {
                        /* 按滑动条比例从本容器倒入目标容器 */
                        string dst = a["to"].Str("");
                        if (vs.ContainsKey(dst)) {
                            double frac = a["frac"].Num(0.5);
                            v.PourAmount = (float)frac;
                            Lab.PourFraction(v, vs[dst], frac);
                        }
                        break;
                    }
                    case "compute": {
                        string cname = a["name"].Str("");
                        string cerr = null;
                        /* 支持引用前一步算出的变量 */
                        string srcName = a["from_var"].Str("");
                        if (srcName.Length > 0) {
                            double src;
                            if (vars.TryGetValue(srcName, out src)) a.O["n"] = MiniJson.Num(src);
                            else { events.Push(MiniJson.Str("引用不存在的变量 " + srcName)); }
                        }
                        double cv;
                        string calcKind = a["calc"].Str("");
                        if (calcKind == "mass")        cv = v.Mass();
                        else if (calcKind == "mol_of") cv = v.Get(a["species"].Str());
                        else if (calcKind == "species_delta_ratio") {
                            /* 计量比 = (初始量 − 剩余量) / 已加入的另一种试剂。
                               直接读容器真实状态，而不是拿题面数字自己算 ——
                               这样才是真的在检验引擎的化学计量。 */
                            double a0 = a["a0"].Num();
                            double consumed = a0 - v.Get(a["a"].Str());
                            double bAdded = a["b"].Num();
                            cv = Math.Abs(bAdded) < 1e-15 ? double.NaN : consumed / bAdded;
                        } else cv = DoCalc(calcKind, a, vars, out cerr);
                        if (cname.Length > 0) vars[cname] = cv;
                        if (cerr != null) events.Push(MiniJson.Str("计算失败 " + cname + ": " + cerr));
                        break;
                    }
                    case "titrate": {
                        /* 逐步加入滴定剂，每步 settle，用于终点类断言 */
                        string id = a["id"].Str("");
                        double conc = a["conc"].Num();
                        double vol = a["to_cm3"].Num();
                        double step = a["step_cm3"].Num(1.0);
                        double from = a["from_cm3"].Num(0);
                        var curve = MiniJson.Arr();
                        string probeId = a["probe"].Str("");
                        for (double t = from + step; t <= vol + 1e-9; t += step) {
                            v.Introduce(id, conc * step / 1000.0, true);
                            Lab.Settle(v);
                            var pt = MiniJson.Obj();
                            pt.Put("v_cm3", MiniJson.Num(Math.Round(t * 100) / 100));
                            pt.Put("free_titrant", MiniJson.Num(v.Get(id)));
                            if (probeId.Length > 0) pt.Put("probe", MiniJson.Num(v.Get(probeId)));
                            Color cc; string cn = "无色";
                            if (Lab.TrySolutionColor(v, out cc)) cn = Lab.ColorName(cc);
                            pt.Put("colour", MiniJson.Str(cn));
                            curve.Push(pt);
                        }
                        res.Put("titration_curve", curve);
                        break;
                    }
                    default:
                        events.Push(MiniJson.Str("未知操作: " + op));
                        break;
                }
            }
            /* 若没显式 settle，最后补一次 */
            foreach (var v in vs.Values) Lab.Settle(v);

            /* ---- 终态 ---- */
            var final = MiniJson.Obj();
            foreach (var kv in vs) final.Put(kv.Key, ObjState(kv.Value));
            res.Put("final_state", final);
            foreach (var p in c["probes"].A ?? new List<JVal>()) {
                string pid = p.Str();
                var one = MiniJson.Obj();
                foreach (var kv in vs) {
                    var vv = kv.Value;
                    one.Put("amount", MiniJson.Num(vv.Get(pid)));
                    one.Put("precip", MiniJson.Num(vv.Precipitates.ContainsKey(pid) ? vv.Precipitates[pid] : 0));
                    one.Put("gas", MiniJson.Num(vv.Gases.ContainsKey(pid) ? vv.Gases[pid] : 0));
                    one.Put("colour", MiniJson.Str(ObjState(vv)["colour"].Str()));
                    break;
                }
                probesOut.Put(pid, one);
            }
            res.Put("probes", probesOut);

            var rxn = MiniJson.Arr();
            foreach (var l in Lab.Log.Where(x => x.Kind == "rxn").Take(12))
                rxn.Push(MiniJson.Str(l.Text));
            res.Put("reactions_observed", rxn);

            /* ---- 断言 ---- */
            foreach (var asrt in c["asserts"].A ?? new List<JVal>()) {
                var r = MiniJson.Obj();
                string aid = asrt["id"].Str("");
                string kind = asrt["kind"].Str("");
                r.Put("id", MiniJson.Str(aid));
                r.Put("kind", MiniJson.Str(kind));
                bool pass = false; string detail = "";
                try {
                    var v = pick(asrt);
                    switch (kind) {
                        case "amount": {
                            string sp = asrt["species"].Str();
                            double got = v.Get(sp), exp = asrt["expect"].Num(), tol = asrt["tol"].Num(1e-6);
                            pass = Math.Abs(got - exp) <= tol;
                            detail = $"{sp}: got={got:G8} expect={exp:G8} tol={tol:G3}";
                            break;
                        }
                        case "ratio": {
                            string sa = asrt["a"].Str(), sb = asrt["b"].Str();
                            double A = v.Get(sa), B = v.Get(sb);
                            double got = B == 0 ? double.NaN : A / B;
                            double exp = asrt["expect"].Num(), tol = asrt["tol"].Num(1e-3);
                            pass = !double.IsNaN(got) && Math.Abs(got - exp) <= tol;
                            detail = $"{sa}/{sb} = {got:G6} (expect {exp})";
                            break;
                        }
                        case "present": {
                            string sp = asrt["species"].Str();
                            double got = v.Get(sp), min = asrt["min"].Num(1e-9);
                            pass = got > min;
                            detail = $"{sp} = {got:G6} > {min:G3}";
                            break;
                        }
                        case "absent": {
                            string sp = asrt["species"].Str();
                            double got = v.Get(sp), max = asrt["max"].Num(1e-9);
                            pass = got <= max;
                            detail = $"{sp} = {got:G6} <= {max:G3}";
                            break;
                        }
                        case "mass": {
                            double got = v.Mass(), exp = asrt["expect"].Num(), tol = asrt["tol"].Num(0.01);
                            pass = Math.Abs(got - exp) <= tol;
                            detail = $"质量 {got:F4} g (期望 {exp:F4} ± {tol})";
                            break;
                        }
                        /* ---- 离子检验器材 ---- */
                        case "litmus_turned": {
                            /* 石蕊试纸是否变成了指定方向的颜色 */
                            var ti = FindTest(asrt);
                            string want = asrt["to"].Str("blue");   // blue / red / none
                            bool changedBlue = ti != null && ti.Tint == "#3a5ad8";
                            bool changedRed  = ti != null && ti.Tint == "#d83a3a";
                            bool bleached    = ti != null && ti.Tint == "#f0f0f0";
                            pass = want == "blue" ? changedBlue
                                 : want == "red"  ? changedRed
                                 : want == "bleach" ? bleached
                                 : !(changedBlue || changedRed || bleached);
                            detail = ti == null ? "没有这块试纸"
                                   : $"{ti.Name}({ti.StateText}) 变色={ti.Tint} → {(pass ? "符合" : "不符合")}期望 {want}";
                            break;
                        }
                        case "splint_state": {
                            var ti = FindTest(asrt);
                            string want = asrt["expect"].Str("Burning");
                            pass = ti != null && ti.Splint.ToString() == want;
                            detail = ti == null ? "没有木条" : $"木条状态 {ti.Splint}（期望 {want}）";
                            break;
                        }
                        case "popped": {
                            /* 是否发生了爆鸣。min 给下限、max 给上限（用于「不该爆鸣」的负向验证） */
                            double pv = v.Fx.Pop;
                            /* 注意：缺失的键拿到的是 JVal.Null 而不是 C# null，
                               所以要用「有没有给定值」来判断，不能用 != null。 */
                            double mx = asrt["max"].Num(-1), mn = asrt["min"].Num(-1);
                            bool hasMin = mn >= 0, hasMax = mx >= 0;
                            if (hasMax)       pass = pv <= mx;
                            else if (hasMin)  pass = pv >= mn;
                            else              pass = pv > 1e-9;
                            detail = pass ? $"爆鸣强度 {pv:F2}（符合期望）"
                                          : $"爆鸣强度 {pv:F2}（不符合期望 {(hasMax ? "≤" + asrt["max"].Num(0) : "≥" + asrt["min"].Num(0))}）";
                            break;
                        }
                        case "test_result": {
                            /* 观察描述里是否含有指定关键词 */
                            var ti = FindTest(asrt);
                            string kw = asrt["contains"].Str("");
                            string got = ti != null ? (ti.LastResult ?? "") : lastTestResult;
                            pass = kw.Length == 0 || got.Contains(kw);
                            detail = got;
                            break;
                        }
                        /* ---- 容器标签的两种视图 ---- */
                        case "view_has": {
                            /* 指定视图里是否有某物种，且 mol / 浓度对得上 */
                            bool mol = asrt["molecular"].Bool(true);
                            var vv = Chem.VesselView.Lines(v, mol);
                            string sp2 = asrt["species"].Str("");
                            var hit = vv.FirstOrDefault(x => x.Id == sp2);
                            bool ok = hit != null;
                            string msg = ok ? $"{hit.Formula} {hit.Mol:G4} mol {hit.Conc:F4} M" : "未找到";
                            /* 注意：缺失的键拿到的是 JVal.Null 而不是 C# null，
                               所以用「有没有给定值」（这里用负数哨兵）来判断。 */
                            double wantMol = asrt["mol"].Num(-1), wantConc = asrt["conc"].Num(-1);
                            double tol = asrt["tol"].Num(-1);
                            if (ok && wantMol >= 0) {
                                double t2 = tol > 0 ? tol : 1e-9;
                                if (Math.Abs(hit.Mol - wantMol) > t2) { ok = false; msg += $" (mol 期望 {wantMol:G4})"; }
                            }
                            if (ok && wantConc >= 0) {
                                double t2 = tol > 0 ? tol : 1e-4;
                                if (double.IsNaN(hit.Conc) || Math.Abs(hit.Conc - wantConc) > t2) { ok = false; msg += $" (浓度期望 {wantConc:F4})"; }
                            }
                            pass = ok;
                            detail = $"{(mol ? "具体物质" : "离子状态")}视图 " + msg;
                            break;
                        }
                        case "view_absent": {
                            bool mol = asrt["molecular"].Bool(true);
                            var vv = Chem.VesselView.Lines(v, mol);
                            string sp2 = asrt["species"].Str("");
                            bool found = vv.Any(x => x.Id == sp2);
                            pass = !found;
                            detail = $"{(mol ? "具体物质" : "离子状态")}视图里{(found ? "仍出现" : "不含")} {sp2}";
                            break;
                        }
                        case "view_count": {
                            bool mol = asrt["molecular"].Bool(true);
                            int cnt = Chem.VesselView.Lines(v, mol).Count;
                            int want = (int)asrt["expect"].Num(0);
                            pass = cnt == want;
                            detail = $"{(mol ? "具体物质" : "离子状态")}视图共 {cnt} 条（期望 {want}）";
                            break;
                        }
                        /* ---- 反应方程式的规范化输出 ---- */
                        case "eq_format": {
                            /* 按中/英规范格式化指定规则，并检查关键片段 */
                            ChemLab.Chem.Rules.Init();
                            string want = asrt["rule"].Str("");
                            var rule = ChemLab.Chem.Rules.All.FirstOrDefault(x => x.Parsed && x.Eq == want);
                            if (rule == null) { pass = false; detail = "找不到规则: " + want; break; }
                            bool en = asrt["english"].Bool(false);
                            bool ion = asrt["ionic"].Bool(false);
                            string got = ChemLab.Chem.EqFormat.Format(rule, en, ion);
                            var checks = new List<string>();
                            foreach (var kv in (asrt["contains"].A ?? new List<JVal>()))
                                checks.Add(kv.Str(""));
                            pass = true;
                            foreach (var ck in checks)
                                if (!got.Contains(ck)) { pass = false; detail = $"缺少「{ck}」→ {got}"; }
                            if (pass) detail = got;
                            break;
                        }
                        case "eq_condition": {
                            ChemLab.Chem.Rules.Init();
                            string want2 = asrt["rule"].Str("");
                            var rule2 = ChemLab.Chem.Rules.All.FirstOrDefault(x => x.Parsed && x.Eq == want2);
                            if (rule2 == null) { pass = false; detail = "找不到规则: " + want2; break; }
                            string cl = ChemLab.Chem.EqFormat.CondLabel(rule2.Cond, asrt["english"].Bool(false));
                            string expect = asrt["expect"].Str("");
                            pass = cl == expect;
                            detail = $"条件标注 = 「{cl}」(期望「{expect}」)";
                            break;
                        }
                        /* ---- 焓变计算：盖斯定律求解 ---- */
                        case "hess_edges": {
                            /* 用一条回路解未知焓变。
                               nodes: 节点名数组；edges: [from,to,dh,src] 其中 src 为
                               'table'/'manual'/'unknown'。断言 kind=var 取回结果。 */
                            ChemLab.Chem.HessLaw.Init();
                            var names = (asrt["nodes"].A ?? new List<JVal>()).Select(x => x.Str("")).ToList();
                            var hessEdges = new List<ChemLab.Chem.HessEdge>();
                            foreach (var ev in (asrt["edges"].A ?? new List<JVal>())) {
                                var arr = ev.A;
                                int f = names.IndexOf(arr[0].Str("")), t2 = names.IndexOf(arr[1].Str(""));
                                var src = arr[3].Str("manual");
                                hessEdges.Add(new ChemLab.Chem.HessEdge {
                                    From = f, To = t2, DH = arr[2].Num(0),
                                    Source = src == "unknown" ? ChemLab.Chem.EnthalpySource.Unknown
                                           : src == "table"   ? ChemLab.Chem.EnthalpySource.Table
                                           : ChemLab.Chem.EnthalpySource.Manual,
                                });
                            }
                            var cyc = ChemLab.Chem.HessLaw.FindCycle(names.Count, hessEdges);
                            bool sok = ChemLab.Chem.HessLaw.SolveCycle(cyc, hessEdges, out double unk, out string swhy);
                            string vn = asrt["as"].Str("dh");
                            if (!string.IsNullOrEmpty(vn)) vars[vn] = unk;
                            pass = sok;
                            detail = (cyc.Count >= 4 ? "回路 " + string.Join("→", cyc) + "：" : "") + swhy;
                            break;
                        }
                        case "bond_energy": {
                            ChemLab.Chem.HessLaw.Init();
                            double got = ChemLab.Chem.HessLaw.BondEnergy(
                                asrt["a"].Str(""), asrt["b"].Str(""), asrt["order"].Num(1));
                            double exp = asrt["expect"].Num(0), tol = asrt["tol"].Num(0.5);
                            pass = !double.IsNaN(got) && Math.Abs(got - exp) <= tol;
                            detail = $"{asrt["a"].Str("")}-{asrt["b"].Str("")} 键能 = {got:F0}（期望 {exp:F0}）";
                            break;
                        }
                        case "atomisation": {
                            ChemLab.Chem.HessLaw.Init();
                            double got = ChemLab.Chem.HessLaw.AtomisationOf(asrt["el"].Str(""));
                            double exp = asrt["expect"].Num(0), tol = asrt["tol"].Num(0.5);
                            pass = !double.IsNaN(got) && Math.Abs(got - exp) <= tol;
                            detail = $"{asrt["el"].Str("")} 原子化焓 = {got:F0}（期望 {exp:F0}）";
                            break;
                        }
                        case "water": {
                            double gw = v.Water, ew = asrt["expect"].Num(), tw = asrt["tol"].Num(0.5);
                            pass = Math.Abs(gw - ew) <= tw;
                            detail = $"水量 {gw:F1} mL (期望 {ew:F1} ± {tw})";
                            break;
                        }
                        case "temp": {
                            double got = v.Temp, exp = asrt["expect"].Num(), tol = asrt["tol"].Num(0.5);
                            pass = Math.Abs(got - exp) <= tol;
                            detail = $"温度 {got:F2} ℃ (期望 {exp:F2} ± {tol})";
                            break;
                        }
                        case "temp_le":
                        case "temp_ge": {
                            double got = v.Temp, lim = asrt["limit"].Num();
                            pass = kind == "temp_ge" ? got >= lim : got <= lim;
                            detail = $"温度 {got:F2} ℃ {(kind == "temp_ge" ? ">=" : "<=")} {lim}";
                            break;
                        }
                        case "colour": {
                            Color cc; string cn = "无色";
                            if (Lab.TrySolutionColor(v, out cc)) cn = Lab.ColorName(cc);
                            string want = asrt["contains"].Str();
                            pass = cn.Contains(want);
                            detail = $"colour = {cn} (want contains '{want}')";
                            break;
                        }
                        case "equation_balanced": {
                            pass = CheckEquation(asrt["equation"].Str(), out detail);
                            break;
                        }
                        case "equation_unbalanced": {
                            /* 负向断言：这条方程式**必须**不守恒才算通过 */
                            bool bal = CheckEquation(asrt["equation"].Str(), out detail);
                            pass = !bal;
                            detail = (bal ? "错误地判为守恒 → " : "正确地判为不守恒 → ") + detail;
                            break;
                        }
                        case "molar_mass": {
                            double got = Formula.MolarMass(asrt["formula"].Str());
                            double exp = asrt["expect"].Num(), tol = asrt["tol"].Num(0.1);
                            pass = Math.Abs(got - exp) <= tol;
                            detail = $"Mr({asrt["formula"].Str()}) = {got:F2} (expect {exp})";
                            break;
                        }
                        case "numeric": {
                            /* 纯计算断言：expect 给出期望值，got 由 harness 在外部算好后放进 case */
                            double got = asrt["got"].Num(), exp = asrt["expect"].Num(), tol = asrt["tol"].Num(1e-6);
                            pass = Math.Abs(got - exp) <= tol;
                            detail = $"got={got:G8} expect={exp:G8}";
                            break;
                        }
                        case "var": {
                            /* 断言 compute 产出的中间量 */
                            string vn = asrt["name"].Str();
                            double got;
                            if (!vars.TryGetValue(vn, out got)) { pass = false; detail = "变量不存在: " + vn; break; }
                            double exp = asrt["expect"].Num(), tol = asrt["tol"].Num(1e-6);
                            pass = Math.Abs(got - exp) <= tol;
                            detail = $"{vn} = {got:G8} (expect {exp:G8} ± {tol:G3})";
                            break;
                        }
                        case "var_absent": {
                            string vn = asrt["name"].Str();
                            pass = !vars.ContainsKey(vn);
                            detail = $"{vn} 应不存在";
                            break;
                        }
                        case "var_ge":
                        case "var_le": {
                            string vn = asrt["name"].Str();
                            double got;
                            if (!vars.TryGetValue(vn, out got)) { pass = false; detail = "变量不存在: " + vn; break; }
                            double lim = asrt["limit"].Num();
                            pass = kind == "var_ge" ? got >= lim : got <= lim;
                            detail = $"{vn} = {got:G6} {(kind == "var_ge" ? ">=" : "<=")} {lim:G6}";
                            break;
                        }
                        case "fx_mirror": {
                            pass = v.Fx.Mirror;
                            detail = "银镜 = " + v.Fx.Mirror;
                            break;
                        }
                        case "fx_flag": {
                            string fn = asrt["flag"].Str();
                            bool got = fn == "flame" ? v.Fx.Flame
                                     : fn == "mirror" ? v.Fx.Mirror
                                     : fn == "explode" ? v.Fx.Explode > 0
                                     : false;
                            pass = got;
                            detail = $"{fn} = {got}";
                            break;
                        }
                        case "custom_ok":
                            pass = true; detail = asrt["note"].Str("ok");
                            break;
                        default:
                            pass = false; detail = "未知断言类型: " + kind;
                            break;
                    }
                } catch (Exception ex) {
                    pass = false; detail = "断言抛异常: " + ex.Message;
                }
                r.Put("pass", MiniJson.Bool(pass));
                r.Put("detail", MiniJson.Str(detail));
                r.Put("required", MiniJson.Bool(asrt["required"].Bool(true)));
                asserts.Push(r);
            }

            res.Put("asserts", asserts);
            res.Put("events", events);
            bool allPass = asserts.A.Count(x => x["pass"].Bool()) == asserts.A.Count;
            res.Put("status", MiniJson.Str(allPass ? "PASS" : "FAIL"));
        } catch (Exception e) {
            res.Put("status", MiniJson.Str("error"));
            res.Put("error", MiniJson.Str(e.GetType().Name + ": " + e.Message));
            res.Put("asserts", asserts);
        }
        return res;
    }

    /* ================= 入口 ================= */

    public static void Run() {
        string inPath = null, outPath = null;
        foreach (var a in Environment.GetCommandLineArgs()) {
            if (a.StartsWith("-specIn="))  inPath  = a.Substring(8);
            if (a.StartsWith("-specOut=")) outPath = a.Substring(9);
        }
        if (string.IsNullOrEmpty(inPath) || !File.Exists(inPath)) {
            Debug.LogError("[Spec] 找不到用例文件: " + inPath);
            if (Application.isBatchMode) EditorApplication.Exit(2);
            return;
        }
        var root = MiniJson.Parse(File.ReadAllText(inPath, Encoding.UTF8));
        var cases = root.Kind == JVal.K.Arr ? root.A : new List<JVal> { root };

        var outArr = MiniJson.Arr();
        int pass = 0, fail = 0, err = 0;
        foreach (var cse in cases) {
            var r = RunCase(cse);
            outArr.Push(r);
            string st = r["status"].Str();
            if (st == "PASS") pass++; else if (st == "error") err++; else fail++;
        }
        var wrapper = MiniJson.Obj();
        wrapper.Put("cases", outArr);
        wrapper.Put("summary", MiniJson.Obj()
            .Put("total", MiniJson.Num(cases.Count))
            .Put("pass", MiniJson.Num(pass))
            .Put("fail", MiniJson.Num(fail))
            .Put("error", MiniJson.Num(err)));

        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllText(outPath, MiniJson.Write(wrapper), new UTF8Encoding(false));
        Debug.Log($"[Spec] {cases.Count} 个用例：PASS {pass} / FAIL {fail} / ERROR {err} → {outPath}");
        if (Application.isBatchMode) EditorApplication.Exit(fail + err == 0 ? 0 : 1);
    }
}

}
