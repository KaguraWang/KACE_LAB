/* =====================================================================
   教程进度条：独立一块 Overlay 画布
     之前挂在 LabUI 的画布下，被方程式标签挡住了（世界空间画布的排序
     跟层级无关）。宠物用的是独立 Overlay 画布，一直显示正常 ——
     所以进度条也照这个路子来，绕开层级问题。
   ===================================================================== */
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;

namespace ChemLab.Game {

public class TutorialBar : MonoBehaviour {
    public static TutorialBar Instance;

    Canvas _canvas;
    RectTransform _root, _panel;
    Text _label;

    public static TutorialBar Show() {
        if (Instance != null) { Instance.gameObject.SetActive(true); Instance.Refresh(); return Instance; }
        var host = LabBootstrap.Instance != null ? LabBootstrap.Instance.gameObject : null;
        if (host == null) return null;
        var go = new GameObject("TutorialBar");
        go.transform.SetParent(host.transform, false);
        Instance = go.AddComponent<TutorialBar>();
        Instance.Build();
        return Instance;
    }

    void Build() {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = PetView.SortingOrder - 10;   // 低于宠物，高于实验台界面
        var sc = gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(LabUI.DesignW, LabUI.DesignH);
        sc.matchWidthOrHeight = 0.5f;
        var gr = gameObject.AddComponent<GraphicRaycaster>();
        gr.enabled = true;
        _root = (RectTransform)transform;

        var pgo = new GameObject("Panel", typeof(RectTransform));
        pgo.transform.SetParent(_root, false);
        var img = pgo.AddComponent<Image>();
        img.color = new Color(0.09f, 0.15f, 0.21f, 0.97f);
        _panel = (RectTransform)pgo.transform;
        _panel.anchorMin = new Vector2(0.5f, 1); _panel.anchorMax = new Vector2(0.5f, 1);
        _panel.pivot = new Vector2(0.5f, 1);
        _panel.anchoredPosition = new Vector2(0, -52);     // 让开顶栏
        _panel.sizeDelta = new Vector2(720, 36);

        var tgo = new GameObject("Label", typeof(RectTransform));
        tgo.transform.SetParent(_panel, false);
        _label = tgo.AddComponent<Text>();
        _label.font = LabUI.UiFont; _label.fontSize = 14;
        _label.color = new Color(0.9f, 0.96f, 1f);
        _label.alignment = TextAnchor.MiddleLeft;
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        _label.raycastTarget = false;
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16, 0); trt.offsetMax = new Vector2(-220, 0);

        Btn("下一步 ▶", -112, () => PetTutorial.Next(), new Color(0.20f, 0.40f, 0.30f));
        Btn("跳过教程 ⏭", -8, () => PetTutorial.Skip(), new Color(0.40f, 0.26f, 0.26f));
        Refresh();
    }

    void Btn(string label, float x, System.Action act, Color col) {
        var go = new GameObject("B", typeof(RectTransform));
        go.transform.SetParent(_panel, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(100, 28);
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

    public void Refresh() {
        if (_label == null) return;
        if (!PetTutorial.Running) { gameObject.SetActive(false); return; }
        gameObject.SetActive(true);
        int cur = Mathf.Min(PetTutorial.StepIndex + 1, PetTutorial.StepCount);
        string kind = PetTutorial.Current == TutorialKind.IonTest ? "实验教程" : "计算器教程";
        string hint = PetTutorial.WaitingOnUser ? "   ← 等你操作" : "";
        string source = kind + "  " + cur + " / " + PetTutorial.StepCount + "   " + PetTutorial.StepTitle + hint;
        string kindEn = PetTutorial.Current == TutorialKind.IonTest ? "Lab tutorial" : "Calculator tutorial";
        string target = kindEn + "  " + cur + " / " + PetTutorial.StepCount + "   " + PetTutorial.EnglishStepTitle
            + (PetTutorial.WaitingOnUser ? "   ← waiting for you" : "");
        Localization.RegisterTranslation(source, target);
        _label.text = source;
        _panel.SetAsLastSibling();
    }

    public void Hide() { gameObject.SetActive(false); }

    /// <summary>供诊断用</summary>
    public string Audit() {
        if (_panel == null) return "[TutAudit] 进度条未创建";
        var a = _panel.TransformPoint(_panel.rect.min);
        var b = _panel.TransformPoint(_panel.rect.max);
        return string.Format("[TutAudit] 激活={0} 屏幕 x {1:F0}..{2:F0} y {3:F0}..{4:F0} 文案「{5}」",
            gameObject.activeInHierarchy,
            Mathf.Min(a.x, b.x), Mathf.Max(a.x, b.x),
            Mathf.Min(a.y, b.y), Mathf.Max(a.y, b.y),
            _label != null ? _label.text : "(无)");
    }
}

}
