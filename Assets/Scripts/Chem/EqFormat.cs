/* =====================================================================
   反应方程式的规范化输出
     中文（中学规范，依据科普中国/人教版）：
       · 无机反应用「=」连接，有机反应用「→」
       · 可逆反应用可逆符号「⇌」
       · 反应条件写在等式上方（多个条件以顿号分隔）
       · 气体生成物标「↑」、沉淀标「↓」；**不标 (s)/(l)/(g)/(aq)**
     英文（CIE 规范）：
       · 一律用「→」，可逆用「⇌」
       · 条件写在箭头上方
       · 每种物质后必须括号标注状态 (s)/(l)/(g)/(aq)

   离子方程式：把两边都出现的旁观离子消掉，再约成最简整数比。
   ===================================================================== */
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ChemLab.Chem {

public static class EqFormat {

    /* ---------------- 条件标注 ---------------- */
    /// <summary>把引擎内部的 cond 键翻成教科书上写的反应条件。
    /// 只返回「需要写在箭头上方」的条件 —— acid/neutral 这类是引擎的判定门槛，
    /// 不是教科书上的反应条件，不能写出来。</summary>
    public static string CondLabel(string cond, bool english) {
        if (string.IsNullOrEmpty(cond)) return "";
        var parts = new List<string>();
        switch (cond) {
            case "heat":       parts.Add(english ? "heat" : "加热"); break;
            case "hot":        parts.Add(english ? "strong heat" : "高温"); break;
            case "ignite":     parts.Add(english ? "ignite" : "点燃"); break;
            case "light":      parts.Add(english ? "light" : "光照"); break;
            case "catal":      parts.Add(english ? "catalyst" : "催化剂"); break;
            case "catal_hot":  parts.Add(english ? "catalyst" : "催化剂");
                               parts.Add(english ? "high temperature" : "高温"); break;
            case "catal_warm": parts.Add(english ? "catalyst" : "催化剂");
                               parts.Add(english ? "heat" : "加热"); break;
            case "conc":       parts.Add(english ? "conc." : "浓"); break;
            case "conc_nh3":   parts.Add(english ? "conc. NH₃" : "浓氨水"); break;
            case "reflux":     parts.Add(english ? "heat under reflux" : "回流"); break;
            case "distil":     parts.Add(english ? "distil" : "蒸馏"); break;
            /* acid / neutral / cool / no_i2 等是引擎判定用的，不是反应条件 */
            default: break;
        }
        return string.Join(english ? ", " : "、", parts);
    }

    /* ---------------- 连接符 ---------------- */
    public static string Connector(Rules.Rule r, bool english) {
        if (r.Reversible) return "⇌";                 // 中英规范一致
        if (english) return "→";
        /* 中文：无机用等号，有机用箭头 */
        return IsOrganic(r) ? "→" : "=";
    }

    /// <summary>判断是不是有机反应（中文化学规范里有机反应要写箭头而不是等号）</summary>
    public static bool IsOrganic(Rules.Rule r) {
        foreach (var sp in r.Reactants.Concat(r.Products))
            if (IsOrganicId(sp.Id)) return true;
        return false;
    }
    public static bool IsOrganicId(string id) {
        string fam = ChemData.FamilyOf(id);
        if (!string.IsNullOrEmpty(fam)) return true;
        /* 含碳的分子（CO2/CO3^2-/CN- 这类无机碳除外）也算有机 */
        ChemData.Sub.TryGetValue(id ?? "", out var s);
        if (s == null) return false;
        if (!s.Formula.Contains("C")) return false;
        string f = s.Formula;
        if (f == "CO2" || f == "CO" || f == "H2CO3" || f.Contains("CO3") ||
            f.Contains("CN") || f == "CS2" || f.Contains("HCO3") || f.Contains("C2O4")) return false;
        return true;
    }

    /* ---------------- 分子式 → 带上下标的显示形式 ---------------- */
    /* ---------------- 分子式 → 带上下标的显示形式 ----------------
       注意：Unity 的 legacy Text **不支持 <sub>/<sup>**（只认 b/i/size/color/material/quad），
       所以不能靠富文本。这里改用 Unicode 的上下标字符，效果也更接近教科书排版。 */
    static readonly char[] SUB = { '₀','₁','₂','₃','₄','₅','₆','₇','₈','₉' };
    static readonly char[] SUP = { '⁰','¹','²','³','⁴','⁵','⁶','⁷','⁸','⁹' };

    /// <summary>去掉首字母和可选的小写字母后就没有剩余 —— 说明是单个元素符号</summary>
    static bool IsSingleElement(string s) {
        if (string.IsNullOrEmpty(s)) return false;
        if (!char.IsUpper(s[0])) return false;
        int i = 1;
        while (i < s.Length && char.IsLower(s[i])) i++;
        return i == s.Length;
    }

    static string Sub(string digits) {
        var sb = new StringBuilder();
        foreach (var ch in digits) sb.Append(ch >= '0' && ch <= '9' ? SUB[ch - '0'] : ch);
        return sb.ToString();
    }
    static string Sup(string s) {
        var sb = new StringBuilder();
        foreach (var ch in s) {
            if (ch >= '0' && ch <= '9') sb.Append(SUP[ch - '0']);
            else if (ch == '-') sb.Append('⁻');
            else if (ch == '+') sb.Append('⁺');
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary>把 SO4^2- 排成 SO₄²⁻、把 Ca(OH)2 排成 Ca(OH)₂</summary>
    public static string Pretty(string formula) {
        if (string.IsNullOrEmpty(formula)) return "";
        string f = formula, charge = "";
        int caret = f.IndexOf('^');
        if (caret >= 0) { charge = f.Substring(caret + 1); f = f.Substring(0, caret); }
        else {
            /* 没有 ^ 的离子把结尾的电荷拆出来。难点是「数字到底是下标还是电荷」：
                 Mn2+  前面是 Mn（单个元素符号）→ 2 是电荷，得 Mn²⁺
                 NH4+  前面是 NH（不止一个元素）→ 4 是下标，电荷只有 +，得 NH₄⁺
                 HCO3- 同理，3 是下标 */
            var m = System.Text.RegularExpressions.Regex.Match(f, @"(\d*)([+-])$");
            if (m.Success && m.Index > 0) {
                string digits = m.Groups[1].Value;
                string stem = f.Substring(0, m.Index);
                if (digits.Length > 0 && !IsSingleElement(stem)) {
                    /* 数字属于分子式本身 —— 电荷只取符号 */
                    charge = m.Groups[2].Value;
                    f = f.Substring(0, m.Index + digits.Length);   // 保留数字当下标
                } else {
                    charge = m.Value;
                    f = stem;
                }
            }
        }
        var sb = new StringBuilder();
        int i = 0;
        while (i < f.Length) {
            char ch = f[i];
            sb.Append(ch);
            /* 元素符号或右括号后面的数字是下标 */
            if (char.IsLetter(ch) || ch == ')') {
                int j = i + 1;
                while (j < f.Length && char.IsDigit(f[j])) j++;
                if (j > i + 1) { sb.Append(Sub(f.Substring(i + 1, j - i - 1))); i = j; continue; }
            }
            i++;
        }
        if (!string.IsNullOrEmpty(charge)) sb.Append(Sup(charge));
        return sb.ToString();
    }
    /* ---------------- 状态符号（英文规范必须标） ---------------- */
    static string StateTag(string id) {
        ChemData.Sub.TryGetValue(id ?? "", out var s);
        if (s == null) return "(aq)";
        switch (s.State) {
            case "s":  return "(s)";
            case "l":  return "(l)";
            case "g":  return "(g)";
            default:   return "(aq)";
        }
    }

    /* ---------------- 中文的 ↑ / ↓ ---------------- */
    /// <summary>中文规范：只有生成物才标 ↑/↓；
    /// 反应物中已有气体时，气态生成物不再标 ↑（人教版明确要求）。</summary>
    static string CNMark(string id, bool isProduct, bool hasGasReactant, int productCount, bool inSolution) {
        if (!isProduct) return "";
        ChemData.Sub.TryGetValue(id ?? "", out var s);
        if (s == null) return "";
        bool isGas = s.State == "g";
        bool isPrecip = ChemData.Precip.ContainsKey(s.Formula);
        if (isGas) {
            if (hasGasReactant) return "";      // 反应物里有气体就不标
            if (productCount <= 1) return "";   // 产物只有一种且是气体也不标
            return "↑";
        }
        if (isPrecip && inSolution) return "↓"; // 非溶液反应不标沉淀号
        return "";
    }

    /* ---------------- 主格式化 ---------------- */
    /// <summary>格式化一条规则。ionic=true 时输出离子方程式（若该反应写得出）。</summary>
    public static string Format(Rules.Rule r, bool english, bool ionic, bool inSolution = true) {
        if (r == null) return "";
        if (ionic) {
            string ion = IonicEquation(r, english, inSolution);
            if (!string.IsNullOrEmpty(ion)) return ion;
            /* 写不出离子式就退回总方程式 */
        }
        return FormatFull(r, english, inSolution);
    }

    public static string FormatFull(Rules.Rule r, bool english, bool inSolution) {
        bool hasGasReactant = r.Reactants.Any(x => IsGas(x.Id));
        int prodCount = r.Products.Count;
        string left  = Side(r.Reactants, english, false, hasGasReactant, prodCount, inSolution);
        string right = Side(r.Products,  english, true,  hasGasReactant, prodCount, inSolution);
        return left + " " + Connector(r, english) + " " + right;
    }

    /// <summary>离子方程式：消去两边都出现的旁观离子，再约成最简整数比</summary>
    public static string IonicEquation(Rules.Rule r, bool english, bool inSolution) {
        /* 注意：规则大多是用**分子式**写的（2NaOH + H2SO4 -> ...），里面根本没有离子。
           所以先把能电离的分子按电离表展开成离子，再消旁观离子 ——
           否则「离子方程式」永远只会退回总方程式。 */
        var net = new Dictionary<string, double>();
        void AddIons(string id, double coef) {
            ChemData.Diss.TryGetValue(id, out var d);
            if (d != null && !d.None && d.Ions != null && d.Ions.Count > 0 && id != "h2o" && id != "H2O") {
                foreach (var ion in d.Ions) {
                    double cur; net.TryGetValue(ion.Key, out cur);
                    net[ion.Key] = cur + coef * ion.Value;
                }
                return;
            }
            double c0; net.TryGetValue(id, out c0);
            net[id] = c0 + coef;
        }
        foreach (var sp in r.Reactants) AddIons(sp.Id, -sp.Coef);
        foreach (var sp in r.Products)  AddIons(sp.Id,  sp.Coef);
        /* 原来那两行直接累加分子，改为上面的展开版 */

        /* 消去净系数为 0 的（旁观离子） */
        var left = new List<Rules.Species>(); var right = new List<Rules.Species>();
        bool anyIon = false;
        foreach (var kv in net) {
            if (System.Math.Abs(kv.Value) < 1e-9) continue;   // 旁观离子，消掉
            ChemData.Sub.TryGetValue(kv.Key, out var s);
            bool isIon = s != null && s.IsIon;
            if (isIon) anyIon = true;
            var sp = new Rules.Species { Id = kv.Key, Coef = (int)System.Math.Round(System.Math.Abs(kv.Value)) };
            if (kv.Value > 0) right.Add(sp); else left.Add(sp);
        }
        /* 没有离子参与就不是离子反应，写不出离子式 */
        if (!anyIon || left.Count == 0 || right.Count == 0) return "";
        /* 化简成最简整数比 */
        double g = 0;
        foreach (var sp in left.Concat(right)) g = Gcd(g, sp.Coef);
        if (g > 1.5) {
            foreach (var sp in left)  sp.Coef = (int)System.Math.Round(sp.Coef / g);
            foreach (var sp in right) sp.Coef = (int)System.Math.Round(sp.Coef / g);
        }
        left  = left.OrderByDescending(x => x.Coef).ToList();
        right = right.OrderByDescending(x => x.Coef).ToList();

        bool hasGasReactant = left.Any(x => IsGas(x.Id));
        string l = Side(left,  english, false, hasGasReactant, right.Count, inSolution);
        string rr= Side(right, english, true,  hasGasReactant, right.Count, inSolution);
        return l + " " + Connector(r, english) + " " + rr;
    }

    static double Gcd(double a, double b) {
        a = System.Math.Round(System.Math.Abs(a)); b = System.Math.Round(System.Math.Abs(b));
        while (b > 1e-9) { double t = a % b; a = b; b = t; }
        return a < 1e-9 ? 1 : a;
    }

    static bool IsGas(string id) {
        ChemData.Sub.TryGetValue(id ?? "", out var s);
        return s != null && s.State == "g";
    }

    static string Side(List<Rules.Species> list, bool english, bool isProduct,
                       bool hasGasReactant, int prodCount, bool inSolution) {
        var sb = new StringBuilder();
        foreach (var sp in list) {
            if (sb.Length > 0) sb.Append(" + ");
            if (sp.Coef != 1) sb.Append(sp.Coef.ToString());
            ChemData.Sub.TryGetValue(sp.Id, out var s);
            string f = s != null ? s.Formula : sp.Id;
            sb.Append(Pretty(f));
            if (english) sb.Append(StateTag(sp.Id));
            else sb.Append(CNMark(sp.Id, isProduct, hasGasReactant, prodCount, inSolution));
        }
        return sb.ToString();
    }

    static string Num(double c) {
        double round = System.Math.Round(c);
        if (System.Math.Abs(c - round) < 1e-6) return ((int)round).ToString();
        return c.ToString("0.##");
    }

    /// <summary>把 Unicode 上下标还原成 ASCII，便于按分子式反查</summary>
    public static string Normalize(string s) {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder();
        var sup = new StringBuilder();
        foreach (var ch in s) {
            int k = System.Array.IndexOf(SUB, ch);
            if (k >= 0) { sb.Append((char)('0' + k)); continue; }
            k = System.Array.IndexOf(SUP, ch);
            if (k >= 0) { sup.Append((char)('0' + k)); continue; }
            if (ch == '⁻') { sup.Append('-'); continue; }
            if (ch == '⁺') { sup.Append('+'); continue; }
            /* 上标只可能出现在末尾；一旦回到普通字符就把攒的上标冲掉 */
            if (sup.Length > 0) { sb.Append('^').Append(sup); sup.Length = 0; }
            sb.Append(ch);
        }
        if (sup.Length > 0) sb.Append('^').Append(sup);
        return sb.ToString();
    }

    /* ---------------- 供自检交叉验证：把格式化结果还原成配平检查 ---------------- */
    /// <summary>把方程式文本（含状态符号、↑↓、上下标标记）拆回「系数+分子式」列表</summary>
    public static List<Rules.Species> ParseSide(string side) {
        var outList = new List<Rules.Species>();
        foreach (var raw in side.Split('+')) {
            string t = raw.Trim();
            if (t.Length == 0) continue;
            /* 去掉状态符号和中文的箭头标记 */
            t = System.Text.RegularExpressions.Regex.Replace(t, @"\((s|l|g|aq)\)$", "");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[↑↓]$", "");
            /* 把 Unicode 上下标还原成「分子式^电荷」的形式 */
            t = Normalize(t);
            double coef = 1;
            int i = 0;
            while (i < t.Length && (char.IsDigit(t[i]) || t[i] == '.')) i++;
            if (i > 0 && i < t.Length) { double.TryParse(t.Substring(0, i), out coef); t = t.Substring(i); }
            /* 电荷写法在数据表里不统一：H+ 没有 ^，SO4^2- 有 ^。
               所以依次试几种形式再反查，避免因为写法差异拆解失败。 */
            string id = ChemData.Resolve(t);
            if (id == null && t.Contains("^")) id = ChemData.Resolve(t.Replace("^", ""));
            if (id == null && t.Contains("^")) {
                int c2 = t.IndexOf('^');
                id = ChemData.Resolve(t.Substring(0, c2) + t.Substring(c2 + 1));
            }
            outList.Add(new Rules.Species { Id = id ?? t, Coef = (int)System.Math.Round(coef) });
        }
        return outList;
    }
}

}
