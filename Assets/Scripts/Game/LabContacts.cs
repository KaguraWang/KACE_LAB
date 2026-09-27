using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {
// Splint snapping is separate from the burner vessel stand: a vessel and a
// splint may both use the same flame. The tip is positioned inside the flame,
// not merely near the burner base in screen space.
public static class SplintSnap {
    public const float TipHeight=0.62f;
    public const float OffsetX=0.07f;
    public const float TipAboveNozzle=0.22f;

    public static Apparatus NearestBurner(Vector3 position) {
        Apparatus best=null;float distance=SnapController.Radius;
        foreach(var burner in ApparatusSet.All)if(burner.Kind==ApparatusKind.BunsenBurner){
            float d=Vector2.Distance(new Vector2(position.x,position.z),new Vector2(burner.Pos.x,burner.Pos.z));
            if(d<distance){distance=d;best=burner;}
        }
        return best;
    }
    public static bool Attach(TestItem splint,Apparatus burner) {
        if(splint==null||splint.Kind!=TestItemKind.Splint||burner==null||
           burner.Kind!=ApparatusKind.BunsenBurner||!ApparatusSet.All.Contains(burner))return false;
        splint.AttachedBurner=burner;
        Follow(splint);
        return true;
    }
    public static void Follow(TestItem splint) {
        if(splint==null||splint.AttachedBurner==null)return;
        var burner=splint.AttachedBurner;
        if(!ApparatusSet.All.Contains(burner)){splint.AttachedBurner=null;return;}
        splint.Pos=burner.Pos+Vector3.right*OffsetX;
        splint.LiftY=ApparatusDimensions.BurnerHeight+TipAboveNozzle-TipHeight;
    }
    public static void MoveAside(TestItem splint) {
        if(splint==null||splint.AttachedBurner==null)return;
        var burner=splint.AttachedBurner;
        splint.AttachedBurner=null;
        splint.Pos=burner.Pos+Vector3.right*(SnapController.Radius+0.18f);
        splint.LiftY=0f;
    }
}
// 从可见道具的接触端与容器局部剖面判断接触，绝不按屏幕距离触发反应。
public static class LabContacts {
    public static Vector3 Tip(TestItem t) => t.Pos + Vector3.up *
        (t.LiftY + (t.Kind == TestItemKind.Splint ? SplintSnap.TipHeight : 0.02f));

    public static void Tick() {
        foreach (var t in TestSet.All) {
            var tip = Tip(t);
            bool inFlame = false;
            foreach (var a in ApparatusSet.All) {
                if (a.Kind != ApparatusKind.BunsenBurner || !a.Lit || !a.CanLight) continue;
                var d = tip - a.Pos;
                if (new Vector2(d.x, d.z).magnitude < 0.11f && d.y >= ApparatusDimensions.BurnerHeight-.01f && d.y <= ApparatusDimensions.BurnerHeight+.02f + a.FlameHeight)
                    inFlame = true;
            }
            Vessel contact = null; bool liquid = false;
            foreach (var v in Lab.Vessels) {
                var geo = VesselMesh.Get(v.Kind);
                var d = tip - (v.Pos + Vector3.up * v.LiftY);
                if (d.y < 0 || d.y > geo.H + 0.10f) continue;
                var radius = VesselMesh.RadiusAt(VesselMesh.Profile(geo.Type, geo.R, geo.H), d.y / geo.H);
                if (new Vector2(d.x, d.z).magnitude > Mathf.Max(0.02f, radius - 0.015f)) continue;
                contact = v;
                liquid = v.Volume() > 0.5 && d.y <= Mathf.Clamp(VesselMesh.FillFractionFor(v.Kind, v.Volume()), 0, 0.96f) * geo.H;
                if (!liquid && v.Fx.Flame && d.y >= geo.H * 0.8f) inFlame = true;
                break;
            }
            if (inFlame && t.Kind == TestItemKind.Splint && !liquid) GasTest.Light(t);
            string key = contact == null ? "" : contact.Id + "|" + (liquid
                ? "liquid|" + (contact.Ph < 6.99 ? "acid" : contact.Ph > 7.01 ? "base" : "neutral")
                : "gas|" + GasTest.DominantGas(contact)) + "|" + t.Splint + "|" + t.Moist;
            if (contact != null && key != t.ContactKey) {
                if (liquid) GasTest.DipLiquid(contact, t);
                else GasTest.Insert(contact, t);
            }
            t.ContactKey = key;
        }
    }
}
}
