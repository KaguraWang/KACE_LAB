const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

/* 1) 修变量名：示例里的节点变量 c 与外部无关，但被 Connect 引用成 co */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('var c = AddNode(new Vector2(0, -60)')) {
    lines[i] = lines[i].replace('var c = AddNode', 'var co = AddNode');
    console.log('  改第', i + 1, '行');
  }
}
fs.writeFileSync(E, lines.join('\n'), 'utf8');

/* 2) 标注用法向偏移（按行找，字符串替换不可靠） */
let c2 = fs.readFileSync(E, 'utf8');
const a = c2.indexOf('            var t = EnsureLabel(e);');
if (a < 0) { console.log('✗ 找不到标注块'); process.exit(1); }
const bMark = 'trt.anchoredPosition = Quadratic(p0, ctrl, p1, 0.5f) + new Vector2(0, 24);';
const b = c2.indexOf(bMark);
if (b < 0) { console.log('✗ 找不到定位行'); process.exit(1); }

const nw = `            var t = EnsureLabel(e);
            string eq = EdgeEquation(e);
            string dh = e.Source == EnthalpySource.Unknown ? "ΔH = ?"
                      : string.Format("ΔH = {0:F0} kJ/mol", e.DH);
            t.text = eq.Length > 0 ? eq + "\\n" + dh : dh;
            t.fontSize = 13;
            var trt = (RectTransform)t.transform;
            /* 标注沿曲线法向偏移。固定往 Y 上抬的话会压在节点上 ——
               法向取「曲线中点附近的切线」转 90°，再统一取朝上的一侧。 */
            Vector2 midPt = Quadratic(p0, ctrl, p1, 0.5f);
            Vector2 tan = Quadratic(p0, ctrl, p1, 0.56f) - Quadratic(p0, ctrl, p1, 0.44f);
            Vector2 nrm2 = tan.sqrMagnitude > 1e-6f
                         ? new Vector2(-tan.y, tan.x).normalized
                         : new Vector2(0, 1);
            if (nrm2.y < 0) nrm2 = -nrm2;
            trt.sizeDelta = new Vector2(190f, 40f);
            trt.anchoredPosition = midPt + nrm2 * 26f;`;

c2 = c2.slice(0, a) + nw + c2.slice(b + bMark.length);
fs.writeFileSync(E, c2, 'utf8');
console.log('✓ 标注改为法向偏移');

/* 3) 示例节点摆开 */
c2 = fs.readFileSync(E, 'utf8');
c2 = c2.replace('var a = AddNode(new Vector2(-320, 120), "C", "C");', 'var a = AddNode(new Vector2(-340, 60), "C", "C");');
c2 = c2.replace('var b = AddNode(new Vector2(0, 200), "CO2", "CO2");', 'var b = AddNode(new Vector2(40, 250), "CO2", "CO2");');
c2 = c2.replace('var co = AddNode(new Vector2(0, -60), "CO", "CO");', 'var co = AddNode(new Vector2(40, -140), "CO", "CO");');
fs.writeFileSync(E, c2, 'utf8');
console.log('✓ 示例节点已摆开');
