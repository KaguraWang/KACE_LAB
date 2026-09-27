const fs = require('fs');
const SUB = {'₀':'0','₁':'1','₂':'2','₃':'3','₄':'4','₅':'5','₆':'6','₇':'7','₈':'8','₉':'9'};
const SUP = {'⁰':'0','¹':'1','²':'2','³':'3','⁴':'4','⁵':'5','⁶':'6','⁷':'7','⁸':'8','⁹':'9','⁻':'-','⁺':'+'};
const FAMILY = JSON.parse(fs.readFileSync('D:/ChemLabUnity/caie/a2_family.json', 'utf8'));
const EXTRA_FAM = {
  'alkylbenzenewithatleastonebenzylicH':'methylbenzene',
  'primaryamine':'primary_amine','ammonium/aminesalt':'rnh3cl',
  'ammonium/alkylammoniumsalt':'rnh3cl','hydroxy/carboxylicmonomersorfragments':'hydroxy_acid',
  'alcohol-containingfragments':'primary_alcohol','carboxylic-acidfragments':'carboxylic_acid',
  'protonatedaminefragments':'rnh3cl','carboxylatefragments':'carboxylate',
  'aminefragments':'primary_amine','salts':'salt','primaryamide':'primaryamide',
  'mixtureof2-nitrophenoland4-nitrophenol':'nitrophenol','commonaqueousnucleophiles':'nucleophile',
  'essentiallynoreaction':'none',
};

function norm(s) {
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
function stripProse(s) {
  if (!s) return '';
  let t = s.split(/\s*,\s*then\s+/i)[0];
  t = t.split(/\s*;\s*/)[0];
  t = t.split(/\s*,\s*(with|and|giving|which|or|followed|slight|mixture|oxidised|after|before|insoluble|soluble|both|all|either|protonated|amine|carboxylate|hydroxy|e\.g\.)/i)[0];
  return t.trim();
}
function firstFormula(t) {
  const m = String(t).match(/^([A-Za-z0-9\(\)\[\]\{\}%^+\-–]+)/);
  return m ? m[1] : t;
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
function lookup(t) {
  const raw = String(t).replace(/[–—]/g, '-');
  return FAMILY[raw] || FAMILY[raw.toLowerCase()] || EXTRA_FAM[raw.toLowerCase().replace(/[^a-z0-9]/g, '')]
      || FAMILY[t] || resolve(t);
}

const GENERIC = {
  'MCO3': [['mgco3','mgo'],['caco3','cao'],['srco3','sro'],['baco3','bao']],
};

function side2terms(side) {
  const out = [];
  for (const tok of stripProse(side).split('+').map(x => x.trim()).filter(Boolean)) {
    let t = norm(tok);
    let coef = 1;
    const m = t.match(/^(\d+)(?=[A-Z(\[])/);
    if (m) { coef = parseInt(m[1], 10); t = t.slice(m[1].length); }
    let id = lookup(t) || lookup(norm(firstFormula(tok)));
    if (!id && GENERIC[t]) { out.push({ generic: t, coef }); continue; }
    if (id) out.push({ id, coef });
    else out.push({ id: null, coef, raw: t });
  }
  return out;
}

const a2 = JSON.parse(fs.readFileSync('D:/ChemLabUnity/caie/a2_reactions.json', 'utf8'));
const rules = [], bad = [];
for (const r of a2) {
  const L = side2terms(r.reactants), R = side2terms(stripProse(r.products));
  if (L.length === 0 || R.length === 0 || L.some(x => x.id === null) || R.some(x => x.id === null)) {
    bad.push({ id: r.id, L: L.filter(x=>!x.id).map(x=>x.raw), R: R.filter(x=>!x.id).map(x=>x.raw) });
    continue;
  }
  rules.push({ entry: r, L, R });
}
console.log('生成规则:', rules.length, '/', a2.length, ' 剩余:', bad.length);
for (const b of bad) console.log(`  ${b.id}: 左[${b.L.join(',')}] 右[${b.R.join(',')}]`);
fs.writeFileSync('D:/ChemLabUnity/caie/a2_rules_final.json', JSON.stringify(rules, null, 1), 'utf8');
