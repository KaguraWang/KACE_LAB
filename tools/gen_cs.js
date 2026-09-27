/* =====================================================================
   把 D:\ChemLab 里已验证的 JS 化学数据转成 C#
   数据只有一份真相来源，绝不手工重打
   ===================================================================== */
const fs = require('fs');
const path = require('path');

const SRC = 'D:/ChemLab';
const OUT = 'D:/ChemLabUnity/Assets/Scripts/Chem';


/* 本地化文件单独求值：它坏了也不能拖垮整个构建 */
const i18nPath = path.join(SRC, 'js/i18n_en.js');
const files = ['js/data.js', 'js/data2.js', 'js/cie9701.js', 'js/gastests.js', 'js/rules.js', 'js/a2rules.js', 'js/enthalpy.js', 'js/engine.js'];
const code = files.map(f => fs.readFileSync(path.join(SRC, f), 'utf8')).join('\n');

const api = new Function(code + `
  return { SUBSTANCES, IONS, SOLUBILITY, PRECIPITATES, FLAME_COLORS, GAS_NATURE, GAS_SPLINT, VOLATILE, BOND_ENERGY, FORMATION_H, REACTION_H, ATOMISATION, ELECTRON_AFFINITY,
           PRECIP_RULES, RULES, SPECIAL, DISSOCIATION, MOLARITY, WATER_REACTIVE,
           CIE_FAMILY, CIE_ORGANIC_RULES, CIE_FAMILY_MAP, CIE_DISSOLVE_H, CIE_RULE_H };
`)();

api.I18N_EN = null;
if (fs.existsSync(i18nPath)) {
  try { api.I18N_EN = new Function(fs.readFileSync(i18nPath, 'utf8') + '\nreturn I18N_EN;')(); }
  catch (e) { console.warn('[warn] i18n_en.js 解析失败，按空表处理: ' + e.message); }
}

/* ---------- 转义：TSV 里不能出现制表符和换行 ---------- */
const cell = v => String(v == null ? '' : v)
  .replace(/\t/g, ' ').replace(/\r?\n/g, ' ').replace(/^\s+|\s+$/g, '');
const row = arr => arr.map(cell).join('\t');

function banner(title, note) {
  return `/* =====================================================================
   ${title}
   ${note}
   —— 本文件由 tools/gen_cs.js 从 D:\\ChemLab\\js\\*.js 自动生成，请勿手改
   ===================================================================== */
`;
}

/* ================= 物质 ================= */
/* id 名 式 态 固色 液色 密度 溶解性 类别 备注 */
const substances = api.SUBSTANCES.map(s => row([s[0], s[1], s[2], s[3], s[4], s[5], s[6], s[7], s[8], s[9]]));

/* ================= 离子 ================= */
/* id 电荷 颜色 备注 */
const ions = api.IONS.map(i => row([i[0], i[1], i[2], i[3]]));

/* ================= 电离表 ================= */
/* id | ions:k=v;k=v | weak(0/1) | degree | none(0/1) | moleculeAs */
const diss = Object.entries(api.DISSOCIATION).map(([id, d]) => {
  const ionStr = Object.entries(d.ions || {}).map(([k, v]) => `${k}=${v}`).join(';');
  return row([id, ionStr, d.weak ? 1 : 0, d.degree == null ? '' : d.degree,
              d.none ? 1 : 0, d.moleculeAs || '']);
});

/* ================= 沉淀外观 ================= */
/* 化学式 颜色 描述 溶于酸 溶于氨水 */
const precs = Object.entries(api.PRECIPITATES).map(([f, p]) =>
  row([f, p.color, p.desc, p.acid ? 1 : 0, p.ammonia ? 1 : 0]));

/* ================= 沉淀生成规则 ================= */
/* 阳离子 阴离子 沉淀id 阳系数 阴系数 */
const precipRules = api.PRECIP_RULES.map(r => row(r));

/* ================= 反应规则 ================= */
/* 方程 现象 cond gas bubble heat smoke flame precip mirror explode tint clearTint deposit float odor rev */
const rules = require('./reaction_corrections').correctRules(api.RULES).map(r => {
  const f = r.fx || {};
  return row([r.eq, r.obs, r.cond || '', f.gas || '', f.bubble == null ? '' : f.bubble,
              f.heat == null ? '' : f.heat, f.smoke == null ? '' : f.smoke,
              f.flame || '', f.precip || '', f.mirror ? 1 : 0, f.explode ? 1 : 0,
              f.tint || '', f.clearTint ? 1 : 0, f.deposit || '',
              f.float ? 1 : 0, f.odor == null ? '' : f.odor, f.rev ? 1 : 0]);
});

/* ================= 特殊规则 ================= */
/* 只导出判断逻辑需要的字段；when 函数本身在 JS 里，C# 端手写等价实现 */
const specials = api.SPECIAL.map(s => row([s.id, s.obs,
  s.fx && s.fx.tint ? s.fx.tint : '', s.fx && s.fx.clearTint ? 1 : 0]));

/* ================= 溶解性规则 / 焰色 / 浓度 ================= */
const solub = Object.entries(api.SOLUBILITY).map(([an, r]) =>
  row([an, r.always ? 1 : 0,
       (r.insolubleWith || []).join(';'), (r.slightlyWith || []).join(';'),
       (r.solubleWith || []).join(';'), r.alwaysInsolubleOtherwise ? 1 : 0]));

/* 易挥发物种：溶在水里但上方有蒸气，气体检验能检出 */
const volatiles = Object.entries(api.VOLATILE || {}).map(([k, v]) => row([k, v]));

/* 焓变数据（CIE 数据手册 + 通行值） */
const bondEnergy = (api.BOND_ENERGY || []).map(r => row([r[0], r[1], r[2], r[3]]));
const formationH = Object.entries(api.FORMATION_H || {}).map(([k, v]) => row([k, v]));
const reactionH  = (api.REACTION_H || []).map(r => row([r[0], r[1], r[2]]));
const atomisation = Object.entries(api.ATOMISATION || {}).map(([k, v]) => row([k, v]));
const electronAff = Object.entries(api.ELECTRON_AFFINITY || {}).map(([k, v]) => row([k, v]));

/* 气体的酸碱性：气体id 性质(base/acid/neut/bleach) —— 石蕊检验的依据 */
const gasNature = Object.entries(api.GAS_NATURE || {}).map(([k, v]) => row([k, v]));

/* 气体对木条的作用：气体id 燃着现象 带火星现象 观察描述 */
const gasSplint = Object.entries(api.GAS_SPLINT || {}).map(([k, v]) =>
  row([k, v.burning || '', v.justOut || '', v.obs || '']));

const flames = Object.entries(api.FLAME_COLORS).map(([ion, col]) => row([ion, col]));
const molarity = Object.entries(api.MOLARITY).map(([id, m]) => row([id, m]));
const waterReactive = [...api.WATER_REACTIVE].map(x => row([x]));

/* ---------- 本地化表：译文还没到位时就是空表 ---------- */
const _en = api.I18N_EN || {};
const i18nSub = Object.entries(_en.sub || {}).map(([k, v]) => row([k, v]));
const i18nTxt = Object.entries(_en.text || {}).map(([k, v]) => row([k, v]));
const i18nCls = Object.entries(_en.cls || {}).map(([k, v]) => row([k, v]));
const i18nVes = Object.entries(_en.vessel || {}).map(([k, v]) => row([k, v]));

/* ================= CIE 9701：官能团家族标记 ================= */
/* 物质id  家族 */
const families = Object.entries(api.CIE_FAMILY).map(([id, fam]) => row([id, fam]));
/* 新加的物种自带家族（第 11 列） */
for (const s of api.SUBSTANCES) if (s[10]) families.push(row([s[0], s[10]]));

/* ================= CIE 9701：通用有机规则 ================= */
/* 反应家族 产物家族 条件 可用试剂 副产物 现象 */
const orgRules = api.CIE_ORGANIC_RULES.map(r => row(r));

/* ================= CIE 9701：家族到分子的映射 ================= */
/* 家族 反应物id 产物id */
const orgMap = (api.CIE_FAMILY_MAP || []).map(r => row(r));
const disH  = Object.entries(api.CIE_DISSOLVE_H || {}).map(([k, v]) => row([k, v]));
const ruleH = Object.entries(api.CIE_RULE_H || {}).map(([k, v]) => row([k, v]));
/* ---------- CROSSCHECK：所有被引用的 id 必须真的存在于物质表 ----------
   踩过的坑：焓变表里写了 nh4br，但物质表根本没有这个物种，
   运行时不报错、只是静默什么都不发生。这里显式检查。 */
{
  const ids = new Set(api.SUBSTANCES.map(s => s[0]));
  const missing = [];
  const chk = (label, list) => { for (const id of list) if (id && !ids.has(id)) missing.push(label + ":" + id); };
  chk("dissolveH", Object.keys(api.CIE_DISSOLVE_H || {}));
  chk("family", Object.keys(api.CIE_FAMILY || {}));
  for (const r of (api.CIE_FAMILY_MAP || [])) { chk("orgMap.from", [r[1]]); chk("orgMap.to", [r[3]]); }
  if (missing.length) {
    console.error("[CROSSCHECK] 有 " + missing.length + " 个被引用的 id 不在物质表里：");
    for (const m of missing.slice(0, 40)) console.error("   " + m);
    process.exit(3);
  }
  console.log("  交叉校验通过：所有引用的 id 都存在");
}

/* ---------- 输出 ---------- */
fs.mkdirSync(OUT, { recursive: true });

const parts = [];
parts.push(banner('化学数据（自动生成）', '208 种物质 / 53 个离子 / 电离表 / 溶解度 / 沉淀 / 焰色'));
parts.push(`
using System.Collections.Generic;

namespace ChemLab.Chem {

public static partial class ChemData {

/* ---------- 物质：id 名 式 态 固色 液色 密度 溶解性 类别 备注 ---------- */
public const string TblSubstances = @"
${substances.join('\n')}
";

/* ---------- 离子：id 电荷 颜色 备注 ---------- */
public const string TblIons = @"
${ions.join('\n')}
";

/* ---------- 电离表：id 离子(名=系数;) 弱电解质 电离度 不电离 分子别名 ---------- */
public const string TblDissociation = @"
${diss.join('\n')}
";

/* ---------- 沉淀外观：化学式 颜色 描述 溶于酸 溶于氨水 ---------- */
public const string TblPrecipitates = @"
${precs.join('\n')}
";

/* ---------- 沉淀生成：阳离子 阴离子 沉淀id 阳系数 阴系数 ---------- */
public const string TblPrecipRules = @"
${precipRules.join('\n')}
";

/* ---------- 溶解度通则：阴离子 总是可溶 难溶阳离子 微溶阳离子 可溶阳离子 其余难溶 ---------- */
public const string TblSolubility = @"
${solub.join('\n')}
";


/* ---------- 易挥发物种：物种id 蒸气对应气体id ---------- */
public const string TblVolatile = @"
${volatiles.join('\n')}
";

/* ---------- 键能：原子A 原子B 键级 键能kJ/mol ---------- */
public const string TblBondEnergy = @"
${bondEnergy.join('\n')}
";

/* ---------- 标准生成焓：物种id kJ/mol ---------- */
public const string TblFormationH = @"
${formationH.join('\n')}
";

/* ---------- 原子化焓：元素 kJ/mol ---------- */
public const string TblAtomisation = @"
${atomisation.join('\n')}
";

/* ---------- 电子亲和能：物种 kJ/mol ---------- */
public const string TblElectronAffinity = @"
${electronAff.join('\n')}
";

/* ---------- 反应焓：方程式 kJ/mol 说明 ---------- */
public const string TblReactionH = @"
${reactionH.join('\n')}
";

/* ---------- 气体的酸碱性（石蕊检验的依据） ---------- */
public const string TblGasNature = @"
${gasNature.join('\n')}
";

/* ---------- 气体对木条的作用（燃烧/复燃/爆鸣） ---------- */
public const string TblGasSplint = @"
${gasSplint.join('\n')}
";

/* ---------- 焰色：离子 颜色 ---------- */
public const string TblFlameColors = @"
${flames.join('\n')}
";

/* ---------- 水溶液储备液浓度 mol/L ---------- */
public const string TblMolarity = @"
${molarity.join('\n')}
";

/* ---------- 遇水剧烈反应的物质 ---------- */
public const string TblWaterReactive = @"
${waterReactive.join('\n')}
";

/* ---------- 反应规则：方程 现象 cond gas bubble heat smoke flame precip mirror explode tint clearTint deposit float odor ---------- */
public const string TblRules = @"
${rules.join('\n')}
";

/* ---------- 英文本地化：物质id 英文名 / 中文原文 英文译文 ---------- */

/* ---------- 焓变：物种id 溶解焓 kJ/mol ---------- */
public const string TblDissolveH = @"
${disH.join('\n')}
";

/* ---------- 焓变：方程式 反应焓 kJ/mol ---------- */
public const string TblRuleH = @"
${ruleH.join('\n')}
";

/* ---------- 官能团家族：物质id 家族 ---------- */
public const string TblFamilies = @"
${families.join('\n')}
";

/* ---------- 通用有机规则：反应家族 产物家族 条件 可用试剂 副产物 现象 ---------- */
public const string TblOrganicRules = @"
${orgRules.join('\n')}
";

/* ---------- 家族映射：家族 反应物id 产物id ---------- */
public const string TblOrganicMap = @"
${orgMap.join('\n')}
";

/* ---------- 本地化：物质id 英文名 ---------- */
public const string TblI18nSub = @"
${i18nSub.join('\n')}
";

/* ---------- 本地化：中文原文 英文译文 ---------- */
public const string TblI18nText = @"
${i18nTxt.join('\n')}
";

/* ---------- 本地化：类别 / 容器 ---------- */
public const string TblI18nCls = @"
${i18nCls.join('\n')}
";
public const string TblI18nVessel = @"
${i18nVes.join('\n')}
";

/* ---------- 特殊规则（颜色指示剂类）：id 现象 tint clearTint ---------- */
public const string TblSpecials = @"
${specials.join('\n')}
";

}
}
`);

const generated = parts.join('');
if (process.argv.includes('--check')) {
  const current = fs.readFileSync(path.join(OUT, 'ChemData.g.cs'), 'utf8');
  const before = current.replace(/\r\n/g, '\n').split('\n');
  const after = generated.replace(/\r\n/g, '\n').split('\n');
  let changed = 0;
  for (let i = 0; i < Math.max(before.length, after.length); i++) {
    if (before[i] === after[i]) continue;
    changed++;
    if (changed <= 16) console.log(`line ${i + 1}\ncurrent: ${before[i]}\ngenerated: ${after[i]}`);
  }
  console.log(`Generator check: ${changed} changed lines; no files written.`);
  process.exit(changed ? 1 : 0);
}
fs.writeFileSync(path.join(OUT, 'ChemData.g.cs'), generated, 'utf8');

console.log('已生成 ChemData.g.cs');
console.log('  物质 ' + substances.length + ' 条');
console.log('  离子 ' + ions.length + ' 条');
console.log('  电离表 ' + diss.length + ' 条');
console.log('  沉淀外观 ' + precs.length + ' 条');
console.log('  沉淀生成规则 ' + precipRules.length + ' 条');
console.log('  反应规则 ' + rules.length + ' 条');
console.log('  特殊规则 ' + specials.length + ' 条');
console.log('  焰色 ' + flames.length + ' 条');
console.log('  大小 ' + (fs.statSync(path.join(OUT, 'ChemData.g.cs')).size / 1024).toFixed(1) + ' KB');
