using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {
// Disposable hidden-Player inspection. Images are evidence for visual review,
// not an automatic assertion that apparatus look realistic.
public sealed class ApparatusVisualAudit : MonoBehaviour {
    readonly List<string> report=new List<string>();
    int passed,failed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch(){
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-apparatusVisualTest")<0)return;
        Application.runInBackground=true;
        new GameObject("Apparatus visual audit").AddComponent<ApparatusVisualAudit>();
    }
    void Check(string label,bool ok){report.Add((ok?"PASS ":"FAIL ")+label);if(ok)passed++;else failed++;}
    IEnumerator Start(){
        yield return new WaitForSeconds(2);
        Directory.CreateDirectory("Logs/apparatus-visual");WorldClock.Set(0);
        foreach(VesselKind kind in Enum.GetValues(typeof(VesselKind))){
            var model=Lab.AddVessel(kind);LabBootstrap.Instance.SyncViews();yield return null;yield return null;
            var view=FindObjectsOfType<VesselView>().FirstOrDefault(v=>v.Model==model);
            Check(kind+" visible model exists",view!=null);
            report.Add("CAPACITY "+kind+" nominalMl="+model.Capacity+" innerMl="+(VesselMesh.InnerCapacity(kind)*1000));
            // Independently sampled solver cavity, rather than round-tripping
            // the scale's own analytic integration. No added runtime fluid work.
            var cavity=new SolidLiquidLevel(kind);
            double worst=0,worstReading=0;
            foreach(var mark in VesselGraduations.Marks(kind)){
                double expected=kind==VesselKind.Burette?model.Capacity-mark.Reading:mark.Reading;
                double error=Math.Abs(cavity.CavityVolumeMl(mark.Height)-expected);
                if(error>worst){worst=error;worstReading=mark.Reading;}
            }
            double tolerance=Math.Max(.1,model.Capacity*.01);
            report.Add("CALIBRATION "+kind+" maxErrorMl="+worst+" atReading="+worstReading+" toleranceMl="+tolerance);
            Check(kind+" printed scale agrees with simulated volume within 1 percent nominal or 0.1 mL",worst<=tolerance);
            if(view!=null){
                bool glass=kind!=VesselKind.Crucible && kind!=VesselKind.Dish;
                var marks=view.GetComponentsInChildren<GlasswareBranding>();
                Check(kind+" glass brand present where applicable",!glass||marks.Length>0);
                Check(kind+" brands have renderable material and no collider",marks.All(m=>m.GetComponent<MeshRenderer>()?.sharedMaterial?.shader?.isSupported==true && m.GetComponentsInChildren<Collider>().Length==0));
                var scale=view.GetComponentInChildren<VesselGraduations>();
                Check(kind+" expected graduations present",VesselGraduations.Marks(kind).Count==0 || scale!=null);
                Capture(view.transform,"vessel-"+kind);
            }
            Lab.RemoveVessel(model);yield return null;
        }
        foreach(var view in FindObjectsOfType<ApparatusView>())Capture(view.transform,"apparatus-"+view.Model.Kind);
        foreach(var view in FindObjectsOfType<TestItemView>())Capture(view.transform,"test-"+view.Model.Kind);
        foreach(bool reflux in new[]{false,true}){
            DistillationView.CreateKit(reflux);yield return null;yield return null;
            var source=Lab.Vessels.LastOrDefault(v=>v.Distillation!=null && v.Distillation.Reflux==reflux);
            var view=FindObjectsOfType<VesselView>().FirstOrDefault(v=>v.Model==source);
            var receiver=source==null?null:FindObjectsOfType<VesselView>().FirstOrDefault(v=>v.Model==source.Distillation.Receiver);
            Check("assembly exists "+reflux,view!=null);
            if(view!=null)Capture(view.transform,reflux?"assembly-reflux":"assembly-distillation",receiver!=null && receiver!=view?receiver.transform:null);
        }
        report.Add("TOTAL "+passed+" passed, "+failed+" failed; images require visual review");
        File.WriteAllLines("Logs/apparatus-visual/report.txt",report);
        Application.Quit(failed==0?0:1);
    }
    void Capture(Transform root,string name,Transform companion=null){
        var members=new List<Transform>(root.GetComponentsInChildren<Transform>(true));
        if(companion!=null)members.AddRange(companion.GetComponentsInChildren<Transform>(true));
        var renderers=members.Select(t=>t.GetComponent<Renderer>()).Where(r=>r!=null && r.enabled && r.gameObject.activeInHierarchy).ToArray();
        Check(name+" has renderers",renderers.Length>0);if(renderers.Length==0)return;
        Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        var layers=members.Select(t=>t.gameObject.layer).ToArray();
        var canvases=FindObjectsOfType<Canvas>();var active=canvases.Select(c=>c.enabled).ToArray();
        var cameraHost=new GameObject("Isolated apparatus camera");var camera=cameraHost.AddComponent<Camera>();
        var target=new RenderTexture(1024,1024,24);var texture=new Texture2D(1024,1024,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try{
            foreach(var canvas in canvases)canvas.enabled=false;
            foreach(var member in members)member.gameObject.layer=31;
            camera.enabled=false;camera.targetTexture=target;camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.25f,.29f);
            camera.depthTextureMode=DepthTextureMode.Depth;camera.orthographic=true;
            float radius=Mathf.Max(.1f,bounds.extents.magnitude);
            camera.orthographicSize=radius*1.12f;camera.nearClipPlane=.01f;camera.farClipPlane=radius*10+10;
            for(int angle=0;angle<3;angle++){
                camera.transform.position=bounds.center+Quaternion.Euler(0,angle*120,0)*new Vector3(0,radius*.65f,-radius*3);
                camera.transform.LookAt(bounds.center);camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1024,1024),0,0);texture.Apply();
                string path="Logs/apparatus-visual/"+name+"-"+angle+".png";
                File.WriteAllBytes(path,texture.EncodeToPNG());report.Add("IMAGE "+path);
            }
        }finally{
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Destroy(target);Destroy(texture);Destroy(cameraHost);
            for(int i=0;i<members.Count;i++)if(members[i]!=null)members[i].gameObject.layer=layers[i];
            for(int i=0;i<canvases.Length;i++)if(canvases[i]!=null)canvases[i].enabled=active[i];
        }
    }
}
}
