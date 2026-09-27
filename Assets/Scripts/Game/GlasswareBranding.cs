using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {
// Ink is a shallow curved decal on the exterior, with no collider or liquid volume.
// The camera starts on +Z. This sector faces it while remaining separate
// from graduations at -PI/2 and their numerals at -PI/2+.36.
public sealed class GlasswareBranding : MonoBehaviour {
    const string TexturePath="Branding/kazewa-industries-transparent";
    const float CenterAngle=1.0f;
    const float SurfaceOffset=.0018f;
    static Texture2D artwork;
    static Material ink;
    Mesh mesh;

    static Material Ink {
        get {
            if(ink!=null)return ink;
            artwork=Resources.Load<Texture2D>(TexturePath);
            var shader=Shader.Find("ChemLab/GlassMark");
            if(artwork==null||shader==null){Debug.LogError("[ChemLab] Missing transparent glassware brand artwork or shader");return null;}
            ink=new Material(shader){name="Kazewa glass print"};
            ink.mainTexture=artwork;
            return ink;
        }
    }

    public static void CreateVesselMark(Transform glass,VesselKind kind){
        if(kind==VesselKind.Crucible||kind==VesselKind.Dish)return;
        var geometry=VesselMesh.Get(kind);
        var profile=VesselMesh.Profile(geometry.Type,geometry.R,geometry.H);
        // Height windows are on the body, away from rims, feet, neck marks and
        // the calibrated pipette's full-circumference capacity line near its top.
        float center,angularWidth,height;
        switch(kind){
            case VesselKind.Beaker: center=.52f;angularWidth=1.18f;height=.39f;break;
            case VesselKind.Flask: center=.45f;angularWidth=1.13f;height=.37f;break;
            case VesselKind.Bottle: center=.48f;angularWidth=1.18f;height=.40f;break;
            case VesselKind.Cylinder: center=.49f;angularWidth=1.48f;height=.21f;break;
            case VesselKind.Tube: center=.55f;angularWidth=1.75f;height=.17f;break;
            case VesselKind.RoundBottom: center=.53f;angularWidth=1.15f;height=.43f;break;
            case VesselKind.Burette: center=.48f;angularWidth=1.62f;height=.095f;break;
            case VesselKind.Pipette: center=.45f;angularWidth=1.57f;height=.095f;break;
            default:return;
        }
        float y=geometry.H*center;
        float radius=VesselMesh.RadiusAt(profile,center);
        // A square image occupies its natural proportions on the curved wall.
        height=Mathf.Min(height,radius*angularWidth);
        var mark=NewMark(glass,"Kazewa surface print");
        mark.mesh=CurvedPatch(y,height,angularWidth,atY=>VesselMesh.RadiusAt(profile,atY/geometry.H)+SurfaceOffset);
        Finish(mark);
    }

    public static void CreateTubeMark(Transform tube,float radius,float length){
        if(radius<=0||length<=0)return;
        var mark=NewMark(tube,"Kazewa surface print");
        // Tube mesh is unit height/radius and scaled by its parent. Preserve
        // the logo's world-space aspect ratio through that nonuniform scale.
        float angularWidth=1.65f;
        float physicalHeight=Mathf.Min(radius*angularWidth,length*.42f);
        float normalizedHeight=physicalHeight/length;
        mark.mesh=CurvedPatch(.52f,normalizedHeight,angularWidth,y=>1f+SurfaceOffset/radius);
        Finish(mark);
    }

    static GlasswareBranding NewMark(Transform parent,string name){
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        return go.AddComponent<GlasswareBranding>();
    }

    static Mesh CurvedPatch(float centerY,float height,float angularWidth,System.Func<float,float> radiusAt){
        const int across=24,up=12;
        var vertices=new List<Vector3>((across+1)*(up+1));
        var uv=new List<Vector2>(vertices.Capacity);
        var triangles=new List<int>(across*up*6);
        for(int j=0;j<=up;j++){
            float v=(float)j/up;
            float y=centerY+(v-.5f)*height;
            float r=radiusAt(y);
            for(int i=0;i<=across;i++){
                float u=(float)i/across;
                float angle=CenterAngle+(u-.5f)*angularWidth;
                vertices.Add(new Vector3(Mathf.Cos(angle)*r,y,Mathf.Sin(angle)*r));
                uv.Add(new Vector2(u,v));
                if(i==0||j==0)continue;
                int n=j*(across+1)+i;
                // Front faces point outward so rear-wall ink is culled.
                triangles.Add(n-across-2);triangles.Add(n-1);triangles.Add(n);
                triangles.Add(n-across-2);triangles.Add(n);triangles.Add(n-across-1);
            }
        }
        var result=new Mesh{name="Curved Kazewa print"};
        result.SetVertices(vertices);result.SetUVs(0,uv);result.SetTriangles(triangles,0);
        result.RecalculateNormals();result.RecalculateBounds();
        return result;
    }

    static void Finish(GlasswareBranding mark){
        mark.gameObject.AddComponent<MeshFilter>().sharedMesh=mark.mesh;
        var renderer=mark.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=Ink;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows=false;
    }

    void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
}
