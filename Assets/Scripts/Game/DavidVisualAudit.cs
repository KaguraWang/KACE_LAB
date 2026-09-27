using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {
// Pixel-level audit: copying the eight proxy vertices alone would pass the old
// console test. Compare actual raymarched surfaces, then destroy the live solver.
public sealed class DavidVisualAudit : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch() {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-davidVisualTest") < 0) return;
        Application.runInBackground = true;
        new GameObject("David visual audit").AddComponent<DavidVisualAudit>();
    }
    IEnumerator Start() {
        yield return null;
        int exit = 0;
        try { Run(); } catch (Exception e) { Debug.LogException(e); exit = 1; }
        yield return null;
        Application.Quit(exit);
    }
    static void Run() {
        int pass=0,fail=0; var lines=new List<string>();
        void Check(string name,bool ok){lines.Add((ok?"PASS ":"FAIL ")+name);if(ok)pass++;else fail++;}
        var cameraObject=new GameObject("David pixel camera");var camera=cameraObject.AddComponent<Camera>();
        camera.enabled=false;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=Color.clear;camera.orthographic=true;camera.aspect=1;
        var target=new RenderTexture(192,192,24,RenderTextureFormat.ARGBFloat);target.Create();camera.targetTexture=target;
        var pixels=new Texture2D(192,192,TextureFormat.RGBAFloat,false,true);var previous=RenderTexture.active;
        Color[] Capture(){camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,192,192),0,0);pixels.Apply();return pixels.GetPixels();}
        float Difference(Color[] a,Color[] b){float d=0;for(int i=0;i<a.Length;i++)d=Mathf.Max(d,Mathf.Abs(a[i].r-b[i].r),Mathf.Abs(a[i].g-b[i].g),Mathf.Abs(a[i].b-b[i].b),Mathf.Abs(a[i].a-b[i].a));return d;}
        int Hits(Color[] image){int n=0;foreach(var p in image)if(p.a>.99f)n++;return n;}
        try {
            foreach(var kind in new[]{VesselKind.Beaker,VesselKind.Flask,VesselKind.Tube}) {
                var go=new GameObject("Liquid proxy audit");go.layer=29;go.transform.position=new Vector3(100,0,100);
                var filter=go.AddComponent<MeshFilter>();var renderer=go.AddComponent<MeshRenderer>();
                var material=new Material(Shader.Find("ChemLab/FluidVolume"));renderer.sharedMaterial=material;
                var solver=new GpuFluid();FluidVisualSnapshot snapshot=null;Mesh proxyCopy=null;
                try {
                    double ml=kind==VesselKind.Tube?8:60;
                    solver.Configure(kind,ml,filter,material);
                    if(kind==VesselKind.Beaker){
                        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.SetActive(false);
                        var mesh=cube.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
                        for(int i=0;i<vertices.Length;i++)vertices[i]*=.18f;
                        solver.ConfigureSolids(new[]{new SolidBodySeed{LocalVertices=vertices,Triangles=mesh.triangles,Center=new Vector3(0,.1f,0)}});
                        UnityEngine.Object.Destroy(cube);
                    }
                    for(int i=0;i<8;i++)solver.Step(Vector3.zero,Quaternion.identity,1f/60,false,0);
                    snapshot=solver.CaptureVisualSnapshot();
                    Check(kind+" snapshot owns a separate GPU density texture",snapshot.Material.GetTexture("_FluidDensity")!=material.GetTexture("_FluidDensity"));
                    material.SetFloat("_AuditSurface",1);snapshot.Material.SetFloat("_AuditSurface",1);
                    var bounds=filter.sharedMesh.bounds;camera.orthographicSize=Mathf.Max(bounds.size.y,bounds.size.x)*.62f;
                    camera.nearClipPlane=.01f;camera.farClipPlane=30;
                    Color[] frozen=null;int liveHits=0;
                    foreach(var direction in new[]{new Vector3(0,.15f,-1),new Vector3(1,.4f,-.4f),new Vector3(.1f,1,.1f)}){
                        camera.transform.position=go.transform.position+bounds.center+direction.normalized*10;
                        camera.transform.LookAt(go.transform.position+bounds.center);
                        renderer.sharedMaterial=material;var live=Capture();
                        renderer.sharedMaterial=snapshot.Material;frozen=Capture();liveHits=Hits(live);
                        Check(kind+" actual liquid silhouette matches source "+direction,liveHits>50&&Difference(live,frozen)<1e-5f);
                    }
                    // The former generic Ghost shader exposes the full proxy. This
                    // negative control must differ substantially from the liquid.
                    var boxMaterial=new Material(Resources.Load<Shader>("ChemLab/Ghost"));
                    boxMaterial.SetColor("_Color",Color.white);boxMaterial.SetColor("_OutlineColor",Color.white);
                    renderer.sharedMaterial=boxMaterial;int boxHits=Hits(Capture());
                    Check(kind+" proxy-box negative control differs from the real liquid",boxHits>liveHits*1.1f);
                    UnityEngine.Object.Destroy(boxMaterial);
                    renderer.sharedMaterial=snapshot.Material;
                    if(kind==VesselKind.Beaker){
                        solver.UpdateSolidPositions(new[]{new Vector3(.2f,.1f,0)});
                        Check("frozen liquid keeps original solid clipping after the source solid moves",Hits(frozen)>50&&Difference(frozen,Capture())<1e-5f);
                    }
                    // Copies must survive an amount change and disposal of every
                    // original texture/buffer. A shared Material is insufficient.
                    proxyCopy=UnityEngine.Object.Instantiate(filter.sharedMesh);
                    solver.Configure(kind,ml*.3,filter,material);solver.Dispose();
                    UnityEngine.Object.Destroy(filter.sharedMesh);filter.sharedMesh=proxyCopy;
                    Check(kind+" snapshot survives source reallocation and disposal",Hits(frozen)>50&&Difference(frozen,Capture())<1e-5f);
                    snapshot.Material.SetFloat("_AuditSurface",0);snapshot.Material.SetColor("_Color",new Color(.02f,.55f,.95f,.52f));
                    var tinted=Capture();int colored=0;foreach(var p in tinted)if(p.a>.05f&&p.b>p.r*1.1f)colored++;
                    Check(kind+" frozen surface renders the blue translucent tint",colored>50);
                    snapshot.Material.SetColor("_Color",new Color(.02f,.55f,.95f,0));snapshot.Material.SetFloat("_GhostFade",0);
                    float maxAlpha=0;foreach(var p in Capture())maxAlpha=Mathf.Max(maxAlpha,p.a);
                    Check(kind+" liquid rim fades with the fill",maxAlpha<1e-6f);
                    lines.Add("METRIC "+kind+" liquidPixels="+liveHits+" proxyPixels="+boxHits);
                } finally {
                    snapshot?.Dispose();solver.Dispose();
                    if(filter.sharedMesh!=null)UnityEngine.Object.Destroy(filter.sharedMesh);
                    UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(go);
                }
            }
            Check("all GPU snapshot resources released",FluidVisualSnapshot.LiveCount==0);
        } finally {
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();
            UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(cameraObject);
            lines.Add("TOTAL "+pass+" passed, "+fail+" failed");Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/david-visual-audit.txt",lines);
        }
        if(fail>0)throw new Exception("David visual audit failed: "+fail);
    }
}
}
