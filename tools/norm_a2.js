/* =====================================================================
   A2 反应解析：Unicode → 引擎写法
     两处关键判据：
     1) 电荷要不要写 ^ —— 和引擎里保持一致：
          Fe2+ / H+ / OH-   （去掉数字后是单个元素符号 → 数字是电荷，不写 ^）
          SO4^2- / CO3^2-   （不是单个元素 → 数字是下标，电荷写 ^）
     2) 文档里的 RCOCl / alkene / alcohol 这类是**官能团家族**，
        不是具体物种。引擎的有机反应本来就是按家族走的，所以映射到家族名。
   ===================================================================== */
const fs = require('fs');

const SUB = { '₀':'0','₁':'1','₂':'2','₃':'3','₄':'4','₅':'5','₆':'6','₇':'7','₈':'8','₉':'9' };
const SUP = { '⁰':'0','¹':'1','²':'2','³':'3','⁴':'4','⁵':'5','⁶':'6','⁷':'7','⁸':'8','⁹':'9',
              '⁻':'-','⁺':'+' };

/* 文档里的官能团写法 → 引擎家族名 */
const FAMILY = {
  'alkane':'alkane', 'alkene':'alkene', 'halogenoalkane':'halogenoalkane', 'RX':'halogenoalkane',
  'alcohol':'primary_alcohol', 'ROH':'primary_alcohol', 'primaryalcohol':'primary_alcohol',
  'secondaryalcohol':'secondary_alcohol', 'tertiaryalcohol':'tertiary_alcohol',
  'aldehyde':'aldehyde', 'ketone':'ketone', 'aldehydeorketone':'carbonyl',
  'carboxylicacid':'carboxylic_acid', 'RCOOH':'carboxylic_acid',
  'carboxylate':'carboxylate', 'carboxylatesalt':'carboxylate',
  'ester':'ester', 'RCOCl':'acyl_chloride', 'acylchloride':'acyl_chloride',
  'amide':'amide', 'RCN':'nitrile', 'nitrile':'nitrile',
  'amine':'primary_amine', 'RNH2':'primary_amine', "R'NH2":'secondary_amine',
  'RCH2NH2':'primary_amine', 'phenylamine':'phenylamine', '–NH2':'primary_amine',
  'phenol':'phenol', 'C6H5OH':'phenol', 'benzene':'benzene', 'methylbenzene':'methylbenzene',
  'nitrobenzene':'nitrobenzene', 'chlorobenzene':'chlorobenzene',
  'benzenediazoniumsalt':'benzenediazonium',
  'polyester':'polyester', 'polyamide':'polyamide',
  'diol':'diol', 'dicarboxylicacid':'dicarboxylic_acid', 'dioylchloride':'dioyl_chloride',
  'diamine':'diamine', 'aminoacid/zwitterion':'amino_acid', 'aminoacid':'amino_acid',
  'peptide':'peptide',
  'Cl2orBr2':'halogen', 'HX':'HX',
};

function norm(s) {
  if (!s) return '';
  let t = '', sup = '';
  for (const ch of s) {
    if (SUB[ch] !== undefined) { t += SUB[ch]; continue; }
    if (SUP[ch] !== undefined) { sup += SUP[ch]; continue; }
    if (sup) { t += '{%' + sup + '%}'; sup = ''; }
    t += ch;
  }
  if (sup) t += '{%' + sup + '%}';
  t = t.replace(/\((s|l|g|aq)\)/g, '').replace(/\s+/g, '');
  /* 处理 {%电荷%}：数字是电荷还是下标 */
  t = t.replace(/([A-Za-z\)\]]+)\{%(\d*)([+-])%\}/g, (m, stem, digs, sign) => {
    const single = /^[A-Z][a-z]?$/.test(stem);
    if (digs.length === 0) return stem + sign;                 // H+ / OH-
    return single ? stem + digs + sign : stem + digs + '^' + digs + sign;  // Fe2+ / SO4^2-
  });
  t = t.replace(/\{%([^%]*?)%\}/g, '$1');
  return t;
}

const a2 = JSON.parse(fs.readFileSync('D:/ChemLabUnity/caie/a2_reactions.json', 'utf8'));

const dir = 'D:/ChemLab/js/';
const src = ['data.js','data2.js','cie9701.js'].map(f => fs.readFileSync(dir + f, 'utf8')).join('\n');
const fn = new Function(src + '; return { SUBSTANCES, IONS };');
const { SUBSTANCES, IONS } = fn();
/* 引擎里分子是小写（h2o/co2/nh3），离子是大写（H+/SO4^2-）。
   查表要大小写不敏感，否则 30 次 H2O 全被当成「缺失」。 */
const ids = new Set();
const idx = new Map();
const key = x => String(x).toLowerCase().replace(/\s+/g, '');
for (const s of SUBSTANCES) { ids.add(s[0]); if (!idx.has(key(s[0]))) idx.set(key(s[0]), s[0]); if (!idx.has(key(s[2]))) idx.set(key(s[2]), s[0]); }
for (const s of (IONS || [])) { ids.add(s[0]); if (!idx.has(key(s[0]))) idx.set(key(s[0]), s[0]); }
function resolve(x) {
  if (ids.has(x)) return x;
  const k = key(x);
  if (idx.has(k)) return idx.get(k);
  /* SO42- 与 SO4^2- 两种写法互认 */
  const noCaret = k.replace(/\^/g, '');
  if (idx.has(noCaret)) return idx.get(noCaret);
  for (const [kk, vv] of idx) if (kk.replace(/\^/g, '') === noCaret) return vv;
  return null;
}

function terms(side) {
  return side.split('+').map(x => x.trim()).filter(Boolean).map(tok => {
    let t = norm(tok);
    let coef = 1;
    const m = t.match(/^(\d+)(?=[A-Z(\[])/);
    if (m) { coef = parseInt(m[1], 10); t = t.slice(m[1].length); }
    return { coef, id: t };
  });
}

const missing = new Map(), famUsed = new Map();
let ok = 0;
for (const r of a2) {
  const L = terms(r.reactants), R = terms(r.products);
  if (!L.length || !R.length) continue;
  ok++;
  for (const tk of L.concat(R)) {
    const fam = FAMILY[tk.id] || FAMILY[tk.id.toLowerCase()];
    if (fam) { famUsed.set(fam, (famUsed.get(fam) || 0) + 1); continue; }
    if (resolve(tk.id)) continue;
    missing.set(tk.id, (missing.get(tk.id) || 0) + 1);
  }
}
console.log('可解析:', ok, '/', a2.length);
console.log('映射到家族:', famUsed.size, '种');
console.log('仍缺失的具体物种:', missing.size, '种');
console.log('');
[...missing.entries()].sort((a, b) => b[1] - a[1]).forEach(([k, v]) => console.log(`  ${String(v).padStart(2)}×  ${k}`));
