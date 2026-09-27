const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 46).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};
const E = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemEngine.cs';

/* ---- 1) 化合物账本字段 ---- */
P(E, `    public double Water;          // mL`,
`    /// <summary>化合物账本：记录「以中性化合物形式投入、且还没被消耗掉」的量。
    /// 「显示成具体物质」就是靠它 —— 光看离子表是**推不出**原本是什么化合物的：
    /// NaCl + CuSO4 和 CuCl2 + Na2SO4 给出的离子完全相同，必须记账。</summary>
    public readonly Dictionary<string, double> Ledger = new Dictionary<string, double>();
    public double Water;          // mL`);

/* ---- 2) Introduce 记账 ---- */
P(E, `            if (solubleHere && s.State != "g") {
            if (dis != null && !dis.None && dis.Ions.Count > 0) {`,
`            if (solubleHere && s.State != "g") {
            if (dis != null && !dis.None && dis.Ions.Count > 0) {`);
P(E, `                double d = dis.Weak ? dis.Degree : 1.0;`,
`                /* 记入账本：这是「具体物质」视图的数据来源 */
                AddTo(Ledger, id, mol);
                double d = dis.Weak ? dis.Degree : 1.0;`);

/* ---- 3) TakeAny 消耗离子时按比例回扣账本 ---- */
P(E, `    public double TakeAny(string id, double mol) {
        if (id == "h2o") {`,
`    /// <summary>某种化合物按电离表能提供多少目标离子</summary>
    double LedgerIonShare(string compound, string ion, out double perMol) {
        perMol = 0;
        ChemData.Diss.TryGetValue(compound, out var dis);
        if (dis == null || dis.Ions == null) return 0;
        double v; if (!dis.Ions.TryGetValue(ion, out v)) return 0;
        perMol = v;
        double have; Ledger.TryGetValue(compound, out have);
        return have * v;
    }

    /// <summary>消耗了某个离子 -> 按贡献比例回扣账本里的化合物。
    /// 这样 Na+ 被反应吃掉时，账本里的 NaCl 会相应减少，
    /// 「具体物质」视图不会显示已经反应掉的物质。</summary>
    void ChargeLedger(string ion, double mol) {
        if (mol <= 1e-12 || Ledger.Count == 0) return;
        var shares = new List<KeyValuePair<string, double>>();
        double total = 0;
        foreach (var kv in Ledger) {
            double per; double s = LedgerIonShare(kv.Key, ion, out per);
            if (s > 1e-12) { shares.Add(new KeyValuePair<string, double>(kv.Key, s)); total += s; }
        }
        if (total <= 1e-12) return;
        /* 账本里能提供的该离子不够，就按比例扣光能扣的部分 */
        double use = Math.Min(mol, total);
        foreach (var kv in shares) {
            double per; LedgerIonShare(kv.Key, ion, out per);
            double take = use * (kv.Value / total) / Math.Max(per, 1e-12);
            double have; Ledger.TryGetValue(kv.Key, out have);
            double left = have - take;
            if (left <= 1e-12) Ledger.Remove(kv.Key); else Ledger[kv.Key] = left;
        }
    }

    public double TakeAny(string id, double mol) {
        if (id == "h2o") {`);

P(E, `        double left = mol;
        foreach (var map in new[]{ Solutes, Undissolved, Gases, Precipitates }) {`,
`        /* 离子被取用时同步回扣账本 */
        if (ChemData.Sub.TryGetValue(id, out var st) && st.IsIon) ChargeLedger(id, mol);
        double left = mol;
        foreach (var map in new[]{ Solutes, Undissolved, Gases, Precipitates }) {`);

/* ---- 4) Empty / Reset 清空账本 ---- */
P(E, `        v.Solutes.Clear(); v.Undissolved.Clear(); v.Gases.Clear(); v.Precipitates.Clear();`,
`        v.Solutes.Clear(); v.Undissolved.Clear(); v.Gases.Clear(); v.Precipitates.Clear(); v.Ledger.Clear();`);

console.log('补丁处数:', n);
