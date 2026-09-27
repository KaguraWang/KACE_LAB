/* =====================================================================
   程序化生成器皿网格
   剖面算法与 Web 版一致：在 (半径, 高度) 平面给出二维轮廓，绕 Y 轴旋转。
   玻璃做成「闭合壳体」—— 外壁 + 内壁首尾相连成一个闭合环路再旋转，
   这样得到的是水密网格，正反面都能正确着色。
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {

public static class VesselMesh {

    /* 几何单位：1 世界单位 = 100 mm；体积 1 立方世界单位 = 1000 mL。 */
    public const double GravityWorldUnitsPerSecondSquared=98.1;
    public struct Geo {
        public float R, H, Wall;   // 半径、高度、壁厚
        public int Type;           // 0 烧杯 1 锥形瓶 2 试管 3 量筒 4 圆底烧瓶 5 蒸发皿/坩埚
    }

    public static Geo Get(VesselKind k) => k switch {
        VesselKind.Beaker      => new Geo { R = 0.35f, H = 0.95f, Type = 0 },
        VesselKind.Flask       => new Geo { R = 0.425f, H = 1.45f, Type = 1 },
        VesselKind.Tube        => new Geo { R = 0.10f, H = 1.50f, Type = 2 },
        VesselKind.Bottle      => new Geo { R = 0.35f, H = 1.38f, Type = 8 },
        // DURAN 213902402: 100 mL, 29 mm body OD, 256 mm overall height.
        VesselKind.Cylinder    => new Geo { R = 0.145f, H = 2.56f, Type = 3 },
        VesselKind.RoundBottom => new Geo { R = 0.46f, H = 0.96f, Type = 4 },
        VesselKind.Crucible    => new Geo { R = 0.36f, H = 0.44f, Type = 5 },
        VesselKind.Dish        => new Geo { R = 0.48f, H = 0.24f, Type = 5 },
        /* 滴定管：细长管 + 下端收细的尖嘴，带旋塞 */
        // DWK 243303604: 50 mL / 820 mm. Tube OD 12.6 mm is a model choice.
        VesselKind.Burette     => new Geo { R = 0.063f, H = 8.20f, Type = 6 },
        /* 移液管：中段膨大球、上下细颈、下端尖嘴 */
        // DWK 243381405: 25 mL / 530 mm. Bulb radius integrates to 25 mL at y=4.5.
        VesselKind.Pipette     => new Geo { R = 0.09705252f, H = 5.30f, Type = 7 },
        _                      => new Geo { R = 0.40f, H = 0.82f, Type = 0 },
    };

    /// <summary>轮廓点：从底到顶，(半径, 高度)</summary>
    public static List<Vector2> Profile(int type, float R, float H) {
        var p = new List<Vector2>();
        int N = 22;
        switch (type) {
            case 0:  /* 烧杯：直筒微外扩 */
                for (int i = 0; i <= N; i++) {
                    float t = (float)i / N;
                    p.Add(new Vector2(R * (0.94f + 0.06f * t), H * t));
                }
                break;
            case 1:  /* 锥形瓶：锥体 + 瓶颈 */
                for (int i = 0; i <= 20; i++) {
                    float t = (float)i / 20;
                    p.Add(new Vector2(R * (1f - 0.60f * t), H * 0.78f * t));
                }
                for (int i = 1; i <= 10; i++)
                    p.Add(new Vector2(R * 0.40f, H * (0.78f + 0.22f * i / 10f)));
                break;
            case 2:  /* 试管：半球底 + 直筒 */
                for (int i = 0; i <= 14; i++) {
                    float a = (float)i / 14 * Mathf.PI * 0.5f;
                    p.Add(new Vector2(R * Mathf.Sin(a), R * (1f - Mathf.Cos(a))));
                }
                for (int i = 1; i <= N; i++)
                    p.Add(new Vector2(R, R + (H - R) * i / N));
                break;
            case 3:  /* 量筒：实心六角底座另建，不能作为宽口液体腔。 */
                float cylinderWall = Mathf.Max(0.012f,R*0.052f);
                p.Add(new Vector2(cylinderWall,0));
                p.Add(new Vector2(cylinderWall,0.04f));
                p.Add(new Vector2(R,0.06f));
                for (int i = 1; i <= N; i++)
                    p.Add(new Vector2(R,0.06f+(H-0.06f)*i/N));
                break;
            case 6:  /* 滴定管：尖嘴 → 旋塞 → 细长刻度管 */
                p.Add(new Vector2(R * 0.22f, 0));
                p.Add(new Vector2(R * 0.22f, H * 0.030f));
                p.Add(new Vector2(R * 0.62f, H * 0.055f));   /* 旋塞 */
                p.Add(new Vector2(R * 1.05f, H * 0.075f));
                p.Add(new Vector2(R * 1.05f, H * 0.105f));
                for (int i = 1; i <= 16; i++)
                    p.Add(new Vector2(R, H * (0.105f + 0.895f * i / 16f)));
                break;
            case 7:  /* 移液管：尖嘴 → 细颈 → 膨大球 → 细颈 */
                p.Add(new Vector2(.015f,0));
                p.Add(new Vector2(.03f,H*(.3f/5.3f)));
                p.Add(new Vector2(.03f,H*(1.65f/5.3f)));
                for (int i = 1; i <= 32; i++) {
                    float t=i/32f;
                    float r=Mathf.Sqrt(Mathf.Clamp01(Mathf.Sin(t*Mathf.PI)));
                    p.Add(new Vector2(.03f+(R-.03f)*r,H*((1.65f+1.4f*t)/5.3f)));
                }
                p.Add(new Vector2(.03f,H));
                break;
            case 8:  /* 试剂瓶：平底、直身、肩部、窄颈；不能复用敞口烧杯。 */
                p.Add(new Vector2(R*0.88f,0));
                p.Add(new Vector2(R,H*0.035f));
                p.Add(new Vector2(R,H*0.68f));
                p.Add(new Vector2(R*0.94f,H*0.74f));
                p.Add(new Vector2(R*0.56f,H*0.86f));
                p.Add(new Vector2(R*0.56f,H));
                break;
            case 4:  /* 圆底烧瓶 */
                {
                    float shoulder = H * 0.72f;
                    float endAngle = Mathf.PI - Mathf.Asin(0.34f);
                    for (int i = 0; i <= 18; i++) {
                        float a = (float)i / 18 * endAngle;
                        p.Add(new Vector2(R * Mathf.Sin(a), shoulder * (1f - Mathf.Cos(a)) / (1f - Mathf.Cos(endAngle))));
                    }
                    for (int i = 1; i <= 8; i++)
                        p.Add(new Vector2(R * 0.34f, shoulder + (H - shoulder) * i / 8f));
                }
                break;
            default: /* 蒸发皿 / 坩埚：口大底小的浅碗 */
                for (int i = 0; i <= N; i++) {
                    float t = (float)i / N;
                    p.Add(new Vector2(R * (0.55f + 0.45f * t), H * t));
                }
                break;
        }
        return p;
    }

    /// <summary>某个高度处的半径（按剖面插值）</summary>
    public static float RadiusAt(List<Vector2> prof, float yFrac) {
        yFrac = Mathf.Clamp01(yFrac);
        float total = prof[prof.Count - 1].y;
        float y = total * yFrac;
        for (int i = 1; i < prof.Count; i++) {
            if (prof[i].y >= y) {
                var a = prof[i - 1]; var b = prof[i];
                float k = Mathf.Approximately(b.y, a.y) ? 0 : (y - a.y) / (b.y - a.y);
                return Mathf.Lerp(a.x, b.x, k);
            }
        }
        return prof[prof.Count - 1].x;
    }

    /* ---------------------------------------------------------------
       旋转一个闭合的二维环路 → 水密网格
       loop 里的点按顺序首尾相连
       --------------------------------------------------------------- */
    public static Mesh RevolveLoop(List<Vector2> loop, int segs) {
        int n = loop.Count;
        var verts = new Vector3[n * (segs + 1)];
        var norms = new Vector3[verts.Length];
        var uvs   = new Vector2[verts.Length];

        for (int i = 0; i < n; i++) {
            var pt = loop[i];
            /* 环路在该点的法线（用前后点的切线旋转 90°） */
            Vector2 prev = loop[(i - 1 + n) % n];
            Vector2 next = loop[(i + 1) % n];
            Vector2 tan = (next - prev).normalized;
            Vector2 nrm2 = new Vector2(tan.y, -tan.x);

            for (int j = 0; j <= segs; j++) {
                float a = (float)j / segs * Mathf.PI * 2f;
                float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                int idx = i * (segs + 1) + j;
                verts[idx] = new Vector3(pt.x * cs, pt.y, pt.x * sn);
                /* 近轴顶点：法线必须取轴向，否则一圈薄三角形会渲染成扇形放射纹 */
                if (pt.x < 0.006f) norms[idx] = new Vector3(0f, nrm2.y >= 0f ? 1f : -1f, 0f);
                else               norms[idx] = new Vector3(nrm2.x * cs, nrm2.y, nrm2.x * sn);
                uvs[idx]   = new Vector2((float)j / segs, (float)i / n);
            }
        }

        var tris = new List<int>((n) * segs * 6);
        for (int i = 0; i < n; i++) {
            int i2 = (i + 1) % n;
            for (int j = 0; j < segs; j++) {
                int a = i * (segs + 1) + j;
                int b = i * (segs + 1) + j + 1;
                int c = i2 * (segs + 1) + j + 1;
                int d = i2 * (segs + 1) + j;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(a); tris.Add(d); tris.Add(c);
            }
        }

        var m = new Mesh { name = "Revolved" };
        m.indexFormat = verts.Length > 65000
            ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        m.vertices = verts; m.normals = norms; m.uv = uvs;
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }

    static readonly Dictionary<VesselKind,float> InnerCapacities = new Dictionary<VesselKind,float>();
    /// <summary>内腔容积（立方世界单位）。1 世界单位代表 0.1 米，
    /// 因此 1 立方世界单位对应 1 升（1000 mL）。</summary>
    public static float InnerCapacity(VesselKind kind) {
        if(InnerCapacities.TryGetValue(kind,out float cached))return cached;
        var g = Get(kind);
        var prof = Profile(g.Type, g.R, g.H);
        float w = Mathf.Max(0.012f, g.R * 0.052f);
        float vol = 0f;
        for (int i = 1; i < prof.Count; i++) {
            float r0 = Mathf.Max(0.0005f, prof[i - 1].x - w);
            float r1 = Mathf.Max(0.0005f, prof[i].x - w);
            float dy = prof[i].y - prof[i - 1].y;
            vol += Mathf.PI * (r0 * r0 + r0 * r1 + r1 * r1) / 3f * dy;   // 圆台
        }
        InnerCapacities[kind]=vol;
        return vol;   // 单位：立方世界单位
    }

    /// <summary>把容积（mL）换算成液面高度比例</summary>
    public static float FillFractionFor(VesselKind kind, double ml) {
        var g = Get(kind);
        var p = Profile(g.Type, g.R, g.H);
        float wall = Mathf.Max(0.012f, g.R * 0.052f);
        double remaining = System.Math.Max(0, ml) / 1000.0;
        for (int i = 1; i < p.Count; i++) {
            float h = p[i].y - p[i-1].y;
            float r0 = Mathf.Max(0.0005f, p[i-1].x-wall), r1 = Mathf.Max(0.0005f, p[i].x-wall);
            double vol = Mathf.PI * h * (r0*r0+r0*r1+r1*r1)/3;
            if (remaining > vol) { remaining -= vol; continue; }
            float lo = 0, hi = 1;
            for (int k = 0; k < 24; k++) {
                float f = (lo+hi)*0.5f, r = Mathf.Lerp(r0,r1,f);
                double partial = Mathf.PI * h*f * (r0*r0+r0*r+r*r)/3;
                if (partial < remaining) lo=f; else hi=f;
            }
            return (p[i-1].y+h*(lo+hi)*0.5f)/g.H;
        }
        return ml > 0 ? 1 : 0;
    }

    /// <summary>水平圆环（吸附提示用）</summary>
    public static Mesh BuildRing(float r0, float r1, int segs = 32) {
        var m = new Mesh { name = "Ring" };
        var v = new List<Vector3>();
        var t = new List<int>();
        for (int i = 0; i <= segs; i++) {
            float a = (float)i / segs * Mathf.PI * 2f;
            float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            v.Add(new Vector3(r0 * cs, 0f, r0 * sn));
            v.Add(new Vector3(r1 * cs, 0f, r1 * sn));
        }
        for (int i = 0; i < segs; i++) {
            int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
            t.Add(a); t.Add(c); t.Add(b);
            t.Add(b); t.Add(c); t.Add(d);
        }
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>玻璃外壳：外壁 + 内壁首尾相接成闭合环</summary>
    public static Mesh BuildGlass(VesselKind kind, int segs = 64) {
        return RevolveLoop(GlassShellProfile(kind), segs);
    }

    // Collision-only tessellation: remove intermediate collinear profile points,
    // not corners. Keep the original radial segmentation and render mesh intact.
    public static Mesh BuildCollisionGlass(VesselKind kind, int segs = 64) {
        var original=GlassShellProfile(kind);
        var simplified=new List<Vector2>(original);
        bool changed=true;
        while(changed&&simplified.Count>3){
            changed=false;
            for(int i=0;i<simplified.Count;i++){
                var a=simplified[(i+simplified.Count-1)%simplified.Count];
                var b=simplified[i];var c=simplified[(i+1)%simplified.Count];
                var span=c-a;float length=span.sqrMagnitude;
                if(length<1e-16f)continue;
                float t=Vector2.Dot(b-a,span)/length;
                if(t<0||t>1||(b-(a+span*t)).sqrMagnitude>1e-14f)continue;
                simplified.RemoveAt(i);changed=true;break;
            }
        }
        // Guard cumulative removal error: every original point must still lie
        // on the simplified closed profile to within 0.00001 mm.
        foreach(var point in original){
            float distance=float.PositiveInfinity;
            for(int i=0;i<simplified.Count;i++){
                var a=simplified[i];var span=simplified[(i+1)%simplified.Count]-a;
                float t=span.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,span)/span.sqrMagnitude):0;
                distance=Mathf.Min(distance,(point-a-span*t).sqrMagnitude);
            }
            if(distance>1e-14f)return RevolveLoop(original,segs);
        }
        return RevolveLoop(simplified,segs);
    }

    // Shared with thermal geometry: never estimate glass mass from nominal
    // liquid capacity. The visual shell and its physical volume use one loop.
    static List<Vector2> GlassShellProfile(VesselKind kind) {
        var g = Get(kind);
        var prof = Profile(g.Type, g.R, g.H);
        float w = Mathf.Max(0.012f, g.R * 0.052f);

        var loop = new List<Vector2>();
        /* 外壁自下而上 */
        for (int i = 0; i < prof.Count; i++) loop.Add(prof[i]);
        /* 内壁自上而下（半径减壁厚；试管这类圆弧底用整体内缩近似） */
        for (int i = prof.Count - 1; i >= 0; i--) {
            var pt = prof[i];
            float inner = Mathf.Max(kind==VesselKind.Cylinder?0f:(kind==VesselKind.Burette||kind==VesselKind.Pipette?.0005f:.02f), pt.x - w);
            float y = (i == 0) ? w : pt.y;     // 底部留出玻璃厚度
            loop.Add(new Vector2(inner, y));
        }
        return loop;
    }

    /// <summary>Axisymmetric shell material volume in mL, excluding separate
    /// feet, taps and accessories. This is geometry, not a material assignment:
    /// ceramic vessels must not be given glass density or heat capacity.</summary>
    public static double ShellVolumeMl(VesselKind kind) {
        var loop=GlassShellProfile(kind);
        double volume=0;
        for(int i=0;i<loop.Count;i++){
            var a=loop[i];var b=loop[(i+1)%loop.Count];
            double ra=a.x,rb=b.x;
            volume+=System.Math.PI/3*((double)b.y-a.y)*(ra*ra+ra*rb+rb*rb);
        }
        return System.Math.Abs(volume)*1000;
    }

    // Exterior body area including its bottom; open mouth and separately
    // modelled accessories are excluded. 1 world square = 0.01 m².
    public static double OuterSurfaceAreaM2(VesselKind kind){
        var g=Get(kind);var profile=Profile(g.Type,g.R,g.H);
        double area=System.Math.PI*profile[0].x*profile[0].x;
        for(int i=1;i<profile.Count;i++){
            var a=profile[i-1];var b=profile[i];double dr=(double)b.x-a.x,dy=(double)b.y-a.y;
            area+=System.Math.PI*((double)a.x+b.x)*System.Math.Sqrt(dr*dr+dy*dy);
        }
        return area*.01;
    }

    // Solid 60 mm across-corners hexagonal foot, 4 mm thick. Foot dimensions
    // are a modelling choice, not a manufacturer-certified metrology claim.
    public static Mesh BuildCylinderFoot() => RevolveLoop(new List<Vector2>{
        new Vector2(0,0),new Vector2(.30f,0),new Vector2(.30f,.04f),new Vector2(0,.04f)
    },6);

    /// <summary>液体：内腔在给定高度比例以下的实体（含底盖与顶面）</summary>
    public static Mesh BuildLiquid(VesselKind kind, float fillFrac, int segs = 48) {
        var g = Get(kind);
        var prof = Profile(g.Type, g.R, g.H);
        float w = Mathf.Max(0.012f, g.R * 0.052f);
        float totalH = prof[prof.Count - 1].y;
        float fillH = Mathf.Clamp(fillFrac, 0f, 0.96f) * totalH;

        /* 内腔轮廓里取 y <= fillH 的部分 */
        var inner = new List<Vector2>();
        foreach (var pt in prof) {
            if (pt.y > fillH) break;
            inner.Add(new Vector2(Mathf.Max(0.02f, pt.x - w), pt.y));
        }
        float rTop = RadiusAt(prof, Mathf.Clamp01(fillH / totalH)) - w;
        rTop = Mathf.Max(0.02f, rTop);

        /* 侧壁 + 底盖 + 顶盖，构成一个封闭体 */
        var loop = new List<Vector2>();
        for (int i = 0; i < inner.Count; i++) loop.Add(inner[i]);
        if (inner.Count == 0) loop.Add(new Vector2(rTop, 0));
        loop.Add(new Vector2(rTop, fillH));       // 顶面外侧
        /* 中轴：半径必须**正好是 0**。
           原来写 0.004f，想着「收到轴心」，但那是一根有面积的细柱；
           RevolveLoop 对 pt.x < 0.006f 的顶点会把法线强制成轴向，
           于是这根柱子按「水平盖面」打光，在液体正中显出一条深色竖线 ——
           试管里那根来路不明的「杆」就是它。正好 0 时顶点全部重合，
           三角形退化成零面积，什么都不画。 */
        // 多圈液面为二维波动模拟提供顶点，而不只是一个平圆盘。
        for (int i = 1; i <= 18; i++) loop.Add(new Vector2(rTop*(1f-i/18f), fillH));
        loop.Add(new Vector2(0f, 0f));           // 中轴：必须正好是 0（见下）
        return RevolveLoop(loop, segs);
    }

    /// <summary>沉淀 / 未溶固体：底部一层带起伏的堆料</summary>
    public static Mesh BuildSediment(VesselKind kind, float solidFrac, int segs = 40, int seed = 0) {
        var g = Get(kind);
        var prof = Profile(g.Type, g.R, g.H);
        float w = Mathf.Max(0.012f, g.R * 0.052f);
        float totalH = prof[prof.Count - 1].y;

        /* 堆高。原来只有 0.03 起步、而且是个平盘 —— 少量固体在白台面上几乎看不见。
           真实倒进容器的粉末会堆成一个小丘，所以这里：
             ① 抬高下限（少量也要看得见一层）
             ② 做成中间高、边缘低的锥形堆，而不是平板 */
        float frac = Mathf.Clamp(0.06f + solidFrac * 0.85f, 0.05f, 0.42f);
        float top = frac * totalH;

        var rnd = new System.Random(seed * 7919 + 13);
        var loop = new List<Vector2>();

        /* 侧壁：从堆底贴着容器内壁往上收 */
        int M = 14;
        for (int i = 0; i <= M; i++) {
            float t = (float)i / M;
            float y = Mathf.Lerp(w, top * 0.55f, t);
            float rr = RadiusAt(prof, Mathf.Clamp01(y / totalH)) - w;
            /* 越往上越收，像堆起来的粉 */
            rr *= Mathf.Lerp(1f, 0.72f, t);
            loop.Add(new Vector2(Mathf.Max(0.02f, rr), y));
        }

        /* 锥顶：中间隆起，表面带颗粒起伏 */
        int K = 22;
        float peak = top;
        for (int i = K; i >= 0; i--) {
            float t = (float)i / K;                       // 1 → 0：从边缘收到中心
            float baseR = Mathf.Max(0.02f, RadiusAt(prof, Mathf.Clamp01(peak / totalH)) - w) * 0.72f;
            float rr = baseR * t;
            /* 中心最高，边缘落到堆底；再叠一点随机颗粒感 */
            float cone = peak * (1f - t * t * 0.55f);
            /* 起伏放大到堆高的 45%，并叠一层高频颗粒 ——
               纯平滑的白堆放在白台面上会整片糊掉，看不出是固体。 */
            float bump = ((float)rnd.NextDouble() - 0.5f) * peak * 0.45f;
            bump += Mathf.Sin(t * 37.7f + seed) * peak * 0.055f;
            bump += Mathf.Sin(t * 91.3f + seed * 1.7f) * peak * 0.030f;
            loop.Add(new Vector2(rr, cone + bump));
        }

        return RevolveLoop(loop, segs);
    }
}

}
