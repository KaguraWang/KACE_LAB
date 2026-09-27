/* =====================================================================
   从 CIE 数据手册里抽取三张表
     1) 电离能（第 1–4）
     2) 键能（双原子精确值 + 多原子平均值）
     3) 标准电极电势
   以及重要常数。输出 JS 数据文件供引擎使用。
   ===================================================================== */
const fs = require('fs');
const SRC = 'D:/ChemLabUnity/caie/extracted/cie_specimen_data_booklet.txt';
const txt = fs.readFileSync(SRC, 'utf8');
const pages = txt.split(/===== PAGE (\d+) =====/);
const P = {};
for (let i = 1; i < pages.length; i += 2) P[+pages[i]] = pages[i + 1];

const out = { ionisation: [], bondDiatomic: [], bondPoly: [], electrode: [], constants: {} };

/* ---------- 1) 电离能（p4） ---------- */
for (const ln of (P[4] || '').split('\n')) {
  /* 形如： H 1 1310 – – –   /   Be 4 900 1760 14800 21000 */
  const m = ln.match(/^\s*([A-Z][a-z]?)\s+(\d{1,2})\s+(\d+)\s+(\d+|–)\s+(\d+|–)\s+(\d+|–)\s*$/);
  if (!m) continue;
  const num = x => x === '–' ? null : parseInt(x, 10);
  out.ionisation.push({ el: m[1], z: +m[2], ie: [num(m[3]), num(m[4]), num(m[5]), num(m[6])] });
}

/* ---------- 2) 键能 ---------- */
/* PDF 里单键用的是私用区字符 U+F8E7（字体编码），要当成单键处理。
   开链的键符号一共有四种：U+F8E7 / – / — 是单键，= 是双键，≡ 是三键，
   "…." 是苯环里的离域键。 */
const SB = '[\\uF8E7\\u2013\\u2014-]';
const BOND_TOK = new RegExp('([A-Z][a-z]?)\\\\s*(' + SB + '|=|\\u2261|\\u2026\\\\.{1,4})\\\\s*([A-Z][a-z]?)(?:\\\\s*\\\\(([^)]*)\\\\))?\\\\s+(\\\\d{3,4})', 'g');
function orderOf(sym) {
  if (sym === '=') return 2;
  if (sym === '\\u2261') return 3;
  if (/\\u2026|\\./.test(sym)) return 1.5;     // 苯环离域键，单独标注
  return 1;
}
for (const [pg, isExact] of [[5, true], [6, false]]) {
  for (const ln of (P[pg] || '').split('\n')) {
    BOND_TOK.lastIndex = 0;
    let m;
    while ((m = BOND_TOK.exec(ln))) {
      const a = m[1], sym = m[2], b = m[3], note = m[4] || '', kJ = +m[5];
      if (a === 'Bond' || b === 'Energy') continue;
      const rec = { a, b, order: orderOf(sym), note, kJ, exact: isExact };
      (isExact ? out.bondDiatomic : out.bondPoly).push(rec);
    }
  }
}

/* ---------- 3) 电极电势（p7–p8） ---------- */
const clean = s => s.replace(/\s+/g, '')
  .replace(/(\d)\s*\.\s*(\d)/g, '$1.$2')      // "+ 1 . 0 7" → "+1.07"
  .replace(/\u2013/g, '−').replace(/-/g, '−');
for (const pg of [7, 8]) {
  for (const ln of (P[pg] || '').split('\n')) {
    /* 末尾是 ±数字，前面是 "物种 + ne− 物种" */
    const m = ln.match(/^(.*?)\s*([+\u2212−-]\s*[\d.\s]+)\s*$/);
    if (!m) continue;
    const eq = m[1].trim();
    if (!/e[\u2212−-]|e–/.test(eq)) continue;
    const v = parseFloat(clean(m[2]));
    if (isNaN(v)) continue;
    out.electrode.push({ eq: eq.replace(/\s+/g, ' '), E: v });
  }
}

/* ---------- 4) 常数（p3） ---------- */
const consts = {
  R: 8.31, F: 9.65e4, L: 6.02e23, h: 6.63e-34, c: 3.00e8,
  e: -1.60e-19, Vm_stp: 22.4, Vm_rtp: 24.0, Kw: 1.00e-14, c_water: 4.18,
};
out.constants = consts;

/* ---------- 输出 ---------- */
fs.writeFileSync('D:/ChemLabUnity/caie/specimen_tables.json', JSON.stringify(out, null, 1), 'utf8');
console.log('电离能:', out.ionisation.length, '种元素');
console.log('  样例:', JSON.stringify(out.ionisation.slice(0, 4)));
console.log('双原子键能:', out.bondDiatomic.length, '条');
console.log('  样例:', JSON.stringify(out.bondDiatomic.slice(0, 6)));
console.log('多原子键能:', out.bondPoly.length, '条');
console.log('  样例:', JSON.stringify(out.bondPoly.slice(0, 6)));
console.log('电极电势:', out.electrode.length, '条');
console.log('  样例:', JSON.stringify(out.electrode.slice(0, 3)));
