using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;
namespace ChemLab.Game {
public partial class LabUI {
    PourTransaction _pour;
    Vessel _pourA,_pourB;
    Vector3 _pourAPos,_pourBPos,_pourCamPos;float _pourALift,_pourBLift;
    Quaternion _pourCamRotation;int _pourMask;
    RectTransform _pourModal;Slider _pourLiquid,_pourSolid;Text _pourAmount,_pourMessage;
    bool _pourGrams,_pourBusy;
    double _pourAppliedLiquid,_pourAppliedSolid;
    readonly List<GameObject> _pourPackets=new List<GameObject>();
    readonly List<Material> _pourPacketMaterials=new List<Material>();
    GameObject _pourStream;Mesh _pourStreamMesh;
    readonly Dictionary<GameObject,bool> _pourHiddenUi=new Dictionary<GameObject,bool>();
    readonly Dictionary<GameObject,int> _pourLayers=new Dictionary<GameObject,int>();
    public bool IsPouring=>_pour!=null;
    public void BeginStagedPour(Vessel a,Vessel b){
        if(IsPouring||IsDosing||a==null||b==null||a==b)return;
        try{_pour=new PourTransaction(a,b);}catch(System.Exception e){Hint(e.Message);return;}
        PetMenu.Close();_pourA=a;_pourB=b;_pourAPos=a.Pos;_pourBPos=b.Pos;_pourALift=a.LiftY;_pourBLift=b.LiftY;
        _pourCamPos=Lab3D.Cam.transform.position;_pourCamRotation=Lab3D.Cam.transform.rotation;_pourMask=Lab3D.Cam.cullingMask;
        foreach(var view in FindObjectsOfType<VesselView>())if(view.Model==a||view.Model==b)
            foreach(var t in view.GetComponentsInChildren<Transform>(true)){_pourLayers[t.gameObject]=t.gameObject.layer;t.gameObject.layer=26;}
        Lab3D.Cam.cullingMask=1<<26;_pourGrams=false;_pourAppliedLiquid=_pourAppliedSolid=0;
        foreach(Transform child in _canvas.transform){_pourHiddenUi[child.gameObject]=child.gameObject.activeSelf;child.gameObject.SetActive(false);}
        if(PetView.Instance!=null){var pet=PetView.Instance.gameObject;_pourHiddenUi[pet]=pet.activeSelf;pet.SetActive(false);}
        _pourModal=(RectTransform)Node("StagedPourModal",_canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(0,0,0,.05f));
        var panel=Node("StagedPourPanel",_pourModal,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-320,20),new Vector2(320,275),BG);
        _pourAmount=Label(panel,"",16,INK,TextAnchor.UpperLeft,new Vector2(0,1),Vector2.one,new Vector2(20,-50),new Vector2(-20,-15));
        Slider MakeSlider(string name,float y){
            var track=Node(name,panel,Vector2.zero,Vector2.zero,new Vector2(25,y),new Vector2(450,y+20),LabUiTheme.Card);
            var handle=Node("Handle",track,Vector2.zero,new Vector2(0,1),new Vector2(-7,0),new Vector2(7,0),ACC);
            var slider=track.gameObject.AddComponent<Slider>();slider.handleRect=(RectTransform)handle;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=0;slider.maxValue=100;slider.value=50;
            slider.onValueChanged.AddListener(_=>UpdatePourAmounts());return slider;
        }
        _pourLiquid=MakeSlider("LiquidAmount",170);_pourSolid=MakeSlider("SolidAmount",120);_pourSolid.interactable=_pour.SolidMassGrams>0;
        _pourLiquid.interactable=_pour.LiquidMassGrams>0;if(!_pourLiquid.interactable)_pourLiquid.value=0;if(!_pourSolid.interactable)_pourSolid.value=0;
        var unit=Node("Units",panel,Vector2.zero,Vector2.zero,new Vector2(480,120),new Vector2(615,190),BG2);
        Btn(unit,"百分比 / g",()=>SetPourUnits(!_pourGrams),LabUiTheme.Card,15,true);
        _pourMessage=Label(panel,"全场反应已暂停；预览后可撤销，完成才提交",12,DIM,TextAnchor.MiddleLeft,Vector2.zero,new Vector2(1,0),new Vector2(20,70),new Vector2(-20,110));
        void Button(string title,int x,System.Action action){Color color=title=="完成操作"?LabUiTheme.Accent:LabUiTheme.Card;var node=Node(title,panel,Vector2.zero,Vector2.zero,new Vector2(x,15),new Vector2(x+140,58),color);Btn(node,title,()=>{if(!_pourBusy)action();},color,16,true);}
        Button("预览 / Enter",15,PreviewStagedPour);Button("撤销",170,()=>StartCoroutine(FlyContents(0,0)));Button("完成操作",325,()=>StartCoroutine(EndStagedPour(true)));Button("取消",480,()=>StartCoroutine(EndStagedPour(false)));
        SetLayerRecursive(_pourModal.gameObject,UiLayer);UpdatePourAmounts();PrewarmFonts();StartCoroutine(FlyPour(false));
    }
    void UpdatePourAmounts(){if(_pour==null||_pourLiquid==null||_pourSolid==null)return;
        _pourAmount.text=_pourGrams?$"液体 {_pourLiquid.value:F3} / {_pour.LiquidMassGrams:F3} g     固体 {_pourSolid.value:F3} / {_pour.SolidMassGrams:F3} g":$"液体 {_pourLiquid.value:F1}%     固体 {_pourSolid.value:F1}%";
    }
    public void SetPourUnits(bool grams){
        if(_pour==null)return;float liquid=_pourLiquid.normalizedValue,solid=_pourSolid.normalizedValue;_pourGrams=grams;
        _pourLiquid.maxValue=grams?(float)System.Math.Max(1e-9,_pour.LiquidMassGrams):100;
        _pourSolid.maxValue=grams?(float)System.Math.Max(1e-9,_pour.SolidMassGrams):100;
        _pourLiquid.normalizedValue=liquid;_pourSolid.normalizedValue=solid;UpdatePourAmounts();
    }
    void RefreshPourViews(){foreach(var view in FindObjectsOfType<VesselView>())if(view.Model==_pourA||view.Model==_pourB){view.transform.position=view.Model.Pos+Vector3.up*view.Model.LiftY;view.Refresh();}}
    void PreviewStagedPour(){if(_pour==null||_pourBusy)return;StartCoroutine(FlyContents(_pourLiquid.normalizedValue,_pourSolid.interactable?_pourSolid.normalizedValue:0));}
    IEnumerator FlyContents(double liquid,double solid){
        if(!_pour.Preview(liquid,solid,out string message)){_pourMessage.text=message;yield break;}
        _pour.Preview(_pourAppliedLiquid,_pourAppliedSolid,out _);_pourBusy=true;
        double dl=liquid-_pourAppliedLiquid,ds=solid-_pourAppliedSolid;
        const int count=16;const float travel=.85f,spacing=.055f;
        for(int phase=0;phase<2;phase++){
            double delta=phase==0?dl:ds,volume=phase==0?_pour.LiquidVolumeMl:_pour.SolidVolumeMl;
            var material=new Material(Shader.Find(phase==0?"ChemLab/Glass":"Standard"));
            if(phase==0){Color tint=Lab.TrySolutionColor(_pourA,out Color solution)?solution:new Color(.86f,.95f,1);tint.a=.16f;material.SetColor("_Tint",tint);material.SetFloat("_Refraction",.08f);material.SetFloat("_ReflStrength",.9f);}else material.color=new Color(.48f,.43f,.34f);_pourPacketMaterials.Add(material);
            if(phase==0)continue; // Liquid is one mesh, not sixteen hidden legacy spheres.
            for(int j=0;j<count;j++){
                var packet=GameObject.CreatePrimitive(PrimitiveType.Sphere);packet.name="PourSolidPacket";packet.layer=26;Destroy(packet.GetComponent<Collider>());packet.GetComponent<Renderer>().sharedMaterial=material;
                var mesh=packet.GetComponent<MeshFilter>().sharedMesh;double meshVolume=0;var v=mesh.vertices;var f=mesh.triangles;
                for(int k=0;k<f.Length;k+=3)meshVolume+=Vector3.Dot(v[f[k]],Vector3.Cross(v[f[k+1]],v[f[k+2]]))/6.0;
                float scale=(float)System.Math.Pow(System.Math.Abs(delta)*volume/1000/count/System.Math.Abs(meshVolume),1.0/3);
                packet.transform.localScale=Vector3.one*scale;packet.SetActive(false);_pourPackets.Add(packet);
            }
        }
        _pourStream=new GameObject("PourLiquidStream");_pourStream.layer=26;_pourStreamMesh=new Mesh{name="Continuous suction stream"};_pourStreamMesh.MarkDynamic();_pourStream.AddComponent<MeshFilter>().sharedMesh=_pourStreamMesh;_pourStream.AddComponent<MeshRenderer>().sharedMaterial=_pourPacketMaterials[0];
        Vector3 FlowPoint(Vessel a,Vessel b,float u){
            Vector3 start=a.Pos+Vector3.up*.15f,end=b.Pos+Vector3.up*.15f;
            Vector3 mouthA=a.Pos+Vector3.up*(VesselMesh.Get(a.Kind).H+.15f),mouthB=b.Pos+Vector3.up*(VesselMesh.Get(b.Kind).H+.15f);
            if(u<.15f)return Vector3.Lerp(start,mouthA,u/.15f);
            if(u>.85f)return Vector3.Lerp(mouthB,end,(u-.85f)/.15f);
            float q=(u-.15f)/.7f,r=1-q;
            // Vertical tangents at both mouths join the extraction/insertion legs smoothly.
            return r*r*r*mouthA+3*r*r*q*(mouthA+Vector3.up*.6f)+3*r*q*q*(mouthB+Vector3.up*.6f)+q*q*q*mouthB;
        }
        for(float elapsed=0;elapsed<travel+spacing*(count-1);elapsed+=Time.unscaledDeltaTime){
            int departed=0,arrived=0;
            for(int j=0;j<count;j++){float u=(elapsed-j*spacing)/travel;if(u>=0)departed++;if(u>=1)arrived++;
                {
                    var packet=_pourPackets[j];double delta=ds;
                    bool active=u>=0&&u<1&&System.Math.Abs(delta)>1e-10&&packet.transform.localScale.x>1e-6f;packet.SetActive(active);if(!active)continue;
                    Vessel a=delta>0?_pourA:_pourB,b=delta>0?_pourB:_pourA;
                    Vector3 start=a.Pos+Vector3.up*.15f,end=b.Pos+Vector3.up*.15f;
                    Vector3 mouthA=a.Pos+Vector3.up*(VesselMesh.Get(a.Kind).H+.15f),mouthB=b.Pos+Vector3.up*(VesselMesh.Get(b.Kind).H+.15f);
                    if(u<.15f)packet.transform.position=Vector3.Lerp(start,mouthA,u/.15f);
                    else if(u>.85f)packet.transform.position=Vector3.Lerp(mouthB,end,(u-.85f)/.15f);
                    else{float q=(u-.15f)/.7f;Vector3 control=(mouthA+mouthB)*.5f+Vector3.up*.9f;packet.transform.position=(1-q)*(1-q)*mouthA+2*q*(1-q)*control+q*q*mouthB;}
                }
            }
            // A continuous pump stream must emit continuously too: packet-sized
            // inventory steps made its radius pulse sixteen times per pour.
            double emitted=Mathf.Clamp01(elapsed/(spacing*(count-1))),received=Mathf.Clamp01((elapsed-travel)/(spacing*(count-1)));
            double outL=_pourAppliedLiquid+dl*(dl>=0?emitted:received),inL=_pourAppliedLiquid+dl*(dl>=0?received:emitted);
            double outS=_pourAppliedSolid+ds*(ds>=0?departed:arrived)/count,inS=_pourAppliedSolid+ds*(ds>=0?arrived:departed)/count;
            if(!_pour.PreviewFlight(outL,outS,inL,inS,out string flightError)){
                RejectPourPreview(flightError);yield break;
            }
            double liquidVolume=(outL-inL)*_pour.LiquidVolumeMl/1000;
            float head=Mathf.Clamp01(elapsed/travel),tail=Mathf.Clamp01((elapsed-spacing*(count-1))/travel);
            bool streamVisible=liquidVolume>1e-12&&head-tail>.005f;_pourStream.SetActive(streamVisible);
            if(streamVisible){
                Vessel a=dl>=0?_pourA:_pourB,b=dl>=0?_pourB:_pourA;
                Vector3 origin=FlowPoint(a,b,tail);_pourStream.transform.position=origin;
                var path=new Vector3[49];for(int k=0;k<path.Length;k++)path[k]=FlowPoint(a,b,Mathf.Lerp(tail,head,k/(float)(path.Length-1)))-origin;
                PourStreamMesh.Update(_pourStreamMesh,path,liquidVolume,elapsed);}
            RefreshPourViews();yield return null;
        }
        ClearPourPackets();
        if(!_pour.Preview(liquid,solid,out string finalNotice)){RejectPourPreview(finalNotice);yield break;}
        _pourAppliedLiquid=liquid;_pourAppliedSolid=solid;RefreshPourViews();_pourBusy=false;
        _pourMessage.text=string.IsNullOrEmpty(finalNotice)?"预览完成；可撤销、重新选量或完成操作":
            "预览完成；"+finalNotice+"。可撤销、重新选量或完成操作";
    }
    void ClearPourPackets(){if(_pourStream!=null)Destroy(_pourStream);if(_pourStreamMesh!=null)Destroy(_pourStreamMesh);_pourStream=null;_pourStreamMesh=null;foreach(var packet in _pourPackets)if(packet!=null)Destroy(packet);_pourPackets.Clear();foreach(var m in _pourPacketMaterials)if(m!=null)Destroy(m);_pourPacketMaterials.Clear();}
    void RejectPourPreview(string error){
        ClearPourPackets();_pour.Undo();_pourAppliedLiquid=_pourAppliedSolid=0;
        RefreshPourViews();_pourBusy=false;_pourMessage.text=error+"；已恢复倾倒前状态，请重新选量";
    }
    IEnumerator FlyPour(bool back){
        _pourBusy=true;var stage=new Vector3(100,0,100);var cam=Lab3D.Cam.transform;
        Vector3 a0=_pourA.Pos,b0=_pourB.Pos,c0=cam.position;Quaternion q0=cam.rotation;
        Vector3 a1=back?_pourAPos:stage+Vector3.left*.7f,b1=back?_pourBPos:stage+Vector3.right*.7f,c1=back?_pourCamPos:stage+new Vector3(0,2.5f,-4.5f);
        Quaternion q1=back?_pourCamRotation:Quaternion.LookRotation(stage+Vector3.up*.65f-c1);
        for(float t=0;t<1;t+=Time.unscaledDeltaTime/.8f){float f=Mathf.SmoothStep(0,1,t);_pourA.Pos=Vector3.Lerp(a0,a1,f);_pourB.Pos=Vector3.Lerp(b0,b1,f);_pourA.LiftY=back?_pourALift:0;_pourB.LiftY=back?_pourBLift:0;cam.SetPositionAndRotation(Vector3.Lerp(c0,c1,f),Quaternion.Slerp(q0,q1,f));RefreshPourViews();yield return null;}
        _pourA.Pos=a1;_pourB.Pos=b1;cam.SetPositionAndRotation(c1,q1);RefreshPourViews();_pourBusy=false;
    }
    IEnumerator EndStagedPour(bool commit){_pourBusy=true;if(!commit)yield return FlyContents(0,0);yield return FlyPour(true);RestorePourScene();var transaction=_pour;_pour=null;if(commit)transaction.Commit();else transaction.Dispose();RefreshVesselPanel();RefreshLog();}
    void RestorePourScene(){foreach(var entry in _pourLayers)if(entry.Key!=null)entry.Key.layer=entry.Value;_pourLayers.Clear();foreach(var entry in _pourHiddenUi)if(entry.Key!=null)entry.Key.SetActive(entry.Value);_pourHiddenUi.Clear();Lab3D.Cam.cullingMask=_pourMask;if(_pourModal!=null)Destroy(_pourModal.gameObject);_pourModal=null;}
    void UpdateStagedPour(){if(!IsPouring)return;if(LabInput.GetKeyDown(KeyCode.Return)||LabInput.GetKeyDown(KeyCode.KeypadEnter))PreviewStagedPour();if(LabInput.GetKeyDown(KeyCode.Escape)&&!_pourBusy)StartCoroutine(EndStagedPour(false));}
    void OnDisable(){
        if(_pour==null)return;StopAllCoroutines();ClearPourPackets();_pourA.Pos=_pourAPos;_pourB.Pos=_pourBPos;_pourA.LiftY=_pourALift;_pourB.LiftY=_pourBLift;
        if(Lab3D!=null&&Lab3D.Cam!=null)Lab3D.Cam.transform.SetPositionAndRotation(_pourCamPos,_pourCamRotation);
        _pour.Dispose();_pour=null;RestorePourScene();RefreshPourViews();_pourBusy=false;
    }
}
}
