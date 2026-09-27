/* =====================================================================
   生成全部测试用例
   来源：
     Gate 2  化学核心单元测试（规范 §4）
     R01-R05 强制回归案例（规范 §9）
     P3      真题逐题（规范 §4 Gate 3）—— 用 CI 的 ground truth
     ADV     对抗与负向测试（规范 §4 Gate 4）
   ===================================================================== */
const fs = require('fs');
const path = require('path');

const OUT = 'D:/ChemLabUnity/caie/spec';
const cases = [];
let seq = 0;
function C(id, o) { cases.push(Object.assign({ case_id: id, mode: 'simulate' }, o)); }

/* 快捷：单容器 + 一串动作 */
function sim(id, vessel, actions, asserts, extra = {}) {
  C(id, Object.assign({
    vessels: [Object.assign({ id: 'v', kind: 'flask' }, vessel)],
    actions, asserts
  }, extra));
}
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);

/* =====================================================================
   Gate 2 —— 化学核心单元测试
   ===================================================================== */

/* --- 2.1 计量：摩尔、浓度、气体体积、摩尔质量 --- */
C('G2-01-molar-mass', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [],
  asserts: [
    A('molar_mass', { formula: 'CuSO4·5H2O', expect: 249.7, tol: 1.0 }),
    A('molar_mass', { formula: 'K2Cr2O7',     expect: 294.2, tol: 1.0 }),
    A('molar_mass', { formula: 'Ca(OH)2',     expect: 74.1,  tol: 1.0 }),
    A('molar_mass', { formula: '(NH4)2SO4',   expect: 132.1, tol: 1.0 }),
    A('molar_mass', { formula: 'FeSO4·7H2O',  expect: 278.0, tol: 1.5 }),
    A('molar_mass', { formula: 'Na2CO3',      expect: 106.0, tol: 1.0 }),
    A('molar_mass', { formula: 'C6H12O6',     expect: 180.2, tol: 1.0 }),
    A('molar_mass', { formula: 'Fe2(SO4)3',   expect: 399.9, tol: 1.0 }),
  ]
});

/* --- 2.2 配平：质量/电荷/电子守恒 --- */
C('G2-02-balance-ok', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [],
  asserts: [
    A('equation_balanced', { equation: 'Zn + 2H+ -> Zn2+ + H2' }),
    A('equation_balanced', { equation: 'MnO4- + 8H+ + 5Fe2+ -> Mn2+ + 5Fe3+ + 4H2O' }),
    A('equation_balanced', { equation: '5H2O2 + 2MnO4- + 6H+ -> 5O2 + 8H2O + 2Mn2+' }),
    A('equation_balanced', { equation: 'Ba2+ + SO4^2- -> BaSO4' }),
    A('equation_balanced', { equation: 'Ag+ + Cl- -> AgCl' }),
    A('equation_balanced', { equation: 'CaCO3 + 2H+ -> Ca2+ + H2O + CO2' }),
    A('equation_balanced', { equation: '2Fe2+ + Cl2 -> 2Fe3+ + 2Cl-' }),
    A('equation_balanced', { equation: 'Cr2O7^2- + 14H+ + 6Fe2+ -> 2Cr3+ + 6Fe3+ + 7H2O' }),
    A('equation_balanced', { equation: 'Fe2+ -> Fe3+ + e-' }),
    A('equation_balanced', { equation: 'MnO4- + 8H+ + 5e- -> Mn2+ + 4H2O' }),
    A('equation_balanced', { equation: '2H2 + O2 -> 2H2O' }),
    A('equation_balanced', { equation: 'NH4+ + OH- -> NH3 + H2O' }),
  ]
});
C('G2-03-balance-BAD', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [],
  asserts: [
    /* 这些全都不守恒，必须判失败（负向：断言本身要求它失败 => 用 absent 语义）
       这里改用「断言不通过」的表达：expect_balanced:false */
    { id: 'BAD1', kind: 'equation_unbalanced', equation: 'Zn + H+ -> Zn2+ + H2' },
    { id: 'BAD2', kind: 'equation_unbalanced', equation: 'MnO4- + 8H+ + Fe2+ -> Mn2+ + Fe3+ + 4H2O' },
    { id: 'BAD3', kind: 'equation_unbalanced', equation: 'Fe2+ -> Fe3+' },
    { id: 'BAD4', kind: 'equation_unbalanced', equation: 'Ba2+ + SO4^2- -> BaSO4-' },
    { id: 'BAD5', kind: 'equation_unbalanced', equation: '2H2 + O2 -> H2O' },
    { id: 'BAD6', kind: 'equation_unbalanced', equation: 'Cu + 2Ag+ -> Cu2+ + Ag' },
  ]
});

/* --- 2.3 质子转移 / 沉淀 / 气体 / 氧化还原 --- */
sim('G2-04-acid-base', { water_ml: 20 },
  [{ op: 'add_ml', id: 'naoh', ml: 5 }, { op: 'add_ml', id: 'hcl', ml: 5 }, { op: 'settle' }],
  [A('present', { species: 'Cl-' }), A('amount', { species: 'OH-', expect: 0, tol: 1e-6 })]);
sim('G2-05-precipitate', { water_ml: 20 },
  [{ op: 'add_g', id: 'agno3', g: 1.0 }, { op: 'add_g', id: 'nacl', g: 0.5 }, { op: 'settle' }],
  [A('present', { species: 'agcl' }), A('colour', { contains: '白' })]);
sim('G2-06-gas-evolution', { water_ml: 20 },
  [{ op: 'add_g', id: 'caco3', g: 2.0 }, { op: 'add_ml', id: 'hcl', ml: 10 }, { op: 'settle' }],
  [A('present', { species: 'co2', min: 1e-4 }), A('present', { species: 'Ca2+' })]);
sim('G2-07-redox-displacement', { water_ml: 20 },
  [{ op: 'add_g', id: 'cuso4', g: 2.0 }, { op: 'add_g', id: 'fe', g: 1.0 }, { op: 'settle' }],
  [A('present', { species: 'Fe2+' }), A('absent', { species: 'Cu2+', max: 1e-3 })]);

/* --- 2.4 限量试剂 / 过量试剂 --- */
sim('G2-08-limiting', { water_ml: 20 },
  [{ op: 'add_mol', id: 'agno3', mol: 0.001 }, { op: 'add_mol', id: 'nacl', mol: 0.005 }, { op: 'settle' }],
  [A('amount', { species: 'Ag+', expect: 0, tol: 1e-7 }),
   A('present', { species: 'Cl-', min: 0.003 })]);
sim('G2-09-excess', { water_ml: 20 },
  [{ op: 'add_mol', id: 'agno3', mol: 0.005 }, { op: 'add_mol', id: 'nacl', mol: 0.001 }, { op: 'settle' }],
  [A('amount', { species: 'Cl-', expect: 0, tol: 1e-7 }),
   A('present', { species: 'Ag+', min: 0.003 })]);

/* --- 2.5 观察：沉淀颜色（CIE 定性分析） --- */
const qa = [
  ['G2-10-cu-oh2',  'cuso4', 'naoh', 'cuoh2', '蓝'],
  ['G2-11-fe2-oh2', 'feso4', 'naoh', 'feoh2', '绿'],
  ['G2-12-fe3-oh3', 'fecl3', 'naoh', 'feoh3', '红'],
  ['G2-13-agcl',    'nacl',  'agno3', 'agcl', '白'],
  ['G2-14-agbr',    'nabr',  'agno3', 'agbr', '淡黄'],
  ['G2-15-agi',     'nai',   'agno3', 'agi',  '黄'],
  ['G2-16-baso4',   'na2so4','bacl2', 'baso4','白'],
  ['G2-17-pbi2',    'ki',    'pbno32','pbi2', '金'],
];
for (const [id, x, y, prec, colr] of qa) {
  sim(id, { water_ml: 20 },
    [{ op: 'add_g', id: x, g: 1.0 }, { op: 'add_g', id: y, g: 1.0 }, { op: 'settle' }],
    [A('present', { species: prec, min: 1e-6 })],
    { note: `期望沉淀颜色 ${colr}` });
}

/* --- 2.6 两性与过量溶解 --- */
sim('G2-18-amphoteric-al', { water_ml: 20 },
  [{ op: 'add_mol', id: 'alcl3', mol: 0.001 }, { op: 'add_mol', id: 'naoh', mol: 0.003 }, { op: 'settle' }],
  [A('present', { species: 'aloh3', min: 1e-7 })]);
sim('G2-19-amphoteric-al-excess', { water_ml: 20 },
  [{ op: 'add_mol', id: 'alcl3', mol: 0.001 }, { op: 'add_mol', id: 'naoh', mol: 0.02 }, { op: 'settle' }],
  [A('absent', { species: 'aloh3', max: 1e-7 }), A('present', { species: 'AlO2-', min: 1e-7 })]);

/* --- 2.7 热分解 --- */
sim('G2-20-thermal-decomp', { water_ml: 0 },
  [{ op: 'add_g', id: 'caco3', g: 2.0 }, { op: 'set_temp', c: 900 }, { op: 'settle' }],
  [{ id: 'T1', kind: 'present', species: 'cao', min: 1e-9, required: false },
   { id: 'T2', kind: 'present', species: 'co2', min: 1e-9, required: false }]);

/* --- 2.8 可逆平衡 / 弱电解质 --- */
sim('G2-21-weak-acid', { water_ml: 20 },
  [{ op: 'add_ml', id: 'ch3cooh', ml: 5 }, { op: 'settle' }],
  [{ id: 'W1', kind: 'present', species: 'H+', min: 1e-9 }]);

/* --- 2.9 有机官能团变换 --- */
sim('G2-22-esterification', { water_ml: 0 },
  [{ op: 'add_ml', id: 'ch3cooh_glacial', ml: 5 }, { op: 'add_ml', id: 'c2h5oh', ml: 6 },
   { op: 'add_ml', id: 'h2so4_conc', ml: 1 }, { op: 'set_temp', c: 80 }, { op: 'settle' }],
  [A('present', { species: 'ch3cooc2h5', min: 1e-6 })]);
sim('G2-23-aldehyde-oxidation', { water_ml: 20 },
  [{ op: 'add_ml', id: 'ch3cho', ml: 2 }, { op: 'add_g', id: 'k2cr2o7', g: 1.0 },
   { op: 'add_ml', id: 'h2so4', ml: 3 }, { op: 'heat', mode: 'reflux' }, { op: 'settle' }],
  [A('present', { species: 'ch3cooh', min: 1e-6 })]);
sim('G2-24-tollens', { water_ml: 20 },
  [{ op: 'add_g', id: 'agno3', g: 0.8 }, { op: 'add_ml', id: 'nh3', ml: 40 },
   { op: 'add_ml', id: 'ch3cho', ml: 2 }, { op: 'set_temp', c: 50 }, { op: 'settle' }],
  [A('fx_mirror', {})]);

/* =====================================================================
   R01 —— 酸性高锰酸根滴定 Fe2+
   ===================================================================== */
sim('R01-01-stoich', { water_ml: 70 },
  [{ op: 'add_mol', id: 'feso4', mol: 1.25e-3 }, { op: 'add_mol', id: 'h2so4', mol: 0.010 },
   { op: 'add_mol', id: 'kmno4', mol: 2.50e-4 }, { op: 'settle' }],
  [A('amount', { species: 'Fe2+',  expect: 0,       tol: 1e-7 }),
   A('amount', { species: 'MnO4-', expect: 0,       tol: 1e-8 }),
   A('amount', { species: 'Fe3+',  expect: 1.25e-3, tol: 1e-6 }),
   A('amount', { species: 'Mn2+',  expect: 2.50e-4, tol: 1e-6 })]);
C('R01-02-ratio', {
  mode: 'simulate', vessels: [{ id: 'v', kind: 'flask', water_ml: 70 }],
  actions: [
    { op: 'add_mol', id: 'feso4', mol: 1.25e-3 },
    { op: 'add_mol', id: 'h2so4', mol: 0.010 },
    { op: 'add_mol', id: 'kmno4', mol: 2.0e-4 },        // 亚当量，留 Fe2+ 便于算比值
    { op: 'settle' },
    { op: 'compute', name: 'ratio', calc: 'species_delta_ratio', a: 'Fe2+', a0: 1.25e-3, b: 2.0e-4 }
  ],
  asserts: [
    A('equation_balanced', { equation: 'MnO4- + 8H+ + 5Fe2+ -> Mn2+ + 5Fe3+ + 4H2O' }),
    A('var', { name: 'ratio', expect: 5.0, tol: 1e-6 })
  ]
});
sim('R01-03-pre-endpoint', { water_ml: 70 },
  [{ op: 'add_mol', id: 'feso4', mol: 1.25e-3 }, { op: 'add_mol', id: 'h2so4', mol: 0.010 },
   { op: 'add_mol', id: 'kmno4', mol: 2.40e-4 }, { op: 'settle' }],
  [A('amount', { species: 'Fe2+', expect: 5.0e-5, tol: 1e-7 }),
   A('absent', { species: 'MnO4-', max: 1e-9 })]);
sim('R01-04-post-endpoint', { water_ml: 70 },
  [{ op: 'add_mol', id: 'feso4', mol: 1.25e-3 }, { op: 'add_mol', id: 'h2so4', mol: 0.010 },
   { op: 'add_mol', id: 'kmno4', mol: 2.60e-4 }, { op: 'settle' }],
  [A('absent', { species: 'Fe2+', max: 1e-7 }),
   A('amount', { species: 'MnO4-', expect: 1.0e-5, tol: 1e-8 })]);
sim('R01-05-hcl-interference', { water_ml: 70 },
  [{ op: 'add_mol', id: 'feso4', mol: 1.25e-3 }, { op: 'add_mol', id: 'hcl_conc', mol: 0.020 },
   /* 必须用过量高锰酸根：与 Fe2+ 等量时它会全被吃掉，轮不到氧化 Cl- */
   { op: 'add_mol', id: 'kmno4', mol: 5.00e-4 }, { op: 'settle' }],
  [A('present', { species: 'cl2', min: 1e-9 }),
   A('amount', { species: 'Cl-', expect: 0.0, tol: 0.0200 - 1e-6, required: false })],
  { note: '错用 HCl：过量高锰酸根氧化 Cl- 生成氯气，滴定剂被额外消耗' });
sim('R01-06-no-acid', { water_ml: 70 },
  [A('absent', { species: 'Mn2+', max: 1e-6 }),
   A('present', { species: 'mno2', min: 1e-7 })]);

/* =====================================================================
   R02 —— 滴定数据与平均值
   ===================================================================== */
C('R02-01-good-data', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [
    { op: 'compute', name: 'mean', calc: 'batch_eval', readings: [
      { label: 'rough', initial: 0.00, final: 25.60 },
      { label: 'acc1',  initial: 0.15, final: 25.15 },
      { label: 'acc2',  initial: 0.40, final: 25.40 }] }
  ],
  asserts: [
    A('var', { name: 'mean',        expect: 25.00, tol: 0.005 }),
    A('var', { name: 'mean_2dp',    expect: 25.00, tol: 1e-9 }),
    A('var', { name: 'sel_count',   expect: 2.0,   tol: 1e-9 }),
    A('var', { name: 'all_spread',  expect: 0.00,  tol: 0.005 }),   // 只统计准确滴定，不含 rough
    A('var_le', { name: 'sel_spread', limit: 0.20 }),
  ]
});
C('R02-02-bad-spread', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [
    { op: 'compute', name: 'mean', calc: 'batch_eval', readings: [
      { label: 'acc1', initial: 0.10, final: 24.90 },
      { label: 'acc2', initial: 0.30, final: 25.60 }] }
  ],
  asserts: [
    /* 两组相差 0.40 > 0.20，系统不得声称它们一致 */
    A('var_ge', { name: 'sel_spread', limit: 0.35 }),
    A('var', { name: 'concordant', expect: 0.0, tol: 1e-9 }),
  ]
});
C('R02-03-three-concordant', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [
    { op: 'compute', name: 'mean', calc: 'batch_eval', readings: [
      { label: 'rough', initial: 0.00, final: 25.50 },
      { label: 'acc1',  initial: 0.05, final: 25.10 },
      { label: 'acc2',  initial: 0.20, final: 25.20 },
      { label: 'acc3',  initial: 0.35, final: 25.40 }] }
  ],
  asserts: [
    /* MS 层级：先找完全相同的两组（25.05 出现两次），所以只取那两组 */
    A('var', { name: 'sel_count', expect: 2.0, tol: 1e-9 }),
    A('var', { name: 'mean',      expect: 25.05, tol: 0.005 }),
    A('var_le', { name: 'all_spread', limit: 0.20 }),
  ]
});

/* =====================================================================
   R03 —— 定性分析状态机
   ===================================================================== */
const r03 = [
  ['R03-01-cu2-oh',   'cuso4',  'naoh',  'cuoh2',  false, '浅蓝沉淀；不溶于过量 NaOH（只溶于过量氨水）'],
  ['R03-02-zn2-oh',   'znso4',  'naoh',  'znoh2',  true,  '白色沉淀，过量 NaOH 溶解'],
  ['R03-03-al3-oh',   'alcl3',  'naoh',  'aloh3',  true,  '白色胶状，过量 NaOH 溶解'],
  ['R03-04-fe2-oh',   'feso4',  'naoh',  'feoh2',  false, '绿色沉淀，过量不溶'],
  ['R03-05-fe3-oh',   'fecl3',  'naoh',  'feoh3',  false, '红棕色沉淀，过量不溶'],
];
for (const [id, salt, base, prec, soluble, note] of r03) {
  sim(id, { water_ml: 20 },
    [{ op: 'add_g', id: salt, g: 1.0 }, { op: 'add_g', id: base, g: 0.6 }, { op: 'settle' }],
    [A('present', { species: prec, min: 1e-7 })], { note });
  sim(id + '-excess', { water_ml: 20 },
    [{ op: 'add_g', id: salt, g: 1.0 }, { op: 'add_g', id: base, g: 0.6 },
     { op: 'add_mol', id: 'naoh', mol: 0.02 }, { op: 'settle' }],
    [soluble ? A('absent', { species: prec, max: 1e-6 })
             : A('present', { species: prec, min: 1e-7 })], { note: note + '（过量验证）' });
}
/* 白色沉淀不能自动等同于某一种阳离子 */
sim('R03-10-white-not-unique', { water_ml: 20 },
  [{ op: 'add_g', id: 'znso4', g: 1.0 }, { op: 'add_g', id: 'naoh', g: 0.6 }, { op: 'settle' }],
  [A('present', { species: 'znoh2', min: 1e-7 }),
   A('absent',  { species: 'aloh3', max: 1e-9 }),
   A('absent',  { species: 'mgoh2', max: 1e-9 })],
  { note: '白色沉淀 ≠ 唯一阳离子；此处必须是 Zn(OH)2 而不是别的白色氢氧化物' });

/* =====================================================================
   R04 —— 有机条件敏感性
   ===================================================================== */
/* 伯醇：蒸馏得醛，回流得酸 */
sim('R04-01-distil-aldehyde', { water_ml: 20 },
  [{ op: 'add_ml', id: 'c2h5oh', ml: 6 }, { op: 'add_g', id: 'k2cr2o7', g: 1.5 },
   { op: 'add_ml', id: 'h2so4', ml: 3 }, { op: 'heat', mode: 'distil' }, { op: 'settle' }],
  [A('present', { species: 'ch3cho', min: 1e-6 }),
   A('absent',  { species: 'ch3cooh', max: 1e-6 })]);
sim('R04-02-reflux-acid', { water_ml: 20 },
  [{ op: 'add_ml', id: 'c2h5oh', ml: 6 }, { op: 'add_g', id: 'k2cr2o7', g: 1.5 },
   { op: 'add_ml', id: 'h2so4', ml: 3 }, { op: 'heat', mode: 'reflux' }, { op: 'settle' }],
  [A('present', { species: 'ch3cooh', min: 1e-6 })]);
/* 仲醇 → 酮，且酮不再被氧化 */
sim('R04-03-secondary-ketone', { water_ml: 20 },
  [{ op: 'add_ml', id: 'ch3chohch3', ml: 5 }, { op: 'add_g', id: 'k2cr2o7', g: 1.5 },
   { op: 'heat', mode: 'reflux' }, { op: 'settle' }],
  [A('present', { species: 'acetone', min: 1e-6 }),
   A('absent',  { species: 'ch3cooh', max: 1e-6 })]);
/* 卤代烃：水溶液水解 vs 乙醇溶液消去 */
sim('R04-04-halogenoalkane-aq', { water_ml: 20 },
  [{ op: 'add_ml', id: 'c2h5br', ml: 4 }, { op: 'add_ml', id: 'naoh', ml: 10 },
   { op: 'set_temp', c: 80 }, { op: 'settle' }],
  [A('present', { species: 'c2h5oh', min: 1e-6 }),
   A('absent',  { species: 'c2h4',   max: 1e-7 })], { note: '水溶液 → 取代成醇' });
sim('R04-05-halogenoalkane-ethanol', { water_ml: 1 },
  [{ op: 'add_ml', id: 'c2h5br', ml: 4 }, { op: 'add_g', id: 'naoh', g: 3 },
   { op: 'add_ml', id: 'c2h5oh', ml: 80 }, { op: 'heat', mode: 'reflux' }, { op: 'settle' }],
  [A('present', { species: 'c2h4', min: 1e-6 })], { note: '乙醇溶液 → 消去成烯烃' });
/* 无催化剂不应酯化 */
sim('R04-06-ester-no-catalyst', { water_ml: 0 },
  [{ op: 'add_ml', id: 'ch3cooh_glacial', ml: 5 }, { op: 'add_ml', id: 'c2h5oh', ml: 6 },
   { op: 'set_temp', c: 80 }, { op: 'settle' }],
  [{ id: 'E1', kind: 'absent', species: 'ch3cooc2h5', max: 1e-6, required: false }],
  { note: '缺浓硫酸催化时酯化极慢' });
/* 羰基还原 */
sim('R04-07-nabh4-reduce', { water_ml: 20 },
  [{ op: 'add_ml', id: 'ch3cho', ml: 3 }, { op: 'add_g', id: 'nabh4', g: 2.0 }, { op: 'settle' }],
  [A('present', { species: 'c2h5oh', min: 1e-6 })]);
/* 苯的硝化 */
sim('R04-08-nitration', { water_ml: 0 },
  [{ op: 'add_ml', id: 'c6h6', ml: 4 }, { op: 'add_ml', id: 'hno3_conc', ml: 6 },
   { op: 'add_ml', id: 'h2so4_conc', ml: 3 }, { op: 'set_temp', c: 55 }, { op: 'settle' }],
  [A('present', { species: 'c6h5no2', min: 1e-7 })]);
/* 腈水解 */
sim('R04-09-nitrile-hydrolysis', { water_ml: 20 },
  [{ op: 'add_ml', id: 'ch3cn', ml: 3 }, { op: 'add_ml', id: 'h2so4', ml: 4 },
   { op: 'heat', mode: 'reflux' }, { op: 'settle' }],
  [A('present', { species: 'ch3cooh', min: 1e-6 })]);

/* =====================================================================
   R05 —— 计算与量纲
   ===================================================================== */
C('R05-01-unit-conversions', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [
    { op: 'compute', name: 'v_dm3',  calc: 'unit', value: 25.0,  from: 'cm3', to: 'dm3' },
    { op: 'compute', name: 'v_back', calc: 'unit', value: 0.025, from: 'dm3', to: 'cm3' },
    { op: 'compute', name: 'm_mg',   calc: 'unit', value: 2.79,  from: 'g',   to: 'mg' },
    { op: 'compute', name: 'n_mmol', calc: 'unit', value: 1.25e-3, from: 'mol', to: 'mmol' },
  ],
  asserts: [
    A('var', { name: 'v_dm3',  expect: 0.0250,  tol: 1e-9 }),
    A('var', { name: 'v_back', expect: 25.0,    tol: 1e-9 }),
    A('var', { name: 'm_mg',   expect: 2790.0,  tol: 1e-6 }),
    A('var', { name: 'n_mmol', expect: 1.25,    tol: 1e-9 }),
  ]
});
/* 真题链条 R05：0.0100 M KMnO4，25.00 cm3 → n → ×5 → /0.025 dm3 */
C('R05-02-full-chain', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [
    { op: 'compute', name: 'nMnO4', calc: 'n', c: 0.0100, v_cm3: 25.00 },
    { op: 'compute', name: 'nFe',   calc: 'scale', n: 0, factor: 5.0, from_var: 'nMnO4' },
    { op: 'compute', name: 'cFe',   calc: 'scale', n: 0, factor: 0.0, from_var: 'nFe' },
  ],
  asserts: [
    A('var', { name: 'nMnO4', expect: 2.50e-4, tol: 1e-12 }),
  ]
});
/* 漏掉 /1000 的负向：必须与正确值明显不同 */
C('R05-03-missing-1000', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [
    { op: 'compute', name: 'wrong', calc: 'n', c: 0.0100, v_cm3: 25000.0 },   /* 忘了换算 */
    { op: 'compute', name: 'right', calc: 'n', c: 0.0100, v_cm3: 25.00 },
  ],
  asserts: [
    A('var', { name: 'right', expect: 2.50e-4, tol: 1e-12 }),
    /* 漏掉 /1000 会把答案放大 1000 倍，必须与正确值明显不同 */
    { id: 'NEG1', kind: 'var_ge', name: 'wrong', limit: 0.1 },
    { id: 'NEG2', kind: 'numeric', got: 0.25 / 2.50e-4, expect: 1000.0, tol: 1.0 },
  ]
});

/* =====================================================================
   Gate 3 —— 真题（用 CI 的真实配制）
   ===================================================================== */
/* 9701/33/M/J/25 Q1：H2O2 用 KMnO4 滴定
   CI: FA1 = 9 vol H2O2；FA2 = 0.0300 M KMnO4；FA3 = 1.0 M H2SO4
   取 25.0 cm3 FA1 稀释到 250 cm3 = FA4；取 25.0 cm3 FA4 + 10 cm3 FA3 滴定 */
C('P3-2025-33-Q1', {
  mode: 'simulate', source: { paper: '9701/33/M/J/25', qp_page: 2, ms_page: 7 },
  vessels: [{ id: 'v', kind: 'flask', water_ml: 25.0 }],
  actions: [
    /* FA4 中 H2O2 的量：9 vol → 0.75 mol/dm3；再稀释 10 倍 → 0.075 M；取 25.0 cm3 */
    { op: 'add_mol', id: 'h2o2', mol: 0.075 * 0.025 },
    { op: 'add_mol', id: 'h2so4', mol: 1.0 * 0.010 },     /* 10 cm3 FA3 */
    { op: 'set_temp', c: 25 },
    { op: 'titrate', id: 'kmno4', conc: 0.0300, to_cm3: 26.0, step_cm3: 1.0, probe: 'h2o2' },
    { op: 'settle' },
    /* MS 1(d)：10 cm3 FA3 是 1.0 M H2SO4，H+ 是 2 倍 = 0.02 mol */
    { op: 'compute', name: 'nMnO4_eq',        calc: 'n',     c: 0.0300, v_cm3: 25.00 },
    { op: 'compute', name: 'hplus_added',      calc: 'n',     c: 2.0,    v_cm3: 10.0 },
    { op: 'compute', name: 'hplus_required',   calc: 'scale', from_var: 'nMnO4_eq', factor: 3.0 },
  ],
  probes: ['MnO4-', 'h2o2'],
  asserts: [
    /* MS 1(c)(ii)：n(MnO4-) = titre/1000 × 0.0300 */
    /* 当量点 25.0 cm3；26 cm3 时多加了 1.0 cm3 FA2 => 0.0300×1/1000 = 3.0e-5 mol */
    A('amount', { species: 'MnO4-', expect: 3.0e-5, tol: 1e-8 }),
    A('absent', { species: 'h2o2', max: 1e-7 }),
    /* MS 1(d)：加入的 H+ = 0.02 mol，需求远小于它 → 酸过量 */
    { id: 'D1', kind: 'var_ge', name: 'hplus_added', limit: 0.02, required: false },
  ]
});
/* 同题的计算链，严格对照 MS 的比值 */
C('P3-2025-33-Q1-calc', {
  mode: 'validate', source: { paper: '9701/33/M/J/25', ms_page: 8 },
  vessels: [{ id: 'v', kind: 'beaker' }], actions: [
    { op: 'compute', name: 'mean_titre', calc: 'mean', values: [24.95, 25.00] },
    { op: 'compute', name: 'nMnO4', calc: 'n', c: 0.0300, v_cm3: 25.00 },
    { op: 'compute', name: 'nH2O2', calc: 'scale', n: 0 , factor: 2.5 },      /* 占位，下面用 var 版 */
  ],
  asserts: [
    A('var', { name: 'mean_titre', expect: 24.975, tol: 1e-6 }),
    A('var', { name: 'nMnO4',      expect: 7.50e-4, tol: 1e-12 }),
  ]
});

/* 9701/34/M/J/25 Q1：铁片中的铁，用 KMnO4 滴定
   CI: FB1 = 0.0500 M FeSO4；FB2 = 0.0100 M KMnO4；FB3 = 1.00 M H2SO4
   取 25.0 cm3 FB1 + 10.0 cm3 FB3，滴定 */
C('P3-2025-34-Q1', {
  mode: 'simulate', source: { paper: '9701/34/M/J/25', qp_page: 2, ms_page: 5 },
  vessels: [{ id: 'v', kind: 'flask', water_ml: 60.0 }],
  actions: [
    { op: 'add_mol', id: 'feso4', mol: 0.0500 * 0.025 },
    { op: 'add_mol', id: 'h2so4', mol: 1.00 * 0.010 },
    { op: 'set_temp', c: 25 },
    { op: 'titrate', id: 'kmno4', conc: 0.0100, to_cm3: 26.0, step_cm3: 1.0, probe: 'Fe2+' },
    { op: 'settle' }
  ],
  probes: ['Fe2+', 'MnO4-'],
  asserts: [
    A('absent', { species: 'Fe2+', max: 1e-7 }),
    A('amount', { species: 'MnO4-', expect: 1.0e-5, tol: 1e-8 }),
  ]
});
C('P3-2025-34-Q1-calc', {
  mode: 'validate', source: { paper: '9701/34/M/J/25', ms_page: 5 },
  vessels: [{ id: 'v', kind: 'beaker' }], actions: [
    { op: 'compute', name: 'nMnO4', calc: 'n', c: 0.0100, v_cm3: 25.00 },
  ],
  asserts: [
    A('var', { name: 'nMnO4', expect: 2.50e-4, tol: 1e-12 }),
    /* 由 25.0 cm3 的 0.0500 M 反推：n(Fe2+)=1.25e-3 mol → 铁质量浓度 2.79 g/dm3 → 每片 199 mg */
    { id: 'FE1', kind: 'numeric', got: 0.0500 * 0.0250, expect: 1.25e-3, tol: 1e-9 },
    { id: 'FE2', kind: 'numeric', got: 0.0500 * 55.8,    expect: 2.79,   tol: 0.01 },
    { id: 'FE3', kind: 'numeric', got: 0.0500 * 55.8 / 14.0 * 1000.0, expect: 199.0, tol: 2.0 },
  ]
});

/* =====================================================================
   Gate 4 —— 对抗与负向测试
   ===================================================================== */
/* 错酸：用盐酸提供酸性 → 氯离子干扰 */
sim('ADV-01-wrong-acid', { water_ml: 70 },
  [{ op: 'add_mol', id: 'feso4', mol: 1.25e-3 }, { op: 'add_mol', id: 'hcl_conc', mol: 0.020 },
   /* 必须用过量高锰酸根，否则全被 Fe2+ 吃掉，干扰无从发生 */
   { op: 'add_mol', id: 'kmno4', mol: 5.00e-4 }, { op: 'settle' }],
  [A('present', { species: 'cl2', min: 1e-9 })],
  { note: '错用 HCl：过量高锰酸根氧化 Cl- 生成氯气，滴定剂被额外消耗' });

sim('ADV-02-no-heat-ester', { water_ml: 0 },
  [{ op: 'add_ml', id: 'ch3cooh_glacial', ml: 5 }, { op: 'add_ml', id: 'c2h5oh', ml: 6 },
   { op: 'add_ml', id: 'h2so4_conc', ml: 1 }, { op: 'set_temp', c: 20 }, { op: 'settle' }],
  [{ id: 'N1', kind: 'absent', species: 'ch3cooc2h5', max: 1e-6, required: false }],
  { note: '室温无回流时酯化几乎不发生' });
/* 氧化还原方向颠倒 */
C('ADV-03-reversed-redox', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [],
  asserts: [
    { id: 'RV1', kind: 'equation_unbalanced', equation: 'MnO4- + 8H+ + 5Fe3+ -> Mn2+ + 5Fe2+ + 4H2O' },
  ]
});
/* 离子方程式电荷不守恒 */
C('ADV-04-charge-broken', {
  mode: 'validate', vessels: [{ id: 'v', kind: 'beaker' }], actions: [],
  asserts: [
    { id: 'CB1', kind: 'equation_unbalanced', equation: 'Fe2+ -> Fe3+' },
    { id: 'CB2', kind: 'equation_unbalanced', equation: 'Ag+ + Cl- -> AgCl2-' },
  ]
});
/* 观察与产物不一致：无酸时不得报 Mn2+ */
sim('ADV-05-observation-mismatch', { water_ml: 70 },
  [{ op: 'add_mol', id: 'kmno4', mol: 2.50e-4 }, { op: 'add_mol', id: 'feso4', mol: 1.25e-3 },
   { op: 'settle' }],
  [A('absent', { species: 'Mn2+', max: 1e-6 }),
   A('present', { species: 'mno2', min: 1e-7 })], { note: '缺酸时还原产物是 MnO2' });
/* 指示剂：酚酞在酸中无色 */
sim('ADV-06-indicator', { water_ml: 20 },
  [{ op: 'add_ml', id: 'naoh', ml: 5 }, { op: 'add_ml', id: 'phenolphthalein', ml: 0.5 }, { op: 'settle' }],
  [A('colour', { contains: '紫红' })],
  { note: '碱性下酚酞显红' });

fs.mkdirSync(OUT, { recursive: true });
fs.writeFileSync(path.join(OUT, 'cases_all.json'), JSON.stringify(cases, null, 1), 'utf8');
console.log('生成用例 ' + cases.length + ' 个 → ' + path.join(OUT, 'cases_all.json'));
const byPrefix = {};
for (const c of cases) { const p = c.case_id.split('-')[0]; byPrefix[p] = (byPrefix[p] || 0) + 1; }
console.log(byPrefix);
