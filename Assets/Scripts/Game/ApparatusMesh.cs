/* =====================================================================
   器材网格：本生灯、三脚架
   都用程序化旋转体生成，与器皿保持同一套做法。
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {

public static class ApparatusMesh {

    /* ---------------- 本生灯 ---------------- */
    /* 底座（扁圆盘）+ 灯管（细筒）+ 顶部灯口，侧面有进气调节环 */
    public static Mesh BuildBunsenBurner() {
        var prof = new List<Vector2>();
        prof.Add(new Vector2(0.001f, 0f));
        prof.Add(new Vector2(0.425f, 0f));
        prof.Add(new Vector2(0.425f, 0.035f));
        prof.Add(new Vector2(0.20f, 0.055f));      /* 底座收口 */
        prof.Add(new Vector2(0.115f, 0.090f));
        prof.Add(new Vector2(0.062f, 0.130f));     /* 灯管根部 */
        prof.Add(new Vector2(0.062f, 0.150f));
        prof.Add(new Vector2(0.090f, 0.175f));     /* 进气环 */
        prof.Add(new Vector2(0.090f, 0.215f));
        prof.Add(new Vector2(0.055f, 0.240f));
        prof.Add(new Vector2(0.065f, ApparatusDimensions.BurnerHeight-.025f));
        prof.Add(new Vector2(0.070f, ApparatusDimensions.BurnerHeight));
        prof.Add(new Vector2(0.052f, ApparatusDimensions.BurnerHeight));
        prof.Add(new Vector2(0.052f, .24f)); // hollow barrel, not a solid lid at the flame
        prof.Add(new Vector2(0f, .24f));
        return Revolve(prof, 28);
    }

    /* ---------------- 三脚架 ---------------- */
    /* 三条腿 + 顶部圆环 + 石棉网。用旋转体近似（圆环），腿单独做盒子。 */
    public static Mesh BuildTripodRing(float r = ApparatusDimensions.TripodRadius, float h = ApparatusDimensions.TripodHeight) {
        var prof = new List<Vector2>();
        float legR = 0.030f;
        /* 顶环 */
        prof.Add(new Vector2(r - 0.10f, h));
        prof.Add(new Vector2(r, h));
        prof.Add(new Vector2(r, h - 0.045f));
        prof.Add(new Vector2(r - 0.10f, h - 0.045f));
        prof.Add(new Vector2(r - 0.10f, h));
        return Revolve(prof, 36);
    }

    public static Mesh BuildTripodLeg(int index, float r = ApparatusDimensions.TripodRadius, float h = ApparatusDimensions.TripodHeight) {
        /* 一条从顶环斜向下的腿 */
        var m = new Mesh { name = "TripodLeg" };
        float ang = index * Mathf.PI * 2f / 3f;
        Vector3 top = new Vector3(Mathf.Cos(ang) * (r - 0.05f), h, Mathf.Sin(ang) * (r - 0.05f));
        Vector3 bot = new Vector3(Mathf.Cos(ang) * (r + 0.08f), 0f, Mathf.Sin(ang) * (r + 0.08f));
        Vector3 dir = (bot - top).normalized;
        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * 0.022f;
        Vector3 fwd = Vector3.Cross(dir, side).normalized * 0.022f;
        var v = new List<Vector3>();
        var t = new List<int>();
        /* 八棱柱近似圆柱腿 */
        Vector3[] ring = { side + fwd, side - fwd, -side - fwd, -side + fwd };
        for (int e = 0; e < 2; e++) {
            Vector3 c = e == 0 ? top : bot;
            foreach (var rr in ring) v.Add(c + rr);
        }
        for (int i = 0; i < 4; i++) {
            int a = i, b = (i + 1) % 4, c2 = 4 + i, d = 4 + (i + 1) % 4;
            t.Add(a); t.Add(c2); t.Add(d);
            t.Add(a); t.Add(d); t.Add(b);
        }
        for (int i = 1; i < 4 - 1; i++) {
            t.Add(0); t.Add(i); t.Add(i + 1);
            t.Add(4); t.Add(4 + i + 1); t.Add(4 + i);
        }
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /* ---------------- 通用旋转体 ---------------- */
    static Mesh Revolve(List<Vector2> prof, int segs) {
        var m = new Mesh { name = "Apparatus" };
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();
        int n = prof.Count;
        for (int i = 0; i < n; i++) {
            Vector2 pt = prof[i];
            Vector2 dir;
            if (i == 0) dir = prof[1] - pt;
            else if (i == n - 1) dir = pt - prof[n - 2];
            else dir = (prof[i + 1] - prof[i - 1]);
            dir.Normalize();
            Vector3 nrm = new Vector3(dir.y, -dir.x, 0f);
            if (Mathf.Abs(pt.x) < 0.0015f) nrm = new Vector3(0f, dir.y >= 0f ? -1f : 1f, 0f);
            for (int s = 0; s <= segs; s++) {
                float a = (float)s / segs * Mathf.PI * 2f;
                float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                verts.Add(new Vector3(pt.x * cs, pt.y, pt.x * sn));
                norms.Add(new Vector3(nrm.x * cs, nrm.y, nrm.x * sn).normalized);
            }
        }
        int stride = segs + 1;
        for (int i = 0; i < n - 1; i++)
            for (int s = 0; s < segs; s++) {
                int a = i * stride + s, b = a + 1, c = a + stride, d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        m.SetVertices(verts); m.SetNormals(norms); m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }
}

}
