/* =====================================================================
   气体检验响应表的交叉校验
   目的：石蕊变色方向必须和气体的酸碱性一致，不能有遗漏或自相矛盾。
   这是纯数据层面的检查，跑在生成期，任何一条对不上就中断构建。
   ===================================================================== */
const fs = require('fs');

const dir = 'D:/ChemLab/js/';
function load(f) {
  const src = fs.readFileSync(dir + f, 'utf8');
  const fn = new Function(src + '; return { GAS_NATURE, GAS_SPLINT, VOLATILE };');
  return fn();
}
const api = load('gastests.js') || {};
const dataSrc = fs.readFileSync(dir + 'data.js', 'utf8') + fs.readFileSync(dir + 'data2.js', 'utf8') + fs.readFileSync(dir + 'cie9701.js', 'utf8');
const dataFn = new Function(dataSrc + '; return { SUBSTANCES, IONS };');
const { SUBSTANCES, IONS } = dataFn();

/* 离子单独一张表，查表时两张都要算上 */
const ids = new Set(SUBSTANCES.map(s => s[0]));
for (const s of (IONS || [])) ids.add(Array.isArray(s) ? s[0] : s);
const problems = [];
const warns = [];

const NAT = api.GAS_NATURE || {};
const SPL = api.GAS_SPLINT || {};
const VOL = api.VOLATILE || {};

/* ---- 检查 1：每条石蕊响应必须有明确的酸碱性分类 ---- */
for (const [gas, nat] of Object.entries(NAT)) {
  if (!['base', 'acid', 'neut', 'bleach'].includes(nat))
    problems.push(`GAS_NATURE['${gas}'] = '${nat}' 不是合法分类（应 base/acid/neut/bleach）`);
}

/* ---- 检查 2：木条响应表里的每种气体都该有酸碱性登记（否则石蕊检验会按中性处理） ---- */
for (const gas of Object.keys(SPL)) {
  if (!(gas in NAT))
    warns.push(`GAS_SPLINT 里有 '${gas}'，但 GAS_NATURE 没登记 → 石蕊检验会当成中性气体`);
}

/* ---- 检查 3：爆鸣只应出现在可燃气体上，且必须是氢气那种能形成爆炸混合物的 ---- */
const POP_OK = new Set(['h2']);
for (const [gas, v] of Object.entries(SPL)) {
  if ((v.burning === 'pop' || v.justOut === 'pop_weak') && !POP_OK.has(gas))
    problems.push(`'${gas}' 被判为会爆鸣，但它不是氢气 —— 爆鸣是氢气的特征检验，不能乱给`);
}
if (!SPL['h2'] || SPL['h2'].burning !== 'pop')
  problems.push(`氢气必须「点燃的木条伸入发出尖锐爆鸣声」，现在登记的是 '${SPL['h2'] && SPL['h2'].burning}'`);

/* ---- 检查 4：复燃只应出现在助燃气体上 ---- */
const RELIGHT_OK = new Set(['o2', 'n2o']);
for (const [gas, v] of Object.entries(SPL)) {
  if (v.justOut === 'relight' && !RELIGHT_OK.has(gas))
    problems.push(`'${gas}' 被判为能使带火星木条复燃，但它不助燃 —— 复燃是氧气的特征检验`);
}
if (!SPL['o2'] || SPL['o2'].justOut !== 'relight')
  problems.push(`氧气必须「带火星的木条复燃」，现在登记的是 '${SPL['o2'] && SPL['o2'].justOut}'`);

/* ---- 检查 5：不能既说会熄灭又说会燃得更旺 ---- */
for (const [gas, v] of Object.entries(SPL)) {
  const lit = [v.burning, v.justOut];
  if (lit.includes('out') && lit.includes('brighter'))
    problems.push(`'${gas}' 同时登记了「熄灭」和「燃得更旺」，自相矛盾`);
  if (!v.burning || !v.justOut)
    problems.push(`'${gas}' 的燃着/带火星现象没填全`);
}

/* ---- 检查 6：碱性与酸性气体必须有正确的石蕊方向（这是本表存在的意义） ---- */
/* 官方 CIE 表：ammonia 使湿润红色石蕊变蓝；这就隐含 NH3 是碱性 */
const MUST_BE_BASE = ['nh3', 'ch3nh2', 'ch3nhch3', 'c2h5nh2', '(ch3)3n'];
for (const g of MUST_BE_BASE) {
  if (NAT[g] !== 'base')
    problems.push(`'${g}' 是胺/氨，必须登记为 base（遇湿润红色石蕊变蓝），现在是 '${NAT[g]}'`);
}
const MUST_BE_ACID = ['co2', 'so2', 'so3', 'hcl', 'hbr', 'hi', 'h2s', 'no2', 'hf', 'hcn'];
for (const g of MUST_BE_ACID) {
  if (NAT[g] !== 'acid')
    problems.push(`'${g}' 是酸性气体，必须登记为 acid（遇湿润蓝色石蕊变红），现在是 '${NAT[g]}'`);
}
const MUST_BE_NEUT = ['h2', 'o2', 'n2', 'co', 'no', 'ch4', 'c2h4', 'c2h2', 'c2h6', 'he', 'ar'];
for (const g of MUST_BE_NEUT) {
  if (NAT[g] && NAT[g] !== 'neut')
    problems.push(`'${g}' 是中性气体，石蕊不应变色，现在登记为 '${NAT[g]}'`);
}

/* ---- 检查 7：挥发表里的键必须在物种表里真实存在（防止大小写写错导致静默失效） ---- */
for (const key of Object.keys(VOL)) {
  if (!ids.has(key) && !ids.has(key.toLowerCase()) && !ids.has(key.toUpperCase()))
    problems.push(`VOLATILE['${key}'] 在物质表里找不到对应物种（很可能是大小写写错了）`);
  const vap = VOL[key];
  if (!(vap in NAT))
    warns.push(`VOLATILE['${key}'] → '${vap}'，但 '${vap}' 没在 GAS_NATURE 里登记`);
}

/* ---- 检查 8：官方表列的四条气体检验必须全部可判 ---- */
const OFFICIAL = [['nh3', 'base'], ['co2', 'acid'], ['h2', 'pop'], ['o2', 'relight']];
for (const [gas, need] of OFFICIAL) {
  if (need === 'base' && NAT[gas] !== 'base') problems.push(`官方表：${gas} 应使湿润红色石蕊变蓝`);
  if (need === 'acid' && NAT[gas] !== 'acid') problems.push(`官方表：${gas} 应为酸性气体`);
  if (need === 'pop'  && SPL[gas]?.burning !== 'pop') problems.push(`官方表：${gas} 应使点燃木条爆鸣`);
  if (need === 'relight' && SPL[gas]?.justOut !== 'relight') problems.push(`官方表：${gas} 应使带火星木条复燃`);
}

/* ---- 输出 ---- */
console.log('─'.repeat(70));
console.log('气体检验响应表交叉校验');
console.log(`  酸碱性登记 ${Object.keys(NAT).length} 种气体（其中碱性 ${
  Object.values(NAT).filter(v => v === 'base').length}、酸性 ${
  Object.values(NAT).filter(v => v === 'acid').length}、中性 ${
  Object.values(NAT).filter(v => v === 'neut').length}、漂白 ${
  Object.values(NAT).filter(v => v === 'bleach').length}）`);
console.log(`  木条响应登记 ${Object.keys(SPL).length} 种气体`);
console.log(`  易挥发物种 ${Object.keys(VOL).length} 条`);

if (warns.length) {
  console.log(`\n  ⚠ ${warns.length} 条提醒：`);
  for (const w of warns) console.log('     ' + w);
}
if (problems.length) {
  console.log(`\n  ✗ ${problems.length} 条矛盾，必须修正：`);
  for (const p of problems) console.log('     ' + p);
  process.exit(4);
}
console.log('\n  ✓ 全部检查通过：石蕊方向与气体酸碱性一致，爆鸣只给氢气，复燃只给助燃气体');
console.log('─'.repeat(70));

module.exports = { problems, warns };
