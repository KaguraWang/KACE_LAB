/* =====================================================================
   安全版：只导出「每一项都能解析成真实物种」的 A2 规则
     上一版把家族名当物种写进了方程式，产生了 phenol + naoh -> c + h2o 这种垃圾。
     这里严格要求：两边的每一项都必须在物种表里真实存在，且不含家族名。
   ===================================================================== */
const fs = require('fs');
const data = JSON.parse(fs.readFileSync('D:/ChemLabUnity/caie/a2_rules_final.json', 'utf8'));

const dir = 'D:/ChemLab/js/';
const src = ['data.js','data2.js','cie9701.js'].map(f => fs.readFileSync(dir + f, 'utf8')).join('\n');
const { SUBSTANCES, IONS } = new Function(src + '; return { SUBSTANCES, IONS };')();
const ids = new Set();
for (const s of SUBSTANCES) ids.add(s[0]);
for (const s of (IONS || [])) ids.add(s[0]);

/* 家族名清单（这些不是物种，不能直接写进方程式） */
const FAM_NAMES = new Set(['alkane','alkene','halogenoalkane','primary_alcohol','secondary_alcohol',
 'tertiary_alcohol','aldehyde','ketone','carbonyl','carboxylic_acid','carboxylate','ester','phenyl_ester',
 'acyl_chloride','amide','secondary_amide','tertiary_amide','nitrile','primary_amine','secondary_amine',
 'phenylamine','amine','phenol','benzene','arene','halogenoarene','methylbenzene','polyester','polyamide',
 'peptide','diol','dicarboxylic_acid','dioyl_chloride','diamine','hydroxy_acid','amino_acid',
 'amino_acid_cation','zwitterion','hydrazone','azonium','azo_compound','hydroxy_azo','dibromoalkane',
 'vicinaldiol','hydroxynitrile','dnph_hydrazone','rona','rch2oh','organic','salt','nucleophile','none',
 'benzoic_acid','halogen','HX','halide','phenoxide','agx','nitrophenol']);

function condOf(e) {
  const t = ((e.temperature||'') + ' ' + (e.medium||'') + ' ' + (e.reagent||'') + ' ' + (e.notes||'')).toLowerCase();
  if (/strong heat|strong heating|high temperature|ignite|burn|complete combustion/.test(t)) return 'hot';
  if (/heat|warm|reflux|boil/.test(t)) return 'heat';
  if (/uv|light|sunlight/.test(t)) return 'light';
  if (/concentrated|conc\.|fuming/.test(t)) return 'conc';
  if (/acidified/.test(t)) return 'acid';
  if (/catalyst|catalys/.test(t)) return 'catal';
  return '';
}
function shortObs(s) {
  if (!s) return '';
  let t = s.replace(/\*\*/g, '').replace(/\s+/g, ' ').trim();
  t = t.split(/(?<=[.;])\s/)[0].replace(/[;.]$/, '');
  if (t.length > 80) t = t.slice(0, 78) + '…';
  return t;
}

const lines = [], kept = [], dropped = [];
for (const r of data) {
  const all = r.L.concat(r.R);
  const bad = all.filter(x => !ids.has(x.id) || FAM_NAMES.has(x.id));
  if (bad.length) { dropped.push({ id: r.entry.id, why: bad.map(x => x.id).join(',') }); continue; }
  const side = arr => arr.map(x => (x.coef && x.coef !== 1 ? x.coef : '') + x.id).join(' + ');
  const eq = side(r.L) + ' -> ' + side(r.R);
  const e = r.entry;
  const cond = condOf(e);
  const obs = shortObs(e.observation).replace(/'/g, "\\'");
  const fx = [];
  if (cond) fx.push(`cond:'${cond}'`);
  const ol = (e.observation || '').toLowerCase();
  if (/gas|bubble|effervesc|fizz/.test(ol)) fx.push('bubble: 2');
  fx.push("a2:'" + e.id + "'");
  lines.push(`R('${eq}', '${obs}', { ${fx.join(', ')} }),`);
  kept.push(e.id);
}

const header = [
'',
'/* =====================================================================',
'   CIE 9701 A2 反应数据库（Topics 23–37）',
'   来源：CIE_9701_A2_Reactions_Conditions_Observations.md',
'   由 tools/emit_a2_safe.js 自动生成 —— 只收录两边每一项都是真实物种的条目，',
'   涉及官能团家族的那些反应由引擎已有的有机家族系统处理。请勿手改。',
'   ===================================================================== */',
''];

const R = 'D:/ChemLab/js/rules.js';
let rc = fs.readFileSync(R, 'utf8');
const cut = rc.indexOf('CIE 9701 A2 反应数据库');
if (cut >= 0) { const head = rc.lastIndexOf('/* ====', cut); rc = rc.slice(0, head >= 0 ? head : cut); }
fs.writeFileSync(R, rc.replace(/\s*$/, '\n') + header.join('\n') + lines.join('\n') + '\n', 'utf8');

console.log('收录规则:', kept.length, '条');
console.log('跳过（含家族名或未解析）:', dropped.length, '条');
console.log('');
console.log('收录的条目：');
console.log('  ' + kept.join(', '));
