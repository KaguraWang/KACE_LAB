/* =====================================================================
   化学核心：化学式解析、物质/离子模型、反应规则解析
   —— 从 D:\ChemLab\js\engine.js 移植
   ===================================================================== */
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ChemLab.Chem {

/* ==================================================== 化学式解析 */
public static class Formula {
    static readonly Dictionary<string, double> M = new Dictionary<string, double> {
        {"H",1.008},{"He",4.003},{"Li",6.94},{"Be",9.012},{"B",10.81},{"C",12.011},
        {"N",14.007},{"O",15.999},{"F",18.998},{"Ne",20.18},{"Na",22.990},{"Mg",24.305},
        {"Al",26.982},{"Si",28.085},{"P",30.974},{"S",32.06},{"Cl",35.45},{"Ar",39.95},
        {"K",39.098},{"Ca",40.078},{"Ti",47.867},{"Cr",51.996},{"Mn",54.938},{"Fe",55.845},
        {"Co",58.933},{"Ni",58.693},{"Cu",63.546},{"Zn",65.38},{"Br",79.904},{"Ag",107.868},
        {"Sn",118.710},{"I",126.904},{"Ba",137.327},{"Pt",195.084},{"Au",196.967},
        {"Hg",200.592},{"Pb",207.2},{"Sr",87.62},
    };

    /// <summary>解析一个化学式单位，支持括号。返回 元素→个数</summary>
    /// <summary>去掉首字母后的小写字母就没有剩余 —— 说明这是单个元素符号</summary>
    static bool SingleElement(string s) {
        if (string.IsNullOrEmpty(s)) return false;
        if (!char.IsUpper(s[0])) return false;
        int i = 1;
        while (i < s.Length && char.IsLower(s[i])) i++;
        return i == s.Length;
    }

    /// <summary>Read charge without confusing an atom subscript with charge magnitude.</summary>
    public static int IonicCharge(string formula) {
        if (string.IsNullOrWhiteSpace(formula)) return 0;
        string s = formula.Replace("(aq)", "").Trim();
        var explicitCharge = System.Text.RegularExpressions.Regex.Match(s, @"\^(\d*)([+-])$");
        var match = explicitCharge.Success ? explicitCharge :
            System.Text.RegularExpressions.Regex.Match(s, @"(\d*)([+-])$");
        if (!match.Success || match.Index == 0) return 0;
        string digits = match.Groups[1].Value, stem = s.Substring(0, match.Index);
        int magnitude = 1;
        if (digits.Length > 0 && (explicitCharge.Success || SingleElement(stem) || stem.EndsWith("]")))
            magnitude = int.Parse(digits, CultureInfo.InvariantCulture);
        return match.Groups[2].Value == "-" ? -magnitude : magnitude;
    }

    static Dictionary<string,int> ParseUnit(string s) {
        var outp = new Dictionary<string,int>();
        int i = 0;
        void Merge(Dictionary<string,int> src, int n) {
            foreach (var kv in src) {
                outp.TryGetValue(kv.Key, out int cur);
                outp[kv.Key] = cur + kv.Value * n;
            }
        }
        while (i < s.Length) {
            char c = s[i];
            if (c == '(' || c == '[') {
                char close = c == '(' ? ')' : ']';
                int depth = 1, j = i + 1;
                while (j < s.Length && depth > 0) {
                    if (s[j] == c) depth++;
                    else if (s[j] == close) depth--;
                    if (depth > 0) j++;
                }
                var inner = ParseUnit(s.Substring(i + 1, Math.Max(0, j - i - 1)));
                i = j + 1;
                int n = 0; bool has = false;
                while (i < s.Length && char.IsDigit(s[i])) { n = n * 10 + (s[i] - '0'); i++; has = true; }
                Merge(inner, has ? n : 1);
            } else if (char.IsUpper(c)) {
                string el = c.ToString(); i++;
                while (i < s.Length && char.IsLower(s[i])) el += s[i++];
                int n = 0; bool has = false;
                while (i < s.Length && char.IsDigit(s[i])) { n = n * 10 + (s[i] - '0'); i++; has = true; }
                Merge(new Dictionary<string,int> { { el, 1 } }, has ? n : 1);
            } else i++;
        }
        return outp;
    }

    /// <summary>解析完整化学式（含结晶水 ·）</summary>
    public static Dictionary<string,int> Parse(string str) {
        var total = new Dictionary<string,int>();
        if (string.IsNullOrEmpty(str)) return total;
        foreach (var raw in str.Split('·', '•')) {
            string s = raw.Trim();
            if (s.Length == 0) continue;
            /* 结晶水前面的系数：必须从 0 开始累加，否则 "5H2O" 会变成 15 个水 */
            int outer = 0, k = 0;
            while (k < s.Length && char.IsDigit(s[k])) { outer = outer * 10 + (s[k] - '0'); k++; }
            if (k > 0) s = s.Substring(k); else outer = 1;
            /* 去掉末尾电荷与 (aq) 标记 */
            /* 电荷的剥离要区分「数字是下标还是电荷」：
                 Mn2+  去掉数字后是 Mn（单个元素）→ 2 是电荷
                 NH4+  去掉数字后是 NH（不止一个元素）→ 4 是下标，只剥符号
                 HCO3- 同理，3 是下标
                 SO4^2- 有 ^ 显式标出，直接剥 */
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\^\d*[+-]\s*$", "");
            var chg = System.Text.RegularExpressions.Regex.Match(s, @"(\d*)([+-])\s*$");
            if (chg.Success && chg.Index > 0) {
                string stem = s.Substring(0, chg.Index);
                string digs = chg.Groups[1].Value;
                s = (digs.Length == 0 || SingleElement(stem) || stem.EndsWith("]"))
                    ? stem                                   /* 数字是电荷 */
                    : s.Substring(0, chg.Index + digs.Length); /* 数字是下标，只剥符号 */
            }
            s = s.Replace("(aq)", "");
            var a = ParseUnit(s);
            foreach (var kv in a) {
                total.TryGetValue(kv.Key, out int cur);
                total[kv.Key] = cur + kv.Value * outer;
            }
        }
        return total;
    }

    public static double MolarMass(string str) {
        var a = Parse(str);
        double m = 0;
        foreach (var kv in a) if (M.TryGetValue(kv.Key, out double w)) m += w * kv.Value;
        return m > 0 ? m : 1.0;
    }
}

/* ==================================================== 物质 / 离子 */
public class Substance {
    public string Id, Name, Formula, State, SolidColor, SolColor, Cls, Note, Form;
    public double Density, Mass;
    public char Sol;                       // k 易溶 / s 微溶 / n 难溶 / r 与水反应 / - 不适用
    public bool IsIon;
    public int Charge;
    public Dictionary<string,int> Atoms = new Dictionary<string,int>();

    public bool HasSolidColor => !string.IsNullOrEmpty(SolidColor);
    public bool HasSolColor   => !string.IsNullOrEmpty(SolColor);
}

public static partial class ChemData {
    static bool _built;

    public static readonly Dictionary<string, Substance> Sub = new Dictionary<string, Substance>();
    public static readonly Dictionary<string, string> SpeciesIndex = new Dictionary<string, string>();

    /* 电离：id -> (离子名 -> 系数), 是否弱电解质, 电离度, 是否不电离, 分子别名 */
    public class Dissoc { public Dictionary<string,int> Ions = new Dictionary<string,int>();
                          public bool Weak; public double Degree = 1.0; public bool None; public string MoleculeAs;
                          public int WaterProduced; }
    public static readonly Dictionary<string, Dissoc> Diss = new Dictionary<string, Dissoc>();

    public class PrecipInfo { public string Color, Desc; public bool Acid, Ammonia; }
    public static readonly Dictionary<string, PrecipInfo> Precip = new Dictionary<string, PrecipInfo>();

    public class PrecipRule { public string Cat, An, Id; public int NC, NA; }
    public static readonly List<PrecipRule> PrecipRules = new List<PrecipRule>();

    public static readonly Dictionary<string, string> FlameColor = new Dictionary<string, string>();

    /* ---- CIE 9701：官能团家族与通用有机规则 ---- */
    public static readonly Dictionary<string, string> Family = new Dictionary<string, string>();
    public static readonly Dictionary<string, string> NameToId = new Dictionary<string, string>();
    /* ---- 焓变数据（P3 热化学题） ---- */
    static readonly Dictionary<string, double> DissolveH = new Dictionary<string, double>();
    static readonly Dictionary<string, double> RuleH = new Dictionary<string, double>();
    /* ---- 气体检验响应表（离子检验用） ---- */
    static readonly Dictionary<string, string> GasNat = new Dictionary<string, string>();
    static readonly Dictionary<string, string[]> GasSpl = new Dictionary<string, string[]>();
    static readonly Dictionary<string, string> Volatile = new Dictionary<string, string>();
    /* 小写键的副本：物种 id 有 HSO3- / HCO3- 这种大写写法，
       也有 nh3 / so2 这种小写写法，查表必须两边都认。 */
    static readonly Dictionary<string, string> VolatileLower = new Dictionary<string, string>();
    public class OrgRule { public string From, To, Cond, Reagents, Byproducts, Obs; }
    public static readonly List<OrgRule> OrgRules = new List<OrgRule>();
    public static readonly Dictionary<string, string> OrgMap = new Dictionary<string, string>();

    /* ---- 本地化 ---- */
    public static readonly Dictionary<string, string> EnSub = new Dictionary<string, string>();
    public static readonly Dictionary<string, string> EnText = new Dictionary<string, string>();
    public static readonly Dictionary<string, string> EnCls = new Dictionary<string, string>();
    public static readonly Dictionary<string, string> EnVessel = new Dictionary<string, string>();
    public static readonly Dictionary<string, double> Molarity = new Dictionary<string, double>();
    public static readonly HashSet<string> WaterReactive = new HashSet<string>();

    static string[] Lines(string tbl) =>
        tbl.Split('\n');

    static string[] Cols(string line) => line.Split('\t');

    static char SolChar(string s) => string.IsNullOrEmpty(s) ? '-' : s[0];

    public static void EnsureBuilt() {
        if (_built) return;
        _built = true;

        /* ---- 物质 ---- */
        foreach (var ln in Lines(TblSubstances+"\n"+TblOrganicExpansion)) {
            if (string.IsNullOrWhiteSpace(ln)) continue;
            var c = Cols(ln);
            if (c.Length < 9) continue;
            var s = new Substance {
                Id = c[0], Name = c[1], Formula = c[2], State = c[3],
                SolidColor = c[4], SolColor = c[5],
                Density = ParseD(c[6], 1.0), Sol = SolChar(c[7]), Cls = c[8],
                Note = c.Length > 9 ? c[9] : "",
            };
            s.Atoms = Formula.Parse(s.Formula);
            s.Charge = Formula.IonicCharge(s.Formula);
            s.IsIon = s.Charge != 0;
            if (s.IsIon) { s.Cls = "ion"; s.State = "aq"; }
            if (s.Id == "phenoxide") s.Name = "苯氧根离子";
            s.Mass = Formula.MolarMass(s.Formula);
            s.Form = GuessForm(s);
            Sub[s.Id] = s;
        }

        // Internal phase ledger, not an additional room-temperature shelf reagent.
        Sub["water_ice"]=new Substance {Id="water_ice",Name="冰",Formula="H2O",State="s",
            SolidColor="#c9e5ee",SolColor="",Density=.9167,Mass=Formula.MolarMass("H2O"),
            Atoms=Formula.Parse("H2O"),Sol='n',Cls="internal_phase",Form="frozen",
            Note="纯水常压定温冻结模型；冰密度采用近0℃常数近似，未包含相变潜热"};

        /* ---- 离子 ---- */
        foreach (var ln in Lines(TblIons)) {
            if (string.IsNullOrWhiteSpace(ln)) continue;
            var c = Cols(ln);
            if (c.Length < 2) continue;
            // Legacy ion table also contains neutral C6H6O6. Its molecular
            // stock entry already exists; do not register a second, fake ion.
            if (ParseI(c[1]) == 0) continue;
            var s = new Substance {
                Id = c[0], Name = c[0], Formula = c[0], State = "aq",
                SolidColor = "", SolColor = c.Length > 2 ? c[2] : "",
                Density = 1.0, Sol = 'k', Cls = "ion",
                Note = c.Length > 3 ? c[3] : "",
                IsIon = true, Charge = ParseI(c[1]), Mass = Formula.MolarMass(c[0]), Atoms=Formula.Parse(c[0]), Form = "liquid",
            };
            Sub[s.Id] = s;
        }

        /* ---- 物种索引：id / 化学式 / 中文名 → id（先到先得） ---- */
        foreach (var kv in Sub) {
            var s = kv.Value;
            Index(s.Id, s.Id);
            Index(s.Formula, s.Id);
            Index(s.Name, s.Id);
        }
        /* 别名：规则里可能用简写 */
        Alias("NH3", "nh3"); Alias("H2O", "h2o"); Alias("C6H5OH", "phoh");
        Alias("Cu2(OH)2CO3", "cuco3"); Alias("Fe(CN)3", "fescn3");

        /* ---- 电离表 ---- */
        foreach (var ln in Lines(TblDissociation)) {
            if (string.IsNullOrWhiteSpace(ln)) continue;
            var c = Cols(ln);
            if (c.Length < 3) continue;
            var d = new Dissoc {
                Weak = c[2] == "1",
                Degree = c.Length > 3 ? ParseD(c[3], 0.01) : 1.0,
                None = c.Length > 4 && c[4] == "1",
                MoleculeAs = c.Length > 5 ? c[5] : null,
            };
            if (!string.IsNullOrEmpty(c[1]))
                foreach (var pair in c[1].Split(';')) {
                    if (pair.Length == 0) continue;
                    int eq = pair.LastIndexOf('=');
                    if (eq <= 0) continue;
                    d.Ions[pair.Substring(0, eq)] = ParseI(pair.Substring(eq + 1));
                }
            Diss[c[0]] = d;
        }
        // Curated engine corrections survive regeneration of the legacy stock tables.
        // These retain the existing lumped weak-acid model; full stepwise speciation is separate.
        Diss["h3po4"].Ions["H+"]=3;
        Diss["h2s"].Ions["H+"]=2;
        Diss["nh3"].WaterProduced=-1;
        Diss["cuso45h2o"].WaterProduced=5;
        const string oleate="C17H33COO-";
        Sub[oleate]=new Substance{Id=oleate,Name="油酸根",Formula=oleate,State="aq",Density=1,Sol='k',Cls="ion",IsIon=true,Charge=-1,Mass=Formula.MolarMass(oleate),Atoms=Formula.Parse(oleate),Form="liquid"};
        SpeciesIndex[oleate]=oleate;
        Diss["c17h33coona"].Ions[oleate]=1;
        const string cyanide="CN-";
        Sub[cyanide]=new Substance{Id=cyanide,Name="氰离子",Formula=cyanide,State="aq",Density=1,Sol='k',Cls="ion",IsIon=true,Charge=-1,Mass=Formula.MolarMass(cyanide),Atoms=Formula.Parse(cyanide),Form="liquid"};
        Index(cyanide,cyanide);Index("氰离子",cyanide);
        var cyanideSalt=new Dissoc();cyanideSalt.Ions["K+"]=1;cyanideSalt.Ions[cyanide]=1;
        Diss["kcn"]=cyanideSalt;

        /* ---- 沉淀外观 ---- */
        foreach (var ln in Lines(TblPrecipitates)) {
            if (string.IsNullOrWhiteSpace(ln)) continue;
            var c = Cols(ln);
            if (c.Length < 3) continue;
            Precip[c[0]] = new PrecipInfo {
                Color = c[1], Desc = c[2],
                Acid = c.Length > 3 && c[3] == "1",
                Ammonia = c.Length > 4 && c[4] == "1",
            };
        }

        /* ---- 沉淀生成规则 ---- */
        foreach (var ln in Lines(TblPrecipRules)) {
            if (string.IsNullOrWhiteSpace(ln)) continue;
            var c = Cols(ln);
            if (c.Length < 5) continue;
            PrecipRules.Add(new PrecipRule { Cat = c[0], An = c[1], Id = c[2],
                                             NC = ParseI(c[3]), NA = ParseI(c[4]) });
        }

        /* ---- 焰色 / 浓度 / 遇水 ---- */
        foreach (var ln in Lines(TblFlameColors)) {
            var c = Cols(ln); if (c.Length < 2) continue;
            if (!string.IsNullOrWhiteSpace(c[0])) FlameColor[c[0]] = c[1];
        }
        foreach (var ln in Lines(TblMolarity)) {
            var c = Cols(ln); if (c.Length < 2) continue;
            if (!string.IsNullOrWhiteSpace(c[0])) Molarity[c[0]] = ParseD(c[1], 1.0);
        }
        foreach (var ln in Lines(TblWaterReactive)) {
            var s = ln.Trim();
            if (s.Length > 0) WaterReactive.Add(s);
        }

        /* ---- 官能团家族 ---- */
        foreach (var ln in Lines(TblFamilies)) {
            var c2 = Cols(ln);
            if (c2.Length < 2 || string.IsNullOrWhiteSpace(c2[0])) continue;
            Family[c2[0]] = c2[1];
        }
        /* ---- 通用有机规则 ---- */
        foreach (var ln in Lines(TblOrganicRules)) {
            var c2 = Cols(ln);
            if (c2.Length < 6 || string.IsNullOrWhiteSpace(c2[0])) continue;
            OrgRules.Add(new OrgRule { From = c2[0], To = c2[1], Cond = c2[2],
                                       Reagents = c2[3], Byproducts = c2[4], Obs = c2[5] });
        }
        /* ---- 家族 → 产物映射 ---- */
        foreach (var ln in Lines(TblOrganicMap)) {
            var c2 = Cols(ln);
            if (c2.Length < 4) continue;
            /* 键含产物家族：同一反应物在不同条件下产物不同 */
            OrgMap[c2[0] + "|" + c2[1] + "|" + c2[2]] = c2[3];
        }
        // The legacy family table points phenol coupling to azobenzene,
        // dropping phenol's oxygen. The balanced rule/data product is the
        // existing p-hydroxyazobenzene entry.
        OrgMap["diazonium|c6h5n2cl|azo"] = "azo_dye";
        /* ---- 本地化 ---- */
        Fill(EnSub, TblI18nSub); Fill(EnSub, TblI18nSubAdditional);
        Fill(EnText, TblI18nText); Fill(EnText, TblI18nUiAdditional);
        Fill(EnCls, TblI18nCls); Fill(EnVessel, TblI18nVessel);

        foreach (var ln in Lines(TblDissolveH)) {
            var c2 = Cols(ln);
            if (c2.Length < 2) continue;
            double dv; if (double.TryParse(c2[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out dv)) DissolveH[c2[0]] = dv;
        }
        foreach (var ln in Lines(TblRuleH)) {
            var c2 = Cols(ln);
            if (c2.Length < 2) continue;
            double dv; if (double.TryParse(c2[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out dv)) RuleH[Norm(c2[0])] = dv;
        }

        foreach (var ln in Lines(TblVolatile)) {
            var cv = Cols(ln);
            if (cv.Length >= 2) {
                Volatile[cv[0]] = cv[1];
                VolatileLower[cv[0].ToLowerInvariant()] = cv[1];
            }
        }
        foreach (var ln in Lines(TblGasNature)) {
            var c2 = Cols(ln);
            if (c2.Length >= 2) GasNat[c2[0]] = c2[1];
        }
        foreach (var ln in Lines(TblGasSplint)) {
            var c2 = Cols(ln);
            if (c2.Length >= 4) GasSpl[c2[0]] = new[] { c2[1], c2[2], c2[3] };
        }

        /* 中文名 → id，用于把列表里的物质名整体翻成英文 */
        foreach (var kv in Sub) if (!NameToId.ContainsKey(kv.Value.Name)) NameToId[kv.Value.Name] = kv.Key;

        Rules.Init();
    }

    static void Fill(Dictionary<string,string> map, string tbl) {
        foreach (var ln in tbl.Split('\n')) {
            if (string.IsNullOrWhiteSpace(ln)) continue;
            int t = ln.IndexOf('\t');
            if (t <= 0) continue;
            map[ln.Substring(0, t)] = ln.Substring(t + 1);
        }
    }

    /// <summary>把物种 id 翻成当前语言的名字</summary>
    public static string SubName(string id) {
        if (Localization.English) {
            string en;
            if (EnSub.TryGetValue(id, out en)) return en;
        }
        Substance s;
        return Sub.TryGetValue(id, out s) ? s.Name : id;
    }

    /// <summary>把方程式规范化后查表（去掉多余空格）</summary>
    static string Norm(string eq) {
        if (string.IsNullOrEmpty(eq)) return "";
        var parts = eq.Split(' ');
        var sb = new System.Text.StringBuilder();
        foreach (var t in parts) if (t.Length > 0) { if (sb.Length > 0) sb.Append(' '); sb.Append(t); }
        return sb.ToString();
    }

    /// <summary>某个物种 mol 数对应的质量（g）</summary>
    public static double MassOf(string id, double mol) {
        Substance s;
        if (!Sub.TryGetValue(id, out s)) return 0;
        return mol * Formula.MolarMass(s.Formula);
    }

    /* ---- 气体检验查表 ---- */
    /// <summary>气体的酸碱性：base / acid / neut / bleach；未知按中性处理</summary>
    public static string GasNature(string gasId) {
        string v; return GasNat.TryGetValue(gasId ?? "", out v) ? v : "neut";
    }
    public static string GasSplintBurning(string gasId) {
        string[] v; return GasSpl.TryGetValue(gasId ?? "", out v) ? v[0] : "out";
    }
    public static string GasSplintJustOut(string gasId) {
        string[] v; return GasSpl.TryGetValue(gasId ?? "", out v) ? v[1] : "out";
    }
    public static string GasSplintObs(string gasId) {
        string[] v; return GasSpl.TryGetValue(gasId ?? "", out v) ? v[2] : "";
    }
    /// <summary>物种的中文名（观察描述里用）</summary>
    public static string NameOf(string id) {
        Substance s;
        return Sub.TryGetValue(id ?? "", out s) ? s.Name : (id ?? "");
    }
    /// <summary>表里登记了多少种气体的酸碱性（自检用）</summary>
    public static int GasNatureCount => GasNat.Count;
    public static int GasSplintCount => GasSpl.Count;

    /// <summary>该物种溶于水时上方蒸气对应哪种气体；不挥发返回 null</summary>
    public static string VapourOf(string id) {
        if (string.IsNullOrEmpty(id)) return null;
        string v;
        if (Volatile.TryGetValue(id, out v)) return v;
        return VolatileLower.TryGetValue(id.ToLowerInvariant(), out v) ? v : null;
    }

    /// <summary>物质所属的官能团家族；不是有机物返回空串</summary>
    public static string FamilyOf(string id) {
        string v; return Family.TryGetValue(id ?? "", out v) ? v : "";
    }
    /// <summary>按分子式反查物种 id（用统一的物种索引，大小写与空格都不敏感）</summary>
    public static string IdByFormula(string formula) {
        if (string.IsNullOrEmpty(formula)) return null;
        return Resolve(formula);
    }

    public static double DissolveHeat(string id) {
        double v; return DissolveH.TryGetValue(id, out v) ? v : 0;
    }
    public static double RuleHeat(string eq) {
        // Solid quicklime hydration, not the old aqueous-ion shortcut.
        // JANAF 298.15 K formation enthalpies (kJ/mol): CaO(cr) -635.089,
        // Ca(OH)2(cr) -986.085, H2O(l) -285.830 => -65.166.
        // https://janaf.nist.gov/tables/Ca-029.html
        // https://janaf.nist.gov/tables/Ca-020.html
        // https://janaf.nist.gov/tables/H-063.html
        if(Norm(eq)=="CaO + H2O -> Ca(OH)2")return -65.166;
        double v; return RuleH.TryGetValue(Norm(eq), out v) ? v : 0;
    }

    static void Index(string key, string id) {
        if (string.IsNullOrEmpty(key)) return;
        var k = key.ToLowerInvariant().Replace(" ", "");
        if (!SpeciesIndex.ContainsKey(k)) SpeciesIndex[k] = id;
    }
    static void Alias(string key, string id) => SpeciesIndex[key.ToLowerInvariant()] = id;

    /// <summary>把规则里的物种写法解析成 id；解析不出来返回 null（该规则跳过）</summary>
    public static string Resolve(string tok) {
        if (Sub.ContainsKey(tok)) return tok;
        var k = tok.ToLowerInvariant().Replace(" ", "");
        return SpeciesIndex.TryGetValue(k, out string id) ? id : null;
    }

    public static double ParseD(string s, double fallback) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : fallback;
    public static int ParseI(string s) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;

    /* 外观形态：决定渲染成粉末 / 晶体 / 金属块 / 液滴 */
    static string GuessForm(Substance s) {
        if (s.State == "g") return "gas";
        if (s.State == "l" || s.State == "aq") return "liquid";
        if (s.Cls == "metal") {
            if (s.Id == "na" || s.Id == "k") return "soft";
            if (s.Note != null && (s.Note.Contains("粉") || s.Note.Contains("粉末"))) return "powder";
            return "metal";
        }
        if (s.Cls == "nonmetal")
            return (s.Id == "c" || s.Id == "p_red" || s.Id == "p_white") ? "powder" : "crystal";
        if (s.Cls == "oxide") return "powder";
        if (s.Cls == "salt") {
            if (s.Note != null && s.Note.Contains("晶体")) return "crystal";
            if (s.Id == "caco3" || s.Id == "baco3" || s.Id == "cuco3") return "granule";
            return "crystal";
        }
        if (s.Cls == "organic") {
            if (s.Note != null && s.Note.Contains("晶体")) return "crystal";
            if (s.Id == "oil") return "liquid";
            return "powder";
        }
        return "powder";
    }
}

/* ==================================================== 反应规则 */
public static class Rules {
    public class Species { public string Id; public int Coef; public string Raw; }

    public class Rule {
        public string Eq, Obs, Cond;
        public List<Species> Reactants = new List<Species>();
        public List<Species> Products  = new List<Species>();
        /* 效果 */
        public string Gas, Flame, Precip, Tint, Deposit;
        public int Bubble, Heat, Smoke, Odor;
        public bool Mirror, Explode, ClearTint, Float;
        /// <summary>可逆反应 —— 中英规范都用双向箭头 ⇌</summary>
        public bool Reversible;
        /// <summary>有机反应 —— 中文化学规范里有机反应用箭头而不是等号</summary>
        public bool Organic;
        public bool Parsed;
    }

    public static readonly List<Rule> All = new List<Rule>();
    public class Special { public string Id, Obs, Tint; public bool ClearTint; }
    public static readonly List<Special> Specials = new List<Special>();

    static bool _init;
    public static void Init() {
        if (_init) return; _init = true;

        foreach (var ln in ChemData.TblRules.Split('\n')) {
            if (string.IsNullOrWhiteSpace(ln)) continue;
            var c = ln.Split('\t');
            if (c.Length < 3) continue;
            // OrganicRound owns the heated ethanolic substitution, including NH4+ / Br-.
            // This legacy duplicate had no conditions and bypassed that branch entirely.
            if(c[0]=="C2H5Br + 2NH3 -> C2H5NH2 + NH4Br")continue;
            // Nitration is handled once, with sulfuric acid / heat requirements
            // and the actual molecular or ionic nitric-acid inventory.
            if(c[0]=="C6H6 + HNO3 -> C6H5NO2 + H2O")continue;
            var r = new Rule {
                Eq = c[0], Obs = c[1], Cond = c.Length > 2 ? c[2] : "",
                Gas = c.Length > 3 ? c[3] : "",
                Bubble = c.Length > 4 ? ChemData.ParseI(c[4]) : 0,
                Heat   = c.Length > 5 ? ChemData.ParseI(c[5]) : 0,
                Smoke  = c.Length > 6 ? ChemData.ParseI(c[6]) : 0,
                Flame  = c.Length > 7 ? c[7] : "",
                Precip = c.Length > 8 ? c[8] : "",
                Mirror = c.Length > 9 && c[9] == "1",
                Explode= c.Length > 10 && c[10] == "1",
                Tint   = c.Length > 11 ? c[11] : "",
                ClearTint = c.Length > 12 && c[12] == "1",
                Deposit= c.Length > 13 ? c[13] : "",
                Float  = c.Length > 14 && c[14] == "1",
                Odor   = c.Length > 15 ? ChemData.ParseI(c[15]) : 0,
                Reversible = c.Length > 16 && c[16] == "1",
            };
            // The generated legacy observation claimed *any* amount of quicklime
            // boils water and emitted fume even when no heat budget was solved.
            // Keep the qualitative reaction, not a false quantity-independent boil.
            if(r.Eq=="CaO + H2O -> Ca2+ + 2OH-"){
                r.Eq="CaO + H2O -> Ca(OH)2";
                r.Obs="氧化钙与水反应生成固体氢氧化钙；可溶部分形成石灰水，温升随反应量变化";
                r.Smoke=0;
            }
            ParseEquation(r);
            All.Add(r);
        }

        ChemData.AddExpansionRules();
        // Wet lime slurry can carbonate directly; only reacting once with the
        // small dissolved fraction leaves most Ca(OH)2 inert. Excess CO2 then
        // converts the white CaCO3 precipitate into dissolved bicarbonate.
        // https://pubs.acs.org/doi/abs/10.1021/ie060732l
        // https://www.cambridgeinternational.org/images/347473-june-2015-question-paper-31.pdf
        var slurryCarbonation=new Rule{Eq="Ca(OH)2 + CO2 -> CaCO3 + H2O",
            Obs="湿润的氢氧化钙固体与二氧化碳反应，生成白色碳酸钙沉淀",
            Cond="wet_slurry",Precip="caco3"};
        ParseEquation(slurryCarbonation);All.Add(slurryCarbonation);
        var excessDissolvedCarbonDioxide=new Rule{Eq="CaCO3 + CO2 + H2O -> Ca2+ + 2HCO3-",
            Obs="继续通入过量二氧化碳后，白色碳酸钙沉淀逐渐溶解",
            Cond="wet_bicarbonate"};
        ParseEquation(excessDissolvedCarbonDioxide);
        // Give already dissolved CO2 a chance to react with carbonate before
        // the generic CO2 <=> H2CO3 rule consumes it for this settle pass.
        int hydration=All.FindIndex(rule=>rule.Eq=="CO2 + H2O -> H2CO3");
        if(hydration>=0)All.Insert(hydration,excessDissolvedCarbonDioxide);
        else All.Add(excessDissolvedCarbonDioxide);
        foreach (var ln in ChemData.TblSpecials.Split('\n')) {
            if (string.IsNullOrWhiteSpace(ln)) continue;
            var c = ln.Split('\t');
            if (c.Length < 2) continue;
            Specials.Add(new Special { Id = c[0], Obs = c[1],
                Tint = c.Length > 2 ? c[2] : "", ClearTint = c.Length > 3 && c[3] == "1" });
        }
    }

    public static void ParseEquationPublic(Rule r) => ParseEquation(r);

    static void ParseEquation(Rule r) {
        var sides = r.Eq.Split(new[]{"->"}, StringSplitOptions.None);
        if (sides.Length < 2) return;
        var L = ParseSide(sides[0]);
        var R = ParseSide(sides[1]);
        if (L == null || R == null) return;
        r.Reactants = L; r.Products = R; r.Parsed = true;
    }

    static List<Species> ParseSide(string side) {
        var list = new List<Species>();
        /* 只按「空格+加号+空格」切 —— 离子本身就带 + 号 */
        var toks = System.Text.RegularExpressions.Regex.Split(side.Trim(), @"\s\+\s");
        foreach (var t0 in toks) {
            var t = t0.Trim();
            if (t.Length == 0) continue;
            int coef = 1, i = 0;
            while (i < t.Length && char.IsDigit(t[i])) { if (i == 0) coef = 0; coef = coef * 10 + (t[i] - '0'); i++; }
            var raw = t.Substring(i).Trim();
            if (raw.Length == 0) continue;
            var id = ChemData.Resolve(raw);
            if (id == null) return null;              // 有解析不出来的物种 → 整条规则作废
            list.Add(new Species { Id = id, Coef = coef, Raw = raw });
        }
        return list;
    }
}

}
