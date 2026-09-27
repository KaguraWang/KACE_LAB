/* =====================================================================
   按钮文字颜色按底色自适应
     用户反馈：左边「湿润」（蓝 ACC）和「伸入」（黄 WARN）两个按钮
     是白字，根本看不清。

     与其只改这两个，不如在 Btn() 里按底色亮度自动选字色 ——
     同样的问题在「倒」（ACC）和本生灯那个「点燃」（WARN）上也存在，
     以后再加浅色按钮也不用记得手动改。
     判据：感知亮度 > 0.62 用深色字，否则用浅色字。
   ===================================================================== */
const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let c = fs.readFileSync(U, 'utf8');
let n = 0;

if (!c.includes('static Color InkOn(')) {
  c = c.replace('    Button Btn(Transform parent, string txt, Action onClick, Color? bg = null, int size = 16) {',
`    /// <summary>按底色亮度选字色：浅底用深字，深底用浅字。
    /// 感知亮度用 0.299R+0.587G+0.114B —— 比直接平均更接近人眼。</summary>
    static Color InkOn(Color bg) {
        float lum = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
        return lum > 0.62f ? new Color(0.06f, 0.08f, 0.11f) : INK;
    }

    Button Btn(Transform parent, string txt, Action onClick, Color? bg = null, int size = 16) {`);
  n++;
}

/* Btn 里用 InkOn */
const oldLine = `        var t = Label(go.transform, txt, size, INK, TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(2,0), new Vector2(-2,0));`;
const newLine = `        var t = Label(go.transform, txt, size, InkOn(img.color), TextAnchor.MiddleCenter,
                        Vector2.zero, Vector2.one, new Vector2(2,0), new Vector2(-2,0));`;
if (c.includes(oldLine)) { c = c.replace(oldLine, newLine); n++; }
else console.log('  ✗ Btn 里的 Label 行没命中');

/* MakeTagBtn / MakeEqBtn 之类也走同一个辅助？先看它们是否单独设了颜色 */
const others = c.match(/Label\([^)]*,\s*INK,\s*TextAnchor\.MiddleCenter/g);
console.log('  其它用 INK 居中文字的地方:', others ? others.length : 0);

fs.writeFileSync(U, c, 'utf8');
console.log('改动处数:', n);
