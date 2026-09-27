/* =====================================================================
   自适应对话气泡
     之前的问题：
       先读 _bubbleText.preferredHeight，但那时气泡宽度还停在旧值（或 0），
       量出来的要么是「一整行」的高度、要么是错的，于是框永远不够高，
       字就冒出来了。
     正确顺序：
       ① 先按文字长度决定气泡宽度
       ② 把 Text 的矩形宽度设成那个宽度（并开启换行）
       ③ 用 GetPreferredHeight(文字, 以该宽度生成的设置) 量真实高度
       ④ 气泡高度 = 文字高度 + 上下内边距
   ===================================================================== */
const fs = require('fs');
const P = 'D:/ChemLabUnity/Assets/Scripts/Game/PetView.cs';
let c = fs.readFileSync(P, 'utf8');
let n = 0;

/* 1) 构造气泡时把 Text 的换行与溢出设对，并记录内边距 */
c = c.replace(`        _bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _bubbleText.verticalOverflow = VerticalWrapMode.Overflow;`,
`        _bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _bubbleText.verticalOverflow = VerticalWrapMode.Overflow;
        _bubbleText.supportRichText = true;`);
n++;

/* 2) 换掉整个 Say 的尺寸计算 */
const oldSay = c.slice(c.indexOf('    public void Say(string text, float seconds = 6f) {'),
                       c.indexOf('    public void ShutUp()'));
const newSay = `    /* 气泡内边距与宽度范围 */
    const float PadX = 14f, PadY = 12f;
    const float MinW = 170f, MaxW = 420f;
    const float LineH = 19f;

    /// <summary>说话。气泡宽高都按文字自适应 —— 先定宽，再量高。</summary>
    public void Say(string text, float seconds = 6f) {
        if (_bubble == null || _bubbleText == null) return;
        _bubbleText.text = text;

        /* ① 先按字数估一个宽度：短句窄一点，长句宽一点，但不超上限 */
        int chars = 0;
        foreach (var ch in text) if (ch != '\\n') chars++;
        int hardLines = 1;
        foreach (var ch in text) if (ch == '\\n') hardLines++;
        /* 中文按一个字约 15px 估宽，再按最长的一行收窄 */
        float wantW = Mathf.Clamp(chars * 15f / Mathf.Max(1, hardLines) + PadX * 2 + 24f, MinW, MaxW);
        /* 宽了不好看 —— 上限也跟字数走 */
        wantW = Mathf.Min(wantW, 90f + chars * 9.5f);

        /* ② 把 Text 设成这个宽度，才能量出正确的换行高度 */
        var trt = _bubbleText.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(PadX, PadY);
        trt.offsetMax = new Vector2(-PadX, -PadY);
        _bubble.sizeDelta = new Vector2(wantW, 40f);
        Canvas.ForceUpdateCanvases();

        /* ③ 用真实宽度量高度 */
        float innerW = Mathf.Max(20f, wantW - PadX * 2f);
        var gs = _bubbleText.GetGenerationSettings(new Vector2(innerW, 0f));
        gs.generateOutOfBounds = true;
        gs.horizontalOverflow = HorizontalWrapMode.Wrap;
        gs.verticalOverflow = VerticalWrapMode.Overflow;
        float textH = _bubbleText.cachedTextGeneratorForLayout
                        .GetPreferredHeight(text, gs) / _bubbleText.pixelsPerUnit;

        /* ④ 定高 */
        float wantH = Mathf.Max(LineH + PadY * 2f, textH + PadY * 2f + 2f);
        _bubble.sizeDelta = new Vector2(wantW, wantH);

        /* 气泡挂在宠物左上方，位置随尺寸重算 */
        var petRt = (RectTransform)transform.Find("Sprite");
        float pw = petRt != null ? petRt.sizeDelta.x : 192f;
        float ph = petRt != null ? petRt.sizeDelta.y : 208f;
        _bubble.anchoredPosition = Home + new Vector2(-pw * 0.5f - 10f, ph * 0.18f + wantH * 0.5f);

        _bubble.gameObject.SetActive(true);
        _bubbleUntil = LabClock() + seconds;
        _bubble.SetAsLastSibling();
        Play("wave");
    }

`;
if (c.includes('    public void Say(string text, float seconds = 6f) {')) {
  c = c.replace(oldSay, newSay);
  n++;
} else console.log('  ✗ 找不到 Say');

fs.writeFileSync(P, c, 'utf8');
console.log('改动处数:', n);
