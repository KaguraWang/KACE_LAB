const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

/* 找到 AddNode 方法体，把结尾整段重写成正确的形式 */
let start = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public Node AddNode(Vector2 pos, string coef, string formula)')) { start = i; break; }
}
if (start < 0) { console.log('✗ 找不到 AddNode'); process.exit(1); }

/* 从 start 往后找 "return n;" */
let ret = -1;
for (let i = start; i < start + 60; i++) if (lines[i] && lines[i].trim() === 'return n;') { ret = i; break; }
/* 再往后找到方法收尾的 } */
let close = ret;
for (let i = ret; i < ret + 6; i++) if (lines[i] && lines[i].trim() === '}') { close = i; break; }

console.log('AddNode:', start + 1, '.. return 在', ret + 1, '收尾在', close + 1);

/* 重建结尾 */
const tail = [
  '        _nodes.Add(n);',
  '        Commit(n, formula);                     /* 匹配物种、写出规范化学式 */',
  '        if (!string.IsNullOrEmpty(coef)) {',
  '            n.CoefField.text = coef;',
  '            /* TryParse 失败会把 out 参数置 0，那会让标注显示成「0CO₂」—— 解析不了就当 1 */',
  '            if (!double.TryParse(coef, out n.Coef)) n.Coef = 1;',
  '        }',
  '        SyncNode(n);',
  '        return n;',
  '    }',
];
/* 从 _nodes.Add(n); 那一行开始替换到 close */
let tailStart = -1;
for (let i = start; i < close; i++) if (lines[i] && lines[i].includes('_nodes.Add(n);')) { tailStart = i; break; }
if (tailStart < 0) { console.log('✗ 找不到 _nodes.Add'); process.exit(1); }
lines.splice(tailStart, close - tailStart + 1, ...tail);
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ AddNode 结尾已重建');

const c = fs.readFileSync(E, 'utf8');
const idx = c.indexOf('public Node AddNode(Vector2 pos, string coef, string formula)');
console.log(c.substring(idx, idx + 900).split('\n').slice(-16).join('\n'));
