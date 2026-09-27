const fs = require('fs');
let n = 0;
const P = (p, a, b) => { let c = fs.readFileSync(p, 'utf8'); if (!c.includes(a)) { console.log('  ✗ 锚点未命中'); return false; } fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true; };

/* ---- 1) Ca(OH)2 的浓度门槛：澄清石灰水必须是清液 ---- */
P('D:/ChemLabUnity/Assets/Scripts/Chem/ChemEngine.cs',
`            if (!ChemData.Precip.TryGetValue(s.Formula, out var info)) continue;
            double extent = Math.Min(c / pr.NC, a / pr.NA);`,
`            if (!ChemData.Precip.TryGetValue(s.Formula, out var info)) continue;
            /* Ca(OH)2 微溶：只有浓度较高时才析出。澄清石灰水（约 0.02 mol/dm3）
               必须是清液 —— 否则「通 CO2 使石灰水变浑浊」这个最经典的检验
               一加进去就先自己浑浊了。 */
            if (pr.Id == "caoh2") {
                double vol0 = Math.Max(0.001, v.Water / 1000.0);
                if (c / vol0 < 0.10 || a / vol0 < 0.20) continue;
            }
            double extent = Math.Min(c / pr.NC, a / pr.NA);`);

/* ---- 2) 挥发物种计入气相：氨水上方就是氨气 ---- */
P('D:/ChemLabUnity/Assets/Scripts/Chem/GasTests.cs',
`    /// <summary>容器里含量最多的气体（检验的对象）</summary>
    public static string DominantGas(Vessel v) {
        string best = null; double bm = 1e-9;
        foreach (var kv in v.Gases) if (kv.Value > bm) { bm = kv.Value; best = kv.Key; }
        return best;
    }`,
`    /// <summary>容器里含量最多的气体（检验的对象）。
    /// 除了气相，**溶在水里的挥发性物种也算** —— 氨水上方就是氨气，
    /// 亚硫酸上方就是二氧化硫，气体检验正是靠这一点。只查气相的话，
    /// 这些在数据里以「xx 溶液」形式存在的物种永远检不出来。</summary>
    public static string DominantGas(Vessel v) {
        string best = null; double bm = 1e-9;
        foreach (var kv in v.Gases) if (kv.Value > bm) { bm = kv.Value; best = kv.Key; }
        /* 溶液里的挥发性成分折算成蒸气 */
        foreach (var kv in v.Solutes) {
            string vap = ChemData.VapourOf(kv.Key);
            if (vap == null) continue;
            /* 已经算过气相就不重复计 */
            if (v.Gases.ContainsKey(vap)) continue;
            if (kv.Value > bm) { bm = kv.Value; best = vap; }
        }
        return best;
    }`);

/* ---- 3) popped 断言支持 max ---- */
P('D:/ChemLabUnity/Assets/Editor/SpecRunner.cs',
`                        case "popped": {
                            /* 是否发生了爆鸣 */
                            double pv = v.Fx.Pop;
                            if (asrt["min"].Num(0) > 0) { pass = pv >= asrt["min"].Num(0); }
                            else { pass = pv > 1e-9; }
                            detail = pass ? $"发生了爆鸣（强度 {pv:F2}）" : "没有发生爆鸣";
                            break;
                        }`,
`                        case "popped": {
                            /* 是否发生了爆鸣。min 给下限、max 给上限（用于「不该爆鸣」的负向验证） */
                            double pv = v.Fx.Pop;
                            bool hasMin = asrt["min"] != null, hasMax = asrt["max"] != null;
                            if (hasMax)       pass = pv <= asrt["max"].Num(0);
                            else if (hasMin)  pass = pv >= asrt["min"].Num(0);
                            else              pass = pv > 1e-9;
                            detail = pass ? $"爆鸣强度 {pv:F2}（符合期望）"
                                          : $"爆鸣强度 {pv:F2}（不符合期望 {(hasMax ? "≤" + asrt["max"].Num(0) : "≥" + asrt["min"].Num(0))}）";
                            break;
                        }`);

/* ---- 4) 补碳酸铵 ---- */
let jc = fs.readFileSync('D:/ChemLab/js/cie9701.js', 'utf8');
if (!jc.includes("'nh42co3'")) {
  jc = jc.replace('SUBSTANCES.push(...[', `SUBSTANCES.push(...[
  ['nh42co3','碳酸铵','(NH4)2CO3','s','#f8f8f8','#eef4f8',1.50,'k','salt','受热分解，遇酸放出 CO2，遇碱放出 NH3'],`);
  fs.writeFileSync('D:/ChemLab/js/cie9701.js', jc, 'utf8');
  n++;
}
let ec = fs.readFileSync('D:/ChemLab/js/engine.js', 'utf8');
if (!ec.includes("'nh42co3':")) {
  ec = ec.replace("  'na2so3':  { ions: { 'Na+': 2, 'SO3^2-': 1 } },",
                  "  'na2so3':  { ions: { 'Na+': 2, 'SO3^2-': 1 } },\n  'nh42co3': { ions: { 'NH4+': 2, 'CO3^2-': 1 } },");
  fs.writeFileSync('D:/ChemLab/js/engine.js', ec, 'utf8');
  n++;
}

console.log('补丁处数:', n);
