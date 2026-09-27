/* =====================================================================
   编辑器工具入口（命令行 batchmode 用）
     -executeMethod ChemLab.Editor.ChemLabTools.RunTests
     -executeMethod ChemLab.Editor.ChemLabTools.BuildScene
     -executeMethod ChemLab.Editor.ChemLabTools.RenderShot
   ===================================================================== */
using System;
using System.IO;
using UnityEditor;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChemLab.Editor {

public static class ChemLabTools {

    const string ScenePath = "Assets/Scenes/Lab.unity";

    public static void RunTests() {
        var report = ChemSelfTest.Run();
        Debug.Log("[ChemLab 自检]\n" + report);
        ChemLab.Chem.Lab.Reset();          // 自检会留下容器，渲染前清干净
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "selftest.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, report, new System.Text.UTF8Encoding(false));
        bool fail = report.Contains("失败 0") == false;
        if (Application.isBatchMode) EditorApplication.Exit(fail ? 1 : 0);
    }

    [MenuItem("ChemLab/构建实验场景")]
    public static void BuildScene() {
        /* 运行时程序化搭建场景：只把引导脚本挂到空场景上，
           几何、材质、灯光、UI 全部由代码生成，避免手写 .unity 文件 */
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("LabBootstrap");
        go.AddComponent<ChemLab.Game.LabBootstrap>();
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        var list = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorBuildSettings.scenes = list;
        Debug.Log("[ChemLab] 场景已生成: " + ScenePath);
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    /// <summary>无头渲染一张截图，用于验证画面</summary>
    public static void RenderShot() {
        /* 必须先把资产导入完 —— batchmode 下着色器常常还没导入就被 Shader.Find 找了 */
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.SaveAssets();
        var arg = "";
        foreach (var a in Environment.GetCommandLineArgs())
            if (a.StartsWith("-shotOut=")) arg = a.Substring("-shotOut=".Length);
        if (string.IsNullOrEmpty(arg)) arg = "D:/ChemLabUnity/Logs/shot.png";

        var report = ChemSelfTest.Run();
        File.WriteAllText("D:/ChemLabUnity/Logs/selftest.txt", report, new System.Text.UTF8Encoding(false));
        Debug.Log("[ChemLab 自检]\n" + report);
        ChemLab.Chem.Lab.Reset();          // 自检会留下容器，渲染前清干净

        if (!File.Exists(ScenePath)) BuildScene_Internal();
        EditorSceneManager.OpenScene(ScenePath);

        var boot = UnityEngine.Object.FindObjectOfType<ChemLab.Game.LabBootstrap>();
        if (boot == null) { Debug.LogError("场景里没有 LabBootstrap"); EditorApplication.Exit(2); return; }

        var shot = boot.RenderToFile(1920, 1080, arg);
        Debug.Log("[ChemLab] 截图: " + shot);
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    const string ResDir = "Assets/Resources/ChemLab";

    /// <summary>把自定义着色器做成材质资产放进 Resources —— 只靠 Shader.Find 的话打包会被剥离</summary>
    [MenuItem("ChemLab/生成材质资产")]
    public static void EnsureMaterials() {
        Directory.CreateDirectory(ResDir);
        var want = new (string res, string shader)[] {
            ("Glass",    "ChemLab/Glass"),
            ("Liquid",   "ChemLab/Liquid"),
            ("Particle", "ChemLab/Particle"),
            ("Sky",      "ChemLab/Sky"),
            ("Bench",    "ChemLab/Bench"),
            ("Metal",    "ChemLab/Metal"),
            ("Solid",    "ChemLab/Solid"),
            ("ScaleInk", "ChemLab/ScaleInk"),
            ("GlassMark", "ChemLab/GlassMark"),
            ("Flame",    "ChemLab/Flame"),
            ("FluidVolume", "ChemLab/FluidVolume"),
        };
        foreach (var (res, sh) in want) {
            var path = ResDir + "/" + res + ".mat";
            var shader = Shader.Find(sh);
            if (shader == null) { Debug.LogError("找不到着色器 " + sh); continue; }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            } else m.shader = shader;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ChemLab] 材质资产已就绪: " + ResDir);
    }

    /// <summary>构建 Windows 独立播放器</summary>
    public static void BuildPlayer() {
        EnsureMaterials();
        if (!File.Exists(ScenePath)) BuildScene_Internal();
        var opts = new BuildPlayerOptions {
            scenes = new[] { ScenePath },
            locationPathName = "Build/ChemLab.exe",
            target = BuildTarget.StandaloneWindows64,
            options = System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-solidProfilerAudit")>=0?BuildOptions.Development:BuildOptions.None,
        };
        bool previousTiming=PlayerSettings.enableFrameTimingStats;
        UnityEditor.Build.Reporting.BuildReport rep;
        try {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-frameTimingAudit")>=0) PlayerSettings.enableFrameTimingStats=true;
            rep = BuildPipeline.BuildPlayer(opts);
        } finally { PlayerSettings.enableFrameTimingStats=previousTiming; }
        var sum = rep.summary;
        Debug.Log($"[ChemLab] 构建结果 {sum.result}  大小 {(sum.totalSize / 1048576.0):F1} MB  错误 {sum.totalErrors}");
        if (Application.isBatchMode) EditorApplication.Exit(sum.result == BuildResult.Succeeded && sum.totalErrors==0 ? 0 : 1);
    }
    static void BuildScene_Internal() {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("LabBootstrap");
        go.AddComponent<ChemLab.Game.LabBootstrap>();
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }
}

}
