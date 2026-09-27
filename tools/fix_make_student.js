const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/make_student.js';
let lines = fs.readFileSync(P, 'utf8').split('\n');

/* 找到 files 数组的头和尾 */
let a = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('const files = [')) { a = i; break; }
}
if (a < 0) { console.log('✗ 找不到 files 数组'); process.exit(1); }
let b = -1;
for (let i = a; i < a + 80; i++) {
  if (lines[i] !== undefined && lines[i].trim() === '];') { b = i; break; }
}
if (b < 0) { console.log('✗ 找不到数组结尾'); process.exit(1); }
console.log('files 数组:', a + 1, '..', b + 1, '（共', b - a - 1, '项）');

const nw = [
  '/* 要同步的文件。',
  '   原来是写死的一张清单 —— 结果我后来新建的 OutlineFx.cs 没加进去，',
  '   教师端能编译、学生端却报「找不到 OutlineFx」，而且很难发现。',
  '   改成：Assets/Scripts 下的 .cs 全量同步（自动带上新建的文件），',
  '   再补上 Editor、Resources 里那几个明确要的。 */',
  'function walk(dir, out) {',
  '  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {',
  '    const p = path.join(dir, e.name);',
  '    if (e.isDirectory()) walk(p, out);',
  '    else if (e.name.endsWith(".cs")) out.push(path.relative(T, p).replace(/\\\\/g, "/"));',
  '  }',
  '  return out;',
  '}',
  'const files = [',
  '  ...walk(path.join(T, "Assets/Scripts"), []),',
  '  "Assets/Editor/SpecRunner.cs",',
  '  "Assets/Editor/ChemLabTools.cs",',
  '  "Assets/Resources/anon_sheet_a.png",',
  '  "Assets/Resources/anon_sheet_a.png.meta",',
  '  "Assets/Resources/anon_sheet_b.png",',
  '  "Assets/Resources/anon_sheet_b.png.meta",',
  '  "Assets/Resources/anon_skill.txt",',
  '];',
];
lines.splice(a, b - a + 1, ...nw);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 已改成自动扫描');
