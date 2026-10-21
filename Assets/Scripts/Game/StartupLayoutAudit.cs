using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
public sealed class StartupLayoutAudit:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-startupLayoutTest")>=0)new GameObject("Startup layout audit").AddComponent<StartupLayoutAudit>();}
 IEnumerator Start(){
  Application.runInBackground=true;yield return null;yield return null;
  for(int i=0;i<20;i++)yield return null;
  WindowTargetInput.InjectForTest("CAPTURE startup-layout");yield return null;yield return null;
  var host=LabBootstrap.Instance;host.enabled=false;var driver=DavidDriver.Instance;
  var lines=new List<string>();int failures=0;
  void Check(string s,bool ok){lines.Add((ok?"PASS ":"FAIL ")+s);if(!ok)failures++;}
  var sources=new List<Transform>();
  foreach(var v in host.GetComponentsInChildren<ApparatusView>())sources.Add(v.transform);
  foreach(var v in host.GetComponentsInChildren<TestItemView>())sources.Add(v.transform);
  DavidMode.TrySet("1 0",out _);
  foreach(var source in sources){
   const float height=.35f;Vector3 pose=new Vector3(0,height,0);float lift=height;bool flat=true;
   driver.TestSeed(123);driver.Begin(source,(p,l)=>{pose=p;lift=l;},()=>true,Vector3.zero,height,Vector3.zero,height);
   for(int i=0;i<120;i++){driver.SetIntent(new Vector3(.1f,0f,0),lift+.1f);flat&=Mathf.Abs(pose.y-height)<1e-5f&&Mathf.Abs(lift-height)<1e-5f;yield return null;}
   bool arrived=false;driver.BeginFinish(()=>arrived=true);
   for(int i=0;i<120&&!arrived;i++){yield return null;flat&=Mathf.Abs(pose.y-height)<1e-5f;}
   Check(source.name+" horizontal dashes and final return",flat&&arrived&&Mathf.Abs(pose.x-.1f)<1e-4);
   driver.CancelAndClear();
  }
  DavidMode.ResetForTest();Check("all five auxiliary instruments covered",sources.Count==5);
  Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/startup-layout-audit.txt",lines);
  Application.Quit(failures==0?0:1);
 }
}
}
