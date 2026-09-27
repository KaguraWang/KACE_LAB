/* =====================================================================
   进入计算器时把实验台界面藏起来
     要求：不是拉低透明度，要真正不可交互 —— 所以用 SetActive(false)。
     返回时再恢复。3D 实验台本身不用管：计算器的背景是不透明的全屏层，
     而且输入也要挡住，否则在计算器里按 WASD 会去平移实验台的相机。
   ===================================================================== */
const fs = require('fs');
let n = 0;

/* ---- 1) LabBootstrap：开放显隐与「是否在计算器里」 ---- */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('SetLabUiVisible')) {
  b = b.replace('    public void RefreshTutorialBar() {',
`    /// <summary>实验台界面的显隐。
    /// 用 SetActive 而不是改透明度 —— 藏起来之后就不能再点了，
    /// 否则计算器背后会有一堆看不见但能点的按钮。 </summary>
    public void SetLabUiVisible(bool on) {
        if (_ui != null) _ui.gameObject.SetActive(on);
    }
    /// <summary>计算器是否正开着（开着时实验台的输入要全部忽略）</summary>
    public static bool CalculatorActive = false;

    public void RefreshTutorialBar() {`);
  fs.writeFileSync(B, b, 'utf8'); n++;
  console.log('  ✓ SetLabUiVisible 已加');
}

/* ---- 2) HandleInput 开头加守卫 ---- */
b = fs.readFileSync(B, 'utf8');
if (!b.includes('if (CalculatorActive) return;')) {
  b = b.replace(`    void HandleInput() {
        var m = Input.mousePosition;`,
`    void HandleInput() {
        /* 计算器开着时，实验台不接收任何输入 ——
           否则在计算器里按 WASD、滚轮会去动实验台的相机。 */
        if (CalculatorActive) return;

        var m = Input.mousePosition;`);
  fs.writeFileSync(B, b, 'utf8'); n++;
  console.log('  ✓ 输入守卫已加');
}

/* ---- 3) EnthalpyScene：开/关时同步实验台界面 ---- */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let e = fs.readFileSync(E, 'utf8');
if (!e.includes('SetLabUiVisible')) {
  /* Show 里：激活已有实例 */
  e = e.replace(`        if (Instance != null) {
            Instance.gameObject.SetActive(true);`,
`        if (Instance != null) {
            Instance.gameObject.SetActive(true);
            SetLabVisible(false);`);
  /* Show 里：新建实例后 */
  e = e.replace(`        s.Build(font);
        Instance = s;`,
`        s.Build(font);
        Instance = s;
        SetLabVisible(false);`);
  /* Back 里：恢复 */
  e = e.replace(`    void Back() { gameObject.SetActive(false); }`,
`    void Back() {
        gameObject.SetActive(false);
        SetLabVisible(true);      // 返回实验台时把界面还回来
    }

    /// <summary>切换实验台界面的显隐（并挡住它的输入）</summary>
    static void SetLabVisible(bool labOn) {
        LabBootstrap.CalculatorActive = !labOn;
        if (LabBootstrap.Instance != null) LabBootstrap.Instance.SetLabUiVisible(labOn);
    }`);
  fs.writeFileSync(E, e, 'utf8'); n++;
  console.log('  ✓ EnthalpyScene 已接入');
}

/* ---- 4) 教程切换场景那处也要跟着走 ---- */
e = fs.readFileSync(E, 'utf8');
if (!e.includes('public static void SetLabVisible')) {
  /* 如果上面第 3 步没命中（说明写法不同），单独补 */
  e = e.replace('    public static EnthalpyScene Show(GameObject host, Font font) {',
`    public static void SetLabVisible(bool labOn) {
        LabBootstrap.CalculatorActive = !labOn;
        if (LabBootstrap.Instance != null) LabBootstrap.Instance.SetLabUiVisible(labOn);
    }

    public static EnthalpyScene Show(GameObject host, Font font) {`);
  fs.writeFileSync(E, e, 'utf8'); n++;
}

fs.writeFileSync(E, e, 'utf8');
console.log('改动处数:', n);

/* 查验 */
const chk = fs.readFileSync(E, 'utf8');
console.log('  SetLabVisible 出现次数:', (chk.match(/SetLabVisible/g) || []).length);
const chkB = fs.readFileSync(B, 'utf8');
console.log('  CalculatorActive 出现次数:', (chkB.match(/CalculatorActive/g) || []).length);
