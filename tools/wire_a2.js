const fs = require('fs');
let n = 0;
const P = (p, a, b, label) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 44)); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* 1) 撤掉机械生成的那批错规则 */
const R = 'D:/ChemLab/js/rules.js';
let rc = fs.readFileSync(R, 'utf8');
const cut = rc.indexOf('CIE 9701 A2 反应数据库');
if (cut >= 0) {
  const head = rc.lastIndexOf('/* ====', cut);
  rc = rc.slice(0, head >= 0 ? head : cut);
  fs.writeFileSync(R, rc.replace(/\s*$/, '\n'), 'utf8');
  console.log('✓ 已撤掉机械生成的那批');
}

/* 2) 把 a2rules.js 注册进加载顺序（在 rules.js 之后，因为要用 R） */
for (const f of ['D:/ChemLab/index.html', 'D:/ChemLabUnity/tools/gen_cs.js']) {
  let c = fs.readFileSync(f, 'utf8');
  if (!c.includes('a2rules.js')) {
    if (f.endsWith('.html')) c = c.replace('<script src="js/engine.js"></script>',
      '<script src="js/a2rules.js"></script>\r\n  <script src="js/engine.js"></script>');
    else c = c.replace("'js/rules.js', 'js/engine.js'", "'js/rules.js', 'js/a2rules.js', 'js/engine.js'");
    fs.writeFileSync(f, c, 'utf8'); n++;
  }
}
console.log('加载顺序:', n > 0 ? '已注册' : '已存在');
console.log('补丁处数:', n);

/* 3) 检查引用的物种存在性 */
const dir = 'D:/ChemLab/js/';
const src = ['data.js','data2.js','cie9701.js'].map(f => fs.readFileSync(dir + f, 'utf8')).join('\n');
const { SUBSTANCES, IONS } = new Function(src + '; return { SUBSTANCES, IONS };')();
const ids = new Set();
const key = x => String(x).toLowerCase().replace(/\s+/g, '');
const idx = new Map();
for (const s of SUBSTANCES) { ids.add(s[0]); if (!idx.has(key(s[0]))) idx.set(key(s[0]), s[0]); if (!idx.has(key(s[2]))) idx.set(key(s[2]), s[0]); }
for (const s of (IONS || [])) { ids.add(s[0]); if (!idx.has(key(s[0]))) idx.set(key(s[0]), s[0]); }

const a2 = fs.readFileSync(dir + 'a2rules.js', 'utf8');
const miss = new Set();
for (const m of a2.matchAll(/R\('([^']+)'/g)) {
  for (const side of m[1].split('->')) {
    for (let tok of side.split('+')) {
      tok = tok.trim().replace(/^[\d.]+/, '');
      if (!tok) continue;
      if (!ids.has(tok) && !idx.has(key(tok))) miss.add(tok);
    }
  }
}
console.log('');
console.log('A2 规则条数:', (a2.match(/^R\(/gm) || []).length);
console.log('未解析的物种:', miss.size ? [...miss].join(', ') : '无');
