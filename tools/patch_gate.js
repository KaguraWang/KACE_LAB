const fs = require('fs');
let n = 0;
const P = (p, a, b, label) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 50).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ============ 1) PetMenu：聊天入口按解锁状态决定，并加跳过/继续 ============ */
const M = 'D:/ChemLabUnity/Assets/Scripts/Game/PetMenu.cs';
P(M, `        var items = new List<(string, Action)> {
            ("重新开始教程 · 离子鉴定实验", () => StartTutorial(TutorialKind.IonTest)),
            ("重新开始教程 · 焓变计算器",   () => StartTutorial(TutorialKind.BornHaber)),
            ("打开聊天（AI 助手）",          () => PetChat.Toggle()),
            ("——", null),`,
`        bool unlocked = PetProgress.AiUnlocked;
        var items = new List<(string, Action)>();

        /* 教程没走完：先给「继续 / 跳过」，聊天是灰的 */
        if (PetTutorial.Running) {
            items.Add(("▶ 继续教程（" + PetTutorial.StepTitle + "）", () => PetTutorial.Next()));
            items.Add(("⏭ 跳过教程并解锁 AI 助手", () => PetTutorial.Skip()));
            items.Add(("——", null));
        } else if (!PetProgress.LabDone) {
            items.Add(("▶ 开始教程 · 离子鉴定实验", () => StartTutorial(TutorialKind.IonTest)));
            items.Add(("⏭ 直接跳过教程并解锁 AI 助手", () => { PetProgress.FinishLab(true); PetView.Instance?.Say("好，那 AI 助手给你解锁了。想学的话随时右键我。", 8f); }));
            items.Add(("——", null));
        } else {
            items.Add(("重新开始教程 · 离子鉴定实验", () => StartTutorial(TutorialKind.IonTest)));
            items.Add(("重新开始教程 · 焓变计算器",   () => StartTutorial(TutorialKind.BornHaber)));
        }

        items.Add(unlocked ? ("打开聊天（AI 助手）", () => PetChat.Toggle())
                           : ("🔒 聊天（教程完成后解锁）", () => PetView.Instance?.Say(
                               "聊天要等你把教程走完、或者跳过之后才解锁哦。\n右键我再选一次「跳过教程」就可以立刻用。", 9f)));
        items.Add(("——", null));`);

/* ============ 2) PetChat：未解锁时不打开，并给提示 ============ */
const C = 'D:/ChemLabUnity/Assets/Scripts/Game/PetChat.cs';
P(C, `    public static PetChat Toggle() {
        if (Instance == null) {`,
`    public static PetChat Toggle() {
        /* AI 助手是教程的奖励：没走完（也没跳过）之前不给开 */
        if (!PetProgress.AiUnlocked) {
            PetView.Instance?.Say(
                "聊天还没解锁 —— 先把教程走完，或者右键我选「跳过教程」。\\n" +
                "（我不是故意为难你，教程里教的都是这软件的用法，你会用得上。）", 10f);
            return null;
        }
        if (Instance == null) {`);
P(C, `        if (Cfg == null) Cfg = AiConfig.Load();
        if (!Cfg.Ready) {
            Add(ChatMsg.Who.System,
                "还没配置 AI 接口。点标题栏的「设置」填入 baseURL 与 API Key 就能用了。\\n" +
                "（这不影响宠物和教程功能，它们不需要联网。）");
            return;
        }`,
`        PetProgress.ChatUsed = true;
        if (Cfg == null) Cfg = AiConfig.Load();
        if (!Cfg.Ready) {
            Add(ChatMsg.Who.System,
                "还没配置 AI 接口。这是学生分发版的默认状态 —— AI 功能需要你自己填。\\n" +
                "点标题栏的「设置」填入 baseURL 与 API Key 就能用了。\\n" +
                "（宠物、教程、实验台、焓变计算器全都不受影响，都不需要联网。）");
            return;
        }`);

/* ============ 3) LabBootstrap：教程进度条 + 首启判断改用 PetProgress ============ */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
P(B, '        if (!PetTutorial.SeenLab) PetTutorial.StartDelayed(TutorialKind.IonTest, 3.0f);',
     '        if (!PetProgress.LabDone) PetTutorial.StartDelayed(TutorialKind.IonTest, 3.0f);');

const bar = `
    /* ================================================================
       教程进度条：顶在画面上方，显示到第几步、以及「下一步 / 跳过」
       ================================================================ */
    RectTransform _tutBar; Text _tutText;
    public void BuildTutorialBar() {
        if (_tutBar != null) { _tutBar.gameObject.SetActive(true); RefreshTutorialBar(); return; }
        var go = new GameObject("TutorialBar", typeof(RectTransform));
        go.transform.SetParent(_canvas.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.10f, 0.16f, 0.22f, 0.97f);
        _tutBar = (RectTransform)go.transform;
        _tutBar.anchorMin = new Vector2(0.5f, 1); _tutBar.anchorMax = new Vector2(0.5f, 1);
        _tutBar.pivot = new Vector2(0.5f, 1);
        _tutBar.anchoredPosition = new Vector2(0, -6);
        _tutBar.sizeDelta = new Vector2(620, 34);

        var tgo = new GameObject("T", typeof(RectTransform));
        tgo.transform.SetParent(_tutBar, false);
        _tutText = tgo.AddComponent<Text>();
        _tutText.font = LabUI.UiFont; _tutText.fontSize = 14;
        _tutText.color = new Color(0.88f, 0.95f, 1f);
        _tutText.alignment = TextAnchor.MiddleLeft;
        _tutText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _tutText.raycastTarget = false;
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14, 0); trt.offsetMax = new Vector2(-210, 0);

        MakeTutBtn("下一步 ▶", -104, () => PetTutorial.Next());
        MakeTutBtn("跳过教程 ⏭", -8, () => PetTutorial.Skip());
        RefreshTutorialBar();
    }
    void MakeTutBtn(string label, float x, System.Action act) {
        var go = new GameObject("B", typeof(RectTransform));
        go.transform.SetParent(_tutBar, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.20f, 0.32f, 0.42f, 1f);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(94, 26);
        var b = go.AddComponent<Button>(); b.targetGraphic = img;
        b.onClick.AddListener(() => act());
        var tgo = new GameObject("T", typeof(RectTransform));
        tgo.transform.SetParent(rt, false);
        var t = tgo.AddComponent<Text>();
        t.font = LabUI.UiFont; t.fontSize = 12; t.color = Color.white;
        t.text = label; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
    }
    public void RefreshTutorialBar() {
        if (_tutBar == null || _tutText == null) return;
        if (!PetTutorial.Running) { _tutBar.gameObject.SetActive(false); return; }
        _tutBar.gameObject.SetActive(true);
        int cur = Mathf.Min(PetTutorial.StepIndex + 1, PetTutorial.StepCount);
        string kind = PetTutorial.Current == TutorialKind.IonTest ? "实验教程" : "计算器教程";
        string hint = PetTutorial.WaitingOnUser ? "  ← 等你操作" : "";
        _tutText.text = $"{kind}  {cur} / {PetTutorial.StepCount}   {PetTutorial.StepTitle}{hint}";
    }
    public void HideTutorialBar() { if (_tutBar != null) _tutBar.gameObject.SetActive(false); }
`;
if (!B || !fs.readFileSync(B, 'utf8').includes('BuildTutorialBar')) {
  const lines = fs.readFileSync(B, 'utf8').split('\n');
  let idx = -1;
  for (let i = 0; i < lines.length; i++) if (lines[i].includes('public void SyncViews()')) { idx = i; break; }
  if (idx >= 0) {
    lines.splice(idx, 0, bar);
    fs.writeFileSync(B, lines.join('\n'), 'utf8'); n++;
    console.log('  ✓ 教程进度条已插入');
  } else console.log('  ✗ 找不到 SyncViews');
}
console.log('补丁处数:', n);
