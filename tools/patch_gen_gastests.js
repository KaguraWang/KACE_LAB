const fs = require('fs');
const p = 'D:/ChemLabUnity/tools/gen_cs.js';
let c = fs.readFileSync(p, 'utf8');

// 1) 生成两张表
const anchor = "/* ---------- 焰色：离子 颜色 ---------- */";
const add = `/* ---------- 气体的酸碱性：气体id 性质(base/acid/neut/bleach) ---------- */
const gasNature = Object.entries(api.GAS_NATURE || {}).map(([k, v]) => row([k, v]));

/* ---------- 气体对木条的作用：气体id 燃着现象 带火星现象 观察描述 ---------- */
const gasSplint = Object.entries(api.GAS_SPLINT || {}).map(([k, v]) =>
  row([k, v.burning || '', v.justOut || '', v.obs || '']));

/* ---------- 焰色：离子 颜色 ---------- */`;
if (!c.includes('const gasNature')) { c = c.replace(anchor, add); }

// 2) 输出两张表
const anchor2 = `/* ---------- 焰色：离子 颜色 ---------- */
public const string TblFlameColors = @"`;
const add2 = `/* ---------- 气体的酸碱性（石蕊检验的依据） ---------- */
public const string TblGasNature = @"
\${gasNature.join('\\n')}
";

/* ---------- 气体对木条的作用（燃烧/复燃/爆鸣） ---------- */
public const string TblGasSplint = @"
\${gasSplint.join('\\n')}
";

/* ---------- 焰色：离子 颜色 ---------- */
public const string TblFlameColors = @"`;
if (!c.includes('TblGasNature')) { c = c.replace(anchor2, add2); }

fs.writeFileSync(p, c, 'utf8');
console.log('生成器已加入两张气体检验表');
console.log('  gasNature:', c.includes('const gasNature'), ' TblGasNature:', c.includes('TblGasNature'));
console.log('  gasSplint:', c.includes('const gasSplint'), ' TblGasSplint:', c.includes('TblGasSplint'));
