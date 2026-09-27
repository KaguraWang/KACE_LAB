/* =====================================================================
   解析桌面的 A2 反应数据库 markdown
     条目格式：
       ### A2-26-01 — 标题
       **Priority:** P1
       - **Reactants:** `2Fe²⁺ + S₂O₈²⁻`
       - **reagent:** ...
       - **temperature:** ...
       - **medium:** aqueous
       - **products:** `2Fe³⁺ + 2SO₄²⁻`
       - **observation:** ...
       - **notes:** ...
     输出：结构化 JSON，供后续生成规则用。
   ===================================================================== */
const fs = require('fs');
const SRC = 'C:/Users/Simon/Desktop/CIE_9701_A2_Reactions_Conditions_Observations.md';
const OUT = 'D:/ChemLabUnity/caie/a2_reactions.json';

const raw = fs.readFileSync(SRC, 'utf8');
const lines = raw.split(/\r?\n/);

const blocks = [];
let cur = null, section = '';
for (const ln of lines) {
  const h1 = ln.match(/^#\s+(.+)$/);
  const h2 = ln.match(/^##\s+(.+)$/);
  const h3 = ln.match(/^###\s+([A-Z0-9-]+)\s+—\s+(.+)$/);
  if (h3) {
    if (cur) blocks.push(cur);
    cur = { id: h3[1], title: h3[2].trim(), section, fields: {} };
    continue;
  }
  if (h2) { section = h2[1].trim(); continue; }
  if (h1) { section = h1[1].trim(); continue; }
  if (!cur) continue;
  const f = ln.match(/^-\s+\*\*(.+?):\*\*\s*(.*)$/);
  if (f) { cur.fields[f[1].trim().toLowerCase()] = f[2].trim(); continue; }
  const p = ln.match(/^\*\*Priority:\*\*\s*(.+)$/);
  if (p) { cur.fields['priority'] = p[1].trim(); continue; }
  /* 续行并到上一个字段（观察描述经常换行） */
  if (ln.trim() && Object.keys(cur.fields).length > 0) {
    const keys = Object.keys(cur.fields);
    const last = keys[keys.length - 1];
    if (!/^#/.test(ln.trim())) cur.fields[last] += ' ' + ln.trim();
  }
}
if (cur) blocks.push(cur);

/* 拆出反引号里的化学式 */
function pick(s) {
  if (!s) return '';
  const m = s.match(/`([^`]+)`/);
  return (m ? m[1] : s).trim();
}

const out = blocks.map(b => ({
  id: b.id,
  title: b.title,
  section: b.section,
  priority: (b.fields['priority'] || '').replace(/\s+/g, ' ').trim(),
  reactants: pick(b.fields['reactants']),
  reagent: (b.fields['reagent'] || '').trim(),
  temperature: (b.fields['temperature'] || '').trim(),
  medium: (b.fields['medium'] || '').trim(),
  products: pick(b.fields['products']),
  observation: (b.fields['observation'] || '').trim(),
  notes: (b.fields['notes'] || '').trim(),
  extra: Object.fromEntries(Object.entries(b.fields).filter(([k]) =>
    !['priority','reactants','reagent','temperature','medium','products','observation','notes'].includes(k))),
}));

fs.mkdirSync('D:/ChemLabUnity/caie', { recursive: true });
fs.writeFileSync(OUT, JSON.stringify(out, null, 1), 'utf8');

/* 统计 */
const bySec = {};
for (const r of out) bySec[r.section] = (bySec[r.section] || 0) + 1;
console.log('解析条目数:', out.length);
console.log('缺 reactants 的:', out.filter(r => !r.reactants).length);
console.log('缺 products 的:', out.filter(r => !r.products).length);
console.log('');
console.log('按章节：');
for (const [k, v] of Object.entries(bySec)) console.log('  ' + v.toString().padStart(3) + '  ' + k);
console.log('');
console.log('前 5 条示例：');
for (const r of out.slice(0, 5)) {
  console.log(`  ${r.id}  ${r.reactants}  →  ${r.products}`);
  console.log(`     条件: ${r.temperature} | ${r.medium} | ${r.reagent.slice(0, 40)}`);
}
