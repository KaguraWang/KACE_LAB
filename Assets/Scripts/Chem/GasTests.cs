/* =====================================================================
   离子检验器材：石蕊试纸、木条
     石蕊：干燥不响应（必须是水溶液中的离子才能显色）；湿润的红石蕊遇碱性气体变蓝、
           湿润的蓝石蕊遇酸性气体变红。这是 CIE 定性分析里最常用的气体检验。
     木条：三态 —— 正常 / 点燃 / 刚刚熄灭（带火星）。
           点燃的木条伸入氢气发出尖锐爆鸣声；带火星的木条伸入氧气复燃。
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;

namespace ChemLab.Chem {

public enum TestItemKind { RedLitmus, BlueLitmus, Splint }
public enum SplintState { Normal, Burning, JustOut }

public class TestItem {
    public string Id;
    public TestItemKind Kind;
    public Vector3 Pos;
    public float LiftY;
    // A splint can rest next to a burner flame without occupying its vessel stand.
    public Apparatus AttachedBurner;
    /// <summary>石蕊是否已湿润（未湿润不显色）</summary>
    public bool Moist;
    /// <summary>木条状态</summary>
    public SplintState Splint = SplintState.Normal;
    /// <summary>石蕊变色后的颜色（空 = 保持原色）</summary>
    public string Tint = "";
    /// <summary>最近一次检验的结论</summary>
    public string LastResult = "";
    /// <summary>木条还能烧多久（秒）；燃尽后自动变成「刚刚熄灭」</summary>
    public float BurnLeft;
    /// <summary>刚刚熄灭后还能复燃的时间窗（秒）</summary>
    public float GlowLeft;
    /// <summary>焦黑强度与熄火后的保留时间；独立于是否还能复燃。</summary>
    public float CharLevel, CharHoldLeft;

    public void StopBurning(bool keepEmbers) {
        if (Splint == SplintState.Burning) CharHoldLeft = GasTest.CharHoldSeconds;
        Splint = keepEmbers ? SplintState.JustOut : SplintState.Normal;
        BurnLeft = 0;
        GlowLeft = keepEmbers ? GasTest.EmberSeconds : 0;
    }
    /// <summary>伸出时播放一次声音的标记</summary>
    public float PendingSound;
    public Vessel PendingSoundVessel;
    /// <summary>声音类型：pop / relight / fizz</summary>
    public string PendingSoundKind = "";
    public string ContactKey = "";

    public string Name => Kind switch {
        TestItemKind.RedLitmus  => "红色石蕊试纸",
        TestItemKind.BlueLitmus => "蓝色石蕊试纸",
        _                       => "木条",
    };

    public string StateText {
        get {
            if (Kind == TestItemKind.Splint)
                return Splint switch {
                    SplintState.Burning => "正在燃烧",
                    SplintState.JustOut => "刚刚熄灭（带火星）",
                    _                   => "未点燃",
                };
            return Moist ? "已湿润" : "干燥";
        }
    }
}

public static class TestSet {
    public static readonly List<TestItem> All = new List<TestItem>();
    static int _next = 1;

    public static TestItem Add(TestItemKind kind, Vector3 pos) {
        var t = new TestItem { Id = "t" + (_next++), Kind = kind, Pos = pos };
        All.Add(t);
        return t;
    }

    public static void Clear() { All.Clear(); _next = 1; }

    public static void Remove(TestItem t) { if (t != null) { t.AttachedBurner = null; All.Remove(t); } }

    /// <summary>每帧推进木条的燃烧与余烬</summary>
    public static void Tick(float dt) {
        dt = Mathf.Max(0, dt);
        foreach (var t in All) {
            if (t.Kind != TestItemKind.Splint) continue;
            float remaining = dt;
            if (t.Splint == SplintState.Burning) {
                float burningTime = Mathf.Min(remaining, Mathf.Max(0, t.BurnLeft));
                t.CharLevel = Mathf.MoveTowards(t.CharLevel, 1, burningTime / GasTest.CharBuildSeconds);
                t.BurnLeft = Mathf.Max(0, t.BurnLeft - burningTime);
                remaining -= burningTime;
                if (t.BurnLeft <= 0f) t.StopBurning(true);
            }
            if (t.Splint != SplintState.Burning) {
                if (t.Splint == SplintState.JustOut) {
                    t.GlowLeft = Mathf.Max(0, t.GlowLeft - remaining);
                    if (t.GlowLeft <= 0f) t.Splint = SplintState.Normal;
                }
                float held = Mathf.Min(remaining, t.CharHoldLeft);
                t.CharHoldLeft -= held;
                t.CharLevel = Mathf.MoveTowards(t.CharLevel, 0, (remaining-held) / GasTest.CharResetSeconds);
            }
        }
    }
}

/* =====================================================================
   检验的执行
   ===================================================================== */
public static class GasTest {
    public const float EmberSeconds = 20f;
    public const float CharHoldSeconds = 20f, CharBuildSeconds = 3f, CharResetSeconds = 3f;

    public static void DipLiquid(Vessel v, TestItem t) {
        if (v == null || t == null || v.Volume() <= 0.5) return;
        if (t.Kind == TestItemKind.Splint) {
            // 浸水会冷却余烬，不能再用来检验氧气。
            t.StopBurning(false);
            t.LastResult = "木条浸入液体，火焰和余烬熄灭";
        } else {
            t.Moist = true;
            if (v.Ph < 7.0 - 0.01) t.Tint = "#d83a3a";
            else if (v.Ph > 7.0 + 0.01) t.Tint = "#3a5ad8";
            t.LastResult = "石蕊试纸接触溶液，pH=" + v.Ph.ToString("F1");
        }
        Lab.Note(v, t.LastResult, "obs");
    }

    /// <summary>容器里含量最多的气体（检验的对象）。
    /// 除了气相，**溶在水里的挥发性物种也算** —— 氨水上方就是氨气，
    /// 亚硫酸上方就是二氧化硫，气体检验正是靠这一点。只查气相的话，
    /// 这些在数据里以「xx 溶液」形式存在的物种永远检不出来。</summary>
    public static string DominantGas(Vessel v) {
        string best = null; double bm = 1e-9;
        foreach (var kv in v.Gases) if (kv.Value > bm) { bm = kv.Value; best = kv.Key; }
        /* 溶液里的挥发性成分折算成蒸气 */
        foreach (var kv in v.Solutes) {
            string vap = ChemData.VapourOf(kv.Key);
            if (vap == null) continue;
            /* 已经算过气相就不重复计 */
            if (v.Gases.ContainsKey(vap)) continue;
            if (kv.Value > bm) { bm = kv.Value; best = vap; }
        }
        return best;
    }

    /* 石蕊：湿润的红石蕊遇碱性气体变蓝；湿润的蓝石蕊遇酸性气体变红 */
    const string RedAsIs  = "#d83a3a";
    const string RedToBlue = "#3a5ad8";
    const string BlueAsIs = "#3a5ad8";
    const string BlueToRed = "#d83a3a";

    /// <summary>把检验器材伸到容器口，返回观察到的现象</summary>
    public static string Insert(Vessel v, TestItem t) {
        if (v == null || t == null) return "";
        string gas = DominantGas(v);

        /* 没有气体时也要如实说「没有明显现象」，而不是随便给个结论 */
        if (gas == null) {
            t.LastResult = t.Kind == TestItemKind.Splint
                ? (t.Splint == SplintState.Burning ? "木条继续燃烧，没有明显变化" : "没有明显现象")
                : (t.Moist ? "试纸没有明显变化" : "干燥的试纸没有变化（必须先湿润）");
            return t.LastResult;
        }

        string nature = ChemData.GasNature(gas);

        /* ---------- 石蕊试纸 ---------- */
        if (t.Kind != TestItemKind.Splint) {
            if (!t.Moist) {
                t.LastResult = "干燥的试纸不变色 —— 必须先用蒸馏水湿润才能检验气体";
                return t.LastResult;
            }
            bool isRed = t.Kind == TestItemKind.RedLitmus;
            if (nature == "base") {
                if (isRed) {
                    t.Tint = RedToBlue;
                    t.LastResult = $"湿润的红色石蕊试纸变蓝，说明该气体显碱性（{ChemData.NameOf(gas)}）";
                } else {
                    /* 保持原色 —— Tint 必须留空。若填成本身的蓝色，
                       「没变色」和「变成了蓝色」就无法区分了。 */
                    t.Tint = "";
                    t.LastResult = $"蓝色石蕊试纸在碱性气体中保持蓝色，没有变化（{ChemData.NameOf(gas)}）";
                }
            } else if (nature == "acid") {
                if (!isRed) {
                    t.Tint = BlueToRed;
                    t.LastResult = $"湿润的蓝色石蕊试纸变红，说明该气体显酸性（{ChemData.NameOf(gas)}）";
                } else {
                    t.Tint = "";
                    t.LastResult = $"红色石蕊试纸在酸性气体中保持红色，没有变化（{ChemData.NameOf(gas)}）";
                }
            } else if (nature == "bleach") {
                t.Tint = "#f0f0f0";
                t.LastResult = $"试纸先变红随后褪成白色 —— 该气体有漂白性（{ChemData.NameOf(gas)}）";
            } else {
                t.LastResult = $"试纸没有明显变化 —— 该气体既不显酸性也不显碱性（{ChemData.NameOf(gas)}）";
            }
            v.Fx.NotedRules.Add("litmus-" + t.Id);
            Lab.Note(v, t.LastResult, "obs");
            return t.LastResult;
        }

        /* ---------- 木条 ---------- */
        string burning = ChemData.GasSplintBurning(gas);
        string justOut = ChemData.GasSplintJustOut(gas);
        string desc    = ChemData.GasSplintObs(gas);

        if (t.Splint == SplintState.Normal) {
            t.LastResult = "木条没有点燃，什么也检验不出来";
            Lab.Note(v, t.LastResult, "obs");
            return t.LastResult;
        }

        if (t.Splint == SplintState.Burning) {
            switch (burning) {
                case "pop":
                    /* 尖锐的爆鸣 —— 氢气最经典的检验 */
                    v.Gases.Remove(gas);
                    t.StopBurning(true);
                    t.PendingSound = 1f; t.PendingSoundKind = "pop";
                    t.PendingSoundVessel = v;
                    v.Fx.Pop = 1f; v.Fx.Bubble = 0; v.Fx.Flame = false;
                    t.LastResult = string.IsNullOrEmpty(desc) ? "发出尖锐的爆鸣声" : desc;
                    break;
                case "brighter":
                    t.PendingSound = 0.4f; t.PendingSoundKind = "fizz";
                    t.LastResult = string.IsNullOrEmpty(desc) ? "木条燃烧得更旺" : desc;
                    break;
                case "quiet":
                case "sooty":
                    t.LastResult = string.IsNullOrEmpty(desc) ? "气体安静地燃烧，没有爆鸣声" : desc;
                    break;
                default:  /* out */
                    t.StopBurning(true);
                    v.Gases.Remove(gas);
                    t.LastResult = string.IsNullOrEmpty(desc) ? "木条立即熄灭" : desc;
                    break;
            }
            Lab.Note(v, t.LastResult, "obs");
            return t.LastResult;
        }

        /* 刚刚熄灭（带火星） */
        if (justOut == "relight" && t.GlowLeft > 0f) {
            t.Splint = SplintState.Burning; t.BurnLeft = 8f; t.CharHoldLeft = 0;
            t.PendingSound = 0.7f; t.PendingSoundKind = "fizz";
            v.Fx.Glow = 1f;
            t.LastResult = string.IsNullOrEmpty(desc) ? "带火星的木条复燃" : desc;
        } else if (justOut == "pop_weak") {
            v.Gases.Remove(gas);
            t.StopBurning(false);
            t.PendingSound = 0.8f; t.PendingSoundKind = "pop";
            v.Fx.Pop = 0.7f;
            t.PendingSoundVessel = v;
            t.LastResult = "发出轻微的爆鸣声，说明是氢气（伸入燃着的木条现象更明显）";
        } else {
            t.StopBurning(false);
            t.LastResult = string.IsNullOrEmpty(desc) ? "带火星的木条熄灭" : desc;
        }
        Lab.Note(v, t.LastResult, "obs");
        return t.LastResult;
    }

    /// <summary>点燃木条</summary>
    public static bool Light(TestItem t) {
        if (t == null || t.Kind != TestItemKind.Splint) return false;
        if (t.Splint == SplintState.Burning) return true;
        t.Splint = SplintState.Burning;
        t.BurnLeft = 12f;
        t.CharHoldLeft = 0;
        t.LastResult = "木条被点燃";
        return true;
    }

    /// <summary>把木条吹熄 / 甩熄 —— 用于得到「刚刚熄灭」的带火星状态</summary>
    public static bool Extinguish(TestItem t) {
        if (t == null || t.Kind != TestItemKind.Splint) return false;
        if (t.Splint != SplintState.Burning) return false;
        t.StopBurning(true);
        t.LastResult = "木条的火焰熄灭，还带着火星";
        return true;
    }

    /// <summary>湿润石蕊试纸（用蒸馏水）</summary>
    public static bool Moisten(TestItem t) {
        if (t == null || t.Kind == TestItemKind.Splint) return false;
        t.Moist = true;
        t.Tint = "";
        t.LastResult = "试纸已用蒸馏水湿润";
        return true;
    }
}

}
