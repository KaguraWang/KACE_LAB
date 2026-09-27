using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
// A connected glassware kit: ordinary child colliders belong to its source vessel,
// so normal depth-ordered picking/dragging/right-click rules also apply to the kit.
public sealed class DistillationView:MonoBehaviour {
    VesselView source;
    DistillationAssembly assembly;
    Material glass,metal,water,condensate;
    readonly List<Renderer> flowRenderers=new List<Renderer>();
    double lastTransferred;
    public bool CondensateVisible {get;private set;}
    public float CondensatePhase {get;private set;}
    Transform parts;
    Vector3 lastReceiver;
    Vector3 lastSource;
    float lastLift;
    bool lastReflux,lastCooling;
    static Mesh pipe,coolantPipe;
    public static void SyncFor(VesselView view,Material glassTemplate,Material metalTemplate){
        var existing=view.GetComponentInChildren<DistillationView>();
        if(view.Model.Distillation==null){if(existing!=null){existing.gameObject.SetActive(false);Destroy(existing.gameObject);}return;}
        if(existing==null){
            var go=new GameObject("DistillationKit");go.transform.SetParent(view.transform,false);
            existing=go.AddComponent<DistillationView>();existing.source=view;existing.lastSource=view.Model.Pos;
            existing.glass=new Material(glassTemplate);existing.metal=new Material(metalTemplate);
            existing.water=new Material(glassTemplate);existing.water.SetColor("_Tint",new Color(.25f,.7f,.9f,.23f));
            existing.condensate=new Material(Shader.Find("ChemLab/Condensate"));
        }
        existing.Sync();
    }
    void Sync(){
        var rig=source.Model.Distillation;if(rig==null)return;
        var moved=source.Model.Pos-lastSource;moved.y=0;
        if(assembly==rig&&!rig.Reflux&&rig.Receiver!=null)rig.Receiver.Pos+=moved;
        lastSource=source.Model.Pos;
        Vector3 receiver=rig.Receiver==null?Vector3.zero:rig.Receiver.Pos+Vector3.up*rig.Receiver.LiftY-transform.position;
        if(parts==null||assembly!=rig||lastLift!=source.Model.LiftY||lastReflux!=rig.Reflux||lastCooling!=rig.Cooling||(lastReceiver-receiver).sqrMagnitude>1e-6f){
            assembly=rig;lastLift=source.Model.LiftY;lastReceiver=receiver;lastReflux=rig.Reflux;lastCooling=rig.Cooling;Build();
        }
        // Visual feedback follows actual transported inventory, not heat alone.
        // This is a wall-film cue, not a second simulated liquid reservoir.
        if(!PourTransaction.IsActive){
            double transferred=rig.CollectedMl+rig.ReturnedMl;
            CondensateVisible=rig.Running&&rig.Cooling&&transferred>lastTransferred+1e-12;
            lastTransferred=transferred;
            if(CondensateVisible)CondensatePhase=Mathf.Repeat(CondensatePhase+Time.deltaTime*2,100);
            condensate.SetFloat("_FlowPhase",CondensatePhase);
        }
        foreach(var renderer in flowRenderers)renderer.enabled=CondensateVisible;
    }
    Transform Tube(string name,Vector3 a,Vector3 b,float radius,Material material,bool pick=true,bool coolant=false){
        if(pipe==null)pipe=VesselMesh.RevolveLoop(new List<Vector2>{new Vector2(1,0),new Vector2(1,1),new Vector2(.82f,1),new Vector2(.82f,0)},48);
        if(coolantPipe==null){
            float inner=(float)(CondenserDimensions.BoreOuterMeters/CondenserDimensions.JacketInnerMeters);
            coolantPipe=VesselMesh.RevolveLoop(new List<Vector2>{new Vector2(1,0),new Vector2(1,1),new Vector2(inner,1),new Vector2(inner,0)},48);
        }
        var mesh=coolant?coolantPipe:pipe;
        var go=new GameObject(name);go.transform.SetParent(parts,false);
        go.transform.localPosition=a;go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
        go.transform.localScale=new Vector3(radius,(b-a).magnitude,radius);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
        if(material==glass)GlasswareBranding.CreateTubeMark(go.transform,radius,(b-a).magnitude);
        // Match the actual hollow glass surface. A normalized capsule with radius 1
        // cannot have height 1: Unity expands it beyond both ends of the tube.
        if(pick)go.AddComponent<MeshCollider>().sharedMesh=mesh;
        return go.transform;
    }
    void Build(){
        if(parts!=null){parts.name="Retired glassware";parts.gameObject.SetActive(false);Destroy(parts.gameObject);}
        parts=new GameObject("Connected glassware").transform;parts.SetParent(transform,false);
        flowRenderers.Clear();
        float h=VesselMesh.Get(source.Model.Kind).H;
        Vector3 neck=Vector3.up*h,head=neck+Vector3.up*.65f;
        // The receiving flask may be taller than the boiling flask. Maintain a
        // downhill condensate path instead of tilting the condenser uphill.
        if(!assembly.Reflux)head.y=Mathf.Max(head.y,lastReceiver.y+VesselMesh.Get(assembly.Receiver.Kind).H+.9f);
        Tube("Still head",neck,head,.1f,glass);
        Vector3 begin,end;
        if(assembly.Reflux){begin=head;end=begin+Vector3.up*3;}
        else {
            begin=head+Vector3.right*.35f;
            float targetH=VesselMesh.Get(assembly.Receiver.Kind).H;
            end=lastReceiver+Vector3.up*(targetH+.3f);
            Tube("Side arm",head,begin,.08f,glass);
            Tube("Receiver adapter",end,lastReceiver+Vector3.up*(targetH-.06f),.065f,glass);
            Tube("Thermometer sheath",head+Vector3.down*.25f,head+Vector3.up*.5f,.034f,glass);
            Tube("Thermometer column",head,head+Vector3.up*.45f,.012f,metal,false);
        }
        assembly.CondenserLengthMeters=(end-begin).magnitude*CondenserDimensions.SceneUnitMeters;
        Tube("Liebig inner vapor tube",begin,end,(float)(CondenserDimensions.BoreOuterMeters/CondenserDimensions.SceneUnitMeters),glass);
        // Fit the film inside the 0.0451-radius inner glass bore. Orient its
        // local +Y in the direction of condensate return/collection.
        var film=Tube("Condensate wall film",assembly.Reflux?end:begin,assembly.Reflux?begin:end,.039f,condensate,false);
        flowRenderers.Add(film.GetComponent<Renderer>());
        var returnFilm=Tube("Condensate outlet film",assembly.Reflux?begin:end,
            assembly.Reflux?neck:lastReceiver+Vector3.up*(VesselMesh.Get(assembly.Receiver.Kind).H-.06f),.028f,condensate,false);
        flowRenderers.Add(returnFilm.GetComponent<Renderer>());
        var axis=(end-begin).normalized;
        float inset=(float)(CondenserDimensions.JacketEndInsetMeters/CondenserDimensions.SceneUnitMeters);
        Tube("Condenser water jacket",begin+axis*inset,end-axis*inset,(float)(CondenserDimensions.JacketOuterMeters/CondenserDimensions.SceneUnitMeters),glass);
        if(assembly.Cooling)Tube("Cooling water",begin+axis*inset,end-axis*inset,(float)(CondenserDimensions.JacketInnerMeters/CondenserDimensions.SceneUnitMeters),water,false,true);
        Vector3 low=begin.y<end.y?begin:end,high=begin.y<end.y?end:begin;
        Vector3 lowPort=Vector3.Lerp(low,high,.12f),highPort=Vector3.Lerp(low,high,.88f);
        Tube("Water inlet lower port",lowPort,lowPort+Vector3.forward*.34f,.045f,glass);
        Tube("Water outlet upper port",highPort,highPort+Vector3.back*.34f,.045f,glass);
        float tabletop=-source.Model.LiftY;
        Tube("Inlet hose",lowPort+Vector3.forward*.34f,new Vector3(lowPort.x,tabletop+.06f,.75f),.028f,water);
        Tube("Outlet hose",highPort+Vector3.back*.34f,new Vector3(highPort.x,tabletop+.06f,-.75f),.028f,water);
        Vector3 support=(begin+end)*.5f+Vector3.forward*.35f;
        Tube("Retort stand",new Vector3(support.x,tabletop,support.z),new Vector3(support.x,Mathf.Max(begin.y,end.y)+.15f,support.z),.035f,metal);
        Tube("Retort foot",new Vector3(support.x-.45f,tabletop+.025f,support.z),new Vector3(support.x+.45f,tabletop+.025f,support.z),.07f,metal);
        Tube("Condenser clamp",support,(begin+end)*.5f,.027f,metal);
    }
    void OnDestroy(){if(glass!=null)Destroy(glass);if(metal!=null)Destroy(metal);if(water!=null)Destroy(water);if(condensate!=null)Destroy(condensate);}
    public static void Install(Vessel source,bool reflux,Vessel receiver=null){
        if(source==null)return;
        if(Lab.Vessels.Any(v=>v!=source&&v.Distillation?.Receiver==source)){
            Lab.Note(source,"该容器正作为接收瓶，请先拆除原连接","obs");return;
        }
        bool createdReceiver=!reflux&&receiver==null;
        if(createdReceiver)receiver=Lab.AddVessel(VesselKind.Flask);
        var rig=source.Distillation??new DistillationAssembly(source);
        if(!rig.Configure(reflux,receiver,out string message)){
            if(createdReceiver)Lab.RemoveVessel(receiver);
            Lab.Note(source,message,"obs");return;
        }
        if(!reflux){SnapController.Detach(receiver);receiver.Pos=source.Pos+Vector3.right*3.2f;receiver.LiftY=0;}
        Lab.Note(source,reflux?"已安装竖直回流冷凝器；右键打开冷凝水后启动":"已安装蒸馏头、温度计和冷凝器，并将接收瓶移到出口；右键打开冷凝水后启动","setup");
        LabBootstrap.Instance?.SyncViews();
        LabBootstrap.Instance?.FrameDistillation(source);
    }
    public static void CreateKit(bool reflux){
        // Reserve bench space for the whole assembly, not the generic single-
        // vessel spawn slot which can place its condenser through a tripod.
        Vector3 position=Vector3.zero;bool found=false;
        for(int ring=0;ring<=8&&!found;ring++)for(int x=-ring;x<=ring&&!found;x++)for(int z=-ring;z<=ring&&!found;z++){
            if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))!=ring)continue;
            var candidate=new Vector3(x*3,0,z*3);
            var center=candidate+Vector3.right*(reflux?0:1.6f);float radius=reflux?1.2f:2.8f;
            bool clear=true;
            foreach(var v in Lab.Vessels)if(Vector3.Distance(center,new Vector3(v.Pos.x,0,v.Pos.z))<radius+VesselMesh.Get(v.Kind).R+.4f){clear=false;break;}
            if(clear)foreach(var a in ApparatusSet.All)if(Vector3.Distance(center,new Vector3(a.Pos.x,0,a.Pos.z))<radius+1.2f){clear=false;break;}
            if(clear)foreach(var t in TestSet.All)if(Vector3.Distance(center,new Vector3(t.Pos.x,0,t.Pos.z))<radius+.6f){clear=false;break;}
            if(clear){position=candidate;found=true;}
        }
        if(!found){Lab.Note(null,"台面没有足够空位放置整套装置，请先腾出空间","obs");return;}
        var source=Lab.AddVessel(VesselKind.RoundBottom);source.Pos=position;Install(source,reflux);
        LabBootstrap.Instance?.SetFocus(source);
    }
    static void ChooseReceiver(Vessel source,Vector2 screen){
        var choices=new List<(string,Action)>{("新建250 mL锥形接收瓶",()=>Install(source,false))};
        foreach(var v in Lab.Vessels.Where(v=>v!=source&&!v.Sealed&&!MeteredTransfer.IsTool(v)&&v.Distillation==null&&(v.ReceivingDistillation==null||v.ReceivingDistillation==source.Distillation)).ToArray()){
            var target=v;choices.Add((VesselNames.Name(v.Kind)+" "+v.Id+"（移到出口）",()=>Install(source,false,target)));
        }
        PetMenu.BuildMenu(screen,choices);
    }
    public static void AddMenu(Vessel v,Vector2 screen,List<(string,Action)> items){
        if(v.Kind!=VesselKind.RoundBottom&&v.Kind!=VesselKind.Flask)return;
        var rig=v.Distillation;
        if(rig==null){
            items.Add(("安装回流冷凝器",()=>Install(v,true)));
            items.Add(("安装蒸馏装置／选接收瓶",()=>ChooseReceiver(v,screen)));
            return;
        }
        items.Add((rig.Cooling?"关闭冷凝水":"打开冷凝水（"+rig.CoolantCelsius+"℃）",()=>{rig.Cooling=!rig.Cooling;Lab.Note(v,rig.Cooling?"冷凝水已打开：下进上出":"冷凝水已关闭","setup");}));
        items.Add((rig.Running?"停止蒸馏／回流":"启动蒸馏／回流",()=>{
            if(rig.Running)rig.Stop();else if(!rig.Start(out string error))Lab.Note(v,error,"obs");
            if(rig.Running)Lab.Note(v,rig.Notice,"setup");
        }));
        items.Add(("查看装置状态",()=>Lab.Note(v,(rig.Reflux?"回流":"蒸馏")+"：收集 "+rig.CollectedMl.ToString("F2")+" mL，回流 "+rig.ReturnedMl.ToString("F2")+" mL；"+rig.Notice,"obs")));
        items.Add(("传热设置（水／甲醇／乙醇）",()=>{
            void Choices(string title,double[] values,Action<double> set){
                var list=new List<(string,Action)>();
                foreach(double value in values){double chosen=value;list.Add((title+value,()=>{set(chosen);Lab.Note(v,"传热教学参数："+title+chosen,"setup");}));}
                PetMenu.BuildMenu(screen,list);
            }
            PetMenu.BuildMenu(screen,new List<(string,Action)>{
                ("基准功率（灯50%） "+rig.HeaterPowerWatts+" W",()=>Choices("功率 W：",new[]{0d,100,250,500},value=>rig.HeaterPowerWatts=value)),
                ("冷却水流量 "+rig.WaterFlowGramsPerSecond+" g/s",()=>Choices("流量 g/s：",new[]{0d,.1,1,10,20},value=>rig.WaterFlowGramsPerSecond=value)),
                ("冷却水入口 "+rig.CoolantCelsius+" ℃",()=>Choices("入口 ℃：",new[]{5d,15,25,35},value=>rig.CoolantCelsius=value)),
                ("总热导 UA "+rig.ConductanceWPerK+" W/K（教学设定）",()=>Choices("UA W/K：",new[]{1d,5,10,20},value=>rig.ConductanceWPerK=value)),
                ("查看热量账本",()=>UnityEngine.Object.FindObjectOfType<LabUI>()?.ShowThermalLedger(rig)),
                ("传热模型说明",()=>UnityEngine.Object.FindObjectOfType<LabUI>()?.ShowThermalLedger(rig,true))
            });
        }));
        items.Add(("查看装置全貌",()=>LabBootstrap.Instance?.FrameDistillation(v)));
        items.Add(("拆除冷凝装置",()=>rig.Detach()));
    }
}
}
