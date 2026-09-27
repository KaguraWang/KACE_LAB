/* =====================================================================
   宠物右键选单 + 教程调度
     选单项：
       · 重新开始教程 → 实验引导 / 计算器引导
       · 打开聊天（AI 助手）
       · 换大小、隐藏
   ===================================================================== */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;

namespace ChemLab.Game {

public static class PetMenu {
    static RectTransform _layer;
    public static RectTransform CurrentPanel { get; private set; }
    public static bool IsOpen => CurrentPanel != null && CurrentPanel.gameObject.activeSelf;

    static RectTransform Layer() {
        if (_layer != null) return _layer;
        var host = PetView.Instance != null ? PetView.Instance.transform.parent : null;
        var go = new GameObject("PetMenuLayer", typeof(RectTransform));
        go.transform.SetParent(host, false);
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = PetView.SortingOrder + 1;    // 比宠物还高一层，保证点得到
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(LabUI.DesignW, LabUI.DesignH);
        sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        _layer = (RectTransform)go.transform;
        return _layer;
    }

    public static void Close() {
        if (_layer == null) return;
        CurrentPanel = null;
        for (int i = _layer.childCount - 1; i >= 0; i--) {
            var child = _layer.GetChild(i).gameObject;
            child.SetActive(false); UnityEngine.Object.Destroy(child);
        }
    }

    public static void Show(PetView pet, Vector2 screenPos) {
        Close();
        bool unlocked = PetProgress.AiUnlocked;
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
        items.Add(("——", null));
        items.Add(("变小一点", () => pet.SetScale(pet.Scale - 0.15f)));
        items.Add(("变大一点", () => pet.SetScale(pet.Scale + 0.15f)));
        items.Add(("回到右下角", () => { pet.ReturnToLabCorner(); pet.Say("好，我回来啦。"); }));
        items.Add(("先躲起来", () => pet.Hide()));
        BuildMenu(screenPos, items);
    }

    public static void BuildMenu(Vector2 screenPos, List<(string, Action)> items) {
        Close();
        EnthalpyScene.Instance?.CloseMenu();
        var layer = Layer();
        var catcher = new GameObject("MenuCatcher", typeof(RectTransform), typeof(Image), typeof(MenuDismiss));
        catcher.transform.SetParent(layer, false);
        var crt = (RectTransform)catcher.transform;
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.offsetMin = crt.offsetMax = Vector2.zero;
        catcher.GetComponent<Image>().color = new Color(0, 0, 0, 0.004f);
        catcher.GetComponent<MenuDismiss>().Close = Close;
        float h = items.Count * 30 + 10;
        var panel = new GameObject("Menu", typeof(RectTransform));
        panel.transform.SetParent(layer, false);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.10f, 0.14f, 0.20f, 0.99f);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 1);
        /* 屏幕坐标换算到画布本地坐标 */
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            layer, screenPos, null, out local);
        local += new Vector2(layer.rect.width * 0.5f, layer.rect.height * 0.5f);
        rt.anchoredPosition = local;
        rt.sizeDelta = new Vector2(250, h);
        MenuPlacement.AtPointer(rt, layer, screenPos);
        CurrentPanel = rt;

        for (int i = 0; i < items.Count; i++) {
            var it = items[i];
            if (it.Item2 == null) continue;                   // 分隔线
            var bgo = new GameObject("I", typeof(RectTransform));
            bgo.transform.SetParent(rt, false);
            var bimg = bgo.AddComponent<Image>();
            bimg.color = new Color(0.16f, 0.22f, 0.30f, 1f);
            var brt = (RectTransform)bgo.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(1, 1);
            brt.anchoredPosition = new Vector2(-5, -5 - i * 30);
            brt.sizeDelta = new Vector2(240, 28);
            var b = bgo.AddComponent<Button>();
            b.targetGraphic = bimg;
            var act = it.Item2;
            b.onClick.AddListener(() => { Close(); act(); });
            var tgo = new GameObject("T", typeof(RectTransform));
            tgo.transform.SetParent(brt, false);
            var t = tgo.AddComponent<Text>();
            t.font = LabUI.UiFont; t.fontSize = 13; t.color = Color.white;
            Localization.Apply(t, it.Item1); t.alignment = TextAnchor.MiddleLeft;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 9; t.resizeTextMaxSize = 13;
            t.raycastTarget = false;
            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10, 0); trt.offsetMax = new Vector2(-6, 0);
        }
    }

    public static void StartTutorial(TutorialKind kind) {
        Close();
        PetTutorial.Start(kind);
    }
}

}
