const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点未命中:', a.slice(0, 50).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ---- 1) 气态物种要进气相（原来直接落进未溶物，气体检验全查不到） ---- */
P('D:/ChemLabUnity/Assets/Scripts/Chem/ChemEngine.cs',
`        AddTo(Undissolved, id, mol);
        return true;
    }`,
`        /* 气态物质要进气相。原来这里没有出口，CH4/O2 这类直接被塞进「未溶物」，
           气相永远是空的 —— 木条、石蕊这些气体检验就什么也检不出来。 */
        if (s.State == "g") { AddTo(Gases, id, mol); return true; }
        AddTo(Undissolved, id, mol);
        return true;
    }`);

/* ---- 2) 石灰水浓度门槛 ---- */
P('D:/ChemLabUnity/Assets/Scripts/Chem/ChemEngine.cs',
`            double extent = Math.Min(c / pr.NC, a / pr.NA);
            if (extent <= 1e-9) continue;
            v.TakeAny(pr.Cat, extent * pr.NC);`,
`            /* Ca(OH)2 微溶：只有浓度较高时才析出。澄清石灰水（约 0.02 mol/dm3）
               必须是清液 —— 否则「通 CO2 使石灰水变浑浊」这个最经典的检验
               一加进去就先自己浑浊了。 */
            if (pr.Id == "caoh2") {
                double vol0 = Math.Max(0.001, v.Water / 1000.0);
                if (c / vol0 < 0.10 || a / vol0 < 0.20) continue;
            }
            double extent = Math.Min(c / pr.NC, a / pr.NA);
            if (extent <= 1e-9) continue;
            v.TakeAny(pr.Cat, extent * pr.NC);`);

/* ---- 3) 石蕊「不变色」时 Tint 要清空（否则和「变成蓝色」分不开） ---- */
P('D:/ChemLabUnity/Assets/Scripts/Chem/GasTests.cs',
`                } else {
                    t.Tint = BlueAsIs;
                    t.LastResult = $"蓝色石蕊试纸在碱性气体中保持蓝色，没有变化（{ChemData.NameOf(gas)}）";
                }`,
`                } else {
                    /* 保持原色 —— Tint 必须留空。若填成本身的蓝色，
                       「没变色」和「变成了蓝色」就无法区分了。 */
                    t.Tint = "";
                    t.LastResult = $"蓝色石蕊试纸在碱性气体中保持蓝色，没有变化（{ChemData.NameOf(gas)}）";
                }`);
P('D:/ChemLabUnity/Assets/Scripts/Chem/GasTests.cs',
`                } else {
                    t.Tint = RedAsIs;
                    t.LastResult = $"红色石蕊试纸在酸性气体中保持红色，没有变化（{ChemData.NameOf(gas)}）";
                }`,
`                } else {
                    t.Tint = "";
                    t.LastResult = $"红色石蕊试纸在酸性气体中保持红色，没有变化（{ChemData.NameOf(gas)}）";
                }`);

/* ---- 4) popped 的 max 判定：缺失键返回的是 JVal.Null 而不是 C# null ---- */
P('D:/ChemLabUnity/Assets/Editor/SpecRunner.cs',
`                            bool hasMin = asrt["min"] != null, hasMax = asrt["max"] != null;
                            if (hasMax)       pass = pv <= asrt["max"].Num(0);
                            else if (hasMin)  pass = pv >= asrt["min"].Num(0);
                            else              pass = pv > 1e-9;`,
`                            /* 注意：缺失的键拿到的是 JVal.Null 而不是 C# null，
                               所以要用「有没有给定值」来判断，不能用 != null。 */
                            double mx = asrt["max"].Num(-1), mn = asrt["min"].Num(-1);
                            bool hasMin = mn >= 0, hasMax = mx >= 0;
                            if (hasMax)       pass = pv <= mx;
                            else if (hasMin)  pass = pv >= mn;
                            else              pass = pv > 1e-9;`);

/* ---- 5) 亚硫酸氢根也是 SO2 的蒸气来源 ---- */
let j = 'D:/ChemLab/js/gastests.js';
let jc = fs.readFileSync(j, 'utf8');
if (!jc.includes("'hso3-'")) {
  jc = jc.replace("  'nh3': 'nh3', 'so2': 'so2', 'co2': 'co2', 'h2s': 'h2s',",
`  'nh3': 'nh3', 'so2': 'so2', 'co2': 'co2', 'h2s': 'h2s',
  /* 亚硫酸及其酸式根同样向气相提供 SO2 —— 溶解后 SO2 变成了 H+ + HSO3-，
     只登记 so2 的话溶解完就检不出来了 */
  'hso3-': 'so2', 'h2so3': 'so2',
  /* 碳酸氢根同理 */
  'hco3-': 'co2', 'h2co3': 'co2',
  /* 硫化氢溶解后是 HS- */
  'hs-': 'h2s', 's2-': 'h2s',`);
  fs.writeFileSync(j, jc, 'utf8'); n++;
}

console.log('补丁处数:', n);
