using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
// Decorative dimensions are teaching-model choices, in 100 mm world units.
public static class ApparatusDetails {
    public const float GauzeTopOffset=.03f;
    public static Mesh Inlet(){
        var profile=new List<Vector2>{new Vector2(0,0),new Vector2(.035f,0)};
        for(int i=0;i<4;i++){profile.Add(new Vector2(.042f,.03f+i*.045f));profile.Add(new Vector2(.032f,.055f+i*.045f));}
        profile.Add(new Vector2(.028f,.22f));profile.Add(new Vector2(.018f,.22f));
        profile.Add(new Vector2(.018f,0));profile.Add(new Vector2(0,0));
        return VesselMesh.RevolveLoop(profile,24);
    }
    public static Mesh Gauze(){
        var rod=VesselMesh.RevolveLoop(new List<Vector2>{new Vector2(0,0),new Vector2(.004f,0),new Vector2(.004f,1.30f),new Vector2(0,1.30f)},8);
        var parts=new List<CombineInstance>();float height=ApparatusDimensions.TripodHeight;
        for(int i=0;i<=32;i++){
            float offset=-.64f+i*.04f;
            parts.Add(new CombineInstance{mesh=rod,transform=Matrix4x4.TRS(new Vector3(-.65f,height+.004f,offset),Quaternion.Euler(0,0,-90),Vector3.one)});
            parts.Add(new CombineInstance{mesh=rod,transform=Matrix4x4.TRS(new Vector3(offset,height+.012f,-.65f),Quaternion.Euler(90,0,0),Vector3.one)});
        }
        var result=new Mesh{name="WovenMetalGauze"};result.CombineMeshes(parts.ToArray(),true,true);
        Object.Destroy(rod);return result;
    }
    public static Mesh Ceramic(){
        float bottom=ApparatusDimensions.TripodHeight+.008f,top=ApparatusDimensions.TripodHeight+GauzeTopOffset;
        var result=VesselMesh.RevolveLoop(new List<Vector2>{new Vector2(0,bottom),new Vector2(.35f,bottom),new Vector2(.35f,top),new Vector2(0,top)},48);
        var colors=new Color[result.vertexCount];var traits=new Vector2[result.vertexCount];
        for(int i=0;i<colors.Length;i++){colors[i]=Color.white;traits[i]=new Vector2(0,.18f);}
        result.colors=colors;result.uv=traits;return result;
    }
}
}
