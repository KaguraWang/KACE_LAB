const fs = require('fs');
let n = 0;
const P = (p, a, b, label) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 44).replace(/\n/g,'⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* 1) 修 FindCycle 里的语法错误 */
P('D:/ChemLabUnity/Assets/Scripts/Chem/Enthalpy.cs',
`        for (int i = 0; i < nodeCount; i++) parent[i] = -1; else { }
        for (int i = 0; i < nodeCount; i++) parent[i] = -1;`,
`        for (int i = 0; i < nodeCount; i++) parent[i] = -1;`);

/* 2) 注册 enthalpy.js 到加载顺序 */
for (const f of ['D:/ChemLab/index.html', 'D:/ChemLabUnity/tools/gen_cs.js']) {
  let c = fs.readFileSync(f, 'utf8');
  if (!c.includes('enthalpy.js')) {
    if (f.endsWith('.html')) c = c.replace('<script src="js/a2rules.js"></script>',
      '<script src="js/a2rules.js"></script>\r\n  <script src="js/enthalpy.js"></script>');
    else c = c.replace("'js/a2rules.js',", "'js/a2rules.js', 'js/enthalpy.js',");
    fs.writeFileSync(f, c, 'utf8'); n++;
  }
}

/* 3) 生成器导出三张表 */
const G = 'D:/ChemLabUnity/tools/gen_cs.js';
P(G, 'GAS_NATURE, GAS_SPLINT, VOLATILE,',
     'GAS_NATURE, GAS_SPLINT, VOLATILE, BOND_ENERGY, FORMATION_H, REACTION_H,');

P(G, `/* 气体的酸碱性：气体id 性质(base/acid/neut/bleach) —— 石蕊检验的依据 */`,
`/* 焓变数据（CIE 数据手册 + 通行值） */
const bondEnergy = (api.BOND_ENERGY || []).map(r => row([r[0], r[1], r[2], r[3]]));
const formationH = Object.entries(api.FORMATION_H || {}).map(([k, v]) => row([k, v]));
const reactionH  = (api.REACTION_H || []).map(r => row([r[0], r[1], r[2]]));

/* 气体的酸碱性：气体id 性质(base/acid/neut/bleach) —— 石蕊检验的依据 */`);

P(G, `/* ---------- 气体的酸碱性（石蕊检验的依据） ---------- */`,
`/* ---------- 键能：原子A 原子B 键级 键能kJ/mol ---------- */
public const string TblBondEnergy = @"
\${bondEnergy.join('\\n')}
";

/* ---------- 标准生成焓：物种id kJ/mol ---------- */
public const string TblFormationH = @"
\${formationH.join('\\n')}
";

/* ---------- 反应焓：方程式 kJ/mol 说明 ---------- */
public const string TblReactionH = @"
\${reactionH.join('\\n')}
";

/* ---------- 气体的酸碱性（石蕊检验的依据） ---------- */`);

console.log('补丁处数:', n);

/* 4) ChemData.ParseD 是否存在 */
const C = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemCore.cs';
const cc = fs.readFileSync(C, 'utf8');
console.log('ParseD 存在:', /public static double ParseD/.test(cc));
console.log('ParseI 存在:', /public static int ParseI/.test(cc));
