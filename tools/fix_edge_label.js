/* =====================================================================
   焓变连线的标注位置
     之前：标注固定在曲线中点 + (0,24)，于是压在了节点上。
     现在：沿曲线的**法向**偏移，并根据标注自身尺寸避开两侧。
   ===================================================================== */
const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(E, 'utf8');
let n = 0;

const oldBlk = `            var t = EnsureLabel(e);
            string eq = EdgeEquation(e);
            string dh = e.Source == EnthalpySource.Unknown ? "ΔH = ?"
                      : $"ΔH = {e.DH:F0} kJ/mol";
            t.text = eq.Length > 0 ? eq + "\\n" + dh : dh;
            t.fontSize = 13;
            var trt = (RectTransform)t.transform;
            trt.anchoredPosition = Quadratic(p0, ctrl, p1, 0.5f) + new Vector2(0, 24);`;

const newBlk = `            var t = EnsureLabel(e);
            string eq = EdgeEquation(e);
            string dh = e.Source == EnthalpySource.Unknown ? "ΔH = ?"
                      : $"ΔH = {e.DH:F0} kJ/mol";
            t.text = eq.Length > 0 ? eq + "\\n" + dh : dh;
            t.fontSize = 13;
            var trt = (RectTransform)t.transform;
            /* 标注沿曲线的法向偏移，不能固定往 Y 上抬 —— 那样子弹到节点上。
               法向取「曲线中点处的切线」转 90°；偏移量取固定值，
               保证标注始终在曲线的一侧而不是压在线上。 */
            Vector2 midPt = Quadratic(p0, ctrl, p1, 0.5f);
            Vector2 tan = (Quadratic(p0, ctrl, p1, 0.56f) - Quadratic(p0, ctrl, p1, 0.44f));
            Vector2 nrm2 = tan.sqrMagnitude > 1e-6f
                         ? new Vector2(-tan.y, tan.x).normalized
                         : new Vector2(0, 1);
            /* 法向朝上/朝右更符合阅读习惯 */
            if (nrm2.y < 0) nrm2 = -nrm2;
            trt.sizeDelta = new Vector2(190f, 40f);
            trt.anchoredPosition = midPt + nrm2 * 26f;`;

if (c.includes(oldBlk)) { c = c.replace(oldBlk, newBlk); n++; console.log('  ✓ 标注改为法向偏移'); }
else console.log('  ✗ 锚点未命中');

/* EnsureLabel 的尺寸也要跟上，否则多行文字溢出 */
c = c.replace(`        return MkText(e.Rt, "", 14, Color.white, TextAnchor.MiddleCenter,
                      new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);`,
`        var t = MkText(e.Rt, "", 13, Color.white, TextAnchor.MiddleCenter,
                       new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = (RectTransform)t.transform;
        rt.sizeDelta = new Vector2(190f, 40f);
        return t;`);
n++;

/* 示例场景把节点摆得开一点，标注才不挤 */
c = c.replace(`        var a = AddNode(new Vector2(-320, 120), "C", "C");       // 石墨
        var b = AddNode(new Vector2(0, 200), "CO2", "CO2");
        var c = AddNode(new Vector2(0, -60), "CO", "CO");`,
`        var a = AddNode(new Vector2(-340, 60), "C", "C");        // 石墨
        var b = AddNode(new Vector2(40, 250), "CO2", "CO2");
        var c = AddNode(new Vector2(40, -140), "CO", "CO");`);

fs.writeFileSync(E, c, 'utf8');
console.log('改动处数:', n);
