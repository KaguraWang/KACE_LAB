const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 46).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ---- 1) 无需宿主即可合成并检查波形（供自检使用） ---- */
P('D:/ChemLabUnity/Assets/Scripts/Game/ChemAudio.cs',
`    /* 给无头验证用：合成出来的波形是否真的有信号（不是静音、不是削顶） */`,
`    /// <summary>不依赖宿主直接合成一次爆鸣并检查波形。
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

    /* 给无头验证用：合成出来的波形是否真的有信号（不是静音、不是削顶） */`);

/* ---- 2) 自检里加一段音频验证 ---- */
P('D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs',
`            TCheck("负向D：过量高锰酸钾氧化了氯离子，生成氯气", cl2 > 1e-9);`,
`            TCheck("负向D：过量高锰酸钾氧化了氯离子，生成氯气", cl2 > 1e-9);

            /* ---------- 爆鸣音：必须真的有声音输出 ---------- */
            var pr = ChemLab.Game.ChemAudio.ProbeRaw();
            Say($"   爆鸣音：峰值 {pr.peak:F3}  有效值 {pr.rms:F4}  长度 {pr.len} 采样  过零 {pr.zeroCross} 次");
            TCheck("爆鸣音不是静音（峰值 > 0.5）", pr.peak > 0.5f);
            TCheck("爆鸣音有足够能量（有效值 > 0.02）", pr.rms > 0.02f);
            TCheck("爆鸣音没有削顶失真（峰值 ≤ 1.0）", pr.peak <= 1.0f);
            TCheck("爆鸣音有高频成分（过零 > 400 次，听起来才尖锐）", pr.zeroCross > 400);
            TCheck("爆鸣音长度合适（0.2–0.6 s）", pr.len > 44100 * 0.2 && pr.len < 44100 * 0.6);`);

console.log('补丁处数:', n);
