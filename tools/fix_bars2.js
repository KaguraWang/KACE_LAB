const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let c = fs.readFileSync(U, 'utf8');
let n = 0;

/* 1) MakeTagBtn：x 改绝对坐标 + 支持宽度 */
c = c.replace(`    Button MakeTagBtn(string label, System.Action act, float x, float y) {
        var slot = Node("TB_" + label, _tagBar, new Vector2(0,0), new Vector2(0,0),
                        Vector2.zero, Vector2.zero, new Color(0,0,0,0));
        var r = (RectTransform)slot.transform;
        r.pivot = new Vector2(0,0);
        r.anchoredPosition = new Vector2(78 + x, y);
        r.sizeDelta = new Vector2(72, 22);
        return Btn(slot, label, act, BG2, 11);
    }`,
`    Button MakeTagBtn(string label, System.Action act, float x, float y, float w = 72) {
        var slot = Node("TB_" + label, _tagBar, new Vector2(0,0), new Vector2(0,0),
                        Vector2.zero, Vector2.zero, new Color(0,0,0,0));
        var r = (RectTransform)slot.transform;
        r.pivot = new Vector2(0,0);
        r.anchoredPosition = new Vector2(x, y);      /* x 是栏内绝对坐标 */
        r.sizeDelta = new Vector2(w, 22);
        return Btn(slot, label, act, BG2, 11);
    }`);
n++;

/* 2) MakeEqBtn：x 同样改绝对坐标 */
c = c.replace(`    Button MakeEqBtn(string label, System.Action act, float x, float y, float w) {
        var slot = Node("EQ_" + label, _eqBar, new Vector2(0,0), new Vector2(0,0),
                        Vector2.zero, Vector2.zero, new Color(0,0,0,0));
        var r = (RectTransform)slot.transform;
        r.pivot = new Vector2(0,0);
        r.anchoredPosition = new Vector2(78 + x, y);
        r.sizeDelta = new Vector2(w, 24);
        return Btn(slot, label, act, BG2, 11);
    }`,
`    Button MakeEqBtn(string label, System.Action act, float x, float y, float w) {
        var slot = Node("EQ_" + label, _eqBar, new Vector2(0,0), new Vector2(0,0),
                        Vector2.zero, Vector2.zero, new Color(0,0,0,0));
        var r = (RectTransform)slot.transform;
        r.pivot = new Vector2(0,0);
        r.anchoredPosition = new Vector2(x, y);      /* x 是栏内绝对坐标 */
        r.sizeDelta = new Vector2(w, 22);
        return Btn(slot, label, act, BG2, 11);
    }`);
n++;

/* 3) 时长 / 秒 的两个标签改成单行调用 */
c = c.replace(`        var dl = BarLabel(_eqBar, "时长", 24);
        ((RectTransform)dl.transform).anchoredPosition = new Vector2(78, 0);`,
`        BarLabel(_eqBar, "时长", 24, 236, 62);`);
c = c.replace(`        var ul = BarLabel(_eqBar, "秒", 20);
        ((RectTransform)ul.transform).anchoredPosition = new Vector2(172, 0);`,
`        BarLabel(_eqBar, "秒", 20, 336, 62);`);
n += 2;

/* 4) 容器标签栏的 具体/离子 按钮宽度（第二行三个按钮要放得下） */
fs.writeFileSync(U, c, 'utf8');
console.log('改动处数:', n);

/* 查验 */
const chk = fs.readFileSync(U, 'utf8');
for (const k of ['r.anchoredPosition = new Vector2(x, y);', 'float w = 72', 'BarLabel(_eqBar, "时长", 24, 236, 62);'])
  console.log('  ' + k + ' → ' + (chk.includes(k) ? '✓' : '✗'));
