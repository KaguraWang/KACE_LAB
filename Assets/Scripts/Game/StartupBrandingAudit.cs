using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.Game {
public sealed class StartupBrandingAudit:MonoBehaviour {
    readonly List<string> lines=new List<string>();int passed,failed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-startupBrandTest")>=0)new GameObject("Startup branding audit").AddComponent<StartupBrandingAudit>();}
    void Check(string label,bool ok){lines.Add((ok?"PASS ":"FAIL ")+label);if(ok)passed++;else failed++;}
    IEnumerator Start(){
        var opening=StartupBranding.Instance;
        Check("production opening is active",opening!=null&&StartupBranding.Active);
        if(opening!=null){
            Check("provided artwork loads",opening.Logo.texture!=null);
            Check("experiment clock paused without changing rate",WorldClock.Frozen&&WorldClock.DeltaTime==0&&WorldClock.Rate==1);
            Check("white background",opening.transform.Find("White backdrop").GetComponent<Image>().color==Color.white);
            float deadline=Time.realtimeSinceStartup+8;
            while(opening!=null&&opening.Elapsed<.3f&&Time.realtimeSinceStartup<deadline)yield return null;
            Check("actual fade in has intermediate alpha",opening!=null&&opening.Logo.color.a>0&&opening.Logo.color.a<1);
            WindowTargetInput.InjectForTest("CAPTURE brand-fade-in");yield return null;yield return null;
            WindowTargetInput.InjectForTest("KEYDOWN BackQuote");yield return null;yield return null;
            WindowTargetInput.InjectForTest("KEYUP BackQuote");
            Check("console hotkey blocked during opening",DeveloperConsole.Instance!=null&&!DeveloperConsole.Instance.IsOpen);
            while(opening!=null&&opening.Elapsed<1f&&Time.realtimeSinceStartup<deadline)yield return null;
            Check("hold is fully visible",opening!=null&&opening.Logo.color.a==1);
            if(opening!=null){
                var r=opening.Logo.rectTransform;Vector2 center=r.TransformPoint(r.rect.center);
                Check("artwork is centered",Vector2.Distance(center,new Vector2(Screen.width*.5f,Screen.height*.5f))<1);
                Check("artwork keeps original aspect",Mathf.Abs(r.rect.width/r.rect.height-(float)opening.Logo.texture.width/opening.Logo.texture.height)<.001f);
                Check("artwork fits screen",r.rect.width<Screen.width&&r.rect.height<Screen.height);
            }
            WindowTargetInput.InjectForTest("CAPTURE brand-hold");yield return null;yield return null;
            while(opening!=null&&opening.Elapsed<2.4f&&Time.realtimeSinceStartup<deadline)yield return null;
            Check("actual fade out has intermediate alpha",opening!=null&&opening.Logo.color.a>0&&opening.Logo.color.a<1);
            WindowTargetInput.InjectForTest("CAPTURE brand-fade-out");yield return null;yield return null;
            while(StartupBranding.Active&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;
            Check("opening cleans up and clock resumes",!StartupBranding.Active&&StartupBranding.Instance==null&&!WorldClock.Frozen&&WorldClock.DeltaTime>0);
        }
        var mark=GameObject.Find("KaceLabWordmark")?.GetComponent<RawImage>();
        Check("top navigation uses generated image",mark!=null&&mark.texture!=null&&mark.GetComponent<Text>()==null&&!mark.raycastTarget);
        WindowTargetInput.InjectForTest("CAPTURE brand-navigation");for(int i=0;i<5;i++)yield return null;
        WindowTargetInput.InjectForTest("KEYDOWN BackQuote");yield return null;yield return null;
        WindowTargetInput.InjectForTest("KEYUP BackQuote");yield return null;
        Check("console hotkey works after opening",DeveloperConsole.Instance!=null&&DeveloperConsole.Instance.IsOpen);
        lines.Add($"TOTAL {passed} passed, {failed} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/startup-brand-audit.txt",lines);
        Application.Quit(failed==0?0:1);
    }
}
}
