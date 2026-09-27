const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Chem/EqFormat.cs';
let c = fs.readFileSync(p, 'utf8');

/* ---- 1) Pretty：无 ^ 时用「前面是不是单个元素符号」判断数字归属 ----
   Mn2+  -> 前面是 Mn（单个元素）      -> 2 是电荷 -> Mn²⁺
   NH4+  -> 前面是 NH（两个元素）      -> 4 是下标，电荷只有 + -> NH₄⁺
   SO4^2- -> 有 ^，直接分开
   HCO3- -> 前面是 HCO（多个元素）     -> 3 是下标，电荷是 - -> HCO₃⁻              */
const oldCharge = `        else {
            /* 没有 ^ 的离子（如 OH-、H+、Mn2+）把结尾的电荷拆出来 */
            var m = System.Text.RegularExpressions.Regex.Match(f, @"(\\d*[+-])$");
            if (m.Success && m.Length < f.Length) { charge = m.Value; f = f.Substring(0, f.Length - m.Length); }
        }`;
const newCharge = `        else {
            /* 没有 ^ 的离子把结尾的电荷拆出来。难点是「数字到底是下标还是电荷」：
                 Mn2+  前面是 Mn（单个元素符号）→ 2 是电荷，得 Mn²⁺
                 NH4+  前面是 NH（不止一个元素）→ 4 是下标，电荷只有 +，得 NH₄⁺
                 HCO3- 同理，3 是下标 */
            var m = System.Text.RegularExpressions.Regex.Match(f, @"(\\d*)([+-])$");
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
        }`;
if (c.includes(oldCharge)) { c = c.replace(oldCharge, newCharge); console.log('✓ Pretty 电荷判定已修'); }
else console.log('✗ Pretty 锚点');

/* IsSingleElement 辅助 */
if (!c.includes('static bool IsSingleElement')) {
  c = c.replace('    static string Sub(string digits) {',
`    /// <summary>去掉首字母和可选的小写字母后就没有剩余 —— 说明是单个元素符号</summary>
    static bool IsSingleElement(string s) {
        if (string.IsNullOrEmpty(s)) return false;
        if (!char.IsUpper(s[0])) return false;
        int i = 1;
        while (i < s.Length && char.IsLower(s[i])) i++;
        return i == s.Length;
    }

    static string Sub(string digits) {`);
  console.log('✓ IsSingleElement 已加');
}

/* ---- 2) Normalize：把结尾的 Unicode 上标还原成 ^电荷，而不是直接接在后面 ----
   CO₃²⁻ -> CO3^2-   （不是 CO32-，否则按分子式反查不到物种） ---- */
const oldNorm = c.match(/    public static string Normalize\(string s\) \{[\s\S]*?\n    \}/);
if (oldNorm) {
  const newNorm = `    public static string Normalize(string s) {
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
    }`;
  c = c.replace(oldNorm[0], newNorm);
  console.log('✓ Normalize 已改为还原 ^');
} else console.log('✗ Normalize 锚点');

fs.writeFileSync(p, c, 'utf8');

/* ---- 3) ParseSide 也要处理「下标数字 vs 电荷」---- */
const q = 'D:/ChemLabUnity/Assets/Scripts/Chem/EqFormat.cs';
let qc = fs.readFileSync(q, 'utf8');
const oldPS = qc.match(/    public static List<Rules\.Species> ParseSide\(string side\) \{[\s\S]*?\n    \}/);
if (oldPS) {
  const newPS = `    public static List<Rules.Species> ParseSide(string side) {
        var outList = new List<Rules.Species>();
        foreach (var raw in side.Split('+')) {
            string t = raw.Trim();
            if (t.Length == 0) continue;
            /* 去掉状态符号和中文的箭头标记 */
            t = System.Text.RegularExpressions.Regex.Replace(t, @"\\((s|l|g|aq)\\)$", "");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[↑↓]$", "");
            /* 把 Unicode 上下标还原成「分子式^电荷」的形式 */
            t = Normalize(t);
            double coef = 1;
            int i = 0;
            while (i < t.Length && (char.IsDigit(t[i]) || t[i] == '.')) i++;
            if (i > 0 && i < t.Length) { double.TryParse(t.Substring(0, i), out coef); t = t.Substring(i); }
            string id = ChemData.Resolve(t);
            if (id != null) outList.Add(new Rules.Species { Id = id, Coef = (int)System.Math.Round(coef) });
            else outList.Add(new Rules.Species { Id = t, Coef = (int)System.Math.Round(coef) });
        }
        return outList;
    }`;
  qc = qc.replace(oldPS[0], newPS);
  fs.writeFileSync(q, qc, 'utf8');
  console.log('✓ ParseSide 已改用 Resolve 兜底');
} else console.log('✗ ParseSide 锚点');
