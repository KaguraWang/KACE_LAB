using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
// Identifying physical details, not extra fluid chambers. Dimensions of the
// stopcock are modelling choices; DWK specifies a PTFE key and the 25 mL blue band.
public sealed class VolumetricDetails : MonoBehaviour {
    readonly List<Material> materials=new List<Material>();
    Mesh band;
    Material Finish(Color color){var m=new Material(Shader.Find("ChemLab/Solid"));m.SetColor("_Color",color);materials.Add(m);return m;}
    GameObject Part(string name,PrimitiveType shape,Vector3 position,Vector3 size,Material material){
        var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(transform,false);
        go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    public static void Create(Transform glass,VesselKind kind){
        if(kind!=VesselKind.Burette&&kind!=VesselKind.Pipette)return;
        var root=new GameObject("VolumetricDetails");root.transform.SetParent(glass,false);var details=root.AddComponent<VolumetricDetails>();
        var blue=details.Finish(new Color(.035f,.22f,.65f));
        if(kind==VesselKind.Pipette){
            // One blue identification band, distinct from the single capacity mark.
            details.band=VesselMesh.RevolveLoop(new List<Vector2>{new Vector2(.0305f,4.91f),new Vector2(.0305f,4.95f),new Vector2(.03f,4.95f),new Vector2(.03f,4.91f)},48);
            var ring=new GameObject("25mLBlueCodeBand");ring.transform.SetParent(root.transform,false);
            ring.AddComponent<MeshFilter>().sharedMesh=details.band;ring.AddComponent<MeshRenderer>().sharedMaterial=blue;
        }else{
            float y=VesselMesh.Get(kind).H*.09f;
            var ptfe=details.Finish(new Color(.92f,.92f,.87f));
            var key=details.Part("PTFEStopcockKey",PrimitiveType.Cylinder,new Vector3(0,y,0),new Vector3(.065f,.12f,.065f),ptfe);
            key.transform.localRotation=Quaternion.Euler(0,0,90);
            details.Part("StopcockHandle",PrimitiveType.Cube,new Vector3(.15f,y,0),new Vector3(.045f,.18f,.045f),blue);
            var nut=details.Part("StopcockRetainingNut",PrimitiveType.Cylinder,new Vector3(-.13f,y,0),new Vector3(.09f,.025f,.09f),blue);
            nut.transform.localRotation=Quaternion.Euler(0,0,90);
        }
    }
    void OnDestroy(){foreach(var material in materials)if(material!=null)Destroy(material);if(band!=null)Destroy(band);}
}
}
