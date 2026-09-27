/* =====================================================================
   阿侬（Anon）精灵图系统
     素材：C:\Users\Simon\.codex\pets\anon\spritesheet.webp
           1536 × 2288，8 列 × 11 行，每格 192 × 208

     两个坑，都踩过：
     1) 原图高 2288，超过 Unity 的 maxTextureSize 上限会被缩放，
        格子尺寸不再是整数，切图必然错位。
        → 预处理成上下两张（1536×1248 与 1536×1040）。
     2) 有的行末尾是**空格子**。如果按固定帧数循环，就会周期性闪一下空白。
        → 加载时读 alpha 通道，把每行「真正有内容」的格子找出来，
          动画只在有效帧之间循环。这正是之前闪烁的原因。
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;

namespace ChemLab.Game {

public static class PetSprite {
    public const int Cols = 8, TotalRows = 11;
    public const int CellW = 192, CellH = 208;
    /// <summary>上半张装 0..5 行，下半张装 6..10 行</summary>
    const int SplitRow = 6;

    static Texture2D _top, _bottom;
    static Sprite[,] _cells;
    /// <summary>每一行里真正有内容的列号（按顺序）</summary>
    static List<int>[] _valid;

    public static bool Ready { get; private set; }
    public static string Error = "";

    public static void Init() {
        if (Ready) return;
        _top = Resources.Load<Texture2D>("anon_sheet_a");
        _bottom = Resources.Load<Texture2D>("anon_sheet_b");
        if (_top == null) { Error = "找不到 Resources/anon_sheet_a"; return; }
        if (_bottom == null) { Error = "找不到 Resources/anon_sheet_b"; return; }
        if (_top.width != Cols * CellW || _top.height != SplitRow * CellH) {
            Error = string.Format("anon_sheet_a 尺寸异常 {0}×{1}（应为 {2}×{3}）",
                _top.width, _top.height, Cols * CellW, SplitRow * CellH);
            return;
        }
        if (_bottom.width != Cols * CellW || _bottom.height != (TotalRows - SplitRow) * CellH) {
            Error = string.Format("anon_sheet_b 尺寸异常 {0}×{1}（应为 {2}×{3}）",
                _bottom.width, _bottom.height, Cols * CellW, (TotalRows - SplitRow) * CellH);
            return;
        }
        if (!_top.isReadable || !_bottom.isReadable) {
            Error = "精灵图不可读（meta 里 isReadable 必须是 1），无法识别空格子";
            return;
        }
        _cells = new Sprite[TotalRows, Cols];
        _valid = new List<int>[TotalRows];
        for (int r = 0; r < TotalRows; r++) _valid[r] = ScanRow(r);
        Ready = true;
    }

    /// <summary>扫一行，返回有内容的列号。
    /// 判据：该格里不透明像素超过一个小阈值（防止抗锯齿边缘的零星像素）。</summary>
    static List<int> ScanRow(int row) {
        var tex = row < SplitRow ? _top : _bottom;
        int localRow = row < SplitRow ? row : row - SplitRow;
        int y0 = tex.height - (localRow + 1) * CellH;
        var px = tex.GetPixels(0, y0, tex.width, CellH);
        var list = new List<int>();
        const int MinPixels = 40;      // 少于这个数就当成空格子
        for (int c = 0; c < Cols; c++) {
            int count = 0;
            for (int y = 0; y < CellH && count < MinPixels; y += 2) {
                int rowBase = y * tex.width + c * CellW;   /* base 是 C# 关键字，不能当名字 */
                for (int x = 0; x < CellW; x += 2) {
                    if (px[rowBase + x].a > 0.28f) { count++; if (count >= MinPixels) break; }
                }
            }
            if (count >= MinPixels) list.Add(c);
        }
        return list;
    }

    /// <summary>某一帧的不透明像素占比（0..1）。用来验证「没有空格子」——
    /// 光判断 sprite 不为 null 是不够的，空格子同样能建出 sprite。 </summary>
    public static float FrameFill(int row, int index) {
        Init();
        if (_valid == null || _texOf(row) == null) return 0f;
        row = ((row % TotalRows) + TotalRows) % TotalRows;
        var v = _valid[row];
        if (v.Count == 0) return 0f;
        index = ((index % v.Count) + v.Count) % v.Count;
        int col = v[index];
        var tex = _texOf(row);
        int localRow = row < SplitRow ? row : row - SplitRow;
        int y0 = tex.height - (localRow + 1) * CellH;
        var px = tex.GetPixels(col * CellW, y0, CellW, CellH);
        int on = 0, total = 0;
        for (int i = 0; i < px.Length; i += 3) { total++; if (px[i].a > 0.28f) on++; }
        return total > 0 ? (float)on / total : 0f;
    }
    static Texture2D _texOf(int row) { return row < SplitRow ? _top : _bottom; }

    /// <summary>这一行有多少个有效帧</summary>
    public static int FrameCount(int row) {
        Init();
        if (_valid == null) return 0;
        row = ((row % TotalRows) + TotalRows) % TotalRows;
        return _valid[row].Count;
    }

    public static string Diagnose() {
        Init();
        if (!Ready) return "✗ " + Error;
        var sb = new System.Text.StringBuilder();
        sb.AppendFormat("✓ 精灵图两张 {0}×{1} + {2}×{3}，{4} 行 × {5} 列（每格 {6}×{7}）",
            _top.width, _top.height, _bottom.width, _bottom.height,
            TotalRows, Cols, CellW, CellH);
        sb.Append("\n      每行有效帧数：");
        for (int r = 0; r < TotalRows; r++)
            sb.Append(r + "=" + _valid[r].Count + (r < TotalRows - 1 ? " " : ""));
        return sb.ToString();
    }

    /// <summary>取第 row 行第 col 格的精灵（col 是**列号**，不是帧序号）</summary>
    public static Sprite Cell(int row, int col) {
        Init();
        if (_cells == null) return null;
        row = ((row % TotalRows) + TotalRows) % TotalRows;
        col = ((col % Cols) + Cols) % Cols;
        if (_cells[row, col] != null) return _cells[row, col];

        var tex = row < SplitRow ? _top : _bottom;
        int localRow = row < SplitRow ? row : row - SplitRow;
        /* Unity 纹理坐标原点在左下，精灵图第 0 行在最上面，所以 y 要翻过来 */
        float x = col * CellW;
        float y = tex.height - (localRow + 1) * CellH;
        var sp = Sprite.Create(tex, new Rect(x, y, CellW, CellH), new Vector2(0.5f, 0.5f), 100f);
        sp.name = "anon_" + row + "_" + col;
        _cells[row, col] = sp;
        return sp;
    }

    /// <summary>按**帧序号**取精灵：只在有效帧之间循环。
    /// 动画系统一律走这个入口，就不会闪空格子了。</summary>
    public static Sprite Frame(int row, int index) {
        Init();
        if (_valid == null) return null;
        row = ((row % TotalRows) + TotalRows) % TotalRows;
        var v = _valid[row];
        if (v.Count == 0) return Cell(row, 0);
        index = ((index % v.Count) + v.Count) % v.Count;
        return Cell(row, v[index]);
    }

    /* ---------------- 动画定义 ----------------
       只声明「放哪一行、多快」——帧数由上面的扫描结果决定，
       不再手写，避免空格子又被算进去。 */
    public class Anim {
        public int Row;
        public float Fps;
        public bool Loop = true;
        public string Next;
        public Anim(int row, float fps, bool loop = true, string next = null) {
            Row = row; Fps = fps; Loop = loop; Next = next;
        }
        public int Frames => FrameCount(Row);
    }

    public static readonly Dictionary<string, Anim> Anims = new Dictionary<string, Anim> {
        { "idle",   new Anim(0,  6f) },
        { "walkR",  new Anim(1, 10f) },
        { "walkL",  new Anim(2, 10f) },
        { "wave",   new Anim(3,  8f, false, "idle") },
        { "jump",   new Anim(4, 10f, false, "idle") },
        { "fail",   new Anim(5,  6f, false, "idle") },
        { "wait",   new Anim(6,  4f) },
        { "focus",  new Anim(7,  8f) },      // 处理任务中
        { "review", new Anim(8,  8f) },      // 复核 / 思考
        { "gazeA",  new Anim(9,  6f) },
        { "gazeB",  new Anim(10, 6f) },
    };
}

}
