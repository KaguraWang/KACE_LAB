const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点未命中:', a.slice(0, 46).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ---- 1) 亚硫酸盐遇酸放出 SO2（CIE anions 表里 SO3^2- 那一条的化学本体） ---- */
let r = fs.readFileSync('D:/ChemLab/js/rules.js', 'utf8');
if (!r.includes("SO3^2- + 2H+ -> SO2")) {
  r = r.replace("R('S2O3^2- + 2H+ -> SO2 + S + H2O'",
`/* CIE anions：SO3^2- 遇稀酸放出 SO2（刺激性气味，能使酸性 KMnO4 褪色、使品红褪色） */
R('SO3^2- + 2H+ -> SO2 + H2O', '产生有刺激性气味的气体，能使湿润的蓝色石蕊试纸变红', { gas:'so2', bubble: 3, odor: 2 }),
R('Na2SO3 + 2HCl -> 2NaCl + SO2 + H2O', '固体溶解并产生有刺激性气味的气体', { gas:'so2', bubble: 3, odor: 2 }),
R('S2O3^2- + 2H+ -> SO2 + S + H2O'`);
  fs.writeFileSync('D:/ChemLab/js/rules.js', r, 'utf8'); n++;
}

/* ---- 2) 气体转移操作：把 A 里产生的气体通入 B（石灰水检验就是这么做的） ---- */
P('D:/ChemLabUnity/Assets/Editor/SpecRunner.cs',
`                    case "advance": {`,
`                    case "transfer_gas": {
                        /* 把一只容器里产生的气体通入另一只容器 ——
                           石灰水检验必须让 CO2 进到**独立的**石灰水里，
                           直接倒在同一个容器里会被溶液里的其它离子（如 Zn2+）抢走 OH-。 */
                        string toId = a["to"].Str("");
                        if (!vs.ContainsKey(toId)) break;
                        var dst = vs[toId];
                        string only = a["gas"].Str("");
                        double move = a["mol"].Num(0);
                        var keys = new List<string>(v.Gases.Keys);
                        foreach (var gk in keys) {
                            if (only.Length > 0 && gk != only) continue;
                            double amt = move > 0 ? Math.Min(v.Gases[gk], move) : v.Gases[gk];
                            if (amt <= 1e-12) continue;
                            v.Gases[gk] -= amt;
                            if (v.Gases[gk] <= 1e-12) v.Gases.Remove(gk);
                            dst.Gases.TryGetValue(gk, out double cur);
                            dst.Gases[gk] = cur + amt;
                        }
                        Lab.Note(dst, "将生成的气体通入该容器", "add");
                        Lab.Settle(dst);
                        break;
                    }
                    case "advance": {`);

console.log('补丁处数:', n);
