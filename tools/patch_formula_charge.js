const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemCore.cs';
let c = fs.readFileSync(p, 'utf8');

/* 原来的正则把「下标数字」也当成电荷剥掉了：
   NH4+  -> NH   （H 只剩 1 个，应为 4）
   HCO3- -> HCO  （少一个 O）
   CH3COO- -> CH3CO
   这些离子的摩尔质量全部偏小，配液和滴定计算都受影响。
   正确判据：只有「带 ^ 的显式电荷」或「去掉数字后剩下单个元素符号」
   时，数字才算电荷；否则数字是下标，只能剥掉正负号。 */
const old = `            s = System.Text.RegularExpressions.Regex.Replace(s, @"\\^?\\d*[+-]\\s*$", "");`;
const nw = `            /* 电荷的剥离要区分「数字是下标还是电荷」：
                 Mn2+  去掉数字后是 Mn（单个元素）→ 2 是电荷
                 NH4+  去掉数字后是 NH（不止一个元素）→ 4 是下标，只剥符号
                 HCO3- 同理，3 是下标
                 SO4^2- 有 ^ 显式标出，直接剥 */
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\\^\\d*[+-]\\s*$", "");
            var chg = System.Text.RegularExpressions.Regex.Match(s, @"(\\d*)([+-])\\s*$");
            if (chg.Success && chg.Index > 0) {
                string stem = s.Substring(0, chg.Index);
                string digs = chg.Groups[1].Value;
                s = (digs.Length == 0 || SingleElement(stem))
                    ? stem                                   /* 数字是电荷 */
                    : s.Substring(0, chg.Index + digs.Length); /* 数字是下标，只剥符号 */
            }`;
if (!c.includes(old)) { console.log('✗ 锚点'); process.exit(1); }
c = c.replace(old, nw);

/* SingleElement 辅助 */
if (!c.includes('static bool SingleElement')) {
  c = c.replace('    static Dictionary<string,int> ParseUnit(string s) {',
`    /// <summary>去掉首字母后的小写字母就没有剩余 —— 说明这是单个元素符号</summary>
    static bool SingleElement(string s) {
        if (string.IsNullOrEmpty(s)) return false;
        if (!char.IsUpper(s[0])) return false;
        int i = 1;
        while (i < s.Length && char.IsLower(s[i])) i++;
        return i == s.Length;
    }

    static Dictionary<string,int> ParseUnit(string s) {`);
}
fs.writeFileSync(p, c, 'utf8');
console.log('✓ 分子式电荷剥离已修');
console.log('  SingleElement:', /static bool SingleElement/.test(c));
