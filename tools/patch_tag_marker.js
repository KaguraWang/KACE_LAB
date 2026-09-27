const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 48).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ---- 1) 用中文标记代替 ↓/↑（这两个字形缺失，渲染成方块还会盖住数字） ---- */
P('D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs',
`            string tag = s.IsPrecipitate ? "↓" : (s.IsGas ? "↑" : "");`,
`            /* 用中文标记而不是 ↓/↑ —— 这两个字形在系统字体里缺失，
               会渲染成方块并盖住后面的数字（看起来像把 15.00 读成 5.00）。 */
            string tag = s.IsPrecipitate ? "沉 " : (s.IsGas ? "气 " : "");`);

/* ---- 2) SUT 输出里带上标签行，便于精确核对而不是靠看截图 ---- */
P('D:/ChemLabUnity/Assets/Editor/SpecRunner.cs',
`        o.Put("tests", tarr);
        o.Put("pop", MiniJson.Num(v.Fx.Pop));`,
`        o.Put("tests", tarr);
        o.Put("pop", MiniJson.Num(v.Fx.Pop));

        /* 标签两视图的实际内容 —— 便于精确核对，不用去数截图上的小字 */
        var molArr = MiniJson.Arr();
        foreach (var s in Chem.VesselView.Lines(v, true))
            molArr.Push(MiniJson.Str($"{s.Formula}|{s.Mol:G6}|{s.Conc:G6}"));
        o.Put("tag_molecular", molArr);
        var ionArr = MiniJson.Arr();
        foreach (var s in Chem.VesselView.Lines(v, false))
            ionArr.Push(MiniJson.Str($"{s.Formula}|{s.Mol:G6}|{s.Conc:G6}"));
        o.Put("tag_ionic", ionArr);`);

console.log('补丁处数:', n);
