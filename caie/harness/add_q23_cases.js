/* =====================================================================
   P3 的 Q2 / Q3 用例
      Q2：热化学（溶解焓 / 分解焓 / 中和焓）
      Q3：定性分析状态机
   依据：QP 方法 + CI 真实配制 + MS 比值链
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
   9701/33/M/J/24 Q2 —— NH4Br 溶解焓（吸热，ΔH = +16.8 kJ/mol）
   CI：FA4 = 7.5 g 的 NH4Br:NaCl = 3:2 混合物
   方法：25.0 cm3 蒸馏水 + 全部 FA4，测最低温度
   MS：(b)(i) energy = mcΔT = 25 × 4.18 × ΔT
       (b)(ii) n(NH4Br) = energy/(16.8×1000)
       (b)(iii) m = n × 97.9
       (b)(iv) % = m / 质量(FA4)
   ===================================================================== */
/* 7.5 g 混合物里 NH4Br 占 3/5 = 4.5 g → n = 4.5/97.9 = 0.04597 mol
   ΔT = n·ΔH/(m·c) = 0.04597×16800/(25×4.18) = 7.39 K 的**下降** */
add({
  case_id: 'P3-2024-33-Q2-dissolve', mode: 'simulate',
  source: { paper: '9701/33/M/J/24', qp_page: 4, ms_page: 7, ci: 'NH4Br 4.5 g' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 25.0 }],
  actions: [
    { op: 'set_temp', c: 25 },
    { op: 'add_mol', id: 'nh4br', mol: 4.5 / 97.9 },   /* 混合物中的 NH4Br 部分；另一盐假设 ΔH=0 */
    { op: 'settle' }
  ],
  asserts: [
    /* 吸热 → 温度必须**下降**：25 − 7.39 = 17.61 ℃ */
    A('temp', { expect: 17.61, tol: 0.15 }),
    A('temp_le', { limit: 24.0 }),
    A('absent', { species: 'nh4br', max: 1e-6 })
  ]
});
add({
  case_id: 'P3-2024-33-Q2-calc', mode: 'validate',
  source: { paper: '9701/33/M/J/24', ms_page: 7 },
  vessels: [{ id: 'v', kind: 'beaker' }],
  actions: [
    { op: 'compute', name: 'dT',     calc: 'scale', n: 0, factor: 0 },   /* 占位，下面用 numeric 断言 */
    { op: 'compute', name: 'mass',   calc: 'scale', n: 0, factor: 0 }
  ],
  asserts: [
    /* ΔT = 0.04597 × 16800 / (25 × 4.18) = 7.39 K */
    { id: 'DT',  kind: 'numeric', got: (4.5/97.9) * 16800 / (25 * 4.18), expect: 7.39, tol: 0.02 },
    /* energy = mcΔT */
    { id: 'E1',  kind: 'numeric', got: 25 * 4.18 * ((4.5/97.9)*16800/(25*4.18)), expect: 772.3, tol: 1 },
    /* n = energy / 16800 */
    { id: 'N1',  kind: 'numeric', got: 772.3 / 16800, expect: 0.04597, tol: 1e-4 },
    /* m = n × 97.9 */
    { id: 'M1',  kind: 'numeric', got: 0.04597 * 97.9, expect: 4.501, tol: 0.02 },
    /* % = m / 7.5 */
    { id: 'P1',  kind: 'numeric', got: 0.04597 * 97.9 / 7.5 * 100, expect: 60.0, tol: 0.5 }
  ]
});

/* =====================================================================
   9701/33/M/J/25 Q2 —— H2O2 催化分解放热（ΔH = −98.2 kJ/mol）
   CI：FA1 = 9 vol H2O2 ≈ 0.75 mol/dm3
   实验 2：40.0 cm3 FA1 + MnO2，测最大温升
   MS：(b)(i) energy = mcΔT，m = 40
       (b)(ii) n(H2O2) = energy/(98.2×1000)，[H2O2] = n/0.040
   ===================================================================== */
add({
  case_id: 'P3-2025-33-Q2-decompose', mode: 'simulate',
  source: { paper: '9701/33/M/J/25', qp_page: 6, ms_page: 9, ci: 'FA1 = 9 vol H2O2' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 40.0 }],
  actions: [
    { op: 'set_temp', c: 25 },
    { op: 'add_mol', id: 'h2o2', mol: 0.75 * 0.040 },   /* 9 vol ≈ 0.75 M，取 40.0 cm3 */
    { op: 'add_g',  id: 'mno2', g: 0.5 },               /* 催化剂 */
    { op: 'settle' }
  ],
  probes: ['o2'],
  asserts: [
    A('present', { species: 'o2', min: 1e-4 }),
    A('absent',  { species: 'h2o2', max: 1e-5 })
  ]
});
add({
  case_id: 'P3-2025-33-Q2-calc', mode: 'validate',
  source: { paper: '9701/33/M/J/25', ms_page: 9 },
  vessels: [{ id: 'v', kind: 'beaker' }],
  actions: [
    { op: 'compute', name: 'n', calc: 'n', c: 0.75, v_cm3: 40.0 }
  ],
  asserts: [
    { id: 'N1',  kind: 'var', name: 'n', expect: 0.0300, tol: 1e-9 },
    /* energy = n × 98.2 kJ = 2946 J */
    { id: 'E1',  kind: 'numeric', got: 0.0300 * 98200, expect: 2946, tol: 5 },
    /* ΔT = E/(m·c)，m = 40 g */
    { id: 'DT',  kind: 'numeric', got: 2946 / (40 * 4.18), expect: 17.6, tol: 0.1 },
    /* 反推：[H2O2] = E/(98.2×1000)/0.040 */
    { id: 'C1',  kind: 'numeric', got: 2946 / 98200 / 0.040, expect: 0.75, tol: 0.01 }
  ]
});

/* =====================================================================
   9701/34/M/J/25 Q2 —— 测温滴定求中和焓
   CI：FB4 = 1.00 M NaOH，FB5 = 2.00 M HCl
   方法：10.0 cm3 FB4 + 水 + 不同体积 FB5，总体积固定 20.0 cm3
   当量点：n(HCl) = n(NaOH) = 0.0100 mol → V(HCl) = 5.00 cm3
   ===================================================================== */
add({
  case_id: 'P3-2025-34-Q2-equivalence', mode: 'simulate',
  source: { paper: '9701/34/M/J/25', qp_page: 2, ms_page: 6, ci: 'FB4=1.00M NaOH, FB5=2.00M HCl' },
  vessels: [{ id: 'v', kind: 'beaker', water_ml: 10.0 }],
  actions: [
    { op: 'set_temp', c: 25 },
    { op: 'add_mol', id: 'naoh', mol: 1.00 * 0.0100 },      /* 10.0 cm3 FB4 */
    /* 当量点的盐酸：5.00 cm3 × 2.00 M = 0.0100 mol */
    { op: 'add_mol', id: 'hcl',  mol: 2.00 * 0.00500 },
    { op: 'settle' }
  ],
  probes: ['OH-'],
  asserts: [
    A('absent', { species: 'OH-', max: 1e-7 }),
    A('absent', { species: 'H+',  max: 2e-3 })
  ]
});
add({
  case_id: 'P3-2025-34-Q2-calc', mode: 'validate',
  source: { paper: '9701/34/M/J/25', ms_page: 6 },
  vessels: [{ id: 'v', kind: 'beaker' }],
  actions: [
    { op: 'compute', name: 'nNaOH', calc: 'n', c: 1.00, v_cm3: 10.0 },
    { op: 'compute', name: 'vHCl',  calc: 'v', n: 0.0, c: 2.00 }
  ],
  asserts: [
    A('var', { name: 'nNaOH', expect: 0.0100, tol: 1e-9 }),
    /* 当量点体积：n/c × 1000 = 5.00 cm3 */
    { id: 'V1', kind: 'numeric', got: 0.0100 / 2.00 * 1000, expect: 5.00, tol: 1e-9 },
    /* 中和焓 57.3 kJ/mol → 能量 573 J，总体积 20.0 g → ΔT = 6.85 K */
    { id: 'E1', kind: 'numeric', got: 0.0100 * 57300, expect: 573, tol: 1 },
    { id: 'DT', kind: 'numeric', got: 573 / (20.0 * 4.18), expect: 6.85, tol: 0.02 }
  ]
});

/* =====================================================================
   9701/33/M/J/24 Q3 —— 定性分析：FA5 = ZnCO3 + KI 混合物
   （两个阳离子 Zn2+/K+，两个阴离子 CO3^2-/I-）
   标准检验：加稀盐酸冒气泡；加 NaOH 白色沉淀且溶于过量；加 AgNO3 黄色沉淀
   ===================================================================== */
const znI = [
  /* 加稀盐酸 → 碳酸盐分解冒 CO2，锌进入溶液 */
  { case_id: 'P3-2024-33-Q3-acid', why: 'CO3^2- + 2H+ -> CO2 + H2O',
    water: 20, acts: [{ op: 'add_g', id: 'znco3', g: 1.0 }, { op: 'add_ml', id: 'hcl', ml: 10 }],
    as: [A('present', { species: 'co2', min: 1e-4 }), A('present', { species: 'Zn2+', min: 1e-4 })] },
  /* 加 NaOH → 白色 Zn(OH)2 */
  { case_id: 'P3-2024-33-Q3-naoh', why: 'Zn2+ + 2OH- -> Zn(OH)2',
    water: 20, acts: [{ op: 'add_g', id: 'znso4', g: 1.0 }, { op: 'add_g', id: 'naoh', g: 0.6 }],
    as: [A('present', { species: 'znoh2', min: 1e-7 }), A('colour', { contains: '白' })] },
  /* 过量 NaOH → 溶解成锌酸盐（区别于其它白色氢氧化物） */
  { case_id: 'P3-2024-33-Q3-naoh-excess', why: 'Zn(OH)2 + 2OH- -> ZnO2^2- + 2H2O',
    water: 20, acts: [{ op: 'add_g', id: 'znso4', g: 1.0 }, { op: 'add_g', id: 'naoh', g: 0.6 },
                      { op: 'add_mol', id: 'naoh', mol: 0.03 }],
    as: [A('absent', { species: 'znoh2', max: 1e-6 })] },
  /* 加 AgNO3 → 黄色 AgI（区别于白色 AgCl、淡黄 AgBr） */
  { case_id: 'P3-2024-33-Q3-agno3', why: 'Ag+ + I- -> AgI',
    water: 20, acts: [{ op: 'add_g', id: 'ki', g: 0.5 }, { op: 'add_g', id: 'agno3', g: 1.0 }],
    as: [A('present', { species: 'agi', min: 1e-7 })] },
  /* 白色沉淀不能自动等同某一种阳离子 */
  { case_id: 'P3-2024-33-Q3-white-not-unique', why: '白色沉淀的去歧',
    water: 20, acts: [{ op: 'add_g', id: 'znso4', g: 1.0 }, { op: 'add_g', id: 'naoh', g: 0.6 }],
    as: [A('absent', { species: 'aloh3', max: 1e-9 }), A('absent', { species: 'mgoh2', max: 1e-9 }),
         A('absent', { species: 'caco3', max: 1e-9 })] }
];
for (const t of znI) {
  add({
    case_id: t.case_id, mode: 'simulate',
    source: { paper: '9701/33/M/J/24', qp_page: 6, ms_page: 8, rationale: t.why },
    vessels: [{ id: 'v', kind: 'tube', water_ml: t.water }],
    actions: t.acts.concat([{ op: 'settle' }]),
    asserts: t.as
  });
}

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length);
const g = {};
for (const c of cases) { const p = c.case_id.split('-')[0]; g[p] = (g[p] || 0) + 1; }
console.log(g);
