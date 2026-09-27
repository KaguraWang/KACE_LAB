/* =====================================================================
   程序化音效：不依赖任何外部音频资源，全部用代码合成。
     爆鸣 pop      —— 氢气与空气混合点燃的尖锐爆鸣
     复燃 relight  —— 木条重新燃起的轻微「噗」声
     燃烧 fizz     —— 燃得更旺时的气流声
   ===================================================================== */
using UnityEngine;

namespace ChemLab.Game {

public static class ChemAudio {
    static AudioSource _src;
    static AudioClip _pop, _relight, _fizz;
    static float _lastPlay;

    public static void Ensure(GameObject host) {
        if (_src != null) return;
        _src = host.GetComponent<AudioSource>();
        if (_src == null) _src = host.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.spatialBlend = 0f;
        _src.volume = 0.85f;
        _pop     = MakePop();
        _relight = MakeRelight();
        _fizz    = MakeFizz();
    }

    public static void Play(string kind, float amp = 1f) {
        if (_src == null) return;
        /* 同一帧内不重复触发 */
        if (Time.unscaledTime - _lastPlay < 0.12f) return;
        _lastPlay = Time.unscaledTime;
        var clip = kind == "pop" ? _pop : kind == "relight" ? _relight : _fizz;
        if (clip == null) return;
        _src.PlayOneShot(clip, Mathf.Clamp01(amp));
    }

    /* ---------------- 合成 ----------------
       rate 采样率；所有片段都用「快起音 + 指数衰减」的包络，
       起音时间决定听起来是「尖锐」还是「沉闷」。 */

    const int RATE = 44100;

    /// <summary>爆鸣：极快起音的高频爆裂 + 低频冲击叠加，整体约 0.35 s</summary>
    static AudioClip MakePop() {
        const float dur = 0.35f;
        int n = (int)(RATE * dur);
        var data = new float[n];
        var rng = new System.Random(20250921);
        for (int i = 0; i < n; i++) {
            float t = (float)i / RATE;
            /* 爆裂部分：白噪声，起音 3 ms，衰减 0.06 s */
            float envNoise = Mathf.Exp(-t / 0.060f) * Mathf.Min(1f, t / 0.003f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            /* 给噪声加一层高通感：与前一采样作差 */
            float hp = noise - (i > 0 ? data[i - 1] * 0.0f : 0f);
            /* 低频冲击：120 Hz 迅速下滑到 50 Hz，给出「砰」的厚度 */
            float f = Mathf.Lerp(140f, 48f, Mathf.Clamp01(t / 0.12f));
            float envThump = Mathf.Exp(-t / 0.10f) * Mathf.Min(1f, t / 0.002f);
            float thump = Mathf.Sin(2f * Mathf.PI * f * t) * envThump;
            /* 尾部余响：一点低频噪声，模拟实验室的回声 */
            float tail = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t / 0.18f) * 0.12f;
            data[i] = Mathf.Clamp(noise * envNoise * 0.72f + thump * 0.62f + hp * 0f + tail, -1f, 1f);
        }
        var c = AudioClip.Create("chemlab_pop", n, 1, RATE, false);
        c.SetData(data, 0);
        return c;
    }

    /// <summary>复燃：一声较轻的「噗」，起音稍缓，无低频冲击</summary>
    static AudioClip MakeRelight() {
        const float dur = 0.22f;
        int n = (int)(RATE * dur);
        var data = new float[n];
        var rng = new System.Random(77001);
        for (int i = 0; i < n; i++) {
            float t = (float)i / RATE;
            float env = Mathf.Exp(-t / 0.055f) * Mathf.Min(1f, t / 0.010f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float f = Mathf.Lerp(260f, 120f, Mathf.Clamp01(t / 0.08f));
            float tone = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t / 0.07f);
            data[i] = Mathf.Clamp(noise * env * 0.42f + tone * 0.28f, -1f, 1f);
        }
        var c = AudioClip.Create("chemlab_relight", n, 1, RATE, false);
        c.SetData(data, 0);
        return c;
    }

    /// <summary>燃烧加剧：持续的气流噪声</summary>
    static AudioClip MakeFizz() {
        const float dur = 0.45f;
        int n = (int)(RATE * dur);
        var data = new float[n];
        var rng = new System.Random(31337);
        float lp = 0f;
        for (int i = 0; i < n; i++) {
            float t = (float)i / RATE;
            /* 淡入淡出，避免爆音 */
            float env = Mathf.Min(1f, t / 0.05f) * Mathf.Min(1f, (dur - t) / 0.12f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * 0.25f;      /* 一阶低通，得到「呼」的气流声 */
            data[i] = Mathf.Clamp(lp * env * 0.55f, -1f, 1f);
        }
        var c = AudioClip.Create("chemlab_fizz", n, 1, RATE, false);
        c.SetData(data, 0);
        return c;
    }

    /// <summary>不依赖宿主直接合成一次爆鸣并检查波形。
    /// 自检用它确认「真的有声音输出」——不是静音，也不是削顶失真的噪声。</summary>
    public static (float peak, float rms, int len, int zeroCross) ProbeRaw() {
        var saved = _pop;
        _pop = MakePop();
        var r = Probe();
        /* 统计过零点：太低说明是直流或削顶，太高说明是纯噪声 */
        var buf = new float[_pop.samples];
        _pop.GetData(buf, 0);
        int zc = 0;
        for (int i = 1; i < buf.Length; i++)
            if ((buf[i - 1] < 0f && buf[i] >= 0f) || (buf[i - 1] >= 0f && buf[i] < 0f)) zc++;
        _pop = saved;
        return (r.peak, r.rms, r.len, zc);
    }

    /* 给无头验证用：合成出来的波形是否真的有信号（不是静音、不是削顶） */
    public static (float peak, float rms, int len) Probe() {
        if (_pop == null) return (0, 0, 0);
        var buf = new float[_pop.samples];
        _pop.GetData(buf, 0);
        float peak = 0, sum = 0;
        foreach (var s in buf) { float a = Mathf.Abs(s); if (a > peak) peak = a; sum += s * s; }
        return (peak, Mathf.Sqrt(sum / buf.Length), buf.Length);
    }
}

}
