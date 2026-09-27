/* =====================================================================
   器材交互验证用例
     本生灯：吸附 → 调火力 → 点燃 → 加热
     倾倒：按滑动条比例把 A 倒进 B（用质量守恒反推验证）
   ===================================================================== */
const fs = require('fs');
const path = require('path');
const file = 'D:/ChemLabUnity/caie/spec/cases_all.json';
const cases = JSON.parse(fs.readFileSync(file, 'utf8'));
const have = new Set(cases.map(c => c.case_id));
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);
const add = c => { if (!have.has(c.case_id)) { cases.push(c); have.add(c.case_id); } };

/* ---- 本生灯：吸附 + 点燃 + 加热 ---- */
add({
  case_id: 'APP-01-burner-heats', mode: 'simulate',
  note: '容器吸附到本生灯上，点燃后应按火力升温',
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 50 }],
  actions: [
    { op: 'set_temp', c: 25 },
    { op: 'burner', gas: 0.8, lit: true, seconds: 5.0 }
  ],
  asserts: [
    /* 出气量 0.8 → 加热速率 = lerp(8,60,0.8) = 49.6 ℃/s，5 s 应升到 100 ℃ 以上 */
    A('temp_ge', { limit: 100 }),
    A('temp_le', { limit: 400 })
  ]
});

/* ---- 本生灯：粗调火力应该更慢 ---- */
add({
  case_id: 'APP-02-burner-low-gas', mode: 'simulate',
  note: '小火力加热同样时间，升温应明显少于大火力',
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 50 }],
  actions: [
    { op: 'set_temp', c: 25 },
    { op: 'burner', gas: 0.15, lit: true, seconds: 5.0 }
  ],
  asserts: [
    /* 0.15 → lerp(8,60,0.15) = 15.8 ℃/s，5 s 约 79 ℃ */
    A('temp_ge', { limit: 60 }),
    A('temp_le', { limit: 120 })
  ]
});

/* ---- 本生灯：气量不足点不着 ---- */
add({
  case_id: 'APP-03-burner-too-low', mode: 'simulate',
  note: '出气量低于点火下限时点不着，温度不该变',
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 50 }],
  actions: [
    { op: 'set_temp', c: 25 },
    { op: 'burner', gas: 0.05, lit: true, seconds: 5.0 }
  ],
  asserts: [
    A('temp', { expect: 25, tol: 0.5 })
  ]
});

/* ---- 倾倒：按比例 ---- */
add({
  case_id: 'APP-04-pour-half', mode: 'simulate',
  note: 'A 悬停在 B 上方，按 50% 倒入；用水量与溶质双重核对',
  vessels: [
    { id: 'a', kind: 'beaker', water_ml: 100 },
    { id: 'b', kind: 'beaker', water_ml: 20 }
  ],
  actions: [
    { op: 'add_mol', vessel: 'a', id: 'nacl', mol: 0.02 },
    { op: 'settle', vessel: 'a' },
    { op: 'snap', vessel: 'a', onto: 'b' },
    { op: 'pour_frac', vessel: 'a', to: 'b', frac: 0.5 },
    { op: 'settle', vessel: 'b' }
  ],
  asserts: [
    /* a 剩 50 mL / b 变 70 mL */
    { id: 'W1', kind: 'water', vessel: 'a', expect: 50.0, tol: 0.5 },
    { id: 'W2', kind: 'water', vessel: 'b', expect: 70.0, tol: 0.5 },
    { id: 'N1', kind: 'amount', vessel: 'a', species: 'Cl-', expect: 0.010, tol: 1e-6 },
    { id: 'N2', kind: 'amount', vessel: 'b', species: 'Cl-', expect: 0.010, tol: 1e-6 }
  ]
});

/* ---- 倾倒：全部倒完 ---- */
add({
  case_id: 'APP-05-pour-all', mode: 'simulate',
  note: '滑动条拉到 100% 时 A 应被倒空',
  vessels: [
    { id: 'a', kind: 'tube', water_ml: 30 },
    { id: 'b', kind: 'beaker', water_ml: 20 }
  ],
  actions: [
    { op: 'add_mol', vessel: 'a', id: 'cuso4', mol: 0.005 },
    { op: 'settle', vessel: 'a' },
    { op: 'snap', vessel: 'a', onto: 'b' },
    { op: 'pour_frac', vessel: 'a', to: 'b', frac: 1.0 },
    { op: 'settle', vessel: 'b' }
  ],
  asserts: [
    { id: 'N1', kind: 'absent', vessel: 'a', species: 'Cu2+', max: 1e-9 },
    { id: 'N2', kind: 'present', vessel: 'b', species: 'Cu2+', min: 0.004 }
  ]
});

/* ---- 倾倒：两容器混合后反应应该在目标容器里发生 ---- */
add({
  case_id: 'APP-06-pour-causes-reaction', mode: 'simulate',
  note: '把 AgNO3 倒进 NaCl 里，沉淀应在目标容器产生',
  vessels: [
    { id: 'a', kind: 'tube', water_ml: 20 },
    { id: 'b', kind: 'beaker', water_ml: 20 }
  ],
  actions: [
    { op: 'add_mol', vessel: 'a', id: 'agno3', mol: 0.002 },
    { op: 'settle', vessel: 'a' },
    { op: 'add_mol', vessel: 'b', id: 'nacl', mol: 0.002 },
    { op: 'settle', vessel: 'b' },
    { op: 'snap', vessel: 'a', onto: 'b' },
    { op: 'pour_frac', vessel: 'a', to: 'b', frac: 0.8 },
    { op: 'settle', vessel: 'b' }
  ],
  asserts: [
    { id: 'P1', kind: 'present', vessel: 'b', species: 'agcl', min: 1e-7 },
    /* 倒过去 80% × 0.002 = 0.0016 mol，NaCl 有 0.002 mol → 限量的是 Ag+ */
    { id: 'P2', kind: 'amount', vessel: 'b', species: 'agcl', expect: 0.0016, tol: 1e-5 }
  ]
});

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length);
