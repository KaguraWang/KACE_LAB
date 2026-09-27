const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemCore.cs';
let c = fs.readFileSync(p, 'utf8');
const R = (a, b) => { if (c.includes(a)) { c = c.replace(a, b); return true; } return false; };

// 1) 两张表的存储
const r1 = R('    static readonly Dictionary<string, double> RuleH = new Dictionary<string, double>();',
`    static readonly Dictionary<string, double> RuleH = new Dictionary<string, double>();
    /* ---- 气体检验响应表（离子检验用） ---- */
    static readonly Dictionary<string, string> GasNat = new Dictionary<string, string>();
    static readonly Dictionary<string, string[]> GasSpl = new Dictionary<string, string[]>();`);

// 2) 解析
const r2 = R(`        /* 中文名 → id，用于把列表里的物质名整体翻成英文 */`,
`        foreach (var ln in Lines(TblGasNature)) {
            var c2 = Cols(ln);
            if (c2.Length >= 2) GasNat[c2[0]] = c2[1];
        }
        foreach (var ln in Lines(TblGasSplint)) {
            var c2 = Cols(ln);
            if (c2.Length >= 4) GasSpl[c2[0]] = new[] { c2[1], c2[2], c2[3] };
        }

        /* 中文名 → id，用于把列表里的物质名整体翻成英文 */`);

// 3) 查询接口
const r3 = R('    public static double DissolveHeat(string id) {',
`    /* ---- 气体检验查表 ---- */
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

    public static double DissolveHeat(string id) {`);

fs.writeFileSync(p, c, 'utf8');
console.log(`ChemCore 补丁: 存储=${r1} 解析=${r2} 接口=${r3}`);

// 4) Vessel.Fx.Pop
const q = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemEngine.cs';
let e = fs.readFileSync(q, 'utf8');
const r4 = e.includes('public float Pop;') ? false :
  (e = e.replace('    public class VesselEffects {\n        public float Bubble, Smoke, Glow;',
`    public class VesselEffects {
        public float Bubble, Smoke, Glow;
        /// <summary>爆鸣强度（>0 表示刚发生一次爆鸣，界面据此播音并闪烁）</summary>
        public float Pop;`), true);
fs.writeFileSync(q, e, 'utf8');
console.log('Fx.Pop:', r4);
