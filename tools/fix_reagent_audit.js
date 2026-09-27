const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

/* 字段实际是 _query（搜索词）和 _contentList（列表容器），不是 _search/_reagents */
let from = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('REAGENT|search|')) { from = i - 5; break; }
}
if (from < 0) { console.log('✗ 找不到 ReagentAudit'); process.exit(1); }
let to = -1;
for (let i = from; i < from + 20; i++) {
  if (lines[i] && lines[i].includes('REAGENT|total|')) { to = i + 1; break; }
}
console.log('替换', from + 1, '..', to + 1);
for (let i = from; i <= to; i++) console.log('  旧:', lines[i]);

const nw = [
  '    public string ReagentAudit() {',
  '        var sb = new System.Text.StringBuilder();',
  '        sb.AppendLine("REAGENT|cat|" + _cat);',
  '        sb.AppendLine("REAGENT|query|" + (_query != null ? _query : ""));',
  '        sb.AppendLine("REAGENT|total|" + _reagents.Count);',
  '        int shown = 0;',
  '        if (_contentList != null) {',
  '            for (int i = 0; i < _contentList.childCount; i++) {',
  '                var ch = _contentList.GetChild(i);',
  '                if (ch != null && ch.gameObject.activeSelf) shown++;',
  '            }',
  '        }',
  '        sb.AppendLine("REAGENT|visible|" + shown);',
  '        return sb.ToString();',
  '    }',
];
lines.splice(from, to - from + 1, ...nw);
fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('✓ ReagentAudit 已按实际字段重写');
const c = fs.readFileSync(U, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
