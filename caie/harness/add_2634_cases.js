/* =====================================================================
   9701/34/M/J/26 Q2 —— 水合盐 MX2·2H2O 的鉴定
   CI：FB 6 = 2.0 g BaCl2·2H2O
   M 是第 2 族阳离子，X 是第 17 族阴离子
   受热只失去结晶水：BaCl2·2H2O -> BaCl2 + 2H2O
   MS：n(H2O) = 失重/18；(b)(ii) Mr(MX2) = 残留质量 / (n(H2O)/2)
       (b)(iii) Ar(M) = Mr(MX2) − 2Ar(X)
   ===================================================================== */
const fs = require('fs');
const path = require('path');
const file = 'D:/ChemLabUnity/caie/spec/cases_all.json';
const cases = JSON.parse(fs.readFileSync(file, 'utf8'));
const have = new Set(cases.map(c => c.case_id));
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);
const add = c => { if (!have.has(c.case_id)) { cases.push(c); have.add(c.case_id); } };

const MR_HYDRATE = 208.3 + 36;          // BaCl2 + 2H2O = 244.3
const MASS_FB6 = 2.0;
const N_HYDRATE = MASS_FB6 / MR_HYDRATE;      // 8.187e-3
const MASS_LOSS = 2 * N_HYDRATE * 18;         // 0.2947
const MASS_RESIDUE = MASS_FB6 - MASS_LOSS;    // 1.7053
const MR_MX2 = MASS_RESIDUE / N_HYDRATE;      // 208.3
const AR_M = MR_MX2 - 2 * 35.45;              // 137.4 → Ba

/* ---- Q2(a)(i)：坩埚灼烧只失结晶水 ---- */
add({
  case_id: 'P3-2026-34-Q2-dehydrate', mode: 'simulate',
  source: { paper: '9701/34/M/J/26', qp_page: 4, ci: 'FB6 = 2.0 g BaCl2·2H2O' },
  vessels: [{ id: 'v', kind: 'crucible', water_ml: 0 }],
  actions: [
    { op: 'add_mol', id: 'bacl22h2o', mol: N_HYDRATE },
    { op: 'settle' },
    /* 先小火再强热 —— 与 QP 的方法一致 */
    { op: 'burner', gas: 0.35, lit: true, seconds: 4.0 },
    { op: 'burner', gas: 1.0,  lit: true, seconds: 8.0 },
    { op: 'settle' }
  ],
  probes: ['bacl2'],
  asserts: [
    A('absent',  { species: 'bacl22h2o', max: 1e-6 }),
    A('present', { species: 'bacl2', min: 5e-3 }),
    /* 残留质量 = 无水盐 1.7053 g（结晶水 0.2947 g 已逸出） */
    A('mass', { expect: MASS_RESIDUE, tol: 0.01 }),
    A('temp_ge', { limit: 62 })
  ]
});

/* ---- Q2(b)：MS 的计算链 ---- */
add({
  case_id: 'P3-2026-34-Q2-calc', mode: 'validate',
  source: { paper: '9701/34/M/J/26', ms_page: 8 },
  vessels: [{ id: 'v', kind: 'crucible' }],
  actions: [],
  asserts: [
    /* (b)(i) n(H2O) = 失重/18 = 2 × n(盐) */
    { id: 'N1', kind: 'numeric', got: MASS_LOSS / 18, expect: 2 * N_HYDRATE, tol: 1e-6 },
    /* (b)(ii) n(无水盐) = n(H2O)/2，Mr = 1.7053 / n */
    { id: 'M1', kind: 'numeric', got: MASS_RESIDUE / (MASS_LOSS / 18 / 2), expect: MR_MX2, tol: 0.3 },
    /* (b)(iii) Ar(M) = Mr(MX2) − 2Ar(Cl) = 137.4 → Ba */
    { id: 'A1', kind: 'numeric', got: MR_MX2 - 2 * 35.45, expect: AR_M, tol: 0.3 },
    /* 质量守恒：无水盐 + 结晶水 = 原样品 */
    { id: 'C1', kind: 'numeric', got: MASS_RESIDUE + MASS_LOSS, expect: MASS_FB6, tol: 1e-6 },
    /* 结晶水占样品质量的百分比（MS 常见的中间量） */
    { id: 'P1', kind: 'numeric', got: MASS_LOSS / MASS_FB6 * 100, expect: 14.74, tol: 0.05 }
  ]
});

/* ---- Q2(a)(ii)：残留物加硝酸银检验阴离子 X ---- */
add({
  case_id: 'P3-2026-34-Q2-anion-test', mode: 'simulate',
  source: { paper: '9701/34/M/J/26', qp_page: 4, expect: 'X = Cl，AgNO3 得白色沉淀' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    /* 用灼烧后的残留物 BaCl2 做阴离子检验 */
    { op: 'add_mol', id: 'bacl2', mol: 0.005 },
    { op: 'add_ml', id: 'hno3', ml: 4 },        /* 先酸化，排除碳酸根等干扰 */
    { op: 'add_g', id: 'agno3', g: 1.0 },
    { op: 'settle' }
  ],
  probes: ['agcl'],
  asserts: [
    A('present', { species: 'agcl', min: 1e-6 })
  ]
});
/* 对照：氯化银溶于氨水，所以能进一步确认是 Cl 而不是 Br/I */
add({
  case_id: 'P3-2026-34-Q2-anion-confirm', mode: 'simulate',
  source: { paper: '9701/34/M/J/26', qp_page: 4 },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    { op: 'add_mol', id: 'bacl2', mol: 0.005 },
    { op: 'add_g', id: 'agno3', g: 1.0 },
    /* 1 mol/dm3 的稀氨水：0.0059 mol AgCl 需要 2 倍的 NH3，量要给够 */
    { op: 'add_ml', id: 'nh3', ml: 25 },
    { op: 'settle' }
  ],
  asserts: [
    /* AgCl 在稀氨水里就能全溶 —— 这正是与 AgBr 的区别 */
    A('absent', { species: 'agcl', max: 1e-6 })
  ]
});

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length);
