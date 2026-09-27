/* =====================================================================
   最终生成 A2 规则
     1) 剥掉产品字段里的叙述后缀（"Al(OH)3, then soluble aluminate..." → "Al(OH)3"）
     2) 通用式 MCO3/MO 展开成第 2 族具体金属
     3) 补最后几个盐
     4) 生成 R(...) 规则写进 rules.js
   ===================================================================== */
const fs = require('fs');
const SUB = {'₀':'0','₁':'1','₂':'2','₃':'3','₄':'4','₅':'5','₆':'6','₇':'7','₈':'8','₉':'9'};
const SUP = {'⁰':'0','¹':'1','²':'2','³':'3','⁴':'4','⁵':'5','⁶':'6','⁷':'7','⁸':'8','⁹':'9','⁻':'-','⁺':'+'};

const FAMILY = {
  'alkane':'alkane','alkene':'alkene','halogenoalkane':'halogenoalkane','RX':'halogenoalkane',
  'alcohol':'primary_alcohol','ROH':'primary_alcohol','primaryalcohol':'primary_alcohol',
  "R'OH":'primary_alcohol','secondaryalcohol':'secondary_alcohol',
  'aldehyde':'aldehyde','ketone':'ketone','aldehydeorketone':'carbonyl',
  'carboxylicacid':'carboxylic_acid','RCOOH':'carboxylic_acid','carboxylate':'carboxylate',
  'carboxylatesalt':'carboxylate','ester':'ester',"RCOOR'":'ester','phenylester':'phenyl_ester',
  'RCOCl':'acyl_chloride','acylchloride':'acyl_chloride',
  'amide':'amide','RCONH2':'amide',"RCONHR'":'secondary_amide',"RCONR'2":'tertiary_amide',
  'RCN':'nitrile','nitrile':'nitrile','amine':'primary_amine','RNH2':'primary_amine',
  "R'NH2":'secondary_amine','RCH2NH2':'primary_amine',"R'2NH":'secondary_amine',
  'secondaryamine':'secondary_amine','phenylamine':'phenylamine','–NH2':'primary_amine',
  'NH3oramine':'amine','phenol':'phenol','C6H5OH':'phenol','benzene':'benzene',
  'methylbenzene':'methylbenzene','nitrobenzene':'nitrobenzene','chlorobenzene':'chlorobenzene',
  'bromobenzene':'bromobenzene','arene':'arene','halogenoarene':'halogenoarene',
  '(chloromethyl)benzene':'(chloromethyl)benzene','phenylethanone(acetophenone)':'phenylethanone',
  'cyclohexane':'cyclohexane','benzenediazoniumsalt':'benzenediazonium','azocompound':'azo_compound',
  'hydroxy-azocompound':'hydroxy_azo','sulfate':'so4^2-','sulfur':'s',
  'polyester':'polyester','polyamide':'polyamide','polyamide/polypeptide':'polyamide',
  'dipeptide/tripeptide':'peptide','peptide':'peptide','diol':'diol',
  'dicarboxylicacid':'dicarboxylic_acid','dioylchloride':'dioyl_chloride','diamine':'diamine',
  'HO–R–COOH':'hydroxy_acid','aminoacid/zwitterion':'amino_acid','aminoacid':'amino_acid',
  'H3N+–CHR–COOH':'amino_acid_cation','H2N–CHR–COO-':'zwitterion',
  'Cl2orBr2':'halogen','Cl2/Br2':'halogen','HX':'HX','X-':'halide','AgX':'agx','salt':'salt',
  'benzoicacid':'benzoic_acid','benzoicacid(orsubstitutedbenzoicacid)':'benzoic_acid',
  'CH3CO–':'ch3co-','RONa':'rona','RCH2OH':'rch2oh','RCOO-':'carboxylate',
  'hydroxynitrile':'hydroxynitrile','dibromoalkane':'dibromoalkane','vicinaldiol':'vicinaldiol',
  '2,4-DNPhydrazone':'dnph_hydrazone','iodine–starchcomplex':'starch',
  'carboxylate/oxidisedproducts':'carboxylate','oxidisableorganiccompound':'organic',
  'oxidisedorganicproduct':'organic','acid–baseinteraction':'salt',
  'enhancedcombustion':'co2','primaryalcoholfromaldehyde/secondaryalcoholfromketone':'primary_alcohol',
  'carbonylcompounds/carboxylicacids/CO2dependingonsubstitution':'carboxylate',
};

/* 产品字段里的叙述后缀：第一个逗号/分号之后基本都是解释，不是化学式 */
function stripProse(s) {
  if (!s) return s;
  let t = s;
  /* "A, then B" / "A; B" / "A, with B" 这类 */
  t = t.split(/\s*,\s*then\s+/i)[0];
  t = t.split(/\s*;\s*/)[0];
  t = t.split(/\s*,\s*(with|and|giving|which|or|followed|slight|mixture|oxidised|after|before|insoluble|soluble|both|all|either)/i)[0];
  return t.trim();
}

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

function side2terms(side, expandGeneric) {
  const out = [];
  for (const tok of side.split('+').map(x => x.trim()).filter(Boolean)) {
    let t = norm(tok);
    let coef = 1;
    const m = t.match(/^(\d+)(?=[A-Z(\[])/);
    if (m) { coef = parseInt(m[1], 10); t = t.slice(m[1].length); }
    if (expandGeneric && (t === 'MCO3' || t === 'MO')) {
      const pairs = t === 'MCO3' ? [['mgco3','mgo'],['caco3','cao'],['srco3','sro'],['baco3','bao']] : null;
      if (pairs) { out.push({ generic: 'MCO3', coef, options: pairs }); continue; }
      out.push({ generic: t, coef }); continue;
    }
    const fam = FAMILY[t] || FAMILY[t.toLowerCase()];
    if (fam) { out.push({ kind: 'family', id: fam, coef, raw: t }); continue; }
    const id = resolve(t);
    if (id) { out.push({ kind: 'species', id, coef, raw: t }); continue; }
    out.push({ kind: 'missing', id: t, coef, raw: t });
  }
  return out;
}

const cie = fs.readFileSync(dir + 'cie9701.js', 'utf8');
const famMatch = cie.match(/const CIE_FAMILY\s*=\s*\{([\s\S]*?)\n\};/);
const knownFams = new Set();
if (famMatch) for (const m of famMatch[1].matchAll(/'([a-z_()0-9\[\]+\-]+)'\s*:/g)) knownFams.add(m[1]);

const a2 = JSON.parse(fs.readFileSync('D:/ChemLabUnity/caie/a2_reactions.json', 'utf8'));
const rules = [], skipped = [];

for (const r of a2) {
  const prod = stripProse(r.products);
  const L = side2terms(r.reactants, false);
  const R = side2terms(prod, true);
  const miss = L.concat(R).filter(x => x.kind === 'missing');
  const gen = R.filter(x => x.generic);
  if (miss.length || gen.length) { skipped.push({ id: r.id, miss: miss.map(x=>x.raw), gen: gen.map(x=>x.generic), prod }); continue; }
  rules.push({ entry: r, L, R });
}

console.log('可生成规则:', rules.length, '/', a2.length);
console.log('跳过:', skipped.length);
fs.writeFileSync('D:/ChemLabUnity/caie/a2_generated.json', JSON.stringify({ rules, skipped }, null, 1), 'utf8');
console.log('');
console.log('=== 仍跳过的 ===');
for (const s of skipped.slice(0, 40))
  console.log(`  ${s.id.padEnd(18)} 缺:${(s.miss.join(',') || '-').padEnd(28)} 通用:${s.gen.join(',') || '-'}`);
