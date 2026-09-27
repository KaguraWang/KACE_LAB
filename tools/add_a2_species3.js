const fs = require('fs');
const p = 'D:/ChemLab/js/cie9701.js';
let c = fs.readFileSync(p, 'utf8');
if (c.includes('A2EX3')) { console.log('已添加'); process.exit(0); }

const add = `
/* A2 规则需要但还缺的物种 */
const A2EX3 = [
  /* 第 2 族 */
  ['srco3','碳酸锶','SrCO3','s','#f8f8f8','',3.70,'n','salt','受热分解生成 SrO'],
  ['sro','氧化锶','SrO','s','#f8f8f8','',3.50,'n','oxide',''],
  ['bao','氧化钡','BaO','s','#f8f8f8','',5.72,'n','oxide',''],
  ['ca(no3)2','硝酸钙','Ca(NO3)2','s','#f8f8f8','',2.50,'k','salt','受热分解为 CaO + NO2 + O2'],
  /* 钴 */
  ['Co2+','钴(II) 离子','Co2+','aq','','#e88a9a',1.00,'k','ion','粉红色水合离子'],
  ['Co(OH)2','氢氧化钴(II)','Co(OH)2','s','#4a6ac8','',1.00,'n','base','先蓝后粉红'],
  ['[Co(NH3)6]2+','六氨合钴(II) 离子','[Co(NH3)6]2+','aq','','#a08040',1.00,'k','ion','黄棕色'],
  ['[CoCl4]2-','四氯合钴(II) 离子','[CoCl4]2-','aq','','#2a4ac8',1.00,'k','ion','蓝色四面体配离子'],
  /* 苯系衍生物 */
  ['c6h5ch2cl','苄基氯','C6H5CH2Cl','l','','',1.10,'n','organic','侧链氯代产物'],
  ['c6h2br3oh','2,4,6-三溴苯酚','C6H2Br3OH','s','#f8f8f0','',8.00,'n','organic','白色沉淀'],
  ['c6h2br3nh2','2,4,6-三溴苯胺','C6H2Br3NH2','s','#f8f8f0','',8.00,'n','organic','白色沉淀'],
  ['c6h5n=nc6h4oh','对羟基偶氮苯','C6H5N=NC6H4OH','s','#e05a1a','',8.00,'n','organic','橙红色偶氮染料'],
  ['ch3cooc6h5','乙酸苯酯','CH3COOC6H5','l','','',1.08,'n','organic','有芳香气味'],
  /* 胺与氨基酸 */
  ['C6H5NH2','苯胺','C6H5NH2','l','','#f0f0d8',1.02,'n','base','无色油状，易被氧化变褐'],
  ['C2H5NH2','乙胺','C2H5NH2','aq','','',0.90,'k','base','有鱼腥味的碱性气体'],
  ['C2H5NH3+','乙基铵离子','C2H5NH3+','aq','','',1.00,'k','ion','乙胺质子化产物'],
  ['CH3CH2NH2','乙胺','C2H5NH2','aq','','',0.90,'k','base',''],
  ['H2NCH2COOH','甘氨酸','H2NCH2COOH','s','#f8f8f8','',1.61,'k','organic','最简单的氨基酸，两性'],
  ['H2NCH2COO-','甘氨酸根','H2NCH2COO-','aq','','',1.00,'k','ion',''],
  ['[H3NCH2COOH]+','质子化甘氨酸','[H3NCH2COOH]+','aq','','',1.00,'k','ion',''],
  ['H2NCH2CONHCH2COOH','甘氨酰甘氨酸','H2NCH2CONHCH2COOH','s','#f8f8f8','',1.50,'k','organic','二肽，含肽键'],
  ['C6H5N2Cl','氯化苯重氮盐','C6H5N2Cl','aq','','#f8f8f0',1.00,'k','salt','0–5 ℃ 稳定'],
  ['n2','氮气','N2','g','#f0f0f0','',1.25,'n','nonmetal','无色无味'],
  /* 新生态氧（氧化侧链时用） */
  ['o','新生态氧','[O]','aq','','',1.00,'k','other','氧化剂示意写法'],
];

SUBSTANCES.push(...A2EX3);
`;
c = c.replace('/* ---------------------------------------------------------------\n   焓变数据', add + '\n/* ---------------------------------------------------------------\n   焓变数据');
fs.writeFileSync(p, c, 'utf8');
console.log('✓ 已加入', (add.match(/^  \[/gm) || []).length, '个物种');

/* 电离条目 */
const e = 'D:/ChemLab/js/engine.js';
let ec = fs.readFileSync(e, 'utf8');
if (!ec.includes("'ca(no3)2':")) {
  ec = ec.replace("  'c6h5ona':  { ions: { 'C6H5O-': 1, 'Na+': 1 } },",
`  'c6h5ona':  { ions: { 'C6H5O-': 1, 'Na+': 1 } },
  'ca(no3)2': { ions: { 'Ca2+': 1, 'NO3-': 2 } },
  'c6h5n2cl': { ions: { 'C6H5N2+': 1, 'Cl-': 1 } },
  'C2H5NH2':  { ions: { 'C2H5NH3+': 1, 'OH-': 1 }, weak: true, degree: 0.01 },
  'H2NCH2COOH': { ions: {}, none: true },
  'H2NCH2CONHCH2COOH': { ions: {}, none: true },`);
  ec = ec.replace(/  'c6h5n2cl': \{ ions: \{ 'C6H5N2\+': 1, 'Cl-': 1 \} \},\n/, '');
  fs.writeFileSync(e, ec, 'utf8');
  console.log('✓ 电离条目已加');
}
