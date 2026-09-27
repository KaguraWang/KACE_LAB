/* =====================================================================
   焓变计算：键能法 + 生成焓法 + 盖斯定律
     三种算法都能独立算出反应焓，互相验证：
       · 键能法：ΔH = Σ(反应物键能) − Σ(生成物键能)
       · 生成焓法：ΔH = Σ(产物 ΔHf) − Σ(反应物 ΔHf)
       · 盖斯定律：沿任意闭合回路，Σ ΔH = 0
   ===================================================================== */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChemLab.Chem {

public enum EnthalpySource { Table, Manual, Unknown }

/// <summary>焓变计算节点的一条边</summary>
public class HessEdge {
    public int From, To;
    /// <summary>这条边的焓变（kJ/mol），未知时为 0</summary>
    public double DH;
    public EnthalpySource Source = EnthalpySource.Unknown;
    /// <summary>这条边代表的反应（用来查表/键能计算）</summary>
    public string Equation = "";
    public bool Reversed;         // 画的时候箭头是否反向
    /// <summary>把这条边反向使用时的焓变（Hess 环路会用到）</summary>
    public double DHFrom(int node) => node == From ? DH : -DH;
}

public static class HessLaw {

    /* ---------------- 键能 ---------------- */
    static readonly Dictionary<string, double> Bond = new Dictionary<string, double>();
    static readonly Dictionary<string, double> FormH = new Dictionary<string, double>();
    static readonly List<(string eq, double dh, string note)> RxH = new List<(string, double, string)>();
    static readonly Dictionary<string, double> AtomH = new Dictionary<string, double>();
    static readonly Dictionary<string, double> EA = new Dictionary<string, double>();
    static bool _init;

    public static void Init() {
        if (_init) return; _init = true;
        Rules.Init();
        foreach (var row in ChemData.TblBondEnergy.Split('\n')) {
            if (string.IsNullOrWhiteSpace(row)) continue;
            var c = row.Split('\t');
            if (c.Length < 4) continue;   /* 表是 4 列：原子A 原子B 键级 键能 */
            Bond[Key(c[0], c[1], ChemData.ParseD(c[2], 1))] = ChemData.ParseD(c[3], 0);
        }
        foreach (var row in ChemData.TblFormationH.Split('\n')) {
            if (string.IsNullOrWhiteSpace(row)) continue;
            var c = row.Split('\t');
            if (c.Length < 2) continue;
            FormH[c[0].Trim().ToLowerInvariant()] = ChemData.ParseD(c[1], 0);
        }
        foreach (var row in ChemData.TblAtomisation.Split('\n')) {
            if (string.IsNullOrWhiteSpace(row)) continue;
            var c = row.Split('\t');
            if (c.Length < 2) continue;
            AtomH[c[0].Trim()] = ChemData.ParseD(c[1], 0);
        }
        foreach (var row in ChemData.TblElectronAffinity.Split('\n')) {
            if (string.IsNullOrWhiteSpace(row)) continue;
            var c = row.Split('\t');
            if (c.Length < 2) continue;
            EA[c[0].Trim()] = ChemData.ParseD(c[1], 0);
        }
        foreach (var row in ChemData.TblReactionH.Split('\n')) {
            if (string.IsNullOrWhiteSpace(row)) continue;
            var c = row.Split('\t');
            if (c.Length < 3) continue;
            RxH.Add((c[0].Trim(), ChemData.ParseD(c[1], 0), c[2].Trim()));
        }
    }

    static string Key(string a, string b, double order) {
        /* 键能表里 A-B 与 B-A 是同一个键，统一排序 */
        var x = string.CompareOrdinal(a, b) <= 0 ? a : b;
        var y = string.CompareOrdinal(a, b) <= 0 ? b : a;
        return x + "|" + y + "|" + order.ToString("0.#");
    }

    /// <summary>原子化焓 ΔH_at（元素标准态 → 气态原子）</summary>
    public static double AtomisationOf(string el) {
        Init();
        double v;
        return AtomH.TryGetValue(el ?? "", out v) ? v : double.NaN;
    }
    /// <summary>电子亲和能 EA</summary>
    public static double ElectronAffinityOf(string sp) {
        Init();
        double v;
        return EA.TryGetValue(sp ?? "", out v) ? v : double.NaN;
    }

    public static int BondCount { get { Init(); return Bond.Count; } }
    public static int RxCount { get { Init(); return RxH.Count; } }

    /// <summary>查键能；查不到返回 NaN</summary>
    public static double BondEnergy(string a, string b, double order) {
        double v;
        return Bond.TryGetValue(Key(a, b, order), out v) ? v : double.NaN;
    }

    /// <summary>按分子式计算总键能。分子式里每个键按「成键数」累加。
    /// 用不了键能法（比如离子化合物）时返回 NaN。</summary>
    public static double BondSum(string formula, out string why) {
        why = "";
        var atoms = Formula.Parse(formula);
        if (atoms.Count == 0) { why = "分子式解析失败"; return double.NaN; }
        /* 这是一套简化处理：只对能按「重键比例」估出键数的共价分子有效，
           离子晶体（NaCl/CaO 等）没有键能数据，直接说明并退回生成焓法。 */
        if (atoms.Count == 1) return 0;                    // 单原子，无键
        if (atoms.Count == 2) {
            var e = atoms.Keys.ToList();
            int na = atoms[e[0]], nb = atoms[e[1]];
            if (na == 1 && nb == 1) {
                double v = BondEnergy(e[0], e[1], 1);
                if (double.IsNaN(v)) v = BondEnergy(e[0], e[1], 2);
                if (double.IsNaN(v)) v = BondEnergy(e[0], e[1], 3);
                if (double.IsNaN(v)) { why = $"没有 {e[0]}-{e[1]} 的键能数据"; return double.NaN; }
                return v;
            }
        }
        why = "多原子分子需要结构式才能数键，键能法不适用";
        return double.NaN;
    }

    /* ---------------- 生成焓法 ---------------- */
    public static double FormationOf(string id) {
        double v;
        return FormH.TryGetValue((id ?? "").ToLowerInvariant(), out v) ? v : double.NaN;
    }

    /// <summary>ΔH = Σ产物 ΔHf − Σ反应物 ΔHf</summary>
    public static double FromFormation(List<Rules.Species> reactants, List<Rules.Species> products, out string why) {
        why = "";
        double r = 0, p = 0;
        foreach (var s in reactants) {
            double v = FormationOf(s.Id);
            if (double.IsNaN(v)) { why = $"缺少 {s.Id} 的标准生成焓"; return double.NaN; }
            r += v * s.Coef;
        }
        foreach (var s in products) {
            double v = FormationOf(s.Id);
            if (double.IsNaN(v)) { why = $"缺少 {s.Id} 的标准生成焓"; return double.NaN; }
            p += v * s.Coef;
        }
        return p - r;
    }

    /* ---------------- 查表 ---------------- */
    /// <summary>按方程式查反应焓。会做规范化（空格、箭头写法、系数比例）</summary>
    public static double Lookup(string equation, out string note) {
        note = "";
        Init();
        string want = Norm(equation);
        foreach (var r in RxH) {
            if (Norm(r.eq) == want) { note = r.note; return r.dh; }
            /* 系数成比例也算命中（2H2 + O2 与 4H2 + 2O2 是同一条） */
            double scale;
            if (SameShape(Norm(r.eq), want, out scale) && Math.Abs(scale - 1) < 1e-9) {
                note = r.note; return r.dh;
            }
        }
        return double.NaN;
    }

    static string Norm(string s) {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("→", "->").Replace("=", "->").Replace("⇌", "->");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
        s = s.Replace(" ", "").ToLowerInvariant();
        return s;
    }
    static bool SameShape(string a, string b, out double scale) {
        scale = 1;
        var sa = a.Split(new[]{"->"}, StringSplitOptions.None);
        var sb = b.Split(new[]{"->"}, StringSplitOptions.None);
        if (sa.Length != 2 || sb.Length != 2) return false;
        return sa[0] == sb[0] && sa[1] == sb[1];
    }

    /* ================================================================
       盖斯定律：沿闭合回路求和必须为 0
       把图看成有向图，每条边带 ΔH；从起点出发回到起点，
       沿路径累加「正向为 +ΔH、逆向为 −ΔH」，闭合时结果应为 0。
       如果有且仅有一个未知量，就能解出来。
       ================================================================ */

    /// <summary>找一条闭合回路（DFS 找环）</summary>
    public static List<int> FindCycle(int nodeCount, List<HessEdge> edges) {
        var adj = new List<int>[nodeCount];
        for (int i = 0; i < nodeCount; i++) adj[i] = new List<int>();
        foreach (var e in edges) { adj[e.From].Add(e.To); adj[e.To].Add(e.From); }
        var state = new int[nodeCount];          // 0 未访问 1 在栈上 2 已完成
        var parent = new int[nodeCount];
        var stack = new List<int>();
        for (int i = 0; i < nodeCount; i++) parent[i] = -1;
        var found = new List<int>();
        bool Dfs(int u) {
            state[u] = 1; stack.Add(u);
            foreach (var v in adj[u]) {
                if (state[v] == 0) { parent[v] = u; if (Dfs(v)) return true; }
                else if (state[v] == 1 && parent[u] != v) {
                    /* 找到环：从 u 回溯到 v */
                    int k = stack.Count - 1;
                    while (k >= 0 && stack[k] != v) k--;
                    for (int j = k; j < stack.Count; j++) found.Add(stack[j]);
                    found.Add(v);
                    return true;
                }
            }
            state[u] = 2; stack.RemoveAt(stack.Count - 1);
            return false;
        }
        for (int i = 0; i < nodeCount && found.Count == 0; i++) if (state[i] == 0) Dfs(i);
        return found;
    }

    /// <summary>沿一条闭合回路求未知量。
    /// 回路上有且仅有一条待求边时可解；未知边多于一条则说明条件不足。</summary>
    public static bool SolveCycle(List<int> cycle, List<HessEdge> edges,
                                  out double unknownDH, out string why) {
        unknownDH = 0; why = "";
        if (cycle == null || cycle.Count < 3) { why = "回路不闭合（至少需要三个节点）"; return false; }
        double known = 0; HessEdge unk = null; bool unkReversed = false;
        for (int i = 0; i + 1 < cycle.Count; i++) {
            int a = cycle[i], b = cycle[i + 1];
            var e = edges.FirstOrDefault(x => (x.From == a && x.To == b) || (x.From == b && x.To == a));
            if (e == null) { why = $"节点 {a} 与 {b} 之间没有连线"; return false; }
            double v = e.DHFrom(a);
            if (e.Source == EnthalpySource.Unknown) {
                if (unk != null) { why = "回路上有不止一个未知量，条件不足"; return false; }
                unk = e; unkReversed = (e.From != a);
            } else known += v;
        }
        if (unk == null) {
            /* 全部已知：检查是否闭合（ΣΔH 应为 0） */
            if (Math.Abs(known) < 1.0) { why = $"回路已闭合，ΣΔH = {known:F1} kJ/mol ≈ 0 ✓"; return false; }
            why = $"回路不闭合：ΣΔH = {known:F1} kJ/mol，应为 0";
            return false;
        }
        /* 未知边应使总和为 0 */
        unknownDH = unkReversed ? known : -known;
        why = $"沿回路 ΣΔH = 0 → 未知焓变 = {unknownDH:F1} kJ/mol";
        unk.DH = unknownDH;
        unk.Source = EnthalpySource.Manual;
        return true;
    }
}

}
