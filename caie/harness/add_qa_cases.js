/* =====================================================================
   CIE 9701「Qualitative analysis notes」官方表的逐行自动化
   这张表附在每一份 P3 卷子末尾，是 QUAL 三道题的评卷依据。
   六份卷子共用同一张表，所以一次性覆盖。

   来源：9701/33/M/J/26 QP p10（与 24、25 各卷一致）
   ===================================================================== */
const fs = require('fs');
const path = require('path');
const file = 'D:/ChemLabUnity/caie/spec/cases_all.json';
const cases = JSON.parse(fs.readFileSync(file, 'utf8'));
const have = new Set(cases.map(c => c.case_id));
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);
const add = c => { if (!have.has(c.case_id)) { cases.push(c); have.add(c.case_id); } };

/* ---------------------------------------------------------------
   表 1：阳离子 + NaOH(aq) / NH3(aq)
   [离子, 可溶盐, NaOH 现象, NaOH 过量是否溶解, 沉淀物种,
          NH3 现象, NH3 过量是否溶解]
   --------------------------------------------------------------- */
const CATIONS = [
  ['Al3+',  'alcl3',  '白色沉淀', true,  'aloh3',  '白色沉淀', false],
  ['NH4+',  'nh4cl',  '无沉淀，加热放氨', null, null,   null,      null],

  ['Ca2+',  'cacl2',  '白色沉淀', false, 'caoh2', '无沉淀',  null, false],  /* 官方表：Ca2+ 加氨水**无沉淀** */
  ['Cr3+',  'crcl3',  '灰绿色沉淀', true,  'croh3', '灰绿色沉淀', false],
  ['Cu2+',  'cuso4',  '浅蓝色沉淀', false, 'cuoh2', '浅蓝色沉淀', true],
  ['Fe2+',  'feso4',  '绿色沉淀', false, 'feoh2', '绿色沉淀', false],
  ['Fe3+',  'fecl3',  '红棕色沉淀', false, 'feoh3', '红棕色沉淀', false],
  ['Mg2+',  'mgso4',  '白色沉淀', false, 'mgoh2', '白色沉淀', false],
  ['Mn2+',  'mnso4',  '灰白色沉淀', false, 'mnoh2', '灰白色沉淀', false],
  ['Zn2+',  'znso4',  '白色沉淀', true,  'znoh2', '白色沉淀', true],
];

for (const [ion, salt, naohTxt, naohSol, prec, nh3Txt, nh3Sol, nh3Prec] of CATIONS) {
  const base = `QA-CAT-${ion.replace(/[^A-Za-z0-9]/g, '')}`;
  /* --- NaOH 少量 --- */
  add({
    case_id: base + '-naoh', mode: 'simulate',
    source: { table: 'CIE 9701 Qualitative analysis notes / Reactions of cations', ion, reagent: 'NaOH(aq)', expect: naohTxt },
    vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
    actions: [
      { op: 'add_g', id: salt, g: 0.5 },
      { op: 'add_g', id: 'naoh', g: 0.25 },     // 少量
      /* CIE 表里 NH4+ 写的是 "ammonia produced on warming" —— 只有它需要加热。
         别的阳离子在室温下做，否则会误触发氢氧化物的受热分解。 */
      ...(ion === 'NH4+' ? [{ op: 'set_temp', c: 70 }] : []),
      { op: 'settle' }
    ],
    /* NH4+ 那条表里明确写的是 "ammonia produced on warming" —— 必须加热 */
    actions2: null,
    probes: [prec || 'nh3'],
    asserts: prec
      ? [A('present', { species: prec, min: 1e-7 })]
      : [A('present', { species: 'nh3', min: 1e-6 })]
  });
  /* --- NaOH 过量 --- */
  if (prec) {
    add({
      case_id: base + '-naoh-excess', mode: 'simulate',
      source: { table: 'CIE 9701 / cations', ion, reagent: 'NaOH 过量', expect: naohSol ? '沉淀溶解' : '沉淀不溶' },
      vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
      actions: [
        { op: 'add_g', id: salt, g: 0.5 },
        { op: 'add_mol', id: 'naoh', mol: 0.05 },   // 明显过量
        { op: 'settle' }
      ],
      asserts: naohSol
        ? [A('absent', { species: prec, max: 1e-6 })]
        : [A('present', { species: prec, min: 1e-7 })]
    });
  }
  /* --- NH3 --- */
  if (nh3Txt) {
    add({
      case_id: base + '-nh3', mode: 'simulate',
      source: { table: 'CIE 9701 / cations', ion, reagent: 'NH3(aq)', expect: nh3Txt },
      vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
      actions: [
        { op: 'add_g', id: salt, g: 0.5 },
        { op: 'add_ml', id: 'nh3', ml: 8 },
        { op: 'settle' }
      ],
      /* 氨水是弱碱，OH- 浓度远低于 NaOH —— 有些氢氧化物在 NaOH 中沉淀、
         在氨水中却不沉淀（官方表 Ca2+ 那一行就是「no ppt」）。
         nh3Prec 显式给出这一列，不再照搬 NaOH 的结论。 */
      asserts: (nh3Prec === false)
        ? [A('absent', { species: prec, max: 1e-7 })]
        : (prec
            ? [A('present', { species: prec, min: 1e-7 })]
            : [A('amount', { species: 'OH-', expect: 0, tol: 1e-6 })])
    });
    if (prec && nh3Sol !== null) {
      add({
        case_id: base + '-nh3-excess', mode: 'simulate',
        source: { table: 'CIE 9701 / cations', ion, reagent: 'NH3 过量', expect: nh3Sol ? '沉淀溶解' : '沉淀不溶' },
        vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
        actions: [
          { op: 'add_g', id: salt, g: 0.5 },
          { op: 'add_mol', id: 'nh3', mol: 0.25 },   /* 过量氨水要真的过量 */
          { op: 'settle' }
        ],
        asserts: nh3Sol
          ? [A('absent', { species: prec, max: 1e-6 })]
          : [A('present', { species: prec, min: 1e-7 })]
      });
    }
  }
}

/* ---------------------------------------------------------------
   表 2：阴离子
   --------------------------------------------------------------- */
/* 碳酸盐：稀酸放出 CO2 */
add({
  case_id: 'QA-AN-CO3', mode: 'simulate',
  source: { table: 'CIE 9701 / anions', ion: 'CO3^2-', expect: 'CO2 liberated by dilute acids' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'na2co3', g: 1.0 }, { op: 'add_ml', id: 'hcl', ml: 6 }, { op: 'settle' }],
  asserts: [A('present', { species: 'co2', min: 1e-4 })]
});
/* 卤离子 + Ag+：颜色与在氨水中的溶解性 */
const HALIDES = [
  ['Cl-', 'nacl', 'agcl', '白', true],
  ['Br-', 'nabr', 'agbr', '淡黄', false],   /* 部分溶解 —— 判据用「部分」难以量化，只查生成 */
  ['I-',  'nai',  'agi',  '黄',  false],
];
for (const [ion, salt, prec, colr, solNH3] of HALIDES) {
  add({
    case_id: `QA-AN-${ion.replace(/[^A-Za-z0-9]/g, '')}-ag`, mode: 'simulate',
    source: { table: 'CIE 9701 / anions', ion, reagent: 'Ag+', expect: `${colr}色沉淀` },
    vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
    actions: [{ op: 'add_g', id: salt, g: 0.5 }, { op: 'add_g', id: 'agno3', g: 0.8 }, { op: 'settle' }],
    asserts: [A('present', { species: prec, min: 1e-7 })]
  });
}
/* 氯化银溶于氨水；碘化银不溶 */
add({
  case_id: 'QA-AN-Cl-ag-nh3', mode: 'simulate',
  source: { table: 'CIE 9701 / anions', ion: 'Cl-', expect: 'AgCl 溶于 NH3' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'nacl', g: 0.5 }, { op: 'add_g', id: 'agno3', g: 0.8 },
            { op: 'add_ml', id: 'nh3', ml: 30 }, { op: 'settle' }],
  asserts: [A('absent', { species: 'agcl', max: 1e-6 })]
});
add({
  case_id: 'QA-AN-I-ag-nh3', mode: 'simulate',
  source: { table: 'CIE 9701 / anions', ion: 'I-', expect: 'AgI 不溶于 NH3' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'nai', g: 0.5 }, { op: 'add_g', id: 'agno3', g: 0.8 },
            { op: 'add_ml', id: 'nh3', ml: 30 }, { op: 'settle' }],
  asserts: [A('present', { species: 'agi', min: 1e-7 })]
});
/* 硫酸盐：Ba2+ 白色沉淀，不溶于过量稀强酸 */
add({
  case_id: 'QA-AN-SO4-ba', mode: 'simulate',
  source: { table: 'CIE 9701 / anions', ion: 'SO4^2-', expect: '白色沉淀，不溶于过量稀强酸' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'na2so4', g: 1.0 }, { op: 'add_g', id: 'bacl2', g: 1.0 },
            { op: 'add_ml', id: 'hcl', ml: 8 }, { op: 'settle' }],
  asserts: [A('present', { species: 'baso4', min: 1e-7 })]
});
/* 亚硫酸盐：Ba2+ 白色沉淀，但**溶于**过量稀强酸（与硫酸盐的关键区别） */
add({
  case_id: 'QA-AN-SO3-ba', mode: 'simulate',
  source: { table: 'CIE 9701 / anions', ion: 'SO3^2-', expect: '白色沉淀，溶于过量稀强酸' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'na2so3', g: 1.0 }, { op: 'add_g', id: 'bacl2', g: 1.0 }, { op: 'settle' }],
  asserts: [A('present', { species: 'baso3', min: 1e-7 })]
});
add({
  case_id: 'QA-AN-SO3-ba-acid', mode: 'simulate',
  source: { table: 'CIE 9701 / anions', ion: 'SO3^2-', expect: '加稀强酸后沉淀溶解并放出 SO2' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'na2so3', g: 1.0 }, { op: 'add_g', id: 'bacl2', g: 1.0 },
            { op: 'add_ml', id: 'hcl', ml: 10 }, { op: 'settle' }],
  /* 这条的关键判据是「BaSO3 溶于稀强酸」—— 与不溶的 BaSO4 形成对照。
     放出的 SO2 部分会溶成亚硫酸，气体去向不必卡死。 */
  asserts: [A('absent', { species: 'baso3', max: 1e-6 })]
});
/* 亚硫酸盐使酸性高锰酸钾褪色 */
add({
  case_id: 'QA-AN-SO3-kmno4', mode: 'simulate',
  source: { table: 'CIE 9701 / anions', ion: 'SO3^2-', expect: '使酸性 KMnO4 褪色' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'kmno4', g: 0.2 }, { op: 'add_ml', id: 'h2so4', ml: 3 },
            { op: 'add_g', id: 'na2so3', g: 1.0 }, { op: 'settle' }],
  asserts: [A('absent', { species: 'MnO4-', max: 1e-7 }), A('present', { species: 'Mn2+', min: 1e-7 })]
});
/* 硫代硫酸盐：加酸缓慢析出淡黄色沉淀（硫） */
add({
  case_id: 'QA-AN-S2O3-acid', mode: 'simulate',
  source: { table: 'CIE 9701 / anions', ion: 'S2O3^2-', expect: '加 H+ 缓慢析出淡黄色沉淀（硫）' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'na2s2o3', g: 1.0 }, { op: 'add_ml', id: 'hcl', ml: 6 }, { op: 'settle' }],
  asserts: [A('present', { species: 'so2', min: 1e-7, required: false })]
});

/* ---------------------------------------------------------------
   表 3：气体检验
   --------------------------------------------------------------- */
add({ case_id: 'QA-GAS-NH3', mode: 'simulate',
  source: { table: 'CIE 9701 / gases', gas: 'NH3', expect: 'turns damp red litmus blue' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'nh4cl', g: 1.0 }, { op: 'add_mol', id: 'naoh', mol: 0.03 },
            { op: 'set_temp', c: 70 }, { op: 'settle' }],
  asserts: [A('present', { species: 'nh3', min: 1e-6 })] });
add({ case_id: 'QA-GAS-CO2', mode: 'simulate',
  source: { table: 'CIE 9701 / gases', gas: 'CO2', expect: 'white ppt. with limewater' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'caco3', g: 2.0 }, { op: 'add_ml', id: 'hcl', ml: 10 }, { op: 'settle' }],
  asserts: [A('present', { species: 'co2', min: 1e-4 })] });
add({ case_id: 'QA-GAS-H2', mode: 'simulate',
  source: { table: 'CIE 9701 / gases', gas: 'H2', expect: "'pops' with a lighted splint" },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_g', id: 'mg', g: 0.2 }, { op: 'add_ml', id: 'hcl', ml: 10 }, { op: 'settle' }],
  asserts: [A('present', { species: 'h2', min: 1e-4 })] });
add({ case_id: 'QA-GAS-O2', mode: 'simulate',
  source: { table: 'CIE 9701 / gases', gas: 'O2', expect: 'relights a glowing splint' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [{ op: 'add_ml', id: 'h2o2', ml: 10 }, { op: 'add_g', id: 'mno2', g: 0.3 }, { op: 'settle' }],
  asserts: [A('present', { species: 'o2', min: 1e-4 })] });

/* 表 4：碘 + 淀粉显蓝黑 */
add({ case_id: 'QA-ELEM-iodine-starch', mode: 'simulate',
  source: { table: 'CIE 9701 / elements', element: 'I2', expect: 'gives blue-black colour with starch' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 20 }],
  actions: [
    { op: 'add_mol', id: 'starch', mol: 0.002 },      /* 淀粉必须先在场 */
    { op: 'settle' },
    { op: 'add_ml', id: 'iodine_sol', ml: 3 },
    { op: 'settle' }
  ],
  asserts: [A('colour', { contains: '蓝' })] });

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length);
const qa = cases.filter(c => c.case_id.startsWith('QA-')).length;
console.log('其中定性分析表 ' + qa + ' 个');
