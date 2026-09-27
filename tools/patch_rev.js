const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 50).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ---- 1) 生成器：规则表加一列 rev（可逆） ---- */
const g = 'D:/ChemLabUnity/tools/gen_cs.js';
P(g, `              f.float ? 1 : 0, f.odor == null ? '' : f.odor]);`,
`              f.float ? 1 : 0, f.odor == null ? '' : f.odor, f.rev ? 1 : 0]);`);
P(g, `/* 方程 现象 cond gas bubble heat smoke flame precip mirror explode tint clearTint deposit float odor */`,
`/* 方程 现象 cond gas bubble heat smoke flame precip mirror explode tint clearTint deposit float odor rev */`);

/* ---- 2) C#：Rule 加 Reversible 字段 ---- */
const r = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemCore.cs';
P(r, 'public class OrgRule { public string From, To, Cond, Reagents, Byproducts, Obs; }',
`public class OrgRule { public string From, To, Cond, Reagents, Byproducts, Obs; }`);

/* ---- 3) 可逆反应规则（真实的、教科书里的） ---- */
const j = 'D:/ChemLab/js/rules.js';
let jc = fs.readFileSync(j, 'utf8');
if (!jc.includes('rev:1')) {
  const anchor = "/* --- 定性分析笔记表补全";
  const add = `/* =====================================================================
   可逆反应（rev:1）—— 中英规范都用双向箭头 ⇌
   来源：工业合成氨、接触法制硫酸、酯化、弱电解质电离
   ===================================================================== */
R('N2 + 3H2 -> 2NH3', '化合生成氨，反应可逆，产率受压强与温度影响', { gas:'nh3', heat: 6, rev: 1, cond:'catal_hot' }),
R('2SO2 + O2 -> 2SO3', '接触室中二氧化硫被氧化，反应可逆', { heat: 5, rev: 1, cond:'catal_hot' }),
R('H2 + I2 -> 2HI', '碘蒸气与氢气化合，反应可逆', { rev: 1, cond:'heat' }),
R('2NO2 -> N2O4', '红棕色变浅，反应可逆', { rev: 1 }),
R('CH3COOH + C2H5OH -> CH3COOC2H5 + H2O', '有果香味的酯生成，反应可逆', { rev: 1, cond:'catal_warm' }),

`;
  if (jc.includes(anchor)) { jc = jc.replace(anchor, add + anchor); fs.writeFileSync(j, jc, 'utf8'); n++; }
  else console.log('  ✗ 可逆规则锚点');
}
console.log('补丁处数:', n);
