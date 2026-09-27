/* =====================================================================
   阿侬 · UI 层宠物
     · 挂在一块 sortingOrder 最高的画布上，永远在最上层
     · 左键点一下：冒个随机问候 / 中断当前动作
     · 左键拖动：跟着鼠标走，走路时切左右朝向动画
     · 右键：弹出选单（重新开始教程 / 聊天 / 切换大小 / 隐藏）
     · 说话：头顶冒对话气泡，自动换行与定时消失
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;
using UnityEngine.EventSystems;

namespace ChemLab.Game {

public class PetView : MonoBehaviour {
    public static PetView Instance;

    /* ---------------- 配置 ---------------- */
    public const string PetName = "阿侬";
    /// <summary>UI 层的排序号：比实验台界面（0–100）和焓变计算器（100）都高</summary>
    public const int SortingOrder = 900;

    public float Scale = 1.0f;                 // 0.7 小 / 1.0 中 / 1.35 大
    public Vector2 Home;   // 相对画布中心的局部坐标；默认位置按窗口尺寸计算
    bool _dockAtLabCorner = true;

    Image _img;
    RectTransform _rt, _root;
    Canvas _canvas;
    RectTransform _bubble;
    Text _bubbleText;
    Image _bubbleBg;

    string _anim = "idle";
    double _animT;

    // 窗口级回归测试读取这些只读状态，验证气泡确实显示且按文本重新布局。
    public bool BubbleVisible => _bubble != null && _bubble.gameObject.activeInHierarchy;
    public Vector2 BubbleSize => _bubble != null ? _bubble.sizeDelta : Vector2.zero;
    public int BubbleTextLength => _bubbleText != null && _bubbleText.text != null ? _bubbleText.text.Length : 0;
    int _frame;
    bool _dragging;
    Vector2 _dragOff;
    double _bubbleUntil;
    double _nextIdleChat = 60;                 // 隔一阵自己说句话

    /* ---------------- 创建 ---------------- */
    public static PetView Show(GameObject host, Font font) {
        if (Instance != null) return Instance;
        var go = new GameObject("PetView");
        go.transform.SetParent(host.transform, false);
        var p = go.AddComponent<PetView>();
        p.Build(font);
        Instance = p;
        return p;
    }

    void Build(Font font) {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = SortingOrder;      // 永远在最上层
        var sc = gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(LabUI.DesignW, LabUI.DesignH);
        sc.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        _root = (RectTransform)transform;

        /* 对话气泡（在宠物左侧，避免挡住她） */
        var bg = new GameObject("Bubble", typeof(RectTransform));
        bg.transform.SetParent(_root, false);
        _bubbleBg = bg.AddComponent<Image>();
        _bubbleBg.color = LabUiTheme.Panel;
        _bubbleBg.raycastTarget = false;
        _bubble = (RectTransform)bg.transform;
        _bubble.anchorMin = _bubble.anchorMax = new Vector2(0.5f, 0.5f);
        _bubble.pivot = new Vector2(1f, 0f);
        _bubble.sizeDelta = new Vector2(280, 70);
        var tgo = new GameObject("T", typeof(RectTransform));
        tgo.transform.SetParent(_bubble, false);
        _bubbleText = tgo.AddComponent<Text>();
        _bubbleText.font = font; _bubbleText.fontSize = 15;
        _bubbleText.color = LabUiTheme.Ink;
        _bubbleText.alignment = TextAnchor.UpperLeft;
        _bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _bubbleText.verticalOverflow = VerticalWrapMode.Overflow;
        _bubbleText.supportRichText = true;
        _bubbleText.raycastTarget = false;
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12, 8); trt.offsetMax = new Vector2(-12, -8);
        _bubble.gameObject.SetActive(false);

        /* 宠物本体 */
        var go = new GameObject("Sprite", typeof(RectTransform));
        go.transform.SetParent(_root, false);
        _img = go.AddComponent<Image>();
        _img.sprite = PetSprite.Cell(0, 0);
        _img.preserveAspect = true;
        _img.raycastTarget = true;
        // The 192x208 sprite cells contain substantial transparent padding.
        // Let clicks through those pixels so nearby 3D test tools remain usable.
        // PetSprite keeps both source textures readable for this alpha hit test.
        _img.alphaHitTestMinimumThreshold = 0.15f;
        _rt = (RectTransform)go.transform;
        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);

        var drag = go.AddComponent<PetDrag>();
        drag.pet = this;

        ApplyLayout();
        Play("wave");
    }

    public void ApplyLayout() {
        if (_rt == null) return;
        _rt.sizeDelta = new Vector2(PetSprite.CellW * Scale, PetSprite.CellH * Scale);
        if (_dockAtLabCorner) Home = LabCorner();
        _rt.anchoredPosition = Home;
        if (_bubble != null)
            _bubble.anchoredPosition = Home + new Vector2(-_rt.sizeDelta.x * 0.5f - 8f, _rt.sizeDelta.y * 0.30f);
        ClampBubble();
    }

    Vector2 LabCorner() => new Vector2(
        _root.rect.width * .5f - _rt.sizeDelta.x * .5f - 180f,
        -_root.rect.height * .5f + _rt.sizeDelta.y * .5f + 18f);

    public void ReturnToLabCorner() {
        _dockAtLabCorner = true;
        ApplyLayout();
    }

    /* ---------------- 动画 ---------------- */
    public void Play(string name) {
        if (!PetSprite.Anims.ContainsKey(name)) name = "idle";
        _anim = name; _animT = 0; _frame = 0;
    }
    public string CurrentAnim => _anim;

    void Update() {
        // Keep the default dock inside resized windows. User-dragged positions
        // remain free; no animation frame can move the pet back over the tools.
        if (_dockAtLabCorner && _rt != null && (Home-LabCorner()).sqrMagnitude>.01f) ApplyLayout();
        if (!PetSprite.Ready) return;
        double dt = Mathf.Min(0.1f, Time.unscaledDeltaTime);
        var a = PetSprite.Anims[_anim];
        /* 帧数由精灵扫描得出，不是写死的 —— 空格子不会被算进循环 */
        int total = Mathf.Max(1, PetSprite.FrameCount(a.Row));
        _animT += dt * a.Fps;
        if (_animT >= total) {
            if (a.Loop) _animT -= total * Mathf.Floor((float)(_animT / total));
            else { Play(string.IsNullOrEmpty(a.Next) ? "idle" : a.Next); return; }
        }
        int f = Mathf.Clamp((int)_animT, 0, total - 1);
        if (f != _frame || _img.sprite == null) {
            _frame = f;
            var sp = PetSprite.Frame(a.Row, f);      // 按有效帧序号取
            if (sp != null) _img.sprite = sp;
        }

        /* 气泡到点自动收 */
        if (_bubble != null && _bubble.gameObject.activeSelf && LabClock() > _bubbleUntil)
            _bubble.gameObject.SetActive(false);

        /* 隔一阵自己说句话（只在她闲着的时候） */
        if (LabClock() > _nextIdleChat && !_dragging && _bubble != null && !_bubble.gameObject.activeSelf) {
            _nextIdleChat = LabClock() + 120;
            Say(IdleChatter());
        }
    }


    double LabClock() { return Time.unscaledTimeAsDouble; }

    static readonly string[] Chatter = {
        "需要我帮忙搭个实验吗？",
        "右键我可以重新开始教程～",
        "焓变计算器在顶栏，要不要试试？",
        "我在这儿陪着你就好。",
        "点住我可以把我拖到别处哦。",
    };
    string IdleChatter() { return Chatter[Random.Range(0, Chatter.Length)]; }

    /* ---------------- 说话 ---------------- */
    /* 气泡内边距与宽度范围 */
    const float PadX = 14f, PadY = 12f;
    const float MinW = 170f, MaxW = 420f;
    const float LineH = 19f;

    /// <summary>说话。气泡宽高都按文字自适应 —— 先定宽，再量高。</summary>
    public void Say(string text, float seconds = 6f) {
        if (_bubble == null || _bubbleText == null) return;
        Localization.Apply(_bubbleText, text);

        /* ① 先按字数估一个宽度：短句窄一点，长句宽一点，但不超上限 */
        int chars = 0;
        foreach (var ch in text) if (ch != '\n') chars++;
        int hardLines = 1;
        foreach (var ch in text) if (ch == '\n') hardLines++;
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
                        .GetPreferredHeight(_bubbleText.text, gs) / _bubbleText.pixelsPerUnit;

        /* ④ 定高 */
        float wantH = Mathf.Max(LineH + PadY * 2f, textH + PadY * 2f + 2f);
        _bubble.sizeDelta = new Vector2(wantW, wantH);

        /* 气泡挂在宠物左上方，位置随尺寸重算 */
        var petRt = (RectTransform)transform.Find("Sprite");
        float pw = petRt != null ? petRt.sizeDelta.x : 192f;
        float ph = petRt != null ? petRt.sizeDelta.y : 208f;
        _bubble.anchoredPosition = Home + new Vector2(-pw * 0.5f - 10f, ph * 0.18f + wantH * 0.5f);
        ClampBubble();

        _bubble.gameObject.SetActive(true);
        _bubbleUntil = LabClock() + seconds;
        _bubble.SetAsLastSibling();
        Play("wave");
    }

    public void ShutUp() { if (_bubble != null) _bubble.gameObject.SetActive(false); }
    void ClampBubble() {
        if(_bubble==null||_root==null)return;
        var corners=new Vector3[4];_bubble.GetWorldCorners(corners);
        Vector2 min=_root.InverseTransformPoint(corners[0]),max=_root.InverseTransformPoint(corners[2]);
        Rect bounds=_root.rect;Vector2 correction=Vector2.zero;
        if(min.x<bounds.xMin+8)correction.x=bounds.xMin+8-min.x;
        else if(max.x>bounds.xMax-8)correction.x=bounds.xMax-8-max.x;
        if(min.y<bounds.yMin+8)correction.y=bounds.yMin+8-min.y;
        else if(max.y>bounds.yMax-8)correction.y=bounds.yMax-8-max.y;
        _bubble.anchoredPosition+=correction;
    }

    /* ---------------- 交互入口（由 PetDrag 调用） ---------------- */
    public void OnLeftClick() {
        Say(Chatter[Random.Range(0, Chatter.Length)], 4f);
    }
    public void OnRightClick(Vector2 screenPos) {
        PetMenu.Show(this, screenPos);
    }
    public void SetDrag(bool on, Vector2 screenPos) {
        _dragging = on;
        if (on) {
            _dockAtLabCorner = false;
            // Screen pixels and CanvasScaler local units differ. Keep the
            // exact point grabbed inside the sprite under the pointer.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPos, null, out var local))
                _dragOff = Home - local;
            else _dragOff = Vector2.zero;
        } else {
            Play("idle");
            Home = _rt.anchoredPosition;         // 记住新位置
        }
    }
    public void DragTo(Vector2 screenPos) {
        if (!_dragging || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPos, null, out var local)) return;
        var next = local + _dragOff;
        if (Mathf.Abs(next.x - Home.x) > 0.001f) {
            var walk = next.x < Home.x ? "walkL" : "walkR";
            if (_anim != walk) Play(walk);
        }
        Home = next;
        ApplyLayout();
    }

    /// <summary>进/出计算器时换个落脚点。
    /// 计算器的节点图在中间偏下，她原来的右下角位置会挡住节点。 </summary>
    public void SetHomeForCalc(bool inCalc) {
        _dockAtLabCorner = !inCalc;
        if (inCalc) Home = new Vector2(-720f, -330f);
        ApplyLayout();
        ShutUp();
    }

    public void SetScale(float s) {
        Scale = Mathf.Clamp(s, 0.6f, 1.8f);
        ApplyLayout();
    }
    public void Hide() { gameObject.SetActive(false); }
    public void Unhide() { gameObject.SetActive(true); }
}

/* ---------------- 拖动 / 点击 ---------------- */
public class PetDrag : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
    public PetView pet;
    bool _moved;
    public void OnPointerClick(PointerEventData e) {
        if (_moved) { _moved = false; return; }   /* 拖过了不算点击，但要复位，否则拖一次之后永远点不动 */
        if (e.button == PointerEventData.InputButton.Right) pet.OnRightClick(e.position);
        else pet.OnLeftClick();
    }
    public void OnBeginDrag(PointerEventData e) { _moved = false; pet.SetDrag(true, e.pressPosition); pet.DragTo(e.position); }
    public void OnPointerDown(PointerEventData e) { _moved = false; }   /* 每次按下复位 */
    public void OnDrag(PointerEventData e) { _moved = true; pet.DragTo(e.position); }
    public void OnEndDrag(PointerEventData e) { pet.SetDrag(false, e.position); }
}

}
