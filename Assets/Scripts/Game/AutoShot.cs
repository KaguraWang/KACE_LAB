/* 命令行自截图：-shotAfter=N -shotPath=xxx.png
   ScreenCapture 截的是应用自己的帧缓冲，窗口被遮挡也完全不影响 */
using System.IO;
using UnityEngine;

namespace ChemLab.Game {
public class AutoShot : MonoBehaviour {
    float _t;
    bool _done, _parsed;
    string _path;
    float _after = -1f;

    void Update() {
        if (!_parsed) {
            _parsed = true;
            foreach (var a in System.Environment.GetCommandLineArgs()) {
                if (a.StartsWith("-shotAfter=")) float.TryParse(a.Substring(11), out _after);
                if (a.StartsWith("-shotPath="))  _path = a.Substring(10);
            }
        }
        if (_path == null || _done) return;
        _t += Time.deltaTime;
        if (_t < Mathf.Max(0.5f, _after)) return;
        _done = true;
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(_path);
        Debug.Log("[ChemLab] 画面已保存: " + _path);
        Invoke(nameof(Bye), 2.0f);
    }
    void Bye() { Application.Quit(); }
}
}