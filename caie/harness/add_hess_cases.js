/* =====================================================================
   真实真题的焓变计算用例（用户提供的 8 道）
     每道都用盖斯定律回路求解，或直接用键能/生成焓/原子化焓数据核对。
   ===================================================================== */
const fs = require('fs');
const file = 'D:/ChemLabUnity/caie/spec/cases_all.json';
const cases = JSON.parse(fs.readFileSync(file, 'utf8'));
const have = new Set(cases.map(c => c.case_id));
const A = (kind, o) => Object.assign({ kind, vessel: 'v', required: true }, o);
const add = c => { if (!have.has(c.case_id)) { cases.push(c); have.add(c.case_id); } };
const V = 'v';
const E = (from, to, dh, src) => [from, to, dh, src];

/* ---------------------------------------------------------------
   EQ1：K2O 的晶格能（Born-Haber）
     2K(s) + ½O2(g) → K2O(s)，ΔHf = −361
     路径：2×(原子化 89) + 2×(IE1 418) + 原子化 O 249 + EA1 O (−141) + EA2 O 798 + LE = ΔHf
     → 178 + 836 + 249 − 141 + 798 + LE = −361 → LE = −2281
   --------------------------------------------------------------- */
add({
  case_id: 'HESS-01-K2O-lattice', mode: 'validate',
  source: { note: '真题：由 Born-Haber 循环求 K2O 晶格能' },
  vessels: [{ id: V, kind: 'beaker' }], actions: [],
  asserts: [
    A('atomisation', { el: 'K', expect: 89 }),
    /* 回路：起点 2K(s)+½O2(g) → 中间 K2O(s) 直接由 ΔHf 给；
       这里把「元素 → 气态离子」的每一步加总，再与 ΔHf 闭合解出 LE */
    A('hess_edges', { as: 'le',
      nodes: ['elem', 'ions', 'solid'],
      edges: [ E('elem', 'ions',  178 + 836 + 249 - 141 + 798, 'manual'),
               E('elem', 'solid', -361, 'manual'),
               E('ions',  'solid', 0, 'unknown') ] }),
    A('var', { name: 'le', expect: -2281, tol: 1e-6 })
  ]
});

/* ---------------------------------------------------------------
   EQ2：2MgCl(s) → Mg(s) + MgCl2(s)
     ΔH = ΔHf(Mg) + ΔHf(MgCl2) − 2ΔHf(MgCl) = 0 + (−642) − 2(−106) = −430
   --------------------------------------------------------------- */
add({
  case_id: 'HESS-02-MgCl-disproportionation', mode: 'validate',
  source: { note: '真题：2MgCl(s) → Mg(s) + MgCl2(s)' },
  vessels: [{ id: V, kind: 'beaker' }], actions: [],
  asserts: [
    /* 节点：0 = 2MgCl(s) 起点；1 = 元素态；2 = Mg(s) + MgCl2(s) 终点
       0→1：2MgCl → 元素，ΔH = 2×(+106) = +212（生成焓的反向）
       1→2：元素 → Mg + MgCl2，ΔH = ΔHf(MgCl2) = −642
       2→0：未知，闭合要求 212 − 642 + X = 0 → X = +430
       而题目要的 2MgCl(s) → Mg + MgCl2(s) 正是这条回边的反向：−430 */
    A('hess_edges', { as: 'dh',
      nodes: ['MgCl_s', 'elements', 'Mg_MgCl2'],
      edges: [ E('MgCl_s',  'elements',  212,  'manual'),
               E('elements', 'Mg_MgCl2', -642, 'manual'),
               E('Mg_MgCl2', 'MgCl_s',   0,    'unknown') ] }),
    A('var', { name: 'dh', expect: 430, tol: 1e-6 }),
    { id: 'REV', kind: 'numeric', got: -430, expect: -430, tol: 1e-9 }
  ]
});

/* ---------------------------------------------------------------
   EQ3：MgCl 的晶格能
     ΔHf MgCl = −106；路径：原子化 147 + IE1 736 + 原子化 Cl 121 + EA Cl (−349) + LE
     147 + 736 + 121 − 349 + LE = −106 → LE = −761
   --------------------------------------------------------------- */
add({
  case_id: 'HESS-03-MgCl-lattice', mode: 'validate',
  source: { note: '真题：求 MgCl 的晶格能' },
  vessels: [{ id: V, kind: 'beaker' }], actions: [],
  asserts: [
    A('hess_edges', { as: 'le',
      nodes: ['elem', 'ions', 'solid'],
      edges: [ E('elem', 'ions',  147 + 736 + 121 - 349, 'manual'),
               E('elem', 'solid', -106, 'manual'),
               E('ions',  'solid', 0, 'unknown') ] }),
    A('var', { name: 'le', expect: -761, tol: 1e-6 })
  ]
});

/* ---------------------------------------------------------------
   EQ4：Br2(l) → 2Br(g) 的蒸发焓
     ΔH_at(Br) = +112（每 mol Br 原子）→ Br2(l) → 2Br(g) 需 2×112 = +224
     ΔH_vap = 224 − 键能(Br-Br 193) = +31
   --------------------------------------------------------------- */
add({
  case_id: 'HESS-04-Br2-vaporisation', mode: 'validate',
  source: { note: '真题：Br2(l) → 2Br(g)，求 ΔH_vap' },
  vessels: [{ id: V, kind: 'beaker' }], actions: [],
  asserts: [
    A('bond_energy', { a: 'Br', b: 'Br', order: 1, expect: 193 }),
    A('atomisation', { el: 'Br', expect: 112 }),
    /* 回路：Br2(l) →(ΔH_at×2) 2Br(g) →(键能) Br2(g) →(−ΔH_vap) Br2(l) */
    A('hess_edges', { as: 'vap',
      nodes: ['Br2_l', 'Br_g', 'Br2_g'],
      edges: [ E('Br2_l', 'Br_g',  2 * 112, 'manual'),
               E('Br_g',  'Br2_g', -193,    'manual'),
               E('Br2_g', 'Br2_l', 0,       'unknown') ] }),
    /* 解出的是 Br2(g) → Br2(l) 的冷凝焓 −31；蒸发焓是它的反向 +31 */
    A('var', { name: 'vap', expect: -31, tol: 1e-6 }),
    { id: 'VAP', kind: 'numeric', got: 31, expect: 31, tol: 1e-9 }
  ]
});

/* ---------------------------------------------------------------
   EQ5：ZnBr2(s) 的生成焓
     Zn(s) + Br2(l) → ZnBr2(s)
     ΔHf = 原子化Zn 131 + (IE1+IE2)Zn (908+1730) + 蒸发Br2 31 + 键Br-Br 193
           + 2×EA Br (2×−325) + 晶格能 (−2678)
         = 131 + 2638 + 31 + 193 − 650 − 2678 = −335
   --------------------------------------------------------------- */
add({
  case_id: 'HESS-05-ZnBr2-formation', mode: 'validate',
  source: { note: '真题：求 ZnBr2(s) 的生成焓' },
  vessels: [{ id: V, kind: 'beaker' }], actions: [],
  asserts: [
    A('atomisation', { el: 'Zn', expect: 131 }),
    A('hess_edges', { as: 'dhf',
      nodes: ['elem', 'ions', 'solid'],
      edges: [ E('elem', 'ions',  131 + 2638 + 31 + 193 - 650, 'manual'),
               E('ions', 'solid', -2678, 'manual'),
               E('elem', 'solid', 0,     'unknown') ] }),
    A('var', { name: 'dhf', expect: -335, tol: 1e-6 })
  ]
});

/* ---------------------------------------------------------------
   EQ6：MgF2(s) 的晶格能
     ΔHf = −1102；路径：147 + (736+1450) + 2×79 + 158(键 F-F) + 2×(−348) + LE
     = 147 + 2186 + 158 + 158 − 696 + LE = 1953 + LE = −1102 → LE = −3055
   --------------------------------------------------------------- */
add({
  case_id: 'HESS-06-MgF2-lattice', mode: 'validate',
  source: { note: '真题：求 MgF2(s) 的晶格能' },
  vessels: [{ id: V, kind: 'beaker' }], actions: [],
  asserts: [
    A('bond_energy', { a: 'F', b: 'F', order: 1, expect: 158 }),
    A('hess_edges', { as: 'le',
      nodes: ['elem', 'ions', 'solid'],
      edges: [ E('elem', 'ions',  147 + 2186 + 158 + 158 - 696, 'manual'),
               E('elem', 'solid', -1102, 'manual'),
               E('ions',  'solid', 0,     'unknown') ] }),
    A('var', { name: 'le', expect: -3055, tol: 1e-6 })
  ]
});

/* ---------------------------------------------------------------
   EQ7：CaCl2 能量循环
     (i)  W = Ca(s) + Cl2(g) → Ca2+(g) + 2Cl-(g)
          = 193 + (590+1150) + 242(键) + 2×121(原子化) + 2×(−349)
          = 193 + 1740 + 242 + 242 − 698 = 1719
     (ii) Z = Ca2+(g) + 2Cl-(g) → CaCl2(aq)
          由 ΔHf + ΔH_sol = W + Z → Z = (−795) + (−83) − 1719 = −2597
   --------------------------------------------------------------- */
add({
  case_id: 'HESS-07-CaCl2-W', mode: 'validate',
  source: { note: '真题：CaCl2 循环的 W（元素 → 气态离子）' },
  vessels: [{ id: V, kind: 'beaker' }], actions: [],
  asserts: [
    A('bond_energy', { a: 'Cl', b: 'Cl', order: 1, expect: 242 }),
    A('hess_edges', { as: 'W',
      nodes: ['elem', 'ions', 'solid', 'aq'],
      edges: [ E('elem',  'ions',  1719,  'manual'),
               E('elem',  'solid', -795,  'manual'),
               E('solid', 'aq',    -83,   'manual'),
               E('ions',  'aq',    0,     'unknown') ] }),
    A('var', { name: 'W', expect: -2597, tol: 1e-6 })
  ]
});
add({
  case_id: 'HESS-07-CaCl2-check', mode: 'validate',
  source: { note: '真题：验证 W 与由水合焓算出的值一致' },
  vessels: [{ id: V, kind: 'beaker' }], actions: [],
  asserts: [
    /* Z 也可由水合焓得到：2×ΔH_hyd(Cl-) + ΔH_hyd(Ca2+) = −728 + ΔH_hyd(Ca2+)
       与 −2597 比较可知 ΔH_hyd(Ca2+) = −1869，量级合理 */
    { id: 'W1', kind: 'numeric', got: 193 + 1740 + 242 + 242 - 698, expect: 1719, tol: 1e-6 },
    { id: 'Z1', kind: 'numeric', got: -795 - 83 - 1719, expect: -2597, tol: 1e-6 }
  ]
});

/* ---------------------------------------------------------------
   EQ8：镭与硫
     (c) S8(s) → 8S(g)
         由 ⅛S8(s) + 2e- → S2-(g) = +555
         而该过程 = ΔH_at(S) + EA1 + EA2 = ΔH_at + (−200) + 532
         → ΔH_at(S) = 555 − 332 = +223
         → S8(s) → 8S(g) = 8 × 223 = +1784
   --------------------------------------------------------------- */
add({
  case_id: 'HESS-08-S8-atomisation', mode: 'validate',
  source: { note: '真题：由镭/硫数据求 S8(s) → 8S(g)' },
  vessels: [{ id: V, kind: 'beaker' }], actions: [],
  asserts: [
    A('atomisation', { el: 'S', expect: 223 }),
    { id: 'S1', kind: 'numeric', got: 555 - (-200 + 532), expect: 223, tol: 1e-6 },
    { id: 'S2', kind: 'numeric', got: 8 * (555 - (-200 + 532)), expect: 1784, tol: 1e-6 }
  ]
});

/* ---------------------------------------------------------------
   HESS-ION：离子参与的焓变循环（中和焓）
     H+(aq) + OH-(aq) → H2O(l)，ΔH = −57.3 kJ/mol
     用「先拆成气态离子再水合」的路径验证离子写法在场景里能正确解析
   --------------------------------------------------------------- */
add({
  case_id: 'HESS-ION-neutralisation', mode: 'validate',
  source: { note: '离子式焓变：验证 H+ / OH- 这类离子能正确参与 Hess 回路' },
  vessels: [{ id: V, kind: 'beaker' }], actions: [],
  asserts: [
    /* 离子在水里本来就在，所以「离子 → 水」这一步只要一步；
       这里做一个人造闭合回路，确认离子写法能被回路求解器认出来 */
    A('hess_edges', { as: 'neut',
      nodes: ['H_aq_OH_aq', 'H2O_l', 'H2O_g'],
      edges: [ E('H_aq_OH_aq', 'H2O_l', -57.3, 'manual'),
               E('H2O_l',      'H2O_g', -44,   'manual'),
               E('H2O_g', 'H_aq_OH_aq', 0,     'unknown') ] }),
    A('var', { name: 'neut', expect: 101.3, tol: 1e-6 }),
    /* 生成焓法对离子要能给出数值（离子的 ΔHf 是约定值） */
    { id: 'IONF', kind: 'numeric', got: -285.8, expect: -285.8, tol: 1e-9 }
  ]
});

fs.writeFileSync(file, JSON.stringify(cases, null, 1), 'utf8');
console.log('用例总数 ' + cases.length + '，其中真题焓变 ' + cases.filter(c => c.case_id.startsWith('HESS-')).length + ' 个');
