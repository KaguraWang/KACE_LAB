const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let c = fs.readFileSync(p, 'utf8');
if (c.includes('焓变计算')) { console.log('已存在'); process.exit(0); }

const anchor = `            /* ---------- 爆鸣音：必须真的有声音输出 ---------- */`;
const block = `            /* ================================================================
               焓变计算：键能法 / 生成焓法 / 盖斯定律
               三种算法互相验证 —— 同一条反应用不同路径算出来必须一致。
               ================================================================ */
            {
                HessLaw.Init();

                /* ---- 1) 键能表 ---- */
                Say($"   H-H 键能 = {HessLaw.BondEnergy("H","H",1):F0} kJ/mol（期望 436）");
                TCheck("H-H 键能查表正确", Math.Abs(HessLaw.BondEnergy("H", "H", 1) - 436) < 0.5);
                TCheck("O=O 键能查表正确", Math.Abs(HessLaw.BondEnergy("O", "O", 2) - 496) < 0.5);
                TCheck("C-H 键能查表正确", Math.Abs(HessLaw.BondEnergy("C", "H", 1) - 410) < 0.5);
                /* 键能表里 A-B 与 B-A 是同一个键 */
                TCheck("键能查表对顺序不敏感",
                       Math.Abs(HessLaw.BondEnergy("H", "Cl", 1) - HessLaw.BondEnergy("Cl", "H", 1)) < 1e-9);

                /* ---- 2) 用键能算 H2 + Cl2 -> 2HCl ---- */
                /* ΔH = (H-H + Cl-Cl) − 2×(H-Cl) = (436 + 242) − 2×431 = −184 kJ/mol */
                double hh = HessLaw.BondEnergy("H", "H", 1), clcl = HessLaw.BondEnergy("Cl", "Cl", 1),
                       hcl = HessLaw.BondEnergy("H", "Cl", 1);
                double dhBond = (hh + clcl) - 2 * hcl;
                Say($"   键能法 H2+Cl2→2HCl：ΔH = ({hh:F0}+{clcl:F0}) − 2×{hcl:F0} = {dhBond:F0} kJ/mol");
                TCheck("键能法算 H2+Cl2→2HCl 得 −184 kJ/mol", Math.Abs(dhBond - (-184)) < 1.5);

                /* ---- 3) 生成焓法：甲烷燃烧 ---- */
                /* CH4 + 2O2 -> CO2 + 2H2O
                   ΔH = [ΔHf(CO2) + 2ΔHf(H2O)] − ΔHf(CH4)
                      = [−394 + 2×(−286)] − (−75) = −891 kJ/mol */
                double fco2 = HessLaw.FormationOf("co2"), fh2o = HessLaw.FormationOf("h2o"),
                       fch4 = HessLaw.FormationOf("ch4");
                double dhForm = (fco2 + 2 * fh2o) - fch4;
                Say($"   生成焓法 CH4+2O2→CO2+2H2O：ΔH = ({fco2:F0}+2×{fh2o:F0}) − ({fch4:F0}) = {dhForm:F0} kJ/mol");
                TCheck("生成焓法算甲烷燃烧得 −891 kJ/mol", Math.Abs(dhForm - (-891)) < 3);

                /* ---- 4) 查表 ---- */
                double dhTab = HessLaw.Lookup("CH4 + 2O2 -> CO2 + 2H2O", out string note);
                Say($"   查表 CH4 燃烧：{dhTab:F0} kJ/mol（{note}）");
                TCheck("反应焓查表命中", !double.IsNaN(dhTab) && Math.Abs(dhTab - (-890)) < 1);

                /* ---- 5) 盖斯定律：沿闭合回路 ΣΔH 必须为 0 ---- */
                /* 造一个三角形回路：
                     A --(ΔH1=+100)--> B --(ΔH2=−250)--> C --(ΔH3=?)--> A
                   闭合要求 100 − 250 + ΔH3 = 0 → ΔH3 = +150 */
                var edges = new System.Collections.Generic.List<HessEdge> {
                    new HessEdge { From = 0, To = 1, DH = 100,  Source = EnthalpySource.Manual },
                    new HessEdge { From = 1, To = 2, DH = -250, Source = EnthalpySource.Manual },
                    new HessEdge { From = 2, To = 0, DH = 0,    Source = EnthalpySource.Unknown },
                };
                var cyc = HessLaw.FindCycle(3, edges);
                Say("   找到回路：" + string.Join(" → ", cyc));
                TCheck("能找到闭合回路", cyc.Count >= 4);
                bool ok = HessLaw.SolveCycle(cyc, edges, out double unk, out string why);
                Say("   " + why);
                TCheck("盖斯定律解出未知焓变 = +150 kJ/mol", ok && Math.Abs(unk - 150) < 1e-6);

                /* ---- 6) 未知量多于一个时应当拒绝求解 ---- */
                var edges2 = new System.Collections.Generic.List<HessEdge> {
                    new HessEdge { From = 0, To = 1, DH = 0, Source = EnthalpySource.Unknown },
                    new HessEdge { From = 1, To = 2, DH = 0, Source = EnthalpySource.Unknown },
                    new HessEdge { From = 2, To = 0, DH = 50, Source = EnthalpySource.Manual },
                };
                var cyc2 = HessLaw.FindCycle(3, edges2);
                bool ok2 = HessLaw.SolveCycle(cyc2, edges2, out _, out string why2);
                Say("   " + why2);
                TCheck("两个未知量时应拒绝求解并说明原因", !ok2);

                /* ---- 7) 交叉验证：同一反应用键能法与查表法结果应接近 ---- */
                double viaTable = HessLaw.Lookup("H2 + Cl2 -> 2HCl", out _);
                Say($"   交叉验证 H2+Cl2→2HCl：键能法 {dhBond:F0} vs 查表 {viaTable:F0} kJ/mol");
                TCheck("键能法与查表法结果一致（差 < 3 kJ/mol）",
                       !double.IsNaN(viaTable) && Math.Abs(dhBond - viaTable) < 3);
            }

`
if (!c.includes(anchor)) { console.log('✗ 锚点'); process.exit(1); }
c = c.replace(anchor, block + anchor);
fs.writeFileSync(p, c, 'utf8');
console.log('✓ 焓变自检已加入');
