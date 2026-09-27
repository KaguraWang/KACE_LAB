/* =====================================================================
   两处修正
   1) 标注离线的距离不够
      Off=30 而标注半高 20 —— 标注边缘离曲线只剩 10px，视觉上就是压着线。
      改成「半高 + 14」的净空，并把「离别的线太近」的惩罚加重。
   2) 计算器打开时阿侬挡住节点
      她平时停在右下角，而计算器的节点图占中间偏下，正好被她盖住。
      进计算器时把她挪到左下角，出来再回原位。
   ===================================================================== */
const fs = require('fs');
let n = 0;

/* ---- 1) 标注净空 ---- */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let e = fs.readFileSync(E, 'utf8');
e = e.replace('        const float Off = 30f;                // 距曲线的基准偏移',
              '        const float Off = LH * 0.5f + 16f;   // 净空 = 标注半高 + 16，边缘才不会压着线');
e = e.replace('                score -= Mathf.Max(0f, 26f - minD) * 0.8f;',
              '                /* 离曲线太近 = 视觉上压线，罚得重一点 */\n                score -= Mathf.Max(0f, (LH * 0.5f + 10f) - minD) * 3f;');
fs.writeFileSync(E, e, 'utf8'); n++;
console.log('  ✓ 标注净空已加大');

/* ---- 2) 计算器打开时把宠物挪开 ---- */
let c = fs.readFileSync(E, 'utf8');
if (!c.includes('PetView.Instance?.SetHomeForCalc')) {
  c = c.replace(`    static void SetLabVisible(bool labOn) {
        LabBootstrap.CalculatorActive = !labOn;
        if (LabBootstrap.Instance != null) LabBootstrap.Instance.SetLabUiVisible(labOn);
    }`,
`    static void SetLabVisible(bool labOn) {
        LabBootstrap.CalculatorActive = !labOn;
        if (LabBootstrap.Instance != null) LabBootstrap.Instance.SetLabUiVisible(labOn);
        /* 阿侬平时停在右下角，正好会盖住计算器下方的节点 —— 进计算器时挪到左下角 */
        PetView.Instance?.SetHomeForCalc(!labOn);
    }`);
  fs.writeFileSync(E, c, 'utf8'); n++;
  console.log('  ✓ 计算器里宠物让位');
}

/* ---- 3) PetView 加 SetHomeForCalc ---- */
const P = 'D:/ChemLabUnity/Assets/Scripts/Game/PetView.cs';
let p = fs.readFileSync(P, 'utf8');
if (!p.includes('SetHomeForCalc')) {
  p = p.replace('    public void SetScale(float s) {',
`    /// <summary>进/出计算器时换个落脚点。
    /// 计算器的节点图在中间偏下，她原来的右下角位置会挡住节点。 </summary>
    public void SetHomeForCalc(bool inCalc) {
        Home = inCalc ? new Vector2(-720f, -330f) : new Vector2(-120f, -250f);
        ApplyLayout();
        ShutUp();
    }

    public void SetScale(float s) {`);
  fs.writeFileSync(P, p, 'utf8'); n++;
  console.log('  ✓ PetView.SetHomeForCalc 已加');
}

console.log('改动处数:', n);
