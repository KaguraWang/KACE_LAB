/* =====================================================================
   器皿的粒子效果：气泡 / 烟雾 / 火焰
   ---------------------------------------------------------------------
   不用 ParticleSystem 的发射器 —— 它的时序依赖游戏循环，无头渲染时不跑。
   这里改成「按时间解析布点」：每个粒子的位置/大小/透明度由公式直接算出，
   给定同一个 t 结果永远一致，有头无头都对。
   四边形在 CPU 侧做朝向相机的公告板。
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {

public class VesselFx : MonoBehaviour {
    class P { public float sx, sz, phase, rise, size, sway; }

    MeshFilter _mfB, _mfS, _mfF;
    Mesh _mB, _mS, _mF;
    readonly List<P> _bub = new List<P>();
    readonly List<P> _smk = new List<P>();
    readonly List<P> _flm = new List<P>();
    Vessel _model;

    static readonly List<Vector3> _v = new List<Vector3>();
    static readonly List<Color> _c = new List<Color>();
    static readonly List<Vector2> _uv = new List<Vector2>();
    static readonly List<int> _t = new List<int>();

    const int NB = 48, NS = 28, NF = 24;

    public static VesselFx Create(Transform parent, Material matAlpha, Material matAdd, Vessel model) {
        var go = new GameObject("Fx");
        go.transform.SetParent(parent, false);
        var fx = go.AddComponent<VesselFx>();
        fx._model = model;

        var rnd = new System.Random(model != null ? model.Id.GetHashCode() : 1);
        void Fill(List<P> list, int n) {
            for (int i = 0; i < n; i++) {
                list.Add(new P {
                    sx = (float)(rnd.NextDouble() * 2 - 1),
                    sz = (float)(rnd.NextDouble() * 2 - 1),
                    phase = (float)rnd.NextDouble(),
                    rise = 0.65f + (float)rnd.NextDouble() * 0.7f,
                    size = 0.7f + (float)rnd.NextDouble() * 0.7f,
                    sway = (float)(rnd.NextDouble() * 2 - 1),
                });
            }
        }
        Fill(fx._bub, NB); Fill(fx._smk, NS); Fill(fx._flm, NF);

        fx._mfB = fx.MakeMesh("Bubbles", go.transform, matAlpha, out fx._mB);
        fx._mfS = fx.MakeMesh("Smoke",   go.transform, matAlpha, out fx._mS);
        /* 火焰用 alpha 混合：加法混合叠在近乎纯白的台面上根本看不出来 */
        fx._mfF = fx.MakeMesh("Flame",   go.transform, matAlpha, out fx._mF);
        return fx;
    }

    MeshFilter MakeMesh(string name, Transform parent, Material mat, out Mesh mesh) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mesh = new Mesh { name = name };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        mf.sharedMesh = mesh;
        var material = new Material(mat);
        material.SetFloat("_ParticleKind", name=="Bubbles" ? 0 : name=="Smoke" ? 1 : 2);
        VesselBoundary.Configure(material, _model.Kind);
        mr.sharedMaterial = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return mf;
    }

    /* ---------------------------------------------------------------
       把一组粒子铺成朝向相机的公告板四边形
       --------------------------------------------------------------- */
    void Build(List<P> list, Mesh mesh, Camera cam, int active,
               System.Func<int, float, Vector3> pos,
               System.Func<int, float, float> size,
               System.Func<int, float, Color> col) {
        if (mesh == null) return;
        _v.Clear(); _c.Clear(); _uv.Clear(); _t.Clear();

        if (cam != null && active > 0) {
            Vector3 right = transform.InverseTransformDirection(cam.transform.right), up = transform.InverseTransformDirection(cam.transform.up);
            /* pos() 回调返回的**已经是局部坐标**（相对 FX 对象）。
               这里以前又减了一次 transform.position（世界坐标），
               于是粒子被整体推偏、器皿一动粒子还朝反方向跑。
               网格本来就是在 FX 的局部空间里建的，不需要转世界坐标。 */

            for (int i = 0; i < active && i < list.Count; i++) {
                float t = Mathf.Repeat(_time + list[i].phase, 1f);
                Vector3 p = pos(i, t);                   // 本来就是局部坐标，不要再减 origin
                float s = size(i, t);
                if (s <= 0.0001f) continue;
                Color c = col(i, t);
                if (c.a <= 0.004f) continue;

                int b = _v.Count;
                Vector3 r = right * s, u = up * s * (mesh==_mF ? 2.1f : 1f);
                _v.Add(p - r - u); _uv.Add(new Vector2(0, 0));
                _v.Add(p + r - u); _uv.Add(new Vector2(1, 0));
                _v.Add(p + r + u); _uv.Add(new Vector2(1, 1));
                _v.Add(p - r + u); _uv.Add(new Vector2(0, 1));
                for (int k = 0; k < 4; k++) _c.Add(c);
                _t.Add(b); _t.Add(b + 1); _t.Add(b + 2);
                _t.Add(b); _t.Add(b + 2); _t.Add(b + 3);
            }
        }

        mesh.Clear();
        if (_v.Count == 0) return;
        mesh.SetVertices(_v); mesh.SetColors(_c); mesh.SetUVs(0, _uv);
        mesh.SetTriangles(_t, 0);
        mesh.RecalculateBounds();
    }

    float _time;
    void OnDestroy() {
        foreach(var mf in new[]{_mfB,_mfS,_mfF}) if(mf!=null) {
            Destroy(mf.sharedMesh);
            Destroy(mf.GetComponent<MeshRenderer>().sharedMaterial);
        }
    }
    float _R = 0.3f, _H = 0.8f, _fill = 0.3f;
    float _bubAmt, _smkAmt;
    bool _flameOn;

    public void Sync(float time, Camera cam, float R, float H, float fill) {
        _time = time; _R = R; _H = H; _fill = Mathf.Clamp(fill, 0.02f, 0.95f);
        if (_model == null) return;
        _bubAmt = Mathf.Clamp01(_model.Fx.Bubble / 1.6f);
        _smkAmt = Mathf.Clamp01(_model.Fx.Smoke / 1.6f);
        _flameOn = _model.Fx.Flame && _model.Burner == null;
        float closed=_model.Sealed?1:0;
        _mfB.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_ClosedContainer",closed);
        _mfS.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_ClosedContainer",closed);
        _mfF.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_ClosedContainer",closed);

        float surfY = _H * _fill;
        float innerR = _R * 0.72f;
        _mfB.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_LiquidHeight",surfY);
        var profile=VesselMesh.Profile(VesselMesh.Get(_model.Kind).Type,R,H);
        float wall=Mathf.Max(0.012f,R*0.052f);
        float Radius(float y) => Mathf.Max(0.005f,VesselMesh.RadiusAt(profile,Mathf.Clamp01(y/H))-wall);

        /* ---- 气泡：底部升到液面，到顶归零重来 ---- */
        int nb = Mathf.RoundToInt(NB * _bubAmt);
        Build(_bub, _mB, cam, nb,
            (i, t) => {
                var q = _bub[i];
                float y = _H * 0.03f + t * (surfY - _H * 0.03f);
                float sw = Mathf.Sin(t * 12.56f + q.phase * 9f) * innerR * 0.18f;
                var radial=Vector2.ClampMagnitude(new Vector2(q.sx*0.7f+sw*q.sway, q.sz*0.7f),0.85f)*Radius(y);
                return new Vector3(radial.x, y, radial.y);
            },
            (i, t) => 0.007f * _bub[i].size * (0.5f + 0.7f * t),
            (i, t) => new Color(0.93f, 0.97f, 1f, Mathf.Sin(t * Mathf.PI) * 0.9f));

        /* ---- 烟雾 / 逸出气体：液面以上，边升边散 ---- */
        int ns = Mathf.RoundToInt(NS * _smkAmt);
        Color gc = GasColor(_model);
        Build(_smk, _mS, cam, ns,
            (i, t) => {
                var q = _smk[i];
                float y = _model.Sealed ? Mathf.Lerp(surfY,H,t) : surfY + t * _H * 1.5f * q.rise;
                float spread=y<H ? Radius(y)*0.65f : Radius(H)+(y-H)*0.26f;
                var radial=Vector2.ClampMagnitude(new Vector2(q.sx,q.sz),1)*spread;
                radial.x+=Mathf.Sin(t*5+q.phase*7)*Mathf.Max(0,y-H)*0.12f;
                return new Vector3(radial.x,y,radial.y);
            },
            (i, t) => 0.058f * _smk[i].size * (0.6f + t * 1.8f),
            (i, t) => new Color(gc.r, gc.g, gc.b, Mathf.Sin(t * Mathf.PI) * 0.36f));

        /* ---- 火焰：器皿底下一小簇，往上收拢 ---- */
        Color fc = FlameColor(_model);
        Build(_flm, _mF, cam, _flameOn ? NF : 0,
            (i, t) => {
                var q = _flm[i];
                /* 火焰从器皿底部往外上方窜，铺开到器皿外圈，才看得见 */
                float y = Mathf.Max(surfY,_H*0.72f) + t * _R * 1.1f * q.rise;
                float spread = Radius(Mathf.Min(y,H))*(0.5f-t*0.35f);
                float ang = q.phase * 6.2832f;
                return new Vector3(Mathf.Cos(ang)*spread,y,Mathf.Sin(ang)*spread);
            },
            (i, t) => 0.045f * _flm[i].size * (1.0f - t * 0.65f),
            (i, t) => new Color(Mathf.Lerp(1.00f, fc.r, t),
                                Mathf.Lerp(0.94f, fc.g, t),
                                Mathf.Lerp(0.70f, fc.b, t),
                                Mathf.Sin(t * Mathf.PI) * 0.95f));
    }

    static Color GasColor(Vessel m) {
        if (m.Gases.Count > 0) {
            foreach (var kv in m.Gases) {
                var s = ChemData.Sub[kv.Key];
                if (!string.IsNullOrEmpty(s.SolidColor) && ColorUtility.TryParseHtmlString(s.SolidColor, out Color c))
                    return c;
            }
        }
        return new Color(0.86f, 0.90f, 0.94f);
    }

    static Color FlameColor(Vessel m) {
        string hex = m.Fx.FlameColor ?? "#ffb84d";
        return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : new Color(1f, 0.72f, 0.30f);
    }
}

}
