const fs = require('fs');
let n = 0;
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

/* 1) 删掉 OnPointerClick 后面多余的那个 } */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].trim() === '}' && lines[i - 1] && lines[i - 1].trim() === '}'
      && lines[i - 2] && lines[i - 2].includes('scene.BeginEdgeMode(node);')) {
    console.log('  删掉第', i + 1, '行多余的 }');
    lines.splice(i, 1);
    n++;
    break;
  }
}
fs.writeFileSync(E, lines.join('\n'), 'utf8');
let c = fs.readFileSync(E, 'utf8');
console.log('  大括号差:',
  (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);

/* 2) ContextMenu 加全屏挡板（签名是 void，不带 public） */
const aMark = '    void ContextMenu(Vector2 screenPos, List<(string label, System.Action act)> items) {';
const a = c.indexOf(aMark);
if (a < 0) { console.log('✗ 找不到 ContextMenu'); process.exit(1); }

/* 找到方法结尾 */
let depth = 0, started = false, end = -1;
for (let i = a; i < c.length; i++) {
  if (c[i] === '{') { depth++; started = true; }
  if (c[i] === '}') { depth--; if (started && depth === 0) { end = i; break; } }
}
console.log('  ContextMenu 范围', a, '..', end);

const oldBody = c.slice(a, end + 1);
console.log('  --- 旧实现 ---');
console.log(oldBody);

const nw = `    void ContextMenu(Vector2 screenPos, List<(string label, System.Action act)> items) {
        CloseMenu();
        float h = items.Count * 30 + 10;

        /* 全屏「挡板」：左键点画面任意别处就把选单关掉。
           必须先建它 —— 这样选单才盖在它上面、点得到。
           挡板本身几乎全透明（alpha 0.001），看不见但能接收点击。 */
        var catcher = MkNode("MenuCatcher", _menuLayer, new Color(0, 0, 0, 0.001f));
        Stretch(catcher);
        var cb = catcher.gameObject.AddComponent<Button>();
        cb.transition = Selectable.Transition.None;
        cb.onClick.AddListener(CloseMenu);

        var panel = MkNode("Menu", _menuLayer, new Color(0.12f, 0.16f, 0.22f, 0.99f));
        panel.anchorMin = panel.anchorMax = new Vector2(0, 0);
        panel.pivot = new Vector2(0, 1);
        panel.anchoredPosition = screenPos;
        panel.sizeDelta = new Vector2(190, h);
        for (int i = 0; i < items.Count; i++) {
            var it = items[i];
            if (it.act == null) continue;                 /* 分隔线 */
            MkBtn(panel, it.label, () => { CloseMenu(); it.act(); },
                  new Vector2(-6, -6 - i * 30), new Vector2(178, 28), new Vector2(1, 1),
                  new Color(0.18f, 0.24f, 0.32f));
        }
    }`;

c = c.slice(0, a) + nw + c.slice(end + 1);

/* 3) CloseMenu 也要把挡板一起清掉（它已经在 _menuLayer 下，遍历子节点会删掉）—— 确认一下 */
const closeMark = '    public void CloseMenu() {';
const ci = c.indexOf(closeMark);
const cEnd = c.indexOf('}', c.indexOf('{', ci)) + 1;
console.log('  CloseMenu 现状:');
console.log(c.slice(ci, cEnd + 1));

fs.writeFileSync(E, c, 'utf8');
n++;
console.log('  ✓ ContextMenu 已加挡板');
console.log('改动处数:', n);
