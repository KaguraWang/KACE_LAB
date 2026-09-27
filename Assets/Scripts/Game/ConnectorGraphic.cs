/* =====================================================================
   自己绘制的反应连接符
     为什么要自己画：ASCII / Unicode 的「=」「→」「⇌」宽度是固定的，
     而化学方程式要求把反应条件写在连接符**正上方**。等号和箭头太短，
     「催化剂、高温」这种条件根本写不下 —— 教科书上的连接符本身就是
     按条件长度画的。所以这里按宽度动态摆出线段和箭头。

     实现说明：本来用自定义 Graphic 重写 OnPopulateMesh，但在世界空间画布里
     它压根没进重建队列（OnPopulateMesh 一次都没被调用）。改用 Image 拼装 ——
     Image 是标准控件，渲染一定有保证。箭头用一个程序化生成的小三角贴图。
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.Game {

public class ConnectorGraphic : MonoBehaviour {

    public enum Kind { Equals, Arrow, Equilibrium }
    Kind _kind = Kind.Arrow;
    public float Width { get; private set; } = 40f;

    readonly List<Image> _parts = new List<Image>();
    RectTransform _rt;
    static Sprite _tri;

    public Color color = new Color(0.96f, 0.98f, 1f);
    float thickness = 4.5f;      // 线粗（画布单位）
    float equalsGap = 11f;       // 等号两条线的间距
    float headLen = 15f, headHalf = 9f;

    public static ConnectorGraphic Attach(GameObject go) {
        var c = go.GetComponent<ConnectorGraphic>();
        if (c == null) c = go.AddComponent<ConnectorGraphic>();
        c._rt = (RectTransform)go.transform;
        return c;
    }

    /// <summary>程序化生成一个指向右的三角形贴图</summary>
    static Sprite Tri() {
        if (_tri != null) return _tri;
        int S = 32;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++) {
                /* 右向三角形：x 越大越窄 */
                float t = 1f - (float)x / (S - 1);
                float halfH = 0.5f * t * S;
                bool inTri = Mathf.Abs(y - S * 0.5f) <= halfH;
                px[y * S + x] = inTri ? Color.white : new Color(1, 1, 1, 0);
            }
        tex.SetPixels(px); tex.Apply();
        _tri = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        return _tri;
    }

    Image NewPart(string name, bool triangle) {
        /* 同理，UI 部件必须带 RectTransform 创建 */
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_rt, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        if (OverlayTagMaterial.Value != null) img.material = OverlayTagMaterial.Value;
        img.raycastTarget = false;
        if (triangle) img.sprite = Tri();
        var r = (RectTransform)go.transform;
        r.anchorMin = new Vector2(0, 0.5f);
        r.anchorMax = new Vector2(0, 0.5f);
        r.pivot = new Vector2(0, 0.5f);
        _parts.Add(img);
        return img;
    }

    void Clear() {
        foreach (var p in _parts) if (p != null) Destroy(p.gameObject);
        _parts.Clear();
    }

    public void Set(Kind k, float width) {
        if (_kind == k && Mathf.Abs(Width - width) < 0.5f && _parts.Count > 0) {
            ApplyColor();
            return;
        }
        _kind = k; Width = width;
        Rebuild();
    }

    public void ApplyColor() {
        foreach (var p in _parts) if (p != null) p.color = color;
    }

    void Rebuild() {
        Clear();
        float w = Width;
        switch (_kind) {
            case Kind.Equals:
                /* 两条平行横线 */
                Bar(0, w,  equalsGap * 0.5f);
                Bar(0, w, -equalsGap * 0.5f);
                break;
            case Kind.Equilibrium:
                /* 上箭头朝右、下箭头朝左 —— 这就是可逆符号 ⇌ 的形态 */
                Bar(0, w - headLen * 0.7f,  equalsGap * 0.55f);
                Head(w - headLen,  equalsGap * 0.55f, false);
                Bar(headLen * 0.7f, w, -equalsGap * 0.55f);
                Head(0, -equalsGap * 0.55f, true);
                break;
            default:
                Bar(0, w - headLen * 0.7f, 0f);
                Head(w - headLen, 0f, false);
                break;
        }
        ApplyColor();
    }

    void Bar(float x0, float x1, float y) {
        var img = NewPart("Bar", false);
        var r = (RectTransform)img.transform;
        r.anchoredPosition = new Vector2(x0, y);
        r.sizeDelta = new Vector2(Mathf.Max(1f, x1 - x0), thickness);
    }

    /// <summary>箭头头部；flip=true 时指向左</summary>
    void Head(float x, float y, bool flip) {
        var img = NewPart("Head", true);
        var r = (RectTransform)img.transform;
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(headLen, headHalf * 2f);
        r.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
    }
}

}
