const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

/* 在 TestPanelClickables 的循环体开头加一行排除 */
let inMethod = false, done = false;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public List<(string name, Rect2 r)> TestPanelClickables()')) { inMethod = true; }
  if (inMethod && lines[i].includes('if (!g.gameObject.activeInHierarchy) continue;')) {
    lines.splice(i + 1, 0,
      '            /* pH 色条是「渐变分段 + 游标」，本来就该叠在一起，不算冲突 */',
      '            if (Path(g.transform).IndexOf("/PhBar/") >= 0) continue;');
    done = true;
    fs.writeFileSync(U, lines.join('\n'), 'utf8');
    console.log('✓ 排除行已插在第', i + 2, '行');
    break;
  }
  if (inMethod && lines[i] === '    }' && i > 0 && done) break;
}
if (!done) console.log('✗ 找不到插入点');
