const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

if (fs.readFileSync(E, 'utf8').includes('[ClickDbg]')) { console.log('- 已存在'); process.exit(0); }

/* 1) NodeDrag.OnPointerClick 加日志 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('public void OnPointerClick(PointerEventData e) {')
      && lines[i + 1] && lines[i + 1].includes('if (_moved) return;')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 NodeDrag.OnPointerClick'); process.exit(1); }
console.log('NodeDrag.OnPointerClick 在第', at + 1, '行:');
for (let i = at; i < at + 4; i++) console.log('   ', lines[i]);

lines.splice(at + 1, 0,
  '            Debug.Log("[ClickDbg] 节点点击 button=" + e.button + " _moved=" + _moved',
  '                    + " pending=" + scene.PendingEdge + " node=" + (node != null ? node.Formula : "null"));');
fs.writeFileSync(E, lines.join('\n'), 'utf8');
console.log('✓ 节点点击日志已加');

/* 2) _moved 只在真的拖动过才置位，并在点击处理完后复位 */
let c = fs.readFileSync(E, 'utf8');
const oldMove = `        public void OnDrag(PointerEventData e) {
            _moved = true;
            node.Pos += e.delta;`;
const newMove = `        public void OnDrag(PointerEventData e) {
            /* 只有真的拖出距离才算拖动 —— 手抖 1px 不该把这次点击吃掉。
               而且 _moved 必须在每次按下时复位，否则拖过一次之后
               所有后续点击都会被 "if (_moved) return" 挡掉。 */
            if (e.delta.sqrMagnitude > 0.5f) _moved = true;
            node.Pos += e.delta;`;
if (c.includes(oldMove)) { c = c.replace(oldMove, newMove); console.log('  ✓ OnDrag 的 _moved 判定已改'); }
else console.log('  ✗ OnDrag 锚点未命中');

const oldClick = `            if (_moved) return;                 // 拖过了就不算点击`;
const newClick = `            if (_moved) { _moved = false; return; }   // 拖过了就不算点击，但要复位`;
if (c.includes(oldClick)) { c = c.replace(oldClick, newClick); console.log('  ✓ 点击里的 _moved 已复位'); }
else console.log('  ✗ 点击锚点未命中');

/* 3) 按下时复位 */
const oldBegin = '        public void OnBeginDrag(PointerEventData e) { _moved = false; }';
const newBegin = '        public void OnBeginDrag(PointerEventData e) { _moved = false; }\n        public void OnPointerDown(PointerEventData e) { _moved = false; }   /* 每次按下都复位 */';
if (c.includes(oldBegin)) {
  c = c.replace(oldBegin, newBegin);
  c = c.replace('public class NodeDrag : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {',
                'public class NodeDrag : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {');
  console.log('  ✓ OnPointerDown 已加');
} else console.log('  ✗ OnBeginDrag 锚点未命中');

fs.writeFileSync(E, c, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
