using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using ChemLab.Chem;

namespace ChemLab.Game {
// 显式 -interactionTest 才启用；运行真实 Player 的帧循环、视图和声音。
public class InteractionRegression : MonoBehaviour {
    readonly List<string> lines = new List<string>();
    int failures,checks;
    IEnumerator AuditReagentArt(LabUI ui) {
        var catalog=Resources.Load<ReagentArtCatalog>("ReagentArtCatalog");
        Check("reagent image catalog covers every shelf entry",catalog!=null&&catalog.entries.Count==ui.TestReagentTotal);
        if(catalog==null)yield break;
        var ids=new HashSet<string>();int pictured=0,missing=0;
        foreach(var entry in catalog.entries){ids.Add(entry.id);if(entry.image!=null)pictured++;else missing++;}
        Check("reagent image import retains unique ids and explicit missing entries",ids.Count==catalog.entries.Count&&pictured==227&&missing==80);
        string previousCat=ui.TestCat,previousQuery=ui.TestQuery;
        foreach(string cat in new[]{"metal","nonmetal","oxide","acid","base","salt","organic","indicator","other"}) {
            ui.TestSetFilter(cat,"");yield return null;Canvas.ForceUpdateCanvases();
            var grid=System.Array.Find(ui.GetComponentsInChildren<UnityEngine.UI.GridLayoutGroup>(),x=>x.name=="Content");
            bool layout=grid!=null&&grid.constraint==UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount&&grid.constraintCount==3;
            bool images=true,labels=true,drag=true;int count=0;
            if(grid!=null)foreach(Transform child in grid.transform){
                if(!child.gameObject.activeSelf)continue;
                count++;var rect=child as RectTransform;
                layout&=Mathf.Abs(rect.rect.width-94)<.1f&&Mathf.Abs(rect.rect.height-164)<.1f;
                var photo=child.Find("ReagentImage") as RectTransform;
                var image=photo==null?null:photo.GetComponent<UnityEngine.UI.Image>();
                string id=child.name.Substring(2);var expected=catalog.Find(id);
                images&=photo!=null&&Mathf.Abs(photo.rect.width-82)<.1f&&Mathf.Abs(photo.rect.height-82)<.1f&&image!=null&&image.sprite==expected&&!image.raycastTarget;
                if(expected==null){var label=photo==null?null:photo.GetComponentInChildren<UnityEngine.UI.Text>();images&=label!=null&&label.text=="暂无图片";}
                foreach(var label in child.GetComponentsInChildren<UnityEngine.UI.Text>())labels&=label.preferredHeight<=label.rectTransform.rect.height+1;
                var handler=child.GetComponent<ReagentDrag>();drag&=handler!=null&&handler.Owner==ui&&handler.SubstanceId==id;
            }
            Check("reagent "+cat+" three-column complete grid",layout&&count==ui.TestFilterCount(cat,""));
            Check("reagent "+cat+" uniform images or labeled placeholders",images);
            Check("reagent "+cat+" text fits and drag handlers remain",labels&&drag);
        }
        ui.TestSetFilter(previousCat,previousQuery);yield return null;
    }
    void Check(string name, bool ok) {
        checks++;
        lines.Add((ok ? "PASS " : "FAIL ") + name);
        if (!ok) failures++;
        Debug.Log("[InteractionTest] " + lines[lines.Count - 1]);
    }
    IEnumerator AuditLivePour(Vessel a,Vessel b,float duration,string label){
        int samples=0;bool finite=true,volumeOk=true,paused=true;double maxRelativeError=0;float clock=Lab.Clock;
        for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime){
            yield return new WaitForEndOfFrame();
            paused&=PourTransaction.IsActive&&Lab.Clock==clock;
            var stream=GameObject.Find("PourLiquidStream");if(stream==null)continue;
            var mesh=stream.GetComponent<MeshFilter>().sharedMesh;if(mesh.vertexCount==0)continue;
            samples++;var vertices=mesh.vertices;var triangles=mesh.triangles;
            foreach(var vertex in vertices)finite&=!float.IsNaN(vertex.sqrMagnitude)&&!float.IsInfinity(vertex.sqrMagnitude);
            double volume=0;for(int i=0;i<triangles.Length;i+=3)volume+=Vector3.Dot(vertices[triangles[i]],Vector3.Cross(vertices[triangles[i+1]],vertices[triangles[i+2]]))/6.0;
            double expected=(50-a.Water-b.Water)/1000;
            if(expected>1e-10){double error=System.Math.Abs(System.Math.Abs(volume)-expected)/expected;maxRelativeError=System.Math.Max(maxRelativeError,error);volumeOk&=error<.02;}
        }
        Check(label+" samples finite continuous stream throughout flight",samples>=10&&finite);
        Check(label+" conserves airborne volume and pauses reactions throughout flight",volumeOk&&paused);
        lines.Add($"METRIC {label} streamFrames={samples} maxRelativeVolumeError={maxRelativeError:G6}");
    }
    IEnumerator AuditCondenserPause(DistillationAssembly rig,DistillationView view,string label){
        var spare=Lab.AddVessel(VesselKind.Beaker);spare.Water=10;
        float phase=view.CondensatePhase,clock=Lab.Clock;
        bool visible=view.CondensateVisible,frozen=true;
        double heat=rig.HeaterEnergyJ,cooling=rig.CoolantEnergyJ,sourceWater=rig.Source.Water,receiverWater=rig.Receiver.Water;
        double storage=rig.CondenserStoredEnergyJ,ambient=rig.CondenserAmbientEnergyJ,wall=rig.CondenserWallC,jacket=rig.CondenserWaterC;
        double vesselStored=rig.VesselStoredEnergyJ,vesselAmbient=rig.VesselAmbientEnergyJ,sourceWall=rig.Source.GlassTemperatureC,receiverWall=rig.Receiver.GlassTemperatureC;
        using(var transaction=new PourTransaction(rig.Source,spare)){
            for(int i=0;i<12;i++){
                yield return new WaitForEndOfFrame();
                frozen&=view.CondensatePhase==phase&&view.CondensateVisible==visible&&
                    rig.HeaterEnergyJ==heat&&rig.CoolantEnergyJ==cooling&&rig.Source.Water==sourceWater&&rig.Receiver.Water==receiverWater&&Lab.Clock==clock&&
                    rig.CondenserStoredEnergyJ==storage&&rig.CondenserAmbientEnergyJ==ambient&&rig.CondenserWallC==wall&&rig.CondenserWaterC==jacket&&
                    rig.VesselStoredEnergyJ==vesselStored&&rig.VesselAmbientEnergyJ==vesselAmbient&&rig.Source.GlassTemperatureC==sourceWall&&rig.Receiver.GlassTemperatureC==receiverWall;
            }
        }
        Check(label+" real frames freeze condensate animation inventories and thermal ledger",visible&&frozen);
        for(int i=0;i<12;i++)yield return new WaitForEndOfFrame();
        Check(label+" real frames resume heat transfer and animation",rig.Running&&rig.HeaterEnergyJ>heat&&rig.CoolantEnergyJ>cooling&&view.CondensatePhase!=phase&&Lab.Clock>clock);
        Lab.RemoveVessel(spare);
    }
    void CaptureVesselModel(VesselView view,string filename) {
        CaptureModel(view.transform,VesselMesh.Get(view.Model.Kind).H,filename);
    }
    void AuditCoolantGeometry(DistillationView view,string label){
        var tube=view.transform.Find("Connected glassware/Cooling water");
        var glass=view.transform.Find("Connected glassware/Liebig inner vapor tube");
        var rig=view.GetComponentInParent<VesselView>().Model.Distillation;
        Check(label+" default storage uses actual rendered tube length",rig.UseTransientCondenser&&System.Math.Abs(rig.CondenserLengthMeters-glass.localScale.y*.1)<1e-7);
        var mesh=tube.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;
        double volume=0;float minimum=float.PositiveInfinity,maximum=0;
        foreach(var vertex in vertices){float radius=new Vector2(vertex.x*tube.localScale.x,vertex.z*tube.localScale.z).magnitude;minimum=Mathf.Min(minimum,radius);maximum=Mathf.Max(maximum,radius);}
        for(int n=0;n<triangles.Length;n+=3)volume+=Vector3.Dot(vertices[triangles[n]],Vector3.Cross(vertices[triangles[n+1]],vertices[triangles[n+2]]))/6d;
        volume=System.Math.Abs(volume*tube.localScale.x*tube.localScale.y*tube.localScale.z)*1000;
        CondenserDimensions.TryMeasure(glass.localScale.y*CondenserDimensions.SceneUnitMeters,out var dimensions);
        Check(label+" coolant annulus fills between glass tubes",Mathf.Abs(minimum-.055f)<1e-6f&&Mathf.Abs(maximum-.1148f)<1e-6f);
        // 48-sided circular approximation has a known 0.285% area deficit.
        Check(label+" coolant rendered and calculated volumes match",System.Math.Abs(volume-dimensions.CoolantMl)/dimensions.CoolantMl<.003);
    }
    void AuditIceOptics(VesselView view){
        var renderer=view.transform.Find("Sediment").GetComponent<MeshRenderer>();
        var material=renderer.sharedMaterials[1];
        var board=GameObject.CreatePrimitive(PrimitiveType.Quad);
        board.name="IceOpticsChecker";board.transform.SetParent(view.transform,false);
        board.transform.localPosition=new Vector3(0,.3f,.7f);board.transform.localScale=Vector3.one*2;
        var pattern=new Texture2D(64,64,TextureFormat.RGB24,false){filterMode=FilterMode.Point};
        var pixels=new Color[4096];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)pixels[y*64+x]=((x/2+y/2)%2)==0?new Color(.88f,.91f,.93f):new Color(.08f,.16f,.24f);
        pattern.SetPixels(pixels);pattern.Apply();
        var boardMaterial=new Material(Resources.Load<Shader>("OpticsAuditBackground")){mainTexture=pattern};
        board.GetComponent<MeshRenderer>().sharedMaterial=boardMaterial;
        var transforms=view.GetComponentsInChildren<Transform>(true);var layers=new int[transforms.Length];
        for(int n=0;n<transforms.Length;n++){layers[n]=transforms[n].gameObject.layer;transforms[n].gameObject.layer=31;}
        var host=new GameObject("IceOpticsAuditCamera");var camera=host.AddComponent<Camera>();
        var target=new RenderTexture(512,512,24);var readback=new Texture2D(512,512,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;float distortion=material.GetFloat("_Distortion");
        try{
            camera.enabled=false;camera.targetTexture=target;camera.cullingMask=1<<31;camera.depthTextureMode=DepthTextureMode.Depth;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;
            camera.orthographic=true;camera.orthographicSize=.37f;camera.nearClipPlane=.01f;camera.farClipPlane=10;
            camera.transform.position=view.transform.position+new Vector3(0,.35f,-2);
            camera.transform.LookAt(view.transform.position+Vector3.up*.28f);
            Color[] Render(string name){camera.Render();RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,512,512),0,0);readback.Apply();
                File.WriteAllBytes(Path.GetFullPath("Logs/"+name+".png"),readback.EncodeToPNG());return readback.GetPixels();}
            var refracted=Render("ice-optics-refracted");
            material.SetFloat("_Distortion",0);var straight=Render("ice-optics-no-distortion");
            renderer.enabled=false;var background=Render("ice-optics-background");
            renderer.enabled=true;var originalMaterials=renderer.sharedMaterials;
            renderer.sharedMaterials=new[]{originalMaterials[0],originalMaterials[0]};
            Color[] opaque;
            try{opaque=Render("ice-optics-opaque-control");}finally{renderer.sharedMaterials=originalMaterials;}
            double delta=0,transmissionDelta=0,opaqueDelta=0;int changed=0;
            for(int n=0;n<refracted.Length;n++){
                double d=Mathf.Abs(refracted[n].r-straight[n].r)+Mathf.Abs(refracted[n].g-straight[n].g)+Mathf.Abs(refracted[n].b-straight[n].b);
                delta+=d;if(d>.02)changed++;
                transmissionDelta+=Mathf.Abs(straight[n].r-background[n].r)+Mathf.Abs(straight[n].g-background[n].g)+Mathf.Abs(straight[n].b-background[n].b);
                opaqueDelta+=Mathf.Abs(opaque[n].r-background[n].r)+Mathf.Abs(opaque[n].g-background[n].g)+Mathf.Abs(opaque[n].b-background[n].b);
            }
            lines.Add($"METRIC ice optical changedPixels={changed} refractionDelta={delta/refracted.Length:G6} transmissionDelta={transmissionDelta/refracted.Length:G6}");
            Check("ice irregular optics visibly refracts checker background",changed>100&&delta/refracted.Length>.001);
            Check("ice transmits background rather than opaque block",opaqueDelta>1&&transmissionDelta<opaqueDelta*.6);
        }finally{
            material.SetFloat("_Distortion",distortion);renderer.enabled=true;
            for(int n=0;n<transforms.Length;n++)transforms[n].gameObject.layer=layers[n];
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();
            board.SetActive(false);Destroy(board);Destroy(boardMaterial);Destroy(pattern);Destroy(target);Destroy(readback);Destroy(host);
        }
    }
    bool AssemblyFitsWorkspace(Vessel source,LabUI ui){
        var area=ui.TagWorkspaceRect();var camera=LabBootstrap.Instance.Cam;
        foreach(var view in FindObjectsOfType<VesselView>()){
            if(view.Model!=source&&view.Model!=source.Distillation.Receiver)continue;
            foreach(var collider in view.GetComponentsInChildren<Collider>()){
                if(!collider.enabled)continue;
                var b=collider.bounds;
                for(int mask=0;mask<8;mask++){
                    var point=b.center+Vector3.Scale(b.extents,new Vector3((mask&1)==0?-1:1,(mask&2)==0?-1:1,(mask&4)==0?-1:1));
                    var pixel=camera.WorldToScreenPoint(point);
                    if(pixel.z<=camera.nearClipPlane||!area.Contains(new Vector2(pixel.x,pixel.y)))return false;
                }
            }
        }
        return true;
    }
    void CaptureModel(Transform root,float h,string filename,Transform companion=null) {
        var canvases=FindObjectsOfType<Canvas>();var canvasEnabled=new bool[canvases.Length];
        for(int i=0;i<canvases.Length;i++){canvasEnabled[i]=canvases[i].enabled;canvases[i].enabled=false;}
        var group=new List<Transform>(root.GetComponentsInChildren<Transform>(true));
        if(companion!=null)group.AddRange(companion.GetComponentsInChildren<Transform>(true));
        var transforms=group.ToArray();var layers=new int[transforms.Length];
        for(int i=0;i<transforms.Length;i++){layers[i]=transforms[i].gameObject.layer;transforms[i].gameObject.layer=31;}
        var host=new GameObject("ModelAuditCamera");var camera=host.AddComponent<Camera>();
        var target=new RenderTexture(640,960,24);var texture=new Texture2D(640,960,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try {
            camera.enabled=false;camera.targetTexture=target;camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.20f,.23f);
            camera.depthTextureMode=DepthTextureMode.Depth;camera.orthographic=true;
            camera.orthographicSize=h*.64f;camera.nearClipPlane=.01f;camera.farClipPlane=50;
            var center=root.position+Vector3.up*h*.5f;
            if(companion!=null)center+=(companion.position-root.position)*.5f;
            for(int angle=0;angle<3;angle++){
                camera.transform.position=center+Quaternion.Euler(0,angle*120,0)*new Vector3(0,h*.35f,-h*2);
                camera.transform.LookAt(center);camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,640,960),0,0);texture.Apply();
                File.WriteAllBytes(Path.GetFullPath("Logs/"+filename+"-"+angle+".png"),texture.EncodeToPNG());
            }
        } finally {
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Destroy(target);Destroy(texture);Destroy(host);
            for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];
            for(int i=0;i<canvases.Length;i++)canvases[i].enabled=canvasEnabled[i];
        }
    }
    IEnumerator Start() {
        yield return new WaitForSeconds(2);
        // These fixtures exercise immediate-placement chemistry workflows.
        // Default animated placement and occupied-pad rejection are covered by KPrinterAudit.
        KPrinterMode.TrySet("0", out _);
        Check("legacy fixtures explicitly use immediate placement", !KPrinterMode.Enabled);
        // Finish the frame-based rack audit before isolated chemistry fixtures
        // publish reaction events; those fixtures are not part of the demo bench.
        var reagentAuditUi=FindObjectOfType<LabUI>();
        if(reagentAuditUi!=null)yield return AuditReagentArt(reagentAuditUi);
        var brandMarks=FindObjectsOfType<GlasswareBranding>();
        Check("glassware print survives Player shader stripping",brandMarks.Length>0&&
            System.Array.TrueForAll(brandMarks,m=>m.GetComponent<MeshRenderer>().sharedMaterial!=null&&
                m.GetComponent<MeshRenderer>().sharedMaterial.shader.isSupported&&
                m.GetComponent<MeshRenderer>().sharedMaterial.mainTexture!=null));
        var lab = LabBootstrap.Instance;
        lab.SuppressDesktopInputForAudit=true;
        lines.Add("METRIC input mode=API/EventSystem; desktop laboratory input suppressed; not OS input injection");
        Check("presentation capped at 60 FPS without changing fixed fluid step",Application.targetFrameRate==60&&QualitySettings.vSyncCount==0);
        Check("starter vessels respect their nominal capacities",Lab.Vessels.Count==4&&
              Lab.Vessels.TrueForAll(v=>v.Volume()+v.SolidVolume()<=v.Capacity+1e-6));
        Check("starter zinc acid beaker does not heat and boil dry unattended",Lab.Vessels.Count==4&&!Lab.Vessels[3].Heating);
        bool pureAcidsLiquid=true,acidGasesActuallyGas=true;
        foreach(var pair in new[]{new System.Collections.Generic.KeyValuePair<string,double>("h2so4",1.8),
                                  new System.Collections.Generic.KeyValuePair<string,double>("hno3",1.5129)}){
            var v=new Vessel(VesselKind.Beaker,Vector3.zero);
            pureAcidsLiquid&=DoseService.TryAdd(v,pair.Key,.001,true,out _)&&v.Water==0&&
                v.Solutes.ContainsKey(pair.Key)&&
                System.Math.Abs(v.LiquidVolume()-.001*ChemData.Sub[pair.Key].Mass/pair.Value)<1e-9;
        }
        foreach(string id in new[]{"h2s","hf"}){
            var v=new Vessel(VesselKind.Beaker,Vector3.zero);
            acidGasesActuallyGas&=DoseService.TryAdd(v,id,.001,true,out _)&&v.Gases.ContainsKey(id)&&
                v.Undissolved.Count==0&&v.Solutes.Count==0;
        }
        Check("pure shelf acids use liquid volumes in live player",pureAcidsLiquid);
        Check("hydrogen sulfide and hydrogen fluoride stay gaseous in live player",acidGasesActuallyGas);
        var runtimeEthanolHeat=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=25};runtimeEthanolHeat.Solutes["c2h5oh"]=.1;
        Lab.ApplyReactionHeat(runtimeEthanolHeat,.001,-10);
        Check("player reaction heat uses integrated ethanol enthalpy instead of phantom water",System.Math.Abs(runtimeEthanolHeat.Mass()*PureLiquidThermalModel.Ethanol.H(runtimeEthanolHeat.Temp)-10)<.001);
        var runtimeUnknownHeat=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=25};runtimeUnknownHeat.Solutes["n_hexane"]=.1;
        Lab.ApplyReactionHeat(runtimeUnknownHeat,.001,-10);
        Check("player unknown-solvent reaction heat preserves temperature and warns",runtimeUnknownHeat.Temp==25&&runtimeUnknownHeat.PhaseNotice.Contains("缺少"));
        var runtimeQualitativeHeat=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        runtimeQualitativeHeat.Undissolved["zn"]=.001;runtimeQualitativeHeat.Solutes["H+"]=.002;runtimeQualitativeHeat.Solutes["Cl-"]=.002;
        Lab.Settle(runtimeQualitativeHeat);
        Check("player unmeasured reaction heat does not add fixed Celsius",runtimeQualitativeHeat.Get("Zn2+")>.0009&&runtimeQualitativeHeat.Temp==25&&runtimeQualitativeHeat.PhaseNotice.Contains("定量焓变"));
        var runtimeQuicklime=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        int runtimeLimeLog=Lab.Log.Count;Lab.AddReagent(runtimeQuicklime,"cao",.001);
        Check("player tiny quicklime legacy route never fabricates boiling or bubbles",runtimeQuicklime.Get("Ca2+")>0&&runtimeQuicklime.Temp>25&&runtimeQuicklime.Temp<25.01&&runtimeQuicklime.Fx.Bubble==0&&runtimeQuicklime.Fx.Smoke==0&&
              !Lab.Log.GetRange(runtimeLimeLog,Lab.Log.Count-runtimeLimeLog).Exists(x=>x.Text.Contains("沸腾")));
        var runtimeShelfLime=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        runtimeLimeLog=Lab.Log.Count;bool runtimeLimeAdded=DoseService.TryAdd(runtimeShelfLime,"cao",.001,false,out _);
        Check("player tiny quicklime shelf route never fabricates boiling or bubbles",runtimeLimeAdded&&runtimeShelfLime.Get("Ca2+")>0&&runtimeShelfLime.Temp>25&&runtimeShelfLime.Temp<25.01&&runtimeShelfLime.Fx.Bubble==0&&runtimeShelfLime.Fx.Smoke==0&&
              !Lab.Log.GetRange(runtimeLimeLog,Lab.Log.Count-runtimeLimeLog).Exists(x=>x.Text.Contains("沸腾")));
        var runtimeSlakedLime=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=25};
        double runtimeLimeInput=2/ChemData.Sub["cao"].Mass;Lab.AddReagent(runtimeSlakedLime,"cao",2);
        bool runtimeLimeCapacity=CalciumHydroxideEquilibrium.TryCapacity(runtimeSlakedLime,out double runtimeSaturated);
        Check("player quicklime produces solid hydroxide and saturated limewater",
              runtimeLimeCapacity&&System.Math.Abs(runtimeSlakedLime.Get("Ca2+")-runtimeSaturated)<3e-7&&
              System.Math.Abs(runtimeSlakedLime.Get("caoh2")-(runtimeLimeInput-runtimeSaturated))<3e-7&&
              runtimeSlakedLime.Temp>25&&runtimeSlakedLime.Temp<40&&runtimeSlakedLime.Fx.Bubble==0);
        runtimeSlakedLime.Temp=60;Lab.Settle(runtimeSlakedLime);
        Check("player heating saturated limewater precipitates hydroxide without mass loss",
              System.Math.Abs(runtimeSlakedLime.Get("Ca2+")-.01512*runtimeSlakedLime.Water/1000)<3e-7&&
              System.Math.Abs(runtimeSlakedLime.Get("Ca2+")+runtimeSlakedLime.Get("caoh2")-runtimeLimeInput)<3e-7);
        var runtimeCarbonation=new Vessel(VesselKind.Bottle,Vector3.zero){Water=100,Temp=25,Sealed=true,PressureBar=1.01325};
        runtimeCarbonation.TrappedAirMoles=runtimeCarbonation.PressureBar*runtimeCarbonation.HeadspaceLitres/runtimeCarbonation.GasRT;
        runtimeCarbonation.Undissolved["caoh2"]=.001;runtimeCarbonation.Gases["co2"]=.001;
        double runtimeCarbonationMass=runtimeCarbonation.Mass();Lab.Settle(runtimeCarbonation);Lab.Settle(runtimeCarbonation);
        Check("player wet hydroxide slurry stays cloudy after repeat settling",
              System.Math.Abs(runtimeCarbonation.Get("caco3")-.001)<1e-8&&runtimeCarbonation.Turbidity>0&&
              System.Math.Abs(runtimeCarbonation.Mass()-runtimeCarbonationMass)<1e-7);
        runtimeCarbonation.Solutes["co2"]=.001;Lab.Settle(runtimeCarbonation);
        Check("player later dissolved carbon dioxide redissolves carbonate without mass loss",
              runtimeCarbonation.Get("caco3")<1e-8&&System.Math.Abs(runtimeCarbonation.Get("HCO3-")-.002)<1e-8&&
              runtimeCarbonation.Turbidity<.01f&&
              System.Math.Abs(runtimeCarbonation.Mass()-runtimeCarbonationMass-ChemData.Sub["co2"].Mass*.001)<1e-7);
        var runtimeLaterGas=new Vessel(VesselKind.Bottle,Vector3.zero){Water=100,Temp=25,Sealed=true,PressureBar=1.01325};
        runtimeLaterGas.TrappedAirMoles=runtimeLaterGas.PressureBar*runtimeLaterGas.HeadspaceLitres/runtimeLaterGas.GasRT;
        runtimeLaterGas.Undissolved["caoh2"]=.001;runtimeLaterGas.Gases["co2"]=.001;
        Lab.Settle(runtimeLaterGas);Lab.Settle(runtimeLaterGas);
        double runtimeLaterGasMass=runtimeLaterGas.Mass();runtimeLaterGas.Gases["co2"]=.001;Lab.Settle(runtimeLaterGas);
        Check("player later gaseous carbon dioxide clears carbonate without mass loss",
              runtimeLaterGas.Get("caco3")<1e-8&&System.Math.Abs(runtimeLaterGas.Get("HCO3-")-.002)<1e-8&&
              System.Math.Abs(runtimeLaterGas.Mass()-runtimeLaterGasMass-ChemData.Sub["co2"].Mass*.001)<1e-7);
        var playerCombustion=new Vessel(VesselKind.Bottle,Vector3.zero){Sealed=true,Temp=25};
        playerCombustion.Solutes["n_hexane"]=.001;playerCombustion.Gases["o2"]=.00475;
        double playerCombustionMass=playerCombustion.Mass();Lab.Ignite(playerCombustion);
        Check("player sealed organic combustion is oxygen limited and conserves mass",
              System.Math.Abs(playerCombustion.Get("n_hexane")-.0005)<1e-10&&
              System.Math.Abs(playerCombustion.Get("co2")-.003)<1e-10&&
              System.Math.Abs(playerCombustion.Water-.0035*Formula.MolarMass("H2O"))<1e-9&&
              System.Math.Abs(playerCombustion.Mass()-playerCombustionMass)<1e-7&&playerCombustion.Temp==25);
        var playerOxygenOnly=new Vessel(VesselKind.Bottle,Vector3.zero){Sealed=true};playerOxygenOnly.Gases["o2"]=.01;
        int playerOxygenLog=Lab.Log.Count;Lab.Ignite(playerOxygenOnly);
        Check("player oxygen without fuel cannot claim combustion",!playerOxygenOnly.Fx.Flame&&
              !Lab.Log.GetRange(playerOxygenLog,Lab.Log.Count-playerOxygenLog).Exists(x=>x.Kind=="rxn"));
        var playerReduction=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100};
        playerReduction.Solutes["ch3cho"]=.004;playerReduction.Undissolved["nabh4"]=.001;
        double playerReductionMass=playerReduction.Mass();Lab.Settle(playerReduction);
        Check("player borohydride reduction consumes reagent and preserves mass",
              System.Math.Abs(playerReduction.Get("c2h5oh")-.004)<1e-9&&
              System.Math.Abs(playerReduction.Get("nabo2")-.001)<1e-9&&
              System.Math.Abs(playerReduction.Mass()-playerReductionMass)<1e-7);
        var playerAzo=new Vessel(VesselKind.Beaker,Vector3.zero){Water=100,Temp=4};
        playerAzo.Solutes["c6h5n2cl"]=.001;playerAzo.Undissolved["phoh"]=.001;
        double playerAzoMass=playerAzo.Mass();Lab.Settle(playerAzo);
        Check("player phenol coupling retains oxygen in hydroxyazo product",
              playerAzo.Get("azo_dye")>.00099&&playerAzo.Get("azo_benzene")<1e-10&&
              System.Math.Abs(playerAzo.Mass()-playerAzoMass)<1e-7);
        var dosingUi=FindObjectOfType<LabUI>();
        var rack=dosingUi==null?null:System.Array.Find(dosingUi.GetComponentsInChildren<RectTransform>(true),x=>x.name=="RightPanel");
        Check("desktop reagent rack uses slim 318-unit layout",rack!=null&&Mathf.Abs(rack.rect.width-318f)<1f);
        var pet=PetView.Instance;
        if(pet!=null){
            pet.Say("你好",30f);
            var shortBubble=pet.BubbleSize;
            pet.Say(new string('测',100),30f);
            Canvas.ForceUpdateCanvases();
            var longBubble=pet.BubbleSize;
            var bubble=pet.transform.Find("Bubble") as RectTransform;
            var bubbleText=bubble!=null?bubble.GetComponentInChildren<UnityEngine.UI.Text>():null;
            bool textFits=bubbleText!=null&&bubbleText.preferredHeight<=bubbleText.rectTransform.rect.height+2f;
            bool onScreen=false;
            if(bubble!=null){
                var corners=new Vector3[4];bubble.GetWorldCorners(corners);
                var lo=RectTransformUtility.WorldToScreenPoint(null,corners[0]);
                var hi=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
                onScreen=lo.x>=-1&&lo.y>=-1&&hi.x<=Screen.width+1&&hi.y<=Screen.height+1;
            }
            Check("pet bubble adapts long text without clipping",pet.BubbleVisible&&longBubble.y>shortBubble.y&&
                  longBubble.x<=420.1f&&textFits);
            Check("pet long bubble stays inside screen",onScreen);
            pet.ShutUp();
            var petSprite=pet.transform.Find("Sprite") as RectTransform;
            var petHome=pet.Home;
            var centreBefore=RectTransformUtility.WorldToScreenPoint(null,petSprite.TransformPoint(petSprite.rect.center));
            var press=centreBefore+new Vector2(12,-8);
            var petDragFinish=press+new Vector2(120,60);
            pet.SetDrag(true,press);pet.DragTo(petDragFinish);
            Canvas.ForceUpdateCanvases();
            var centreAfter=RectTransformUtility.WorldToScreenPoint(null,petSprite.TransformPoint(petSprite.rect.center));
            Check("pet drag follows screen pointer with preserved grab offset",
                  Vector2.Distance(centreAfter-centreBefore,new Vector2(120,60))<1f);
            pet.SetDrag(false,petDragFinish);pet.Home=petHome;pet.ApplyLayout();
        }else Check("pet bubble exists for layout audit",false);
        var tagWorkspace=dosingUi.TagWorkspaceRect();
        var overlayRects=new List<Rect>();
        int visibleVesselTags=0,visibleEquationTags=0;
        bool overlaysInside=true,overlaysSeparate=true;
        foreach(var tag in FindObjectsOfType<VesselTag>())if(tag.RenderVisible){
            var r=tag.ScreenRect;if(r.width<=0||r.height<=0)continue;
            visibleVesselTags++;
            overlaysInside&=r.xMin>=tagWorkspace.xMin-1&&r.yMin>=tagWorkspace.yMin-1
                         &&r.xMax<=tagWorkspace.xMax+1&&r.yMax<=tagWorkspace.yMax+1;
            foreach(var prior in overlayRects)overlaysSeparate&=!r.Overlaps(prior);
            overlayRects.Add(r);
        }
        foreach(var eq in FindObjectsOfType<EquationTag>()){
            var r=eq.ScreenRect;if(r.width<=0||r.height<=0)continue;
            visibleEquationTags++;
            overlaysInside&=r.xMin>=tagWorkspace.xMin-1&&r.yMin>=tagWorkspace.yMin-1
                         &&r.xMax<=tagWorkspace.xMax+1&&r.yMax<=tagWorkspace.yMax+1;
            foreach(var prior in overlayRects)overlaysSeparate&=!r.Overlaps(prior);
            overlayRects.Add(r);
        }
        Check("demo vessel tags and equation stay inside visible workspace",visibleVesselTags>=4&&visibleEquationTags>0&&overlaysInside);
        Check("demo vessel tags and equation do not cover each other",overlaysSeparate);
        var demoTags=System.Array.FindAll(FindObjectsOfType<VesselTag>(),t=>Lab.Vessels.Contains(t.Model));
        Check("unselected vessel tags keep name and capacity without covering models",
              demoTags.Length>=4&&System.Array.TrueForAll(demoTags,t=>t.ShownSpeciesCount==0&&
              t.CapacityText=="标称容量 "+t.Model.Capacity.ToString("0")+" mL"));
        var selectedDemo=Lab.Vessels[1];dosingUi.SelectExternal(selectedDemo);yield return null;
        Check("selected vessel tag expands while neighbors stay compact",
              System.Array.TrueForAll(demoTags,t=>t.Model==selectedDemo?t.ShownSpeciesCount>0:t.ShownSpeciesCount==0));
        var detailToggle=System.Array.Find(dosingUi.GetComponentsInChildren<UnityEngine.UI.Button>(true),b=>b.name=="Btn_详情");
        if(detailToggle!=null)detailToggle.onClick.Invoke();yield return null;
        Check("all-details toggle restores simultaneous substance labels",
              detailToggle!=null&&TagSettings.ShowAllDetails&&System.Array.TrueForAll(demoTags,t=>t.ShownSpeciesCount>0));
        if(detailToggle!=null)detailToggle.onClick.Invoke();dosingUi.SelectExternal(null);yield return null;
        Check("all-details toggle returns to compact default",!TagSettings.ShowAllDetails&&
              System.Array.TrueForAll(demoTags,t=>t.ShownSpeciesCount==0));
        AuditRefractionForeground();
        AuditParticleLid();
        var stagedA=Lab.AddVessel(VesselKind.Beaker);var stagedB=Lab.AddVessel(VesselKind.Beaker);yield return null;yield return null;
        stagedA.Water=40;stagedB.Water=10;stagedA.Temp=80;stagedB.Temp=25;var stagedAPosition=stagedA.Pos;var stagedBPosition=stagedB.Pos;var stagedCamera=lab.Cam.transform.position;float stagedClock=Lab.Clock;
        dosingUi.BeginStagedPour(stagedA,stagedB);yield return new WaitForSeconds(1.1f);
        Check("staged UI pauses global clock and flies to isolated display",dosingUi.IsPouring&&PourTransaction.IsActive&&Lab.Clock==stagedClock&&stagedA.Pos.x>90&&stagedB.Pos.x>90&&lab.Cam.transform.position.x>90);
        dosingUi.SetPourUnits(true);
        var massSlider=System.Array.Find(dosingUi.GetComponentsInChildren<UnityEngine.UI.Slider>(),s=>s.name=="LiquidAmount");
        Check("staged pour gram slider uses actual available mass",massSlider!=null&&System.Math.Abs(massSlider.maxValue-40)<1e-5&&System.Math.Abs(massSlider.value-20)<1e-5);
        dosingUi.SetPourUnits(false);dosingUi.SetPourUnits(true);
        Check("staged pour unit roundtrip preserves quantity",System.Math.Abs(massSlider.value-20)<1e-5);
        var previewButton=System.Array.Find(dosingUi.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="预览 / Enter");
        previewButton.onClick.Invoke();yield return AuditLivePour(stagedA,stagedB,.4f,"forward startup");
        var stream=GameObject.Find("PourLiquidStream");
        Check("staged pour arc uses continuous liquid mesh not beads",stream!=null&&stream.GetComponent<MeshFilter>().sharedMesh.vertexCount>100&&GameObject.Find("PourLiquidPacket")==null&&stagedA.Water+stagedB.Water<50&&Lab.Clock==stagedClock);
        var streamMesh=stream.GetComponent<MeshFilter>().sharedMesh;var streamVertices=streamMesh.vertices;var streamTriangles=streamMesh.triangles;
        bool finiteStream=true;foreach(var vertex in streamVertices)finiteStream&=!float.IsNaN(vertex.sqrMagnitude)&&!float.IsInfinity(vertex.sqrMagnitude);
        double streamVolume=0;for(int i=0;i<streamTriangles.Length;i+=3)streamVolume+=Vector3.Dot(streamVertices[streamTriangles[i]],Vector3.Cross(streamVertices[streamTriangles[i+1]],streamVertices[streamTriangles[i+2]]))/6.0;
        double expectedStreamVolume=(50-stagedA.Water-stagedB.Water)/1000;
        Check("continuous stream has finite vertices and conserved airborne volume",finiteStream&&System.Math.Abs(System.Math.Abs(streamVolume)-expectedStreamVolume)<expectedStreamVolume*.02);
        ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Logs/staged-pour-flight.png"));yield return AuditLivePour(stagedA,stagedB,1.5f,"forward arrival");
        Check("staged pour arc finishes selected amount",System.Math.Abs(stagedA.Water-20)<1e-9&&System.Math.Abs(stagedB.Water-30)<1e-9);
        Check("staged pour visibly retains arrived-liquid temperature",System.Math.Abs(stagedB.Temp-(10*25+20*80)/30.0)<.02);
        var undoButton=System.Array.Find(dosingUi.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="撤销");undoButton.onClick.Invoke();yield return AuditLivePour(stagedA,stagedB,1.9f,"reverse entire flight");
        Check("staged pour undo flies inventory back",stagedA.Water==40&&stagedB.Water==10&&PourTransaction.IsActive);
        Check("staged pour undo restores both temperatures",System.Math.Abs(stagedA.Temp-80)<.02&&System.Math.Abs(stagedB.Temp-25)<.02);
        Check("completed reverse flight removes all stream objects",GameObject.Find("PourLiquidStream")==null&&GameObject.Find("PourSolidPacket")==null);
        dosingUi.enabled=false;yield return null;dosingUi.enabled=true;
        Check("staged UI disable cancels and restores camera and vessels",!PourTransaction.IsActive&&!dosingUi.IsPouring&&stagedA.Pos==stagedAPosition&&stagedB.Pos==stagedBPosition&&Vector3.Distance(lab.Cam.transform.position,stagedCamera)<.01f&&stagedA.Water==40&&stagedB.Water==10);
        var support=ApparatusSet.Add(ApparatusKind.Tripod,stagedAPosition);
        stagedA.TripodRef=support;support.Holds=stagedA;stagedA.LiftY=SnapController.LiftOntoApparatus(support);
        float supportedLift=stagedA.LiftY;
        var placement=new SnapController.Placement(stagedA);SnapController.Detach(stagedA);stagedA.Pos+=Vector3.right*2;placement.Restore();
        Check("pour drag restores original support bidirectionally",stagedA.TripodRef==support&&support.Holds==stagedA&&stagedA.Pos==stagedAPosition&&stagedA.LiftY==supportedLift);
        dosingUi.BeginStagedPour(stagedA,stagedB);yield return new WaitForSeconds(1.1f);
        double expectedStagedTemp=(20*stagedA.Temp+10*stagedB.Temp)/30;
        float finishClock=Lab.Clock;
        previewButton=System.Array.Find(dosingUi.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="预览 / Enter");
        previewButton.onClick.Invoke();yield return new WaitForSeconds(1.9f);
        Check("staged re-preview computes temperature from current snapshot",System.Math.Abs(stagedB.Temp-expectedStagedTemp)<.02);
        var finishButton=System.Array.Find(dosingUi.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="完成操作");
        finishButton.onClick.Invoke();yield return new WaitForSeconds(.3f);
        Check("finish keeps all reactions paused during return flight",PourTransaction.IsActive&&Lab.Clock==finishClock);
        yield return new WaitForSeconds(.7f);
        Check("finish commits selected inventory and resumes clock after return",!dosingUi.IsPouring&&!PourTransaction.IsActive&&Lab.Clock>finishClock&&System.Math.Abs(stagedA.Water-20)<1e-9&&System.Math.Abs(stagedB.Water-30)<1e-9);
        double ambientAfterCommit=25+(expectedStagedTemp-25)*System.Math.Exp(-.06*(Lab.Clock-finishClock));
        Check("staged commit retains mixed temperature after return",System.Math.Abs(stagedB.Temp-ambientAfterCommit)<.1);
        Check("finish restores camera and supported placement",stagedA.Pos==stagedAPosition&&stagedA.LiftY==supportedLift&&stagedA.TripodRef==support&&support.Holds==stagedA&&stagedB.Pos==stagedBPosition&&Vector3.Distance(lab.Cam.transform.position,stagedCamera)<.01f);
        SnapController.Detach(stagedA);ApparatusSet.All.Remove(support);
        Lab.RemoveVessel(stagedA);Lab.RemoveVessel(stagedB);yield return null;
        var meteredReceiver=Lab.AddVessel(VesselKind.Beaker);meteredReceiver.Water=30;meteredReceiver.Temp=25;
        var meteredBurette=Lab.AddVessel(VesselKind.Burette);meteredBurette.Water=10;meteredBurette.Temp=80;
        Check("burette attaches through shared snap route",SnapController.AttachMetered(meteredBurette,meteredReceiver));
        meteredReceiver.Pos+=new Vector3(.3f,0,.2f);yield return null;yield return null;
        Check("attached burette follows receiver above rim",meteredBurette.Pos==meteredReceiver.Pos&&Mathf.Abs(meteredBurette.LiftY-meteredReceiver.LiftY-SnapController.LiftOnto(meteredReceiver,meteredBurette))<1e-5f);
        dosingUi.SelectExternal(meteredBurette);yield return null;yield return null;
        Canvas.ForceUpdateCanvases();
        var meteredPanelRect=GameObject.Find("MeteredToolPanel").GetComponent<RectTransform>();
        var logScrollRect=GameObject.Find("LogScroll").GetComponent<RectTransform>();
        var meterCorners=new Vector3[4];var logCorners=new Vector3[4];
        meteredPanelRect.GetWorldCorners(meterCorners);logScrollRect.GetWorldCorners(logCorners);
        var meterBox=Rect.MinMaxRect(meterCorners[0].x,meterCorners[0].y,meterCorners[2].x,meterCorners[2].y);
        var logBox=Rect.MinMaxRect(logCorners[0].x,logCorners[0].y,logCorners[2].x,logCorners[2].y);
        Check("metered controls and log viewport do not overlap",!meterBox.Overlaps(logBox));
        var frameMetered=GameObject.Find("FrameMeteredTool").GetComponentInChildren<UnityEngine.UI.Button>();
        var beforeFrame=lab.Cam.transform.position;
        frameMetered.onClick.Invoke();yield return null;yield return null;
        var buretteTop=lab.Cam.WorldToScreenPoint(meteredBurette.Pos+Vector3.up*(meteredBurette.LiftY+VesselMesh.Get(VesselKind.Burette).H));
        var receiverBottom=lab.Cam.WorldToScreenPoint(meteredReceiver.Pos+Vector3.up*meteredReceiver.LiftY);
        lines.Add($"METRIC burette frame top={buretteTop} bottom={receiverBottom} screen={Screen.width}x{Screen.height}");
        Check("explicit burette full-view button frames tool and receiver between toolbar and log",
              lab.MeteredFrameActiveFor(meteredBurette)&&buretteTop.z>0&&receiverBottom.z>0&&
              buretteTop.y<Screen.height-25&&receiverBottom.y>Screen.height*.20f&&
              buretteTop.x>Screen.width*.30f&&buretteTop.x<Screen.width*.70f);
        Check("metered full-view suppresses unrelated vessel labels",
              System.Array.TrueForAll(FindObjectsOfType<VesselTag>(true),t=>
                  !t.RenderVisible||t.Model==meteredBurette||t.Model==meteredReceiver));
        Check("metered full-view suppresses unrelated reaction labels",
              System.Array.TrueForAll(FindObjectsOfType<EquationTag>(true),t=>
                  !t.gameObject.activeInHierarchy||t.Ev?.Vessel==meteredBurette||t.Ev?.Vessel==meteredReceiver));
        lab.RenderToFile(1280,800,Path.GetFullPath("Logs/metered-framed-burette.png"));yield return null;
        var framePosition=lab.Cam.transform.position;var frameRight=lab.Cam.transform.right;
        lab.MoveView(1,0,.2f);yield return null;
        Check("metered framing never locks FPS camera or reverses D movement",
              Vector3.Dot(lab.Cam.transform.position-framePosition,frameRight)>.35f);
        frameMetered.onClick.Invoke();yield return null;
        Check("metered frame toggle restores prior free view",!lab.MeteredFrameActiveFor(meteredBurette)&&Vector3.Distance(lab.Cam.transform.position,beforeFrame)<.02f);
        Check("returning from metered frame restores normal bench labels",
              System.Array.Exists(FindObjectsOfType<VesselTag>(true),t=>t.RenderVisible&&t.Model!=meteredBurette&&t.Model!=meteredReceiver));
        var flowDial=FindObjectOfType<FlowDial>();
        var dialPointer=GameObject.Find("FlowDialPointer").GetComponent<RectTransform>();
        Quaternion stoppedDialRotation=dialPointer.localRotation;
        Check("metered tool hides legacy pour amount controls",GameObject.Find("PourBar")==null);
        flowDial.OnDrag(new PointerEventData(EventSystem.current){delta=new Vector2(0,20)});yield return new WaitForSeconds(.6f);
        Check("burette UI dial drives real timed delivery",meteredBurette.MeteredFlowMlPerSecond>0&&meteredBurette.Water<10&&meteredBurette.Water>9&&meteredReceiver.Water>30&&!PourTransaction.IsActive);
        Check("burette delivery mixes temperature as liquid arrives",meteredReceiver.Temp>25&&meteredReceiver.Temp<80);
        var buretteView=System.Array.Find(FindObjectsOfType<VesselView>(),v=>v.Model==meteredBurette);
        Check("burette produces a visible non-pickable flow",buretteView!=null&&buretteView.TestMeteredFlowVisible&&buretteView.GetComponentInChildren<MeteredFlowVisual>().GetComponentsInChildren<Collider>().Length==0);
        Check("burette dial pointer visibly changes with flow",Quaternion.Angle(stoppedDialRotation,dialPointer.localRotation)>20&&dialPointer.rect.height>10&&!dialPointer.GetComponent<UnityEngine.UI.Image>().raycastTarget);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/burette-flow-ui.png"));yield return null;
        SnapController.Detach(meteredBurette);double stoppedWater=meteredBurette.Water;yield return new WaitForSeconds(.2f);
        Check("detaching stops metered delivery",meteredBurette.MeteredFlowMlPerSecond==0&&meteredBurette.Water==stoppedWater);
        Check("burette flow disappears after detaching",buretteView!=null&&!buretteView.TestMeteredFlowVisible);
        var meteredPipette=Lab.AddVessel(VesselKind.Pipette);
        Check("pipette attaches through shared snap route",SnapController.AttachMetered(meteredPipette,meteredReceiver));
        dosingUi.SelectExternal(meteredPipette);yield return null;yield return null;
        frameMetered=GameObject.Find("FrameMeteredTool").GetComponentInChildren<UnityEngine.UI.Button>();
        frameMetered.onClick.Invoke();yield return null;yield return null;
        var pipetteTop=lab.Cam.WorldToScreenPoint(meteredPipette.Pos+Vector3.up*(meteredPipette.LiftY+VesselMesh.Get(VesselKind.Pipette).H));
        receiverBottom=lab.Cam.WorldToScreenPoint(meteredReceiver.Pos+Vector3.up*meteredReceiver.LiftY);
        lines.Add($"METRIC pipette frame top={pipetteTop} bottom={receiverBottom} screen={Screen.width}x{Screen.height}");
        Check("explicit pipette full-view button frames tool and receiver",
              lab.MeteredFrameActiveFor(meteredPipette)&&pipetteTop.z>0&&receiverBottom.z>0&&
              pipetteTop.y<Screen.height-25&&receiverBottom.y>Screen.height*.20f);
        lab.RenderToFile(1280,800,Path.GetFullPath("Logs/metered-framed-pipette.png"));yield return null;
        frameMetered.onClick.Invoke();yield return null;
        var aspirate=System.Array.Find(dosingUi.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="抽取");
        aspirate.onClick.Invoke();yield return new WaitForSeconds(.4f);
        var pipetteView=System.Array.Find(FindObjectsOfType<VesselView>(),v=>v.Model==meteredPipette);
        Check("pipette aspiration shows a moving liquid path",pipetteView!=null&&pipetteView.TestMeteredFlowVisible);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/pipette-flow-ui.png"));yield return null;
        var stopMetered=GameObject.Find("StopFlow").GetComponentInChildren<UnityEngine.UI.Button>();stopMetered.onClick.Invoke();
        double aspirated=meteredPipette.Water;yield return new WaitForSeconds(.2f);
        Check("pipette UI aspirates and stops without staged pour",aspirated>0&&aspirated<25&&meteredPipette.Water==aspirated&&!PourTransaction.IsActive);
        Check("pipette flow disappears after stopping",pipetteView!=null&&!pipetteView.TestMeteredFlowVisible);
        var release=System.Array.Find(dosingUi.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="释放");
        release.onClick.Invoke();yield return new WaitForSeconds(.2f);stopMetered.onClick.Invoke();
        Check("pipette UI releases real liquid",meteredPipette.Water<aspirated);
        release.onClick.Invoke();Lab.RemoveVessel(meteredReceiver);double removedTargetWater=meteredPipette.Water;
        yield return new WaitForSeconds(.2f);
        Check("receiver removal detaches tool and stops flow",meteredPipette.AttachedTo==null&&meteredPipette.LiftY==0&&meteredPipette.MeteredFlowMlPerSecond==0&&meteredPipette.Water==removedTargetWater);
        Lab.RemoveVessel(meteredPipette);Lab.RemoveVessel(meteredBurette);dosingUi.SelectExternal(null);yield return null;
        Check("hiding metered controls restores full log width",Mathf.Abs(logScrollRect.anchorMax.x-1)<1e-5f);
        foreach(VesselKind kind in System.Enum.GetValues(typeof(VesselKind))){
            string buttonName="Btn_"+VesselNames.Name(kind);
            var button=System.Array.Find(dosingUi.GetComponentsInChildren<UnityEngine.UI.Button>(true),b=>b.name==buttonName);
            Check("shelf shows nominal capacity "+kind,button!=null&&button.GetComponentInChildren<UnityEngine.UI.Text>(true).text.Contains(Vessel.NominalCapacity(kind).ToString("0")+" mL"));
            var model=Lab.AddVessel(kind);yield return null;yield return null;
            var capacityTag=System.Array.Find(FindObjectsOfType<VesselTag>(),t=>t.Model==model);
            if(capacityTag!=null)capacityTag.Sync();
            Check("empty vessel retains capacity tag "+kind,capacityTag!=null&&capacityTag.CapacityText=="标称容量 "+model.Capacity.ToString("0")+" mL");
            var view=System.Array.Find(FindObjectsOfType<VesselView>(),v=>v.Model==model);
            if((kind==VesselKind.Burette||kind==VesselKind.Pipette)&&view!=null)CaptureVesselModel(view,"volumetric-model-"+kind);
            if(kind==VesselKind.Burette&&view!=null){
                var handle=view.GlassRoot.Find("VolumetricDetails/StopcockHandle");
                Check("burette stopcock handle belongs to selectable vessel",handle!=null&&handle.GetComponent<Collider>()!=null&&handle.GetComponentInParent<VesselView>()==view&&handle.GetComponent<Renderer>().sharedMaterial.shader.isSupported);
            }
            if(kind==VesselKind.Pipette&&view!=null){
                var band=view.GlassRoot.Find("VolumetricDetails/25mLBlueCodeBand");
                Check("pipette blue code band does not intercept reagent drops",band!=null&&band.GetComponent<Collider>()==null&&band.GetComponent<Renderer>().sharedMaterial.shader.isSupported);
            }
            var graduations=view!=null?view.GetComponentInChildren<VesselGraduations>():null;
            bool hasScale=VesselGraduations.Marks(kind).Count>0;
            Check("vessel printed scale is renderable and does not block drops "+kind,!hasScale?graduations==null:
                graduations!=null&&graduations.GetComponent<MeshRenderer>().sharedMaterial.shader.isSupported&&graduations.GetComponent<MeshFilter>().sharedMesh.vertexCount>0&&graduations.GetComponentsInChildren<Collider>().Length==0);
            if(kind==VesselKind.Cylinder&&view!=null){
                var foot=view.GlassRoot.Find("SolidHexagonalFoot");
                Check("cylinder solid base selects its parent vessel",foot!=null&&foot.GetComponent<Collider>()!=null&&foot.GetComponentInParent<VesselView>()==view);
                model.Water=100;view.Refresh(true);for(int settled=0;settled<25;settled++)yield return null;
                CaptureVesselModel(view,"cylinder-100ml");
            }
            Lab.RemoveVessel(model);yield return null;
        }
        var doseVessel=Lab.AddVessel(VesselKind.Beaker);
        dosingUi.ShowDoseDialog(doseVessel,"h2o");
        Check("drop quantity dialog opens",dosingUi.DoseDialogOpen&&dosingUi.IsDosing);
        foreach(var invalid in new[]{"0","-1","NaN","Infinity","abc"}){
            dosingUi.SetDoseForTest(invalid,false);dosingUi.ConfirmDose();
            Check("dose rejects "+invalid,dosingUi.DoseDialogOpen&&doseVessel.Water==0);
        }
        dosingUi.SetDoseForTest("18",false);dosingUi.ConfirmDose();
        Check("gram dose converts mass to volume",!dosingUi.DoseDialogOpen&&System.Math.Abs(doseVessel.Water-18/ChemData.Sub["h2o"].Density)<1e-8);
        double beforeDose=doseVessel.Water;dosingUi.ConfirmDose();
        Check("duplicate confirm cannot add twice",doseVessel.Water==beforeDose);
        dosingUi.ShowDoseDialog(doseVessel,"h2o");dosingUi.SetDoseForTest("0.1",true);dosingUi.ConfirmDose();
        Check("mol dose uses molar mass",System.Math.Abs(doseVessel.Water-beforeDose-0.1*ChemData.Sub["h2o"].Mass/ChemData.Sub["h2o"].Density)<1e-8);
        beforeDose=doseVessel.Water;
        dosingUi.ShowDoseDialog(doseVessel,"h2o");dosingUi.SetDoseForTest("10000",false);dosingUi.ConfirmDose();
        Check("over-capacity dose is atomic",dosingUi.DoseDialogOpen&&doseVessel.Water==beforeDose);
        dosingUi.CloseDose();
        Check("cancel closes dosing state",!dosingUi.IsDosing);
        dosingUi.ShowDoseDialog(doseVessel,"h2o");dosingUi.SetDoseForTest("1",false);
        var unitButton=System.Array.Find(dosingUi.GetComponentsInChildren<DoseModalButton>(),b=>b.transform.parent.name=="DoseUnit");
        Check("unit button provides safe keyboard submit",unitButton!=null);
        var submit=new UnityEngine.EventSystems.BaseEventData(UnityEngine.EventSystems.EventSystem.current);
        UnityEngine.EventSystems.ExecuteEvents.Execute(unitButton.gameObject,submit,UnityEngine.EventSystems.ExecuteEvents.submitHandler);
        dosingUi.ConfirmDose();
        Check("keyboard submit from unit control preserves grams and adds once",!dosingUi.DoseDialogOpen&&System.Math.Abs(doseVessel.Water-beforeDose-1/ChemData.Sub["h2o"].Density)<1e-8);
        beforeDose=doseVessel.Water;
        dosingUi.ShowDoseDialog(doseVessel,"h2o");Lab.RemoveVessel(doseVessel);dosingUi.ConfirmDose();
        Check("removed target cannot receive dose",!dosingUi.DoseDialogOpen&&doseVessel.Water==beforeDose);
        var gpuVessel=Lab.AddVessel(VesselKind.Beaker);gpuVessel.AddWater(100);gpuVessel.Pos=new Vector3(10,0,0);
        yield return null;yield return null;
        VesselView gpuView=null;foreach(var candidate in FindObjectsOfType<VesselView>())if(candidate.Model==gpuVessel)gpuView=candidate;
        Check("GPU compute capability",gpuView!=null&&gpuView.Fluid.Supported);
        var gpuBefore=gpuView.Fluid.ReadbackForTest();
        gpuVessel.Stirring=true;
        float gpuStart=Time.realtimeSinceStartup;int gpuFrames=0;
        while(gpuFrames++<30)yield return null;
        var gpuAfter=gpuView.Fluid.ReadbackForTest();float motionDistance=0;
        for(int i=0;i<gpuBefore.Length;i++)motionDistance+=Vector3.Distance(gpuBefore[i],gpuAfter[i]);
        Check("GPU kernel actually moves 3D particles",motionDistance>0.01f&&gpuView.Fluid.DispatchCount>0);
        Check("GPU mass and particle count preserved",gpuBefore.Length==gpuAfter.Length&&Mathf.Abs((float)gpuView.Fluid.MassVolumeMl-100)<0.001f);
        lines.Add("METRIC 30-frame wall time seconds="+(Time.realtimeSinceStartup-gpuStart)+" GPU="+SystemInfo.graphicsDeviceName);
        var cam = lab.Cam;
        var eye = cam.transform.position;
        var rotation = cam.transform.rotation;
        var vessel = Lab.Vessels[0];
        lab.SetFocus(vessel);
        var oldPos = vessel.Pos;
        vessel.Pos += Vector3.right;
        yield return new WaitForSeconds(0.3f);
        Check("select/move vessel does not move camera", Vector3.Distance(eye, cam.transform.position) < 0.001f
            && Quaternion.Angle(rotation, cam.transform.rotation) < 0.01f);
        vessel.Pos = oldPos;
        var pointer = new Vector2(Screen.width * 0.45f, Screen.height * 0.7f);
        lab.ShowObjectMenu(new LabPick.Hit { vessel = vessel }, pointer);
        yield return null;
        Check("vessel context menu opens at pointer", MenuAt(PetMenu.CurrentPanel, pointer));
        var heat = PetMenu.CurrentPanel.GetComponentInChildren<UnityEngine.UI.Button>();
        bool heated = vessel.Heating;
        heat.onClick.Invoke();
        Check("menu action updates vessel and closes", vessel.Heating != heated && !PetMenu.IsOpen);
        vessel.Heating = heated;
        var pressureBottle=Lab.AddVessel(VesselKind.Bottle);
        lab.ShowObjectMenu(new LabPick.Hit{vessel=pressureBottle},pointer);
        yield return null;
        PetMenu.CurrentPanel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
        Check("bottle menu seals and closes",pressureBottle.Sealed&&!PetMenu.IsOpen);
        yield return null;yield return null;
        VesselView bottleView=null;foreach(var candidate in FindObjectsOfType<VesselView>())if(candidate.Model==pressureBottle)bottleView=candidate;
        Check("sealed bottle has visible cap",bottleView!=null&&bottleView.transform.Find("SealedCap").gameObject.activeSelf);
        lab.ShowObjectMenu(new LabPick.Hit{vessel=pressureBottle},pointer);
        yield return null;
        PetMenu.CurrentPanel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
        Check("bottle menu opens and closes",!pressureBottle.Sealed&&!PetMenu.IsOpen);
        yield return null;yield return null;
        Check("open bottle removes cap",!bottleView.transform.Find("SealedCap").gameObject.activeSelf);
        foreach (var pos in new[] { Vector2.zero, new Vector2(Screen.width-1, 1),
            new Vector2(Screen.width-1, Screen.height-1), new Vector2(1, Screen.height-1) }) {
            PetMenu.Show(PetView.Instance, pos);
            yield return null;
            Check("pet menu stays within viewport " + pos, InViewport(PetMenu.CurrentPanel));
            var dismiss = FindObjectOfType<MenuDismiss>();
            UnityEngine.EventSystems.ExecuteEvents.Execute(dismiss.gameObject,
                new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current),
                UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            Check("outside pointer closes menu", !PetMenu.IsOpen);
            yield return null;
        }
        var calc = EnthalpyScene.Show(lab.gameObject, LabUI.UiFont);
        calc.OnWorldRightClick(pointer);
        yield return null;
        var calcPanel = System.Array.Find(calc.GetComponentsInChildren<RectTransform>(), x => x.name == "Menu");
        Check("calculator menu opens at pointer", MenuAt(calcPanel, pointer));
        calc.CloseMenu();
        var returnToLab=System.Array.Find(calc.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="返回实验台");
        returnToLab.onClick.Invoke();
        Check("calculator return restores visible interactive laboratory UI",dosingUi.gameObject.activeInHierarchy&&!LabBootstrap.CalculatorActive);
        lines.Add("METRIC camera menu drift="+(cam.transform.position-eye).ToString("F6"));
        Check("menus and calculator preserve laboratory camera",Vector3.Distance(cam.transform.position,eye)<.001f);
        eye=cam.transform.position;
        var expected = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * 0.25f;
        lab.MoveView(0, 1, 0.1f);
        yield return null; yield return null;
        lines.Add("METRIC camera W start="+eye.ToString("F6")+" end="+cam.transform.position.ToString("F6")+" expectedDelta="+expected.ToString("F6"));
        Check("W follows camera heading on horizontal plane", Vector3.Distance(cam.transform.position - eye, expected) < 0.001f);
        eye = cam.transform.position;
        lab.RotateView(new Vector2(60, 15));
        yield return null; yield return null;
        Check("mouse rotation preserves eye position", Vector3.Distance(eye, cam.transform.position) < 0.001f);
        expected = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized * 0.25f;
        lab.MoveView(1, 0, 0.1f);
        yield return null; yield return null;
        Check("D follows camera right", Vector3.Distance(cam.transform.position - eye, expected) < 0.001f);
        eye=cam.transform.position;
        lab.MoveView(-1,0,0.1f);
        yield return null;yield return null;
        Check("A follows camera left",Vector3.Distance(cam.transform.position-eye,-expected)<0.001f);
        var headingBefore=Vector3.ProjectOnPlane(cam.transform.forward,Vector3.up).normalized;
        eye=cam.transform.position;
        lab.RotateView(new Vector2(20,0));
        yield return null;yield return null;
        var headingAfter=Vector3.ProjectOnPlane(cam.transform.forward,Vector3.up).normalized;
        float dragAngle=Vector3.SignedAngle(headingBefore,headingAfter,Vector3.up);
        Check("mouse horizontal drag is reversed",dragAngle<-1);
        Check("mouse drag rotation sensitivity is reduced",dragAngle>-5f&&dragAngle<-3f);
        Check("horizontal mouse drag preserves eye",Vector3.Distance(cam.transform.position,eye)<0.001f);

        // 隔离出一个可见区域，逐种验证实际渲染物上的射线拾取。
        Lab.Reset(); ApparatusSet.Clear(); TestSet.Clear();
        yield return null; yield return null;
        var center = cam.transform.position + cam.transform.forward * 4f;
        var dragTarget=Lab.AddVessel(VesselKind.Beaker);dragTarget.Pos=center;
        yield return null;yield return null;
        var dragPoint=cam.WorldToScreenPoint(center+Vector3.up*VesselMesh.Get(VesselKind.Beaker).H*0.5f);
        dosingUi.BeginReagentDrag("h2o",dragPoint);
        Check("reagent drag finds vessel under pointer",dosingUi.DropTarget==dragTarget);
        dosingUi.EndReagentDrag(dragPoint);
        Check("reagent release opens amount dialog without dosing",dosingUi.DoseDialogOpen&&dragTarget.Water==0);
        dosingUi.CloseDose();
        dosingUi.BeginReagentDrag("h2o",new Vector2(-100,-100));dosingUi.EndReagentDrag(new Vector2(-100,-100));
        Check("drop outside viewport cannot dose",!dosingUi.DoseDialogOpen&&dragTarget.Water==0);
        Lab.RemoveVessel(dragTarget);
        yield return null;yield return null;
        foreach (VesselKind kind in System.Enum.GetValues(typeof(VesselKind))) {
            var v = Lab.AddVessel(kind); v.Pos = center;
            yield return null; yield return null;
            var g = VesselMesh.Get(kind);
            var hit = LabPick.Pick(cam.WorldToScreenPoint(center + Vector3.up * g.H * 0.45f), cam);
            Check("pick vessel " + kind, hit.vessel == v);
            Lab.RemoveVessel(v);
            yield return null; yield return null;
        }
        // 用户要求按真实前后遮挡拾取：玻璃器皿在前时，后方旋塞不可穿透选中。
        var rearBurette=Lab.AddVessel(VesselKind.Burette);rearBurette.Pos=center;
        var frontGlass=Lab.AddVessel(VesselKind.Beaker);
        yield return null;yield return null;
        var rearView=System.Array.Find(FindObjectsOfType<VesselView>(),v=>v.Model==rearBurette);
        var frontView=System.Array.Find(FindObjectsOfType<VesselView>(),v=>v.Model==frontGlass);
        var stopcock=rearView?.GlassRoot.Find("VolumetricDetails/StopcockHandle")?.GetComponent<Collider>();
        bool glassOccludesStopcock=false;
        if(stopcock!=null&&frontView!=null){
            var handlePosition=stopcock.bounds.center;
            var priorCameraRotation=cam.transform.rotation;
            // Aim this isolated fixture at the actual handle. Saved camera yaw
            // can otherwise project it outside the viewport, producing a
            // false failure without testing any glass/handle occlusion.
            cam.transform.LookAt(handlePosition);
            var pointerToHandle=cam.WorldToScreenPoint(handlePosition);
            float bestFront=float.PositiveInfinity,bestHandle=float.PositiveInfinity;
            string candidatePick="-";
            for(int step=6;step<=25&&!glassOccludesStopcock;step++){
                float distance=step*.1f;
                // Keep the beaker's mid-height on the same camera ray. The
                // camera pitch differs by saved settings, so a fixed bench Y
                // makes this occlusion fixture nondeterministic.
                yield return null;yield return null;
                // Camera position and yaw can both change during yields. Build
                // this fixture from the current camera and synchronise the live
                // collider before testing, not from a stale screen pointer.
                handlePosition=stopcock.bounds.center;
                cam.transform.LookAt(handlePosition);
                pointerToHandle=cam.WorldToScreenPoint(handlePosition);
                var towardCamera=(cam.transform.position-handlePosition).normalized;
                frontGlass.Pos=handlePosition+towardCamera*distance
                              -Vector3.up*(VesselMesh.Get(VesselKind.Beaker).H*.5f);
                frontView.transform.localPosition=frontGlass.Pos+Vector3.up*(frontGlass.LiftY+.003f);
                Physics.SyncTransforms();
                var raw=Physics.RaycastAll(cam.ScreenPointToRay(pointerToHandle));
                float frontDistance=float.PositiveInfinity,handleDistance=float.PositiveInfinity;
                foreach(var h in raw){
                    if(h.collider.GetComponentInParent<VesselView>()==frontView)frontDistance=Mathf.Min(frontDistance,h.distance);
                    if(h.collider==stopcock)handleDistance=Mathf.Min(handleDistance,h.distance);
                }
                if(step==6&&handleDistance==float.PositiveInfinity)
                    Debug.Log("[OcclusionFixture] camera="+cam.transform.position+" handle="+handlePosition+
                              " currentBounds="+stopcock.bounds.center+" enabled="+stopcock.enabled+
                              " active="+stopcock.gameObject.activeInHierarchy+" ray="+cam.ScreenPointToRay(pointerToHandle)+
                              " hits="+raw.Length+" rear="+rearView.transform.position);
                if(frontDistance<handleDistance&&handleDistance<float.PositiveInfinity){
                    bestFront=frontDistance;bestHandle=handleDistance;
                    var picked=LabPick.Pick(pointerToHandle,cam);
                    candidatePick=picked.vessel?.Id??picked.Label;
                    glassOccludesStopcock=picked.vessel==frontGlass;
                }
            }
            frontGlass.Pos+=Vector3.right*5;yield return null;yield return null;
            handlePosition=stopcock.bounds.center;
            cam.transform.LookAt(handlePosition);
            pointerToHandle=cam.WorldToScreenPoint(handlePosition);
            var unoccluded=LabPick.Pick(pointerToHandle,cam);
            lines.Add("METRIC stopcock occlusion pointer="+pointerToHandle.ToString("F2")+
                      " front="+bestFront.ToString("F3")+" handle="+bestHandle.ToString("F3")+
                      " occludedPick="+candidatePick+" freePick="+(unoccluded.vessel?.Id??unoccluded.Label));
            Check("unoccluded stopcock still selects burette",unoccluded.vessel==rearBurette);
            cam.transform.rotation=priorCameraRotation;
        }
        Check("front glass occludes rear stopcock",glassOccludesStopcock);
        Lab.RemoveVessel(frontGlass);Lab.RemoveVessel(rearBurette);yield return null;yield return null;
        foreach (ApparatusKind kind in System.Enum.GetValues(typeof(ApparatusKind))) {
            var a = ApparatusSet.Add(kind, center);
            yield return null; yield return null;
            var apparatusView=System.Array.Find(FindObjectsOfType<ApparatusView>(),v=>v.Model==a);
            Check("apparatus uses supported metal shader not bench grid "+kind,apparatusView.GetComponent<MeshRenderer>().sharedMaterial.shader.name=="ChemLab/Metal"&&apparatusView.GetComponent<MeshRenderer>().sharedMaterial.shader.isSupported);
            foreach(string detailName in kind==ApparatusKind.Tripod?new[]{"WireGauze","CeramicCentre"}:new[]{"GasInlet"}){
                var detail=apparatusView.transform.Find(detailName);var detailCollider=detail!=null?detail.GetComponent<Collider>():null;
                bool hittable=detailCollider!=null&&detailCollider.Raycast(new Ray(cam.transform.position,detailCollider.bounds.center-cam.transform.position),out _,100);
                Check("new apparatus detail is pickable "+detailName,hittable&&detail.GetComponentInParent<ApparatusView>()==apparatusView);
            }
            CaptureModel(apparatusView.transform,ApparatusDimensions.Top(kind)+.04f,"apparatus-"+kind);
            lab.ShowObjectMenu(new LabPick.Hit { apparatus = a }, pointer);
            Check("apparatus menu " + kind, PetMenu.IsOpen);
            if(kind==ApparatusKind.BunsenBurner){
                var controlButton=System.Array.Find(PetMenu.CurrentPanel.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text=="调节火力…");
                Check("burner context menu offers UI power dialog without model knob",controlButton!=null);
                controlButton.onClick.Invoke();
                Check("burner power dialog opens and dismisses context menu",dosingUi.BurnerDialogOpen&&!PetMenu.IsOpen);
                var sliderObject=GameObject.Find("BurnerPowerSlider");
                var ignitionObject=GameObject.Find("BurnerIgnition");
                Check("burner slider object active",sliderObject!=null);
                Check("burner ignition object active",ignitionObject!=null);
                Check("burner flame transform exists",apparatusView.Flame!=null);
                var powerSlider=sliderObject.GetComponent<UnityEngine.UI.Slider>();
                var ignition=ignitionObject.GetComponentInChildren<UnityEngine.UI.Button>();
                Check("burner controls contain interactive components",powerSlider!=null&&ignition!=null);
                powerSlider.value=.25f;ignition.onClick.Invoke();apparatusView.Sync();
                float lowFlame=apparatusView.Flame.localScale.y,lowRate=a.HeatRate;
                powerSlider.value=1;apparatusView.Sync();
                Check("burner dialog changes flame geometry and heating rate",a.GasFlow==1&&a.Lit&&apparatusView.Flame.localScale.y>lowFlame&&a.HeatRate>lowRate);
                // Hidden windows can expose a black backbuffer; render the lab
                // canvas explicitly rather than treating a black PNG as visual QA.
                lab.RenderToFile(Screen.width,Screen.height,Path.GetFullPath("Logs/burner-power-ui.png"));yield return null;
                powerSlider.value=0;apparatusView.Sync();ignition.onClick.Invoke();
                Check("zero burner power extinguishes and cannot ignite",!a.Lit&&!apparatusView.Flame.gameObject.activeSelf&&a.HeatRate==0);
                powerSlider.value=.5f;
                Check("raising power does not automatically reignite burner",!a.Lit);
                ignition.onClick.Invoke();
                GameObject.Find("BurnerClose").GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
                Check("burner dialog close preserves selected power and lit state",!dosingUi.BurnerDialogOpen&&a.GasFlow==.5&&a.Lit);
            }
            PetMenu.Close();
            // 本生灯筒身 / 三脚架的圆环。
            var p = center + (kind == ApparatusKind.Tripod ? new Vector3(ApparatusDimensions.TripodRadius-.05f, ApparatusDimensions.TripodHeight+.003f, 0) : Vector3.up * .7f);
            Check("pick apparatus " + kind, LabPick.Pick(cam.WorldToScreenPoint(p), cam).apparatus == a);
            a.Pos += Vector3.right * 0.4f;
            yield return null; yield return null;
            Check("moved apparatus remains pickable " + kind, LabPick.Pick(cam.WorldToScreenPoint(p + Vector3.right * 0.4f), cam).apparatus == a);
            ApparatusSet.Clear();
            yield return null; yield return null;
        }
        foreach (TestItemKind kind in System.Enum.GetValues(typeof(TestItemKind))) {
            var t = TestSet.Add(kind, center);
            yield return null; yield return null;
            lab.ShowObjectMenu(new LabPick.Hit { testItem = t }, pointer);
            Check("test item menu " + kind, PetMenu.IsOpen);
            PetMenu.Close();
            var tv = System.Array.Find(FindObjectsOfType<TestItemView>(), x => x.Model == t);
            Physics.SyncTransforms();
            var p = tv.GetComponent<Collider>().bounds.center;
            var itemHit = LabPick.Pick(cam.WorldToScreenPoint(p), cam);
            Check("pick test item " + kind + " hit=" + itemHit.Label, itemHit.testItem == t);
            TestSet.Clear();
            yield return null; yield return null;
        }
        var burner = ApparatusSet.Add(ApparatusKind.BunsenBurner, Vector3.zero);
        var splint = TestSet.Add(TestItemKind.Splint, Vector3.right * 2);
        yield return null; yield return null;
        var splintView = System.Array.Find(FindObjectsOfType<TestItemView>(), x => x.Model == splint);
        lab.ShowObjectMenu(new LabPick.Hit { testItem = splint }, pointer);
        UnityEngine.UI.Button FindSplintMenuButton(string label) {
            if (PetMenu.CurrentPanel == null) return null;
            foreach (var button in PetMenu.CurrentPanel.GetComponentsInChildren<UnityEngine.UI.Button>())
                if (button.GetComponentInChildren<UnityEngine.UI.Text>()?.text == label) return button;
            return null;
        }
        var lightButton = FindSplintMenuButton("点燃木条");
        Check("splint context menu offers light action", lightButton != null);
        lightButton?.onClick.Invoke();
        yield return null; yield return null;
        Check("context light turns on splint flame effect", splint.Splint == SplintState.Burning &&
              splintView != null && splintView.Flame != null && splintView.Flame.gameObject.activeInHierarchy);
        if (splintView != null) CaptureModel(splintView.transform, .8f, "splint-lit");
        lab.ShowObjectMenu(new LabPick.Hit { testItem = splint }, pointer);
        var extinguishButton = FindSplintMenuButton("熄灭木条（保留余烬20秒）");
        Check("burning splint context menu offers extinguish action", extinguishButton != null);
        extinguishButton?.onClick.Invoke();
        yield return null; yield return null;
        Check("context extinguish shows ember, not flame", splint.Splint == SplintState.JustOut &&
              splintView != null && splintView.EmberVisible && !splintView.Flame.gameObject.activeSelf);
        splint.Splint = SplintState.Normal;
        burner.Lit = true;
        Check("splint snaps when dragged near burner", SplintSnap.NearestBurner(Vector3.right * .3f) == burner &&
              SplintSnap.Attach(splint, burner));
        yield return null; yield return null;
        Check("snapped splint tip meets flame and lights", splint.AttachedBurner == burner &&
              splint.Splint == SplintState.Burning && splintView.Flame.gameObject.activeInHierarchy &&
              Vector3.Distance(LabContacts.Tip(splint),burner.Pos+Vector3.up*(ApparatusDimensions.BurnerHeight+SplintSnap.TipAboveNozzle))<.001f + SplintSnap.OffsetX);
        burner.Pos += Vector3.right * .4f;
        yield return null; yield return null;
        Check("attached splint follows moving burner", Mathf.Abs(splint.Pos.x-burner.Pos.x-SplintSnap.OffsetX)<.001f);
        burner.Holds = new Vessel(VesselKind.Beaker,burner.Pos);
        Check("splint can share burner with a vessel", SplintSnap.NearestBurner(burner.Pos+Vector3.right*.2f)==burner);
        burner.Holds = null;
        SplintSnap.MoveAside(splint);
        Check("detaching splint clears burner attachment", splint.AttachedBurner == null && splint.LiftY == 0);
        SplintSnap.Attach(splint,burner);
        lab.ShowObjectMenu(new LabPick.Hit { testItem = splint }, pointer);
        FindSplintMenuButton("熄灭木条（保留余烬20秒）")?.onClick.Invoke();
        yield return null; yield return null;
        Check("context extinguish detaches from live burner without immediate relight",
              splint.AttachedBurner==null && splint.Splint==SplintState.JustOut &&
              splintView.EmberVisible && !splintView.Flame.gameObject.activeSelf);
        burner.Pos = Vector3.zero;
        splint.Pos = Vector3.right * 2;splint.LiftY=0;
        var hydrogen = Lab.AddVessel(VesselKind.Beaker); hydrogen.Pos = splint.Pos;
        hydrogen.Gases["h2"] = 0.01;
        LabContacts.Tick();
        Check("hydrogen contact queues sharp pop once", splint.PendingSoundKind == "pop" && splint.PendingSound > 0 && !hydrogen.Gases.ContainsKey("h2"));
        yield return null; yield return null;
        Check("player consumes sound request", splint.PendingSound == 0);
        splint.Pos = Vector3.right * 4;
        GasTest.Light(splint); GasTest.Extinguish(splint);
        Check("extinguished ember window is 20 seconds", Mathf.Approximately(splint.GlowLeft, 20));
        TestSet.Tick(19f);
        var oxygen = Lab.AddVessel(VesselKind.Beaker); oxygen.Pos = splint.Pos; oxygen.Gases["o2"] = 0.01;
        LabContacts.Tick();
        Check("oxygen relights at 19 seconds", splint.Splint == SplintState.Burning);
        splint.Pos = Vector3.right * 6;
        GasTest.Extinguish(splint); TestSet.Tick(20.1f);
        oxygen.Pos = splint.Pos;
        LabContacts.Tick();
        Check("oxygen cannot relight after 20 seconds", splint.Splint == SplintState.Normal);
        var charSplint=TestSet.Add(TestItemKind.Splint,Vector3.right*20);
        lab.SyncViews();
        var charView=System.Array.Find(FindObjectsOfType<TestItemView>(),x=>x.Model==charSplint);
        Check("splint has dedicated supported wood material",charView.Wood.sharedMaterial.shader.name=="ChemLab/Wood"&&charView.Wood.sharedMaterial.shader.isSupported);
        CaptureModel(charView.transform,.8f,"wood-fresh");
        GasTest.Light(charSplint);TestSet.Tick(1.5f);charView.Sync();
        Check("burning wood progressively chars its own material",charSplint.CharLevel>.45f&&charSplint.CharLevel<.55f&&
              Mathf.Abs(charView.Wood.sharedMaterial.GetFloat("_Char")-charSplint.CharLevel)<.001f);
        TestSet.Tick(1.5f);charView.Sync();CaptureModel(charView.transform,.8f,"wood-charred-burning");
        GasTest.Extinguish(charSplint);TestSet.Tick(19.5f);charView.Sync();
        Check("charcoal remains unchanged through 19.5 seconds after extinction",charSplint.CharLevel==1&&charSplint.GlowLeft>.49f);
        CaptureModel(charView.transform,.8f,"wood-charred-out");
        TestSet.Tick(.75f);charView.Sync();
        Check("charcoal fades gradually only after 20 seconds",charSplint.CharLevel>.9f&&charSplint.CharLevel<1&&charSplint.Splint==SplintState.Normal);
        GasTest.Light(charSplint);TestSet.Tick(.5f);charView.Sync();
        Check("relighting restores char progression without resetting it",charSplint.CharLevel==1&&charSplint.CharHoldLeft==0);
        GasTest.Extinguish(charSplint);TestSet.Tick(23f);charView.Sync();
        Check("charcoal fully resets after hold plus fade interval",charSplint.CharLevel==0&&charView.Wood.sharedMaterial.GetFloat("_Char")==0);
        CaptureModel(charView.transform,.8f,"wood-reset");
        GasTest.Light(charSplint);TestSet.Tick(3f);
        // Use an actual liquid for the quench branch; oxygen itself is dry.
        oxygen.Water=10;GasTest.DipLiquid(oxygen,charSplint);TestSet.Tick(19f);
        Check("quenching removes ember but preserves charcoal hold",charSplint.Splint==SplintState.Normal&&charSplint.GlowLeft==0&&charSplint.CharLevel==1);
        oxygen.Water=0;TestSet.Remove(charSplint);lab.SyncViews();
        var solution = Lab.AddVessel(VesselKind.Beaker); solution.Pos = Vector3.right * 8;
        solution.AddWater(100); solution.Solutes["OH-"] = 0.001;
        var red = TestSet.Add(TestItemKind.RedLitmus, solution.Pos);
        LabContacts.Tick();
        Check("dry red paper dips in alkali and turns blue", red.Moist && red.Tint == "#3a5ad8");
        solution.Solutes.Clear(); solution.Solutes["H+"] = 0.001;
        yield return null; yield return null;
        Check("paper responds to changing liquid pH while immersed", red.Tint == "#d83a3a");
        var blue = TestSet.Add(TestItemKind.BlueLitmus, solution.Pos);
        LabContacts.Tick();
        Check("blue paper turns red in acid", blue.Tint == "#d83a3a");
        blue.LiftY = 2; blue.Tint = "";
        LabContacts.Tick();
        Check("no reaction outside vessel", blue.Tint == "");
        foreach (var kind in new[] { VesselKind.Flask, VesselKind.RoundBottom }) {
            var g = VesselMesh.Get(kind); var p = VesselMesh.Profile(g.Type, g.R, g.H);
            bool ascending = true;
            for (int i = 1; i < p.Count; i++) ascending &= p[i].y >= p[i-1].y;
            Check("profile never folds downward " + kind, ascending && Mathf.Abs(p[p.Count-1].y-g.H) < 0.001f);
            Check("shoulder narrows towards neck " + kind, VesselMesh.RadiusAt(p, 0.45f) > VesselMesh.RadiusAt(p, 0.9f));
        }
        var solidTest=Lab.AddVessel(VesselKind.Beaker);
        solidTest.Undissolved["starch"]=0.002;
        var sm=SolidGeometry.Build(solidTest);
        var sv=sm.vertices;var st=sm.triangles;double volume=0;
        for(int i=0;i<st.Length;i+=3) volume+=Vector3.Dot(sv[st[i]],Vector3.Cross(sv[st[i+1]],sv[st[i+2]]))/6.0;
        double expectedVolume=SolidGeometry.VolumeMl(solidTest)/1000;
        Check("solid closed mesh volume matches mass/density", expectedVolume>0 && System.Math.Abs(System.Math.Abs(volume)-expectedVolume)<expectedVolume*0.001);
        Destroy(sm);
        var powderFixture=Lab.AddVessel(VesselKind.Flask);powderFixture.Pos=Vector3.right*25;
        foreach(double fraction in new[]{.7,1.5}){
            double ml=VesselMesh.InnerCapacity(powderFixture.Kind)*1000*fraction;
            powderFixture.Undissolved["starch"]=ml*PhaseProperties.Density("starch",25)/ChemData.Sub["starch"].Mass;
            lab.SyncViews();
            var powderView=System.Array.Find(FindObjectsOfType<VesselView>(),x=>x.Model==powderFixture);
            powderView.Refresh();
            var powderMesh=powderView.transform.Find("Sediment").GetComponent<MeshFilter>().sharedMesh;
            Check("live powder does not form sky column at fill "+fraction,powderMesh.bounds.max.y<=VesselMesh.Get(powderFixture.Kind).H+.00001f);
            CaptureModel(powderView.transform,fraction>1?3:VesselMesh.Get(powderFixture.Kind).H,"powder-bounded-"+fraction);
            if(fraction<1){
                powderFixture.Undissolved["cu"]=.01;powderView.Refresh();
                var mixedMesh=powderView.transform.Find("Sediment").GetComponent<MeshFilter>().sharedMesh;
                var positions=mixedMesh.vertices;var traits=mixedMesh.uv;float bedTop=0,grainBottom=float.PositiveInfinity;bool above=true;int grains=0;
                for(int i=0;i<positions.Length;i++)if(traits[i].y<.1f)bedTop=Mathf.Max(bedTop,positions[i].y);
                for(int i=0;i<positions.Length;i++)if(traits[i].y>.1f){grains++;above&=positions[i].y>=bedTop-.00001f;grainBottom=Mathf.Min(grainBottom,positions[i].y);}
                Check("live coarse grains do not intersect powder bed",grains>0&&above);
                Check("live coarse grain rests on powder without hovering",Mathf.Abs(grainBottom-bedTop)<.00001f);
                CaptureVesselModel(powderView,"mixed-powder-grains");
                powderFixture.Undissolved.Remove("cu");
            }
        }
        Lab.RemoveVessel(powderFixture);lab.SyncViews();
        // A temperature edit can arrive before the next chemistry reconciliation.
        // The displayed phase must follow it even while the inventory map is unchanged.
        var thermalSolid=Lab.AddVessel(VesselKind.Beaker);thermalSolid.Temp=10;
        thermalSolid.Undissolved["octanoic_acid"]=.002;
        lab.SyncViews();
        var thermalView=System.Array.Find(FindObjectsOfType<VesselView>(),x=>x.Model==thermalSolid);
        var thermalSediment=thermalView.transform.Find("Sediment");
        var coldMesh=thermalSediment.GetComponent<MeshFilter>().sharedMesh;
        Check("cold inventory displays a solid mesh",thermalSediment.gameObject.activeSelf&&coldMesh.vertexCount>0);
        thermalSolid.Temp=11;thermalView.Refresh();
        Check("temperature edit within unchanged solid phase reuses mesh",thermalSediment.GetComponent<MeshFilter>().sharedMesh==coldMesh);
        thermalSolid.Temp=25;thermalView.Refresh();
        Check("melting hides cached solid before inventory reconciliation",!thermalSediment.gameObject.activeSelf&&
              thermalView.Fluid.MassVolumeMl>0&&System.Math.Abs(thermalView.Fluid.MassVolumeMl-thermalSolid.Volume())<1e-9);
        thermalSolid.Temp=10;thermalView.Refresh();
        Check("cooling restores solid and removes melted fluid",thermalSediment.gameObject.activeSelf&&thermalView.Fluid.MassVolumeMl==0);
        Check("thermal display updates preserve inventory",thermalSolid.Undissolved.Count==1&&thermalSolid.Undissolved["octanoic_acid"]==.002&&thermalSolid.Solutes.Count==0);
        Lab.RemoveVessel(thermalSolid);lab.SyncViews();
        var hotEthanol=new Vessel(VesselKind.Beaker,Vector3.zero){Temp=75};hotEthanol.Solutes["c2h5oh"]=10/ChemData.Sub["c2h5oh"].Mass;
        var coldEthanol=new Vessel(VesselKind.Beaker,Vector3.right){Temp=25};coldEthanol.Solutes["c2h5oh"]=10/ChemData.Sub["c2h5oh"].Mass;
        EthanolThermodynamics.TryLiquidEnthalpy(75,out double ethanolHotH);
        using(var ethanolPour=new PourTransaction(hotEthanol,coldEthanol)){
            Check("runtime hot ethanol pour accepts sourced enthalpy mixing",ethanolPour.Preview(.5,0,out _)&&coldEthanol.Temp>41.8f);
            ethanolPour.Commit();
        }
        EthanolThermodynamics.TryLiquidEnthalpy(coldEthanol.Temp,out double ethanolMixedH);
        Check("runtime ethanol commit conserves mass and sensible energy",System.Math.Abs(hotEthanol.Mass()+coldEthanol.Mass()-20)<1e-8&&System.Math.Abs(15*ethanolMixedH-5*ethanolHotH)<.001);
        foreach(string pureId in new[]{"c2h5oh","ch3oh"})foreach(bool ethanolReflux in new[]{false,true}){
            var es=Lab.AddVessel(VesselKind.RoundBottom);es.Water=0;es.Temp=(float)(PureLiquidThermalModel.For(pureId).BoilingC-.01);es.Solutes[pureId]=10/ChemData.Sub[pureId].Mass;
            var er=Lab.AddVessel(VesselKind.Flask);er.Water=0;er.Temp=25;
            // A previously hot vessel can boil a lower-boiling liquid with
            // no burner input; wall and phase heat must reach the condenser.
            es.GlassTemperatureC=100;
            var energyRig=new DistillationAssembly(es){Cooling=true,HeaterPowerWatts=0};energyRig.Configure(ethanolReflux,er,out _);energyRig.Start(out _);
            Lab.Tick(.2f);
            Check("runtime pure liquid energy condenser transfers "+pureId+" "+ethanolReflux,energyRig.HasThermalResult&&(ethanolReflux?energyRig.ReturnedMl>0:energyRig.CollectedMl>0)&&System.Math.Abs(es.Mass()+er.Mass()+energyRig.EscapedGrams-10)<1e-7);
            Check("runtime hot glass supplies latent heat with one condenser interval "+pureId+" "+ethanolReflux,
                energyRig.HeaterEnergyJ==0&&energyRig.VesselStoredEnergyJ<0&&es.GlassTemperatureC<100&&System.Math.Abs(energyRig.SimulatedCondenserSeconds-.2)<1e-7);
            double savedHeat=energyRig.HeaterEnergyJ,savedMass=es.Mass();
            using(var pausedPour=new PourTransaction(es,er)){Lab.Tick(.2f);energyRig.Tick(.2);}
            Check("runtime pure liquid condenser pauses "+pureId+" "+ethanolReflux,energyRig.HeaterEnergyJ==savedHeat&&es.Mass()==savedMass);
            Lab.RemoveVessel(es);Lab.RemoveVessel(er);
        }
        var iceFixture=Lab.AddVessel(VesselKind.Beaker);iceFixture.Pos=Vector3.right*27;iceFixture.Temp=-5;
        iceFixture.AddWater(20);lab.SyncViews();
        var iceView=System.Array.Find(FindObjectsOfType<VesselView>(),x=>x.Model==iceFixture);
        iceView.Refresh();
        var iceRenderer=iceView.transform.Find("Sediment").GetComponent<MeshRenderer>();
        var iceMesh=iceView.transform.Find("Sediment").GetComponent<MeshFilter>().sharedMesh;
        Check("ice uses separate supported refractive material",iceRenderer.sharedMaterials.Length==2&&
            iceRenderer.sharedMaterials[1].shader.name=="ChemLab/Ice"&&iceRenderer.sharedMaterials[1].shader.isSupported&&
            iceMesh.subMeshCount==2&&iceMesh.GetIndexCount(0)==0&&iceMesh.GetIndexCount(1)>0);
        Check("frozen water renders expanded solid and no liquid",iceView.Fluid.MassVolumeMl==0&&
            iceView.transform.Find("Sediment").gameObject.activeSelf&&System.Math.Abs(iceFixture.SolidVolume()-20/.9167)<1e-8);
        CaptureVesselModel(iceView,"water-frozen");
        AuditIceOptics(iceView);
        WaterIce.AdvanceTemperature(iceFixture,5);PhaseProperties.Reconcile(iceFixture);iceView.Refresh();
        Check("heater crosses zero by partial melting rather than temperature jump",iceFixture.Temp==0&&iceFixture.Water>0&&iceFixture.Water<1&&
            WaterIce.IceMass(iceFixture)>19&&System.Math.Abs(iceFixture.Mass()-20)<1e-8);
        iceFixture.Temp=5;PhaseProperties.Reconcile(iceFixture);iceView.Refresh();
        Check("melted ice restores fluid without duplicate solid mass",System.Math.Abs(iceView.Fluid.MassVolumeMl-20)<1e-8&&
            !iceView.transform.Find("Sediment").gameObject.activeSelf&&System.Math.Abs(iceFixture.Mass()-20)<1e-8);
        CaptureVesselModel(iceView,"water-melted");
        iceFixture.Temp=-5;WaterIce.Reconcile(iceFixture);
        var iceReceiver=Lab.AddVessel(VesselKind.Beaker);iceReceiver.Water=5;iceReceiver.Temp=20;iceReceiver.Pos=Vector3.right*31;
        double iceMixEnergy=WaterIce.Enthalpy(iceFixture)+WaterIce.Enthalpy(iceReceiver);
        using(var icePour=new PourTransaction(iceFixture,iceReceiver)){
            Check("runtime solid ice pour keeps partial melt at zero",icePour.Preview(0,.5,out _)&&iceReceiver.Temp==0&&iceReceiver.Water>5&&WaterIce.IceMass(iceReceiver)>0);
            icePour.Commit();
        }
        Check("runtime ice pour commit conserves latent energy",System.Math.Abs(WaterIce.Enthalpy(iceFixture)+WaterIce.Enthalpy(iceReceiver)-iceMixEnergy)<.001);
        lab.SyncViews();
        var iceMixView=System.Array.Find(FindObjectsOfType<VesselView>(),x=>x.Model==iceReceiver);iceMixView.Refresh();
        Check("runtime partial melt displays both liquid and solid",iceMixView.Fluid.MassVolumeMl>0&&iceMixView.transform.Find("Sediment").gameObject.activeSelf);
        CaptureVesselModel(iceMixView,"ice-water-transfer");Lab.RemoveVessel(iceReceiver);
        Lab.RemoveVessel(iceFixture);lab.SyncViews();
        var stillSource=Lab.AddVessel(VesselKind.RoundBottom);stillSource.Water=20;stillSource.Temp=100;
        stillSource.GlassTemperatureC=100;
        var stillReceiver=Lab.AddVessel(VesselKind.Flask);
        var still=new DistillationAssembly(stillSource){Cooling=true};
        bool connected=still.Configure(false,stillReceiver,out _)&&still.Start(out _);
        Lab.Tick(.2f);
        Check("live laboratory tick transfers distillate without evaporation duplication",connected&&stillReceiver.Water>0&&
            System.Math.Abs(stillSource.Water+stillReceiver.Water-20)<1e-8);
        Check("live water condenser accounts for heater and coolant energy",still.HasThermalResult&&still.HeaterEnergyJ>0&&still.CoolantEnergyJ>0&&still.LastThermalResult.CoolantOutletC>still.CoolantCelsius);
        stillSource.Temp=40;stillSource.Water+=1;double editedWater=stillSource.Water;
        double editedEnergy=editedWater*4.18*40+stillReceiver.Water*4.18*stillReceiver.Temp+still.VesselStoredEnergyJ+still.VesselAmbientEnergyJ;
        still.Tick(.01);
        double afterEditedEnergy=stillSource.Water*4.18*stillSource.Temp+stillReceiver.Water*4.18*stillReceiver.Temp+still.VesselStoredEnergyJ+still.VesselAmbientEnergyJ;
        Check("live condenser respects edited inventory and accounts for hot glass",still.UseVesselThermalStorage&&stillSource.Temp>40&&stillSource.Water==editedWater&&System.Math.Abs(afterEditedEnergy-editedEnergy-2.5)<.002);
        Lab.RemoveVessel(stillReceiver);
        Check("live removal disconnects condenser receiver",stillSource.Distillation==null&&!still.Running);
        Lab.RemoveVessel(stillSource);lab.SyncViews();
        var kitButton=System.Array.Find(FindObjectsOfType<UnityEngine.UI.Button>(true),b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="蒸馏套件 250 mL");
        Check("instrument shelf exposes distillation kit",kitButton!=null);
        kitButton.onClick.Invoke();lab.SyncViews();
        var kitSource=Lab.Vessels.Find(v=>v.Distillation!=null);var kit=kitSource.Distillation;var kitReceiver=kit.Receiver;
        var kitView=System.Array.Find(FindObjectsOfType<VesselView>(),x=>x.Model==kitSource);
        Check("kit creates glassware condenser and connected receiver",kitView.GetComponentInChildren<DistillationView>()!=null&&kitReceiver!=kitSource&&kitView.GetComponentsInChildren<Collider>().Length>8);
        Check("distillation full assembly fits unobstructed UI workspace",AssemblyFitsWorkspace(kitSource,dosingUi));
        lab.ShowObjectMenu(new LabPick.Hit{vessel=kitSource},pointer);
        var coolingButton=System.Array.Find(PetMenu.CurrentPanel.GetComponentsInChildren<UnityEngine.UI.Button>(),b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("打开冷凝水"));
        coolingButton.onClick.Invoke();
        Check("condenser context action enables water and closes menu",kit.Cooling&&!PetMenu.IsOpen);
        Lab.SetHeat(kitSource,true);
        Check("legacy heat action retains configured distillation mode",kit.Running&&kitSource.Mode==HeatMode.Distil);
        var blockedReceiver=Lab.AddVessel(VesselKind.Flask);blockedReceiver.Sealed=true;
        DistillationView.Install(kitSource,false,blockedReceiver);
        Check("failed UI replacement preserves running apparatus and receiver",kitSource.Distillation==kit&&kit.Running&&kit.Receiver==kitReceiver&&kitReceiver.ReceivingDistillation==kit);
        Lab.RemoveVessel(blockedReceiver);
        kitSource.Water=20;kitSource.Temp=100;kitSource.GlassTemperatureC=100;kit.Tick(.2);lab.SyncViews();
        var liveCondenser=kitView.GetComponentInChildren<DistillationView>();
        AuditCoolantGeometry(liveCondenser,"distillation");
        Check("actual distillate transfer shows animated condenser film",liveCondenser.CondensateVisible&&liveCondenser.CondensatePhase>0&&kitReceiver.Water>0);
        var filmObject=liveCondenser.transform.Find("Connected glassware/Condensate wall film");
        Check("condensate film stays inside glass bore and does not intercept picking",filmObject.localScale.x<.055f*.82f&&filmObject.GetComponent<Collider>()==null&&filmObject.GetComponent<Renderer>().sharedMaterial.shader.isSupported);
        CaptureModel(kitView.transform,5.8f,"distillation-condensing",System.Array.Find(FindObjectsOfType<VesselView>(),x=>x.Model==kitReceiver).transform);
        yield return AuditCondenserPause(kit,liveCondenser,"distillation");
        Lab.SetHeat(kitSource,false);
        lab.SyncViews();
        Check("stopped condenser does not show fictitious continuing transfer",!liveCondenser.CondensateVisible);
        Check("legacy stop action stops connected condenser",!kit.Running&&kitSource.Mode==HeatMode.None);
        Vector3 receiverBefore=kitReceiver.Pos;kitSource.Pos+=Vector3.right*.25f;lab.SyncViews();
        Check("dragging source carries connected receiver horizontally",Vector3.Distance(kitReceiver.Pos,receiverBefore+Vector3.right*.25f)<1e-5f);
        lab.SyncViews();
        bool tightPick=true;
        foreach(var collider in kitView.GetComponentInChildren<DistillationView>().GetComponentsInChildren<Collider>()){
            var mesh=collider.GetComponent<MeshFilter>().sharedMesh;
            tightPick&=collider is MeshCollider mc&&mc.sharedMesh==mesh;
        }
        Check("condenser picking uses exact visible hollow tube meshes",tightPick);
        var vaporTube=kitView.GetComponentInChildren<DistillationView>().transform.Find("Connected glassware/Liebig inner vapor tube");
        Check("distillation condenser drains downhill to taller receiving flask",vaporTube.TransformPoint(Vector3.up).y<vaporTube.position.y-.59f);
        var receiverView=System.Array.Find(FindObjectsOfType<VesselView>(),x=>x.Model==kitReceiver);
        CaptureModel(kitView.transform,5.8f,"distillation-kit",receiverView.transform);
        kit.Detach();lab.SyncViews();
        Check("detaching deactivates 3D condenser immediately",kitView.GetComponentInChildren<DistillationView>()==null);
        Lab.RemoveVessel(kitReceiver);Lab.RemoveVessel(kitSource);lab.SyncViews();
        var refluxButton=System.Array.Find(FindObjectsOfType<UnityEngine.UI.Button>(true),b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="回流套件 250 mL");
        Check("instrument shelf exposes reflux kit",refluxButton!=null);
        refluxButton.onClick.Invoke();lab.SyncViews();
        var refluxSource=Lab.Vessels.Find(v=>v.Distillation!=null);var refluxRig=refluxSource.Distillation;
        var refluxView=System.Array.Find(FindObjectsOfType<VesselView>(),x=>x.Model==refluxSource);
        Check("reflux kit returns to source without creating receiver",refluxRig.Reflux&&refluxRig.Receiver==refluxSource&&refluxView.GetComponentInChildren<DistillationView>()!=null);
        Check("reflux full assembly fits unobstructed UI workspace",AssemblyFitsWorkspace(refluxSource,dosingUi));
        refluxRig.Cooling=true;refluxSource.Water=20;refluxSource.Temp=100;Lab.SetHeat(refluxSource,true);
        refluxSource.GlassTemperatureC=100;
        refluxRig.Tick(.2);
        Check("shelf reflux kit circulates without losing water",refluxRig.Running&&refluxSource.Mode==HeatMode.Reflux&&refluxRig.ReturnedMl>0&&refluxSource.Water==20);
        lab.SyncViews();
        var refluxFilm=refluxView.GetComponentInChildren<DistillationView>();
        AuditCoolantGeometry(refluxFilm,"reflux");
        var returningFilm=refluxFilm.transform.Find("Connected glassware/Condensate wall film");
        Check("reflux film visibly returns down the condenser",refluxFilm.CondensateVisible&&returningFilm.TransformPoint(Vector3.up).y<returningFilm.position.y);
        CaptureModel(refluxView.transform,6,"reflux-condensing");
        yield return AuditCondenserPause(refluxRig,refluxFilm,"reflux");
        var refluxFoot=refluxView.GetComponentInChildren<DistillationView>().transform.Find("Connected glassware/Retort foot");
        var hoseHeights=new Dictionary<string,float>();
        foreach(string hoseName in new[]{"Inlet hose","Outlet hose"})
            hoseHeights[hoseName]=refluxView.GetComponentInChildren<DistillationView>().transform.Find("Connected glassware/"+hoseName).TransformPoint(Vector3.up).y;
        float footHeight=refluxFoot.position.y;refluxSource.LiftY=.5f;lab.SyncViews();
        refluxFoot=refluxView.GetComponentInChildren<DistillationView>().transform.Find("Connected glassware/Retort foot");
        Check("reflux stand remains on table after source elevation",Mathf.Abs(refluxFoot.position.y-footHeight)<1e-5f);
        foreach(string hoseName in new[]{"Inlet hose","Outlet hose"}){
            var hose=refluxView.GetComponentInChildren<DistillationView>().transform.Find("Connected glassware/"+hoseName);
            Check(hoseName+" bench endpoint remains grounded after source elevation",Mathf.Abs(hose.TransformPoint(Vector3.up).y-hoseHeights[hoseName])<1e-5f);
        }
        refluxSource.LiftY=0;
        lab.SyncViews();CaptureModel(refluxView.transform,6,"reflux-kit");
        Lab.RemoveVessel(refluxSource);lab.SyncViews();
        var meltedSolvent=new Vessel(VesselKind.Beaker,Vector3.zero);
        meltedSolvent.Precipitates["c2h5oh"]=20/ChemData.Sub["c2h5oh"].Mass;
        double saltCapacity=20*.00034/(1-.00034)/ChemData.Sub["kcl"].Mass;
        Check("melted solvent deposit supports actual dose service",DoseService.TryAdd(meltedSolvent,"kcl",saltCapacity*2,true,out _));
        Check("dose into melted solvent uses ethanol saturation and conserves mass",
            Mathf.Abs((float)(PhaseProperties.Dissolved(meltedSolvent,"kcl")-saltCapacity))<1e-8&&
            meltedSolvent.Precipitates.TryGetValue("kcl",out double remainingSalt)&&System.Math.Abs(remainingSalt-saltCapacity)<1e-8&&
            System.Math.Abs(meltedSolvent.Mass()-20-saltCapacity*2*ChemData.Sub["kcl"].Mass)<1e-8);
        foreach(VesselKind kind in System.Enum.GetValues(typeof(VesselKind))) {
            float a=VesselMesh.FillFractionFor(kind,1),b=VesselMesh.FillFractionFor(kind,2);
            Check("volume-height monotonic "+kind,a>0 && b>a);
        }
        var fluid=new Fluid3D();fluid.Configure(VesselKind.Beaker,100);
        fluid.Impulse(new Vector3(0.3f,0.1f,0));
        for(int i=0;i<25;i++)fluid.Step(Vector3.zero,Quaternion.identity,1f/90,true,0);
        Check("3D particle simulation responds to stirring",fluid.Steps>0&&fluid.KineticEnergy>0.000001f);
        Check("3D fluid conserves particle mass",Mathf.Abs(fluid.MassVolumeMl-100)<0.001f);
        foreach(string shader in new[]{"Solid","Liquid","Glass","Particle","Flame","Bench"}) {
            var sh=Shader.Find("ChemLab/"+shader);Check("render shader supported "+shader,sh!=null&&sh.isSupported);
        }
        // Exercise the actual GPU solver in every cavity, including tapered necks.
        foreach(VesselKind shape in System.Enum.GetValues(typeof(VesselKind))){
            var testObject=new GameObject("GpuBoundaryAudit");
            var filter=testObject.AddComponent<MeshFilter>();
            var mat=new Material(Shader.Find("ChemLab/FluidVolume"));
            var solver=new GpuFluid();var reference=new Fluid3D();
            double amount=System.Math.Min(20,VesselMesh.InnerCapacity(shape)*1000*0.6);
            reference.Configure(shape,amount);solver.Configure(shape,amount,filter,mat);
            int initialField=solver.FieldBuildCount;
            solver.Step(Vector3.zero,Quaternion.identity,0,false,0);
            Check("zero-time fluid update reuses density field "+shape,solver.FieldBuildCount==initialField&&solver.DispatchCount==0);
            var comparisonObject=new GameObject("AllPairsReference");var comparisonFilter=comparisonObject.AddComponent<MeshFilter>();
            var comparisonMaterial=new Material(Shader.Find("ChemLab/FluidVolume"));var comparison=new GpuFluid{AllPairsReferenceForTest=true};
            comparison.Configure(shape,amount,comparisonFilter,comparisonMaterial);comparison.Step(Vector3.zero,Quaternion.identity,0,false,0);
            float maxDifference=0;
            for(int iteration=0;iteration<3;iteration++){
                var position=new Vector3(iteration*.015f,0,iteration*.008f);var solverRotation=Quaternion.Euler(0,iteration*3,iteration*4);
                solver.Step(position,solverRotation,1f/90,true,1);comparison.Step(position,solverRotation,1f/90,true,1);
                var indexed=solver.ReadbackForTest();var exhaustive=comparison.ReadbackForTest();
                for(int i=0;i<indexed.Length;i++)maxDifference=Mathf.Max(maxDifference,Vector3.Distance(indexed[i],exhaustive[i]));
                yield return null;
            }
            ulong visits=solver.NeighborVisitsForTest(),allVisits=comparison.NeighborVisitsForTest();
            Check("GPU cell lookup agrees with exhaustive solver "+shape,maxDifference<.0002f);
            Check("GPU cell lookup reduces candidate comparisons "+shape,visits<allVisits*.9&&allVisits==(ulong)solver.Count*(ulong)solver.Count);
            lines.Add("METRIC neighbor query "+shape+" indexed="+visits+" exhaustive="+allVisits+" maxPositionDifference="+maxDifference);
            comparison.Dispose();Destroy(comparisonFilter.sharedMesh);Destroy(comparisonMaterial);Destroy(comparisonObject);
            bool finite=true,inside=true;float worst=0;
            for(int step=0;step<60;step++){
                var position=new Vector3(Mathf.Sin(step*0.3f)*0.4f,0,Mathf.Cos(step*0.2f)*0.4f);
                solver.Step(position,Quaternion.Euler(0,step*3,step<30?0:35),1f/60,true,2);
                if(step%10==9){
                    yield return null;
                    foreach(var particle in solver.ReadbackForTest()){
                        finite&=!float.IsNaN(particle.x)&&!float.IsNaN(particle.y)&&!float.IsNaN(particle.z)&&!float.IsInfinity(particle.sqrMagnitude);
                        float outside=-reference.BoundaryDistance(particle);
                        worst=Mathf.Max(worst,outside);inside&=outside<0.0005f;
                    }
                }
            }
            Check("GPU finite under movement/stir/tilt "+shape,finite);
            Check("GPU particles stay inside cavity "+shape,inside);
            Check("GPU volume conserved under movement "+shape,System.Math.Abs(solver.MassVolumeMl-amount)<1e-6);
            AuditFluidSurface(testObject,mat,reference,shape);
            lines.Add("METRIC GPU boundary "+shape+" maxOutsideWorldUnits="+worst);
            solver.Dispose();Destroy(filter.sharedMesh);Destroy(mat);Destroy(testObject);
        }
        // Real precipitation and water reactions must create visible equation
        // cards; after the 24-event history wraps, the newest event still wins.
        var reactionTagCup=Lab.AddVessel(VesselKind.Beaker);
        reactionTagCup.Pos=Vector3.zero;reactionTagCup.Water=100;
        reactionTagCup.Solutes["Cu2+"]=.003;
        reactionTagCup.Solutes["OH-"]=.008;
        Lab.Settle(reactionTagCup);
        var precipEvent=Lab.RecentReactions.FindLast(e=>e.Vessel==reactionTagCup&&e.Rule!=null&&e.Rule.Precip=="Cu(OH)2");
        Check("precipitation publishes its balanced equation event",precipEvent!=null&&precipEvent.Rule.Parsed&&
              precipEvent.Rule.Reactants.Count==2&&precipEvent.Rule.Products.Count==1);
        lab.SyncViews();
        var precipTag=EquationTag.Active.Find(t=>t!=null&&t.Ev==precipEvent);
        Check("precipitation equation card is rendered",precipTag!=null&&precipTag.ScreenRect.width>0&&precipTag.ScreenRect.height>0);

        var waterTagCup=Lab.AddVessel(VesselKind.Beaker);
        waterTagCup.Pos=new Vector3(.7f,0,0);waterTagCup.Water=100;
        long beforeWaterTag=Lab.RecentReactions.Count>0?Lab.RecentReactions[Lab.RecentReactions.Count-1].Sequence:0;
        Lab.AddReagent(waterTagCup,"na",.02);
        var waterEvent=Lab.RecentReactions.FindLast(e=>e.Vessel==waterTagCup&&e.Sequence>beforeWaterTag&&
                       e.Rule!=null&&e.Rule.Eq.StartsWith("2Na + 2H2O"));
        lab.SyncViews();
        Check("water reaction publishes a rendered equation card",waterEvent!=null&&
              EquationTag.Active.Exists(t=>t!=null&&t.Ev==waterEvent&&t.ScreenRect.width>0));

        var wrappedRule=precipEvent!=null?precipEvent.Rule:Rules.All.Find(r=>r.Parsed);
        for(int eventIndex=0;eventIndex<28;eventIndex++)Lab.PushReaction(reactionTagCup,wrappedRule,.001);
        var newest=Lab.RecentReactions[Lab.RecentReactions.Count-1];
        lab.SyncViews();
        Check("equation cards keep updating after 24-event history wraps",Lab.RecentReactions.Count==24&&
              EquationTag.Active.Exists(t=>t!=null&&t.Ev==newest));
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/interaction-regression.txt", lines);
        // 独立测试进程中的固定构图，供实际 Player 渲染验收。
        // Remove prior fixtures, not just move them offscreen: hidden fluids still simulate.
        Lab.Reset();ApparatusSet.Clear();TestSet.Clear();
        yield return null;yield return null;
        var showKinds=new[]{VesselKind.Beaker,VesselKind.Flask,VesselKind.RoundBottom,VesselKind.Tube};
        for(int i=0;i<showKinds.Length;i++) {
            var item=Lab.AddVessel(showKinds[i]);item.Pos=new Vector3((i-1.5f)*1.15f,0,0);
            item.AddWater(i==3?25:120);item.Stirring=true;
            if(i==0)item.Solutes["Cu2+"]=0.0004;
            item.Undissolved[i==0?"cu":i==1?"starch":"caco3"]=0.03;
        }
        yield return null;yield return null;
        lab.enabled=false;
        cam.transform.position=new Vector3(0,2.8f,-6f);
        cam.transform.LookAt(new Vector3(0,0.4f,0));
        foreach(var tag in FindObjectsOfType<VesselTag>())tag.Sync();
        foreach(var item in Lab.Vessels) {item.Fx.Bubble=1.4f;item.Fx.Smoke=1;}
        foreach(var view in FindObjectsOfType<VesselView>()) view.Refresh(true);
        yield return new WaitForSeconds(0.5f);
        var frameTimes=new List<float>();
        var performanceViews=FindObjectsOfType<VesselView>();double maxSubmit=0;
        Check("four-vessel performance fixture has exactly four live views",performanceViews.Length==4&&Lab.Vessels.Count==4);
        int collections=System.GC.CollectionCount(0);int fieldBefore=0,fieldAfter=0;
        foreach(var view in performanceViews)fieldBefore+=view.Fluid.FieldBuildCount;
        const int performanceFrames=600;
        for(int frame=0;frame<performanceFrames;frame++){
            yield return null;float elapsed=Time.unscaledDeltaTime*1000;frameTimes.Add(elapsed);
            double submitCpuMs=0;foreach(var view in performanceViews)submitCpuMs+=view.Fluid.LastStepCpuMilliseconds;
            maxSubmit=System.Math.Max(maxSubmit,submitCpuMs);
            if(elapsed>50)lines.Add("METRIC slow frame="+frame+" wallMs="+elapsed+" fluidSubmitCpuMs="+submitCpuMs+" gc0="+System.GC.CollectionCount(0)+" focused="+Application.isFocused+" runInBackground="+Application.runInBackground);
        }
        foreach(var view in performanceViews)fieldAfter+=view.Fluid.FieldBuildCount;
        lines.Add("METRIC fluid submission maxCpuMs="+maxSubmit+" fieldRebuilds="+(fieldAfter-fieldBefore)+" gc0Collections="+(System.GC.CollectionCount(0)-collections));
        frameTimes.Sort();
        bool strictPerformance=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-strictPerformance")>=0;
        if(strictPerformance){
            Check("four-vessel 600-frame performance p95 below 33.4 ms",frameTimes[570]<33.4f);
            Check("four-vessel 600-frame maximum stall below 250 ms",frameTimes[599]<250);
        }else{
            lines.Add("PERF four-vessel p95Below33.4="+(frameTimes[570]<33.4f)+" maxBelow250="+(frameTimes[599]<250)+" (measured, not a functional gate)");
        }
        lines.Add("METRIC visible four-vessel frame time ms p50="+frameTimes[300]+" p95="+frameTimes[570]+" p99="+frameTimes[594]+" max="+frameTimes[599]+" samples="+performanceFrames+" resolution="+Screen.width+"x"+Screen.height+" GPU="+SystemInfo.graphicsDeviceName);
        lines.Add("TOTAL " + (checks - failures) + " passed, " + failures + " failed");
        File.WriteAllLines("Logs/interaction-regression.txt",lines);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/interaction-regression.png"));
        yield return new WaitForSeconds(0.5f);
        Application.Quit(failures == 0 ? 0 : 1);
    }

    void AuditParticleLid(){
        var root=GameObject.CreatePrimitive(PrimitiveType.Quad);root.name="ParticleLidAudit";root.layer=27;
        root.transform.position=new Vector3(30,0,30);
        var mesh=Instantiate(root.GetComponent<MeshFilter>().sharedMesh);
        var vertices=mesh.vertices;var colors=new Color[vertices.Length];
        for(int i=0;i<vertices.Length;i++){vertices[i]=vertices[i]*.2f+Vector3.up*1.2f;colors[i]=Color.white;}
        mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();root.GetComponent<MeshFilter>().sharedMesh=mesh;
        var material=new Material(Shader.Find("ChemLab/Particle"));material.SetFloat("_ClipContainer",1);material.SetFloat("_VesselHeight",1);material.SetFloat("_ParticleKind",1);
        root.GetComponent<MeshRenderer>().sharedMaterial=material;
        var cameraObject=new GameObject("ParticleLidCamera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
        camera.cullingMask=1<<27;camera.orthographic=true;camera.orthographicSize=.2f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        camera.transform.position=root.transform.position+new Vector3(0,1.2f,-2);camera.transform.LookAt(root.transform.position+Vector3.up*1.2f);
        var target=new RenderTexture(64,64,24);target.Create();camera.targetTexture=target;
        var texture=new Texture2D(64,64,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        int Visible(){camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,64,64),0,0);texture.Apply();int n=0;foreach(var pixel in texture.GetPixels())if(pixel.maxColorComponent>.01f)n++;return n;}
        material.SetFloat("_ClosedContainer",0);int open=Visible();
        material.SetFloat("_ClosedContainer",1);int closed=Visible();
        Check("open vessel permits fume above rim",open>20);
        Check("closed vessel clips actual fume fragments above lid",closed==0&&open>20);
        foreach(VesselKind kind in System.Enum.GetValues(typeof(VesselKind))){
            var geometry=VesselMesh.Get(kind);var profile=VesselMesh.Profile(geometry.Type,geometry.R,geometry.H);
            var texcoords=mesh.uv;
            for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3((texcoords[i].x-.5f)*geometry.R*4,texcoords[i].y*geometry.H,0);
            mesh.vertices=vertices;mesh.RecalculateBounds();
            camera.orthographicSize=Mathf.Max(geometry.H*.6f,geometry.R*2.2f);
            // Resolve the radial wall independently of a tall instrument's height.
            camera.aspect=geometry.R*2.2f/camera.orthographicSize;
            camera.transform.position=root.transform.position+new Vector3(0,geometry.H*.5f,-2);
            camera.transform.LookAt(root.transform.position+Vector3.up*geometry.H*.5f);
            VesselBoundary.Configure(material,kind);material.SetFloat("_ClosedContainer",1);
            int clippedVisible=Visible();var clipped=texture.GetPixels();
            material.SetFloat("_ClipContainer",0);Visible();var unguarded=texture.GetPixels();
            int escaped=0,negativeControl=0;
            float wall=Mathf.Max(.012f,geometry.R*.052f),pixelWidth=camera.orthographicSize*camera.aspect*2/64;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){
                var local=camera.ScreenToWorldPoint(new Vector3(x+.5f,y+.5f,2))-root.transform.position;
                if(local.y<=0||local.y>=geometry.H)continue;
                float innerRadius=Mathf.Max(0,VesselMesh.RadiusAt(profile,local.y/geometry.H)-wall);
                // Exclude one pixel at the analytic wall, not arbitrary image masks.
                if(Mathf.Abs(local.x)<=innerRadius+pixelWidth)continue;
                if(clipped[y*64+x].maxColorComponent>.01f)escaped++;
                if(unguarded[y*64+x].maxColorComponent>.01f)negativeControl++;
            }
            Check("particle sidewall fragments contained "+kind,escaped==0&&clippedVisible>5);
            Check("particle sidewall test detects disabled clipping "+kind,negativeControl>5);
            lines.Add("METRIC particle wall "+kind+" visible="+clippedVisible+" outside="+escaped+" unguardedOutside="+negativeControl);
        }
        RenderTexture.active=previous;camera.targetTexture=null;target.Release();Destroy(target);Destroy(texture);Destroy(cameraObject);Destroy(root);Destroy(mesh);Destroy(material);
    }
    void AuditRefractionForeground(){
        const int size=256;
        var root=new GameObject("RefractionAudit");root.transform.position=new Vector3(20,0,20);
        var glass=new GameObject("Glass");glass.layer=28;glass.transform.SetParent(root.transform,false);
        var mesh=VesselMesh.BuildGlass(VesselKind.Beaker);glass.AddComponent<MeshFilter>().sharedMesh=mesh;
        var material=new Material(Shader.Find("ChemLab/Glass"));material.SetFloat("_Refraction",.75f); // Deliberately exaggerated test offset.
        var renderer=glass.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        var stripe=GameObject.CreatePrimitive(PrimitiveType.Cube);stripe.layer=28;stripe.transform.SetParent(root.transform,false);
        var stripeMesh=Instantiate(stripe.GetComponent<MeshFilter>().sharedMesh);var stripeColors=new Color[stripeMesh.vertexCount];
        for(int i=0;i<stripeColors.Length;i++)stripeColors[i]=Color.white;stripeMesh.colors=stripeColors;stripe.GetComponent<MeshFilter>().sharedMesh=stripeMesh;
        stripe.transform.localPosition=new Vector3(.1f,.45f,-.4f);stripe.transform.localScale=new Vector3(.025f,.5f,.015f);
        var ink=new Material(Shader.Find("ChemLab/Solid"));ink.color=Color.red;stripe.GetComponent<MeshRenderer>().sharedMaterial=ink;
        var cameraObject=new GameObject("RefractionAuditCamera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
        camera.cullingMask=1<<28;camera.depthTextureMode=DepthTextureMode.Depth;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.white;
        camera.transform.position=root.transform.position+new Vector3(0,.45f,-3);camera.transform.LookAt(root.transform.position+Vector3.up*.45f);
        camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.orthographicSize=.65f;camera.fieldOfView=25;
        var target=new RenderTexture(size,size,24);target.Create();camera.targetTexture=target;
        var texture=new Texture2D(size,size,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        bool Red(Color c)=>c.r>.015f&&c.r>c.g*1.8f&&c.r>c.b*1.8f;
        Color[] Capture(){camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,size,size),0,0);texture.Apply();return texture.GetPixels();}
        foreach(bool ortho in new[]{false,true}){
            camera.orthographic=ortho;renderer.enabled=false;var baseline=Capture();
            if(!ortho)File.WriteAllBytes("Logs/refraction-baseline.png",texture.EncodeToPNG());
            renderer.enabled=true;var refracted=Capture();
            if(!ortho)File.WriteAllBytes("Logs/refraction-guarded.png",texture.EncodeToPNG());
            Shader.SetGlobalFloat("_LabRefractionAuditBypass",1);var unguarded=Capture();Shader.SetGlobalFloat("_LabRefractionAuditBypass",0);
            if(!ortho)File.WriteAllBytes("Logs/refraction-unguarded.png",texture.EncodeToPNG());
            stripe.SetActive(false);var blank=Capture();stripe.SetActive(true);
            int originals=0,ghosts=0,legacyGhosts=0;
            for(int y=1;y<size-1;y++)for(int x=1;x<size-1;x++){
                if(Red(baseline[y*size+x]))originals++;
                bool near=false;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)near|=Red(baseline[(y+dy)*size+x+dx]);
                int pixel=y*size+x;float backgroundChroma=blank[pixel].r-blank[pixel].g;
                if(!near&&refracted[pixel].r-refracted[pixel].g-backgroundChroma>.004f)ghosts++;
                if(!near&&unguarded[pixel].r-unguarded[pixel].g-backgroundChroma>.004f)legacyGhosts++;
            }
            Check("foreground opaque stripe is not duplicated by glass ortho="+ortho,originals>20&&ghosts<=3);
            Check("refraction test catches unguarded foreground duplication ortho="+ortho,legacyGhosts>3);
            lines.Add("METRIC refraction originalPixels="+originals+" ghostPixels="+ghosts+" unguardedGhostPixels="+legacyGhosts+" ortho="+ortho);
        }
        RenderTexture.active=previous;camera.targetTexture=null;target.Release();Destroy(target);Destroy(texture);Destroy(cameraObject);Destroy(root);Destroy(mesh);Destroy(stripeMesh);Destroy(material);Destroy(ink);
    }

    void AuditFluidSurface(GameObject container,Material material,Fluid3D cavity,VesselKind shape){
        // Read actual raymarched hit positions, not particle positions or a CPU replica.
        container.layer=29;container.transform.position=new Vector3(10000,0,10000);
        var renderer=container.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        material.SetFloat("_AuditSurface",1);
        var cameraObject=new GameObject("SurfaceAuditCamera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
        camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        camera.orthographic=true;camera.orthographicSize=Mathf.Max(cavity.Height*0.7f,cavity.Radius*1.4f);camera.nearClipPlane=0.01f;camera.farClipPlane=10;
        var target=new RenderTexture(96,96,24,RenderTextureFormat.ARGBFloat);target.Create();camera.targetTexture=target;
        camera.aspect=cavity.Radius*1.6f/camera.orthographicSize;
        var pixels=new Texture2D(96,96,TextureFormat.RGBAFloat,false,true);var previous=RenderTexture.active;
        int hits=0;bool contained=true;
        foreach(float yaw in new[]{0f,120f,240f}){
            var center=container.transform.position+Vector3.up*cavity.Height*0.45f;
            camera.transform.position=center+Quaternion.Euler(20,yaw,0)*new Vector3(0,0,-2);
            camera.transform.LookAt(center);camera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,96,96),0,0);pixels.Apply();
            foreach(var pixel in pixels.GetPixels())if(pixel.a>0.99f){
                hits++;float r=Mathf.Sqrt(pixel.r*pixel.r+pixel.b*pixel.b);
                contained&=pixel.g>=-0.0005f&&pixel.g<=cavity.Height+0.0005f&&r<=cavity.InnerRadius(pixel.g)+0.0005f;
            }
        }
        Check("raymarched surface is visible "+shape,hits>3);
        Check("raymarched surface inside cavity "+shape,contained&&hits>3);
        RenderTexture.active=previous;camera.targetTexture=null;target.Release();Destroy(target);Destroy(pixels);Destroy(cameraObject);material.SetFloat("_AuditSurface",0);
    }

    bool InViewport(RectTransform rt) {
        if (rt == null) return false;
        var corners = new Vector3[4]; rt.GetWorldCorners(corners);
        foreach (var c in corners) {
            var p = RectTransformUtility.WorldToScreenPoint(null, c);
            if (p.x < -1 || p.y < -1 || p.x > Screen.width+1 || p.y > Screen.height+1) return false;
        }
        return true;
    }
    bool MenuAt(RectTransform rt, Vector2 pointer) {
        if (rt == null) return false;
        var corners = new Vector3[4]; rt.GetWorldCorners(corners);
        return Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, corners[1]), pointer) < 2;
    }
}
}
