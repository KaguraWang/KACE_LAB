const fs = require('fs');
const P = 'D:/ChemLabUnity/Assets/Scripts/Game/PetView.cs';
let lines = fs.readFileSync(P, 'utf8').split('\n');

if (fs.readFileSync(P, 'utf8').includes('_moved = false; }   /* 每次按下复位')) { console.log('- 已修'); process.exit(0); }

/* 找 PetHit 的 OnPointerClick / OnDrag / OnBeginDrag */
let click = -1, drag = -1, begin = -1, cls = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public void OnPointerClick(PointerEventData e) {') && lines[i + 1] && lines[i + 1].includes('_moved')) click = i;
  if (lines[i].includes('public void OnDrag(PointerEventData e) {') && i > 250) drag = i;
  if (lines[i].includes('public void OnBeginDrag(PointerEventData e)')) begin = i;
  if (lines[i].includes('public class PetHit')) cls = i;
}
console.log('OnPointerClick', click + 1, ' OnDrag', drag + 1, ' OnBeginDrag', begin + 1, ' class', cls + 1);
if (click < 0) { console.log('✗ 找不到 OnPointerClick'); process.exit(1); }
console.log('  旧 OnPointerClick 附近:');
for (let i = click; i < click + 5; i++) console.log('   ', lines[i]);

/* 1) 在 if (_moved) return; 后面加复位 */
lines[click + 1] = '        if (_moved) { _moved = false; return; }   /* 拖过了不算点击，但要复位，否则拖一次之后永远点不动 */';

/* 2) 加 OnPointerDown 复位 + 接口 */
if (cls > 0) {
  lines[cls] = lines[cls].replace('IPointerClickHandler', 'IPointerClickHandler, IPointerDownHandler');
}
if (begin > 0) {
  lines.splice(begin + 1, 0,
    '    public void OnPointerDown(PointerEventData e) { _moved = false; }   /* 每次按下复位 */');
  console.log('  ✓ OnPointerDown 已加');
}
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 已修');
const c = fs.readFileSync(P, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
