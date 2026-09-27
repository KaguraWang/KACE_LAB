/* =====================================================================
   追加真题用例（Gate 3 扩展）
   全部依据：QP 的方法描述 + CI 的真实配制 + MS 的比值链
   ===================================================================== */
const fs = require('fs');
const path = require('path');
const OUT = 'D:/ChemLabUnity/caie/spec';
const file = path.join(OUT, 'cases_all.json');
const cases = JSON.parse(fs.readFileSync(file, 'utf8'));
const have = new Set(cases.map(c => c.case_id));
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);
const add = c => { if (!have.has(c.case_id)) { cases.push(c); have.add(c.case_id); } };

/* =====================================================================
   9701/33/M/J/24 Q1 —— 返滴定测 NH4Cl
   CI: FA1 = 0.0950 M NaOH（由 25.0 cm3 × 2.00 M NaOH 与 NH4Cl 反应后稀释到 250 cm3 得到）
       FA2 = 0.0500 M H2SO4
   MS 比值链：
     (ii)  n(H2SO4) = 0.052 × V/1000      （本卷 CI 实际是 0.0500）
     (iii) n(NaOH 剩于 25 cm3) = (ii) × 2
     (iv)  n(NaOH 初始) = 2.00 × 25.0/1000 = 0.05(00)；与 NH4Cl 反应 = 0.05 − (iii) × 10
     (v)   [NH4Cl] = (iv) × 1000/10
   ===================================================================== */

/* A：验证 NH4+ 被过量碱加热赶出——这是返滴定的前一半 */
add({
  case_id: 'P3-2024-33-Q1-reaction', mode: 'simulate',
  source: { paper: '9701/33/M/J/24', qp_page: 2, ms_page: 7 },
  vessels: [{ id: 'v', kind: 'flask', water_ml: 25.0 }],
  actions: [
    { op: 'add_mol', id: 'naoh', mol: 0.0500 },     /* 25.0 cm3 × 2.00 M */
    { op: 'add_mol', id: 'nh4cl', mol: 0.02625 },   /* 由 CI 的 0.0950 M 反推 */
    { op: 'heat', mode: 'reflux' },
    { op: 'set_temp', c: 80 },
    { op: 'settle' }
  ],
  probes: ['OH-', 'NH3'],
  asserts: [
    A('amount', { species: 'OH-', expect: 0.02375, tol: 1e-4 }),
    A('absent', { species: 'NH4+', max: 1e-5 }),
    A('present', { species: 'nh3', min: 1e-3 })
  ]
});

/* B：用 0.0500 M H2SO4 滴定 25.0 cm3 的 0.0950 M NaOH，当量点应在 23.75 cm3 */
add({
  case_id: 'P3-2024-33-Q1-titration', mode: 'simulate',
  source: { paper: '9701/33/M/J/24', qp_page: 2, ms_page: 7 },
  vessels: [{ id: 'v', kind: 'flask', water_ml: 25.0 }],
  actions: [
    { op: 'add_mol', id: 'naoh', mol: 0.0950 * 0.0250 },   /* 25.0 cm3 FA1 */
    { op: 'set_temp', c: 25 },
    { op: 'titrate', id: 'h2so4', conc: 0.0500, to_cm3: 23.0, step_cm3: 1.0, probe: 'OH-' },
    { op: 'settle' }
  ],
  probes: ['OH-'],
  asserts: [
    /* 当量点 23.75 cm3：23 cm3 时碱尚未耗完 */
    A('present', { species: 'OH-', min: 1e-5 })
  ]
});
/* D：24.0 cm3 已越过当量点，碱应被耗完 —— 两点夹逼出当量点位置 */
add({
  case_id: 'P3-2024-33-Q1-past-endpoint', mode: 'simulate',
  source: { paper: '9701/33/M/J/24', qp_page: 2, ms_page: 7 },
  vessels: [{ id: 'v', kind: 'flask', water_ml: 25.0 }],
  actions: [
    { op: 'add_mol', id: 'naoh', mol: 0.0950 * 0.0250 },
    { op: 'set_temp', c: 25 },
    { op: 'titrate', id: 'h2so4', conc: 0.0500, to_cm3: 24.0, step_cm3: 1.0, probe: 'OH-' },
    { op: 'settle' }
  ],
  probes: ['OH-'],
  asserts: [
    A('absent', { species: 'OH-', max: 1e-6 })
  ]
});

/* C：MS 的完整计算链 */
add({
  case_id: 'P3-2024-33-Q1-calc', mode: 'validate',
  source: { paper: '9701/33/M/J/24', ms_page: 7 },
  vessels: [{ id: 'v', kind: 'beaker' }],
  actions: [
    { op: 'compute', name: 'titre',        calc: 'mean', values: [23.75, 23.75] },
    { op: 'compute', name: 'nH2SO4',       calc: 'n', c: 0.0500, v_cm3: 23.75 },
    { op: 'compute', name: 'nNaOH_25',     calc: 'scale', from_var: 'nH2SO4', factor: 2.0 },
    { op: 'compute', name: 'nNaOH_left',   calc: 'scale', from_var: 'nNaOH_25', factor: 10.0 },
    { op: 'compute', name: 'nNaOH_init',   calc: 'n', c: 2.00, v_cm3: 25.0 },
    { op: 'compute', name: 'nHCl_needed',  calc: 'distill', a: 0, b: 0 }
  ],
  asserts: [
    A('var', { name: 'nH2SO4',     expect: 1.1875e-3, tol: 1e-12 }),
    A('var', { name: 'nNaOH_25',   expect: 2.375e-3,  tol: 1e-12 }),
    A('var', { name: 'nNaOH_left', expect: 0.02375,   tol: 1e-9 }),
    A('var', { name: 'nNaOH_init', expect: 0.0500,    tol: 1e-9 }),
    /* (iv) 与 NH4Cl 反应的 NaOH = 0.0500 − 0.02375 = 0.02625 mol */
    { id: 'M2', kind: 'numeric', got: 0.0500 - 0.02375, expect: 0.02625, tol: 1e-9 },
    /* (v) [NH4Cl] = 0.02625 × 1000/10 = 2.625 mol dm-3 */
    { id: 'A1', kind: 'numeric', got: 0.02625 * 1000 / 10, expect: 2.625, tol: 1e-6 }
  ]
});

/* =====================================================================
   9701/33/M/J/26 Q1 —— 镁与酸反应测氢气体积
   QP：FA1 = 镁，FA2 = 羧酸（RCH2COOH）
   CI：FA1 = 0.17 g 镁；FA2 = 0.25 mol dm-3 硫酸（QP 的「羧酸」是掩护描述）
   Mg + 2H+ -> Mg2+ + H2 ；50.0 cm3 酸，酸过量
   ===================================================================== */
add({
  case_id: 'P3-2026-33-Q1', mode: 'simulate',
  source: { paper: '9701/33/M/J/26', qp_page: 2, ci_page: 4 },
  vessels: [{ id: 'v', kind: 'flask', water_ml: 50.0 }],
  actions: [
    { op: 'add_mol', id: 'h2so4', mol: 0.25 * 0.0500 },       /* 50.0 cm3 FA2 */
    { op: 'add_g',  id: 'mg', g: 0.17 },                      /* 全部 FA1 */
    { op: 'set_temp', c: 25 },
    { op: 'settle' },
    { op: 'compute', name: 'nMg', calc: 'n', c: 0, v_cm3: 0 },
    { op: 'compute', name: 'vH2_cm3', calc: 'scale', from_var: 'nH2', factor: 24000.0 }
  ],
  probes: ['h2', 'Mg2+'],
  asserts: [
    A('present', { species: 'h2',   min: 1e-4 }),
    A('absent',  { species: 'mg',   max: 1e-7 }),
    /* n(Mg) = 0.17/24.3 = 7.00e-3 mol；1:1 生成 H2 */
    A('amount',  { species: 'h2',   expect: 7.00e-3, tol: 3e-4 }),
    A('amount',  { species: 'Mg2+', expect: 7.00e-3, tol: 3e-4 }),
    /* 摩尔体积 24 dm3/mol：7.00e-3 × 24000 = 168 cm3（MS 量级） */
    { id: 'GAS1', kind: 'numeric', got: 0.17 / 24.3 * 24000, expect: 168.0, tol: 4.0 }
  ]
});

/* =====================================================================
   9701/34/M/J/26 Q1 —— 碘量法测硫代硫酸钠
   CI：FB1 = 0.100 M Na2S2O3（待测）；FB2 = 0.0140 M KIO3；FB3 = 1.0 M H2SO4；FB4 = 0.50 M KI
   25.0 cm3 FB2 + 10.0 cm3 FB3 + 10.0 cm3 FB4，用 FB1 滴定
   IO3- + 5I- + 6H+ -> 3I2 + 3H2O
   I2 + 2S2O3^2- -> 2I- + S4O6^2-
   ===================================================================== */
add({
  case_id: 'P3-2026-34-Q1-iodine', mode: 'simulate',
  source: { paper: '9701/34/M/J/26', qp_page: 2, ci_page: 4 },
  vessels: [{ id: 'v', kind: 'flask', water_ml: 45.0 }],
  actions: [
    { op: 'add_mol', id: 'kio3',  mol: 0.0140 * 0.0250 },   /* 25.0 cm3 FB2 */
    { op: 'add_mol', id: 'h2so4', mol: 1.0 * 0.010 },       /* 10.0 cm3 FB3 */
    { op: 'add_mol', id: 'ki',    mol: 0.50 * 0.010 },      /* 10.0 cm3 FB4，过量 */
    { op: 'set_temp', c: 25 },
    { op: 'settle' }
  ],
  probes: ['i2', 'io3-'],
  asserts: [
    /* n(IO3-) = 3.50e-4 → n(I2) = 3 × = 1.05e-3 mol */
    A('absent',  { species: 'io3-', max: 1e-7 }),
    A('amount',  { species: 'i2',   expect: 1.05e-3, tol: 1e-5 })
  ]
});
add({
  case_id: 'P3-2026-34-Q1-titration', mode: 'simulate',
  source: { paper: '9701/34/M/J/26', qp_page: 2, ci_page: 4 },
  vessels: [{ id: 'v', kind: 'flask', water_ml: 45.0 }],
  actions: [
    { op: 'add_mol', id: 'kio3',  mol: 0.0140 * 0.0250 },
    { op: 'add_mol', id: 'h2so4', mol: 1.0 * 0.010 },
    { op: 'add_mol', id: 'ki',    mol: 0.50 * 0.010 },
    { op: 'set_temp', c: 25 },
    { op: 'settle' },
    /* 用 0.100 M 硫代硫酸钠滴定，当量点应在 21.00 cm3 */
    { op: 'titrate', id: 'na2s2o3', conc: 0.100, to_cm3: 22.0, step_cm3: 1.0, probe: 'i2' },
    { op: 'settle' }
  ],
  probes: ['i2'],
  asserts: [
    A('absent', { species: 'i2', max: 1e-6 }),
    /* MS 链：n(S2O3^2-) = 2 × n(I2) = 2.10e-3 mol → V = 21.00 cm3 */
    { id: 'C1', kind: 'numeric', got: 2 * 1.05e-3 / 0.100 * 1000, expect: 21.00, tol: 0.01 } 
  ]
});

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length);
const g = {};
for (const c of cases) { const p = c.case_id.split('-')[0]; g[p] = (g[p] || 0) + 1; }
console.log(g);
