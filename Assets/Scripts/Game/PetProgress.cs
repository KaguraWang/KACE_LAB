/* =====================================================================
   教程进度与 AI 解锁门控
     规则（用户要求）：
       · 只有「教程走完」或「用户主动跳过」之后，AI 助手才可用。
       · 因此教程必须足够详细 —— 它是解锁 AI 的唯一途径，
         也是用户学会这个软件的唯一途径。
   ===================================================================== */
using UnityEngine;

namespace ChemLab.Game {

public static class PetProgress {
    const string KLabDone   = "anon.prog.lab";     // 实验教程：完成或跳过
    const string KLabSkipped= "anon.prog.labSkip"; // 是否是被跳过的
    const string KCalcDone  = "anon.prog.calc";
    const string KChatUsed  = "anon.prog.chat";    // 是否已经用过聊天

    public static bool LabDone {
        get { return PlayerPrefs.GetInt(KLabDone, 0) == 1; }
        private set { PlayerPrefs.SetInt(KLabDone, value ? 1 : 0); PlayerPrefs.Save(); }
    }
    public static bool LabSkipped {
        get { return PlayerPrefs.GetInt(KLabSkipped, 0) == 1; }
        private set { PlayerPrefs.SetInt(KLabSkipped, value ? 1 : 0); PlayerPrefs.Save(); }
    }
    public static bool CalcDone {
        get { return PlayerPrefs.GetInt(KCalcDone, 0) == 1; }
        private set { PlayerPrefs.SetInt(KCalcDone, value ? 1 : 0); PlayerPrefs.Save(); }
    }
    public static bool ChatUsed {
        get { return PlayerPrefs.GetInt(KChatUsed, 0) == 1; }
        set { PlayerPrefs.SetInt(KChatUsed, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// <summary>AI 助手是否已解锁 —— 实验教程完成或跳过后才为 true</summary>
    public static bool AiUnlocked => LabDone;

    public static void FinishLab(bool skipped) {
        LabDone = true;
        LabSkipped = skipped;
    }
    public static void FinishCalc() { CalcDone = true; }

    /// <summary>重置所有进度（调试 / 重新体验用）</summary>
    public static void ResetAll() {
        PlayerPrefs.DeleteKey(KLabDone);
        PlayerPrefs.DeleteKey(KLabSkipped);
        PlayerPrefs.DeleteKey(KCalcDone);
        PlayerPrefs.Save();
    }

    public static string StatusText() {
        if (!LabDone) return "教程进行中 —— 走完或跳过后解锁 AI 助手";
        if (!CalcDone) return "AI 助手已解锁 · 计算器教程还没看过";
        return "AI 助手已解锁";
    }
}

}
