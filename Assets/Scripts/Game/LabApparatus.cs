/* =====================================================================
   器材视图 + 吸附控制器
     - 本生灯有可调火焰，点燃时按出气量给上方容器加热
     - 拖动容器时，靠近器材或别的容器会高亮并吸附
     - 容器悬在另一个容器上方时，可以按滑动条比例倾倒
   ===================================================================== */
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {

public class ApparatusView : MonoBehaviour {
    public Apparatus Model;
    public Transform Flame;
    public MeshRenderer Glow;          // 吸附提示环
    float _last = -1f;
    Material _ceramicMaterial,_brassMaterial;
    readonly List<Mesh> _detailMeshes=new List<Mesh>();
    void AddDetail(string name,Mesh mesh,Material material,Vector3 position,Quaternion rotation){
        var part=new GameObject(name);part.transform.SetParent(transform,false);part.transform.localPosition=position;part.transform.localRotation=rotation;
        part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;
        part.AddComponent<MeshCollider>().sharedMesh=mesh;_detailMeshes.Add(mesh);
    }
    void OnDestroy(){foreach(var mesh in _detailMeshes)if(mesh!=null)Destroy(mesh);if(_ceramicMaterial!=null)Destroy(_ceramicMaterial);if(_brassMaterial!=null)Destroy(_brassMaterial);}

    public static ApparatusView Create(Apparatus a, Transform parent, Material body, Material glow, Material flame) {
        var go = new GameObject("App_" + a.Kind + "_" + a.Id);
        go.transform.SetParent(parent, false);
        var av = go.AddComponent<ApparatusView>();
        av.Model = a;

        Mesh mesh = a.Kind == ApparatusKind.BunsenBurner
                  ? ApparatusMesh.BuildBunsenBurner()
                  : ApparatusMesh.BuildTripodRing();
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        mr.sharedMaterial = body;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        if (a.Kind == ApparatusKind.Tripod) {
            av.AddDetail("WireGauze",ApparatusDetails.Gauze(),body,Vector3.zero,Quaternion.identity);
            av._ceramicMaterial=new Material(Shader.Find("ChemLab/Solid"));av._ceramicMaterial.SetColor("_Color",new Color(.87f,.86f,.80f));
            av.AddDetail("CeramicCentre",ApparatusDetails.Ceramic(),av._ceramicMaterial,Vector3.zero,Quaternion.identity);
            for (int i = 0; i < 3; i++) {
                var lg = new GameObject("Leg" + i);
                lg.transform.SetParent(go.transform, false);
                lg.AddComponent<MeshFilter>().sharedMesh = ApparatusMesh.BuildTripodLeg(i);
                lg.AddComponent<MeshCollider>().sharedMesh = lg.GetComponent<MeshFilter>().sharedMesh;
                var lr = lg.AddComponent<MeshRenderer>();
                lr.sharedMaterial = body;
            }
        }

        if(a.Kind==ApparatusKind.BunsenBurner){
            av._brassMaterial=new Material(body);av._brassMaterial.SetColor("_Color",new Color(.65f,.44f,.16f));
            av.AddDetail("GasInlet",ApparatusDetails.Inlet(),av._brassMaterial,new Vector3(.06f,.14f,0),Quaternion.Euler(0,0,-90));
        }

        /* 吸附提示环：拖到附近时亮起 */
        var rg = new GameObject("Glow");
        rg.transform.SetParent(go.transform, false);
        rg.transform.localPosition = new Vector3(0f, ApparatusDimensions.Top(a.Kind)+.02f, 0f);
        var rmf = rg.AddComponent<MeshFilter>();
        var rmr = rg.AddComponent<MeshRenderer>();
        rmf.sharedMesh = RingMesh(0.30f, 0.36f);
        rmr.sharedMaterial = glow;
        rmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        av.Glow = rmr;
        rmr.enabled = false;

        /* 火焰：锥体，按出气量缩放 */
        if (a.Kind == ApparatusKind.BunsenBurner) {
            var fg = new GameObject("Flame");
            fg.transform.SetParent(go.transform, false);
            fg.transform.localPosition = new Vector3(0f, ApparatusDimensions.BurnerHeight, 0f);
            var fmf = fg.AddComponent<MeshFilter>();
            var fmr = fg.AddComponent<MeshRenderer>();
            fmf.sharedMesh = ConeMesh(0.075f, 1.0f);
            fmr.sharedMaterial = flame;
            fmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fmr.receiveShadows = false;
            av.Flame = fg.transform;
            fg.SetActive(false);
        }
        return av;
    }

    public void SetHighlight(bool on) {
        if (Glow != null) Glow.enabled = on;
    }

    public void Sync() {
        var a = Model;
        transform.localPosition = a.Pos + Vector3.up * 0.003f;   /* 同容器，避免和台面 z-fighting */
        if (Flame != null) {
            bool lit = a.Lit && a.CanLight;
            if (Flame.gameObject.activeSelf != lit) Flame.gameObject.SetActive(lit);
            if (lit) {
                float h = a.FlameHeight;
                if (Mathf.Abs(h - _last) > 1e-4f) {
                    _last = h;
                    Flame.localScale = new Vector3(1f, h, 1f);
                }
            }
        }
    }

    /* ---------------- 程序化小网格 ---------------- */
    static Mesh RingMesh(float r0, float r1, int segs = 32) {
        var m = new Mesh { name = "Ring" };
        var v = new List<Vector3>(); var t = new List<int>();
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

    static Mesh ConeMesh(float r, float h, int segs = 20) {
        var m = new Mesh { name = "Flame" };
        var v = new List<Vector3> { new Vector3(0f, h, 0f) };
        for (int i = 0; i <= segs; i++) {
            float a = (float)i / segs * Mathf.PI * 2f;
            v.Add(new Vector3(r * Mathf.Cos(a), 0f, r * Mathf.Sin(a)));
        }
        var t = new List<int>();
        for (int i = 0; i < segs; i++) { t.Add(0); t.Add(i + 1); t.Add(i + 2); }
        for (int i = 1; i + 2 < v.Count; i++) { t.Add(1); t.Add(i + 1); t.Add(i); }
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}

/* =====================================================================
   吸附控制器：把「拖动中的容器」吸附到附近的器材或容器上
   ===================================================================== */
public static class SnapController {
    // A staged pour is temporary: restore both sides of apparatus ownership,
    // not just the vessel's position after dragging detached it.
    public sealed class Placement {
        readonly Vessel vessel,attached;
        readonly Apparatus burner,tripod;
        readonly Vector3 position;
        readonly float lift;
        public Placement(Vessel v){vessel=v;attached=v.AttachedTo;burner=v.Burner;tripod=v.TripodRef;position=v.Pos;lift=v.LiftY;}
        public void Restore(){
            Detach(vessel);vessel.Pos=position;vessel.LiftY=lift;vessel.AttachedTo=attached;
            vessel.Burner=burner;vessel.TripodRef=tripod;
            if(burner!=null)burner.Holds=vessel;if(tripod!=null)tripod.Holds=vessel;
        }
    }
    public const float Radius = 0.62f;      // 吸附半径（世界单位）
    public const float StackGap = 0.06f;    // 悬停时留的间隙
    public static bool AttachMetered(Vessel tool,Vessel target){
        if(!MeteredTransfer.CanAttach(tool,target))return false;
        Detach(tool);tool.AttachedTo=target;tool.Pos=target.Pos;tool.LiftY=target.LiftY+LiftOnto(target,tool);return true;
    }

    /// <summary>找出离 p 最近的吸附点。返回 null 表示不吸附。</summary>
    public static Vessel NearestVessel(Vector3 p, Vessel exclude) {
        Vessel best = null; float bd = Radius;
        foreach (var v in Lab.Vessels) {
            if (v == exclude) continue;
            if(MeteredTransfer.IsTool(exclude)&&!MeteredTransfer.CanAttach(exclude,v))continue;
            if (v.AttachedTo != null) continue;               // 不允许叠三层
            float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(v.Pos.x, v.Pos.z));
            if (d < bd) { bd = d; best = v; }
        }
        return best;
    }

    public static Apparatus NearestApparatus(Vector3 p, ApparatusKind kind) {
        Apparatus best = null; float bd = Radius;
        foreach (var a in ApparatusSet.All) {
            if (a.Kind != kind) continue;
            if (a.Holds != null) continue;                    // 已经架着一个了
            float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.Pos.x, a.Pos.z));
            if (d < bd) { bd = d; best = a; }
        }
        return best;
    }

    /// <summary>容器悬停在另一个容器上方时应处的高度</summary>
    public static float LiftOnto(Vessel lower, Vessel upper) {
        float hLow = VesselMesh.Get(lower.Kind).H;
        return hLow + StackGap;
    }

    public static float LiftOntoApparatus(Apparatus a) {
        return ApparatusDimensions.Top(a.Kind) + (a.Kind==ApparatusKind.Tripod?ApparatusDetails.GauzeTopOffset:StackGap);
    }

    /// <summary>解除所有吸附</summary>
    public static void Detach(Vessel v) {
        if (v == null) return;
        v.MeteredFlowMlPerSecond=0;
        if (v.AttachedTo != null) v.AttachedTo = null;
        if (v.Burner != null) { if (v.Burner.Holds == v) v.Burner.Holds = null; v.Burner = null; }
        if (v.TripodRef != null) { if (v.TripodRef.Holds == v) v.TripodRef.Holds = null; v.TripodRef = null; }
        v.LiftY = 0f;
    }
}

}
