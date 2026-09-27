const fs = require('fs');
const p = 'D:/ChemLabUnity/caie/harness/make_reports.js';
let c = fs.readFileSync(p, 'utf8');
const R = (a, b) => { if (c.includes(a)) { c = c.replace(a, b); return true; } return false; };

const r1 = R("smd += '本次**只完成了 P3 定量分析题的可自动化部分**，其余真题未执行。具体：\\n\\n';",
             "smd += 'Paper 3 六份卷子的 Q1/Q2/Q3 均已建用例并全部通过。下面把「已覆盖」「范围外」「未自动化」三类如实分开：\\n\\n';");

const r2 = R("    'P3 六份卷子的 Q1/Q2/Q3 全部建有用例；Q3 的类型化检验由 CIE 官方定性分析表逐行覆盖',\n", "");

const r3 = R("  ['9701/33/M/J/25', 'Q1 KMnO4 滴定 H2O2',",
`  ['9701/34/M/J/24', 'Q1 返滴定测碱式碳酸锌 Mr；Q2 坩埚灼烧测 Ar', 'QP p2-5 + CI(ZnCO3·Zn(OH)2, 5.05 g/dm3 KOH) + MS p6-7'],
  ['9701/33/M/J/25', 'Q1 KMnO4 滴定 H2O2；Q2 H2O2 催化分解焓',`);

const r4 = R("  ['9701/33/M/J/26', 'Q1 Mg 与酸反应测 H2 体积',",
             "  ['9701/33/M/J/26', 'Q1 Mg 与酸反应测 H2 体积；Q2 取代羧酸 Mr；Q3 不饱和检验 + Cr2(SO4)3/NaNO3 定性',");

const r5 = R("  ['9701/34/M/J/26', 'Q1 碘量法测 Na2S2O3（IO3-/I2/S2O3^2-）',",
             "  ['9701/34/M/J/26', 'Q1 碘量法测 Na2S2O3；Q2 水合盐 BaCl2·2H2O 脱水测 Ar',");

fs.writeFileSync(p, c, 'utf8');
console.log(`替换结果: intro=${r1} stale=${r2} 2434=${r3} 2633=${r4} 2634=${r5}`);
