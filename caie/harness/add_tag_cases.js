/* =====================================================================
   容器标签（tag）用例
     验证两种视图、各字段、以及「从离子反推化合物」这类关键场景。
   ===================================================================== */
const fs = require('fs');
const file = 'D:/ChemLabUnity/caie/spec/cases_all.json';
const cases = JSON.parse(fs.readFileSync(file, 'utf8'));
const have = new Set(cases.map(c => c.case_id));
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);
const add = c => { if (!have.has(c.case_id)) { cases.push(c); have.add(c.case_id); } };

/* 1. 单一盐：离子状态 vs 具体物质 */
add({
  case_id: 'TAG-01-nacl-both-views', mode: 'simulate',
  source: { feature: '容器标签', note: '同一个容器在两种视图下的内容' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 200 }],
  actions: [
    { op: 'add_mol', id: 'nacl', mol: 0.020 },
    { op: 'settle' }
  ],
  asserts: [
    /* 离子状态：Na+ 与 Cl- 各 0.020 mol，浓度 0.100 mol/dm3 */
    A('view_has', { molecular: false, species: 'Na+', mol: 0.020, conc: 0.100 }),
    A('view_has', { molecular: false, species: 'Cl-', mol: 0.020, conc: 0.100 }),
    A('view_absent', { molecular: false, species: 'nacl' }),
    /* 具体物质：应复原成 NaCl 一条 */
    A('view_has', { molecular: true, species: 'nacl', mol: 0.020, conc: 0.100 }),
    A('view_absent', { molecular: true, species: 'Na+' }),
    A('view_count', { molecular: true, expect: 1 })
  ]
});

/* 2. 关键场景：从离子反推不出化合物，必须靠账本 */
add({
  case_id: 'TAG-02-ledger-not-guess', mode: 'simulate',
  source: { feature: '容器标签', note: 'NaCl+CuSO4 与 CuCl2+Na2SO4 离子完全相同，只能靠记账' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 200 }],
  actions: [
    { op: 'add_mol', id: 'nacl',  mol: 0.020 },
    { op: 'add_mol', id: 'cuso4', mol: 0.005 },
    { op: 'settle' }
  ],
  asserts: [
    /* 离子总共四条 */
    A('view_count', { molecular: false, expect: 4 }),
    /* 具体物质必须是投入的那两种，而不是贪心配出来的 CuCl2 + Na2SO4 */
    A('view_has', { molecular: true, species: 'nacl',  mol: 0.020 }),
    A('view_has', { molecular: true, species: 'cuso4', mol: 0.005 }),
    A('view_absent', { molecular: true, species: 'cucl2' }),
    A('view_absent', { molecular: true, species: 'na2so4' }),
    A('view_count', { molecular: true, expect: 2 })
  ]
});

/* 3. 浓度随体积变化：同一个物质的量，水量加一倍浓度减半 */
add({
  case_id: 'TAG-03-conc-scales-with-volume', mode: 'simulate',
  source: { feature: '容器标签', note: '浓度 = n / V' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 100 }],
  actions: [
    { op: 'add_mol', id: 'kno3', mol: 0.010 },
    { op: 'settle' }
  ],
  asserts: [
    A('view_has', { molecular: true, species: 'kno3', mol: 0.010, conc: 0.100 })
  ]
});
add({
  case_id: 'TAG-04-conc-halved', mode: 'simulate',
  source: { feature: '容器标签', note: '稀释一倍后浓度减半，物质的量不变' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 200 }],
  actions: [
    { op: 'add_mol', id: 'kno3', mol: 0.010 },
    { op: 'settle' }
  ],
  asserts: [
    A('view_has', { molecular: true, species: 'kno3', mol: 0.010, conc: 0.050 })
  ]
});

/* 4. 反应发生后，账本要跟着扣减 —— 消耗掉的物质不该继续显示 */
add({
  case_id: 'TAG-05-ledger-charged-on-reaction', mode: 'simulate',
  source: { feature: '容器标签', note: 'AgNO3 与 NaCl 反应后，两者都被消耗' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 200 }],
  actions: [
    { op: 'add_mol', id: 'nacl',  mol: 0.010 },
    { op: 'add_mol', id: 'agno3', mol: 0.004 },
    { op: 'settle' }
  ],
  probes: ['agcl'],
  asserts: [
    /* 沉淀是标签里的↓条目 */
    A('present', { species: 'agcl', min: 1e-6 }),
    /* Ag+ 全部被消耗，账本里的 AgNO3 应该少掉 0.004 */
    A('view_has', { molecular: true, species: 'nacl', mol: 0.006, tol: 1e-6 }),
    /* 被完全消耗的物质在账本里已被移除，标签里就不该再出现 */
    A('view_absent', { molecular: true, species: 'agno3' })
  ]
});

/* 5. 混合溶液：多条物质同时显示 */
add({
  case_id: 'TAG-06-multi-substance', mode: 'simulate',
  source: { feature: '容器标签', note: '多种物质同时列出' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 100 }],
  actions: [
    { op: 'add_mol', id: 'nacl',  mol: 0.010 },
    { op: 'add_mol', id: 'kno3',  mol: 0.005 },
    { op: 'add_mol', id: 'cuso4', mol: 0.002 },
    { op: 'settle' }
  ],
  asserts: [
    A('view_count', { molecular: true, expect: 3 }),
    A('view_has', { molecular: true, species: 'nacl',  mol: 0.010 }),
    A('view_has', { molecular: true, species: 'kno3',  mol: 0.005 }),
    A('view_has', { molecular: true, species: 'cuso4', mol: 0.002 }),
    /* 离子状态下会拆成 Na+ Cl- K+ NO3- Cu2+ SO4^2- 六条 */
    A('view_count', { molecular: false, expect: 6 })
  ]
});

/* 6. 惰性容器：空容器不应有任何条目（标签显示为空） */
add({
  case_id: 'TAG-07-empty-vessel', mode: 'simulate',
  source: { feature: '容器标签', note: '空容器标签里没有条目' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 0 }],
  actions: [{ op: 'settle' }],
  asserts: [
    A('view_count', { molecular: true, expect: 0 }),
    A('view_count', { molecular: false, expect: 0 })
  ]
});

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length + '，其中标签 ' + cases.filter(c => c.case_id.startsWith('TAG-')).length + ' 个');
