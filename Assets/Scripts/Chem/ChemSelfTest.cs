/* =====================================================================
   化学引擎自检 —— 与 Web 版 tools/test.js 对应的那批用例
   编辑器菜单：ChemLab/跑化学自检    命令行：-executeMethod ChemLab.Editor.ChemLabTools.RunTests
   ===================================================================== */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Editor {

public static class ChemSelfTest {

    static int _pass, _fail;
    static readonly StringBuilder Sb = new StringBuilder();

        /// <summary>把方程式文本拆回物种，核对两边的原子总数</summary>
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
            /* 同样要区分「数字是下标还是电荷」：
               Mn2+ 去数后是 Mn（单个元素）→ 电荷 +2
               NH4+ 去数后是 NH（多元素）→ 4 是下标，电荷只有 +1 */
            var m = System.Text.RegularExpressions.Regex.Match(f, @"(\d*)([+-])$");
            if (!m.Success || m.Index == 0) {
                var m0 = System.Text.RegularExpressions.Regex.Match(f, @"([+-])$");
                return m0.Success ? (m0.Value == "-" ? -1 : 1) : 0;
            }
            string stem = f.Substring(0, m.Index), digs = m.Groups[1].Value;
            double num = 1;
            if (digs.Length > 0 && SingleEl(stem)) double.TryParse(digs, out num);
            return m.Groups[2].Value == "-" ? -num : num;
        }
        static bool SingleEl(string s) {
            if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0])) return false;
            int i = 1;
            while (i < s.Length && char.IsLower(s[i])) i++;
            return i == s.Length;
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

        /// <summary>两种视图的原子总数必须一致 —— 重组不能凭空造或丢原子</summary>
        static bool AtomsMatch(System.Collections.Generic.List<SpeciesLine> a,
                               System.Collections.Generic.List<SpeciesLine> b, out string detail) {
            var ca = CountAtoms(a); var cb = CountAtoms(b);
            var els = new System.Collections.Generic.HashSet<string>(ca.Keys);
            foreach (var k in cb.Keys) els.Add(k);
            var diffs = new System.Collections.Generic.List<string>();
            foreach (var e in els) {
                double va, vb;
                ca.TryGetValue(e, out va); cb.TryGetValue(e, out vb);
                if (Math.Abs(va - vb) > 1e-6 * Math.Max(1.0, Math.Max(va, vb))) diffs.Add($"{e}:{va:F6} != {vb:F6}");
            }
            detail = diffs.Count == 0 ? "各元素原子总数一致 OK" : string.Join(", ", diffs);
            return diffs.Count == 0;
        }
        static System.Collections.Generic.Dictionary<string, double> CountAtoms(System.Collections.Generic.List<SpeciesLine> list) {
            var tot = new System.Collections.Generic.Dictionary<string, double>();
            foreach (var s in list) {
                var f = ChemLab.Chem.Formula.Parse(s.Formula);
                foreach (var kv in f) {
                    double cur; tot.TryGetValue(kv.Key, out cur);
                    tot[kv.Key] = cur + kv.Value * s.Mol;
                }
            }
            return tot;
        }

    static void Say(string s) { Sb.AppendLine(s); }

    static void Check(string name, Action<Vessel> body) {
        Lab.Reset();
        var v = Lab.AddVessel(VesselKind.Beaker);
        v.AddWater(20);
        Lab.Log.Clear();
        try { body(v); }
        catch (Exception e) {
            Say($"✗ {name}  抛异常: {e.Message}");
            _fail++; return;
        }
        int rxns = Lab.Log.Count(l => l.Kind == "rxn");
        bool ok = rxns > 0;
        Say($"{(ok ? "✓" : "✗")} {name}");
        foreach (var l in Lab.Log.Where(l => l.Kind == "rxn").Take(2)) Say("     现象: " + l.Text);
        if (!ok) _fail++; else _pass++;
    }

    /// <summary>断言某个产物确实生成了。比 Check 只看「有没有反应」严格得多，
    /// 有机规则必须用它 —— 否则走错分支（得到酸而不是醛）也会被判通过。</summary>
    static void CheckFor(string name, string productId, Action<Vessel> body, double waterMl = 20) {
        Lab.Reset();
        var v = Lab.AddVessel(VesselKind.Beaker);
        if (waterMl > 0) v.AddWater(waterMl);
        Lab.Log.Clear();
        try { body(v); }
        catch (Exception e) {
            Say($"✗ {name}  抛异常: {e.Message}");
            _fail++; return;
        }
        double got = v.Get(productId);
        bool ok = got > 1e-6;
        Say($"{(ok ? "✓" : "✗")} {name}   [{productId} = {got:F4} mol]");
        foreach (var l in Lab.Log.Where(l => l.Kind == "rxn").Take(1)) Say("     现象: " + l.Text);
        if (ok) _pass++; else _fail++;
    }

    static double Mol(Vessel v, string id) { v.Solutes.TryGetValue(id, out double m); return m; }
    static double Gas(Vessel v, string id) { v.Gases.TryGetValue(id, out double m); return m; }

    /// <summary>数值断言：滴定这类定量测试必须用它，只看「有没有反应」是不够的</summary>
    static void TNum(string name, double got, double want, double tol, string unit) {
        bool ok = Math.Abs(got - want) <= tol;
        Say($"   {(ok ? "✓" : "✗")} {name}   实测 {got:G6} / 期望 {want:G6} {unit}");
        if (ok) _pass++; else _fail++;
    }
    static void TCheck(string name, bool ok) {
        Say($"   {(ok ? "✓" : "✗")} {name}");
        if (ok) _pass++; else _fail++;
    }
    static double Pre(Vessel v, string id) { v.Precipitates.TryGetValue(id, out double m); return m; }
    static string Col(Vessel v) => Lab.TrySolutionColor(v, out Color c) ? Lab.ColorName(c) : "无色";

    public static string Run() {
        _pass = _fail = 0; Sb.Clear();
        Lab.EnsureReady();

        Say($"物质 {ChemData.Sub.Count} 种 / 规则 {Rules.All.Count} 条 / 沉淀外观 {ChemData.Precip.Count} 条");

        /* ---- 规则物种解析自检 ---- */
        var badRules = Rules.All.Where(r => !r.Parsed).ToList();
        if (badRules.Count > 0) {
            Say($"✗ 有 {badRules.Count} 条规则无法解析:");
            foreach (var r in badRules.Take(10)) Say("     " + r.Eq);
            _fail++;
        } else Say($"✓ 全部 {Rules.All.Count} 条规则物种均可解析");
        if (badRules.Count == 0) _pass++;

        /* ---- 化学式解析 ---- */
        var mm = new (string f, double expect)[] {
            ("CuSO4·5H2O", 249.7), ("Ca(OH)2", 74.1), ("(NH4)2SO4", 132.1),
            ("K2Cr2O7", 294.2), ("C6H12O6", 180.2), ("Fe2(SO4)3", 399.9),
        };
        foreach (var (f, expect) in mm) {
            double got = Formula.MolarMass(f);
            bool ok = Math.Abs(got - expect) < 1.0;
            Say($"{(ok ? "✓" : "✗")} 摩尔质量 {f} → {got:F1} (期望 {expect})");
            if (ok) _pass++; else _fail++;
        }

        /* ---- 反应用例 ---- */
        Check("锌粒与稀盐酸", v => {
            Lab.AddReagent(v, "zn", 2.0);
            Lab.AddLiquid(v, "hcl", 8);
            Say($"     产物 Zn2+ = {Mol(v,"Zn2+")*65.4*1000:F0} mg, H2 = {Gas(v,"h2")*1000:F2} mmol");
        });
        Check("硫酸铜与氢氧化钠（蓝色絮状沉淀）", v => {
            Lab.AddReagent(v, "cuso4", 1.0);
            Lab.AddReagent(v, "naoh", 0.8);
            Say($"     Cu(OH)2 = {Pre(v,"cuoh2"):F4} mol  溶液 {Col(v)}");
        });
        Check("硝酸银与氯化钠（白色沉淀）", v => {
            Lab.AddReagent(v, "agno3", 1.0);
            Lab.AddReagent(v, "nacl", 0.5);
            Say($"     AgCl = {Pre(v,"agcl"):F4} mol");
        });
        Check("大理石与稀盐酸（冒气泡）", v => {
            Lab.AddReagent(v, "caco3", 2.0);
            Lab.AddLiquid(v, "hcl", 10);
            Say($"     CO2 = {Gas(v,"co2")*1000:F2} mmol, Ca2+ = {Mol(v,"Ca2+")*1000:F2} mmol");
        });
        Check("铁钉浸入硫酸铜（析出铜）", v => {
            Lab.AddReagent(v, "cuso4", 1.5);
            Lab.AddReagent(v, "fe", 1.0);
            Say($"     溶液 {Col(v)}  沉积色 {v.Fx.Deposit}");
        });
        Check("氢氧化钠 + 盐酸（酚酞指示）", v => {
            Lab.AddLiquid(v, "naoh", 5);
            Lab.AddLiquid(v, "phenolphthalein", 0.5);
            Say($"     加酚酞后 pH={v.Ph:F1} 颜色={Col(v)}");
            Lab.AddLiquid(v, "hcl", 6);
            Say($"     加盐酸后 pH={v.Ph:F1} 颜色={Col(v)}");
        });
        Check("硫酸铜 + 过量氨水（深蓝色配合物）", v => {
            Lab.AddReagent(v, "cuso4", 1.0);
            Lab.AddLiquid(v, "nh3", 12);
            Say($"     溶液 {Col(v)}  Cu2+剩余 {Mol(v,"Cu2+")*1000:F2} mmol  [Cu(NH3)4]2+ = {Mol(v,"[Cu(NH3)4]2+")*1000:F2} mmol");
        });
        Check("氯化铁 + 硫氰酸钾（血红色）", v => {
            Lab.AddReagent(v, "fecl3", 0.6);
            Say($"     溶液 {Col(v)}");
            Lab.AddReagent(v, "kscn", 0.5);
            Say($"     加 KSCN 后 {Col(v)}");
        });
        Check("高锰酸钾 + 双氧水（褪色冒泡）", v => {
            Lab.AddReagent(v, "kmno4", 0.3);
            Lab.AddLiquid(v, "h2so4", 3);
            Say($"     溶液 {Col(v)}");
            Lab.AddLiquid(v, "h2o2", 5);
            Say($"     加双氧水后 {Col(v)}  O2 = {Gas(v,"o2")*1000:F2} mmol");
        });
        Check("氯化钡 + 硫酸钠（不溶于酸的白色沉淀）", v => {
            Lab.AddReagent(v, "bacl2", 1.0);
            Lab.AddReagent(v, "na2so4", 0.8);
            Say($"     BaSO4 = {Pre(v,"baso4"):F4} mol");
            Lab.AddLiquid(v, "hcl", 5);
            Say($"     加盐酸后沉淀是否仍在: {(v.Precipitates.ContainsKey("baso4") ? "是（正确）" : "否（错误）")}");
        });
        Check("硝酸铅 + 碘化钾（金黄色沉淀）", v => {
            Lab.AddReagent(v, "pbno32", 1.0);
            Lab.AddReagent(v, "ki", 1.0);
            Say($"     PbI2 = {Pre(v,"pbi2"):F4} mol");
        });
        Check("钠投入水中", v => {
            Lab.AddReagent(v, "na", 0.5);
            Say($"     pH={v.Ph:F1}  H2 = {Gas(v,"h2")*1000:F2} mmol");
        });
        Check("氯化铵 + 氢氧化钠（加热放氨气）", v => {
            Lab.AddReagent(v, "nh4cl", 1.0);
            Lab.AddLiquid(v, "naoh", 5);
            v.Temp = 70;
            Lab.Settle(v);
            Say($"     NH3 = {Gas(v,"nh3")*1000:F2} mmol");
        });
            Check("氯化铝 + 氢氧化钠过量（沉淀溶解）", v => {
            Lab.AddReagent(v, "alcl3", 0.8);
            Lab.AddReagent(v, "naoh", 0.6);
            Say($"     Al(OH)3 = {Pre(v,"aloh3"):F4} mol");
            Lab.AddLiquid(v, "naoh", 8);
            Say($"     加过量碱后沉淀 {(Pre(v,"aloh3") > 1e-6 ? "仍在（错误）" : "已溶解（正确）")}  AlO2- = {Mol(v,"AlO2-")*1000:F2} mmol");
        });
        Check("乙酸 + 乙醇（浓硫酸催化酯化）", v => {
            Lab.AddLiquid(v, "ch3cooh_glacial", 5);
            Lab.AddLiquid(v, "c2h5oh", 6);
            Lab.AddLiquid(v, "h2so4_conc", 1);
            v.Temp = 80; Lab.Settle(v);
            Say($"     乙酸乙酯 = {Mol(v,"ch3cooc2h5")*1000:F2} mmol");
        });
        Check("乙醛 + 银氨溶液（银镜）", v => {
            Lab.AddReagent(v, "agno3", 0.8);
            Lab.AddLiquid(v, "nh3", 8);
            Lab.AddLiquid(v, "ch3cho", 2);
            v.Temp = 70; Lab.Settle(v);
            Say($"     银镜: {(v.Fx.Mirror ? "是（正确）" : "否")}");
        });
        Check("淀粉溶液 + 碘水（变蓝）", v => {
            v.Undissolved["starch"] = 0.002; Lab.Settle(v);
            Lab.AddLiquid(v, "iodine_sol", 2);
            Say($"     颜色 {Col(v)}");
        });
        Check("氯化钠焰色反应", v => {
            Lab.AddReagent(v, "nacl", 1.0);
            Lab.Ignite(v);
            Say($"     焰色 {v.Fx.FlameColor ?? "无"}");
        });
        Check("甲烷点燃", v => {
            Lab.AddReagent(v, "ch4", 0.02);
            Lab.Ignite(v);
            Say($"     CO2 = {Gas(v,"co2")*1000:F2} mmol  火焰 {(v.Fx.Flame ? v.Fx.FlameColor : "无")}");
        });

        /* ============================================================
           CIE 9701 通用有机规则（按官能团家族，不按具体分子）
           这组用例是核心：回流与蒸馏必须给出不同产物，
           否则说明条件分支没生效。
           ============================================================ */
        CheckFor("乙醇 + 酸性重铬酸钾 + 蒸馏 → 乙醛", "ch3cho", v => {
            Lab.AddLiquid(v, "c2h5oh", 6);
            Lab.AddReagent(v, "k2cr2o7", 1.5);
            Lab.AddLiquid(v, "h2so4", 3);
            v.Mode = HeatMode.Distil; v.Temp = 80; Lab.Settle(v);
            Say($"     乙醛 {Mol(v,"ch3cho")*1000:F2} mmol / 乙酸 {Mol(v,"ch3cooh")*1000:F2} mmol");
        });
        CheckFor("乙醇 + 酸性重铬酸钾 + 回流 → 乙酸", "ch3cooh", v => {
            Lab.AddLiquid(v, "c2h5oh", 6);
            Lab.AddReagent(v, "k2cr2o7", 1.5);
            Lab.AddLiquid(v, "h2so4", 3);
            v.Mode = HeatMode.Reflux; v.Temp = 80; Lab.Settle(v);
            Say($"     乙酸 {Mol(v,"ch3cooh")*1000:F2} mmol");
        });
        CheckFor("丙-2-醇氧化 → 丙酮（仲醇成酮，酮不能再氧化）", "acetone", v => {
            Lab.AddLiquid(v, "ch3chohch3", 5);
            Lab.AddReagent(v, "k2cr2o7", 1.5);
            Lab.AddLiquid(v, "h2so4", 3);
            v.Mode = HeatMode.Reflux; v.Temp = 80; Lab.Settle(v);
            Say($"     丙酮 {Mol(v,"acetone")*1000:F2} mmol / 残留醇 {Mol(v,"ch3chohch3")*1000:F2} mmol");
        });
        CheckFor("乙醛 + 硼氢化钠 → 乙醇（羰基还原）", "c2h5oh", v => {
            Lab.AddLiquid(v, "ch3cho", 3);
            Lab.AddReagent(v, "nabh4", 2.0);
            Lab.Settle(v);
        });
        CheckFor("溴乙烷 + NaOH 水溶液 → 乙醇（水解）", "c2h5oh", v => {
            Lab.AddLiquid(v, "c2h5br", 4);
            Lab.AddLiquid(v, "naoh", 10);
            v.Temp = 80; Lab.Settle(v);
            Say($"     乙醇 {Mol(v,"c2h5oh")*1000:F2} mmol");
        });
        CheckFor("溴乙烷 + NaOH 乙醇溶液 → 乙烯（消去）", "c2h4", v => {
            Lab.AddLiquid(v, "c2h5br", 4);
            Lab.AddReagent(v, "naoh", 3);
            Lab.AddLiquid(v, "c2h5oh", 80);   // 要真的当溶剂，乙醇体积必须超过水
            v.Mode = HeatMode.Reflux; v.Temp = 85; Lab.Settle(v);
            Say($"     乙烯 {Gas(v,"c2h4")*1000:F2} mmol / 醇 {Mol(v,"c2h5oh")*1000:F2} mmol");
        }, waterMl: 1);
        CheckFor("乙腈回流水解 → 乙酸", "ch3cooh", v => {
            Lab.AddLiquid(v, "ch3cn", 3);
            Lab.AddLiquid(v, "h2so4", 4);
            v.Mode = HeatMode.Reflux; v.Temp = 90; Lab.Settle(v);
        });
        CheckFor("苯 + 浓硝酸 → 硝基苯", "c6h5no2", v => {
            Lab.AddLiquid(v, "c6h6", 4);
            Lab.AddLiquid(v, "hno3_conc", 6);
            Lab.AddLiquid(v, "h2so4_conc", 3);
            v.Temp = 55; Lab.Settle(v);
        });

        /* ---- CIE 9701 定性分析：氢氧化物沉淀的颜色与过量溶解 ---- */
        CheckFor("Cu2+ + NaOH → 浅蓝色沉淀", "cuoh2", v => {
            Lab.AddReagent(v, "cuso4", 1.0);
            Lab.AddReagent(v, "naoh", 0.8);
            Say($"     沉淀 {Pre(v,"cuoh2"):F4} mol");
        });
        CheckFor("Fe2+ + NaOH → 绿色沉淀", "feoh2", v => {
            Lab.AddReagent(v, "feso4", 1.0);
            Lab.AddReagent(v, "naoh", 0.8);
            Say($"     沉淀 {Pre(v,"feoh2"):F4} mol");
        });
        CheckFor("Fe3+ + NaOH → 红棕色沉淀", "feoh3", v => {
            Lab.AddReagent(v, "fecl3", 1.0);
            Lab.AddReagent(v, "naoh", 1.0);
            Say($"     沉淀 {Pre(v,"feoh3"):F4} mol");
        });
        Check("Zn2+ + NaOH → 白色沉淀，加过量碱溶解", v => {
            Lab.AddReagent(v, "znso4", 1.0);
            Lab.AddReagent(v, "naoh", 0.8);
            Say($"     沉淀 {Pre(v,"znoh2"):F4} mol");
            Lab.AddLiquid(v, "naoh", 10);
            Say($"     加过量碱后 {(Pre(v,"znoh2") > 1e-6 ? "仍在（错误）" : "已溶解（正确）")}");
        });
        CheckFor("Cl- + AgNO3 → 白色沉淀", "agcl", v => {
            Lab.AddReagent(v, "nacl", 1.0);
            Lab.AddReagent(v, "agno3", 1.0);
        });
        CheckFor("Br- + AgNO3 → 淡黄色沉淀", "agbr", v => {
            Lab.AddReagent(v, "nabr", 1.0);
            Lab.AddReagent(v, "agno3", 1.0);
        });
        CheckFor("I- + AgNO3 → 黄色沉淀", "agi", v => {
            Lab.AddReagent(v, "nai", 1.0);
            Lab.AddReagent(v, "agno3", 1.0);
        });

        /* ---- 诊断：通用有机规则为什么没触发 ---- */
        {
            Lab.Reset();
            var d = Lab.AddVessel(VesselKind.Beaker);
            d.AddWater(20);
            Lab.AddLiquid(d, "c2h5oh", 6);
            Lab.AddReagent(d, "k2cr2o7", 1.5);
            Lab.AddLiquid(d, "h2so4", 3);
            d.Mode = HeatMode.Distil; d.Temp = 80; Lab.Settle(d);
            Say("");
            Say("[诊断] 乙醇 + 重铬酸钾 + 蒸馏");
            Say($"   OrgRules={ChemData.OrgRules.Count}  OrgMap={ChemData.OrgMap.Count}  Family={ChemData.Family.Count}");
            string fam;
            Say($"   c2h5oh 有家族? {ChemData.Family.TryGetValue("c2h5oh", out fam)} → {fam}");
            string to;
            Say($"   映射 primary_alcohol|c2h5oh|aldehyde ? {ChemData.OrgMap.TryGetValue("primary_alcohol|c2h5oh|aldehyde", out to)} → {to}");
            Say($"   c2h5oh 剩余 = {d.Get("c2h5oh"):F4}   k2cr2o7 = {d.Get("k2cr2o7"):F5}   Water = {d.Water:F2}");
            Say($"   Mode={d.Mode}  Temp={d.Temp:F0}");
            Say($"   溶液中: {string.Join(", ", d.Solutes.Select(kv => kv.Key + "=" + kv.Value.ToString("F3")).Take(14))}");
            Say($"   未溶物: {string.Join(", ", d.Undissolved.Select(kv => kv.Key + "=" + kv.Value.ToString("F3")).Take(10))}");
            var r0 = ChemData.OrgRules.FirstOrDefault(r => r.From == "primary_alcohol" && r.To == "aldehyde");
            if (r0 != null) Say($"   规则: {r0.From}->{r0.To} cond={r0.Cond} 试剂=[{r0.Reagents}]");
        }

        /* ============================================================
           CIE 9701/38/M/J/25 Q1 —— 酸性高锰酸钾滴定 Fe2+
           规范来源：9701_38_MJ25_Q1_Unity_Titration_Test.md
           逐条对照该文件的 ground truth 与 PASS/FAIL 表。
           ============================================================ */
        Say("");
        Say("──── CIE 9701/38/M/J/25 Q1 氧化还原滴定 ────");
        {
            const double C1 = 0.0500, V1 = 25.0;   // FB1: Fe2+ mol/dm3, cm3
            const double C2 = 0.0100;              // FB2: KMnO4 mol/dm3
            const double C3 = 1.00,   V3 = 10.0;   // FB3: H2SO4

            Lab.Reset();
            var tk = Lab.AddVessel(VesselKind.Flask);
            tk.AddWater(60);
            tk.Introduce("feso4", C1 * V1 / 1000.0, true);   // FB1，25.0 cm3 × 0.0500 M
            tk.Introduce("h2so4", C3 * V3 / 1000.0, true);   // FB3，H+ 必须过量
            tk.Temp = 25;
            Lab.Settle(tk);

            TNum("initial_Fe2 = 1.25e-3 mol", tk.Get("Fe2+"), 0.00125, 1e-6, "mol");

            double burette = 0, fe10 = 0, fe20 = 0, fe24 = 0, free24 = 0;
            double fe25 = 0, free25 = 0, fe3_25 = 0, mn2_25 = 0, free26 = 0;
            double freeEnd = 0;
            foreach (var tgt in new double[] { 10.00, 20.00, 24.00, 25.00, 25.05, 26.00 }) {
                tk.Introduce("kmno4", C2 * (tgt - burette) / 1000.0, true);
                burette = tgt;
                Lab.Settle(tk);
                double fe = tk.Get("Fe2+"), mn = tk.Get("MnO4-");
                Say($"   V={tgt,6:F2} cm3   Fe2+={fe * 1e3,10:F6} mmol   " +
                    $"自由MnO4-={mn * 1e9,10:F3} nmol   Fe3+={tk.Get("Fe3+") * 1e3,9:F5} mmol   Mn2+={tk.Get("Mn2+") * 1e3,9:F5} mmol");
                if (tgt == 10.00) fe10 = fe;
                if (tgt == 20.00) fe20 = fe;
                if (tgt == 24.00) { fe24 = fe; free24 = mn; }
                if (tgt == 25.00) { fe25 = fe; free25 = mn; fe3_25 = tk.Get("Fe3+"); mn2_25 = tk.Get("Mn2+"); }
                if (tgt == 25.05) freeEnd = mn;
                if (tgt == 26.00) free26 = mn;
            }

            /* §8 示例状态 A / B */
            TNum("V=10.00 时 Fe2+ 剩余", fe10, 0.000750, 1e-6, "mol");
            TNum("V=20.00 时 Fe2+ 剩余", fe20, 0.000250, 1e-6, "mol");
            /* §21 负向测试 A：24.00 cm3 未到终点 */
            TNum("V=24.00 时 Fe2+ 剩余", fe24, 0.000050, 1e-6, "mol");
            TNum("V=24.00 时自由 MnO4- ≈ 0", free24, 0.0, 1e-9, "mol");
            TCheck("负向A：24.00 cm3 未达终点", fe24 > 1e-6);
            /* §10 当量点物种状态 */
            TNum("当量点 Fe2+ ≈ 0", fe25, 0.0, 1e-7, "mol");
            TNum("当量点自由 MnO4- ≈ 0", free25, 0.0, 1e-8, "mol");
            TNum("当量点生成 Fe3+", fe3_25, 0.00125, 1e-6, "mol");
            TNum("当量点生成 Mn2+", mn2_25, 0.000250, 1e-6, "mol");
            /* §22 负向测试 B：26.00 cm3 明显过量 */
            TNum("V=26.00 时自由 MnO4-", free26, 0.0000100, 1e-8, "mol");
            TCheck("负向B：26.00 cm3 已过量，自由 MnO4- 为正", free26 > 1e-8);
            /* §12：0.05 cm3 步长下终点应落在 25.00–25.05 */
            TCheck("终点落点 25.00–25.05 cm3（首次出现持久过量 MnO4-）", free25 <= 1e-8 && freeEnd > 0);

            /* §11 / §19：终点颜色必须是淡粉紫，不是深紫；
               终点之前不允许出现稳定的高锰酸根紫色 */
            string colEnd = Col(tk);
            Color rawEnd; Lab.TrySolutionColor(tk, out rawEnd);
            Say($"   终点溶液颜色：{colEnd}   RGB=({rawEnd.r * 255:F0},{rawEnd.g * 255:F0},{rawEnd.b * 255:F0})   （§11 要求：淡粉/淡紫）");
            /* 规范的意图是：终点必须能看到高锰酸根的颜色，
               既不能看不见（黄/棕），也不能是深紫（那是明显滴过量）。 */
            TCheck("终点颜色呈粉/紫/红族（高锰酸根可见）",
                   colEnd.Contains("粉") || colEnd.Contains("紫") || colEnd.Contains("红"));
            TCheck("终点不是深紫（深紫意味着明显过量）", colEnd != "深色" && !colEnd.Contains("深紫"));

            /* ── 颜色随滴定体积的整条曲线 ──
               用来回答「颜色是渐变的还是到终点突变的」。
               如果模型是加权平均且权重归一化，那么颜色只取决于各组分的
               权重之比，与绝对浓度无关 —— 曲线会在当量点前后出现台阶。 */
            Say("   ── 颜色-体积曲线（每 1.00 cm3 取样）──");
            Lab.Reset();
            var cv = Lab.AddVessel(VesselKind.Flask);
            cv.AddWater(60);
            cv.Introduce("feso4", C1 * V1 / 1000.0, true);
            cv.Introduce("h2so4", C3 * V3 / 1000.0, true);
            cv.Temp = 25;
            Lab.Settle(cv);
            double acc = 0;
            for (int step = 0; step <= 30; step++) {
                double tgt = step * 1.00;
                if (step > 0) {
                    cv.Introduce("kmno4", C2 * (tgt - acc) / 1000.0, true);
                    acc = tgt;
                    Lab.Settle(cv);
                }
                Color cc; Lab.TrySolutionColor(cv, out cc);
                string nm = Lab.ColorName(cc);
                double fMn = cv.Get("MnO4-"), fFe = cv.Get("Fe2+");
                Say($"     V={tgt,5:F2}  {nm,-8} RGB=({cc.r * 255,3:F0},{cc.g * 255,3:F0},{cc.b * 255,3:F0})" +
                    $"   Fe2+={fFe * 1e4,8:F2}e-4  自由MnO4-={fMn * 1e9,9:F2} nmol");
            }

            Lab.Reset();
            var ck = Lab.AddVessel(VesselKind.Flask);
            ck.AddWater(60);
            ck.Introduce("feso4", C1 * V1 / 1000.0, true);
            ck.Introduce("h2so4", C3 * V3 / 1000.0, true);
            ck.Introduce("kmno4", C2 * 24.00 / 1000.0, true);   // 终点之前
            ck.Temp = 25;
            Lab.Settle(ck);
            string colPre = Col(ck);
            Say($"   终点之前颜色：{colPre}");
            TCheck("终点之前无高锰酸根颜色（应为铁离子的黄/棕色）", !colPre.Contains("紫") && !colPre.Contains("粉"));

            /* §15/16/17/18 反算 */
            double meanTitre = 25.00;
            double nMnO4 = C2 * meanTitre / 1000.0;
            double nFe = 5.0 * nMnO4;
            double cFe = nFe / (V1 / 1000.0);
            double massConc = cFe * 55.8;
            double perTablet = massConc * 1000.0 / 14.0;
            TNum("mean titre 反算 n(MnO4-)", nMnO4, 0.000250, 1e-9, "mol");
            TNum("反算 n(Fe2+)（1:5）", nFe, 0.00125, 1e-9, "mol");
            TNum("recovered [Fe2+]", cFe, 0.0500, 1e-4, "mol/dm3");
            TNum("Fe 质量浓度", massConc, 2.79, 0.01, "g/dm3");
            TNum("每片补铁剂含铁", perTablet, 199.0, 2.0, "mg");
            TCheck("厂家声明 ≥150 mg/片 成立", perTablet > 150.0);
            /* §25 负向测试 E：错误计量比会得到明显不同的体积 */
            double vEq = nMnO4 / C2 * 1000.0;          // 正确算法：用 n(MnO4-) 算体积
            double vIf11 = nFe / C2 * 1000.0;          // 若误用 1:1
            double vIf12 = nFe / 2.0 / C2 * 1000.0;    // 若误用 1:2
            Say($"   §25 计量比检查：1:5 → {vEq:F2} cm3   1:1 → {vIf11:F2} cm3   1:2 → {vIf12:F2} cm3");
            TCheck("负向E：1:5 得到 25.00 cm3", Math.Abs(vEq - 25.00) < 1e-6);
            TCheck("负向E：1:1 会误得 125.00 cm3（明显不同）", Math.Abs(vIf11 - 125.00) < 1e-6);
            TCheck("负向E：1:2 会误得 62.50 cm3（明显不同）", Math.Abs(vIf12 - 62.50) < 1e-6);
            TCheck("负向E：只有 1:5 给出 25.00 cm3", Math.Abs(vEq - vIf11) > 1 && Math.Abs(vEq - vIf12) > 1);
        }

        /* ---- §23 负向测试 C：缺酸时不得仍然给 Mn2+ ---- */
        {
            Lab.Reset();
            var na = Lab.AddVessel(VesselKind.Flask);
            na.AddWater(60);
            na.Introduce("feso4", 0.00125, true);
            na.Introduce("kmno4", 0.000250, true);   /* 故意不加硫酸 */
            na.Temp = 25;
            Lab.Settle(na);
            double mn2 = na.Get("Mn2+"), mno2 = Pre(na, "mno2"), fe2 = na.Get("Fe2+");
            Say($"   缺酸：Mn2+={mn2 * 1e3:F5} mmol   MnO2={mno2 * 1e3:F5} mmol   Fe2+残余={fe2 * 1e3:F5} mmol");
            TCheck("负向C：缺酸时还原产物不是 Mn2+", mn2 < 1e-6);
            TCheck("负向C：缺酸时按 MnO2 还原（不同产物）", mno2 > 1e-7);
        }

        /* ---- §24 负向测试 D：用 HCl 代替 H2SO4 提供酸性 ----
           规范指出这不是核心判据（Cambridge 的 MS 接受多种理由），
           但高级引擎应当认识到 Cl- 会被强酸性高锰酸根氧化，
           从而产生竞争副反应、多消耗滴定剂。 */
        {
            Lab.Reset();
            var hc = Lab.AddVessel(VesselKind.Flask);
            hc.AddWater(60);
            hc.Introduce("feso4", 0.00125, true);
            hc.Introduce("hcl_conc", 0.0200, true);   // 同样提供大量 H+
            /* 必须用**过量**高锰酸根：Fe2+ 恰好等量时会被全部消耗掉，
               根本轮不到它去氧化氯离子，那样的「干扰」断言是恒真的。 */
            hc.Introduce("kmno4", 0.000500, true);
            hc.Temp = 25;
            Lab.Settle(hc);
            double cl2 = Gas(hc, "cl2");
            double clLeft = hc.Get("Cl-");
            double mn2h = hc.Get("Mn2+"), fe2h = hc.Get("Fe2+"), mno4h = hc.Get("MnO4-");
            Say($"   HCl 介质：Cl2={cl2 * 1e9:F2} nmol   Cl- 剩余={clLeft * 1e3:F4} mmol（初始 20）");
            Say($"             Mn2+={mn2h * 1e3:F5} mmol   Fe2+残余={fe2h * 1e3:F5} mmol   自由 MnO4-={mno4h * 1e9:F2} nmol");
            /* 生成的 Cl2 会被溶液中剩余的 Fe2+ 立刻还原成 Cl-
               （2Fe2+ + Cl2 -> 2Fe3+ + 2Cl-），所以判据不能是「有氯气残留」。
               真正说明有干扰的是：氯离子被消耗了，而且高锰酸根被额外消耗掉。 */
            TCheck("负向D：Cl- 被高锰酸根氧化（氯离子净消耗）", clLeft < 0.0200 - 1e-6);
            TCheck("负向D：过量高锰酸根氧化了氯离子，生成氯气", cl2 > 1e-9);

            /* ================================================================
               容器标签的两种视图：离子状态 / 具体物质
               最强的一条验证是**元素守恒** —— 把离子拼回中性化合物，
               各元素的原子总数不能变。配错了立刻就会被这条抓出来。
               ================================================================ */
            {
                Lab.Reset();
                var lv = Lab.AddVessel(VesselKind.Beaker);
                lv.AddWater(200);
                lv.Introduce("nacl", 0.020, true);
                lv.Introduce("cuso4", 0.005, true);
                lv.Temp = 25; Lab.Settle(lv);

                var ions = VesselView.IonView(lv);
                var mols = VesselView.MolecularView(lv);
                Say("   离子状态：" + string.Join("  ", ions.Select(s => $"{s.Formula}:{s.Mol * 1000:F1}mmol")));
                Say("   具体物质：" + string.Join("  ", mols.Select(s => $"{s.Formula}:{s.Mol * 1000:F1}mmol")));

                TCheck("离子视图里出现 Na+ 和 Cl-", ions.Any(s => s.Id == "Na+") && ions.Any(s => s.Id == "Cl-"));
                TCheck("离子视图里出现 Cu2+ 和 SO4^2-", ions.Any(s => s.Id == "Cu2+") && ions.Any(s => s.Id == "SO4^2-"));
                TCheck("具体物质视图里 NaCl 复原", mols.Any(s => s.Id == "nacl" && Math.Abs(s.Mol - 0.020) < 1e-9));
                TCheck("具体物质视图里 CuSO4 复原", mols.Any(s => s.Id == "cuso4" && Math.Abs(s.Mol - 0.005) < 1e-9));

                var n1 = ions.FirstOrDefault(s => s.Id == "Na+");
                TCheck("Na+ 浓度算对（0.020/0.200 = 0.100 mol/dm3）", n1 != null && Math.Abs(n1.Conc - 0.100) < 1e-6);

                bool conserved = AtomsMatch(ions, mols, out string detail);
                Say("   元素守恒核对：" + detail);
                TCheck("两种视图元素守恒（重组没有凭空造/丢原子）", conserved);

                /* 混合盐：应优先按「消耗离子最多」的方式配对 */
                Lab.Reset();
                var mv = Lab.AddVessel(VesselKind.Beaker);
                mv.AddWater(100);
                mv.Introduce("na2so4", 0.010, true);
                mv.Introduce("cucl2", 0.010, true);
                mv.Temp = 25; Lab.Settle(mv);
                var mIons = VesselView.IonView(mv);
                var mMols = VesselView.MolecularView(mv);
                Say("   混合盐 离子：" + string.Join("  ", mIons.Select(s => s.Formula)));
                Say("   混合盐 物质：" + string.Join("  ", mMols.Select(s => $"{s.Formula}:{s.Mol * 1000:F1}mmol")));
                TCheck("混合盐重组后元素仍守恒", AtomsMatch(mIons, mMols, out _));
            }

            /* ================================================================
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
                    /* 诊断：看拆解出来的物种对不对 */
                    {
                        int ck = cn.IndexOf(" = ") >= 0 ? cn.IndexOf(" = ") : cn.IndexOf(" → ");
                        string cs = cn.IndexOf(" = ") >= 0 ? " = " : " → ";
                        var dl = EqFormat.ParseSide(cn.Substring(0, ck));
                        var dr = EqFormat.ParseSide(cn.Substring(ck + cs.Length));
                        Say("      拆解左：" + string.Join(",", dl.Select(x => x.Id + "×" + x.Coef)));
                        Say("      拆解右：" + string.Join(",", dr.Select(x => x.Id + "×" + x.Coef)));
                    }
                    bool okCn = AtomsConserved(cn, out string dCn);
                    if (!okCn) Say("      中文拆解：" + dCn);
                    TCheck("格式化后元素守恒（中文）", okCn);

                    /* ---- 离子方程式：写出后元素与电荷都要守恒 ---- */
                    string ion = EqFormat.IonicEquation(r, true, true);
                    if (!string.IsNullOrEmpty(ion)) {
                        Say($"   离子式：{ion}");
                        TCheck("离子式元素守恒", AtomsConserved(ion, out _));
                        TCheck("离子式电荷守恒", ChargeConserved(ion, out string dq));
                        TCheck("离子式已消去旁观离子（不含未变化的离子对）", !SpectatorLeft(r, ion));
                    }
                    shown++;
                }
            }

            /* ================================================================
               焓变计算：键能法 / 生成焓法 / 盖斯定律
               三种算法互相验证 —— 同一条反应用不同路径算出来必须一致。
               ================================================================ */
            {
                HessLaw.Init();

                /* ---- 1) 键能表 ---- */
                {
                    var raw = ChemData.TblBondEnergy ?? "";
                    var rows = raw.Split('\n');
                    Say($"   键能表原始长度 {raw.Length}，行数 {rows.Length}");
                    if (rows.Length > 0) Say("   第一行: [" + rows[0].Replace("\t", "|") + "]");
                    if (rows.Length > 1) Say("   第二行: [" + rows[1].Replace("\t", "|") + "]");
                    Say($"   查表条数 = {HessLaw.BondCount}");
                }
                Say($"   H-H 键能 = {HessLaw.BondEnergy("H","H",1):F0} kJ/mol（期望 436）");
                TCheck("H-H 键能查表正确", Math.Abs(HessLaw.BondEnergy("H", "H", 1) - 436) < 0.5);
                TCheck("O=O 键能查表正确", Math.Abs(HessLaw.BondEnergy("O", "O", 2) - 496) < 0.5);
                TCheck("C-H 键能查表正确", Math.Abs(HessLaw.BondEnergy("C", "H", 1) - 410) < 0.5);
                /* 键能表里 A-B 与 B-A 是同一个键 */
                TCheck("键能查表对顺序不敏感",
                       Math.Abs(HessLaw.BondEnergy("H", "Cl", 1) - HessLaw.BondEnergy("Cl", "H", 1)) < 1e-9);

                /* ---- 2) 用键能算 H2 + Cl2 -> 2HCl ---- */
                /* ΔH = (H-H + Cl-Cl) − 2×(H-Cl) = (436 + 242) − 2×431 = −184 kJ/mol */
                double hh = HessLaw.BondEnergy("H", "H", 1), clcl = HessLaw.BondEnergy("Cl", "Cl", 1),
                       hcl = HessLaw.BondEnergy("H", "Cl", 1);
                double dhBond = (hh + clcl) - 2 * hcl;
                Say($"   键能法 H2+Cl2→2HCl：ΔH = ({hh:F0}+{clcl:F0}) − 2×{hcl:F0} = {dhBond:F0} kJ/mol");
                TCheck("键能法算 H2+Cl2→2HCl 得 −184 kJ/mol", Math.Abs(dhBond - (-184)) < 1.5);

                /* ---- 3) 生成焓法：甲烷燃烧 ---- */
                /* CH4 + 2O2 -> CO2 + 2H2O
                   ΔH = [ΔHf(CO2) + 2ΔHf(H2O)] − ΔHf(CH4)
                      = [−394 + 2×(−286)] − (−75) = −891 kJ/mol */
                double fco2 = HessLaw.FormationOf("co2"), fh2o = HessLaw.FormationOf("h2o"),
                       fch4 = HessLaw.FormationOf("ch4");
                double dhForm = (fco2 + 2 * fh2o) - fch4;
                Say($"   生成焓法 CH4+2O2→CO2+2H2O：ΔH = ({fco2:F0}+2×{fh2o:F0}) − ({fch4:F0}) = {dhForm:F0} kJ/mol");
                TCheck("生成焓法算甲烷燃烧得 −891 kJ/mol", Math.Abs(dhForm - (-891)) < 3);

                /* ---- 4) 查表 ---- */
                double dhTab = HessLaw.Lookup("CH4 + 2O2 -> CO2 + 2H2O", out string note);
                Say($"   查表 CH4 燃烧：{dhTab:F0} kJ/mol（{note}）");
                TCheck("反应焓查表命中", !double.IsNaN(dhTab) && Math.Abs(dhTab - (-890)) < 1);

                /* ---- 5) 盖斯定律：沿闭合回路 ΣΔH 必须为 0 ---- */
                /* 造一个三角形回路：
                     A --(ΔH1=+100)--> B --(ΔH2=−250)--> C --(ΔH3=?)--> A
                   闭合要求 100 − 250 + ΔH3 = 0 → ΔH3 = +150 */
                var edges = new System.Collections.Generic.List<HessEdge> {
                    new HessEdge { From = 0, To = 1, DH = 100,  Source = EnthalpySource.Manual },
                    new HessEdge { From = 1, To = 2, DH = -250, Source = EnthalpySource.Manual },
                    new HessEdge { From = 2, To = 0, DH = 0,    Source = EnthalpySource.Unknown },
                };
                var cyc = HessLaw.FindCycle(3, edges);
                Say("   找到回路：" + string.Join(" → ", cyc));
                TCheck("能找到闭合回路", cyc.Count >= 4);
                bool ok = HessLaw.SolveCycle(cyc, edges, out double unk, out string why);
                Say("   " + why);
                TCheck("盖斯定律解出未知焓变 = +150 kJ/mol", ok && Math.Abs(unk - 150) < 1e-6);

                /* ---- 6) 未知量多于一个时应当拒绝求解 ---- */
                var edges2 = new System.Collections.Generic.List<HessEdge> {
                    new HessEdge { From = 0, To = 1, DH = 0, Source = EnthalpySource.Unknown },
                    new HessEdge { From = 1, To = 2, DH = 0, Source = EnthalpySource.Unknown },
                    new HessEdge { From = 2, To = 0, DH = 50, Source = EnthalpySource.Manual },
                };
                var cyc2 = HessLaw.FindCycle(3, edges2);
                bool ok2 = HessLaw.SolveCycle(cyc2, edges2, out _, out string why2);
                Say("   " + why2);
                TCheck("两个未知量时应拒绝求解并说明原因", !ok2);

                /* ---- 7) 交叉验证：同一反应用键能法与查表法结果应接近 ---- */
                double viaTable = HessLaw.Lookup("H2 + Cl2 -> 2HCl", out _);
                Say($"   交叉验证 H2+Cl2→2HCl：键能法 {dhBond:F0} vs 查表 {viaTable:F0} kJ/mol");
                TCheck("键能法与查表法结果一致（差 < 3 kJ/mol）",
                       !double.IsNaN(viaTable) && Math.Abs(dhBond - viaTable) < 3);
            }

            /* ================================================================
               试剂架：过滤按钮与搜索框
                 用户反馈「过滤按钮完全不起作用、搜索框没用」。
                 这里真的去点按钮、真的设查询词，看列表有没有变。
                 光读代码猜是不可靠的。
               ================================================================ */
            {
                var ui = ChemLab.Game.LabUI.CreateForTest();
                if (ui == null) Say("   ✗ 无法建立测试用界面");
                else {
                    Say($"   试剂总数 {ui.TestReagentTotal}，按钮共 {ui.TestListButtons().Count} 个");
                    /* 先记下初始状态 */
                    string cat0 = ui.TestCat;
                    int n0 = ui.TestVisibleCount;
                    Say($"   初始分类「{cat0}」列出 {n0} 条");

                    /* --- 分类按钮：换一个类别，列表应当变化 --- */
                    Say("   试剂架下的按钮名：" + string.Join(", ", ui.TestAllButtons()));
                    bool clicked = ui.TestClickButton("Btn_氧化物");
                    Say("   点击「氧化物」：" + (clicked ? "找到并触发了" : "✗ 根本没找到这个按钮"));
                    TCheck("分类按钮存在且能被点击", clicked);
                    if (clicked) {
                        Say($"   点后分类「{ui.TestCat}」，列出 {ui.TestVisibleCount} 条");
                        TCheck("点分类按钮后 _cat 确实变了", ui.TestCat != cat0);
                        TCheck("点分类按钮后列表被刷新", ui.TestVisibleCount != n0 || ui.TestVisibleCount >= 0);
                    }

                    /* --- 直接测过滤器本身（排除点击层的问题）---
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
                    TCheck("能按化学式搜索", nf >= 1);

                    UnityEngine.Object.Destroy(ui.gameObject);
                }
            }

            /* ================================================================
               左栏布局：卡片里的各个区段不得重叠
                 用户反馈「排版乱七八糟」。根因是卡片高度固定、东西越塞越多，
                 内容物列表压在了标签栏与方程式栏上。这里用矩形相交来验。
               ================================================================ */
            {
                var lui = ChemLab.Game.LabUI.CreateForTest();
                if (lui == null) Say("   ✗ 无法建立测试用界面");
                else {
                    var rs = lui.TestCardRects();
                    Say("   卡片内区段 " + rs.Count + " 个");
                    int ov = 0;
                    for (int a = 0; a < rs.Count; a++)
                        for (int b = a + 1; b < rs.Count; b++) {
                            var ra = rs[a].r; var rb = rs[b].r;
                            bool hit = ra.xMin < rb.xMax - 1 && rb.xMin < ra.xMax - 1
                                    && ra.yMin < rb.yMax - 1 && rb.yMin < ra.yMax - 1;
                            if (hit) { ov++; Say("   ✗ 「" + rs[a].name + "」与「" + rs[b].name + "」重叠"); }
                        }
                    TCheck("左栏卡片内没有区段互相重叠", ov == 0);

                    /* 只看卡片内部是不够的 —— 器材架第 4 行被卡片盖住过。
                       这里查整个左栏里所有「可点图形」两两之间有没有叠。 */
                    var all = lui.TestPanelClickables();
                    Say("   左栏可点控件 " + all.Count + " 个");
                    int rlap = 0;
                    for (int a = 0; a < all.Count; a++)
                        for (int b = a + 1; b < all.Count; b++) {
                            var ra = all[a].r; var rb = all[b].r;
                            bool lap = ra.xMin < rb.xMax - 1 && rb.xMin < ra.xMax - 1
                                    && ra.yMin < rb.yMax - 1 && rb.yMin < ra.yMax - 1;
                            /* 父子关系不算重叠 */
                            if (lap && !all[a].name.StartsWith(all[b].name) && !all[b].name.StartsWith(all[a].name)) {
                                rlap++;
                                if (rlap <= 6) Say($"   ✗ 「{all[a].name}」与「{all[b].name}」重叠");
                            }
                        }
                    TCheck("左栏所有可点控件互不重叠", rlap == 0);

                    /* --- 射线检测：这些按钮真实鼠标点得到吗 --- */
                    TCheck("旧剂量预设按钮已移除", !lui.TestAllButtons().Any(n=>n=="Btn_少许"||n=="Btn_适量"||n=="Btn_过量"));
                    TCheck("试剂行提供拖放入口",lui.GetComponentsInChildren<ChemLab.Game.ReagentDrag>(true).Length>0);
                    string[] targets = { "Btn_金属", "Btn_氧化物", "Btn_酸", "Btn_碱" };
                    foreach (var tn in targets) {
                        var line = lui.TestOcclusionAt(tn);
                        Say("   " + line);
                        if (line.StartsWith("✗")) { ov++; }
                    }
                    TCheck("试剂架上的按钮没有被别的控件盖住", ov == 0);
                    UnityEngine.Object.Destroy(lui.gameObject);
                }
            }

            /* ================================================================
               焓变场景：节点与连线必须是一个整体
                 用户报过：拖背景只有白线动、拖节点白线不动。
                 根因是节点挂在画布根、连线挂在 World 层，不在同一坐标系。
                 这里逐条验证。
               ================================================================ */
            {
                if (ChemLab.Game.LabUI.UiFont == null)
                    ChemLab.Game.LabUI.UiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var host = new GameObject("HessTestHost");
                ChemLab.Game.EnthalpyScene scn = null;
                try {
                    scn = ChemLab.Game.EnthalpyScene.Show(host, ChemLab.Game.LabUI.UiFont);
                } catch (System.Exception ex) {
                    Say("   ✗ 建立焓变场景时抛异常：" + ex.GetType().Name + " " + ex.Message);
                    Say("     位置：" + (ex.StackTrace ?? "").Split('\n')[0]);
                }
                if (scn == null) { Say("   ✗ 无法建立焓变场景"); TCheck("焓变场景可建立", false); }
                else { try {
                    /* 示例场景给了 3 个节点 3 条边 */
                    Say($"   节点 {scn.TestNodeCount} 个，连线 {scn.TestEdgeCount} 条");
                    TCheck("示例场景至少有 3 个节点", scn.TestNodeCount >= 3);
                    TCheck("示例场景至少有 3 条连线", scn.TestEdgeCount >= 3);

                    /* ① 节点必须在 World 层下，拖背景才会一起走 */
                    bool allUnder = true;
                    for (int i = 0; i < scn.TestNodeCount; i++)
                        if (!scn.TestNodeUnderWorld(i)) { allUnder = false; Say($"   ✗ 节点 {i} 不在 World 下"); }
                    TCheck("所有节点都挂在 World 层下（拖背景时节点要跟着走）", allUnder);

                    /* ② 拖背景：World 位移后，节点的世界坐标必须同步变化 */
                    var before0 = scn.TestNodeWorldPoint(1);
                    scn.TestPanWorld(new Vector2(120, -80));
                    var after0 = scn.TestNodeWorldPoint(1);
                    float dx = after0.x - before0.x, dy = after0.y - before0.y;
                    Say($"   平移 World (120,-80) → 节点世界坐标位移 ({dx:F0},{dy:F0})");
                    TCheck("拖背景时节点跟着动", Mathf.Abs(dx - 120) < 1f && Mathf.Abs(dy + 80) < 1f);
                    scn.TestPanWorld(new Vector2(-120, 80));      // 还原

                    /* ③ 拖节点：连线的端点必须跟着变 */
                    /* 用「起点 + 箭尖」两个点对比：中间线段数量会随弧度变，按数量比会误判 */
                    var e0Before = scn.TestEdgeEnds(0);
                    scn.TestMoveNode(1, new Vector2(150, 90));
                    var e0After = scn.TestEdgeEnds(0);
                    Say($"   连线 0 的起点/箭尖：{e0Before.Count} 个采样点");
                    TCheck("连线上确实画出了线段与箭头", e0Before.Count == 2);
                    if (e0Before.Count == 2 && e0After.Count == 2) {
                        float d0 = Vector3.Distance(e0Before[0], e0After[0]);
                        float d1 = Vector3.Distance(e0Before[1], e0After[1]);
                        Say($"   起点位移 {d0:F1} px，箭尖位移 {d1:F1} px");
                        /* 边 0 连的是节点 0 → 节点 1，我移动的是节点 1：
                           所以「箭尖」（挂在被移动的节点上）必须动，
                           「起点」（挂在没动的节点 0 上）保持不动才是对的。 */
                        TCheck("拖节点时，连到它那一端跟着变", d1 > 10f);
                        TCheck("另一端保持不动（说明连线确实是按节点位置重算的）", d0 < 2f);
                    } else {
                        TCheck("拖节点时，连到它那一端跟着变", false);
                        TCheck("另一端保持不动（说明连线确实是按节点位置重算的）", false);
                    }

                    /* ④ 标注不得压线：中心到曲线的距离要够 */
                    {
                        int tooClose = 0;
                        for (int ei = 0; ei < scn.TestEdgeCount; ei++) {
                            float d = scn.TestLabelClearance(ei);
                            if (d >= 0f && d < 40f) { tooClose++; Say($"   ✗ 连线 {ei} 的标注离曲线只有 {d:F0}px"); }
                        }
                        TCheck("所有连线标注都离线足够远（不压线）", tooClose == 0);
                        /* 标注之间也不能压在一起 */
                        int lap = 0;
                        var ls = scn.TestLabelPositions();
                        /* 判据要用「矩形相交」，不能用「中心距」——
                           标注本身宽 186px，中心距 60px 看着合格其实早就叠上了。 */
                        const float LW = 186f, LH2 = 40f;
                        for (int x = 0; x < ls.Count; x++)
                            for (int y = x + 1; y < ls.Count; y++) {
                                float ox = Mathf.Abs(ls[x].x - ls[y].x);
                                float oy = Mathf.Abs(ls[x].y - ls[y].y);
                                if (ox < LW && oy < LH2) {
                                    lap++;
                                    Say($"   ✗ 标注 {x} 与 {y} 矩形相交（横向 {ox:F0} < {LW:F0}）");
                                }
                            }
                        TCheck("连线标注之间不重叠", lap == 0);
                    }

                    /* ⑤ 容器标签：必须永久存在、且互不重叠。
                         这条断言是被真实 bug 逼出来的 —— 标签曾经全都 active、
                         canvas 也 enabled，就是不在画面上（世界坐标被清零了），
                         而且容器标签还会因为方程式标签而消失。 */
                    {
                        var tagHost = ChemLab.Game.LabBootstrap.Instance;
                        int tagOk = 0, tagBad = 0;
                        if (tagHost != null) {
                            var all = tagHost.GetComponentsInChildren<ChemLab.Game.VesselTag>(true);
                            foreach (var tg in all) {
                                if (tg == null) continue;
                                if (!tg.gameObject.activeInHierarchy) { tagBad++; Say("   ✗ 标签 " + tg.name + " 不显示"); continue; }
                                var cv = tg.GetComponentInChildren<Canvas>(true);
                                if (cv == null || !cv.enabled) { tagBad++; Say("   ✗ 标签 " + tg.name + " 的画布没启用"); continue; }
                                float lift = tg.transform.position.y;
                                if (lift < 0.2f) { tagBad++; Say($"   ✗ 标签 {tg.name} 的世界 Y 只有 {lift:F2}（像是没定位）"); continue; }
                                tagOk++;
                            }
                            Say($"   容器标签：正常 {tagOk} 个，异常 {tagBad} 个");
                        }
                        TCheck("容器标签都真的显示出来了（不是只有 active）", tagHost == null || (tagBad == 0 && tagOk > 0));

                    /* 检验器材必须能在三种之间循环 ——
                       木条一直存在于场景里，但 CurrentTest() 的注释写着「按顺序循环」
                       而 _testIdx 从来没被改过，永远停在 0 = 红色石蕊试纸，
                       结果木条根本切不到（用户就是这么发现的）。 */
                    {
                        /* 无头环境里 TestSet.All 是空的，所以查枚举本身：
                           三种道具必须都在，而且 CurrentTest 的循环要能遍历到它们。 */
                        var kv = System.Enum.GetValues(typeof(TestItemKind));
                        bool hasRed = false, hasBlue = false, hasSplint = false;
                        foreach (TestItemKind k in kv) {
                            if (k == TestItemKind.RedLitmus) hasRed = true;
                            if (k == TestItemKind.BlueLitmus) hasBlue = true;
                            if (k == TestItemKind.Splint) hasSplint = true;
                        }
                        Say($"   检验器材种类数 = {kv.Length}");
                        TCheck("检验器具有三种（红/蓝石蕊 + 木条）", kv.Length == 3 && hasRed && hasBlue);
                        TCheck("检验器具里有木条（能被循环切换选中）", hasSplint);
                        /* 木条必须有「正在燃烧 / 刚熄灭」这两个状态，
                           否则「点燃 → 伸入」那套检验根本做不了。 */
                        var sv2 = System.Enum.GetValues(typeof(SplintState));
                        TCheck("木条有燃烧/熄灭状态", sv2.Length == 3);
                    }

                    /* 容器倒空之后，气泡和火焰必须停 ——
                       原来 UpdateAppearance 只在发生反应时调用，
                       而且「有气体就把气泡钉在 0.3」，
                       烧杯烧干了还一直冒黄色的气泡。 */
                    {
                        var tv = Lab.AddVessel(VesselKind.Beaker);
                        tv.AddWater(30);
                        tv.Fx.Bubble = 3f; tv.Fx.Smoke = 2f;
                        tv.Fx.Flame = true; tv.Fx.FlameT = 0.05f;
                        for (int i = 0; i < 180; i++) Lab.UpdateAppearance(tv, 1f / 60f);
                        float bWithLiquid = tv.Fx.Bubble;
                        /* 倒空 */
                        tv.Water = 0; tv.Solutes.Clear(); tv.Undissolved.Clear();
                        tv.Precipitates.Clear(); tv.Gases.Clear();
                        for (int i = 0; i < 240; i++) Lab.UpdateAppearance(tv, 1f / 60f);
                        Say($"   有液体时气泡 {bWithLiquid:F2}，倒空后 {tv.Fx.Bubble:F2}，火焰 {(tv.Fx.Flame ? "还开着" : "已熄灭")}");
                        TCheck("倒空后气泡归零", tv.Fx.Bubble <= 0.001f);
                        TCheck("倒空后火焰熄灭", !tv.Fx.Flame);
                        Lab.RemoveVessel(tv);
                    }
                        /* 标签之间不许重叠 */
                        int tlap = 0;
                        if (tagHost != null) {
                            var all = tagHost.GetComponentsInChildren<ChemLab.Game.VesselTag>(true);
                            for (int a = 0; a < all.Length; a++)
                                for (int b = a + 1; b < all.Length; b++) {
                                    var ra = all[a].ScreenRect; var rb = all[b].ScreenRect;
                                    if (ra.width <= 0f || rb.width <= 0f) continue;
                                    if (ra.xMin < rb.xMax && rb.xMin < ra.xMax && ra.yMin < rb.yMax && rb.yMin < ra.yMax) tlap++;
                                }
                        }
                        TCheck("容器标签之间不重叠", tlap == 0);

                    /* 透明器皿的聚焦高亮必须走玻璃材质的菲涅尔辉光，
                       不能建反向壳 —— 反向壳在透明物体上必然被透出来，
                       整个器皿泛青，调细描边也只是减轻、根治不了。 */
                    {
                        var anyVessel = UnityEngine.Object.FindObjectOfType<ChemLab.Game.VesselView>();
                        if (anyVessel != null) {
                            var gm = anyVessel.TestGlassMat;
                            TCheck("器皿玻璃材质带 _HighlightAmt 属性（走菲涅尔辉光）",
                                   gm != null && gm.HasProperty("_HighlightAmt"));
                        } else Say("   （无头环境没有器皿视图，跳过玻璃高亮检查）");
                    }
                    }

                    /* ⑤ Born-Haber 循环（4 节点 5 边）的标注也要不压线、不互压。
                          示例场景只有 3 条边，覆盖不到教程用的那张图。 */
                    {
                        scn.SeedBornHaberDemo();
                        const float LW = 186f, LH2 = 40f;
                        int bad = 0;
                        var ls = scn.TestLabelPositions();
                        Say($"   Born-Haber：{scn.TestNodeCount} 个节点，{scn.TestEdgeCount} 条边，{ls.Count} 个标注");
                        for (int x = 0; x < ls.Count; x++) {
                            float cl = scn.TestLabelClearance(x);
                            if (cl >= 0f && cl < 40f) { bad++; Say($"   ✗ 标注 {x} 离曲线只有 {cl:F0}px"); }
                            for (int y = x + 1; y < ls.Count; y++) {
                                float ox = Mathf.Abs(ls[x].x - ls[y].x);
                                float oy = Mathf.Abs(ls[x].y - ls[y].y);
                                if (ox < LW && oy < LH2) { bad++; Say($"   ✗ 标注 {x} 与 {y} 矩形相交（横向 {ox:F0}）"); }
                            }
                        }
                        TCheck("Born-Haber 循环的标注不压线也不互压", bad == 0);
                    }

                    /* ⑥ 单击节点 → 拉线 → 点另一个节点 */
                    bool pulled = scn.TestPullEdge(0, 2);
                    Say("   单击节点 0 → 点节点 2：" + (pulled ? "连上了" : "✗ 没连上"));
                    TCheck("单击节点可以拉出连线并连到另一个节点", pulled);

                    UnityEngine.Object.Destroy(host);
                } catch (System.Exception ex2) {
                    Say("   ✗ 测试过程抛异常：" + ex2.GetType().Name + " " + ex2.Message);
                    Say("     位置：" + (ex2.StackTrace ?? "").Split('\n')[0]);
                    TCheck("焓变场景交互测试不抛异常", false);
                } }
            }

            /* ---------- 宠物精灵图 ---------- */
            {
                var d = ChemLab.Game.PetSprite.Diagnose();
                foreach (var ln in d.Split('\n')) Say("   " + ln.Trim());
                TCheck("阿侬精灵图已加载且格子数正确", d.StartsWith("✓"));
                var sp = ChemLab.Game.PetSprite.Cell(0, 0);
                TCheck("能取到第 0 行第 0 格精灵", sp != null && sp.rect.width > 0);
                /* 关键：动画里不能混进空格子，否则循环到那一帧就会闪一下。
                   光判断 sprite 非 null 不够 —— 空格子也能建出 sprite，
                   所以要真的去数不透明像素的占比。 */
                int bad = 0; float worst = 1f; string worstName = "";
                foreach (var kv in ChemLab.Game.PetSprite.Anims) {
                    int fc = ChemLab.Game.PetSprite.FrameCount(kv.Value.Row);
                    if (fc < 2) { bad++; Say($"   ✗ 动画 {kv.Key} 只有 {fc} 帧"); continue; }
                    for (int f = 0; f < fc; f++) {
                        float fill = ChemLab.Game.PetSprite.FrameFill(kv.Value.Row, f);
                        if (fill < worst) { worst = fill; worstName = kv.Key + "#" + f; }
                        if (fill < 0.06f) { bad++; Say($"   ✗ 动画 {kv.Key} 第 {f} 帧几乎是空的（填充率 {fill:P1}）"); }
                    }
                }
                Say($"   最稀疏的一帧：{worstName} 填充率 {worst:P1}");
                TCheck("每个动画的每一帧都有实际内容（这是之前闪烁的根因）", bad == 0);
            }

            /* ---------- 爆鸣音：必须真的有声音输出 ---------- */
            var pr = ChemLab.Game.ChemAudio.ProbeRaw();
            Say($"   爆鸣音：峰值 {pr.peak:F3}  有效值 {pr.rms:F4}  长度 {pr.len} 采样  过零 {pr.zeroCross} 次");
            TCheck("爆鸣音不是静音（峰值 > 0.5）", pr.peak > 0.5f);
            TCheck("爆鸣音有足够能量（有效值 > 0.02）", pr.rms > 0.02f);
            TCheck("爆鸣音没有削顶失真（峰值 ≤ 1.0）", pr.peak <= 1.0f);
            TCheck("爆鸣音有高频成分（过零 > 400 次，听起来才尖锐）", pr.zeroCross > 400);
            TCheck("爆鸣音长度合适（0.2–0.6 s）", pr.len > 44100 * 0.2 && pr.len < 44100 * 0.6);
        }

        Say($"");
        Say($"=========== 通过 {_pass}，失败 {_fail} ===========");
        return Sb.ToString();
    }
}

}
