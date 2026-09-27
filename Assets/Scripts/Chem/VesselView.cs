/* =====================================================================
   容器的两种视图：离子状态 / 具体物质
     离子状态：直接用引擎溶质表里的物种（Na+、Cl-、Cu2+ ...）
     具体物质：用电离表**反向配对**，把离子拼回中性化合物
               （Na+ + Cl- -> NaCl；Cu2+ + SO4^2- -> CuSO4）

   配对用贪心：每一轮挑「能完全满足、且消耗离子最多」的那个化合物，
   这样 Cu2+ 会优先和 SO4^2- 配成 CuSO4，而不是先被别的少量阴离子抢走。
   ===================================================================== */
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChemLab.Chem {

/// <summary>标签里的一行</summary>
public class SpeciesLine {
    public string Id;
    public string Name;
    public string Formula;
    public double Mol;
    public double Conc;        // mol/dm3
    public bool IsIon;
    public bool IsPrecipitate;
    public bool IsUndissolved;
    public bool IsGas;
}

public static class VesselView {

    /// <summary>H+ / OH- 不参与反向配对 —— 它们配对出来是水，
    /// 而水不是「溶质里的具体物质」，显示成 H2O 反而误导。</summary>
    static readonly HashSet<string> NoRecombine = new HashSet<string> { "h2o", "H2O" };

    /// <summary>离子状态：引擎溶质表原样输出</summary>
    public static List<SpeciesLine> IonView(Vessel v) {
        var list = new List<SpeciesLine>();
        double vol = VolumeDm3(v);
        foreach (var kv in v.Solutes) {
            if (kv.Value <= 1e-12) continue;
            list.Add(Make(kv.Key, kv.Value, vol, false));
        }
        return list.OrderByDescending(x => x.Mol).ToList();
    }

    /// <summary>具体物质：把离子反向配对回中性化合物，剩下的离子照原样列出</summary>
    public static List<SpeciesLine> MolecularView(Vessel v) {
        var pool = new Dictionary<string, double>();
        foreach (var kv in v.Solutes) {
            if (kv.Value <= 1e-12) continue;
            pool[kv.Key] = kv.Value;
        }
        double vol = VolumeDm3(v);

        /* ---- 第一步：账本里记着的化合物直接算数 ----
           光看离子表是**推不出**原本是什么化合物的：NaCl + CuSO4 与
           CuCl2 + Na2SO4 给出的离子完全相同，任何贪心配对都只是其中一种可能。
           所以以投入时记的账本为准。 */
        var result = new Dictionary<string, double>();
        foreach (var kv in v.Ledger) {
            if (kv.Value <= 1e-12) continue;
            ChemData.Diss.TryGetValue(kv.Key, out var ld);
            if (ld == null || ld.Ions == null || ld.Ions.Count == 0) continue;
            /* 账本量不能超过现存离子能支撑的量（反应消耗后离子会变少） */
            double cap = double.MaxValue;
            foreach (var ion in ld.Ions) {
                double have; pool.TryGetValue(ion.Key, out have);
                cap = System.Math.Min(cap, have / ion.Value);
            }
            if (cap <= 1e-12) continue;
            double use = System.Math.Min(kv.Value, cap);
            foreach (var ion in ld.Ions) {
                pool[ion.Key] -= ion.Value * use;
                if (pool[ion.Key] <= 1e-12) pool.Remove(ion.Key);
            }
            result[kv.Key] = use;
        }

        /* ---- 第二步：账本没覆盖到的离子，再用贪心配对兜底 ----
           每一轮找「能完全满足、且消耗离子总量最多」的化合物 */
        bool progress = true;
        int guard = 0;
        while (progress && guard++ < 200) {
            progress = false;
            string bestId = null; double bestExtent = 0; double bestCost = 0;
            foreach (var kv in ChemData.Diss) {
                var id = kv.Key; var dis = kv.Value;
                if (dis == null || dis.Ions == null || dis.Ions.Count == 0) continue;
                if (NoRecombine.Contains(id)) continue;
                /* 该化合物所需的每种离子都要够 */
                double extent = double.MaxValue; double cost = 0;
                bool ok = true;
                foreach (var ion in dis.Ions) {
                    double have; pool.TryGetValue(ion.Key, out have);
                    if (have < ion.Value * 1e-9) { ok = false; break; }
                    extent = System.Math.Min(extent, have / ion.Value);
                    cost += ion.Value;
                }
                if (!ok || extent <= 1e-9) continue;
                /* 优先消耗离子多的，其次消耗量大的 */
                if (cost > bestCost + 1e-9 || (System.Math.Abs(cost - bestCost) < 1e-9 && extent > bestExtent)) {
                    bestId = id; bestExtent = extent; bestCost = cost;
                }
            }
            if (bestId != null) {
                var dis = ChemData.Diss[bestId];
                foreach (var ion in dis.Ions) {
                    pool[ion.Key] -= ion.Value * bestExtent;
                    if (pool[ion.Key] <= 1e-12) pool.Remove(ion.Key);
                }
                pool.TryGetValue(bestId, out double cur);
                pool[bestId] = cur + bestExtent;
                progress = true;
            }
        }

        var list = new List<SpeciesLine>();
        foreach (var kv in result) {
            if (kv.Value <= 1e-12) continue;
            list.Add(Make(kv.Key, kv.Value, vol, false));
        }
        /* 既不在账本里、也配不成化合物的散离子，如实列出 */
        foreach (var kv in pool) {
            if (kv.Value <= 1e-12) continue;
            list.Add(Make(kv.Key, kv.Value, vol, false));
        }
        return list.OrderByDescending(x => x.Mol).ToList();
    }

    /// <summary>按当前设置取视图</summary>
    public static List<SpeciesLine> Lines(Vessel v, bool molecular) =>
        molecular ? MolecularView(v) : IonView(v);

    /// <summary>沉淀 / 未溶物 / 气体也各给一行（标签里分开标注）</summary>
    public static List<SpeciesLine> AllLines(Vessel v, bool molecular) {
        var list = Lines(v, molecular);
        double vol = VolumeDm3(v);
        foreach (var kv in v.Precipitates) {
            if (kv.Value <= 1e-12) continue;
            var sl = Make(kv.Key, kv.Value, vol, false); sl.IsPrecipitate = true; list.Add(sl);
        }
        foreach (var kv in v.Undissolved) {
            if (kv.Value <= 1e-12) continue;
            var sl = Make(kv.Key, kv.Value, vol, false); sl.IsUndissolved = true; list.Add(sl);
        }
        foreach (var kv in v.Gases) {
            if (kv.Value <= 1e-12) continue;
            var sl = Make(kv.Key, kv.Value, vol, false); sl.IsGas = true; list.Add(sl);
        }
        return list;
    }

    static double VolumeDm3(Vessel v) {
        double ml = v.Water > 0 ? v.Water : v.Volume();
        // Concentration has no meaning after the last liquid evaporates. A
        // tiny invented denominator displayed residual ions as billions of M.
        return ml > 1e-9 ? ml / 1000.0 : double.NaN;
    }

    static SpeciesLine Make(string id, double mol, double vol, bool _) {
        ChemData.Sub.TryGetValue(id, out var s);
        bool isIon = s != null && s.IsIon;
        return new SpeciesLine {
            Id = id,
            Name = ChemData.NameOf(id),
            Formula = s != null ? s.Formula : id,
            Mol = mol,
            Conc = mol / vol,
            IsIon = isIon,
        };
    }
}

}
