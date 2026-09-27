const fs = require('fs');
let n = 0;

/* ---- 1) 挥发查表改成大小写不敏感（物种 id 是 HSO3- / HCO3- 这种大写形式，
          我之前写的小写键根本查不到） ---- */
let k = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemCore.cs';
let kc = fs.readFileSync(k, 'utf8');
if (!kc.includes('VolatileLower')) {
  kc = kc.replace('    static readonly Dictionary<string, string> Volatile = new Dictionary<string, string>();',
`    static readonly Dictionary<string, string> Volatile = new Dictionary<string, string>();
    /* 小写键的副本：物种 id 有 HSO3- / HCO3- 这种大写写法，
       也有 nh3 / so2 这种小写写法，查表必须两边都认。 */
    static readonly Dictionary<string, string> VolatileLower = new Dictionary<string, string>();`);
  kc = kc.replace(`        foreach (var ln in Lines(TblVolatile)) {
            var cv = Cols(ln);
            if (cv.Length >= 2) Volatile[cv[0]] = cv[1];
        }`,
`        foreach (var ln in Lines(TblVolatile)) {
            var cv = Cols(ln);
            if (cv.Length >= 2) {
                Volatile[cv[0]] = cv[1];
                VolatileLower[cv[0].ToLowerInvariant()] = cv[1];
            }
        }`);
  kc = kc.replace(`    public static string VapourOf(string id) {
        string v; return Volatile.TryGetValue(id ?? "", out v) ? v : null;
    }`,
`    public static string VapourOf(string id) {
        if (string.IsNullOrEmpty(id)) return null;
        string v;
        if (Volatile.TryGetValue(id, out v)) return v;
        return VolatileLower.TryGetValue(id.ToLowerInvariant(), out v) ? v : null;
    }`);
  fs.writeFileSync(k, kc, 'utf8'); n++;
}
console.log('大小写不敏感查表:', kc.includes('VolatileLower'));

/* ---- 2) 删掉绕过浓度门槛的 Ca(OH)2 规则（沉淀规则里已有带门槛的版本） ---- */
let r = fs.readFileSync('D:/ChemLab/js/rules.js', 'utf8');
const before = r.length;
r = r.replace(/\/\* Ca\(OH\)2 微溶：中等浓度以上才析出白色沉淀，这就是石灰水 \*\/\r?\n/, '');
r = r.replace(/R\('Ca2\+ \+ 2OH- -> Ca\(OH\)2', '产生白色沉淀（氢氧化钙微溶）', \{ precip:'Ca\(OH\)2' \}\),\r?\n/, '');
if (r.length !== before) { fs.writeFileSync('D:/ChemLab/js/rules.js', r, 'utf8'); n++; }
console.log('Ca(OH)2 规则已删:', !r.includes("R('Ca2+ + 2OH- -> Ca(OH)2'"));

/* ---- 3) 挥发表补上常见的大写形式键，双保险 ---- */
let j = fs.readFileSync('D:/ChemLab/js/gastests.js', 'utf8');
if (!j.includes("'HSO3-'")) {
  j = j.replace("  'hso3-': 'so2', 'h2so3': 'so2',",
                "  'HSO3-': 'so2', 'H2SO3': 'so2', 'hso3-': 'so2', 'h2so3': 'so2',");
  j = j.replace("  'hco3-': 'co2', 'h2co3': 'co2',",
                "  'HCO3-': 'co2', 'H2CO3': 'co2', 'hco3-': 'co2', 'h2co3': 'co2',");
  j = j.replace("  'hs-': 'h2s', 's2-': 'h2s',",
                "  'HS-': 'h2s', 'S2-': 'h2s', 'hs-': 'h2s', 's2-': 'h2s',");
  fs.writeFileSync('D:/ChemLab/js/gastests.js', j, 'utf8'); n++;
}
console.log('大写键已补:', j.includes("'HSO3-'"));
console.log('补丁处数:', n);
