/* =====================================================================
   标签层：所有悬浮标签（容器标签 + 方程式标签）放进**同一块**
   ScreenSpaceOverlay 画布，各自按世界坐标投影到屏幕定位。

   为什么不用「每个标签一块世界空间画布」：
     世界空间画布的透明排序按到相机的距离算、不看层级。标签一多、位置一近，
     谁盖谁就完全不可控 —— 容器标签会把方程式标签整个压住。
     同一块 Overlay 画布内部纯按层级排序，稳定可预测；
     而且 Overlay 天然正对屏幕，billboard 也省了。
   ===================================================================== */
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.Game {

public static class TagLayer {
    public static Canvas Canvas;
    public static RectTransform Root;

    /// <summary>建标签层（只建一次）</summary>
    public static RectTransform Ensure(Transform parent) {
        if (Root != null) return Root;
        var go = new GameObject("TagLayer");
        go.transform.SetParent(parent, false);
        Canvas = go.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Canvas.sortingOrder = 5;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(LabUI.DesignW, LabUI.DesignH);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;
        var gr = go.AddComponent<GraphicRaycaster>();
        gr.enabled = false;                       // 标签不参与点击
        Root = (RectTransform)go.transform;
        return Root;
    }

    /// <summary>世界坐标 → Overlay 画布本地坐标</summary>
    public static Vector2 Project(Vector3 world, Camera cam) {
        if (cam == null) cam = Camera.main;
        if (cam == null || Root == null) return Vector2.zero;
        var sp = cam.WorldToScreenPoint(world);
        float sx = Screen.width  > 0 ? Root.rect.width  / Screen.width  : 1f;
        float sy = Screen.height > 0 ? Root.rect.height / Screen.height : 1f;
        return new Vector2((sp.x - Screen.width  * 0.5f) * sx,
                           (sp.y - Screen.height * 0.5f) * sy);
    }

    public static bool Visible(Vector3 world, Camera cam) {
        if (cam == null) cam = Camera.main;
        if (cam == null) return true;
        return cam.WorldToScreenPoint(world).z > 0.01f;
    }
}

}
