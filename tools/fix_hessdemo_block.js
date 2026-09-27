const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 用明确的起止标记重建这一段，不再按行号猜 */
let from = -1, to = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('foreach (var arg2 in System.Environment.GetCommandLineArgs())')) { from = i; break; }
}
if (from < 0) { console.log('✗ 找不到 hessDemo 段'); process.exit(1); }
for (let i = from; i < from + 20; i++) {
  if (lines[i] && lines[i].trim() === '}' && lines[i - 1] && lines[i - 1].includes('SeedBornHaberDemo')) { to = i; break; }
}
if (to < 0) { console.log('✗ 找不到段尾'); process.exit(1); }
console.log('重建范围:', from + 1, '..', to + 1);
for (let i = from; i <= to; i++) console.log('  旧:', lines[i]);

const nw = [
  '        /* -hessDemo：摆出 KCl 的 Born-Haber 循环，单独抽出来便于截图验证 */',
  '        foreach (var arg2 in System.Environment.GetCommandLineArgs())',
  '            if (arg2 == "-hessDemo") {',
  '                var cs2 = EnthalpyScene.Show(gameObject, LabUI.UiFont);',
  '                if (cs2 != null) { cs2.transform.SetAsLastSibling(); cs2.SeedBornHaberDemo(); }',
  '            }',
  '        /* -focusDemo：启动后自动聚焦第一个器皿，用于截图验证轮廓高亮 */',
  '        foreach (var arg3 in System.Environment.GetCommandLineArgs())',
  '            if (arg3 == "-focusDemo") Invoke(nameof(FocusDemo), 2.5f);',
];
lines.splice(from, to - from + 1, ...nw);
fs.writeFileSync(B, lines.join('\n'), 'utf8');
console.log('✓ 已重建');
for (let i = from - 1; i < from + 9; i++) console.log('  新:', lines[i]);
