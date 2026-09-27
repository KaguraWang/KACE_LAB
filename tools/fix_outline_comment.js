const fs = require('fs');
const O = 'D:/ChemLabUnity/Assets/Scripts/Game/OutlineFx.cs';
let lines = fs.readFileSync(O, 'utf8').split('\n');

/* 找到那段坏掉的注释，整段换成合法的 XML 注释 */
let from = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('/// <summary>哪些网格不参与描边。')) { from = i; break; }
}
if (from < 0) { console.log('✗ 找不到注释'); process.exit(1); }
let to = from;
for (let i = from; i < from + 10; i++) {
  if (lines[i].includes('</summary>')) { to = i; break; }
}
console.log('替换', from + 1, '..', to + 1);
for (let i = from; i <= to; i++) console.log('  旧:', lines[i]);

const nw = [
  '    /// <summary>哪些网格不参与描边。</summary>',
  '    /// <remarks>',
  '    /// Fx 下的粒子是每帧重建的公告板四边形，描出来会变成一堆飘在空中的方块；',
  '    /// Liquid / Sediment 在容器内部，描了会被玻璃透出来、把整个容器染蓝。',
  '    /// 只描外壳就够表达「哪个东西被聚焦了」。',
  '    /// </remarks>',
];
lines.splice(from, to - from + 1, ...nw);
fs.writeFileSync(O, lines.join('\n'), 'utf8');
console.log('✓ 注释已修');
