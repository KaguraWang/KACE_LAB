/* =====================================================================
   反应方程式标签用例
     中英两种规范、可逆反应、离子方程式、条件标注
   ===================================================================== */
const fs = require('fs');
const file = 'D:/ChemLabUnity/caie/spec/cases_all.json';
const cases = JSON.parse(fs.readFileSync(file, 'utf8'));
const have = new Set(cases.map(c => c.case_id));
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);
const add = c => { if (!have.has(c.case_id)) { cases.push(c); have.add(c.case_id); } };

/* 1. 中文无机规范：等号、气体↑、不标状态符号 */
add({
  case_id: 'EQ-01-chinese-inorganic', mode: 'validate',
  source: { feature: '方程式标签', note: '中文无机反应用等号，气体标↑，不标状态符号' },
  vessels: [{ id: 'v', kind: 'beaker' }],
  actions: [],
  asserts: [
    A('eq_format', { rule: 'CO3^2- + 2H+ -> H2O + CO2', english: false, ionic: false,
                     contains: [' = ', 'CO₃²⁻', '2H⁺', 'H₂O', 'CO₂↑'] }),
    /* 中文不出现状态符号 */
    A('eq_format', { rule: 'H+ + OH- -> H2O', english: false, ionic: false, contains: [' = '] })
  ]
});

/* 2. 英文规范：箭头 + 每种物质后的状态符号 */
add({
  case_id: 'EQ-02-english-states', mode: 'validate',
  source: { feature: '方程式标签', note: '英文一律箭头，且必须标 (s)/(l)/(g)/(aq)' },
  vessels: [{ id: 'v', kind: 'beaker' }],
  actions: [],
  asserts: [
    A('eq_format', { rule: 'CO3^2- + 2H+ -> H2O + CO2', english: true, ionic: false,
                     contains: [' → ', '(aq)', 'H₂O(l)', 'CO₂(g)'] }),
    A('eq_format', { rule: 'H+ + OH- -> H2O', english: true, ionic: false,
                     contains: [' → ', 'H⁺(aq)', 'OH⁻(aq)', 'H₂O(l)'] })
  ]
});

/* 3. 可逆反应：中英都用双向箭头 ⇌ */
add({
  case_id: 'EQ-03-reversible', mode: 'validate',
  source: { feature: '方程式标签', note: '可逆反应中英都用可逆符号 ⇌' },
  vessels: [{ id: 'v', kind: 'beaker' }],
  actions: [],
  asserts: [
    A('eq_format', { rule: 'N2 + 3H2 -> 2NH3', english: false, ionic: false, contains: [' ⇌ ', 'N₂', 'H₂', 'NH₃'] }),
    A('eq_format', { rule: 'N2 + 3H2 -> 2NH3', english: true,  ionic: false, contains: [' ⇌ ', '(g)'] }),
    A('eq_format', { rule: '2NO2 -> N2O4', english: false, ionic: false, contains: [' ⇌ '] })
  ]
});

/* 4. 离子方程式：消去旁观离子 */
add({
  case_id: 'EQ-04-ionic-spectators-removed', mode: 'validate',
  source: { feature: '方程式标签', note: '离子式要消去两边都出现的旁观离子' },
  vessels: [{ id: 'v', kind: 'beaker' }],
  actions: [],
  asserts: [
    /* 2NaOH + H2SO4 -> Na2SO4 + 2H2O 的离子式应消掉 Na+ 与 SO4^2-，
       只留 H+ + OH- -> H2O */
    A('eq_format', { rule: '2NaOH + H2SO4 -> Na2SO4 + 2H2O', english: true, ionic: true,
                     contains: ['H⁺(aq)', 'OH⁻(aq)', 'H₂O(l)'] })
  ]
});

/* 5. 条件标注：翻成教科书上的写法 */
add({
  case_id: 'EQ-05-conditions', mode: 'validate',
  source: { feature: '方程式标签', note: '反应条件写在连接符上方，要译成教科书用语' },
  vessels: [{ id: 'v', kind: 'beaker' }],
  actions: [],
  asserts: [
    /* 有条件的反应，条件要译成教科书用语 */
    A('eq_condition', { rule: '2H2O2 -> 2H2O + O2', english: false, expect: '催化剂' }),
    A('eq_condition', { rule: '2H2O2 -> 2H2O + O2', english: true,  expect: 'catalyst' }),
    A('eq_condition', { rule: 'CaCO3 -> CaO + CO2', english: false, expect: '加热' }),
    A('eq_condition', { rule: 'CaCO3 -> CaO + CO2', english: true,  expect: 'heat' }),
    /* 无条件的反应不该硬凑一个条件出来 */
    A('eq_condition', { rule: 'H+ + OH- -> H2O',    english: false, expect: '' })
  ]
});

/* 6. 真实反应：Na2CO3 + HCl 两步，元素与电荷都要守恒（由自检覆盖格式化正确性） */
add({
  case_id: 'EQ-06-real-reaction', mode: 'simulate',
  source: { feature: '方程式标签', note: '真实反应要能弹出标签' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 100 }],
  actions: [
    { op: 'add_g', id: 'na2co3', g: 2.0 },
    { op: 'add_ml', id: 'hcl', ml: 10 },
    { op: 'settle' }
  ],
  probes: ['co2'],
  asserts: [
    A('present', { species: 'co2', min: 1e-5 })
  ]
});

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length + '，其中方程式 ' + cases.filter(c => c.case_id.startsWith('EQ-')).length + ' 个');
