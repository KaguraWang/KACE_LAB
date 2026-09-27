const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');

const anchor = `            var rt = g.rectTransform;
            if (rt == null) continue;
            var a = rt.TransformPoint(rt.rect.min);`;
const nw = `            var rt = g.rectTransform;
            if (rt == null) continue;
            /* pH 色条是「渐变分段 + 游标」，本来就该叠在一起，不算冲突 */
            if (Path(g.transform).IndexOf("/PhBar/") >= 0) continue;
            var a = rt.TransformPoint(rt.rect.min);`;

if (u.includes('TestPanelClickables') && !u.includes('/PhBar/')) {
  if (u.includes(anchor)) {
    u = u.replace(anchor, nw);
    fs.writeFileSync(U, u, 'utf8');
    console.log('✓ pH 色条已从可点检查里排除');
  } else console.log('✗ 锚点未命中');
} else console.log('- 已排除或找不到方法');

const chk = fs.readFileSync(U, 'utf8');
console.log('  PhBar 排除存在:', chk.includes('/PhBar/'));
