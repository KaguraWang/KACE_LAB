using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;

namespace ChemLab.Game {
// Independent visible-text acceptance: no calls to the translator's own audit.
// The language button is clicked through the private Player input stream.
public sealed class BilingualUiAudit : MonoBehaviour {
    readonly List<string> lines = new List<string>();
    readonly HashSet<string> missing = new HashSet<string>();
    readonly HashSet<string> seenReagentCards = new HashSet<string>();
    int passed, failed;
    bool preferenceCaptured, hadLanguagePreference;
    int originalLanguagePreference;
    readonly Dictionary<string,int?> progressPreferences=new Dictionary<string,int?>();
    void RestorePreference() {
        if(!preferenceCaptured)return;
        if(hadLanguagePreference)PlayerPrefs.SetInt("chemlab.lang",originalLanguagePreference);
        else PlayerPrefs.DeleteKey("chemlab.lang");
        foreach(var entry in progressPreferences)
            if(entry.Value.HasValue)PlayerPrefs.SetInt(entry.Key,entry.Value.Value);else PlayerPrefs.DeleteKey(entry.Key);
        PlayerPrefs.Save();preferenceCaptured=false;
    }
    void OnDestroy() { RestorePreference(); }
    void OnApplicationQuit() { RestorePreference(); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch() {
        if(Array.IndexOf(Environment.GetCommandLineArgs(), "-bilingualUiTest") < 0) return;
        Application.runInBackground = true;
        new GameObject("Bilingual UI audit").AddComponent<BilingualUiAudit>();
    }
    void Check(string label, bool ok) {
        lines.Add((ok ? "PASS " : "FAIL ") + label);
        if(ok) passed++; else failed++;
    }
    IEnumerator Frames(int count) { for(int i=0;i<count;i++) yield return null; }
    IEnumerator Capture(string name) {
        WindowTargetInput.InjectForTest("CAPTURE bilingual-"+name);
        yield return Frames(5);
    }
    IEnumerator ClickLanguage() {
        var button=FindObjectsOfType<Button>().FirstOrDefault(x=>x.name=="LangBtn");
        Check("language button is available",button!=null && WindowTargetInput.Active);
        if(button==null || !WindowTargetInput.Active) yield break;
        var canvas=button.GetComponentInParent<Canvas>();
        var rt=(RectTransform)button.transform;
        Vector2 p=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rt.TransformPoint(rt.rect.center));
        WindowTargetInput.InjectForTest("MOVE "+Mathf.RoundToInt(p.x)+" "+Mathf.RoundToInt(p.y));
        yield return Frames(2);
        WindowTargetInput.InjectForTest("DOWN 0");yield return Frames(2);
        WindowTargetInput.InjectForTest("UP 0");yield return new WaitForSeconds(.55f);
    }
    void Sample(string context, bool english) {
        Canvas.ForceUpdateCanvases();
        int visible=0, untranslated=0, overflow=0;
        foreach(var field in FindObjectsOfType<InputField>()){
            if(!field.isActiveAndEnabled||field.targetGraphic==null)continue;
            var actual=(Color32)field.targetGraphic.color;
            Check(context+" input background #39C5BB "+PathOf(field.transform),actual.r==0x39&&actual.g==0xC5&&actual.b==0xBB);
        }
        foreach(var text in FindObjectsOfType<Text>()) {
            if(!text.isActiveAndEnabled || text.canvasRenderer.cull || string.IsNullOrWhiteSpace(text.text)) continue;
            var canvas=text.GetComponentInParent<Canvas>();if(canvas==null) continue;
            var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            Vector2 a=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);
            Vector2 b=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
            if(Mathf.Max(a.x,b.x)<0 || Mathf.Min(a.x,b.x)>Screen.width || Mathf.Max(a.y,b.y)<0 || Mathf.Min(a.y,b.y)>Screen.height)continue;
            visible++;
            var ink=text.color;
            bool dark=ink.r<=.3f&&ink.g<=.3f&&ink.b<=.3f;
            bool errorInk=ink.r<=.75f&&ink.r>.35f&&ink.g<.2f&&ink.b<.2f;
            bool secondaryInk=((Color32)ink).Equals((Color32)LabUiTheme.Hint);
            if(!dark&&!errorInk&&!secondaryInk)lines.Add("PALETTE "+context+" | "+PathOf(text.transform)+" | "+ColorUtility.ToHtmlStringRGBA(ink));
            if(context.StartsWith("en-rack-",StringComparison.Ordinal))
                for(Transform ancestor=text.transform;ancestor!=null;ancestor=ancestor.parent)
                    if(ancestor.name.StartsWith("R_",StringComparison.Ordinal)) {seenReagentCards.Add(ancestor.name.Substring(2));break;}
            string raw=text.text;
            lines.Add("TEXT "+context+" | "+PathOf(text.transform)+" | "+raw.Replace('\n','|'));
            var input=text.GetComponentInParent<InputField>();
            if(input!=null && input.textComponent==text) continue; // User input is not interface prose.
            string cleaned=Regex.Replace(raw,"<[^>]+>","").Replace("神楽風和","").Replace("支持風和谢谢喵","");
            // The language selector intentionally advertises Chinese in English mode.
            bool languageSelector=text.GetComponentInParent<Button>()?.name=="LangBtn";
            if(languageSelector)Check(context+" language selector names the destination language",cleaned==(english?"中文":"EN"));
            if(english && !(languageSelector&&cleaned=="中文") && Regex.IsMatch(cleaned,@"[\u3400-\u9fff]")) {
                untranslated++;
                string entry=context+" | "+PathOf(text.transform)+" | "+raw.Replace('\n','|');
                if(missing.Add(entry))lines.Add("UNTRANSLATED "+entry);
            }
            float preferredHeight=text.preferredHeight;
            if(text.resizeTextForBestFit && text.cachedTextGenerator.fontSizeUsedForBestFit>0) {
                var settings=text.GetGenerationSettings(text.rectTransform.rect.size);
                settings.resizeTextForBestFit=false;
                // The generator reports pixel-scaled font size; generation settings
                // apply Canvas scale again. Convert back to local font units first.
                settings.fontSize=Mathf.Clamp(Mathf.RoundToInt(text.cachedTextGenerator.fontSizeUsedForBestFit/text.pixelsPerUnit),text.resizeTextMinSize,text.resizeTextMaxSize);
                using(var generator=new TextGenerator())preferredHeight=generator.GetPreferredHeight(raw,settings)/text.pixelsPerUnit;
            }
            if(text.verticalOverflow==VerticalWrapMode.Truncate && preferredHeight>text.rectTransform.rect.height+2f) {
                overflow++;lines.Add("OVERFLOW "+context+" | "+PathOf(text.transform)+" | height="+preferredHeight+" available="+text.rectTransform.rect.height+" | "+raw.Replace('\n','|'));
            }
            if(text.horizontalOverflow==HorizontalWrapMode.Overflow && text.preferredWidth>text.rectTransform.rect.width+2f) {
                overflow++;lines.Add("WIDTH_OVERFLOW "+context+" | "+PathOf(text.transform)+" | width="+text.preferredWidth+" available="+text.rectTransform.rect.width+" | "+raw.Replace('\n','|'));
            }
        }
        Check(context+" has visible text",visible>0);
        if(english) Check(context+" has no unintended Chinese",untranslated==0);
        Check(context+" has no measured text overflow",overflow==0);
        lines.Add("METRIC "+context+" visible="+visible+" untranslated="+untranslated+" heightOverflow="+overflow);
    }
    static string PathOf(Transform t) { return t.parent==null?t.name:PathOf(t.parent)+"/"+t.name; }
    IEnumerator Start() {
        yield return new WaitForSeconds(2f);
        hadLanguagePreference=PlayerPrefs.HasKey("chemlab.lang");
        originalLanguagePreference=PlayerPrefs.GetInt("chemlab.lang",0);
        preferenceCaptured=true;
        foreach(string key in new[]{"anon.prog.lab","anon.prog.labSkip","anon.prog.calc","anon.prog.chat"})
            progressPreferences[key]=PlayerPrefs.HasKey(key)?(int?)PlayerPrefs.GetInt(key):null;
        var ui=FindObjectOfType<LabUI>();var lab=LabBootstrap.Instance;
        Check("live laboratory exists",ui!=null && lab!=null);
        if(ui!=null && lab!=null) {
            WorldClock.Set(0);
            if(Localization.English) yield return ClickLanguage();
            Check("Chinese baseline",!Localization.English);Sample("zh-baseline",false);
            yield return ClickLanguage();Check("real button switches to English",Localization.English);
            Sample("en-baseline",true);
            WindowTargetInput.InjectForTest("CAPTURE bilingual-baseline");yield return Frames(5);
            foreach(string category in new[]{"metal","nonmetal","oxide","acid","base","salt","organic","indicator","other"}) {
                ui.TestSetFilter(category,"");yield return new WaitForSeconds(.4f);
                Sample("en-rack-"+category,true);
                var rack=FindObjectsOfType<ScrollRect>().FirstOrDefault(s=>s.name=="Scroll" && s.transform.parent.name=="RightPanel");
                Check(category+" reagent scroll is available",rack!=null);
                if(rack!=null) {
                    Canvas.ForceUpdateCanvases();
                    float viewportHeight=rack.viewport.rect.height;
                    int pages=Mathf.Max(1,Mathf.CeilToInt(rack.content.rect.height/Mathf.Max(1,viewportHeight*.8f)));
                    for(int page=1;page<=pages;page++) {
                        rack.verticalNormalizedPosition=1f-(float)page/pages;
                        yield return Frames(3);Sample("en-rack-"+category+"-page-"+page,true);
                    }
                }
            }
            Check("every reagent card sampled on screen",seenReagentCards.Count==ui.TestReagentTotal);
            lines.Add("METRIC reagent coverage seen="+seenReagentCards.Count+" expected="+ui.TestReagentTotal);
            // Create missing kinds only inside this disposable audit Player.
            foreach(VesselKind kind in Enum.GetValues(typeof(VesselKind)))
                if(!Lab.Vessels.Any(v=>v.Kind==kind)) Lab.AddVessel(kind);
            foreach(var vessel in Lab.Vessels.ToArray()) {
                lab.ShowObjectMenu(new LabPick.Hit{vessel=vessel},new Vector2(Screen.width*.5f,Screen.height*.65f));
                yield return new WaitForSeconds(.4f);Sample("en-menu-"+vessel.Kind,true);
                WindowTargetInput.InjectForTest("KEYDOWN Escape");yield return Frames(2);
                WindowTargetInput.InjectForTest("KEYUP Escape");yield return Frames(2);
                if(vessel.Kind==VesselKind.Bottle) {
                    vessel.SetSealed(true);ui.SelectExternal(vessel);yield return new WaitForSeconds(.5f);
                    Sample("en-sealed-bottle",true);
                    Lang.Set(false);yield return new WaitForSeconds(.5f);Sample("zh-sealed-bottle",false);
                    Lang.Set(true);yield return new WaitForSeconds(.5f);Sample("en-sealed-bottle-repeat",true);
                    vessel.SetSealed(false);
                }
            }
            foreach(var apparatus in ApparatusSet.All.ToArray()) {
                lab.ShowObjectMenu(new LabPick.Hit{apparatus=apparatus},new Vector2(Screen.width*.5f,Screen.height*.65f));
                yield return new WaitForSeconds(.5f);Sample("en-apparatus-"+apparatus.Kind,true);
                PetMenu.Close();
                if(apparatus.Kind==ApparatusKind.BunsenBurner) {
                    ui.ShowBurnerDialog(apparatus);yield return new WaitForSeconds(.5f);
                    Sample("en-burner-dialog",true);yield return Capture("burner");ui.CloseBurnerDialog();
                }
            }
            foreach(var item in TestSet.All.ToArray()) {
                lab.ShowObjectMenu(new LabPick.Hit{testItem=item},new Vector2(Screen.width*.5f,Screen.height*.65f));
                yield return new WaitForSeconds(.5f);Sample("en-test-item-"+item.Kind,true);PetMenu.Close();
            }
            foreach(string reagent in new[]{"h2o","nacl","h2"}) {
                ui.ShowDoseDialog(Lab.Vessels.First(),reagent);yield return new WaitForSeconds(.5f);
                Sample("en-dose-"+reagent,true);
                WindowTargetInput.InjectForTest("CAPTURE bilingual-dose-"+reagent);yield return Frames(5);
                var amount=FindObjectsOfType<InputField>().FirstOrDefault(i=>i.name=="DoseAmount");
                Check("dose input exists "+reagent,amount!=null);
                if(amount!=null)amount.text="0.125";
                // Modal blocks the top bar. Exercise the language service on the existing modal,
                // separately from the real language-button input checks above and below.
                Lang.Set(false);yield return new WaitForSeconds(.5f);Sample("zh-open-dose-"+reagent,false);
                Lang.Set(true);yield return new WaitForSeconds(.5f);Sample("en-open-dose-"+reagent,true);
                Check("language switch preserves entered amount "+reagent,amount!=null && amount.text=="0.125");
                ui.CloseDose();
            }
            ui.TestSetFilter("metal","");
            yield return ClickLanguage();Check("real button returns to Chinese",!Localization.English);Sample("zh-return",false);
            Check("existing log returns to Chinese after English rebuild",
                FindObjectsOfType<Text>().Any(t=>PathOf(t.transform).Contains("LogScroll/Content") && t.text.Contains("实验台已就绪")));
            yield return ClickLanguage();Check("second English switch",Localization.English);Sample("en-repeat",true);
            var pourSource=Lab.AddVessel(VesselKind.Beaker);
            var pourTarget=Lab.AddVessel(VesselKind.Beaker);
            pourSource.AddWater(20);LabBootstrap.Instance.SyncViews();yield return Frames(3);
            ui.BeginStagedPour(pourSource,pourTarget);yield return new WaitForSeconds(1.2f);
            Check("staged pour display opened",ui.IsPouring);Sample("en-pour-percent",true);
            ui.SetPourUnits(true);yield return new WaitForSeconds(.5f);Sample("en-pour-grams",true);
            WindowTargetInput.InjectForTest("CAPTURE bilingual-pour");yield return Frames(5);
            WindowTargetInput.InjectForTest("KEYDOWN Escape");yield return Frames(2);
            WindowTargetInput.InjectForTest("KEYUP Escape");
            float pourDeadline=Time.realtimeSinceStartup+12;
            while(ui.IsPouring && Time.realtimeSinceStartup<pourDeadline)yield return null;
            Check("staged pour cancellation restores display",!ui.IsPouring);
            foreach(bool reflux in new[]{false,true}) {
                DistillationView.CreateKit(reflux);yield return new WaitForSeconds(.5f);
                var source=Lab.Vessels.LastOrDefault(v=>v.Distillation!=null && v.Distillation.Reflux==reflux);
                Check("thermal audit assembly exists "+reflux,source!=null);
                if(source==null)continue;
                lab.ShowObjectMenu(new LabPick.Hit{vessel=source},new Vector2(Screen.width*.5f,Screen.height*.65f));
                yield return new WaitForSeconds(.5f);Sample("en-rig-menu-"+reflux,true);PetMenu.Close();
                foreach(bool explanation in new[]{false,true}) {
                    ui.ShowThermalLedger(source.Distillation,explanation);yield return new WaitForSeconds(.5f);
                    Sample("en-thermal-"+reflux+"-"+explanation,true);
                    yield return Capture("thermal-"+reflux+"-"+explanation);
                    ui.CloseThermalLedger();
                }
            }
            WindowTargetInput.InjectForTest("KEYDOWN BackQuote");yield return Frames(2);
            WindowTargetInput.InjectForTest("KEYUP BackQuote");yield return new WaitForSeconds(.5f);
            Check("console opens through input",DeveloperConsole.Instance!=null && DeveloperConsole.Instance.IsOpen);
            Sample("en-console",true);
            WindowTargetInput.InjectForTest("CONSOLETEXT world_speed 3.01");yield return Frames(2);
            WindowTargetInput.InjectForTest("KEYDOWN Return");yield return Frames(2);
            WindowTargetInput.InjectForTest("KEYUP Return");yield return new WaitForSeconds(.5f);
            Sample("en-console-rate-error",true);
            yield return Capture("console-error");
            Check("rejected command preserves paused experiment",WorldClock.Rate==0);
            WindowTargetInput.InjectForTest("KEYDOWN BackQuote");yield return Frames(2);
            WindowTargetInput.InjectForTest("KEYUP BackQuote");yield return new WaitForSeconds(.5f);
            Check("console closes through input",DeveloperConsole.Instance!=null && !DeveloperConsole.Instance.IsOpen);
            var version=VersionInfo.Instance;
            Check("version information exists",version!=null);
            if(version!=null) {
                var point=version.ButtonScreenPosition;
                WindowTargetInput.InjectForTest("MOVE "+Mathf.RoundToInt(point.x)+" "+Mathf.RoundToInt(point.y));
                yield return new WaitForSeconds(.5f);Sample("en-version-info",true);
                yield return Capture("version");
                Check("version popup opens by pointer",version.IsOpen);
                Check("user specified version and credits preserved",version.DisplayText.Contains(VersionInfo.DisplayVersion) && version.DisplayText.Contains("神楽風和") && version.DisplayText.Contains("支持風和谢谢喵"));
                Check("English teacher edition displayed",version.DisplayText.Contains("Teacher Edition"));
                Lang.Set(false);yield return new WaitForSeconds(.5f);Sample("zh-version-info",false);
                Check("Chinese teacher edition displayed",version.DisplayText.Contains("教师版"));
                Lang.Set(true);yield return new WaitForSeconds(.5f);
                WindowTargetInput.InjectForTest("MOVE 600 400");yield return Frames(3);
            }
            // Rendering sweep only: Next bypasses task completion, so this is not a
            // claim that every tutorial exercise was completed through mouse input.
            foreach(TutorialKind kind in Enum.GetValues(typeof(TutorialKind))) {
                PetTutorial.Start(kind);
                int expected=PetTutorial.StepCount;
                var seen=new HashSet<int>();
                while(PetTutorial.Running && seen.Count<=expected) {
                    int step=PetTutorial.StepIndex;
                    if(!seen.Add(step))break;
                    yield return new WaitForSeconds(.55f);
                    Sample("en-tutorial-"+kind+"-"+step,true);
                    PetTutorial.Next();
                }
                Check("all tutorial steps rendered "+kind,expected>0 && seen.Count==expected);
                if(PetTutorial.Running)PetTutorial.Abort();
                yield return new WaitForSeconds(.55f);Sample("en-tutorial-end-"+kind,true);
            }
            var calculator=EnthalpyScene.Instance;
            var edge=calculator!=null?calculator.TestFirstEdge():null;
            Check("calculator edge available for edit-dialog audit",edge!=null);
            if(edge!=null) {
                calculator.AskEnthalpy(edge,true);yield return new WaitForSeconds(.5f);
                Sample("en-calculator-edge-menu",true);yield return Capture("calculator-menu");calculator.CloseMenu();
                calculator.AskManualValue(edge,false);yield return new WaitForSeconds(.5f);
                Sample("en-calculator-value-dialog",true);
                yield return Capture("calculator-value");
                Lang.Set(false);yield return new WaitForSeconds(.5f);Sample("zh-calculator-value-dialog",false);
                Lang.Set(true);yield return new WaitForSeconds(.5f);Sample("en-calculator-value-dialog-repeat",true);
                calculator.CloseMenu();
            }
            // Use an empty in-memory config. Do not load, display, save or send a real API key.
            var savedConfig=PetChat.Cfg;
            PetChat.Cfg=new AiConfig();
            PetChat.Toggle();
            if(PetChat.Instance!=null && !PetChat.Instance.gameObject.activeSelf)PetChat.Toggle();
            yield return new WaitForSeconds(.5f);Sample("en-chat-unconfigured",true);
            yield return Capture("chat");
            var settingsButton=PetChat.Instance==null?null:PetChat.Instance.GetComponentsInChildren<Button>()
                .FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text=="Settings" || b.GetComponentInChildren<Text>()?.text=="设置");
            Check("chat settings button available",settingsButton!=null);
            if(settingsButton!=null) {
                settingsButton.onClick.Invoke();yield return new WaitForSeconds(.5f);Sample("en-chat-settings",true);
                yield return Capture("chat-settings");
                PetMenu.Close();
            }
            const string userMessage="我是阿侬。想要什么实验、想算什么，直接说就行。";
            const string externalReply="实验记录";
            PetChat.Add(ChatMsg.Who.Me,userMessage);
            PetChat.Add(ChatMsg.Who.Anon,externalReply);
            Lang.Set(false);yield return new WaitForSeconds(.5f);
            Lang.Set(true);yield return new WaitForSeconds(.5f);
            var chatTexts=PetChat.Instance==null?new Text[0]:PetChat.Instance.GetComponentsInChildren<Text>();
            Check("user chat message is not translated",chatTexts.Any(t=>t.text.Contains(userMessage)));
            Check("external AI reply is not translated",chatTexts.Any(t=>t.text.Contains(externalReply)));
            if(PetChat.Instance!=null)PetChat.Instance.gameObject.SetActive(false);
            PetChat.Cfg=savedConfig;
        }
        lines.Add("TOTAL "+passed+" passed, "+failed+" failed");
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/bilingual-ui-audit.txt",lines);
        Debug.Log("[BilingualUiAudit] "+lines.Last());
        RestorePreference();
        Application.Quit(failed==0?0:1);
    }
}
}
