using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChemLab.Chem;

namespace ChemLab.Game {
// A small, separate overlay remains available in both laboratory and calculator.
public sealed class VersionInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler {
    public static VersionInfo Instance { get; private set; }
    public const string Author = "神楽風和";
    public const string DisplayVersion = "v0.0.2_SE_CESA";
    public const string LicenseMessage = "支持風和谢谢喵";
    RectTransform panel;
    Text contents;
    bool hovered, selected, english;
    public bool IsOpen => panel != null && panel.gameObject.activeSelf;
    public string DisplayText => contents != null ? contents.text : "";
    public Vector2 ButtonScreenPosition => RectTransformUtility.WorldToScreenPoint(null,
        ((RectTransform)transform).TransformPoint(((RectTransform)transform).rect.center));

    public static void Create(Transform host, Font font) {
        if (Instance != null) return;
        var root = new GameObject("VersionInfoLayer", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(host, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950; // Keep the information text above the pet sprite.
        root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        var button = new GameObject("VersionInfoButton", typeof(RectTransform), typeof(InfoCircle), typeof(Button));
        button.transform.SetParent(root.transform, false);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0);rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-18, 18);rect.sizeDelta = new Vector2(32, 32);
        button.GetComponent<Button>().targetGraphic = button.GetComponent<InfoCircle>();
        var icon = new GameObject("InformationGlyph", typeof(RectTransform), typeof(Text));
        icon.transform.SetParent(button.transform, false);
        var ir = (RectTransform)icon.transform;ir.anchorMin = Vector2.zero;ir.anchorMax = Vector2.one;
        ir.offsetMin = ir.offsetMax = Vector2.zero;
        var glyph = icon.GetComponent<Text>();glyph.font = font;glyph.text = "i";
        glyph.fontSize = 21;glyph.fontStyle = FontStyle.Bold;glyph.alignment = TextAnchor.MiddleCenter;
        glyph.color = LabUiTheme.Ink;glyph.raycastTarget = false;

        var info = button.AddComponent<VersionInfo>();Instance = info;
        var popup = new GameObject("VersionInfoPopup", typeof(RectTransform), typeof(LabUiSurface));
        popup.transform.SetParent(root.transform, false);
        info.panel = (RectTransform)popup.transform;
        info.panel.anchorMin = info.panel.anchorMax = new Vector2(1,0);info.panel.pivot = new Vector2(1,0);
        info.panel.anchoredPosition = new Vector2(-18,58);info.panel.sizeDelta = new Vector2(300,140);
        popup.GetComponent<Image>().color = LabUiTheme.Panel;
        popup.GetComponent<Image>().raycastTarget = false;
        var text = new GameObject("VersionInfoText", typeof(RectTransform), typeof(Text));
        text.transform.SetParent(popup.transform, false);
        var tr = (RectTransform)text.transform;tr.anchorMin = Vector2.zero;tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(16,12);tr.offsetMax = new Vector2(-16,-12);
        info.contents = text.GetComponent<Text>();info.contents.font = font;info.contents.fontSize = 16;
        info.contents.color = LabUiTheme.Ink;info.contents.alignment = TextAnchor.UpperLeft;
        info.contents.horizontalOverflow = HorizontalWrapMode.Wrap;info.contents.lineSpacing = 1.25f;
        info.contents.raycastTarget = false;info.RefreshText();popup.SetActive(false);
    }
    void RefreshText() {
        english = Localization.English;
        contents.text = english
            ? $"<b>ChemLab</b>\nVersion  {DisplayVersion}\nLicense  Teacher Edition\n{LicenseMessage}\nAuthor  {Author}"
            : $"<b>ChemLab</b>\n版本  {DisplayVersion}\n许可证  教师版\n{LicenseMessage}\n作者  {Author}";
    }
    void Show() { if(panel != null) { RefreshText(); panel.gameObject.SetActive(hovered || selected); } }
    public void OnPointerEnter(PointerEventData e) { hovered = true;Show(); }
    public void OnPointerExit(PointerEventData e) { hovered = false;Show(); }
    public void OnSelect(BaseEventData e) { selected = true;Show(); }
    public void OnDeselect(BaseEventData e) { selected = false;Show(); }
    void Update() {
        if(english != Localization.English) RefreshText();
        if(IsOpen && LabInput.GetKeyDown(KeyCode.Escape)) {
            selected = hovered = false;Show();
            if(EventSystem.current?.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
    void OnDestroy() { if(Instance == this) Instance = null; }
}

public sealed class InfoCircle : MaskableGraphic {
    protected override void OnPopulateMesh(VertexHelper helper) {
        helper.Clear();var rect = rectTransform.rect;var center = rect.center;
        float radius = Mathf.Min(rect.width,rect.height)*.45f;
        var vertex = UIVertex.simpleVert;vertex.color = LabUiTheme.Panel;
        vertex.position = center;helper.AddVert(vertex);
        const int segments = 48;
        for(int i=0;i<=segments;i++) {
            float a=i*Mathf.PI*2/segments;
            vertex.position=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
            helper.AddVert(vertex);
        }
        for(int i=0;i<segments;i++) helper.AddTriangle(0,i+1,i+2);
        int start=helper.currentVertCount;vertex.color = LabUiTheme.Border;
        for(int i=0;i<=segments;i++) {
            float a=i*Mathf.PI*2/segments;var dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            vertex.position=center+dir*radius;helper.AddVert(vertex);
            vertex.position=center+dir*(radius-1.5f);helper.AddVert(vertex);
        }
        for(int i=0;i<segments;i++) {int a=start+i*2;helper.AddTriangle(a,a+2,a+1);helper.AddTriangle(a+1,a+2,a+3);}
    }
}
}
