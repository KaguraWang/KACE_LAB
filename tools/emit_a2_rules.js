/* =====================================================================
   把生成好的 A2 规则写进 rules.js
     条件映射：文档的 temperature/medium/reagent 文字 → 引擎的 cond 键
     现象描述：直接用文档的 observation
   ===================================================================== */
const fs = require('fs');
const data = JSON.parse(fs.readFileSync('D:/ChemLabUnity/caie/a2_rules_final.json', 'utf8'));

/* 文档的条件文字 → 引擎 cond 键 */
function condOf(e) {
  const t = (e.temperature + ' ' + e.medium + ' ' + e.reagent + ' ' + (e.notes || '')).toLowerCase();
  const cs = [];
  if (/strong heat|strong heating|high temperature|ignite|burn/.test(t)) cs.push('hot');
  else if (/heat|warm|reflux|boil/.test(t)) cs.push('heat');
  if (/catalyst|uv|light|sunlight|Fe2\+|Fe3\+|AlCl3|concentrated sulfuric/.test(t)) cs.push('catal');
  if (/light|uv|sunlight|sunlight/.test(t)) cs.push('light');
  if (/concentrated|conc\.|fuming/.test(t)) cs.push('conc');
  if (/acidified|acid/.test(t)) cs.push('acid');
  if (/alkaline|NaOH|alkali/.test(t)) cs.push('neutral');
  if (/room temperature|RT|dilute|aqueous/.test(t) && cs.length === 0) return '';
  /* 只保留引擎认得的那几个，优先级：最具体的在前 */
  const known = ['hot','heat','catal','light','conc','acid'];
  for (const k of known) if (cs.includes(k)) return k;
  return '';
}

/* 现象的简短化：文档的描述很长，取第一句 */
function shortObs(s) {
  if (!s) return '';
  let t = s.replace(/\s+/g, ' ').trim();
  t = t.split(/(?<=[.;])\s/)[0];
  if (t.length > 90) t = t.slice(0, 88) + '…';
  return t;
}

/* 只导出「物种当量」形式的规则：家族名不能在 R() 里直接用，
   所以家族组成的反应交给已有的有机家族系统，这里只导出纯物种规则 */
const speciesRules = [], familyRules = [];
for (const r of data) {
  const all = r.L.concat(r.R);
  (all.some(x => /^[a-z_()\[\]0-9]+$/.test(x.id) && !x.id.match(/^[a-z]+\d/)) && all.every(x => /^[a-z]/.test(x.id) || /^[A-Z]/.test(x.id))
    ? speciesRules : speciesRules).push(r);
}

const lines = [];
lines.push('');
lines.push('/* =====================================================================');
lines.push('   CIE 9701 A2 反应数据库（Topics 23–37）');
lines.push('   来源：CIE_9701_A2_Reactions_Conditions_Observations.md');
lines.push('   由 tools/gen_a2_emit.js 从该 markdown 自动生成，请勿手改');
lines.push('   ===================================================================== */');
lines.push('');

let emitted = 0;
for (const r of data) {
  const e = r.entry;
  const side = arr => arr.map(x => (x.coef && x.coef !== 1 ? x.coef : '') + x.id).filter(Boolean).join(' + ');
  const eq = side(r.L) + ' -> ' + side(r.R);
  /* 家族名（下划线形式）不能写进 R()，跳过 */
  if (/\b(primary_alcohol|secondary_alcohol|tertiary_alcohol|halogenoalkane|carboxylic_acid|acyl_chloride|primary_amine|secondary_amine|arene|halogenoarene|polyester|polyamide|peptide|diol|diamine|hydroxy_acid|amino_acid|amino_acid_cation|zwitterion|carbonyl|nitrile|amide|secondary_amide|tertiary_amide|ester|phenyl_ester|methylbenzene|azobenzene|azo_compound|hydroxy_azo|dibromoalkane|vicinaldiol|hydroxynitrile|dnph_hydrazone|rona|rch2oh|organic|salt|nucleophile|none|benzoic_acid|cyclohexane|nitrophenol|phenoxide|bromobenzene|chlorobenzene|nitrobenzene|phenylethanone|agx|cui|cu2o|ch3cocl|ch3cl|chcl3|chi3|ch3cho|hcooh|c2o4\^2-|h2c2o4)\b/.test(eq)) continue;
  const cond = condOf(e);
  const obs = shortObs(e.observation);
  const fx = [];
  if (cond) fx.push(`cond:'${cond}'`);
  if (/gas|bubble|effervesc|fizz/.test((e.observation || '').toLowerCase())) { fx.push("bubble: 2"); }
  if (/precipitat|ppt|solid/.test((e.observation || '').toLowerCase())) { fx.push("precip:''"); }
  lines.push(`R('${eq}', '${obs.replace(/'/g, "\\'")}'${fx.length ? ', { ' + fx.join(', ').replace(", precip:''", '') + ' }' : ''}),`);
  emitted++;
}

lines.push('');
const R = 'D:/ChemLab/js/rules.js';
let rc = fs.readFileSync(R, 'utf8');
const marker = '/* =====================================================================';
const marker2 = 'CIE 9701 A2 反应数据库';
const mi = rc.indexOf(marker2);
if (mi >= 0) {
  const head = rc.lastIndexOf(marker, mi);
  rc = rc.slice(0, head >= 0 ? head : mi);
}
fs.writeFileSync(R, rc + lines.join('\n'), 'utf8');
console.log('已写入规则:', emitted, '条');
console.log('跳过的（含家族名，交给有机系统）:', data.length - emitted, '条');
