using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace ChemLab.Game {
public sealed class ThemeAudit:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Launch(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-themeTest")>=0)new GameObject("Theme audit").AddComponent<ThemeAudit>();}
 IEnumerator Start(){
  Application.runInBackground=true;yield return new WaitForSecondsRealtime(2);
  bool existed=PlayerPrefs.HasKey(LabAppearance.PreferenceKey);int original=PlayerPrefs.GetInt(LabAppearance.PreferenceKey);
  var lines=new List<string>();int failures=0;
  void Check(string name,bool ok){lines.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
  var ui=FindObjectOfType<LabUI>();
  foreach(var name in new[]{"Shelf","Log","Settings"})Check("art loaded "+name,Resources.Load<Texture2D>("Themes/Nessie/"+name)!=null);
  ui.ShowSettings(true);yield return null;Check("settings opens",ui.SettingsOpen);
  var choice=ui.transform.Find("LabCanvas/SettingsScreen/SettingsPage/NessieTheme/Choose").GetComponentInChildren<Button>();
  choice.onClick.Invoke();yield return null;
  Check("button saves theme",LabAppearance.Nessie&&PlayerPrefs.GetInt(LabAppearance.PreferenceKey)==1);
  foreach(var panel in FindObjectsOfType<ThemePanel>()){
   var art=panel.transform.Find("NessieBackground").GetComponent<RawImage>();
   Check("decorated panel "+panel.name,art.enabled&&art.texture!=null&&!art.raycastTarget);
  }
  WindowTargetInput.InjectForTest("CAPTURE nessie-settings");yield return null;yield return null;
  ui.ShowSettings(false);yield return null;Check("settings closes",!ui.SettingsOpen);
  WindowTargetInput.InjectForTest("CAPTURE nessie-laboratory");yield return null;yield return null;
  LabAppearance.Select(false);yield return null;
  foreach(var panel in FindObjectsOfType<ThemePanel>())Check("classic restores "+panel.name,!panel.transform.Find("NessieBackground").GetComponent<RawImage>().enabled);
  if(existed)PlayerPrefs.SetInt(LabAppearance.PreferenceKey,original);else PlayerPrefs.DeleteKey(LabAppearance.PreferenceKey);
  PlayerPrefs.Save();LabAppearance.Select(original==1);if(!existed){PlayerPrefs.DeleteKey(LabAppearance.PreferenceKey);PlayerPrefs.Save();}
  lines.Add("TOTAL failures="+failures);File.WriteAllLines("Logs/theme-audit.txt",lines);Application.Quit(failures==0?0:1);
 }
}
}
