const fs = require('fs');
const R = (p, a, b) => { let c = fs.readFileSync(p, 'utf8'); if (!c.includes(a)) return false; fs.writeFileSync(p, c.replace(a, b), 'utf8'); return true; };

/* ---------- 1) 挥发物种表：溶在水里的这些物质上方有蒸气，气体检验能检出 ---- */
let j = 'D:/ChemLab/js/gastests.js';
let jc = fs.readFileSync(j, 'utf8');
if (!jc.includes('VOLATILE')) {
  jc += `
/* ---------------------------------------------------------------
   易挥发物种：它们溶在水里时，上方仍有可检出的蒸气。
   氨水上方是氨气、亚硫酸上方是二氧化硫 —— 气体检验正是利用这一点。
   没有这张表，NH3 这种在数据里以「氨水 NH3·H2O(aq)」形式存在的物种
   就永远进不了气相，石蕊检验会什么也检不出来。
   --------------------------------------------------------------- */
const VOLATILE = {
  'nh3': 'nh3', 'so2': 'so2', 'co2': 'co2', 'h2s': 'h2s',
  'hcl_aq': 'hcl', 'no2': 'no2', 'cl2': 'cl2', 'hbr': 'hbr', 'hi': 'hi',
  'ch3nh2': 'ch3nh2', 'c2h5nh2': 'c2h5nh2',
};
`;
  fs.writeFileSync(j, jc, 'utf8');
}
console.log('挥发物种表:', jc.includes('VOLATILE'));

/* ---------- 2) 生成器输出这张表 ---------- */
let g = 'D:/ChemLabUnity/tools/gen_cs.js';
let gc = fs.readFileSync(g, 'utf8');
if (!gc.includes('const volatiles')) {
  gc = gc.replace('/* 气体的酸碱性：气体id 性质(base/acid/neut/bleach) —— 石蕊检验的依据 */',
`/* 易挥发物种：溶在水里但上方有蒸气，气体检验能检出 */
const volatiles = Object.entries(api.VOLATILE || {}).map(([k, v]) => row([k, v]));

/* 气体的酸碱性：气体id 性质(base/acid/neut/bleach) —— 石蕊检验的依据 */`);
}
if (!gc.includes('TblVolatile')) {
  gc = gc.replace('/* ---------- 气体的酸碱性（石蕊检验的依据） ---------- */',
`/* ---------- 易挥发物种：物种id 蒸气对应气体id ---------- */
public const string TblVolatile = @"
\${volatiles.join('\\n')}
";

/* ---------- 气体的酸碱性（石蕊检验的依据） ---------- */`);
}
fs.writeFileSync(g, gc, 'utf8');
console.log('生成器:', gc.includes('const volatiles'), gc.includes('TblVolatile'));

/* ---------- 3) ChemCore 解析 ---------- */
let k = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemCore.cs';
let kc = fs.readFileSync(k, 'utf8');
if (!kc.includes('Volatile')) {
  kc = kc.replace('    static readonly Dictionary<string, string[]> GasSpl = new Dictionary<string, string[]>();',
`    static readonly Dictionary<string, string[]> GasSpl = new Dictionary<string, string[]>();
    static readonly Dictionary<string, string> Volatile = new Dictionary<string, string>();`);
  kc = kc.replace(`        foreach (var ln in Lines(TblGasNature)) {`,
`        foreach (var ln in Lines(TblVolatile)) {
            var cv = Cols(ln);
            if (cv.Length >= 2) Volatile[cv[0]] = cv[1];
        }
        foreach (var ln in Lines(TblGasNature)) {`);
  kc = kc.replace('    public static double DissolveHeat(string id) {',
`    /// <summary>该物种溶于水时上方蒸气对应哪种气体；不挥发返回 null</summary>
    public static string VapourOf(string id) {
        string v; return Volatile.TryGetValue(id ?? "", out v) ? v : null;
    }

    public static double DissolveHeat(string id) {`);
  fs.writeFileSync(k, kc, 'utf8');
}
console.log('ChemCore:', kc.includes('VapourOf'));
