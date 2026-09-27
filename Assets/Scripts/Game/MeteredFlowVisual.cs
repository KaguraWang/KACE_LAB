using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {
// A short, non-interactive liquid path between the volumetric tip and its receiver.
// Inventory still moves exclusively through MeteredTransfer; this is presentation.
public sealed class MeteredFlowVisual : MonoBehaviour {
    Vessel tool;
    Material liquid;
    GameObject drop, stream;
    float clock;

    static GameObject Shape(string name,PrimitiveType shape,Transform parent,Material material){
        var go=GameObject.CreatePrimitive(shape);go.name=name;
        go.transform.SetParent(parent,false);
        var collider=go.GetComponent<Collider>();
        if(collider!=null){collider.enabled=false;Destroy(collider);}
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        go.SetActive(false);return go;
    }
    public static MeteredFlowVisual Create(Transform vessel,Vessel model,Material liquidTemplate){
        if(!MeteredTransfer.IsTool(model))return null;
        var root=new GameObject("MeteredFlowVisual");root.transform.SetParent(vessel,false);
        var visual=root.AddComponent<MeteredFlowVisual>();visual.tool=model;
        visual.liquid=new Material(liquidTemplate);
        if(visual.liquid.HasProperty("_ClipContainer"))visual.liquid.SetFloat("_ClipContainer",0);
        visual.drop=Shape("MeteredDrop",PrimitiveType.Sphere,root.transform,visual.liquid);
        visual.stream=Shape("MeteredStream",PrimitiveType.Cylinder,root.transform,visual.liquid);
        return visual;
    }
    public bool IsFlowVisible=>drop!=null&&stream!=null&&(drop.activeSelf||stream.activeSelf);
    public void Sync(float dt){
        if(tool==null||drop==null||stream==null)return;
        var receiver=tool.AttachedTo;
        bool active=receiver!=null&&MeteredTransfer.CanAttach(tool,receiver)&&
                    !PourTransaction.IsActive&&Mathf.Abs((float)tool.MeteredFlowMlPerSecond)>1e-6f;
        if(!active){drop.SetActive(false);stream.SetActive(false);return;}
        float tip=tool.Pos.y+tool.LiftY;
        float rim=receiver.Pos.y+receiver.LiftY+VesselMesh.Get(receiver.Kind).H;
        float gap=tip-rim;
        if(gap<=.012f||gap>.35f){drop.SetActive(false);stream.SetActive(false);return;}
        var source=tool.MeteredFlowMlPerSecond>0?tool:receiver;
        Color color=Lab.TrySolutionColor(source,out Color tint)?tint:new Color(.69f,.83f,.96f);
        liquid.SetColor("_Color",new Color(color.r,color.g,color.b,.88f));
        float rate=Mathf.Abs((float)tool.MeteredFlowMlPerSecond);
        if(rate>=.75f){
            drop.SetActive(false);stream.SetActive(true);
            stream.transform.localPosition=new Vector3(0,-gap*.5f,0);
            float radius=Mathf.Lerp(.0045f,.009f,Mathf.Clamp01(rate/5f));
            stream.transform.localScale=new Vector3(radius,gap*.5f,radius);
        }else{
            stream.SetActive(false);drop.SetActive(true);
            clock+=Mathf.Max(0,dt)*(1.5f+rate*5f);
            float fraction=clock-Mathf.Floor(clock);
            if(tool.MeteredFlowMlPerSecond<0)fraction=1-fraction;
            drop.transform.localPosition=new Vector3(0,-Mathf.Lerp(.007f,gap-.007f,fraction),0);
            drop.transform.localScale=new Vector3(.018f,.024f,.018f);
        }
    }
    void OnDestroy(){if(liquid!=null)Destroy(liquid);}
}
}
