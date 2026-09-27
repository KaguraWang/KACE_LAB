using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChemLab.Chem;
using ChemLab.Game;
namespace ChemLab.Editor {
public static class SolidBoundaryAudit {
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){lines.Add((ok?"PASS ":"FAIL ")+label);if(ok)pass++;else fail++;}
        foreach(VesselKind kind in Enum.GetValues(typeof(VesselKind)))foreach(bool ice in new[]{false,true}){
            var source=ice?IceChunkShape.Vertices:GrainShape.Vertices;
            var vertices=new Vector3[source.Length];for(int i=0;i<source.Length;i++)vertices[i]=Quaternion.Euler(12,31,7)*(source[i]*.006f);
            var boundary=new SolidVesselBoundary(kind);var g=VesselMesh.Get(kind);var profile=VesselMesh.Profile(g.Type,g.R,g.H);
            var random=new System.Random(4301);bool matches=true;int containedCount=0;
            float floor=Mathf.Max(Mathf.Max(.012f,g.R*.052f),kind==VesselKind.Cylinder?.04f:0);
            for(int sample=0;sample<1000;sample++){
                var center=new Vector3((float)(random.NextDouble()*2.4-1.2)*g.R,(float)(random.NextDouble()*1.2-.1)*g.H,(float)(random.NextDouble()*2.4-1.2)*g.R);
                const float tolerance=2e-5f;bool direct=true;
                foreach(var vertex in vertices){
                    var point=center+vertex;
                    if(point.y<floor-tolerance){direct=false;break;}
                    float radius=Mathf.Max(.0005f,VesselMesh.RadiusAt(profile,point.y/g.H)-Mathf.Max(.012f,g.R*.052f));
                    if(point.y<=g.H&&new Vector2(point.x,point.z).magnitude>radius+tolerance){direct=false;break;}
                }
                matches&=boundary.Contains(vertices,center)==direct;if(direct)containedCount++;
            }
            Check($"{kind} ice={ice} broadphase matches 1000 original-profile vertex checks",matches&&containedCount>0&&containedCount<1000);
            foreach(float y in new[]{g.H*.2f,g.H*.5f,g.H*.85f}){
                float radius=VesselMesh.RadiusAt(profile,y/g.H)-Mathf.Max(.012f,g.R*.052f);
                var position=new Vector3(radius+.004f,y,0);var velocity=new Vector3(1,-.2f,.3f);float speed=velocity.sqrMagnitude;
                bool resolved=boundary.TryResolve(vertices,ref position,ref velocity);
                Check($"{kind} ice={ice} y={y} wall containment",resolved&&boundary.Contains(vertices,position));
                Check($"{kind} ice={ice} y={y} collision adds no kinetic energy",velocity.sqrMagnitude<=speed+1e-6f);
            }
            var bottom=new Vector3(0,-.004f,0);var falling=new Vector3(0,-1,0);
            bool supported=boundary.TryResolve(vertices,ref bottom,ref falling);
            bool narrow=kind==VesselKind.Burette||kind==VesselKind.Pipette;
            Check($"{kind} ice={ice} bottom collision or narrow-nozzle rejection",narrow?!supported&&bottom==new Vector3(0,-.004f,0):supported&&boundary.Contains(vertices,bottom));
            var descending=new Vector3(0,g.H*.5f,0);var fallVelocity=Vector3.down;
            bool stopped=true;
            for(int i=0;i<2000;i++){
                var proposed=descending+Vector3.down*.002f;
                stopped&=boundary.ConstrainStep(vertices,descending,ref proposed,ref fallVelocity)&&boundary.Contains(vertices,proposed);
                if(Vector3.Distance(descending,proposed)<1e-7f)break;
                descending=proposed;
            }
            Check($"{kind} ice={ice} descending body stops above bottom or nozzle",stopped&&descending.y>0);
            var above=new Vector3(0,g.H+.1f,0);var rising=Vector3.up;
            Check($"{kind} ice={ice} open mouth permits departure",boundary.TryResolve(vertices,ref above,ref rising)&&rising==Vector3.up);
        }
        var impossible=new[]{new Vector3(-1,0,0),new Vector3(1,0,0),new Vector3(0,.01f,0),new Vector3(0,0,.01f)};
        var pos=new Vector3(0,.4f,0);var vel=Vector3.down;var oldPos=pos;var oldVel=vel;
        bool fit=new SolidVesselBoundary(VesselKind.Beaker).TryResolve(impossible,ref pos,ref vel);
        Check("oversized body rejected without partial mutation",!fit&&pos==oldPos&&vel==oldVel);
        var bodyVertices=new Vector3[IceChunkShape.Vertices.Length];for(int i=0;i<bodyVertices.Length;i++)bodyVertices[i]=IceChunkShape.Vertices[i]*.04f;
        var shape=new SubmergedSolidShape(bodyVertices,IceChunkShape.Faces,Matrix4x4.identity);
        foreach(double density in new[]{.917,2d}){
            var boundary=new SolidVesselBoundary(VesselKind.Beaker);
            var body=new SolidTranslationState(shape,shape.TotalVolume*1000*density,new Vector3(0,.3f,0));
            Check("dynamic boundary attaches "+density,body.AttachBoundary(boundary,bodyVertices));
            bool contained=true;
            for(int i=0;i<600;i++){body.Step(1f/60,.5f,1,30);contained&=boundary.Contains(bodyVertices,body.Position);}
            Check("dynamic float/sink never penetrates vessel "+density,contained);
            Check("dynamic float/sink reaches correct supported position "+density,density<1?
                Math.Abs(body.Position.y-(.5f-shape.PlaneForFraction(density)))<.0001:
                Math.Abs(body.Position.y-(.35f*.052f-shape.Bottom))<.0001);
        }
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/solid-boundary-audit.txt",lines);
        if(fail>0)throw new Exception("Solid boundary audit failed: "+fail);
        Debug.Log("Solid boundary audit passed: "+pass);
    }
}
}
