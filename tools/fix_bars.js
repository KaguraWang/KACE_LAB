/* =====================================================================
   容器标签栏 / 方程式标签栏：统一坐标系重排
     病灶：一个栏里混了两套定位方式 ——
       MakeEqBtn 用 anchoredPosition=(78+x, y)，锚点 (0,0)
       BarLabel  用锚点 (0,0.5) + pivot (0,0.5)，居中
     于是标签和按钮的 y 对不上，叠在一起。

     现在统一成：锚点 (0,0)、pivot (0,0)、尺寸就是真实尺寸，
     全部用栏内像素坐标显式摆放。
   ===================================================================== */
const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let c = fs.readFileSync(U, 'utf8');
let n = 0;

/* ---- 1) BarLabel 改成左下角锚点，和按钮一致 ---- */
c = c.replace(/    Text BarLabel\(Transform parent, string text, float width, int size = 11\) \{[\s\S]*?\n    \}/,
`    Text BarLabel(Transform parent, string text, float width, float x, float y, int size = 11) {
        var t = Label(parent, text, size, DIM, TextAnchor.MiddleLeft,
                      new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, Vector2.zero);
        var rt = (RectTransform)t.transform;
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, 22);
        return t;
    }`);
n++;

/* ---- 2) 各栏标签的调用改成带 (x,y) ---- */
const swaps = [
  [`        _pourLabel = BarLabel(_pourBar, "倾倒量", 74, 12);`,
   `        _pourLabel = BarLabel(_pourBar, "倾倒量", 74, 0, 2, 12);`],
  [`        _burnerLabel = BarLabel(_burnerBar, "本生灯火力", 86, 12);`,
   `        _burnerLabel = BarLabel(_burnerBar, "本生灯火力", 86, 0, 2, 12);`],
  [`        _testLabel = BarLabel(_testBar, "检验器材", 74);`,
   `        _testLabel = BarLabel(_testBar, "检验器材", 74, 0, 2);`],
  [`        BarLabel(_tagBar, "容器标签", 74);`,
   `        BarLabel(_tagBar, "容器标签", 74, 0, 34);`],
  [`        BarLabel(_eqBar, "方程式标签", 74);`,
   `        BarLabel(_eqBar, "方程式标签", 74, 0, 62);`],
  [`        var dl = BarLabel(_eqBar, "时长", 24);
        ((RectTransform)dl.transform).anchoredPosition = new Vector2(78, 0);`,
   `        BarLabel(_eqBar, "时长", 24, 236, 62);`],
  [`        var ul = BarLabel(_eqBar, "秒", 20);
        ((RectTransform)ul.transform).anchoredPosition = new Vector2(172, 0);`,
   `        BarLabel(_eqBar, "秒", 20, 336, 62);`],
];
for (const [a, b] of swaps) {
  if (c.includes(a)) { c = c.replace(a, b); n++; }
  else console.log('  ✗ 未命中:', a.split('\n')[0].trim().slice(0, 46));
}

/* ---- 3) 两个栏加高，容得下两/三行 ---- */
c = c.replace('                       new Vector2(12, 192), new Vector2(-12, 254), new Color(0,0,0,0));',
              '                       new Vector2(12, 192), new Vector2(-12, 256), new Color(0,0,0,0));   /* 64 高，两行 */');
c = c.replace('                      new Vector2(12, 262), new Vector2(-12, 362), new Color(0,0,0,0));',
              '                      new Vector2(12, 264), new Vector2(-12, 364), new Color(0,0,0,0));   /* 100 高，三行 */');
n += 2;

/* ---- 4) 重排两个栏里的按钮 ---- */
/* 容器标签栏：两行
     第 1 行 y=34：标签 | 标签开关(78) | 具体/离子(146)
     第 2 行 y=6 ：物质(78) | mol(146) | 浓度(214) */
c = c.replace(`        MakeTagBtn("标签", () => { TagSettings.ShowTags = !TagSettings.ShowTags; }, 0, 2);
        MakeTagBtn("具体/离子", () => { TagSettings.Molecular = !TagSettings.Molecular; }, 78, 2);
        MakeTagBtn("物质", () => { TagSettings.ShowName = !TagSettings.ShowName; }, 0, 26);
        MakeTagBtn("mol", () => { TagSettings.ShowAmount = !TagSettings.ShowAmount; }, 78, 26);
        MakeTagBtn("浓度", () => { TagSettings.ShowConc = !TagSettings.ShowConc; }, 156, 26);`,
`        /* 行高 28，从下往上：第 2 行 y=6，第 1 行 y=34 */
        MakeTagBtn("标签",     () => { TagSettings.ShowTags = !TagSettings.ShowTags; },   78, 34);
        MakeTagBtn("具体/离子", () => { TagSettings.Molecular = !TagSettings.Molecular; }, 146, 34, 68);
        MakeTagBtn("物质",     () => { TagSettings.ShowName = !TagSettings.ShowName; },    78,  6);
        MakeTagBtn("mol",      () => { TagSettings.ShowAmount = !TagSettings.ShowAmount; }, 146,  6, 68);
        MakeTagBtn("浓度",     () => { TagSettings.ShowConc = !TagSettings.ShowConc; },    214,  6, 68);`);
n++;

/* 方程式标签栏：三行
     第 1 行 y=62：标签 | 显示 | 时长 [10] 秒
     第 2 行 y=34：离子式
     第 3 行 y=6 ：（留白，将来加） */
c = c.replace(`        _eqShow  = MakeEqBtn("显示 开", () => { EqTagSettings.Show = !EqTagSettings.Show; }, 78, 2, 74);
        _eqIonic = MakeEqBtn("离子式 关", () => { EqTagSettings.Ionic = !EqTagSettings.Ionic; }, 0, 32, 74);`,
`        _eqShow  = MakeEqBtn("显示 开",   () => { EqTagSettings.Show = !EqTagSettings.Show; },   78, 62, 74);
        _eqIonic = MakeEqBtn("离子式 关", () => { EqTagSettings.Ionic = !EqTagSettings.Ionic; }, 78, 34, 74);`);
n++;

/* 数字输入框的位置跟着走 */
c = c.replace(`        _eqDur = MakeNumField(_eqBar, new Vector2(112, 30), new Vector2(56, 26), EqTagSettings.Duration,`,
              `        _eqDur = MakeNumField(_eqBar, new Vector2(264, 60), new Vector2(66, 26), EqTagSettings.Duration,`);
n++;

fs.writeFileSync(U, c, 'utf8');
console.log('改动处数:', n);
