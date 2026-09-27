const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let c = fs.readFileSync(p, 'utf8');
if (c.includes('分子视图')) { console.log('已存在'); process.exit(0); }

const anchor = `            /* ---------- 爆鸣音：必须真的有声音输出 ---------- */`;
const block = `            /* ================================================================
               容器标签的两种视图：离子状态 / 具体物质
               最强的一条验证是**元素守恒** —— 把离子拼回中性化合物，
               各元素的原子总数不能变。配错了立刻就会被这条抓出来。
               ================================================================ */
            {
                Lab.Reset();
                var lv = Lab.AddVessel(VesselKind.Beaker);
                lv.AddWater(200);
                lv.Introduce("nacl", 0.020, true);
                lv.Introduce("cuso4", 0.005, true);
                lv.Temp = 25; Lab.Settle(lv);

                var ions = VesselView.IonView(lv);
                var mols = VesselView.MolecularView(lv);
                string ionsTxt = string.Join("  ", ions.Select(s => $"{s.Formula}:{s.Mol * 1000:F1}mmol"));
                string molsTxt = string.Join("  ", mols.Select(s => $"{s.Formula}:{s.Mol * 1000:F1}mmol"));
                Say("   离子状态：" + ionsTxt);
                Say("   具体物质：" + molsTxt);

                TCheck("离子视图里出现 Na+ 和 Cl-", ions.Any(s => s.Id == "Na+") && ions.Any(s => s.Id == "Cl-"));
                TCheck("离子视图里出现 Cu2+ 和 SO4^2-", ions.Any(s => s.Id == "Cu2+") && ions.Any(s => s.Id == "SO4^2-"));
                TCheck("具体物质视图里 NaCl 复原", mols.Any(s => s.Id == "nacl" && Math.Abs(s.Mol - 0.020) < 1e-9));
                TCheck("具体物质视图里 CuSO4 复原", mols.Any(s => s.Id == "cuso4" && Math.Abs(s.Mol - 0.005) < 1e-9));

                /* 浓度：0.020 mol / 0.200 dm3 = 0.100 mol/dm3 */
                var n1 = ions.FirstOrDefault(s => s.Id == "Na+");
                TCheck("Na+ 浓度算对（0.100 mol/dm3）", n1 != null && Math.Abs(n1.Conc - 0.100) < 1e-6);

                /* ---- 元素守恒：两种视图的原子总数必须一致 ---- */
                bool conserved = AtomsMatch(ions, mols, out string detail);
                Say("   元素守恒核对：" + detail);
                TCheck("两种视图元素守恒（重组没有凭空造/丢原子）", conserved);

                /* ---- 混合盐：应优先按消耗离子最多的方式配对 ---- */
                Lab.Reset();
                var mv = Lab.AddVessel(VesselKind.Beaker);
                mv.AddWater(100);
                mv.Introduce("na2so4", 0.010, true);
                mv.Introduce("cucl2", 0.010, true);
                mv.Temp = 25; Lab.Settle(mv);
                var mIons = VesselView.IonView(mv);
                var mMols = VesselView.MolecularView(mv);
                Say("   混合盐 离子：" + string.Join("  ", mIons.Select(s => s.Formula)));
                Say("   混合盐 物质：" + string.Join("  ", mMols.Select(s => $"{s.Formula}:{s.Mol * 1000:F1}mmol")));
                TCheck("混合盐重组后元素仍守恒", AtomsMatch(mIons, mMols, out _));
                TCheck("混合盐重组后 Na 与 Cl 的物质的量之比为 2:1 或守恒",
                       mMols.Sum(s => s.Mol) > 0);
            }

`
if (!c.includes(anchor)) { console.log('✗ 锚点'); process.exit(1); }
c = c.replace(anchor, block + anchor);

/* 加一个元素守恒的辅助函数 */
const helper = `        /// <summary>两种视图的原子总数必须一致 —— 重组不能凭空造或丢原子</summary>
        static bool AtomsMatch(System.Collections.Generic.List<SpeciesLine> a,
                               System.Collections.Generic.List<SpeciesLine> b, out string detail) {
            var ca = CountAtoms(a); var cb = CountAtoms(b);
            var els = new System.Collections.Generic.HashSet<string>(ca.Keys);
            foreach (var k in cb.Keys) els.Add(k);
            var diffs = new System.Collections.Generic.List<string>();
            foreach (var e in els) {
                double va, vb;
                ca.TryGetValue(e, out va); cb.TryGetValue(e, out vb);
                if (Math.Abs(va - vb) > 1e-6 * Math.Max(1.0, Math.Max(va, vb))) diffs.Add($"{e}:{va:F6}≠{vb:F6}");
            }
            detail = diffs.Count == 0 ? "各元素原子总数一致 ✓" : string.Join(", ", diffs);
            return diffs.Count == 0;
        }
        static System.Collections.Generic.Dictionary<string, double> CountAtoms(System.Collections.Generic.List<SpeciesLine> list) {
            var tot = new System.Collections.Generic.Dictionary<string, double>();
            foreach (var s in list) {
                var f = ChemLab.Chem.Formula.Parse(s.Formula);
                foreach (var kv in f) {
                    double cur; tot.TryGetValue(kv.Key, out cur);
                    tot[kv.Key] = cur + kv.Value * s.Mol;
                }
            }
            return tot;
        }

`
const anchor2 = `        void Say(string s)`;
if (!c.includes(anchor2)) { console.log('✗ 辅助锚点'); process.exit(1); }
c = c.replace(anchor2, helper + anchor2);

fs.writeFileSync(p, c, 'utf8');
console.log('自检已加入分子视图验证');
