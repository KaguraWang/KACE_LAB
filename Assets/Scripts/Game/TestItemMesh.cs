/* =====================================================================
   检验器材的网格：石蕊试纸条、木条
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;

namespace ChemLab.Game {

public static class TestItemMesh {

    /// <summary>石蕊试纸：一条薄纸片，略微卷曲</summary>
    public static Mesh BuildLitmus(float w = 0.055f, float h = 0.34f) {
        var m = new Mesh { name = "Litmus" };
        var v = new List<Vector3>();
        var t = new List<int>();
        int NX = 4, NY = 10;
        const float th = 0.001f; // 0.1 mm teaching-model paper thickness.
        /* 正面与背面两片，中间留厚度 */
        for (int side = 0; side < 2; side++) {
            float z = side == 0 ? -th * 0.5f : th * 0.5f;
            for (int j = 0; j <= NY; j++) {
                float fy = (float)j / NY;
                /* 顶端略微卷曲，看起来像真纸 */
                float curl = Mathf.Sin(fy * Mathf.PI) * 0.012f;
                for (int i = 0; i <= NX; i++) {
                    float fx = (float)i / NX - 0.5f;
                    v.Add(new Vector3(fx * w, fy * h, z + curl * (1f - Mathf.Abs(fx) * 2f)));
                }
            }
        }
        int stride = NX + 1, layer = (NY + 1) * stride;
        for (int side = 0; side < 2; side++) {
            int b = side * layer;
            for (int j = 0; j < NY; j++)
                for (int i = 0; i < NX; i++) {
                    int a0 = b + j * stride + i, a1 = a0 + 1, a2 = a0 + stride, a3 = a2 + 1;
                    if (side == 0) { t.Add(a0); t.Add(a2); t.Add(a1); t.Add(a1); t.Add(a2); t.Add(a3); }
                    else           { t.Add(a0); t.Add(a1); t.Add(a2); t.Add(a1); t.Add(a3); t.Add(a2); }
                }
        }
        // Both faces follow the same bent mid-surface. Close its perimeter;
        // opposite curls used to intersect inside the paper.
        void Edge(int a,int b){t.Add(a);t.Add(b);t.Add(b+layer);t.Add(a);t.Add(b+layer);t.Add(a+layer);}
        for(int i=0;i<NX;i++){Edge(i,i+1);Edge(NY*stride+i+1,NY*stride+i);}
        for(int j=0;j<NY;j++){Edge((j+1)*stride,j*stride);Edge(j*stride+NX,(j+1)*stride+NX);}
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>木条：细长木棒，顶端略尖</summary>
    public static Mesh BuildSplint(float w = 0.028f, float h = 0.62f) {
        var m = new Mesh { name = "Splint" };
        var v = new List<Vector3>();
        var t = new List<int>();
        int N = 6;
        /* 木棒主体：四棱柱更好看也更省面 */
        Vector3[] ring(float y, float s) => new[] {
            new Vector3(-w * s, y, -w * s), new Vector3(w * s, y, -w * s),
            new Vector3(w * s, y, w * s),   new Vector3(-w * s, y, w * s)
        };
        float[] ys = { 0f, h * 0.72f, h * 0.86f, h };
        float[] ss = { 1f, 1f, 0.85f, 0.35f };
        for (int k = 0; k < ys.Length; k++) foreach (var p in ring(ys[k], ss[k])) v.Add(p);
        for (int k = 0; k < ys.Length - 1; k++)
            for (int i = 0; i < 4; i++) {
                int a = k * 4 + i, b = k * 4 + (i + 1) % 4, c = a + 4, d = b + 4;
                t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
            }
        /* 底面与顶面 */
        t.Add(0); t.Add(2); t.Add(1); t.Add(0); t.Add(3); t.Add(2);
        int top = (ys.Length - 1) * 4;
        t.Add(top); t.Add(top + 1); t.Add(top + 2); t.Add(top); t.Add(top + 2); t.Add(top + 3);
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>木条顶端的三束火舌，环形截面逐层收束而非单个锥体。</summary>
    public static Mesh BuildFlame(float r = 0.045f, float h = 0.16f, int segs = 12) {
        var m = new Mesh { name = "SplintFlame" };
        var v = new List<Vector3>();
        var t = new List<int>();
        void Lobe(float ox,float oz,float radius,float height,float sway) {
            int start=v.Count;
            float[] levels={0f,.22f,.50f,.76f,1f};
            float[] widths={.54f,1f,.78f,.39f,.015f};
            for(int j=0;j<levels.Length;j++){
                float y=height*levels[j];
                float drift=sway*levels[j]*levels[j];
                for(int i=0;i<segs;i++){
                    float a=2f*Mathf.PI*i/segs;
                    float irregular=1f+.10f*Mathf.Sin(3f*a+j*1.7f);
                    v.Add(new Vector3(ox+drift+radius*widths[j]*irregular*Mathf.Cos(a),
                                      y,oz+radius*widths[j]*irregular*Mathf.Sin(a)));
                }
            }
            for(int j=0;j<levels.Length-1;j++)for(int i=0;i<segs;i++){
                int a=start+j*segs+i,b=start+j*segs+(i+1)%segs;
                int c=a+segs,d=b+segs;
                t.Add(a);t.Add(c);t.Add(b);t.Add(b);t.Add(c);t.Add(d);
            }
        }
        Lobe(0f,0f,r,h,.015f);
        Lobe(-r*.34f,0f,r*.55f,h*.72f,-.018f);
        Lobe(r*.32f,r*.12f,r*.47f,h*.61f,.017f);
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}

}
