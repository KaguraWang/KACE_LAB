using System;
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
// Printed scales follow the same cavity as the liquid; conical vessels are not linear rulers.
public sealed class VesselGraduations : MonoBehaviour {
    public struct Mark { public double Reading,Volume; public float Height; public bool Major; }
    Mesh _mesh;Material _ink,_fontInk;
    public static double VolumeBelow(VesselKind kind,float height){
        var g=VesselMesh.Get(kind);var p=VesselMesh.Profile(g.Type,g.R,g.H);
        float wall=Mathf.Max(.012f,g.R*.052f);double volume=0;
        for(int i=1;i<p.Count;i++){
            if(height<=p[i-1].y)break;
            float h=Mathf.Min(height,p[i].y)-p[i-1].y;if(h<=0)continue;
            float a=Mathf.Max(.0005f,p[i-1].x-wall),b=Mathf.Max(.0005f,p[i].x-wall);
            float end=Mathf.Lerp(a,b,h/(p[i].y-p[i-1].y));
            volume+=Math.PI*h*(a*a+a*end+end*end)/3*1000;
        }
        return volume;
    }
    public static List<Mark> Marks(VesselKind kind){
        var marks=new List<Mark>();var g=VesselMesh.Get(kind);double cap=Vessel.NominalCapacity(kind);
        void Add(double reading,double volume,bool major){marks.Add(new Mark{Reading=reading,Volume=volume,Height=VesselMesh.FillFractionFor(kind,volume)*g.H,Major=major});}
        if(kind==VesselKind.Pipette){Add(cap,cap,true);return marks;}
        if(kind==VesselKind.Burette){
            // The classroom transfer model can dispense the entire inventory;
            // it does not reserve a primed tip. Therefore 50 mL must read zero,
            // and every decrease in inventory must advance the same scale.
            // Adding a fictitious tip holdup here shifted every tick by 3.32 mL.
            for(int i=0;i<=500;i++)Add(i/10.0,cap-i/10.0,i%10==0);
            return marks;
        }
        if(kind!=VesselKind.Beaker&&kind!=VesselKind.Flask&&kind!=VesselKind.Cylinder&&kind!=VesselKind.Bottle)return marks;
        double step=kind==VesselKind.Cylinder?1:25;
        for(double ml=step;ml<=cap;ml+=step)Add(ml,ml,ml%(kind==VesselKind.Cylinder?10:50)==0);
        return marks;
    }
    public static void Create(Transform parent,VesselKind kind){
        var marks=Marks(kind);if(marks.Count==0)return;
        var go=new GameObject("Graduations");go.transform.SetParent(parent,false);
        var owner=go.AddComponent<VesselGraduations>();
        var g=VesselMesh.Get(kind);var profile=VesselMesh.Profile(g.Type,g.R,g.H);
        var vertices=new List<Vector3>();var triangles=new List<int>();
        foreach(var mark in marks){
            float halfWidth=kind==VesselKind.Burette?.0006f:.0012f;
            int segments=kind==VesselKind.Pipette?64:12;
            float span=kind==VesselKind.Pipette?Mathf.PI*2:(mark.Major?.48f:.25f);
            for(int i=0;i<=segments;i++){
                float angle=-Mathf.PI*.5f+span*((float)i/segments-.5f);
                foreach(float y in new[]{mark.Height-halfWidth,mark.Height+halfWidth}){
                    float r=VesselMesh.RadiusAt(profile,y/profile[profile.Count-1].y)+.0008f;
                    vertices.Add(new Vector3(Mathf.Cos(angle)*r,y,Mathf.Sin(angle)*r));
                }
                if(i>0){int n=vertices.Count-4;triangles.AddRange(new[]{n,n+1,n+2,n+1,n+3,n+2});}
            }
            bool numbered=mark.Major&&(kind!=VesselKind.Burette||mark.Reading%5==0);
            if(numbered&&LabUI.UiFont!=null){
                float angle=-Mathf.PI*.5f+.36f;
                var normal=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float r=VesselMesh.RadiusAt(profile,mark.Height/profile[profile.Count-1].y)+.001f;
                var label=new GameObject("Scale_"+mark.Reading.ToString("0"));label.transform.SetParent(go.transform,false);
                label.transform.localPosition=normal*r+Vector3.up*mark.Height;
                label.transform.localRotation=Quaternion.LookRotation(-normal,Vector3.up);
                var text=label.AddComponent<TextMesh>();text.font=LabUI.UiFont;text.fontSize=48;text.characterSize=.009f;
                text.anchor=TextAnchor.MiddleLeft;text.color=new Color(.08f,.18f,.29f);
                text.text=mark.Reading.ToString("0")+(mark.Reading==Vessel.NominalCapacity(kind)?" mL":"");
                if(owner._fontInk==null){owner._fontInk=new Material(Shader.Find("ChemLab/ScaleInk"));owner._fontInk.mainTexture=LabUI.UiFont.material.mainTexture;}
                label.GetComponent<MeshRenderer>().sharedMaterial=owner._fontInk;
            }
        }
        owner._mesh=new Mesh{name="CalibratedScale"};owner._mesh.SetVertices(vertices);owner._mesh.SetTriangles(triangles,0);owner._mesh.RecalculateNormals();owner._mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh=owner._mesh;
        owner._ink=new Material(Shader.Find("ChemLab/Solid"));owner._ink.color=new Color(.08f,.18f,.29f);
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=owner._ink;
        // Built-in camera depth uses the ShadowCaster pass. Ink must participate
        // so refraction can reject foreground scale samples, including glyphs.
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
        // Scales are painted on glass, never colliders that intercept reagent drops.
    }
    void LateUpdate(){
        if(_fontInk==null||LabUI.UiFont==null)return;
        var atlas=LabUI.UiFont.material.mainTexture;
        if(_fontInk.mainTexture!=atlas)_fontInk.mainTexture=atlas;
    }
    void OnDestroy(){if(_mesh!=null)Destroy(_mesh);if(_ink!=null)Destroy(_ink);if(_fontInk!=null)Destroy(_fontInk);}
}
}
