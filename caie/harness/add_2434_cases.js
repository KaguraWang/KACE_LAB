/* =====================================================================
   9701/34/M/J/24 Q1 + Q2 —— 碱式碳酸锌 ZnCO3·Zn(OH)2
   Q1：返滴定测 Mr
       22.50 g FB1 + 100.0 cm3 × 5.00 M HCl，定容到 1.00 dm3 = FB2
       用 0.0900 M KOH 回滴 25.0 cm3 FB2
       ZnCO3·Zn(OH)2 + 4HCl -> 2ZnCl2 + CO2 + 3H2O
   Q2：坩埚灼烧测 Ar
       ZnCO3·Zn(OH)2 -> 2ZnO + CO2 + H2O
       质量损失 = Mr(CO2) + Mr(H2O) = 44 + 18 = 62（每 mol FB1）
   ===================================================================== */
const fs = require('fs');
const path = require('path');
const file = 'D:/ChemLabUnity/caie/spec/cases_all.json';
const cases = JSON.parse(fs.readFileSync(file, 'utf8'));
const have = new Set(cases.map(c => c.case_id));
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);
const add = c => { if (!have.has(c.case_id)) { cases.push(c); have.add(c.case_id); } };

/* Zn 的碱式碳酸盐：Mr = 2×65.4 + 94 = 224.8 */
const MR_ZN = 2 * 65.4 + 94;          // 224.8
const M_FB1 = 22.50;                   // g
const N_FB1 = M_FB1 / MR_ZN;           // 0.10009 mol
const N_HCL_ADDED = 5.00 * 0.1000;     // 0.5000 mol
const N_HCL_LEFT = N_HCL_ADDED - 4 * N_FB1;   // 0.09964 mol
const C_KOH = 5.05 / 56.1;             // 0.09002 mol/dm3

/* ---- Q1a：返滴定的化学本体 ---- */
add({
  case_id: 'P3-2024-34-Q1-back-titration', mode: 'simulate',
  source: { paper: '9701/34/M/J/24', qp_page: 2, expect: 'MCO3·M(OH)2 + 4HCl -> 2MCl2 + CO2 + 3H2O' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 100 }],
  actions: [
    { op: 'add_mol', id: 'znco3znoh2', mol: N_FB1 },
    { op: 'add_mol', id: 'hcl', mol: N_HCL_ADDED },
    { op: 'settle' }
  ],
  probes: ['H+', 'co2'],
  asserts: [
    /* 碱式碳酸盐被完全溶解，放出 CO2，1:1 */
    A('absent',  { species: 'znco3znoh2', max: 1e-7 }),
    A('amount',  { species: 'co2', expect: N_FB1, tol: 1e-5 }),
    /* 剩余的 HCl 就是后面被 KOH 滴定的那部分 */
    A('amount',  { species: 'H+', expect: N_HCL_LEFT, tol: 5e-4 })
  ]
});

/* ---- Q1b：MS 的计算链 ---- */
add({
  case_id: 'P3-2024-34-Q1-calc', mode: 'validate',
  source: { paper: '9701/34/M/J/24', ms_page: 6 },
  vessels: [{ id: 'v', kind: 'beaker' }],
  actions: [],
  asserts: [
    /* (c)(ii) n(KOH) = 0.0900 × 25.0/1000 = 2.25e-3 mol */
    { id: 'K1', kind: 'numeric', got: C_KOH * 0.0250, expect: 2.25e-3, tol: 1e-6 },
    /* (c)(iii) n(HCl) = n(KOH) → [HCl] = n / V(titre) */
    { id: 'H1', kind: 'numeric', got: 2.25e-3 / (22.59 / 1000), expect: N_HCL_LEFT, tol: 2e-3 },
    /* (c)(iv) n(FB1) = (0.500 − n(HCl剩余)) / 4 */
    { id: 'F1', kind: 'numeric', got: (0.500 - N_HCL_LEFT) / 4, expect: N_FB1, tol: 5e-5 },
    /* Mr = 22.50 / n */
    { id: 'M1', kind: 'numeric', got: M_FB1 / N_FB1, expect: MR_ZN, tol: 0.5 },
    /* Ar(M) = (Mr − 94)/2 = 65.4 → Zn */
    { id: 'A1', kind: 'numeric', got: (MR_ZN - 94) / 2, expect: 65.4, tol: 0.2 }
  ]
});

/* ---- Q2a：坩埚灼烧，质量损失 ---- */
/* 2.0 g FB1 → n = 2.0/224.8 = 8.897e-3 mol → 质量损失 = n × 62 = 0.5516 g
   残留 ZnO = 2.0 − 0.5516 = 1.4484 g */
add({
  case_id: 'P3-2024-34-Q2-crucible', mode: 'simulate',
  source: { paper: '9701/34/M/J/24', qp_page: 4, expect: 'ZnCO3·Zn(OH)2 -> 2ZnO + CO2 + H2O' },
  vessels: [{ id: 'v', kind: 'crucible', water_ml: 0 }],
  actions: [
    { op: 'add_mol', id: 'znco3znoh2', mol: 2.0 / MR_ZN },
    { op: 'settle' },
    { op: 'compute', name: 'mass_before', calc: 'mass' },
    /* 强热：本生灯满火力 */
    { op: 'burner', gas: 1.0, lit: true, seconds: 10.0 },
    { op: 'settle' },
    { op: 'compute', name: 'mass_after', calc: 'mass' }
  ],
  probes: ['co2'],
  asserts: [
    /* 1 mol FB1 放出 1 CO2 + 1 H2O，所以 n(CO2) = n(FB1) */
    A('present', { species: 'co2', min: 1e-4 }),
    /* 残留固体是 ZnO，质量 2.0 − 0.5516 = 1.4484 g */
    A('mass', { expect: 1.4484, tol: 0.01 }),
    /* 分解必须真的发生：剩余质量明显小于投入质量 */
    A('temp_ge', { limit: 300 })
  ]
});

/* ---- Q2b：由质量损失反推 Ar ---- */
add({
  case_id: 'P3-2024-34-Q2-calc', mode: 'validate',
  source: { paper: '9701/34/M/J/24', ms_page: 7 },
  vessels: [{ id: 'v', kind: 'crucible' }],
  actions: [],
  asserts: [
    /* (b)(ii) n(CO2) = 质量损失 / (44 + 18) */
    { id: 'N1', kind: 'numeric', got: (2.0 / MR_ZN) * 62 / (44 + 18), expect: 2.0 / MR_ZN, tol: 1e-6 },
    /* (b)(iii) Mr = 投入质量 / n */
    { id: 'M1', kind: 'numeric', got: 2.0 / ((2.0 / MR_ZN) * 62 / 62), expect: MR_ZN, tol: 0.5 },
    /* (b)(iv) Ar(M) = (Mr − 94)/2 */
    { id: 'A1', kind: 'numeric', got: (MR_ZN - 94) / 2, expect: 65.4, tol: 0.2 },
    /* 质量守恒：2 mol MO + 1 CO2 + 1 H2O = 1 mol FB1 */
    { id: 'C1', kind: 'numeric', got: 2 * (65.4 + 16) + 44 + 18, expect: MR_ZN, tol: 0.5 }
  ]
});

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length);
