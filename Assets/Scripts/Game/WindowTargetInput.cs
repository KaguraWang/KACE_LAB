using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChemLab.Game {
// Enabled only by -windowTargetInput. A local pipe addresses one Player process;
// it never activates a window or moves the desktop cursor.
[DefaultExecutionOrder(-10000)]
public sealed class WindowTargetInput : MonoBehaviour {
    static WindowTargetInput current;
    readonly ConcurrentQueue<string> pending=new ConcurrentQueue<string>();
    readonly HashSet<KeyCode> keys=new HashSet<KeyCode>();
    readonly HashSet<KeyCode> keyDown=new HashSet<KeyCode>();
    readonly bool[] buttons=new bool[3],pressed=new bool[3],released=new bool[3];
    readonly ManualResetEvent stopReader=new ManualResetEvent(false);
    Thread reader;
    volatile bool stopping;
    Vector2 position,scroll;
    string logPath;

    public static bool Active=>current!=null;
    public static Vector2 Position=>current!=null?current.position:Vector2.zero;
    public static Vector2 Scroll=>current!=null?current.scroll:Vector2.zero;
    public static bool Button(int i)=>current!=null&&i>=0&&i<3&&current.buttons[i];
    public static bool Pressed(int i)=>current!=null&&i>=0&&i<3&&current.pressed[i];
    public static bool Released(int i)=>current!=null&&i>=0&&i<3&&current.released[i];
    public static bool Key(KeyCode k)=>current!=null&&current.keys.Contains(k);
    public static bool KeyDown(KeyCode k)=>current!=null&&current.keyDown.Contains(k);

    /// <summary>
    /// Test-only: feeds the same protocol line the external pipe would send, so an
    /// in-process audit can drive the real input path without touching the desktop.
    /// </summary>
    public static void InjectForTest(string line) { current?.pending.Enqueue(line); }

    public static void TraceRightClick(Vector2 point, bool overUI, LabPick.Hit hit) {
        if(current==null)return;
        var ui=new List<RaycastResult>();
        if(EventSystem.current!=null)
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},ui);
        string names=string.Join(",",ui.ConvertAll(x=>x.gameObject.name));
        File.AppendAllText(current.logPath,$"RIGHTCLICK at={point.x:F0},{point.y:F0} overUI={overUI} target={hit.vessel?.Id ?? hit.apparatus?.Id ?? hit.testItem?.Id ?? "-"} ui={names}{Environment.NewLine}");
    }

    IEnumerator CaptureFrame(string capturePath) {
        yield return new WaitForEndOfFrame();
        // Hidden windows may have a black backbuffer. Render the current cameras
        // and every active overlay to one target, preserving normal alpha blending.
        var lab=LabBootstrap.Instance;var scene=lab.Cam;var ui=lab.UICam;
        var target=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);
        var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        var active=RenderTexture.active;var sceneTarget=scene.targetTexture;var uiTarget=ui.targetTexture;
        var clear=ui.clearFlags;bool enabled=ui.enabled;int mask=ui.cullingMask;
        var canvases=new List<(Canvas canvas,RenderMode mode,Camera camera,float plane)>();
        var layers=new Dictionary<Transform,int>();
        try {
            scene.targetTexture=target;scene.Render();scene.targetTexture=sceneTarget;
            foreach(var canvas in FindObjectsOfType<Canvas>()){
                if(!canvas.isActiveAndEnabled||!canvas.isRootCanvas||canvas.renderMode!=RenderMode.ScreenSpaceOverlay)continue;
                canvases.Add((canvas,canvas.renderMode,canvas.worldCamera,canvas.planeDistance));
                foreach(var child in canvas.GetComponentsInChildren<Transform>(true)){
                    if(!layers.ContainsKey(child))layers.Add(child,child.gameObject.layer);
                    child.gameObject.layer=31;
                }
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=ui;canvas.planeDistance=10;
            }
            ui.targetTexture=target;ui.clearFlags=CameraClearFlags.Depth;ui.cullingMask=1<<31;ui.enabled=true;
            Canvas.ForceUpdateCanvases();ui.Render();RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();
            File.WriteAllBytes(capturePath,texture.EncodeToPNG());
            File.AppendAllText(logPath,$"CAPTURE {capturePath}{Environment.NewLine}");
        } finally {
            foreach(var entry in canvases){entry.canvas.renderMode=entry.mode;entry.canvas.worldCamera=entry.camera;entry.canvas.planeDistance=entry.plane;}
            foreach(var entry in layers)if(entry.Key!=null)entry.Key.gameObject.layer=entry.Value;
            scene.targetTexture=sceneTarget;ui.targetTexture=uiTarget;ui.clearFlags=clear;ui.enabled=enabled;ui.cullingMask=mask;
            RenderTexture.active=active;Canvas.ForceUpdateCanvases();target.Release();Destroy(target);Destroy(texture);
        }
    }

    void Start(){
        current=this;Application.runInBackground=true;
        logPath=Path.GetFullPath("Logs/window-target-input.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath));
        int pid=System.Diagnostics.Process.GetCurrentProcess().Id;
        File.WriteAllText(logPath,$"READY pid={pid} size={Screen.width}x{Screen.height}{Environment.NewLine}");
        var module=FindObjectOfType<StandaloneInputModule>();
        if(module==null)throw new InvalidOperationException("Window input test needs StandaloneInputModule");
        module.inputOverride=gameObject.AddComponent<WindowTargetBaseInput>();
        reader=new Thread(()=>ReadLoop("ChemLabInput_"+pid)){IsBackground=true,Name="ChemLabWindowInput"};
        reader.Start();
    }
    void ReadLoop(string pipeName){
        while(!stopping){
            try{
                // An overlapped pipe can be closed without blocking Unity's main
                // thread behind a synchronous WaitForConnection/ReadLine call.
                // The reader owns disposal; shutdown only signals and joins it.
                using(var pipe=new NamedPipeServerStream(pipeName,PipeDirection.In,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous)){
                    var connection=pipe.WaitForConnectionAsync();
                    while(!connection.IsCompleted&&!stopReader.WaitOne(50)){}
                    // Close the overlapped operation, then observe its completion.
                    // Never dispose a Task-owned AsyncWaitHandle before Mono's
                    // completion callback has signalled it.
                    if(stopping)pipe.Dispose();
                    connection.GetAwaiter().GetResult();
                    if(stopping)break;
                    using(var input=new StreamReader(pipe)){
                        while(!stopping){
                            var read=input.ReadLineAsync();
                            while(!read.IsCompleted&&!stopReader.WaitOne(50)){}
                            if(stopping)pipe.Dispose();
                            string line=read.GetAwaiter().GetResult();
                            if(stopping||line==null)break;
                            pending.Enqueue(line);
                        }
                    }
                }
            }catch(ObjectDisposedException){break;}
            catch(IOException){if(stopping)break;}
            catch(Exception e){
                if(!stopping)File.AppendAllText(logPath,"PIPE ERROR "+e+Environment.NewLine);
                break;
            }
        }
    }
    void Update(){
        for(int i=0;i<3;i++){pressed[i]=false;released[i]=false;}
        keyDown.Clear();scroll=Vector2.zero;
        // Preserve frame ordering if transport delivery batches several commands.
        // Otherwise DOWN/UP/MENU can all run before the lab sees the press.
        if(pending.TryDequeue(out string line)){
            var words=line.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
            if(words.Length==0)return;
            try{
                switch(words[0].ToUpperInvariant()){
                    case "PING" when words.Length==2:
                        File.AppendAllText(logPath,"ACK "+words[1]+Environment.NewLine);break;
                    case "MOVE" when words.Length==3:
                        position=new Vector2(Mathf.Clamp(float.Parse(words[1],CultureInfo.InvariantCulture),0,Screen.width-1),
                                             Mathf.Clamp(float.Parse(words[2],CultureInfo.InvariantCulture),0,Screen.height-1));break;
                    case "DOWN" when words.Length==2:
                        int down=int.Parse(words[1]);if(down>=0&&down<3){pressed[down]=!buttons[down];buttons[down]=true;}break;
                    case "UP" when words.Length==2:
                        int up=int.Parse(words[1]);if(up>=0&&up<3){released[up]=buttons[up];buttons[up]=false;}break;
                    case "KEYDOWN" when words.Length==2:
                        if(Enum.TryParse(words[1],true,out KeyCode kd)){if(keys.Add(kd))keyDown.Add(kd);}break;
                    case "KEYUP" when words.Length==2:
                        if(Enum.TryParse(words[1],true,out KeyCode ku))keys.Remove(ku);break;
                    case "SCROLL" when words.Length==2:
                        scroll=new Vector2(0,float.Parse(words[1],CultureInfo.InvariantCulture));break;
                    case "CONSOLE":
                        var devConsole=DeveloperConsole.Instance;
                        var consoleLab=LabBootstrap.Instance;
                        var consoleDriver=DavidDriver.Instance;
                        File.AppendAllText(logPath,FormattableString.Invariant(
                            $"CONSOLE open={devConsole!=null&&devConsole.IsOpen} captured={DeveloperConsole.InputCaptured} rate={WorldClock.Rate:0.###} david={DavidMode.Enabled} focus={consoleLab?.TestFocusPoint.x:F2},{consoleLab?.TestFocusPoint.z:F2} drag={consoleLab?.TestDragTargetId ?? "-"} segments={consoleDriver?.SegmentIndexForTest ?? -1}{Environment.NewLine}"));
                        if(devConsole!=null)foreach(var entry in devConsole.GetHistorySnapshot())
                            File.AppendAllText(logPath,$"CONSOLELINE {entry.Replace('\n','|').Replace('\r',' ')}{Environment.NewLine}");
                        break;
                    case "CONSOLETEXT":
                        // The pipe protocol carries KeyCodes, not characters, so an
                        // external test injects the command line text directly.
                        var consoleField=DeveloperConsole.Instance?.TestInputField;
                        var consoleText=line.Length>words[0].Length?line.Substring(words[0].Length).Trim():string.Empty;
                        if(consoleField!=null){consoleField.text=consoleText;consoleField.ActivateInputField();}
                        File.AppendAllText(logPath,$"CONSOLETEXT {consoleText}{Environment.NewLine}");
                        break;
                    case "DAVID":
                        var davidDriver=DavidDriver.Instance;
                        File.AppendAllText(logPath,davidDriver==null
                            ?$"DAVID absent enabled={DavidMode.Enabled}{Environment.NewLine}"
                            :$"DAVID enabled={DavidMode.Enabled} active={davidDriver.Active} {davidDriver.AuditLine()}{Environment.NewLine}");
                        break;
                    case "STATE":
                        File.AppendAllText(logPath,$"STATE mouse={position.x:F0},{position.y:F0} held={buttons[0]} selected={LabBootstrap.Instance?.TestSelectedVesselId} vessels={ChemLab.Chem.Lab.Vessels.Count}{Environment.NewLine}");
                        foreach(var vessel in ChemLab.Chem.Lab.Vessels)if(ChemLab.Chem.MeteredTransfer.IsTool(vessel))
                            File.AppendAllText(logPath,$"TOOL {vessel.Id} {vessel.Kind} attached={vessel.AttachedTo?.Id ?? "-"} pos={vessel.Pos.x:F2},{vessel.Pos.z:F2} lift={vessel.LiftY:F2} volume={vessel.Volume():F3} flow={vessel.MeteredFlowMlPerSecond:F3}{Environment.NewLine}");
                        foreach(var item in ChemLab.Chem.TestSet.All)if(item.Kind==ChemLab.Chem.TestItemKind.Splint){
                            var itemView=Array.Find(FindObjectsOfType<TestItemView>(),v=>v.Model==item);
                            File.AppendAllText(logPath,$"SPLINT {item.Id} state={item.Splint} attached={item.AttachedBurner?.Id ?? "-"} flame={itemView?.Flame?.gameObject.activeInHierarchy ?? false} glow={itemView?.EmberVisible ?? false}{Environment.NewLine}");
                        }
                        break;
                    case "LABLOG":
                        var entries=ChemLab.Chem.Lab.Log;
                        for(int n=Math.Max(0,entries.Count-4);n<entries.Count;n++)
                            File.AppendAllText(logPath,$"LABLOG {entries[n].VesselId} {entries[n].Text.Replace('\n',' ').Replace('\r',' ')}{Environment.NewLine}");
                        break;
                    case "THERMALPANEL":
                        var ledgerModal=GameObject.Find("ThermalLedgerModal");
                        if(ledgerModal==null){File.AppendAllText(logPath,$"THERMALPANEL closed{Environment.NewLine}");break;}
                        foreach(var label in ledgerModal.GetComponentsInChildren<Text>())
                            File.AppendAllText(logPath,$"THERMALPANEL fits={label.preferredHeight<=label.rectTransform.rect.height+1} text={label.text.Replace('\n','|').Replace('\r',' ')}{Environment.NewLine}");
                        break;
                    case "THERMAL" when words.Length==2:
                        var thermalVessel=ChemLab.Chem.Lab.Vessels.Find(v=>v.Id==words[1]);
                        var thermalRig=thermalVessel?.Distillation;
                        if(thermalRig!=null)File.AppendAllText(logPath,FormattableString.Invariant($"THERMAL {thermalVessel.Id} transient={thermalRig.UseTransientCondenser} running={thermalRig.Running} length={thermalRig.CondenserLengthMeters:F6} wall={thermalRig.CondenserWallC:F6} jacket={thermalRig.CondenserWaterC:F6} stored={thermalRig.CondenserStoredEnergyJ:F6} power={thermalRig.HeaterPowerWatts:F1} flow={thermalRig.WaterFlowGramsPerSecond:F1}{Environment.NewLine}"));
                        break;
                    case "PET":
                        var pet=PetView.Instance;
                        var sprite=pet?.transform.Find("Sprite") as RectTransform;
                        if(sprite==null){File.AppendAllText(logPath,$"PET absent{Environment.NewLine}");break;}
                        var petScreen=RectTransformUtility.WorldToScreenPoint(null,sprite.TransformPoint(sprite.rect.center));
                        File.AppendAllText(logPath,$"PET center={petScreen.x:F1},{petScreen.y:F1} home={pet.Home.x:F1},{pet.Home.y:F1} visible={pet.gameObject.activeInHierarchy}{Environment.NewLine}");
                        break;
                    case "INFO":
                        var info=VersionInfo.Instance;
                        if(info==null){File.AppendAllText(logPath,$"INFO absent{Environment.NewLine}");break;}
                        var infoPoint=info.ButtonScreenPosition;
                        File.AppendAllText(logPath,$"INFO at={infoPoint.x:F0},{infoPoint.y:F0} open={info.IsOpen} text={info.DisplayText.Replace('\n','|')}{Environment.NewLine}");
                        break;
                    case "CAMERA":
                        var cam=LabBootstrap.Instance?.Cam;
                        if(cam==null)break;
                        var forward=cam.transform.forward;
                        File.AppendAllText(logPath,$"CAMERA yaw={Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg:F3} pitch={Mathf.Asin(forward.y)*Mathf.Rad2Deg:F3} eye={cam.transform.position.x:F3},{cam.transform.position.y:F3},{cam.transform.position.z:F3}{Environment.NewLine}");
                        break;
                    case "SEARCHTEXT" when words.Length==2:
                        var searchField=Array.Find(FindObjectsOfType<InputField>(),f=>f.name=="Search");
                        if(searchField!=null)searchField.text=words[1]=="-"?"":words[1];
                        break;
                    case "RACKCONTENT":
                        var rackUi=FindObjectOfType<LabUI>();
                        var rackGrid=Array.Find(rackUi.GetComponentsInChildren<GridLayoutGroup>(),g=>g.name=="Content");
                        var rackScroll=rackGrid.GetComponentInParent<ScrollRect>();
                        var viewportCorners=new Vector3[4];rackScroll.viewport.GetWorldCorners(viewportCorners);
                        File.AppendAllText(logPath,$"RACKCONTENT cat={rackUi.TestCat} query={rackUi.TestQuery} count={rackUi.TestVisibleCount} scroll={rackScroll.verticalNormalizedPosition:F3} viewport={viewportCorners[0].x:F0},{viewportCorners[0].y:F0},{viewportCorners[2].x:F0},{viewportCorners[2].y:F0}{Environment.NewLine}");
                        foreach(Transform card in rackGrid.transform){
                            if(!card.gameObject.activeSelf)continue;
                            var cardRect=card as RectTransform;var cardPoint=cardRect.TransformPoint(cardRect.rect.center);
                            var cardImage=card.Find("ReagentImage").GetComponent<Image>();
                            var placeholder=cardImage.GetComponentInChildren<Text>();
                            File.AppendAllText(logPath,$"RACKCARD {card.name.Substring(2)} center={cardPoint.x:F0},{cardPoint.y:F0} image={cardImage.sprite!=null} placeholder={placeholder?.text ?? "-"}{Environment.NewLine}");
                        }
                        break;
                    case "MASS" when words.Length==2:
                        var massVessel=ChemLab.Chem.Lab.Vessels.Find(v=>v.Id==words[1]);
                        if(massVessel!=null)File.AppendAllText(logPath,FormattableString.Invariant($"MASS {massVessel.Id} {massVessel.Mass():G12}{Environment.NewLine}"));
                        break;
                    case "RACK":
                        var rack=Array.Find(FindObjectsOfType<RectTransform>(),x=>x.name=="RightPanel");
                        if(rack==null){File.AppendAllText(logPath,$"RACK absent{Environment.NewLine}");break;}
                        var corners=new Vector3[4];rack.GetWorldCorners(corners);
                        File.AppendAllText(logPath,$"RACK width={rack.rect.width:F1} screen={corners[0].x:F1},{corners[2].x:F1} visible={rack.gameObject.activeInHierarchy}{Environment.NewLine}");
                        break;
                    case "PANELS":
                        var panelUI=FindObjectOfType<LabUI>();
                        if(panelUI==null)break;
                        foreach(string panelName in new[]{"LeftPanel","RightPanel","BottomPanel"}){
                            var panel=panelUI.transform.Find("LabCanvas/"+panelName) as RectTransform;
                            if(panel==null)continue;
                            var group=panel.GetComponent<CanvasGroup>();
                            var origin=RectTransformUtility.WorldToScreenPoint(null,panel.TransformPoint(panel.rect.min));
                            File.AppendAllText(logPath,$"PANEL {panelName} active={panel.gameObject.activeInHierarchy} alpha={group?.alpha ?? 1:F3} blocks={group?.blocksRaycasts ?? true} origin={origin.x:F1},{origin.y:F1}{Environment.NewLine}");
                        }
                        break;
                    // Controlled test fixture, available only in the explicit pipe-input mode.
                    case "SEED" when words.Length==3:
                        foreach(var vessel in ChemLab.Chem.Lab.Vessels)if(vessel.Id==words[1]){
                            double ml=double.Parse(words[2],CultureInfo.InvariantCulture);
                            if(ml<0||ml>vessel.Capacity||double.IsNaN(ml))throw new ArgumentOutOfRangeException(nameof(ml));
                            vessel.Water=ml;
                            File.AppendAllText(logPath,$"SEED {vessel.Id} water={ml:F3}{Environment.NewLine}");
                            break;
                        }
                        break;
                    case "OBJECTS":
                        var camera=LabBootstrap.Instance?.Cam;
                        if(camera!=null)foreach(var vessel in ChemLab.Chem.Lab.Vessels){
                            var geo=VesselMesh.Get(vessel.Kind);
                            var screen=camera.WorldToScreenPoint(vessel.Pos+Vector3.up*(vessel.LiftY+Mathf.Min(geo.H,1.5f)*.5f));
                            File.AppendAllText(logPath,$"OBJECT {vessel.Id} {vessel.Kind} screen={screen.x:F0},{screen.y:F0},{screen.z:F2}{Environment.NewLine}");
                            if(ChemLab.Chem.MeteredTransfer.IsTool(vessel)){
                                var view=Array.Find(FindObjectsOfType<VesselView>(),v=>v.Model==vessel);
                                var detail=view?.GetComponentInChildren<VolumetricDetails>();
                                var handle=detail?.transform.Find("StopcockHandle");
                                if(handle!=null){var hp=camera.WorldToScreenPoint(handle.position);
                                    File.AppendAllText(logPath,$"HANDLE {vessel.Id} screen={hp.x:F0},{hp.y:F0},{hp.z:F2}{Environment.NewLine}");}
                            }
                        }
                        if(camera!=null){
                            foreach(var apparatus in ChemLab.Chem.ApparatusSet.All){
                                var view=Array.Find(FindObjectsOfType<ApparatusView>(),v=>v.Model==apparatus);
                                if(view==null)continue;
                                var screen=camera.WorldToScreenPoint(view.GetComponent<Collider>().bounds.center);
                                File.AppendAllText(logPath,$"APPARATUS {apparatus.Id} {apparatus.Kind} screen={screen.x:F0},{screen.y:F0},{screen.z:F2}{Environment.NewLine}");
                            }
                            foreach(var item in ChemLab.Chem.TestSet.All){
                                var view=Array.Find(FindObjectsOfType<TestItemView>(),v=>v.Model==item);
                                if(view==null)continue;
                                var screen=camera.WorldToScreenPoint(view.GetComponent<Collider>().bounds.center);
                                File.AppendAllText(logPath,$"TESTITEM {item.Id} {item.Kind} screen={screen.x:F0},{screen.y:F0},{screen.z:F2}{Environment.NewLine}");
                            }
                        }
                        break;
                    case "PICK" when words.Length==3:
                        var p=new Vector3(float.Parse(words[1],CultureInfo.InvariantCulture),float.Parse(words[2],CultureInfo.InvariantCulture),0);
                        var hit=LabPick.Pick(p,LabBootstrap.Instance.Cam);
                        File.AppendAllText(logPath,$"PICK {p.x:F0},{p.y:F0} => {hit.vessel?.Id ?? "-"} {hit.Label} apparatus={hit.apparatus?.Id ?? "-"} test={hit.testItem?.Id ?? "-"}{Environment.NewLine}");
                        break;
                    case "FINDPICK" when words.Length==2:
                        var pickCamera=LabBootstrap.Instance?.Cam;
                        var pickEvent=EventSystem.current;
                        var pickVessel=ChemLab.Chem.Lab.Vessels.Find(x=>x.Id==words[1]);
                        var pickApp=ChemLab.Chem.ApparatusSet.All.Find(x=>x.Id==words[1]);
                        var pickTest=ChemLab.Chem.TestSet.All.Find(x=>x.Id==words[1]);
                        if(pickCamera==null||pickEvent==null||pickVessel==null&&pickApp==null&&pickTest==null){
                            File.AppendAllText(logPath,$"FINDPICK {words[1]} unavailable{Environment.NewLine}");break;
                        }
                        var pickSeeds=new List<Vector2>();
                        if(pickVessel!=null){
                            var v=Array.Find(FindObjectsOfType<VesselView>(),x=>x.Model==pickVessel);
                            if(v!=null)foreach(var col in v.GetComponentsInChildren<Collider>())
                                pickSeeds.Add(pickCamera.WorldToScreenPoint(col.bounds.center));
                        } else if(pickApp!=null){
                            var a=Array.Find(FindObjectsOfType<ApparatusView>(),x=>x.Model==pickApp);
                            if(a!=null)foreach(var col in a.GetComponentsInChildren<Collider>())
                                pickSeeds.Add(pickCamera.WorldToScreenPoint(col.bounds.center));
                        } else {
                            var t=Array.Find(FindObjectsOfType<TestItemView>(),x=>x.Model==pickTest);
                            if(t!=null)pickSeeds.Add(pickCamera.WorldToScreenPoint(t.GetComponent<Collider>().bounds.center));
                        }
                        Vector2 foundPoint=Vector2.zero;bool found=false;int uiBlocks=0,hitSamples=0;
                        foreach(var seed in pickSeeds){
                            if(found)break;
                            for(int radius=0;radius<=84&&!found;radius+=6){
                                int samples=radius==0?1:Mathf.Max(12,Mathf.CeilToInt(2f*Mathf.PI*radius/10f));
                                for(int i=0;i<samples;i++){
                                    float angle=2f*Mathf.PI*i/samples;
                                    var point=seed+new Vector2(radius*Mathf.Cos(angle),radius*Mathf.Sin(angle));
                                    // The pipe script sends integer pixels. Validate that exact
                                    // pixel, not a fractional neighbour near an alpha/collider edge.
                                    point=new Vector2(Mathf.Round(point.x),Mathf.Round(point.y));
                                    if(point.x<2||point.x>Screen.width-3||point.y<2||point.y>Screen.height-3)continue;
                                    var selected=LabPick.Pick(point,pickCamera);
                                    if(selected.vessel!=pickVessel||selected.apparatus!=pickApp||selected.testItem!=pickTest)continue;
                                    hitSamples++;
                                    var pointer=new PointerEventData(pickEvent){position=point};
                                    var uiHits=new List<RaycastResult>();pickEvent.RaycastAll(pointer,uiHits);
                                    if(uiHits.Count>0){uiBlocks++;continue;}
                                    foundPoint=point;found=true;break;
                                }
                            }
                        }
                        File.AppendAllText(logPath,found
                            ?$"FINDPICK {words[1]} at={foundPoint.x:F0},{foundPoint.y:F0} uiBlocked={uiBlocks} hits={hitSamples}{Environment.NewLine}"
                            :$"FINDPICK {words[1]} none uiBlocked={uiBlocks} hits={hitSamples}{Environment.NewLine}");
                        break;
                    case "DRAGSPLINT" when words.Length==5:
                        var fromItem=ChemLab.Chem.TestSet.All.Find(x=>x.Id==words[1]&&x.Kind==ChemLab.Chem.TestItemKind.Splint);
                        var targetBurner=ChemLab.Chem.ApparatusSet.All.Find(x=>x.Id==words[2]&&x.Kind==ChemLab.Chem.ApparatusKind.BunsenBurner);
                        var grabPoint=new Vector3(float.Parse(words[3],CultureInfo.InvariantCulture),float.Parse(words[4],CultureInfo.InvariantCulture),0);
                        if(fromItem==null||targetBurner==null||LabPick.Pick(grabPoint,LabBootstrap.Instance.Cam).testItem!=fromItem){
                            File.AppendAllText(logPath,$"DRAGSPLINT blocked{Environment.NewLine}");break;
                        }
                        var grabPlane=new Plane(Vector3.up,Vector3.zero);
                        var grabRay=LabBootstrap.Instance.Cam.ScreenPointToRay(grabPoint);
                        if(!grabPlane.Raycast(grabRay,out float grabDistance))break;
                        var itemOffset=grabRay.GetPoint(grabDistance)-fromItem.Pos;
                        var dropPoint=LabBootstrap.Instance.Cam.WorldToScreenPoint(targetBurner.Pos+itemOffset);
                        File.AppendAllText(logPath,$"DRAGSPLINT {fromItem.Id}->{targetBurner.Id} to={dropPoint.x:F0},{dropPoint.y:F0}{Environment.NewLine}");
                        break;
                    case "DRAGPOINT" when words.Length==5:
                        ChemLab.Chem.Vessel from=null,to=null;
                        foreach(var vessel in ChemLab.Chem.Lab.Vessels){if(vessel.Id==words[1])from=vessel;if(vessel.Id==words[2])to=vessel;}
                        var start=new Vector3(float.Parse(words[3],CultureInfo.InvariantCulture),float.Parse(words[4],CultureInfo.InvariantCulture),0);
                        var grab=LabPick.Pick(start,LabBootstrap.Instance.Cam);
                        if(from==null||to==null||grab.vessel!=from){File.AppendAllText(logPath,$"DRAGPOINT blocked hit={grab.vessel?.Id ?? "-"}{Environment.NewLine}");break;}
                        var plane=new Plane(Vector3.up,Vector3.up*grab.world.y);
                        var ray=LabBootstrap.Instance.Cam.ScreenPointToRay(start);
                        if(!plane.Raycast(ray,out float along))break;
                        var offset=ray.GetPoint(along)-from.Pos;
                        var end=LabBootstrap.Instance.Cam.WorldToScreenPoint(to.Pos+offset);
                        File.AppendAllText(logPath,$"DRAGPOINT {from.Id}->{to.Id} from={start.x:F0},{start.y:F0} to={end.x:F0},{end.y:F0} height={grab.world.y:F2}{Environment.NewLine}");
                        break;
                    case "BUTTONS":
                        foreach(var button in FindObjectsOfType<Button>())if(button.name.StartsWith("Btn_")){
                            var rect=button.GetComponent<RectTransform>();var center=rect.TransformPoint(rect.rect.center);
                            File.AppendAllText(logPath,$"BUTTON {button.name} {center.x:F0},{center.y:F0}{Environment.NewLine}");
                        }
                        break;
                    case "REAGENT" when words.Length==2:
                        foreach(var drag in FindObjectsOfType<ReagentDrag>())if(drag.SubstanceId==words[1]){
                            var rect=drag.GetComponent<RectTransform>();var center=rect.TransformPoint(rect.rect.center);
                            File.AppendAllText(logPath,$"REAGENT {drag.SubstanceId} {center.x:F0},{center.y:F0}{Environment.NewLine}");
                            break;
                        }
                        break;
                    case "DOSE":
                        var ui=FindObjectOfType<LabUI>();
                        File.AppendAllText(logPath,$"DOSE open={ui?.DoseDialogOpen} drop={ui?.DropTarget?.Id ?? "-"} target={ui?.DoseTargetForTest} substance={ui?.DoseSubstanceForTest} mol={ui?.DoseMolForTest} error={ui?.DoseErrorForTest}{Environment.NewLine}");
                        var drop=Array.Find(FindObjectsOfType<VesselView>(),v=>v.Model==ui?.DropTarget);
                        if(drop!=null)File.AppendAllText(logPath,$"DOSE highlight={drop.TestFocusGlow:F3}{Environment.NewLine}");
                        var input=Array.Find(FindObjectsOfType<InputField>(),f=>f.transform.parent!=null&&f.transform.parent.name=="DosePanel");
                        if(input!=null)File.AppendAllText(logPath,$"DOSE input={input.text}{Environment.NewLine}");
                        break;
                    case "CHEM" when words.Length==3:
                        foreach(var vessel in ChemLab.Chem.Lab.Vessels)if(vessel.Id==words[1]){
                            File.AppendAllText(logPath,$"CHEM {vessel.Id} {words[2]}={vessel.Get(words[2]):G9} water={vessel.Water:F3} volume={vessel.Volume():F3} temp={vessel.Temp:F2} heating={vessel.Heating}{Environment.NewLine}");
                            break;
                        }
                        break;
                    case "CAPTURE" when words.Length==2:
                        if(!Regex.IsMatch(words[1],@"^[A-Za-z0-9_-]{1,48}$"))throw new ArgumentException("Invalid capture name");
                        var capturePath=Path.GetFullPath("Logs/window-target-"+words[1]+".png");
                        // Capture the real completed frame, including every overlay
                        // (pet, menu, modal), rather than only the main laboratory UI.
                        StartCoroutine(CaptureFrame(capturePath));
                        break;
                    case "DOSETEXT" when words.Length==2:
                        foreach(var field in FindObjectsOfType<InputField>())if(field.transform.parent!=null&&field.transform.parent.name=="DosePanel"){
                            field.text=words[1];File.AppendAllText(logPath,$"DOSETEXT {field.text}{Environment.NewLine}");break;
                        }
                        break;
                    case "CONTROLS":
                        foreach(var dial in FindObjectsOfType<FlowDial>()){
                            var rect=dial.GetComponent<RectTransform>();var center=rect.TransformPoint(rect.rect.center);
                            File.AppendAllText(logPath,$"DIAL {center.x:F0},{center.y:F0}{Environment.NewLine}");
                        }
                        break;
                    case "MENU":
                        var menu=PetMenu.CurrentPanel;
                        if(menu==null){File.AppendAllText(logPath,$"MENU closed{Environment.NewLine}");break;}
                        var upperLeft=menu.TransformPoint(new Vector3(menu.rect.xMin,menu.rect.yMax));
                        var lowerRight=menu.TransformPoint(new Vector3(menu.rect.xMax,menu.rect.yMin));
                        File.AppendAllText(logPath,$"MENU open upperLeft={upperLeft.x:F0},{upperLeft.y:F0} lowerRight={lowerRight.x:F0},{lowerRight.y:F0}{Environment.NewLine}");
                        foreach(var button in menu.GetComponentsInChildren<Button>()){
                            var caption=button.GetComponentInChildren<Text>()?.text;
                            var rect=button.GetComponent<RectTransform>();var center=rect.TransformPoint(rect.rect.center);
                            File.AppendAllText(logPath,$"MENUITEM {caption} at={center.x:F0},{center.y:F0}{Environment.NewLine}");
                        }
                        break;
                    case "CALC":
                        var calc=EnthalpyScene.Instance;
                        File.AppendAllText(logPath,calc==null?$"CALC absent{Environment.NewLine}":
                            $"CALCSTART{Environment.NewLine}{calc.StateAudit()}{calc.AuditLayout()}CALCEND{Environment.NewLine}");
                        break;
                    case "CALCTEXT" when words.Length==2:
                        foreach(var field in FindObjectsOfType<InputField>())
                            if(field.GetComponentInParent<RectTransform>()!=null&&field.transform.parent!=null&&field.transform.parent.name=="ValueDlg"){
                                field.text=words[1];
                                File.AppendAllText(logPath,$"CALCTEXT {field.text}{Environment.NewLine}");
                                break;
                            }
                        break;
                    case "TAGS":
                        var tagCamera=LabBootstrap.Instance?.Cam;
                        var tagUi=FindObjectOfType<LabUI>();
                        if(tagUi!=null){var workspace=tagUi.TagWorkspaceRect();
                            File.AppendAllText(logPath,$"WORKSPACE {workspace.xMin:F0},{workspace.yMin:F0},{workspace.xMax:F0},{workspace.yMax:F0}{Environment.NewLine}");}
                        foreach(var tag in FindObjectsOfType<VesselTag>()){
                            var r=tag.ScreenRect;
                            File.AppendAllText(logPath,$"TAG {tag.Model?.Id} {r.xMin:F0},{r.yMin:F0},{r.xMax:F0},{r.yMax:F0} offset={tag.ScreenOffset.x:F0},{tag.ScreenOffset.y:F0}{Environment.NewLine}");
                        }
                        if(tagCamera!=null)foreach(var eq in FindObjectsOfType<EquationTag>()){
                            var r=eq.ScreenRect;
                            File.AppendAllText(logPath,$"EQ {eq.Ev?.Vessel?.Id ?? "-"} {r.xMin:F0},{r.yMin:F0},{r.xMax:F0},{r.yMax:F0} offset={eq.ScreenOffset.x:F0},{eq.ScreenOffset.y:F0}{Environment.NewLine}");
                        }
                        break;
                }
            }catch(Exception e){File.AppendAllText(logPath,$"BAD {line}: {e.GetType().Name}{Environment.NewLine}");}
        }
    }
    void StopReader(){
        if(stopping)return;
        stopping=true;stopReader.Set();
        if(reader!=null&&!reader.Join(1000))Debug.LogWarning("Window input reader did not stop within one second");
    }
    void OnApplicationQuit(){StopReader();}
    void OnDestroy(){
        StopReader();
        if(current==this)current=null;
    }
}

public static class LabInput {
    // While the developer console owns the input, the laboratory must not see
    // pointer or key events at all: no camera pan, no dragging, no menu keys and
    // no dialog shortcuts. The console reads the real device itself, so it keeps
    // working while every LabInput consumer goes quiet.
    static bool Suppressed=>DeveloperConsole.InputCaptured||StartupBranding.Active;
    public static Vector3 mousePosition=>WindowTargetInput.Active?(Vector3)WindowTargetInput.Position:Input.mousePosition;
    public static bool GetMouseButton(int i)=>!Suppressed&&(WindowTargetInput.Active?WindowTargetInput.Button(i):Input.GetMouseButton(i));
    public static bool GetMouseButtonDown(int i)=>!Suppressed&&(WindowTargetInput.Active?WindowTargetInput.Pressed(i):Input.GetMouseButtonDown(i));
    public static bool GetMouseButtonUp(int i)=>!Suppressed&&(WindowTargetInput.Active?WindowTargetInput.Released(i):Input.GetMouseButtonUp(i));
    public static bool GetKey(KeyCode k)=>!Suppressed&&(WindowTargetInput.Active?WindowTargetInput.Key(k):Input.GetKey(k));
    public static bool GetKeyDown(KeyCode k)=>!Suppressed&&(WindowTargetInput.Active?WindowTargetInput.KeyDown(k):Input.GetKeyDown(k));
    public static float GetAxis(string axis)=>Suppressed?0f:(WindowTargetInput.Active?(axis=="Mouse ScrollWheel"?WindowTargetInput.Scroll.y:0):Input.GetAxis(axis));
}

public sealed class WindowTargetBaseInput : BaseInput {
    public override bool mousePresent=>true;
    public override bool touchSupported=>false;
    public override Vector2 mousePosition=>WindowTargetInput.Position;
    public override Vector2 mouseScrollDelta=>WindowTargetInput.Scroll;
    public override bool GetMouseButton(int i)=>WindowTargetInput.Button(i);
    public override bool GetMouseButtonDown(int i)=>WindowTargetInput.Pressed(i);
    public override bool GetMouseButtonUp(int i)=>WindowTargetInput.Released(i);
    public override float GetAxisRaw(string axis)=>0;
    public override bool GetButtonDown(string button)=>button=="Submit"&&WindowTargetInput.KeyDown(KeyCode.Return);
}

// StandaloneInputModule normally ignores all pointer events when its EventSystem
// loses OS focus. This test-only EventSystem keeps processing the pipe input.
public sealed class WindowTargetEventSystem : EventSystem {
    protected override void OnApplicationFocus(bool hasFocus) { }
}
}
