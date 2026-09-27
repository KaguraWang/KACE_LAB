const fs = require('fs');
let n = 0;

/* ============ 1) 节点点击 → 拉线 → 点另一个节点连上 ============ */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('if (e.button == PointerEventData.InputButton.Right) scene.OnNodeRightClick(node, e.position);')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 NodeDrag.OnPointerClick'); process.exit(1); }
console.log('NodeDrag.OnPointerClick 的右键盘分支在第', at + 1, '行');
console.log('  旧内容:');
for (let i = at - 2; i <= at + 2; i++) console.log('   ', lines[i]);

/* 整段重写 OnPointerClick（含它前面的 if (_moved) return;） */
let from = at - 1;                       // if (_moved) return;
while (from > 0 && !lines[from].includes('public void OnPointerClick')) from--;
let to = at + 1;                         // else scene.SelectNode(node);
console.log('  替换范围', from + 1, '..', to + 1);

const nw = [
  '        public void OnPointerClick(PointerEventData e) {',
  '            if (_moved) return;                 // 拖过了就不算点击',
  '',
  '            if (e.button == PointerEventData.InputButton.Right) {',
  '                /* 正在拉线时右键 = 取消拉线 */',
  '                if (scene.PendingEdge) { scene.CancelPending(); return; }',
  '                scene.OnNodeRightClick(node, e.position);',
  '                return;',
  '            }',
  '',
  '            /* 左键：已经在拉线 → 连到本节点；否则选中并开始拉线 */',
  '            if (scene.PendingEdge) {',
  '                if (scene.PendingFrom != node) scene.CompletePending(node);',
  '                else scene.CancelPending();     // 点回自己 = 取消',
  '                return;',
  '            }',
  '            scene.SelectNode(node);',
  '            scene.BeginEdgeMode(node);',
  '        }',
];
lines.splice(from, to - from + 1, ...nw);
fs.writeFileSync(E, lines.join('\n'), 'utf8');
n++;
console.log('  ✓ 节点点击拉线已写入');

/* ============ 2) 选单：左键点任意别处关闭 ============ */
let c = fs.readFileSync(E, 'utf8');
if (!c.includes('MenuCatcher')) {
  const oldm = `    public static void Close() {
        if (_layer == null) return;
        for (int i = _layer.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_layer.GetChild(i).gameObject);
    }`;
  /* 这段在 PetMenu 里，不在 EnthalpyScene —— 先看看 EnthalpyScene 的选单实现 */
  const a = c.indexOf('    public void ContextMenu(Vector2 screenPos, List<(string label, System.Action act)> items) {');
  if (a < 0) { console.log('✗ 找不到 EnthalpyScene.ContextMenu'); }
  else {
    const bMark = '    public void CloseMenu() {';
    const b = c.indexOf(bMark);
    const bEnd = c.indexOf('}', c.indexOf('{', b)) + 1;
    console.log('  ContextMenu 在第', c.slice(0, a).split('\\n').length, '行');

    const newCM = `    public void ContextMenu(Vector2 screenPos, List<(string label, System.Action act)> items) {
        CloseMenu();
        float h = items.Count * 30 + 10;

        /* 全屏「挡板」：左键点任意别处就把选单关掉。
           一定要先建它，选单才能盖在它上面。 */
        var catcher = MkNode("MenuCatcher", _menuLayer, new Color(0, 0, 0, 0.001f));
        Stretch(catcher);
        var cb = catcher.gameObject.AddComponent<Button>();
        cb.transition = Selectable.Transition.None;
        cb.onClick.AddListener(CloseMenu);

        var panel = MkNode("Menu", _menuLayer, new Color(0.12f, 0.16f, 0.22f, 0.99f));
        Stretch(catcher);
        panel.anchorMin = panel.anchorMax = new Vector2(0, 0);
        panel.pivot = new Vector2(0, 1);
        panel.anchoredPosition = screenPos;
        panel.sizeDelta = new Vector2(190, h);
        for (int i = 0; i < items.Count; i++) {
            var it = items[i];
            if (it.act == null) continue;                 // 分隔线
            MkBtn(panel, it.label, () => { CloseMenu(); it.act(); },
                  new Vector2(-6, -6 - i * 30), new Vector2(178, 28), new Vector2(1, 1),
                  new Color(0.18f, 0.24f, 0.32f));
        }
    }`;
    /* 用 marker 范围替换 */
    c = c.slice(0, a) + newCM + c.slice(bEnd + 1);
    fs.writeFileSync(E, c, 'utf8');
    n++;
    console.log('  ✓ 选单挡板已加');
  }
} else console.log('  - 挡板已存在');

console.log('改动处数:', n);
