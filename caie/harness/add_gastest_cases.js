/* =====================================================================
   离子检验器材用例 —— 逐条覆盖 CIE 9701「Qualitative analysis notes」
     3 Tests for gases:
        ammonia  NH3  → turns damp red litmus paper blue
        carbon dioxide CO2 → gives a white ppt. with limewater
        hydrogen H2   → 'pops' with a lighted splint
        oxygen   O2   → relights a glowing splint
     4 Tests for elements:
        iodine   I2   → gives blue-black colour with starch
   并在真实的离子检验流程里测试（NH4+ 的检验、CO32- 的检验等）。
   ===================================================================== */
const fs = require('fs');
const path = require('path');
const file = 'D:/ChemLabUnity/caie/spec/cases_all.json';
const cases = JSON.parse(fs.readFileSync(file, 'utf8'));
const have = new Set(cases.map(c => c.case_id));
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);
const add = c => { if (!have.has(c.case_id)) { cases.push(c); have.add(c.case_id); } };

/* =====================================================================
   一、官方表「Tests for gases」四条
   ===================================================================== */

/* 1. NH3 —— 使湿润的红色石蕊试纸变蓝 */
/*    真实情境：铵盐与碱共热放出氨气（CIE cations 表里 NH4+ 那一行） */
add({
  case_id: 'GAS-NH3-red-litmus', mode: 'simulate',
  source: { table: 'CIE 9701 / Tests for gases', gas: 'NH3', expect: 'turns damp red litmus paper blue' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_g', id: 'nh4cl', g: 1.0 },
    { op: 'add_mol', id: 'naoh', mol: 0.03 },
    { op: 'set_temp', c: 80 },
    { op: 'settle' },
    { op: 'add_test', kind: 'red_litmus', as: 't' },
    { op: 'moisten', test: 't' },
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [
    A('present', { species: 'nh3', min: 1e-6 }),
    A('litmus_turned', { test: 't', to: 'blue' }),
    A('test_result', { test: 't', contains: '碱性' })
  ]
});

/* 对照 1：干燥的红石蕊遇氨气**不变色** —— 必须先湿润 */
add({
  case_id: 'GAS-NH3-dry-litmus', mode: 'simulate',
  source: { table: 'CIE 9701 / Tests for gases', gas: 'NH3', note: '干燥试纸不响应' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_g', id: 'nh4cl', g: 1.0 },
    { op: 'add_mol', id: 'naoh', mol: 0.03 },
    { op: 'set_temp', c: 80 },
    { op: 'settle' },
    { op: 'add_test', kind: 'red_litmus', as: 't' },
    /* 刻意不 moisten */
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [
    A('litmus_turned', { test: 't', to: 'none' }),
    A('test_result', { test: 't', contains: '湿润' })
  ]
});

/* 对照 2：蓝色石蕊遇碱性气体保持蓝色 */
add({
  case_id: 'GAS-NH3-blue-litmus', mode: 'simulate',
  source: { table: 'CIE 9701 / Tests for gases', gas: 'NH3', note: '蓝石蕊在碱性中不变' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_g', id: 'nh4cl', g: 1.0 },
    { op: 'add_mol', id: 'naoh', mol: 0.03 },
    { op: 'set_temp', c: 80 },
    { op: 'settle' },
    { op: 'add_test', kind: 'blue_litmus', as: 't' },
    { op: 'moisten', test: 't' },
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [A('litmus_turned', { test: 't', to: 'none' })]
});

/* 2. CO2 —— 使澄清石灰水变浑浊（白色沉淀） */
add({
  case_id: 'GAS-CO2-limewater', mode: 'simulate',
  source: { table: 'CIE 9701 / Tests for gases', gas: 'CO2', expect: 'gives a white ppt. with limewater' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    /* 真实情境：碳酸盐与稀酸反应（CIE anions 表里 CO3^2- 那一行） */
    { op: 'add_g', id: 'na2co3', g: 2.0 },
    { op: 'add_ml', id: 'hcl', ml: 8 },
    { op: 'settle' },
    /* 把气体通入澄清石灰水 */
    { op: 'add_ml', id: 'limewater', ml: 5 },
    { op: 'settle' }
  ],
  probes: ['caco3'],
  asserts: [
    A('present', { species: 'caco3', min: 1e-7 }),
    A('present', { species: 'co2', min: 1e-5, required: false })
  ]
});

/* 3. H2 —— 点燃的木条发出爆鸣声 */
add({
  case_id: 'GAS-H2-pop', mode: 'simulate',
  source: { table: 'CIE 9701 / Tests for gases', gas: 'H2', expect: "'pops' with a lighted splint" },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    /* 真实情境：较活泼金属与稀酸反应（Mg + 2H+ -> Mg2+ + H2） */
    { op: 'add_g', id: 'mg', g: 0.3 },
    { op: 'add_ml', id: 'hcl', ml: 10 },
    { op: 'settle' },
    { op: 'add_test', kind: 'splint', as: 't' },
    { op: 'light_splint', test: 't' },
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [
    A('popped', { min: 0.5 }),
    A('test_result', { test: 't', contains: '爆鸣' })
  ]
});

/* 对照 3：未点燃的木条什么也检验不出来 */
add({
  case_id: 'GAS-H2-unlit-splint', mode: 'simulate',
  source: { table: 'CIE 9701 / Tests for gases', gas: 'H2', note: '木条必须点燃' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_g', id: 'mg', g: 0.3 },
    { op: 'add_ml', id: 'hcl', ml: 10 },
    { op: 'settle' },
    { op: 'add_test', kind: 'splint', as: 't' },
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [
    A('popped', { max: 1e-9 }),
    A('test_result', { test: 't', contains: '没有点燃' })
  ]
});

/* 对照 4：点燃的木条伸入甲烷是安静燃烧，**没有爆鸣** —— 这是和氢气的关键区别 */
add({
  case_id: 'GAS-CH4-no-pop', mode: 'simulate',
  source: { table: 'CIE 9701 / Tests for gases', note: '可燃但无爆鸣' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_mol', id: 'ch4', mol: 0.01 },
    { op: 'settle' },
    { op: 'add_test', kind: 'splint', as: 't' },
    { op: 'light_splint', test: 't' },
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [
    A('popped', { max: 1e-9 }),
    A('test_result', { test: 't', contains: '没有爆鸣' })
  ]
});

/* 4. O2 —— 带火星的木条复燃 */
add({
  case_id: 'GAS-O2-relight', mode: 'simulate',
  source: { table: 'CIE 9701 / Tests for gases', gas: 'O2', expect: 'relights a glowing splint' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    /* 真实情境：过氧化氢在二氧化锰催化下分解 */
    { op: 'add_ml', id: 'h2o2', ml: 12 },
    { op: 'add_g', id: 'mno2', g: 0.3 },
    { op: 'settle' },
    { op: 'add_test', kind: 'splint', as: 't' },
    { op: 'light_splint', test: 't' },
    /* 先熄灭，得到「刚刚熄灭、带火星」那一态 */
    { op: 'extinguish_splint', test: 't' },
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [
    A('present', { species: 'o2', min: 1e-5, required: false }),
    A('splint_state', { test: 't', expect: 'Burning' }),
    A('test_result', { test: 't', contains: '复燃' })
  ]
});

/* 对照 5：带火星的木条伸入二氧化碳熄灭 */
add({
  case_id: 'GAS-CO2-splint-out', mode: 'simulate',
  source: { table: 'CIE 9701 / Tests for gases', note: 'CO2 不支持燃烧' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_g', id: 'na2co3', g: 2.0 },
    { op: 'add_ml', id: 'hcl', ml: 8 },
    { op: 'settle' },
    { op: 'add_test', kind: 'splint', as: 't' },
    { op: 'light_splint', test: 't' },
    { op: 'extinguish_splint', test: 't' },
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [
    A('splint_state', { test: 't', expect: 'Normal' }),
    A('test_result', { test: 't', contains: '熄灭' })
  ]
});

/* =====================================================================
   二、官方表「Tests for elements」—— 碘遇淀粉显蓝黑
   ===================================================================== */
add({
  case_id: 'GAS-ELEM-iodine-starch', mode: 'simulate',
  source: { table: 'CIE 9701 / Tests for elements', element: 'I2', expect: 'blue-black colour with starch' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_mol', id: 'starch', mol: 0.002 },
    { op: 'settle' },
    { op: 'add_ml', id: 'iodine_sol', ml: 3 },
    { op: 'settle' }
  ],
  asserts: [A('colour', { contains: '蓝' })]
});

/* =====================================================================
   三、酸性气体的石蕊检验（官方表没列，但离子检验里同样常用）
   ===================================================================== */
add({
  case_id: 'GAS-SO2-blue-litmus', mode: 'simulate',
  source: { table: 'CIE 9701 / anions', ion: 'SO3^2-', expect: '放出的 SO2 显酸性' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_g', id: 'na2so3', g: 1.5 },
    { op: 'add_ml', id: 'hcl', ml: 8 },
    { op: 'settle' },
    { op: 'add_test', kind: 'blue_litmus', as: 't' },
    { op: 'moisten', test: 't' },
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [
    A('litmus_turned', { test: 't', to: 'red' }),
    A('test_result', { test: 't', contains: '酸性' })
  ]
});

/* =====================================================================
   四、放进真实考卷的检验流程
   ===================================================================== */

/* 9701/33/M/J/24 Q3：FA 5 = ZnCO3 + KI 混合物，两个阳离子两个阴离子。
   QP 的检验表里就有「加稀盐酸」和「加热」两步 —— 这里连同气体检验一起走通。 */
add({
  case_id: 'EXAM-2433-Q3-acid-then-limewater', mode: 'simulate',
  source: { paper: '9701/33/M/J/24', qp_page: 6, test: '加稀盐酸检验碳酸根，气体通入石灰水' },
  /* 两只容器：左边放 FA5 加酸，右边是澄清石灰水，
     把气体**通入**石灰水 —— 不能直接倒在一起，否则锌离子会先把 OH- 抢走。 */
  vessels: [
    { id: 'v',  kind: 'tube', water_ml: 12 },
    { id: 'lw', kind: 'tube', water_ml: 5 }
  ],
  actions: [
    { op: 'add_g', id: 'znco3', g: 1.0 },       /* FA5 里的碳酸锌 */
    { op: 'add_ml', id: 'hcl', ml: 8 },
    { op: 'settle' },
    { op: 'add_ml', vessel: 'lw', id: 'limewater', ml: 5 },
    { op: 'settle', vessel: 'lw' },
    { op: 'transfer_gas', gas: 'co2', to: 'lw' },
    { op: 'settle', vessel: 'lw' }
  ],
  asserts: [
    A('present', { species: 'caco3', min: 1e-7, vessel: 'lw' }),
    /* 锌进入溶液，与 KI 的碘离子分开 */
    A('present', { species: 'Zn2+', min: 1e-4 })
  ]
});

/* 9701/33/M/J/24 Q3：加硝酸银得黄色碘化银（阴离子检验） */
add({
  case_id: 'EXAM-2433-Q3-iodide', mode: 'simulate',
  source: { paper: '9701/33/M/J/24', qp_page: 6, test: '加硝酸银检验碘离子' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_g', id: 'ki', g: 0.5 },
    { op: 'add_g', id: 'agno3', g: 1.0 },
    { op: 'settle' }
  ],
  asserts: [A('present', { species: 'agi', min: 1e-7 })]
});

/* 9701/33/M/J/26 Q3(b) Test 1：FA7 = Cr2(SO4)3 + NaNO3，
   加 NaOH 后加热并加铝箔 —— 放出 NH3，用湿润红色石蕊试纸检验，这一步在 QP 里是明确要求的 */
add({
  case_id: 'EXAM-2633-Q3B-nitrate-litmus', mode: 'simulate',
  source: { paper: '9701/33/M/J/26', qp_page: 8, test: 'Test 1：加 NaOH、加热、加铝箔，用石蕊检验 NH3' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_g', id: 'nano3', g: 1.5 },
    { op: 'add_mol', id: 'naoh', mol: 0.08 },
    { op: 'add_g', id: 'al', g: 0.5 },
    { op: 'set_temp', c: 90 },
    { op: 'settle' },
    { op: 'add_test', kind: 'red_litmus', as: 't' },
    { op: 'moisten', test: 't' },
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [
    A('present', { species: 'nh3', min: 1e-6 }),
    A('litmus_turned', { test: 't', to: 'blue' })
  ]
});

/* 9701/34/M/J/24 Q3：FB5 = (NH4)2CO3 —— MS 明确写「warm FB5 with NaOH AND
   gas/ammonia turns (red) litmus to blue」，正是这条检验的组合 */
add({
  case_id: 'EXAM-2434-Q3-ammonium-carbonate', mode: 'simulate',
  source: { paper: '9701/34/M/J/24', ms_page: 10, expect: 'warm with NaOH，氨气使红色石蕊变蓝' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 15 }],
  actions: [
    { op: 'add_g', id: 'nh42co3', g: 0.5 },
    { op: 'add_mol', id: 'naoh', mol: 0.04 },
    { op: 'set_temp', c: 80 },
    { op: 'settle' },
    { op: 'add_test', kind: 'red_litmus', as: 't' },
    { op: 'moisten', test: 't' },
    { op: 'insert', test: 't', as: 'res' },
    { op: 'settle' }
  ],
  asserts: [
    A('present', { species: 'nh3', min: 1e-6 }),
    A('litmus_turned', { test: 't', to: 'blue' })
  ]
});

/* 9701/34/M/J/24 Q3(a)(iii)：MS 写「fizzing / bubbles / effervescence」——
   碳酸盐遇酸冒气泡，用石灰水确认是 CO2 */
add({
  case_id: 'EXAM-2434-Q3-carbonate-fizz', mode: 'simulate',
  source: { paper: '9701/34/M/J/24', ms_page: 10, expect: 'fizzing；气体使石灰水变浑浊' },
  vessels: [{ id: 'v', kind: 'tube', water_ml: 12 }],
  actions: [
    { op: 'add_g', id: 'nh42co3', g: 0.5 },
    { op: 'add_ml', id: 'hcl', ml: 8 },
    { op: 'settle' },
    { op: 'add_ml', id: 'limewater', ml: 5 },
    { op: 'settle' }
  ],
  asserts: [A('present', { species: 'caco3', min: 1e-7 })]
});

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length);
console.log('其中检验器材 ' + cases.filter(c => c.case_id.startsWith('GAS-')).length +
            ' 个，真实考卷流程 ' + cases.filter(c => c.case_id.startsWith('EXAM-')).length + ' 个');
