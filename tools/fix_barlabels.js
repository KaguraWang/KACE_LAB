const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let c = fs.readFileSync(U, 'utf8');
let n = 0;

/* ---- 1) 加一个栏内标签辅助函数 ---- */
if (!c.includes('RectTransform BarLabel(')) {
  c = c.replace('    /* ---------------- 滑动条 ---------------- */',
`    /* ------------------------------------------------------------ 栏内小标签
       之前这里踩了两个坑，导致文字跑到卡片外面：
       ① 用纵向拉伸锚点（anchorMin.y=0, anchorMax.y=1）再设 sizeDelta.y=26，
          那是在拉伸出来的高度上**又加** 26，高度就失控了；
       ② pivot 是中心、锚在栏的左边缘，矩形会向左凸出一半。
       统一用这个函数：锚点与 pivot 都是 (0, 0.5)，尺寸就是真实尺寸。 */
    Text BarLabel(Transform parent, string text, float width, int size = 11) {
        var t = Label(parent, text, size, DIM, TextAnchor.MiddleLeft,
                      new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        var rt = (RectTransform)t.transform;
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, 24);
        return t;
    }

    /* ---------------- 滑动条 ---------------- */`);
  n++;
}

/* ---- 2) 把五个栏标签换成 BarLabel ---- */
const swaps = [
  [`        _pourLabel = Label(_pourBar, "倾倒量", 12, INK, TextAnchor.MiddleLeft,
                           new Vector2(0,0), new Vector2(0,1), Vector2.zero, Vector2.zero);
        ((RectTransform)_pourLabel.transform).sizeDelta = new Vector2(74, 26);`,
   `        _pourLabel = BarLabel(_pourBar, "倾倒量", 74, 12);`],
  [`        _burnerLabel = Label(_burnerBar, "本生灯火力", 12, INK, TextAnchor.MiddleLeft,
                             new Vector2(0,0), new Vector2(0,1), Vector2.zero, Vector2.zero);
        ((RectTransform)_burnerLabel.transform).sizeDelta = new Vector2(86, 26);`,
   `        _burnerLabel = BarLabel(_burnerBar, "本生灯火力", 86, 12);`],
  [`        _testLabel = Label(_testBar, "检验器材", 11, DIM, TextAnchor.MiddleLeft,
                           new Vector2(0,0), new Vector2(0,1), Vector2.zero, Vector2.zero);
        ((RectTransform)_testLabel.transform).sizeDelta = new Vector2(74, 26);`,
   `        _testLabel = BarLabel(_testBar, "检验器材", 74);`],
  [`        var tl = Label(_tagBar, "容器标签", 11, DIM, TextAnchor.MiddleLeft,
                       new Vector2(0,0), new Vector2(0,1), Vector2.zero, Vector2.zero);
        ((RectTransform)tl.transform).sizeDelta = new Vector2(74, 26);`,
   `        BarLabel(_tagBar, "容器标签", 74);`],
  [`        var el = Label(_eqBar, "方程式标签", 11, DIM, TextAnchor.MiddleLeft,
                       new Vector2(0,0), new Vector2(0,1), Vector2.zero, Vector2.zero);
        ((RectTransform)el.transform).sizeDelta = new Vector2(74, 26);`,
   `        BarLabel(_eqBar, "方程式标签", 74);`],
  [`        var dl = Label(_eqBar, "时长", 11, DIM, TextAnchor.MiddleLeft,
                       new Vector2(0,0), new Vector2(0,1), new Vector2(78, 30), new Vector2(106, 56));`,
   `        var dl = BarLabel(_eqBar, "时长", 24);
        ((RectTransform)dl.transform).anchoredPosition = new Vector2(78, 0);`],
  [`        var ul = Label(_eqBar, "秒", 11, DIM, TextAnchor.MiddleLeft,
                       new Vector2(0,0), new Vector2(0,1), new Vector2(172, 30), new Vector2(200, 56));`,
   `        var ul = BarLabel(_eqBar, "秒", 20);
        ((RectTransform)ul.transform).anchoredPosition = new Vector2(172, 0);`],
];
for (const [a, b] of swaps) {
  if (c.includes(a)) { c = c.replace(a, b); n++; }
  else console.log('  ✗ 未命中:', a.split('\n')[0].trim().slice(0, 50));
}

/* ---- 3) Btn 之后设尺寸要先断开拉伸锚点 ---- */
c = c.replace(`        _pourBtn = Btn(_pourBar, "倒", Pour, ACC, 13);
        { var br = (RectTransform)_pourBtn.transform; br.pivot = new Vector2(0,0);
          br.anchoredPosition = new Vector2(236, 3); br.sizeDelta = new Vector2(52, 22); }`,
`        _pourBtn = Btn(_pourBar, "倒", Pour, ACC, 13);
        FixBtnRect(_pourBtn, new Vector2(236, 0), new Vector2(52, 22));`);
c = c.replace(`        _burnerBtn = Btn(_burnerBar, "点燃", ToggleBurner, WARN, 13);
        { var br = (RectTransform)_burnerBtn.transform; br.pivot = new Vector2(0,0);
          br.anchoredPosition = new Vector2(228, 3); br.sizeDelta = new Vector2(60, 22); }`,
`        _burnerBtn = Btn(_burnerBar, "点燃", ToggleBurner, WARN, 13);
        FixBtnRect(_burnerBtn, new Vector2(228, 0), new Vector2(60, 22));`);
n += 2;

/* ---- 4) FixBtnRect 辅助函数 ---- */
if (!c.includes('void FixBtnRect(')) {
  c = c.replace('    Text BarLabel(Transform parent, string text, float width, int size = 11) {',
`    /// <summary>Btn 建出来的按钮是「撑满父节点」的（锚点 0..1），
    /// 直接改 sizeDelta 不会得到想要的尺寸。要自由摆放必须先断开拉伸。</summary>
    static void FixBtnRect(Button b, Vector2 pos, Vector2 size) {
        var rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    Text BarLabel(Transform parent, string text, float width, int size = 11) {`);
  n++;
}

fs.writeFileSync(U, c, 'utf8');
console.log('改动处数:', n);
