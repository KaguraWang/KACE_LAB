using System;
using System.IO;
using UnityEngine;

namespace ChemLab.Game {
// Explicit diagnostic only. Records whether Windows messages addressed to the
// Player window reach Unity's legacy Input API while the app stays unfocused.
public sealed class WindowInputProbe : MonoBehaviour {
    string path;
    float deadline;
    Vector3 lastMouse;
    void Start(){
        Application.runInBackground=true;
        path=Path.GetFullPath("Logs/window-input-probe.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path,$"START {Screen.width}x{Screen.height} focused={Application.isFocused}{Environment.NewLine}");
        deadline=Time.realtimeSinceStartup+12f;
        lastMouse=Input.mousePosition;
    }
    void Update(){
        var now=Input.mousePosition;
        if(now!=lastMouse){File.AppendAllText(path,$"MOVE {now.x:F0},{now.y:F0}{Environment.NewLine}");lastMouse=now;}
        if(Input.GetMouseButtonDown(0))File.AppendAllText(path,$"LEFT_DOWN {now.x:F0},{now.y:F0}{Environment.NewLine}");
        if(Input.GetMouseButtonUp(0))File.AppendAllText(path,$"LEFT_UP {now.x:F0},{now.y:F0}{Environment.NewLine}");
        if(Input.GetKeyDown(KeyCode.W))File.AppendAllText(path,$"W_DOWN{Environment.NewLine}");
        if(Input.GetKeyUp(KeyCode.W))File.AppendAllText(path,$"W_UP{Environment.NewLine}");
        if(Time.realtimeSinceStartup>=deadline){File.AppendAllText(path,$"END{Environment.NewLine}");Application.Quit();}
    }
    void OnGUI(){
        var e=Event.current;
        if(e!=null&&(e.type==EventType.MouseMove||e.type==EventType.MouseDown||e.type==EventType.MouseUp))
            File.AppendAllText(path,$"GUI {e.type} {e.mousePosition.x:F0},{e.mousePosition.y:F0}{Environment.NewLine}");
    }
}
}
