/* =====================================================================
   从 A2 反应数据库生成引擎规则
     · 能映射成物种/家族的 → 生成 R(...) 规则
     · 映射不了的 → 列出来，便于补物种或补家族映射
   ===================================================================== */
const fs = require('fs');

const SUB = {'₀':'0','₁':'1','₂':'2','₃':'3','₄':'4','₅':'5','₆':'6','₇':'7','₈':'8','₉':'9'};
const SUP = {'⁰':'0','¹':'1','²':'2','³':'3','⁴':'4','⁵':'5','⁶':'6','⁷':'7','⁸':'8','⁹':'9','⁻':'-','⁺':'+'};

/* 文档写法 → 引擎家族名（有机反应按家族走） */
const FAMILY = {
  'alkane':'alkane','alkene':'alkene','halogenoalkane':'halogenoalkane','RX':'halogenoalkane',
  'alcohol':'primary_alcohol','ROH':'primary_alcohol','primaryalcohol':'primary_alcohol',
  "R'OH":'primary_alcohol','secondaryalcohol':'secondary_alcohol','tertiaryalcohol':'tertiary_alcohol',
  'aldehyde':'aldehyde','ketone':'ketone','aldehydeorketone':'carbonyl',
  'carboxylicacid':'carboxylic_acid','RCOOH':'carboxylic_acid','carboxylate':'carboxylate',
  'carboxylatesalt':'carboxylate','ester':'ester','RCOOR\'':'ester','phenylester':'phenyl_ester',
  'RCOCl':'acyl_chloride','acylchloride':'acyl_chloride',
  'amide':'amide','RCONH2':'amide','RCONHR\'':'secondary_amide','RCONR\'2':'tertiary_amide',
  'RCN':'nitrile','nitrile':'nitrile',
  'amine':'primary_amine','RNH2':'primary_amine',"R'NH2":'secondary_amine','RCH2NH2':'primary_amine',
  'R\'2NH':'secondary_amine','secondaryamine':'secondary_amine','phenylamine':'phenylamine',
  '–NH2':'primary_amine','NH3oramine':'amine',
  'phenol':'phenol','C6H5OH':'phenol','benzene':'benzene','methylbenzene':'methylbenzene',
  'nitrobenzene':'nitrobenzene','chlorobenzene':'chlorobenzene','bromobenzene':'bromobenzene',
  'arene':'arene','halogenoarene':'halogenoarene',
  '(chloromethyl)benzene':'(chloromethyl)benzene',
  'phenylethanone(acetophenone)':'phenylethanone',
  'cyclohexane':'cyclohexane',
  'benzenediazoniumsalt':'benzenediazonium','azocompound':'azo_compound',
  'hydroxy-azocompound':'hydroxy_azo',
  'polyester':'polyester','polyamide':'polyamide','polyamide/polypeptide':'polyamide',
  'dipeptide/tripeptide':'peptide','peptide':'peptide',
  'diol':'diol','dicarboxylicacid':'dicarboxylic_acid','dioylchloride':'dioyl_chloride',
  'diamine':'diamine','HO–R–COOH':'hydroxy_acid',
  'aminoacid/zwitterion':'amino_acid','aminoacid':'amino_acid',
  'H3N+–CHR–COOH':'amino_acid_cation','H2N–CHR–COO-':'zwitterion',
  'Cl2orBr2':'halogen','HX':'HX',
  'benzoicacid':'benzoic_acid','benzoicacid(orsubstitutedbenzoicacid)':'benzoic_acid',
};

/* 这些是叙述性文字，不是化学式 —— 跳过 */
const PROSE = /(essentially|common aqueous|mixture of|salts$|alkylbenzene with|alkylbenzene,|from reduction|neutralised|by-product|ammonium\/|primary amine,)/i;

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
  t = t.replace(/([A-Za-z\)\]]+)\{%(\d*)([+-])%\}/g, (m, stem, digs, sign) => {
    const single = /^[A-Z][a-z]?$/.test(stem);
    if (digs.length === 0) return stem + sign;
    return single ? stem + digs + sign : stem + digs + '^' + digs + sign;
  });
  return t.replace(/\{%([^%]*?)%\}/g, '$1');
}

const dir = 'D:/ChemLab/js/';
const src = ['data.js','data2.js','cie9701.js'].map(f => fs.readFileSync(dir + f, 'utf8')).join('\n');
const { SUBSTANCES, IONS } = new Function(src + '; return { SUBSTANCES, IONS };')();
const ids = new Set(), idx = new Map();
const key = x => String(x).toLowerCase().replace(/\s+/g, '');
for (const s of SUBSTANCES) { ids.add(s[0]); if (!idx.has(key(s[0]))) idx.set(key(s[0]), s[0]); if (!idx.has(key(s[2]))) idx.set(key(s[2]), s[0]); }
for (const s of (IONS || [])) { ids.add(s[0]); if (!idx.has(key(s[0]))) idx.set(key(s[0]), s[0]); }
function resolve(x) {
  if (ids.has(x)) return x;
  const k = key(x);
  if (idx.has(k)) return idx.get(k);
  const nc = k.replace(/\^/g, '');
  for (const [kk, vv] of idx) if (kk.replace(/\^/g, '') === nc) return vv;
  return null;
}

function terms(side) {
  return side.split('+').map(x => x.trim()).filter(Boolean).map(tok => {
    let t = norm(tok);
    let coef = 1;
    const m = t.match(/^(\d+)(?=[A-Z(\[])/);
    if (m) { coef = parseInt(m[1], 10); t = t.slice(m[1].length); }
    return { coef, raw: t };
  }).map(tk => {
    const fam = FAMILY[tk.raw] || FAMILY[tk.raw.toLowerCase()];
    if (fam) return { ...tk, kind: 'family', id: fam };
    const id = resolve(tk.raw);
    if (id) return { ...tk, kind: 'species', id };
    return { ...tk, kind: 'missing', id: tk.raw };
  });
}

/* 引擎内部的家族名（看 CIE_FAMILY 里到底有哪些） */
const cie = fs.readFileSync(dir + 'cie9701.js', 'utf8');
const famMatch = cie.match(/const CIE_FAMILY\s*=\s*\{([\s\S]*?)\n\};/);
const knownFams = new Set();
if (famMatch) for (const m of famMatch[1].matchAll(/'([a-z_()0-9\[\]+\-]+)'\s*:/g)) knownFams.add(m[1]);

const a2 = JSON.parse(fs.readFileSync('D:/ChemLabUnity/caie/a2_reactions.json', 'utf8'));
const ok = [], bad = [], prose = [];
for (const r of a2) {
  if (PROSE.test(r.reactants) || PROSE.test(r.products)) { prose.push(r); continue; }
  const L = terms(r.reactants), R = terms(r.products);
  const miss = L.concat(R).filter(x => x.kind === 'missing');
  const vsFam = L.concat(R).filter(x => x.kind === 'family' && !knownFams.has(x.id));
  if (miss.length === 0) ok.push({ r, L, R, vsFam });
  else bad.push({ r, miss: miss.map(x => x.raw) });
}

console.log('总条目:', a2.length);
console.log('可生成规则:', ok.length);
console.log('因缺物种无法生成:', bad.length);
console.log('叙述性条目（跳过）:', prose.length);
console.log('');
console.log('=== 缺失的具体物种 ===');
const mc = new Map();
for (const b of bad) for (const m of b.miss) mc.set(m, (mc.get(m) || 0) + 1);
[...mc.entries()].sort((a,b)=>b[1]-a[1]).forEach(([k,v])=>console.log(`  ${v}×  ${k}`));
console.log('');
console.log('=== 家族名不在 CIE_FAMILY 里的 ===');
const fc = new Map();
for (const o of ok) for (const f of o.vsFam) fc.set(f.id, (fc.get(f.id)||0)+1);
[...fc.entries()].forEach(([k,v])=>console.log(`  ${v}×  ${k}`));
console.log('');
console.log('=== 引擎已有的家族（前 30）===');
console.log('  ' + [...knownFams].slice(0, 30).join(', '));
fs.writeFileSync('D:/ChemLabUnity/caie/a2_mapped.json', JSON.stringify({
  ok: ok.map(o => ({ id: o.r.id, L: o.L, R: o.R, obs: o.r.observation, temp: o.r.temperature, medium: o.r.medium, priority: o.r.priority, title: o.r.title })),
  bad: bad.map(b => ({ id: b.r.id, miss: b.miss, reactants: b.r.reactants, products: b.r.products })),
  prose: prose.map(p => ({ id: p.id, reactants: p.reactants, products: p.products })),
}, null, 1), 'utf8');
