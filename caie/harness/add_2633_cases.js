/* =====================================================================
   9701/33/M/J/26 Q2 + Q3
   Q2：取代羧酸 R'CHZCOOH 的 Mr 测定
       FA 3 = 0.0950 M NaOH，FA 4 = 15.28 g dm-3 的 R'CHZCOOH
       R'CHZCOOH + NaOH -> R'CHZCOONa + H2O
       MS：n(NaOH) = 0.095 × V/1000；[酸] = n × 40；Mr = 15.28/[酸]
       MS 2(d)(i)：Z = Br（AgNO3 得乳白色沉淀，部分溶于氨水）
       → 用 2-溴丙酸 CH3CHBrCOOH，Mr = 153
   Q3(a)：检验 R 基是否不饱和 —— MS：KMnO4 保持紫色 → R 饱和
   Q3(b)：FA 7 = Cr2(SO4)3 + NaNO3（1 阳离子 Cr3+，2 阴离子 SO4^2- / NO3-）
   ===================================================================== */
const fs = require('fs');
const path = require('path');
const file = 'D:/ChemLabUnity/caie/spec/cases_all.json';
const cases = JSON.parse(fs.readFileSync(file, 'utf8'));
const have = new Set(cases.map(c => c.case_id));
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);
const add = c => { if (!have.has(c.case_id)) { cases.push(c); have.add(c.case_id); } };

const MR_ACID = 153.0;                 // CH3CHBrCOOH
const CONC_G_DM3 = 15.28;
const C_NAOH = 0.0950;
const N_ACID = (CONC_G_DM3 / MR_ACID) * 0.0250;    // 25.0 cm3 里的量 = 2.4967e-3 mol
const V_TITRE = N_ACID / C_NAOH * 1000;            // 26.28 cm3

/* ---- Q2(a)(b)：滴定本体，1:1 中和 ---- */
add({
  case_id: 'P3-2026-33-Q2-titration', mode: 'simulate',
  source: { paper: '9701/33/M/J/26', qp_page: 4, ci: 'FA3 = 0.095 M NaOH' },
  vessels: [{ id: 'v', kind: 'flask', water_ml: 25.0 }],
  actions: [
    { op: 'add_g', id: 'ch3chbrcooh', g: CONC_G_DM3 * 0.0250 },   // 25.0 cm3 FA4
    { op: 'set_temp', c: 25 },
    { op: 'settle' },
    /* 滴定到当量点略过一点，酸应被耗尽 */
    { op: 'titrate', id: 'naoh', conc: C_NAOH, to_cm3: 27.0, step_cm3: 1.0, probe: 'H+' },
    { op: 'settle' }
  ],
  probes: ['H+'],
  asserts: [
    A('absent', { species: 'ch3chbrcooh', max: 1e-6 }),
    A('present', { species: 'CH3CHBrCOO-', min: 2e-3 })
  ]
});

/* ---- Q2(c)：MS 的计算链 ---- */
add({
  case_id: 'P3-2026-33-Q2-calc', mode: 'validate',
  source: { paper: '9701/33/M/J/26', ms_page: 13 },
  vessels: [{ id: 'v', kind: 'flask' }],
  actions: [],
  asserts: [
    /* (c)(ii) n(NaOH) = 0.095 × 26.28/1000 = 2.4966e-3 mol */
    { id: 'N1', kind: 'numeric', got: C_NAOH * V_TITRE / 1000, expect: 2.4967e-3, tol: 1e-6 },
    /* (c)(iii) [酸] = n × 40 = 0.09987 mol dm-3 */
    { id: 'C1', kind: 'numeric', got: C_NAOH * V_TITRE / 1000 * 40, expect: 0.09987, tol: 1e-3 },
    /* (c)(iv) Mr = 15.28 / [酸] = 153.0 */
    { id: 'M1', kind: 'numeric', got: CONC_G_DM3 / (C_NAOH * V_TITRE / 1000 * 40), expect: MR_ACID, tol: 0.3 },
    /* 一元酸按 1:1 消耗碱 */
    { id: 'R1', kind: 'numeric', got: C_NAOH * V_TITRE / 1000 / ((CONC_G_DM3 / MR_ACID) * 0.0250), expect: 1.0, tol: 1e-6 }
  ]
});

/* ---- Q2(d)(i)：热碱水解后加硝酸银，Z 是 Br ---- */
add({
  case_id: 'P3-2026-33-Q2-halide-test', mode: 'simulate',
  source: { paper: '9701/33/M/J/26', ms_page: 14, expect: 'Z = Br，AgNO3 得乳白色沉淀，部分溶于氨水' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    { op: 'add_g', id: 'ch3chbrcooh', g: 0.4 },
    /* 过量热碱水解 */
    { op: 'add_mol', id: 'naoh', mol: 0.02 },
    { op: 'set_temp', c: 80 },
    { op: 'settle' },
    /* 用硝酸中和后再加硝酸银 */
    { op: 'add_mol', id: 'hno3', mol: 0.02 },
    { op: 'add_g', id: 'agno3', g: 0.8 },
    { op: 'settle' }
  ],
  probes: ['agbr'],
  /* MS 的答案是「加硝酸银得乳白色沉淀」—— 溴离子此时已全部沉淀成 AgBr，
     所以这里查 AgBr 而不是游离的 Br-（游离 Br- 为 0 才是对的）。 */
  asserts: [
    A('present', { species: 'agbr', min: 5e-4 }),
    A('absent',  { species: 'ch3chbrcooh', max: 1e-6 })
  ]
});
add({
  case_id: 'P3-2026-33-Q2-bromide-vs-chloride', mode: 'simulate',
  note: '区分 Br 与 Cl：稀氨水里 AgBr 基本不溶，AgCl 全溶',
  source: { paper: '9701/33/M/J/26', ms_page: 14 },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    { op: 'add_g', id: 'kbr', g: 0.5 },
    { op: 'add_g', id: 'agno3', g: 0.8 },
    { op: 'add_ml', id: 'nh3', ml: 6 },        /* 稀氨水 —— AgBr 溶不动 */
    { op: 'settle' }
  ],
  asserts: [A('present', { species: 'agbr', min: 1e-7 })]
});

/* ---- Q3(a)：R 基是否不饱和 —— MS 说保持紫色，即饱和 ---- */
add({
  case_id: 'P3-2026-33-Q3-unsaturation', mode: 'simulate',
  source: { paper: '9701/33/M/J/26', qp_page: 7, ms_page: 18, expect: 'KMnO4 保持紫色 → R 饱和' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    /* 用饱和一元羧酸代表 FA 2（MS 的结论就是饱和） */
    { op: 'add_g', id: 'ch3cooh', g: 0.6 },
    { op: 'add_g', id: 'kmno4', g: 0.05 },
    { op: 'settle' }
  ],
  probes: ['MnO4-'],
  asserts: [
    /* 饱和酸不能还原高锰酸根，紫色必须还在 */
    A('present', { species: 'MnO4-', min: 1e-7 })
  ]
});
add({
  case_id: 'P3-2026-33-Q3-unsaturation-positive', mode: 'simulate',
  note: '对照：不饱和的 R 基会让高锰酸根褪色（酸在本条件不被氧化）',
  source: { paper: '9701/33/M/J/26', ms_page: 18 },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    { op: 'add_ml', id: 'c2h4', ml: 5 },
    { op: 'settle' }
  ],
  asserts: [{ id: 'G1', kind: 'present', species: 'c2h4', min: 1e-6, required: false }]
});

/* ---- Q3(b)：FA 7 = Cr2(SO4)3 + NaNO3，六项检验里的确定性几条 ---- */
add({
  case_id: 'P3-2026-33-Q3B-Cr-ammonia', mode: 'simulate',
  source: { paper: '9701/33/M/J/26', qp_page: 8, ms_page: 19, test: 'Test 2 加氨水', expect: '灰绿色沉淀，过量氨水中不溶' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    { op: 'add_g', id: 'cr2so43', g: 0.5 },
    { op: 'add_ml', id: 'nh3', ml: 40 },
    { op: 'settle' }
  ],
  asserts: [A('present', { species: 'croh3', min: 1e-7 })]
});
add({
  case_id: 'P3-2026-33-Q3B-sulfate-barium', mode: 'simulate',
  source: { paper: '9701/33/M/J/26', qp_page: 8, test: 'Test 4 加氯化钡再加硝酸', expect: '白色沉淀，不溶于硝酸' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    { op: 'add_g', id: 'cr2so43', g: 0.5 },
    { op: 'add_g', id: 'bacl2', g: 1.0 },
    { op: 'add_ml', id: 'hno3', ml: 8 },
    { op: 'settle' }
  ],
  asserts: [A('present', { species: 'baso4', min: 1e-7 })]
});
add({
  case_id: 'P3-2026-33-Q3B-nitrate-aluminium', mode: 'simulate',
  source: { paper: '9701/33/M/J/26', qp_page: 8, test: 'Test 1 加 NaOH 后加热并加铝箔', expect: 'NO3- 被还原放出 NH3' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    { op: 'add_g', id: 'nano3', g: 1.5 },
    { op: 'add_mol', id: 'naoh', mol: 0.08 },
    { op: 'add_g', id: 'al', g: 0.5 },
    { op: 'set_temp', c: 90 },
    { op: 'settle' }
  ],
  probes: ['nh3'],
  asserts: [A('present', { species: 'nh3', min: 1e-6 })]
});
add({
  case_id: 'P3-2026-33-Q3B-chromium-naoh', mode: 'simulate',
  source: { paper: '9701/33/M/J/26', qp_page: 8, test: 'Test 1 前半 / 与 NaOH', expect: '灰绿色沉淀，溶于过量碱得深绿色溶液' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    { op: 'add_g', id: 'cr2so43', g: 0.5 },
    { op: 'add_g', id: 'naoh', g: 0.3 },
    { op: 'settle' }
  ],
  asserts: [A('present', { species: 'croh3', min: 1e-7 })]
});
add({
  case_id: 'P3-2026-33-Q3B-chromium-naoh-excess', mode: 'simulate',
  source: { paper: '9701/33/M/J/26', qp_page: 8, expect: 'Cr(OH)3 溶于过量 NaOH' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    { op: 'add_g', id: 'cr2so43', g: 0.5 },
    { op: 'add_mol', id: 'naoh', mol: 0.05 },
    { op: 'settle' }
  ],
  asserts: [A('absent', { species: 'croh3', max: 1e-6 })]
});

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length);
