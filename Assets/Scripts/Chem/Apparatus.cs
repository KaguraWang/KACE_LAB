/* =====================================================================
   实验台器材（不是容器，但容器可以吸附到它上面）
     BunsenBurner  本生灯：容器靠近时高亮，松开后吸附到灯上方；
                   点燃后加热容器，火力由出气量调节
     Tripod        三脚架：容器可以架在上面，配合本生灯使用
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;

namespace ChemLab.Chem {

public enum ApparatusKind { BunsenBurner, Tripod }
public static class ApparatusDimensions {
    // 1 world unit = 100 mm. Eisco CH0088D (150 mm), CH0644A (8.25 in).
    public const float BurnerHeight=1.5f,TripodHeight=2.0955f,TripodRadius=.60325f;
    public static float Top(ApparatusKind kind)=>kind==ApparatusKind.BunsenBurner?BurnerHeight:TripodHeight;
}

public class Apparatus {
    public string Id;
    public ApparatusKind Kind;
    public Vector3 Pos;              // 台面位置（y = 0 为台面）
    public bool Lit;                 // 本生灯是否点燃
    public double GasFlow = 0.5;     // 出气量 0–1，决定火力与火焰高度
    public Vessel Holds;             // 架/吸在上面的容器

    /// <summary>火焰高度（世界单位）。出气量太小时点不着。</summary>
    public float FlameHeight => Lit ? Mathf.Lerp(0.20f, 1.10f, (float)GasFlow) : 0f;

    /// <summary>有火时的加热功率（℃/s）。出气量越大升温越快。</summary>
    public float HeatRate => Lit ? Mathf.Lerp(8f, 60f, (float)GasFlow) : 0f;

    /// <summary>可点着的最低出气量</summary>
    public bool CanLight => GasFlow >= 0.12;
}

public static class ApparatusSet {
    public static readonly List<Apparatus> All = new List<Apparatus>();
    static int _next = 1;

    public static Apparatus Add(ApparatusKind kind, Vector3 pos) {
        var a = new Apparatus { Id = "ap" + (_next++), Kind = kind, Pos = pos };
        All.Add(a);
        return a;
    }

    public static void Clear() { All.Clear(); _next = 1; }

    // Identify the burner independently of its lit state, so extinguishing it
    // cannot accidentally switch a supported vessel back to an ideal heater.
    public static Apparatus HeatingBurner(Vessel v) {
        if(v==null)return null;
        if(v.Burner!=null&&v.Burner.Kind==ApparatusKind.BunsenBurner&&All.Contains(v.Burner))return v.Burner;
        Apparatus nearest=null;float closest=.22f*.22f;
        foreach(var a in All){
            if(a.Kind!=ApparatusKind.BunsenBurner||(a.Holds!=null&&a.Holds!=v))continue;
            var d=v.Pos+Vector3.up*v.LiftY-a.Pos;
            // The maximum supported flame reach is 1.10 units; retain the
            // existing 0.10-unit teaching contact tolerance, not a heat-flux law.
            if(d.y<ApparatusDimensions.BurnerHeight||d.y>ApparatusDimensions.BurnerHeight+1.10f+.1f)continue;
            float distance=d.x*d.x+d.z*d.z;
            if(distance<closest){closest=distance;nearest=a;}
        }
        return nearest;
    }
    public static bool BurnerHeats(Vessel v,Apparatus burner){
        if(v==null||burner==null||!burner.Lit||!burner.CanLight)return false;
        if(v.Burner==burner)return true;
        var d=v.Pos+Vector3.up*v.LiftY-burner.Pos;
        return d.x*d.x+d.z*d.z<.22f*.22f&&d.y>=ApparatusDimensions.BurnerHeight&&
            d.y<=ApparatusDimensions.BurnerHeight+burner.FlameHeight+.1f;
    }

    /// <summary>把容器吸附到本生灯上（或从灯上取下）</summary>
    public static void Attach(Vessel v, Apparatus a) {
        if (v == null) return;
        if (v.Burner != null && v.Burner.Holds == v) v.Burner.Holds = null;
        v.Burner = a;
        if (a != null) a.Holds = v;
    }

    /// <summary>每帧推进：点燃的灯给上面的容器加热</summary>
    public static void Tick(float dt) {
        // Own the pause contract here as well as in the frame driver, so direct
        // simulation steps cannot heat other vessels during a pour transaction.
        if (PourTransaction.IsActive) return;
        foreach (var a in All) {
            if (a.Kind != ApparatusKind.BunsenBurner) continue;
            if (a.Lit && !a.CanLight) a.Lit = false;      // 气量不足自动熄灭
            var v = a.Holds;
            if (v == null) {
                // 拖到三脚架下的本生灯按实际位置加热，不要求建立直接吸附关系。
                if (a.Lit) foreach (var nearby in Lab.Vessels) {
                    if(DistillationAssembly.OwnsThermal(nearby))continue;
                    if (HeatingBurner(nearby)==a&&BurnerHeats(nearby,a))
                        WaterIce.AdvanceTemperature(nearby,Mathf.Min(1150f, nearby.Temp + dt * a.HeatRate));
                }
                continue;
            }
            if (a.Lit) {
                float rate = a.HeatRate;
                if(!DistillationAssembly.OwnsThermal(v))WaterIce.AdvanceTemperature(v,Mathf.Min(1150f, v.Temp + dt * rate));
                v.Fx.Flame = true;
            }
        }
    }
}

}
