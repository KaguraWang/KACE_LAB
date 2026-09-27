using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChemLab.Game {
public static class MenuPlacement {
    public static void AtPointer(RectTransform panel, RectTransform layer, Vector2 screen) {
        Canvas.ForceUpdateCanvases();
        var canvas = layer.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, cam, out var p);
        panel.anchorMin = panel.anchorMax = layer.pivot;
        panel.pivot = new Vector2(0, 1);
        var r = layer.rect;
        p.x = Mathf.Clamp(p.x, r.xMin, Mathf.Max(r.xMin, r.xMax - panel.rect.width));
        p.y = Mathf.Clamp(p.y, Mathf.Min(r.yMax, r.yMin + panel.rect.height), r.yMax);
        panel.anchoredPosition = p;
    }
}
public class MenuDismiss : MonoBehaviour, IPointerDownHandler {
    public Action Close;
    public void OnPointerDown(PointerEventData e) { Close?.Invoke(); }
    void Update() { if (LabInput.GetKeyDown(KeyCode.Escape)) Close?.Invoke(); }
}
}
