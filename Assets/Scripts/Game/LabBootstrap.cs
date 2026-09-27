/* =====================================================================
   实验室场景：程序化搭建
   白色实验台 + 摄影棚灯光 + 玻璃器皿，全部由代码生成，
   不依赖手写 .unity 场景文件，也就不会有资产损坏的问题。
   ===================================================================== */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChemLab.Chem;

namespace ChemLab.Game {

/* ==================================================================== */
public class VesselView : MonoBehaviour {
    public Vessel Model;
    public Transform GlassRoot;
    MeshFilter _glassMF, _liqMF, _sedMF;
    MeshRenderer _glassMR, _liqMR, _sedMR;
    Material _glassMat, _liqMat, _sedMat, _iceMat;
    VesselFx _fx;
    MeteredFlowVisual _meteredFlow;
    GameObject _sealCap;
    Mesh _footMesh;
    float _lastFill = -1, _lastSolidFrac = -1;
    Color _lastColor;
    bool _lastMirror;
    int _solidSignature;
    readonly GpuFluid _fluid = new GpuFluid();
    SolidBodyBatch _solidBodies;
    readonly List<SolidBodySeed> _solidObstacles=new List<SolidBodySeed>();
    Vector3[] _solidPositions=Array.Empty<Vector3>();
    // Every vessel keeps its solid state ready for solids added later. Empty
    // vessels allocate no contact bodies; powder beds remain static obstacles.
    public bool SolidDynamicsEnabled {get;private set;}
    bool _nativeSolidContacts;
    public SolidBodyBatch SolidBodies=>_solidBodies;
    public double LastSolidCpuMilliseconds {get;private set;}
    double _solidReadyAt;bool _solidFirstAutomaticUpdate;
    double _solidDebtSeconds;
    public void EnableSolidDynamics(bool nativeContacts=false){
        if(SolidDynamicsEnabled)return;_nativeSolidContacts=nativeContacts;SolidDynamicsEnabled=true;Refresh(true);
        // The next Unity delta includes time spent before this solver existed.
        // Start its lifetime after synchronous construction, not at frame start.
        _solidReadyAt=Time.realtimeSinceStartupAsDouble;_solidFirstAutomaticUpdate=true;
    }
    public GpuFluid Fluid => _fluid;

    /// <summary>Test-only: active solid poses (empty when the vessel holds no solids).</summary>
    public Vector3[] TestSolidPositions(){
        if(_solidBodies==null)return Array.Empty<Vector3>();
        var copy=new Vector3[_solidBodies.Count];
        _solidBodies.CopyActivePositions(copy);
        return copy;
    }

    /// <summary>Test-only: whether this vessel actually runs a solid solver.</summary>
    public bool TestSolidSolverActive => _solidBodies != null;

    public static VesselView Create(Vessel model, Transform parent, Material glassTpl, Material liqTpl, Material sedTpl,
                                       Material fxSoft, Material fxAdd, Material glowMat) {
        if (glassTpl == null || liqTpl == null || sedTpl == null) {
            Debug.LogError("[ChemLab] 材质模板为空，无法创建视图"); return null;
        }
        var go = new GameObject("Vessel_" + model.Id);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = model.Pos;
        var vv = go.AddComponent<VesselView>();
        vv.Model = model;

        var gr = new GameObject("Glass");
        gr.transform.SetParent(go.transform, false);
        vv.GlassRoot = gr.transform;
        vv._glassMF = gr.AddComponent<MeshFilter>();
        vv._glassMR = gr.AddComponent<MeshRenderer>();
        vv._glassMat = new Material(glassTpl);
        vv._glassMR.sharedMaterial = vv._glassMat;
        vv._glassMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        vv._glassMR.receiveShadows = false;

        var lq = new GameObject("Liquid");
        lq.transform.SetParent(go.transform, false);
        vv._liqMF = lq.AddComponent<MeshFilter>();
        vv._liqMR = lq.AddComponent<MeshRenderer>();
        vv._liqMat = new Material(liqTpl);
        vv._liqMat.SetFloat("_ClipContainer",0);
        vv._liqMR.sharedMaterial = vv._liqMat;
        vv._liqMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var sd = new GameObject("Sediment");
        sd.transform.SetParent(go.transform, false);
        vv._sedMF = sd.AddComponent<MeshFilter>();
        vv._sedMR = sd.AddComponent<MeshRenderer>();
        vv._sedMat = new Material(sedTpl);
        vv._iceMat = new Material(Resources.Load<Shader>("Ice"));
        vv._sedMR.sharedMaterials = new[]{vv._sedMat,vv._iceMat};
        sd.SetActive(false);

        vv._glassMF.sharedMesh = VesselMesh.BuildGlass(model.Kind);
        gr.AddComponent<MeshCollider>().sharedMesh = vv._glassMF.sharedMesh;
        VesselGraduations.Create(gr.transform,model.Kind);
        VolumetricDetails.Create(gr.transform,model.Kind);
        GlasswareBranding.CreateVesselMark(gr.transform,model.Kind);
        if(model.Kind==VesselKind.Cylinder){
            var foot=new GameObject("SolidHexagonalFoot");foot.transform.SetParent(gr.transform,false);
            var footMesh=VesselMesh.BuildCylinderFoot();vv._footMesh=footMesh;foot.AddComponent<MeshFilter>().sharedMesh=footMesh;
            foot.AddComponent<MeshRenderer>().sharedMaterial=vv._glassMat;
            foot.AddComponent<MeshCollider>().sharedMesh=footMesh;
        }
        vv._fx = VesselFx.Create(go.transform, fxSoft, fxAdd, model);
        vv._meteredFlow=MeteredFlowVisual.Create(go.transform,model,liqTpl);
        if(model.Kind==VesselKind.Bottle){
            var cap=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cap.name="SealedCap";cap.transform.SetParent(go.transform,false);
            var geometry=VesselMesh.Get(model.Kind);var profile=VesselMesh.Profile(geometry.Type,geometry.R,geometry.H);
            float mouth=VesselMesh.RadiusAt(profile,1);
            cap.transform.localPosition=new Vector3(0,geometry.H+0.01f,0);cap.transform.localScale=new Vector3(mouth*2.1f,0.015f,mouth*2.1f);
            cap.GetComponent<MeshRenderer>().sharedMaterial=sedTpl;
            vv._sealCap=cap;
        }
        /* 吸附提示环：贴在容器口 */
        {
            var g = VesselMesh.Get(model.Kind);
            var rg = new GameObject("GlowRing");
            rg.transform.SetParent(go.transform, false);
            rg.transform.localPosition = new Vector3(0f, g.H + 0.02f, 0f);
            var rmf = rg.AddComponent<MeshFilter>();
            var rmr = rg.AddComponent<MeshRenderer>();
            rmf.sharedMesh = VesselMesh.BuildRing(g.R * 1.02f, g.R * 1.22f);
            rmr.sharedMaterial = glowMat;
            rmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rmr.receiveShadows = false;
            rmr.enabled = false;
            vv._glowRing = rmr;
        }
        // Process-local negative control for the dense-view audit. The normal
        // player enables native solid contacts in every vessel, including
        // vessels that receive solids after they are created.
        vv.SolidDynamicsEnabled = Array.IndexOf(Environment.GetCommandLineArgs(),"-noDefaultSolidDynamics") < 0;
        vv._nativeSolidContacts = vv.SolidDynamicsEnabled;
        vv.Refresh(true);
        vv._solidReadyAt = Time.realtimeSinceStartupAsDouble;
        vv._solidFirstAutomaticUpdate = true;
        return vv;
    }

    public void Refresh(bool force = false) {
        var m = Model;
        if(_sealCap!=null)_sealCap.SetActive(m.Sealed);
        float fill = VesselMesh.FillFractionFor(m.Kind, m.Volume());   // 按剖面精确换算

        double solidVol = SolidGeometry.VolumeMl(m);
        int signature = SolidGeometry.Signature(m);

        bool colorChanged = false;
        Color lc = Color.white;
        if (Lab.TrySolutionColor(m, out Color c)) { lc = c; colorChanged = true; }

        bool needLiq = m.Volume()!=_fluid.MassVolumeMl || Mathf.Abs(fill-_lastFill)>0.0001f || lc!=_lastColor || force;
        bool needSed = signature != _solidSignature || force;

        if (needLiq) {
            _lastFill = fill;
            _fluid.Configure(m.Kind,m.Volume(),_liqMF,_liqMat);
            _lastColor=lc;
            /* 不透明度按「溶液有多鲜艳」来定：
               无色溶液（水、稀盐酸）要能看透，有色溶液才需要遮住背后 */
            float _mx = Mathf.Max(lc.r, Mathf.Max(lc.g, lc.b));
            float _mn = Mathf.Min(lc.r, Mathf.Min(lc.g, lc.b));
            float _sat = _mx - _mn;
            float _alpha = Mathf.Clamp(0.20f + _sat * 1.25f + m.Turbidity * 0.55f
                                       + m.Precipitates.Count * 0.08f, 0.16f, 0.95f);
            SetColorSafe(_liqMat, "_Color", new Color(lc.r, lc.g, lc.b, _alpha));
            _liqMat.SetFloat("_Turbidity", m.Turbidity);
            bool hasLiq = m.Volume() > 0.05;
            _liqMR.enabled = hasLiq;
            /* 银镜反应：把整个内壁调成银白高反射 */
            if (m.Fx.Mirror != _lastMirror) {
                _lastMirror = m.Fx.Mirror;
                _glassMat.SetColor("_Tint", m.Fx.Mirror
                    ? new Color(0.88f, 0.92f, 0.96f, 0.42f)
                    : new Color(0.86f, 0.93f, 0.97f, 0.06f));
            }
        }
        if (needSed || force) {
            _solidSignature = signature;
            _sedMF.gameObject.SetActive(solidVol>0);
            if (solidVol > 0) {
                var oldSolid=_sedMF.sharedMesh;
                var seeds=SolidDynamicsEnabled?new List<SolidBodySeed>():null;
                var nextMesh=SolidGeometry.Build(m,seeds,_solidBodies?.Seeds);
                SolidBodyBatch next=null;
                try{if(SolidDynamicsEnabled)next=new SolidBodyBatch(m.Kind,nextMesh,seeds,_solidBodies,_nativeSolidContacts);}
                catch{Destroy(nextMesh);throw;}
                bool firstBodies=_solidBodies==null&&next!=null;
                _solidBodies?.Dispose();_solidBodies=next;
                if(firstBodies){_solidDebtSeconds=0;_solidReadyAt=Time.realtimeSinceStartupAsDouble;_solidFirstAutomaticUpdate=true;}
                _sedMF.sharedMesh = nextMesh;
                if(oldSolid!=null) Destroy(oldSolid);
                SetColorSafe(_sedMat, "_Color", Color.white);
                _sedMR.enabled = true;
            } else {
                _sedMR.enabled = false;_solidBodies?.Dispose();_solidBodies=null;_solidDebtSeconds=0;
                if(_sedMF.sharedMesh!=null){Destroy(_sedMF.sharedMesh);_sedMF.sharedMesh=null;}
            }
        }
        if(SolidDynamicsEnabled&&(needLiq||needSed||force)){
            _solidObstacles.Clear();_solidBodies?.CopyActiveObstacles(_solidObstacles);
            _fluid.ConfigureSolids(_solidObstacles);_solidPositions=new Vector3[_solidObstacles.Count];
        }
        SyncFx();
        _liqMat.SetFloat("_Turbidity",m.Turbidity);
    }

    static void SetColorSafe(Material mat, string prop, Color c) {
        if (mat == null) return;
        if (mat.HasProperty(prop)) mat.SetColor(prop, c);
        else if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
    }

    /// <summary>吸附提示环（拖到别的容器附近时亮起）</summary>
    MeshRenderer _glowRing;
    public void SetHighlight(bool on) {
        SetFocusGlow(on ? 1.35f : 0f);
    }

    /// <summary>聚焦高亮：走玻璃材质的菲涅尔辉光，而不是反向壳描边。
    /// 反向壳在透明物体上必然被透出来（描边在 Opaque 队列、玻璃在 Transparent
    /// 队列后画），整个器皿会泛青，调细也只是减轻、根治不了。</summary>
    /// <summary>给自检用的玻璃材质访问点</summary>
    public Material TestGlassMat => _glassMat;
    /// <summary>沉淀渲染开没开（诊断用）</summary>
    public bool TestSedimentOn => _sedMR != null && _sedMR.enabled;
    /// <summary>当前聚焦高亮强度，给窗口测试当客观证据（辉光太淡，像素判据不稳）</summary>
    public float TestFocusGlow => (_glassMat != null && _glassMat.HasProperty("_HighlightAmt"))
                                ? _glassMat.GetFloat("_HighlightAmt") : -1f;

    public void SetFocusGlow(float amt) {
        if (_glassMat != null && _glassMat.HasProperty("_HighlightAmt"))
            _glassMat.SetFloat("_HighlightAmt", amt);
    }

    /// <summary>同步粒子；Refresh 里也调，保证无头渲染时同样生效</summary>
    public float FxTime = 0f;
    public Camera FxCam;

    /// <summary>同步粒子。时间由外部显式喂进来 —— 无头环境没有 Time.deltaTime</summary>
    public void SyncFx() {
        if (_fx == null || Model == null) return;
        var g = VesselMesh.Get(Model.Kind);
        _fx.Sync(FxTime, FxCam, g.R, g.H, VesselMesh.FillFractionFor(Model.Kind, Model.Volume()));
    }
    static Color SedimentColor(Vessel m, Color fallback) {
        /* 按**物质的量**加权，不是简单平均 ——
           平均的话，白色沉淀 + 白色固体混出来几乎全白，在白台面上根本看不见。 */
        float r = 0, g = 0, b2 = 0, wsum = 0;
        void Acc(string id, double w) {
            var s = ChemData.Sub[id];
            if (s == null) return;
            string hex = ChemData.Precip.TryGetValue(s.Formula, out var pi) && !string.IsNullOrEmpty(pi.Color)
                       ? pi.Color : s.SolidColor;
            if (string.IsNullOrEmpty(hex) || !ColorUtility.TryParseHtmlString(hex, out Color c)) return;
            float wf = (float)System.Math.Max(0.0001, w);
            r += c.r * wf; g += c.g * wf; b2 += c.b * wf; wsum += wf;
        }
        foreach (var kv in m.Undissolved) Acc(kv.Key, kv.Value);
        foreach (var kv in m.Precipitates) Acc(kv.Key, kv.Value);
        if (wsum <= 0f) return fallback;
        var col = new Color(r / wsum, g / wsum, b2 / wsum);
        /* 提一点饱和、压一点亮度：白粉放在白台面上要有对比才看得见 */
        float mx = Mathf.Max(col.r, Mathf.Max(col.g, col.b));
        float mn = Mathf.Min(col.r, Mathf.Min(col.g, col.b));
        if (mx - mn < 0.12f) {
            /* 近乎无彩色的固体：整体压暗一档，别和台面糊在一起 */
            /* 压暗得多一点：白粉和白台面几乎同色，只压 14% 看不出来 */
            col = new Color(col.r * 0.72f, col.g * 0.73f, col.b * 0.76f);
        }
        return col;
    }

    /* 液面呼吸、气泡靠近似的轻微起伏 */
    void Update() {
        if (Model == null) return;
        /* 固体/液体求解与粒子视觉共用同一个实验时钟：world_speed 0 时整体冻结。 */
        AdvanceContents(WorldClock.DeltaTime,true);
    }
    public void AdvanceContents(float dt,bool automatic=false){
        LastSolidCpuMilliseconds=0;
        if(Model==null||PourTransaction.IsActive||dt<=0)return;
        var m = Model;
        if(_solidBodies!=null){
            long solidStarted=System.Diagnostics.Stopwatch.GetTimestamp();
            double liquidMass=m.Water;
            foreach(var p in m.Solutes)liquidMass+=ChemData.MassOf(p.Key,p.Value);
            foreach(var set in new[]{m.Undissolved,m.Precipitates})foreach(var p in set)
                if(PhaseProperties.Phase(p.Key,m.Temp)!="s")liquidMass+=ChemData.MassOf(p.Key,p.Value);
            double ml=m.Volume();double density=ml>1e-9?liquidMass/ml:0;
            float solidDt=dt;
            if(automatic&&_solidFirstAutomaticUpdate){
                // World seconds elapsed since this solver was built, measured on the
                // same scaled clock the solver itself runs on.
                solidDt=Mathf.Min(dt,(float)System.Math.Max(0,(Time.realtimeSinceStartupAsDouble-_solidReadyAt)*Time.timeScale*WorldClock.Rate));
                _solidFirstAutomaticUpdate=false;
            }
            _solidDebtSeconds+=solidDt;
            // A renderer stall does not erase elapsed solid simulation time.
            // Native stepping accepts up to 0.25 s per call; carry the rest
            // forward without modifying the chemistry or liquid inventories.
            float contactDt=(float)System.Math.Min(_solidDebtSeconds,.25);
            _solidBodies.StepWithLiquidVolume(contactDt,ml,density,30);
            _solidDebtSeconds=System.Math.Max(0,_solidDebtSeconds-contactDt);
            _solidBodies.UpdateMesh();_solidBodies.CopyActivePositions(_solidPositions);
            // Dry vessels have no allocated GPU solver/obstacle state.
            if(_fluid.Count>0&&_liqMR.enabled)_fluid.UpdateSolidPositions(_solidPositions);
            LastSolidCpuMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-solidStarted)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        }
        if (_liqMR.enabled) {
            _fluid.Step(transform.position,transform.rotation,dt,m.Stirring,m.Fx.Bubble);
        }
    }
    public bool TestMeteredFlowVisible=>_meteredFlow!=null&&_meteredFlow.IsFlowVisible;
    public void SyncMeteredFlow(float dt){if(_meteredFlow!=null)_meteredFlow.Sync(dt);}
    void OnDestroy(){_solidBodies?.Dispose();_fluid.Dispose();if(_sedMF!=null&&_sedMF.sharedMesh!=null)Destroy(_sedMF.sharedMesh);if(_footMesh!=null)Destroy(_footMesh);if(_iceMat!=null)Destroy(_iceMat);}
}

/* ==================================================================== */
public class LabBootstrap : MonoBehaviour {
    public Camera Cam;
    Material _glassTpl, _liqTpl, _sedTpl, _benchMat, _skyMat;
    Material _fxSoft, _fxAdd;
    /// <summary>界面用的层号（标签要挂到同一层才被截图相机看见）</summary>
    public const int UiLayerConst = 31;
    /// <summary>供其它界面模块找到宿主</summary>
    public static LabBootstrap Instance;
    Material _metalMat, _glowMat, _flameMat;
    readonly List<ApparatusView> _apViews = new List<ApparatusView>();
    readonly List<TestItemView> _testViews = new List<TestItemView>();
    readonly List<VesselTag> _tags = new List<VesselTag>();
    readonly List<EquationTag> _eqTags = new List<EquationTag>();
    long _eqSeen;
    Transform _tagRoot;
    Material _paperMat, _woodMat;
    float _lastPopPlayed;
    readonly List<VesselView> _views = new List<VesselView>();
    LabUI _ui;
    DeveloperConsole _developerConsole;
    DavidDriver _david;
    public KPrinter Printer { get; private set; }
    public DeveloperConsole Console => _developerConsole;
    public VesselView FindVesselView(Vessel model) => _views.Find(v => v != null && v.Model == model);
    public void ShowHint(string message) { if (_ui != null) _ui.ShowHint(message); }
    public Vessel SelectedVesselForTags => _ui!=null?_ui.SelectedVessel:null;
    public Camera UICam;
    public const int UiLayer = 31;
    readonly List<Vessel> _bound = new List<Vessel>();

    /* 相机轨道 */
    float _yaw = 0.34f, _pitch = 0.26f, _dist = 7.4f, _distT = 7.4f;
    Vector3 _focus = new Vector3(0f, 0.34f, 0f);
    Vessel _framedMetered;
    Vector3 _beforeMeteredFocus;
    float _beforeMeteredYaw,_beforeMeteredPitch,_beforeMeteredDistance,_beforeMeteredDistanceTarget;

    bool _inited;

    /// <summary>幂等初始化：batchmode 下 Awake 不一定被调到，谁先用到谁负责初始化</summary>
    bool _watchPos;
    float _watchT;
    bool _watchParsed;

    /// <summary>-watchPos=1 时每秒记录器皿位置，用于验证拖动是否真的生效</summary>
    void WatchPositions() {
        if (!_watchParsed) {
            _watchParsed = true;
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == "-watchPos=1") _watchPos = true;
        }
        if (!_watchPos) return;
        _watchT += Time.deltaTime;
        if (_watchT < 1f) return;
        _watchT = 0f;
        var sb = new System.Text.StringBuilder("[ChemLab POS]");
        foreach (var v in Lab.Vessels)
            sb.Append($" {v.Id}=({v.Pos.x:F2},{v.Pos.z:F2}) h={(v.Heating ? 1 : 0)} bub={v.Fx.Bubble:F1} smk={v.Fx.Smoke:F1} fl={(v.Fx.Flame ? 1 : 0)}");
        Debug.Log(sb.ToString());
    }

    public void EnsureInit() {
        if (_inited) return;
        _inited = true;
        // Keep the lab from driving high-refresh laptop GPUs continuously at 165+ Hz.
        // Fluid integration remains fixed at 90 Hz; this only limits presentation work.
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=60;
        Lab.EnsureReady();
        BuildMaterials();
        BuildEnvironment();
        BuildBench();
        BuildApparatus();
        BuildTestItems();
        bool eqDemo = false;
        foreach (var a in System.Environment.GetCommandLineArgs()) if (a == "-eqDemo") eqDemo = true;
        if (eqDemo) SeedEqDemo(); else SeedDemo();
        EnsureEventSystem();
        BuildCamera();
        Lang.Load();
        BuildUI();
        ChemAudio.Ensure(gameObject);
        Instance = this;
        Printer = KPrinter.Create(this);
        /* 开发者控制台：界面、日志与命令分发在 DeveloperConsole；这里只注册
           业务命令，并按 InputCaptured 隔离实验台的输入。 */
        _david = DavidDriver.Ensure(gameObject);
        _developerConsole = DeveloperConsole.Create(transform, LabUI.UiFont);
        RegisterConsoleCommands(_developerConsole);
        foreach (var arg in System.Environment.GetCommandLineArgs())
        /* 阿侬：UI 层宠物，排序号最高，永远在最上层 */
        PetView.Show(gameObject, LabUI.UiFont);
        VersionInfo.Create(transform, LabUI.UiFont);
        /* 首次启动：由阿侬引导走一遍离子鉴定实验。
           用延迟是为了等界面布局跑完，否则她指着空画面讲。 */
        /* -calcDemo 时不要自动开教程 —— 教程第一步会 SwitchScene(false)，
           会把刚打开的计算器又藏起来，两者互相抢场景。 */
        bool calcDemo = false;
        foreach (var arg in System.Environment.GetCommandLineArgs())
            if (arg == "-calcDemo" || arg == "-hessDemo") calcDemo = true;
        if (calcDemo) PetProgress.FinishLab(true);
        /* -noTut：本次进程跳过自动实验教程，并跳过计算器教程。
           不写入实验教程完成状态，避免测试污染玩家首次启动体验。 */
        bool suppressAutoLabTutorial = false;
        foreach (var a7 in System.Environment.GetCommandLineArgs())
            if (a7 == "-noTut") { suppressAutoLabTutorial = true; PetProgress.FinishCalc(); }
        /* -dlgDemo：直接打开手动输入焓变对话框，单独验证对话框本身 */
        foreach (var a8 in System.Environment.GetCommandLineArgs())
            if (a8 == "-dlgDemo") Invoke(nameof(OpenValueDlg), 3f);
        if (!suppressAutoLabTutorial && !PetProgress.LabDone)
            PetTutorial.StartDelayed(TutorialKind.IonTest, 3.0f);
        /* -calcDemo：直接打开焓变计算器，便于截图验证 */
        foreach (var arg in System.Environment.GetCommandLineArgs())
            if (arg == "-calcDemo") {
                var cs = EnthalpyScene.Show(gameObject, LabUI.UiFont);
                Invoke(nameof(DumpInputAudit), 3f);   // 3 秒后把输入诊断打出来
                Debug.Log("AUDITSTART\n" + cs.AuditLayout() + "AUDITEND\n");
                if (cs != null) StartCoroutine(StateTick(cs));   // 每秒打印一次状态，供外部脚本读
            }
            /* -hessDemo：摆出 KCl 的 Born-Haber 循环（教程第 7 步用的那个），
               单独抽出来便于截图验证，不用每次走完整段教程 */
        /* -hessDemo：摆出 KCl 的 Born-Haber 循环，单独抽出来便于截图验证 */
        foreach (var arg2 in System.Environment.GetCommandLineArgs())
            if (arg2 == "-hessDemo") {
                var cs2 = EnthalpyScene.Show(gameObject, LabUI.UiFont);
                if (cs2 != null) { cs2.transform.SetAsLastSibling(); cs2.SeedBornHaberDemo(); }
            }
        /* -petAudit：定期打印阿侬与教程状态，供窗口级测试读 */
        foreach (var a5 in System.Environment.GetCommandLineArgs())
            if (a5 == "-petAudit") StartCoroutine(PetTick());
        /* -calcTut：强制走一遍计算器教程，便于验证 */
        foreach (var a6 in System.Environment.GetCommandLineArgs())
            if (a6 == "-calcTut") Invoke(nameof(ForceCalcTut), 2f);
        /* -tagAudit：把容器标签的状态打出来 */
        foreach (var a4 in System.Environment.GetCommandLineArgs())
            if (a4 == "-tagAudit") Invoke(nameof(DumpTagAudit), 4f);
        /* -focusDemo：启动后自动聚焦第一个器皿，用于截图验证轮廓高亮 */
        foreach (var arg3 in System.Environment.GetCommandLineArgs())
            if (arg3 == "-focusDemo") Invoke(nameof(FocusDemo), 2.5f);
        /* 命令行覆盖标签设置，便于截图验证两种模式 */
        foreach (var a in System.Environment.GetCommandLineArgs()) {
            if (a == "-tagIonic") TagSettings.Molecular = false;
            if (a == "-tagMolecular") TagSettings.Molecular = true;
            if (a == "-tagNoName") TagSettings.ShowName = false;
            if (a == "-tagNoAmount") TagSettings.ShowAmount = false;
            if (a == "-tagNoConc") TagSettings.ShowConc = false;
            if (a == "-tagNoLine") TagSettings.ShowLine = false;
        }
        /* 标签单独一个根节点，方便整体开关 */
        if (_tagRoot == null) {
            var tg = new GameObject("Tags");
            tg.transform.SetParent(transform, false);
            _tagRoot = tg.transform;
        }
        Debug.Log("[ChemLab] 场景初始化完成");
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-interactionTest") >= 0) {
            Application.runInBackground = true;
            gameObject.AddComponent<InteractionRegression>();
        }
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-windowInputProbe")>=0)
            gameObject.AddComponent<WindowInputProbe>();
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-windowTargetInput")>=0)
            gameObject.AddComponent<WindowTargetInput>();
    }

    void Awake() { EnsureInit(); }

    /* ================================================================
       开发者控制台：业务命令
         · world_speed <0-3>  统一实验时间速率，0 冻结全部实验事件
         · david <0|1>        拖动突进与残影
         · kprinter <0|1>     固定位置全息打印，默认开启
       非法、越界、非有限值一律拒绝并说明原因，绝不静默钳制。
       ================================================================ */
    void RegisterConsoleCommands(DeveloperConsole console) {
        if (console == null) return;
        console.RegisterCommand("world_speed", argument => { WorldClock.TrySet(argument, out string message); return message; });
        console.RegisterCommand("david", argument => { DavidMode.TrySet(argument, out string message); return message; });
        console.RegisterCommand("kprinter", argument => { KPrinterMode.TrySet(argument, out string message); return message; });
    }

    /* ---------------- 材质 ---------------- */
    /// <summary>优先用 Resources 里的材质资产（打包后才不会丢），退回 Shader.Find</summary>
    static Material MatFromRes(string resName, string shaderName, string fallback) {
        var m = Resources.Load<Material>("ChemLab/" + resName);
        if (m != null) return new Material(m);
        return new Material(FindShader(shaderName, fallback));
    }

    static Shader FindShader(string name, string fallback) {
        var s = Shader.Find(name);
        if (s != null) return s;
        Debug.LogWarning($"[ChemLab] 找不到着色器 {name}，回退到 {fallback}");
        s = Shader.Find(fallback);
        if (s == null) Debug.LogError($"[ChemLab] 连 {fallback} 都没有");
        return s;
    }

    void BuildMaterials() {
        _skyMat   = MatFromRes("Sky", "ChemLab/Sky", "Skybox/Procedural");
        _glassTpl = MatFromRes("Glass", "ChemLab/Glass", "Standard");
        _liqTpl   = MatFromRes("Liquid", "ChemLab/Liquid", "Standard");
        _sedTpl   = MatFromRes("Solid", "ChemLab/Solid", "Standard");
        if (_sedTpl.HasProperty("_Thickness")) _sedTpl.SetFloat("_Thickness", 3.2f);
        if (_sedTpl.HasProperty("_Gloss")) _sedTpl.SetFloat("_Gloss", 0.25f);
        _fxSoft = MatFromRes("Particle", "ChemLab/Particle", "Particles/Standard Unlit");
        if (_fxSoft.HasProperty("_Softness")) _fxSoft.SetFloat("_Softness", 2.6f);
        _fxAdd = MatFromRes("Particle", "ChemLab/Particle", "Particles/Standard Unlit");
        if (_fxAdd.HasProperty("_Softness")) _fxAdd.SetFloat("_Softness", 1.7f);
        if (_fxAdd.HasProperty("_SrcBlend")) _fxAdd.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (_fxAdd.HasProperty("_DstBlend")) _fxAdd.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (_fxAdd.HasProperty("_Color")) _fxAdd.SetColor("_Color", new Color(1f, 1f, 1f, 1f));

        _benchMat = MatFromRes("Bench", "ChemLab/Bench", "Standard");

        /* 器材用同一种「哑光金属」着色器，只是颜色不同 */
        _metalMat = MatFromRes("Metal", "ChemLab/Metal", "Standard");
        _metalMat.SetColor("_Color", new Color(0.32f, 0.34f, 0.37f));
        _glowMat = MatFromRes("Bench", "ChemLab/Bench", "Standard");
        _glowMat.SetColor("_Color", new Color(0.25f, 0.95f, 0.55f));   // 吸附提示：亮绿
        /* 检验器材：试纸与木条 */
        _paperMat = MatFromRes("Bench", "ChemLab/Bench", "Standard");
        _paperMat.SetColor("_Color", new Color(0.85f, 0.23f, 0.23f));
        _woodMat = new Material(Resources.Load<Shader>("ChemLab/Wood"));
        _woodMat.SetColor("_Color", new Color(0.78f, 0.62f, 0.40f));

        _flameMat = MatFromRes("Flame", "ChemLab/Flame", "Particles/Standard Unlit");
        if (_flameMat.HasProperty("_Color")) _flameMat.SetColor("_Color", new Color(0.12f, 0.4f, 1f, 0.82f));
        if (_flameMat.HasProperty("_SrcBlend")) _flameMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (_flameMat.HasProperty("_DstBlend")) _flameMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (_flameMat.HasProperty("_Softness")) _flameMat.SetFloat("_Softness", 1.6f);
        _benchMat.SetColor("_Color", new Color(0.90f, 0.91f, 0.93f));


    }

    void BuildEnvironment() {
        RenderSettings.skybox = _skyMat;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.65f;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowDistance = 25f;
        QualitySettings.antiAliasing = 4;
        RenderSettings.fog = false;
        RenderSettings.reflectionIntensity = 1.0f;

        /* 三盏灯对应天空里的三块柔光箱 */
        MakeLight("KeyLight",  new Vector3(0.10f, 1.00f, 0.28f), new Color(1.00f, 0.98f, 0.95f), 1.15f);
        MakeLight("FillLight", new Vector3(-1.00f, 0.28f, 0.18f), new Color(0.72f, 0.82f, 1.00f), 0.40f);
        MakeLight("RimLight",  new Vector3(0.80f, 0.16f, -0.55f), new Color(0.92f, 0.80f, 1.00f), 0.22f);
    }

    void MakeLight(string name, Vector3 dir, Color col, float intensity) {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = col;
        l.intensity = intensity;
        l.shadows = name == "KeyLight" ? LightShadows.Soft : LightShadows.None;
        l.shadowStrength = 0.55f;
        go.transform.rotation = Quaternion.LookRotation(-dir.normalized, Vector3.up);
    }

    /* ---------------- 白色台面 ---------------- */
    void BuildBench() {
        /* 台面必须有厚度：之前用的是 Plane 图元（单面无厚度），
           又叠了一个顶面同样在 y=0 的挡板立方体 —— 两个面完全共面，
           一拖动器皿就疯狂 Z-fighting。改成实心方盒。 */
        var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bench.name = "Bench";
        bench.transform.SetParent(transform, false);
        bench.transform.localPosition = new Vector3(0f, -0.20f, 0f);  // 顶面正好落在 y = 0
        bench.transform.localScale = new Vector3(60f, 0.40f, 60f);
        bench.GetComponent<MeshRenderer>().sharedMaterial = _benchMat;

        /* 台面下方的深色裙边，和台面不共面 */
        var skirt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        skirt.name = "BenchSkirt";
        skirt.transform.SetParent(transform, false);
        skirt.transform.localPosition = new Vector3(0f, -0.64f, 0f);
        skirt.transform.localScale = new Vector3(60f, 0.44f, 60f);
        var m = MatFromRes("Bench", "ChemLab/Bench", "Standard");
        m.SetColor("_Color", new Color(0.30f, 0.32f, 0.35f));
        skirt.GetComponent<MeshRenderer>().sharedMaterial = m;
    }
    /* ---------------- 相机 ---------------- */
    /// <summary>台面上的器材：一盏本生灯 + 一个三脚架</summary>
    void BuildApparatus() {
        ApparatusSet.Clear();
        // Keep the burner used for splint ignition inside the central viewport,
        // clear of the fixed reagent panel at the default composition.
        var burner = ApparatusSet.Add(ApparatusKind.BunsenBurner, new Vector3(0.15f, 0f, 1.25f));
        var tripod = ApparatusSet.Add(ApparatusKind.Tripod, new Vector3(1.60f, 0f, 1.25f));
        foreach (var a in ApparatusSet.All) {
            var av = ApparatusView.Create(a, transform, _metalMat, _glowMat, _flameMat);
            av.Sync();
            _apViews.Add(av);
        }
    }

    /// <summary>台面上的检验器材：红/蓝石蕊试纸各一、木条一根</summary>
    void BuildTestItems() {
        TestSet.Clear();
        TestSet.Add(TestItemKind.RedLitmus,  new Vector3(0.55f, 0f, 1.86f));
        TestSet.Add(TestItemKind.BlueLitmus, new Vector3(0.85f, 0f, 1.86f));
        TestSet.Add(TestItemKind.Splint,     new Vector3(1.15f, 0f, 1.86f));
        foreach (var t in TestSet.All) {
            var tv = TestItemView.Create(t, transform, _paperMat, _woodMat, _flameMat);
            tv.Sync();
            _testViews.Add(tv);
        }
    }

    /// <summary>保证场景里有且只有一个 EventSystem。
    /// 它必须独立于任何界面存在 —— 否则藏起某个界面就会连带把输入分发一起关掉。 </summary>
    static void EnsureEventSystem() {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        foreach (var e in UnityEngine.Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>()) {
            if (e != null && e.gameObject.activeInHierarchy) return;
        }
        var ego = new GameObject("EventSystem");
        if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-windowTargetInput")>=0)
            ego.AddComponent<WindowTargetEventSystem>();
        else ego.AddComponent<UnityEngine.EventSystems.EventSystem>();
        ego.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        Debug.Log("[ChemLab] 已创建 EventSystem");
    }

    void BuildCamera() {
        var go = new GameObject("MainCamera");
        go.transform.SetParent(transform, false);
        go.tag = "MainCamera";
        Cam = go.AddComponent<Camera>();
        Cam.depthTextureMode |= DepthTextureMode.Depth;
        Cam.clearFlags = CameraClearFlags.Skybox;
        Cam.fieldOfView = 30f;
        Cam.nearClipPlane = 0.05f;
        Cam.farClipPlane = 200f;
        Cam.allowHDR = true;
        go.AddComponent<AudioListener>();
        gameObject.AddComponent<AutoShot>();
        UpdateCamera(0f);
    }

    void BuildUI() {
        /* 主相机不渲染 UI 层，UI 由专用相机叠加 —— 两台相机共用同一个 RT */
        Cam.cullingMask = -1;

        var ugo = new GameObject("UICamera");
        ugo.transform.SetParent(transform, false);
        UICam = ugo.AddComponent<Camera>();
        UICam.clearFlags = CameraClearFlags.Depth;   // 只清深度，颜色叠在主画面上
        UICam.cullingMask = 1 << UiLayer;
        UICam.depth = 10;
        UICam.fieldOfView = 30f;
        UICam.nearClipPlane = 1f;
        UICam.farClipPlane = 8000f;                  // 画布在约 2000 单位外
        UICam.allowHDR = false;
        UICam.transform.position = Vector3.zero;
        UICam.transform.rotation = Quaternion.identity;
        /* 平时关掉：画布是 Overlay，上屏由它自己负责，这台相机只在编辑器截图时开 */
        UICam.enabled = false;

        var go = new GameObject("LabUI");
        go.transform.SetParent(transform, false);
        _ui = go.AddComponent<LabUI>();
        _ui.Lab3D = this;
        _ui.UICam = Cam;
        _ui.UiLayer = UiLayer;
        _ui.UICam = Cam;
        _ui.Build();
        ChemLab.Game.LabUI.ApplyLang();   // 启动时按上次选择的语言刷新界面
        foreach (var arg in System.Environment.GetCommandLineArgs())
            if (arg == "-dumpLayout" && _ui != null) Debug.Log("DUMPSTART\n" + _ui.TestClickTargets() + _ui.TestDumpBars() + _ui.TestDumpLayout() + "\nDUMPEND");
    }

    void FocusDemo() {
        if (Lab.Vessels.Count == 0) { var v0 = Lab.AddVessel(VesselKind.Beaker); v0.AddWater(60); }
        SetFocus(Lab.Vessels[0]);
        Debug.Log("[ChemLab] focusDemo 聚焦 = " + (_focused != null ? _focused.ToString() : "null")
                + "  轮廓 = " + (_focusOutline != null ? (_focusOutline.IsOn ? "已开" : "未开") : "未建"));
    }

    /// <summary>容器标签状态诊断：为什么标签没显示出来</summary>
    public string TagAudit() {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[TagAudit] ShowTags = " + TagSettings.ShowTags);
        sb.AppendLine("[TagAudit] _tagRoot = " + (_tagRoot == null ? "null！" : _tagRoot.name
                    + " active=" + _tagRoot.gameObject.activeInHierarchy));
        sb.AppendLine("[TagAudit] 容器数 = " + Lab.Vessels.Count + "  标签视图数 = " + _tags.Count);

        var liveTags = new List<VesselTag>();
        foreach (var lt in _tags) if (lt != null && lt.gameObject.activeSelf && lt.RenderVisible) liveTags.Add(lt);

        int lap = 0;
        for (int i = 0; i < liveTags.Count; i++)
            for (int j = i + 1; j < liveTags.Count; j++) {
                var ra = liveTags[i].ScreenRect; var rb = liveTags[j].ScreenRect;
                if (ra.width <= 0f || rb.width <= 0f) continue;
                if (ra.xMin < rb.xMax && rb.xMin < ra.xMax && ra.yMin < rb.yMax && rb.yMin < ra.yMax) lap++;
            }

        sb.AppendLine("[TagAudit] 存活标签 " + liveTags.Count + " 个，重叠对数 = " + lap);
        foreach (var lt in liveTags) {
            sb.AppendLine("[TagAudit]   " + lt.gameObject.name
                        + " ScreenOffset=" + lt.ScreenOffset
                        + " 屏幕矩形=" + lt.ScreenRect);
            sb.AppendLine("[TagAudit]      trace: " + lt.LastSyncTrace);
        }
        sb.AppendLine("[TagAudit] 方程式标签数 = " + _eqTags.Count
                    + "  Active = " + EquationTag.Active.Count);
        return sb.ToString();
    }

    /// <summary>阿侬与教程的状态，给窗口级测试当客观证据。格式：PET|键|值</summary>
    public string PetAudit() {
        var sb = new System.Text.StringBuilder();
        var pv = PetView.Instance;
        sb.AppendLine("PET|exists|" + (pv != null ? "yes" : "no"));
        if (pv != null) {
            sb.AppendLine("PET|anim|" + pv.CurrentAnim);
            sb.AppendLine("PET|home|" + Mathf.RoundToInt(pv.Home.x) + "," + Mathf.RoundToInt(pv.Home.y));
            sb.AppendLine("PET|scale|" + pv.Scale.ToString("F2"));
            sb.AppendLine("PET|bubbleVisible|" + (pv.BubbleVisible ? "yes" : "no"));
            var bubbleSize = pv.BubbleSize;
            sb.AppendLine("PET|bubbleSize|" + Mathf.RoundToInt(bubbleSize.x) + "," + Mathf.RoundToInt(bubbleSize.y));
            sb.AppendLine("PET|bubbleTextLen|" + pv.BubbleTextLength);
            var rt = pv.GetComponent<RectTransform>();
            if (rt != null) {
                var sc = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
                sb.AppendLine("PET|screen|" + Mathf.RoundToInt(sc.x) + "," + Mathf.RoundToInt(sc.y));
                sb.AppendLine("PET|size|" + Mathf.RoundToInt(rt.rect.width) + "," + Mathf.RoundToInt(rt.rect.height));
            }
        }
        sb.AppendLine("PET|tutRunning|" + (PetTutorial.Running ? "yes" : "no"));
        sb.AppendLine("PET|tutStep|" + PetTutorial.StepIndex + "/" + PetTutorial.StepCount);
        sb.AppendLine("PET|tutTitle|" + PetTutorial.StepTitle);
        sb.AppendLine("PET|tutWaiting|" + (PetTutorial.WaitingOnUser ? "yes" : "no"));
        sb.AppendLine("PET|labDone|" + (PetProgress.LabDone ? "yes" : "no"));
        sb.AppendLine("PET|labSkipped|" + (PetProgress.LabSkipped ? "yes" : "no"));
        sb.AppendLine("PET|calcDone|" + (PetProgress.CalcDone ? "yes" : "no"));
        sb.AppendLine("PET|aiUnlocked|" + (PetProgress.AiUnlocked ? "yes" : "no"));
        /* 聚焦高亮强度：0 = 没聚焦，>0 = 有器皿被聚焦 */
        float maxGlow = 0f; int glowCount = 0;
        foreach (var vv in _views) {
            if (vv == null) continue;
            float g0 = vv.TestFocusGlow;
            if (g0 > 0.01f) { glowCount++; if (g0 > maxGlow) maxGlow = g0; }
        }
        sb.AppendLine("PET|glow|" + glowCount + "|" + maxGlow.ToString("F2"));
        /* 器皿在屏幕上的位置 —— 测试要点器皿时直接用，不要猜比例坐标
           （器皿每次生成的位置都不同，猜出来的坐标时灵时不灵）。 */
        foreach (var vv2 in _views) {
            if (vv2 == null || vv2.Model == null) continue;
            if (Cam == null) continue;
            var wsp = vv2.transform.position + Vector3.up * 0.25f;
            var ssp = Cam.WorldToScreenPoint(wsp);
            if (ssp.z <= 0.01f) continue;
            /* 固体体积：诊断「加了固体/沉淀却看不到」用 */
            {
                float sv = 0f;
                foreach (var kv in vv2.Model.Undissolved) { var ss = ChemData.Sub[kv.Key]; if (ss != null) sv += (float)(kv.Value * ss.Mass); }
                foreach (var kv in vv2.Model.Precipitates) { var ss = ChemData.Sub[kv.Key]; if (ss != null) sv += (float)(kv.Value * ss.Mass); }
                float sf = sv > 0 ? Mathf.Clamp(0.03f + sv / 60f, 0.02f, 0.34f) : 0f;
                bool sedOn = vv2.TestSedimentOn;
                sb.AppendLine("SOLID|" + vv2.Model.Id + "|未溶" + vv2.Model.Undissolved.Count
                            + " 沉淀" + vv2.Model.Precipitates.Count
                            + " 体积" + sv.ToString("F2") + " 比例" + sf.ToString("F3")
                            + " 渲染" + (sedOn ? "开" : "关"));
            }
            sb.AppendLine("STATE|vessel|" + vv2.Model.Id + "|"
                        + Mathf.RoundToInt(ssp.x) + "," + Mathf.RoundToInt(ssp.y));
        }

        /* 视觉特效强度：验证「容器空了特效必须停」 */
        foreach (var vv3 in _views) {
            if (vv3 == null || vv3.Model == null) continue;
            var fx = vv3.Model.Fx;
            if (fx.Bubble > 0.01f || fx.Smoke > 0.01f || fx.Glow > 0.01f || fx.Flame)
                sb.AppendLine("FX|" + vv3.Model.Id + "|泡" + fx.Bubble.ToString("F2")
                            + " 烟" + fx.Smoke.ToString("F2") + " 光" + fx.Glow.ToString("F2")
                            + " 焰" + (fx.Flame ? "开" : "关")
                            + " 液" + vv3.Model.Volume().ToString("F0"));
        }

        /* 器皿下面挂了哪些渲染物：排查「试管里那根竖条是什么」 */
        {
            var anyV = _views.Count > 0 ? _views[_views.Count - 1] : null;
            if (anyV != null) {
                var names = new List<string>();
                foreach (var mr in anyV.GetComponentsInChildren<MeshRenderer>(true)) {
                    if (!mr.enabled || !mr.gameObject.activeInHierarchy) continue;
                    var bnd = mr.bounds;
                    names.Add(mr.gameObject.name + "(h" + bnd.size.y.ToString("F2") + ")");
                }
                sb.AppendLine("PARTS|" + anyV.Model.Id + "|" + string.Join(" ", names));
            }
        }
        /* 可机读的点击目标：教程栏按钮、阿侬自己 */
        var tb = FindObjectOfType<TutorialBar>();
        if (tb != null) {
            foreach (var rt in tb.GetComponentsInChildren<RectTransform>(true)) {
                if (rt.name != "B") continue;
                var t = rt.GetComponentInChildren<Text>();
                if (t == null) continue;
                var sc2 = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
                sb.AppendLine("PET|CLICK|tut_" + t.text + "|" + Mathf.RoundToInt(sc2.x) + "|" + Mathf.RoundToInt(sc2.y)
                            + "|" + Mathf.RoundToInt(rt.rect.width) + "|" + Mathf.RoundToInt(rt.rect.height));
            }
        }
        if (pv != null) {
            var prt = pv.GetComponent<RectTransform>();
            if (prt != null) {
                /* 阿侬的贴图在子节点里，取最靠上的那个 Image 的位置 */
                foreach (var ir in pv.GetComponentsInChildren<Image>(true)) {
                    if (ir == null || ir.sprite == null) continue;
                    var irc = RectTransformUtility.WorldToScreenPoint(null, ir.rectTransform.TransformPoint(ir.rectTransform.rect.center));
                    sb.AppendLine("PET|CLICK|pet|" + Mathf.RoundToInt(irc.x) + "|" + Mathf.RoundToInt(irc.y)
                                + "|" + Mathf.RoundToInt(ir.rectTransform.rect.width) + "|" + Mathf.RoundToInt(ir.rectTransform.rect.height));
                    break;
                }
            }
        }
        return sb.ToString();
    }

    System.Collections.IEnumerator PetTick() {
        while (true) {
            yield return new WaitForSeconds(1.0f);
            Debug.Log("[PetState]\n" + PetAudit());
            /* 控件清单也要周期重打：试剂列表项会随搜索/分类变，
               只在启动时打一次的话，搜出来的试剂拿不到坐标 */
            if (_ui != null) Debug.Log("AUDITSTART\n" + _ui.TestClickTargets() + "AUDITEND\n");
            if (_ui != null) Debug.Log("[ReagentState]\n" + _ui.ReagentAudit());
            if (_ui != null) Debug.Log("[SearchProbe]\n" + _ui.ProbeSearch());
        }
    }

    void OpenValueDlg() {
        var s = EnthalpyScene.Instance;
        if (s == null || s.TestEdgeCount == 0) { Debug.Log("[DlgDemo] 没有连线"); return; }
        s.AskManualValue(s.TestFirstEdge(), true);
        Debug.Log("[DlgDemo] 已调用 AskManualValue");
    }

    void ForceCalcTut() {
        PetProgress.FinishLab(true);          /* 先让实验教程算完，否则起不来 */
        if (!PetTutorial.Running) PetTutorial.Start(TutorialKind.BornHaber);
    }

    void DumpTagAudit() { Debug.Log(TagAudit()); }

    /// <summary>在侧栏、工具栏和日志以外的可见区域安置世界空间标签。</summary>
    static Rect PlaceOverlayRect(Rect baseRect,Rect safe,List<Rect> placed) {
        const float gap=8f;
        float maxX=Mathf.Max(safe.xMin,safe.xMax-baseRect.width);
        float maxY=Mathf.Max(safe.yMin,safe.yMax-baseRect.height);
        var xs=new List<float>{baseRect.xMin,safe.xMin,maxX};
        var ys=new List<float>{baseRect.yMin,safe.yMin,maxY};
        foreach(var p in placed){
            xs.Add(p.xMin);xs.Add(p.xMax+gap);xs.Add(p.xMin-baseRect.width-gap);
            ys.Add(p.yMin);ys.Add(p.yMax+gap);ys.Add(p.yMin-baseRect.height-gap);
        }
        float best=float.PositiveInfinity;Rect chosen=baseRect;
        foreach(float xx in xs)foreach(float yy in ys){
            var candidate=new Rect(Mathf.Clamp(xx,safe.xMin,maxX),Mathf.Clamp(yy,safe.yMin,maxY),baseRect.width,baseRect.height);
            float overlap=0;
            foreach(var p in placed){
                float iw=Mathf.Max(0,Mathf.Min(candidate.xMax,p.xMax+gap)-Mathf.Max(candidate.xMin,p.xMin-gap));
                float ih=Mathf.Max(0,Mathf.Min(candidate.yMax,p.yMax+gap)-Mathf.Max(candidate.yMin,p.yMin-gap));
                overlap+=iw*ih;
            }
            float dx=candidate.center.x-baseRect.center.x,dy=candidate.center.y-baseRect.center.y;
            float score=overlap*10000f+dx*dx+dy*dy;
            if(score<best){best=score;chosen=candidate;}
        }
        return chosen;
    }

    void ResolveTagOverlaps() {
        var live = new List<VesselTag>();
        // Sync even currently hidden cards so leaving the explicit metered
        // frame can reactivate the normal bench labels next frame.
        foreach (var t in _tags) if (t != null) {
            // Existing labels return on the next ordinary frame after printing.
            if (Printer != null && Printer.Busy) { t.gameObject.SetActive(false); continue; }
            t.ExtraLift=0f;t.ScreenOffset=Vector2.zero;t.Sync();
            if(t.RenderVisible)live.Add(t);
        }
        if(live.Count==0)return;
        live.Sort((a,b)=>{
            int x=a.ScreenRect.center.x.CompareTo(b.ScreenRect.center.x);
            return x!=0?x:string.CompareOrdinal(a.Model?.Id,b.Model?.Id);
        });
        var safe=_ui!=null?_ui.TagWorkspaceRect():Rect.MinMaxRect(12,12,Screen.width-12,Screen.height-54);
        var placed=new List<Rect>();
        foreach(var tag in live){
            var baseRect=tag.ScreenRect;
            if(baseRect.width<=0||baseRect.height<=0)continue;
            var chosen=PlaceOverlayRect(baseRect,safe,placed);
            tag.ScreenOffset=new Vector2(chosen.xMin-baseRect.xMin,chosen.yMin-baseRect.yMin);
            tag.Sync();placed.Add(tag.ScreenRect);
        }
    }

    void ResolveEquationOverlaps() {
        if(_eqTags.Count==0)return;
        var safe=_ui!=null?_ui.TagWorkspaceRect():Rect.MinMaxRect(12,12,Screen.width-12,Screen.height-54);
        var placed=new List<Rect>();
        foreach(var tag in _tags)if(tag!=null&&tag.RenderVisible&&tag.gameObject.activeInHierarchy){
            var r=tag.ScreenRect;if(r.width>0&&r.height>0)placed.Add(r);
        }
        foreach(var eq in _eqTags)if(eq!=null&&eq.gameObject.activeInHierarchy){
            eq.ScreenOffset=Vector2.zero;
            if(!eq.Sync())continue;
            var baseRect=eq.ScreenRect;
            if(baseRect.width<=0||baseRect.height<=0)continue;
            var chosen=PlaceOverlayRect(baseRect,safe,placed);
            eq.ScreenOffset=new Vector2(chosen.xMin-baseRect.xMin,chosen.yMin-baseRect.yMin);
            eq.Sync();placed.Add(eq.ScreenRect);
        }
    }

    /// <summary>计算器状态定期打印（外部测试脚本读日志判定，不靠猜）</summary>
    System.Collections.IEnumerator StateTick(EnthalpyScene cs) {
        while (true) {
            yield return new WaitForSeconds(1.2f);
            if (cs != null) Debug.Log("[CalcState]\n" + cs.StateAudit());
            /* 选单是临时出现的，控件清单必须每秒重打一次，否则外部脚本拿不到菜单项位置 */
            if (cs != null) Debug.Log("AUDITSTART\n" + cs.AuditLayout() + "AUDITEND\n");
            Debug.Log("[PetState]\n" + PetAudit());
        }
    }

    void DumpInputAudit() {
        var s = EnthalpyScene.Instance;
        if (s != null) Debug.Log(s.AuditInput());
    }

    void UpdateCamera(float dt) {
        float D = _dist;
        float cy = Mathf.Cos(_yaw), sy = Mathf.Sin(_yaw);
        float cp = Mathf.Cos(_pitch), sp = Mathf.Sin(_pitch);
        var eye = _focus + new Vector3(D * sy * cp, D * sp, D * cy * cp);
        Cam.transform.position = eye;
        Cam.transform.LookAt(_focus, Vector3.up);

        // 自由相机不再施加随朝向变化的 UI 补偿位移。
    }

    /* ---------------- 演示内容 ---------------- */
    /// <summary>命令行 -eqDemo 时用的验证场景：一只烧杯放正中，触发一个会放气的反应，
    /// 便于截图核对弹出的方程式标签（连接符、条件、上下标）。</summary>
    void SeedEqDemo() {
        var b = Lab.AddVessel(VesselKind.Beaker);
        b.AddWater(120);
        Lab.AddReagent(b, "na2co3", 3.0);
        Lab.AddLiquid(b, "hcl", 12);
        Lab.SetHeat(b, true);
        EqTagSettings.Duration = 120f;      // 截图期间不消失
        TagSettings.ShowTags = false;       // 验证场景里只留方程式标签，便于核对
        Lab.Log.Clear();
        Lab.Note(null, "方程式标签验证场景", "setup");
    }

    void SeedDemo() {
        var t1 = Lab.AddVessel(VesselKind.Tube);
        var b1 = Lab.AddVessel(VesselKind.Beaker);
        var f1 = Lab.AddVessel(VesselKind.Flask);
        b1.AddWater(190);
        Lab.AddReagent(b1, "cuso4", 4.0);
        Lab.AddReagent(b1, "naoh", 1.2);
        t1.AddWater(24);
        Lab.AddReagent(t1, "kmno4", 0.25);
        f1.AddWater(180);
        Lab.AddReagent(f1, "ki", 1.5);
        Lab.AddReagent(f1, "agno3", 1.0);

        /* 第四只：锌 + 稀盐酸，常温下即可放出氢气；不要让默认场景持续加热至蒸干。 */
        var b2 = Lab.AddVessel(VesselKind.Beaker);
        b2.AddWater(150);
        Lab.AddReagent(b2, "zn", 4.0);
        Lab.AddLiquid(b2, "hcl", 18);

        Lab.Log.Clear();
        Lab.Note(null, "实验台已就绪。左侧选器材；从右侧拖试剂到器皿，松开后输入数量并确认加入。", "setup");
    }


    /* ================================================================
       教程进度条：顶在画面上方，显示到第几步、以及「下一步 / 跳过」
       ================================================================ */
    RectTransform _tutBar; Text _tutText;
    public void BuildTutorialBar() {
        if (_tutBar != null) { _tutBar.gameObject.SetActive(true); RefreshTutorialBar(); return; }
        var go = new GameObject("TutorialBar", typeof(RectTransform));
        go.transform.SetParent(_ui.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.10f, 0.16f, 0.22f, 0.97f);
        _tutBar = (RectTransform)go.transform;
        _tutBar.anchorMin = new Vector2(0.5f, 1); _tutBar.anchorMax = new Vector2(0.5f, 1);
        _tutBar.pivot = new Vector2(0.5f, 1);
        _tutBar.anchoredPosition = new Vector2(0, -46);   /* 让开顶栏那一行 */
        _tutBar.sizeDelta = new Vector2(660, 34);
        _tutBar.SetAsLastSibling();                        /* 压在其它 UI 之上 */

        /* 单独挂一层 Canvas 提高排序，保证不被方程式标签盖住 */
        var cgo = new GameObject("TopCanvas", typeof(RectTransform));
        cgo.transform.SetParent(_tutBar, false);
        var tc = cgo.AddComponent<Canvas>();
        tc.overrideSorting = true; tc.sortingOrder = 60;
        var cg = cgo.AddComponent<UnityEngine.UI.GraphicRaycaster>(); cg.enabled = false;
        var crt = (RectTransform)cgo.transform;
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

        var tgo = new GameObject("T", typeof(RectTransform));
        tgo.transform.SetParent(crt, false);
        _tutText = tgo.AddComponent<Text>();
        _tutText.font = LabUI.UiFont; _tutText.fontSize = 14;
        _tutText.color = new Color(0.88f, 0.95f, 1f);
        _tutText.alignment = TextAnchor.MiddleLeft;
        _tutText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _tutText.raycastTarget = false;
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14, 0); trt.offsetMax = new Vector2(-210, 0);

        MakeTutBtn("下一步 ▶", -104, () => PetTutorial.Next());
        MakeTutBtn("跳过教程 ⏭", -8, () => PetTutorial.Skip());
        RefreshTutorialBar();
    }
    void MakeTutBtn(string label, float x, System.Action act) {
        var go = new GameObject("B", typeof(RectTransform));
        var top = _tutBar.Find("TopCanvas");
        go.transform.SetParent(top != null ? top : _tutBar, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.20f, 0.32f, 0.42f, 1f);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0);
        rt.sizeDelta = new Vector2(94, 26);
        var b = go.AddComponent<Button>(); b.targetGraphic = img;
        b.onClick.AddListener(() => act());
        var tgo = new GameObject("T", typeof(RectTransform));
        tgo.transform.SetParent(rt, false);
        var t = tgo.AddComponent<Text>();
        t.font = LabUI.UiFont; t.fontSize = 12; t.color = Color.white;
        t.text = label; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
    }
    public string TutAudit() {
        if (_tutBar == null) return "[TutAudit] 进度条根本没创建";
        var crt = (RectTransform)_ui.transform;
        var p0 = _tutBar.TransformPoint(_tutBar.rect.min);
        var p1 = _tutBar.TransformPoint(_tutBar.rect.max);
        return string.Format("[TutAudit] 创建={0} 激活={1} 位置 x {2:F0}..{3:F0} y {4:F0}..{5:F0} 文案「{6}」 父={7} 子节点={8}",
            true, _tutBar.gameObject.activeInHierarchy,
            Mathf.Min(p0.x, p1.x), Mathf.Max(p0.x, p1.x),
            Mathf.Min(p0.y, p1.y), Mathf.Max(p0.y, p1.y),
            _tutText != null ? _tutText.text : "(无)", _tutBar.parent.name, _tutBar.childCount);
    }

    /// <summary>实验台界面的显隐。
    /// 用 SetActive 而不是改透明度 —— 藏起来之后就不能再点了，
    /// 否则计算器背后会有一堆看不见但能点的按钮。 </summary>
    public void SetLabUiVisible(bool on) {
        if (_ui != null) _ui.gameObject.SetActive(on);
    }
    /// <summary>计算器是否正开着（开着时实验台的输入要全部忽略）</summary>
    public static bool CalculatorActive = false;

    public void RefreshTutorialBar() {
        if (_tutBar == null || _tutText == null) return;
        if (!PetTutorial.Running) { _tutBar.gameObject.SetActive(false); return; }
        _tutBar.gameObject.SetActive(true);
        int cur = Mathf.Min(PetTutorial.StepIndex + 1, PetTutorial.StepCount);
        string kind = PetTutorial.Current == TutorialKind.IonTest ? "实验教程" : "计算器教程";
        string hint = PetTutorial.WaitingOnUser ? "  ← 等你操作" : "";
        _tutText.text = $"{kind}  {cur} / {PetTutorial.StepCount}   {PetTutorial.StepTitle}{hint}";
    }
    public void HideTutorialBar() { if (_tutBar != null) _tutBar.gameObject.SetActive(false); }

    public void SyncViews() {
        EnsureInit();
        /* 新容器补视图，已删除的移除 */
        for (int i = 0; i < Lab.Vessels.Count; i++) {
            var v = Lab.Vessels[i];
            if (_bound.Contains(v)) continue;
            var nv = VesselView.Create(v, transform, _glassTpl, _liqTpl, _sedTpl, _fxSoft, _fxAdd, _glowMat);
            if (nv == null) return;
            _views.Add(nv);
            _bound.Add(v);
        }
        for (int i = _views.Count - 1; i >= 0; i--) {
            if (!Lab.Vessels.Contains(_views[i].Model)) {
                Destroy(_views[i].gameObject);
                _bound.Remove(_views[i].Model);
                _views.RemoveAt(i);
            }
        }
        foreach (var vv in _views) {
            /* 抬高 3mm：容器底面和台面完全共面会 z-fighting，看起来一闪一闪的 */
            vv.transform.localPosition = vv.Model.Pos + Vector3.up * (vv.Model.LiftY + 0.003f);
            vv.Refresh();
            vv.SyncMeteredFlow(WorldClock.DeltaTime);
            DistillationView.SyncFor(vv,_glassTpl,_metalMat);
        }
        foreach (var a in ApparatusSet.All) {
            if (!_apViews.Any(x => x != null && x.Model == a))
                _apViews.Add(ApparatusView.Create(a, transform, _metalMat, _glowMat, _flameMat));
        }
        for (int i = _apViews.Count - 1; i >= 0; i--) {
            var av = _apViews[i];
            if (av == null || !ApparatusSet.All.Contains(av.Model)) {
                if (av != null) { av.gameObject.SetActive(false); Destroy(av.gameObject); }
                _apViews.RemoveAt(i);
            } else av.Sync();
        }

        /* 容器标签：新容器补 tag，消失的移除 */
        if (_tagRoot != null && LabUI.UiFont != null) {
          if (TagSettings.ShowTags) foreach (var v in Lab.Vessels) {
                if (Printer != null && Printer.IsPrinting(v)) continue;
                if (_tags.Any(x => x != null && x.Model == v)) continue;
                var nt = VesselTag.Create(v, _tagRoot, LabUI.UiFont, Cam);
                /* 按容器在列表里的位置分层错位 */
                nt.Slot = 0; // Screen-space packing handles stagger without pushing cards off the top.
                _tags.Add(nt);
            }
            for (int i = _tags.Count - 1; i >= 0; i--) {
                if (_tags[i] == null) { _tags.RemoveAt(i); continue; }
                if (!Lab.Vessels.Contains(_tags[i].Model)) {
                    Destroy(_tags[i].gameObject);
                    _tags.RemoveAt(i);
                } else if (Printer != null && Printer.IsPrinting(_tags[i].Model)) {
                    Destroy(_tags[i].gameObject);
                    _tags.RemoveAt(i);
                }
            }
            ResolveTagOverlaps();

        /* ---- 反应方程式弹出标签 ---- */
        if (_tagRoot != null && LabUI.UiFont != null) {
            /* 新发生的反应 -> 建标签 */
            foreach (var ev in Lab.RecentReactions) {
                if (ev == null || ev.Sequence <= _eqSeen) continue;
                _eqSeen = ev.Sequence;
                if (ev.Rule == null || !Lab.Vessels.Contains(ev.Vessel)) continue;
                var et = EquationTag.Create(ev, _tagRoot, LabUI.UiFont, Cam);
                /* 同一容器上已有几个标签就往上错开几位，避免叠在一起 */
                int sameHere = 0;
                foreach (var x in _eqTags)
                    if (x != null && x.Ev != null && x.Ev.Vessel == ev.Vessel) sameHere++;
                et.Slot = sameHere;
                EquationTag.Active.Add(et);
                _eqTags.Add(et);
            }
            /* 超时的移除 */
            for (int i = _eqTags.Count - 1; i >= 0; i--) {
                if (_eqTags[i] == null) { _eqTags.RemoveAt(i); continue; }
                if (!_eqTags[i].Sync()) {
                    EquationTag.Active.Remove(_eqTags[i]);
                    Destroy(_eqTags[i].gameObject);
                    _eqTags.RemoveAt(i);
                }
            }
            foreach(var eq in _eqTags)if(eq!=null){
                bool relevant=_framedMetered==null||eq.Ev?.Vessel==_framedMetered||
                    eq.Ev?.Vessel==_framedMetered.AttachedTo;
                relevant &= Printer == null || !Printer.Busy;
                if(eq.gameObject.activeSelf!=relevant)eq.gameObject.SetActive(relevant);
            }
            ResolveEquationOverlaps();
        }
        }
        /* 检验器材：新出现的补视图 */
        for (int i = 0; i < TestSet.All.Count; i++) {
            var t = TestSet.All[i];
            if (_testViews.Any(x => x != null && x.Model == t)) continue;
            _testViews.Add(TestItemView.Create(t, transform, _paperMat, _woodMat, _flameMat));
        }
        for (int i = _testViews.Count - 1; i >= 0; i--) {
            if (_testViews[i] == null || !TestSet.All.Contains(_testViews[i].Model)) {
                if (_testViews[i] != null) Destroy(_testViews[i].gameObject);
                _testViews.RemoveAt(i);
                continue;
            }
            _testViews[i].Sync();
        }

        /* 教程推进：每帧检查一次，等条件成立就进下一步 */
        PetTutorial.Tick();
    }


    void Update() {
        Localization.Tick();
        /* 实验时间只在这里被缩放进世界：world_speed 0 会冻结反应、热力、
           固体/液体求解和计量器流动，而相机、菜单、标签、阿侬与控制台自己
           仍然走真实帧时间，所以冻结时界面照常可用。 */
        float world = WorldClock.DeltaTime;
        WorldClock.AddElapsed(world);
        if (world > 0f) Lab.Tick(world);
        if(PourTransaction.IsActive)return;
        if (world > 0f) ApparatusSet.Tick(world);
        FollowAttachments();
        if (world > 0f) { TestSet.Tick(world); LabContacts.Tick(); }
        /* 爆鸣：引擎里立了标记，这里负责真的发出声音 */
        foreach (var v in Lab.Vessels) {
            if (v.Fx.Pop > 0f) {
                ChemAudio.Play("pop", Mathf.Clamp01(v.Fx.Pop));
                v.Fx.Pop = 0f;
            }
        }
        foreach (var t in TestSet.All) {
            if (t.PendingSound > 0f) {
                if(t.PendingSoundKind != "pop" || t.PendingSoundVessel == null)
                    ChemAudio.Play(t.PendingSoundKind, t.PendingSound);
                t.PendingSoundVessel = null;
                t.PendingSound = 0f;
            }
        }
        WatchPositions();
        SyncViews();
        _dist += (_distT - _dist) * Mathf.Min(1f, Time.deltaTime * 6f);
        foreach (var vv in _views) { vv.FxTime += world; vv.FxCam = Cam; }
        HandleInput();
        UpdateCamera(Time.deltaTime);
        if (_ui != null) { _ui.RefreshVesselPanel(); _ui.RefreshLog(); }
    }

    /* ---------------- 交互 ---------------- */
    Vessel _dragging;
    SnapController.Placement _dragStartPlacement;
    Vector3 _grabOffset;
    float _dragPlaneY;
    Vector2 _clickStart;
    double _clickTime;
    bool _orbiting;
    Vector3 _lastMouse;
    Vessel _snapVessel;          // 本次拖动命中的容器吸附目标
    Apparatus _snapApp;          // 命中的器材吸附目标

    /// <summary>松开鼠标时把吸附关系落实下来</summary>
    void CommitSnap(Vessel v) {
        if (v == null) return;
        if (_snapVessel != null) {
            if(MeteredTransfer.IsTool(v)){if(SnapController.AttachMetered(v,_snapVessel))Lab.Note(v,"已吸附，可使用专用量器面板","setup");return;}
            v.AttachedTo = _snapVessel;
            Lab.Note(v, $"吸附在 {VesselNames.Name(_snapVessel.Kind)} 上方", "setup");
        } else if (_snapApp != null) {
            if (_snapApp.Kind == ApparatusKind.BunsenBurner) { ApparatusSet.Attach(v, _snapApp); Lab.Note(v, "放到本生灯上方", "setup"); }
            else { v.TripodRef = _snapApp; _snapApp.Holds = v; Lab.Note(v, "架到三脚架上", "setup"); }
        }
    }

    void UpdateHighlights() {
        foreach (var av in _apViews) if (av != null) av.SetHighlight(av.Model == _snapApp);
        foreach (var vv in _views) if (vv != null) vv.SetHighlight(vv.Model == _snapVessel);
    }
    void ClearHighlights() {
        _snapVessel = null; _snapApp = null;
        foreach (var av in _apViews) if (av != null) av.SetHighlight(false);
        foreach (var vv in _views) if (vv != null) vv.SetHighlight(false);
    }

    /// <summary>被吸附的容器要跟着宿主一起动</summary>
    void FollowAttachments() {
        foreach (var v in Lab.Vessels) {
            if (v.AttachedTo != null) {
                v.Pos = v.AttachedTo.Pos;
                v.LiftY = v.AttachedTo.LiftY + SnapController.LiftOnto(v.AttachedTo, v);
            } else if (v.Burner != null) {
                v.Pos = v.Burner.Pos;
                v.LiftY = SnapController.LiftOntoApparatus(v.Burner);
            } else if (v.TripodRef != null) {
                v.Pos = v.TripodRef.Pos;
                v.LiftY = SnapController.LiftOntoApparatus(v.TripodRef);
            }
        }
        foreach (var test in TestSet.All) SplintSnap.Follow(test);
    }


    /* ================================================================
       交互
         · 左键点任何东西 → 聚焦到它；点空白 → 取消聚焦
         · WASD → 世界坐标平移（相机相对），同时取消聚焦
         · 长按左键拖动 → 旋转视角（短按不算，免得和「点击聚焦」打架）
         · 右键器皿 → 针对它的操作选单
         · 器皿之外的东西（本生灯、三脚架、石蕊、木条）也能拖
       ================================================================ */
    Vector3 _focusTarget;                 // 聚焦目标（相机会看着它）
    bool _hasFocus;
    double _pressTime;
    Vector2 _pressPos;
    LabPick.Hit _pressHit;
    Apparatus _dragApp;
    TestItem _dragTest;

    void FocusOn(Vector3 p) { _focusTarget = p; _hasFocus = true; }
    void ClearFocus() { _hasFocus = false; }
    /* ================================================================
       聚焦与轮廓高亮
         · 左键点任何东西（器皿 / 器材 / 检验道具）→ 聚焦它并高亮轮廓
         · 点空白处 → 取消聚焦、取消高亮
       ================================================================ */
    object _focused;
    OutlineFx _focusOutline;
    VesselView _focusVessel;

    /// <summary>设聚焦对象；传 null 取消聚焦（同时取消高亮）</summary>
    public void SetFocus(object target) {
        if (ReferenceEquals(_focused, target)) return;
        if (_focusOutline != null) { _focusOutline.SetOn(false); _focusOutline = null; }
        if (_focusVessel != null) { _focusVessel.SetFocusGlow(0f); _focusVessel = null; }
        _focused = target;
        if (target == null) return;

        Transform tr = null;
        if (target is Vessel v) {
            var vv = _views.Find(x => x != null && x.Model == v);
            tr = vv != null ? vv.transform : null;
        } else if (target is Apparatus a) {
            var av = _apViews.Find(x => x != null && x.Model == a);
            tr = av != null ? av.transform : null;
        } else if (target is TestItem t) {
            var tv = _testViews.Find(x => x != null && x.Model == t);
            tr = tv != null ? tv.transform : null;
        }
        if (tr == null) return;
        /* 器皿是透明的 —— 用玻璃着色器的菲涅尔辉光，不要反向壳描边。
           不透明的器材 / 道具才走反向壳。 */
        if (target is Vessel) {
            var fv = _views.Find(x => x != null && x.Model == (Vessel)target);
            if (fv != null) { fv.SetFocusGlow(1.35f); _focusVessel = fv; }
            return;
        }
        _focusOutline = OutlineFx.Attach(tr, new Color(0.30f, 0.85f, 1f), 0.0035f);
        if (_focusOutline != null) _focusOutline.SetOn(true);
    }
    public object Focused => _focused;
    public string TestSelectedVesselId => (_focused as Vessel)?.Id ?? "-";
    // API/EventSystem regression runs must not mix in the desktop user's keys.
    public bool SuppressDesktopInputForAudit { get; set; }

    public void MoveView(float horizontal, float vertical, float dt) {
        var forward = Vector3.ProjectOnPlane(Cam.transform.forward, Vector3.up).normalized;
        var right = Vector3.ProjectOnPlane(Cam.transform.right, Vector3.up).normalized;
        _focus += Vector3.ClampMagnitude(right * horizontal + forward * vertical, 1f) * (dt * 2.5f);
    }

    public void RotateView(Vector2 delta) {
        var eye = Cam.transform.position;
        // About 0.20 degrees per pixel horizontally; small corrections no
        // longer whip the camera across the laboratory.
        _yaw -= delta.x * 0.0035f;
        _pitch = Mathf.Clamp(_pitch + delta.y * 0.003f, -1.45f, 1.45f);
        _focus = eye - new Vector3(_dist * Mathf.Sin(_yaw) * Mathf.Cos(_pitch),
            _dist * Mathf.Sin(_pitch), _dist * Mathf.Cos(_yaw) * Mathf.Cos(_pitch));
    }

    // An explicit one-shot composition aid, never a tracking/focus lock. The
    // actual 530/820 mm tools remain at their physical scale; free camera
    // movement works immediately after framing them.
    public bool MeteredFrameActiveFor(Vessel tool)=>_framedMetered==tool;
    public Vessel FramedMeteredTool=>_framedMetered;
    public void ToggleMeteredFrame(Vessel tool){
        if(_framedMetered==tool){
            _focus=_beforeMeteredFocus;_yaw=_beforeMeteredYaw;_pitch=_beforeMeteredPitch;
            _dist=_beforeMeteredDistance;_distT=_beforeMeteredDistanceTarget;
            _framedMetered=null;UpdateCamera(0f);return;
        }
        if(tool==null||!MeteredTransfer.IsTool(tool)||Cam==null)return;
        if(_framedMetered==null){
            _beforeMeteredFocus=_focus;_beforeMeteredYaw=_yaw;_beforeMeteredPitch=_pitch;
            _beforeMeteredDistance=_dist;_beforeMeteredDistanceTarget=_distT;
        }
        _framedMetered=tool;
        float bottom=tool.AttachedTo==null?tool.Pos.y+tool.LiftY:
            Mathf.Min(tool.Pos.y+tool.LiftY,tool.AttachedTo.Pos.y+tool.AttachedTo.LiftY);
        float top=tool.Pos.y+tool.LiftY+VesselMesh.Get(tool.Kind).H;
        float span=Mathf.Max(.5f,top-bottom);
        // The upper toolbar and lower experiment log leave roughly 65% of the
        // viewport for the 3D subject. Aim below its midpoint so the receiver
        // is lifted above the bottom log instead of disappearing behind it.
        _focus=new Vector3(tool.Pos.x,bottom+span*.42f,tool.Pos.z);
        _distT=_dist=Mathf.Max(7.4f,span/(2f*Mathf.Tan(Cam.fieldOfView*Mathf.Deg2Rad*.5f)*.65f));
        UpdateCamera(0f);
    }

    public void FrameDistillation(Vessel source){
        if(source?.Distillation==null)return;
        var rig=source.Distillation;
        Physics.SyncTransforms();
        var members=_views.FindAll(v=>v.Model==source||v.Model==rig.Receiver);
        var points=new List<Vector3>();var samples=new List<Vector3>();
        foreach(var view in members){
            var body=VesselMesh.Get(view.Model.Kind);
            samples.Add(view.transform.position+Vector3.up*body.H*.5f);
            foreach(var collider in view.GetComponentsInChildren<Collider>()){
                if(!collider.enabled)continue;
                var b=collider.bounds;
                for(int mask=0;mask<8;mask++)points.Add(b.center+Vector3.Scale(b.extents,
                    new Vector3((mask&1)==0?-1:1,(mask&2)==0?-1:1,(mask&4)==0?-1:1)));
                if(collider.name=="Condenser water jacket")samples.Add(b.center);
            }
        }
        if(points.Count==0)return;
        var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
        // A few center rays miss thin tripod legs and the edge of a wire gauze.
        // Include the complete silhouette above the tabletop when evaluating views.
        foreach(var point in points)if(point.y>bounds.min.y+.12f)samples.Add(point);
        Rect area=_ui!=null?_ui.TagWorkspaceRect():new Rect(0,0,Screen.width,Screen.height);
        float pad=Mathf.Min(24,Mathf.Min(area.width,area.height)*.08f);
        area=Rect.MinMaxRect(area.xMin+pad,area.yMin+pad,area.xMax-pad,area.yMax-pad);
        float tan=Mathf.Tan(Cam.fieldOfView*Mathf.Deg2Rad*.5f),aspect=Cam.aspect;
        Vector2 offset=new Vector2(area.center.x/Screen.width*2-1,area.center.y/Screen.height*2-1);
        float bestScore=float.PositiveInfinity,bestYaw=_yaw,bestDistance=8;Vector3 bestFocus=bounds.center;
        // Search real viewpoints, never hide foreground glass or bypass picking.
        // This is one-shot framing; subsequent WASD/drag remain fully free.
        for(int candidate=0;candidate<12;candidate++){
            float yaw=_yaw+candidate*Mathf.PI/6, pitch=.18f;
            var backward=new Vector3(Mathf.Sin(yaw)*Mathf.Cos(pitch),Mathf.Sin(pitch),Mathf.Cos(yaw)*Mathf.Cos(pitch));
            var rotation=Quaternion.LookRotation(-backward,Vector3.up);
            var right=rotation*Vector3.right;var up=rotation*Vector3.up;var forward=-backward;
            float distance=Mathf.Max(2,bounds.size.magnitude);Vector3 focus=Vector3.zero,eye=Vector3.zero;
            for(int step=0;step<100;step++){
                focus=bounds.center-right*(offset.x*distance*tan*aspect)-up*(offset.y*distance*tan);
                eye=focus+backward*distance;bool fits=true;
                foreach(var point in points){
                    Vector3 delta=point-eye;float depth=Vector3.Dot(delta,forward);
                    var pixel=new Vector2((.5f+Vector3.Dot(delta,right)/(2*depth*tan*aspect))*Screen.width,
                        (.5f+Vector3.Dot(delta,up)/(2*depth*tan))*Screen.height);
                    if(depth<Cam.nearClipPlane||!area.Contains(pixel)){fits=false;break;}
                }
                if(fits)break;distance*=1.08f;
            }
            int blocked=0;
            foreach(var sample in samples){
                Vector3 ray=sample-eye;
                foreach(var hit in Physics.RaycastAll(eye,ray.normalized,ray.magnitude,~0,QueryTriggerInteraction.Ignore)){
                    var vessel=hit.collider.GetComponentInParent<VesselView>();
                    if(vessel!=null&&(vessel.Model==source||vessel.Model==rig.Receiver))continue;
                    blocked++;break;
                }
            }
            float foreshortening=rig.Reflux?0:Mathf.Abs(Vector3.Dot(forward,Vector3.right))*12;
            float score=blocked*1000+distance+foreshortening+Mathf.Abs(Mathf.DeltaAngle(_yaw*Mathf.Rad2Deg,yaw*Mathf.Rad2Deg))*.002f;
            if(score<bestScore){bestScore=score;bestYaw=yaw;bestDistance=distance;bestFocus=focus;}
        }
        _yaw=bestYaw;_pitch=.18f;_focus=bestFocus;_distT=_dist=bestDistance;
        UpdateCamera(0);
    }

    // One-shot framing puts the complete printing volume above the log and
    // between the shelves. It never follows the station or locks player input.
    public void FrameConstruction(Bounds bounds) {
        var points = new List<Vector3>();
        for (int mask = 0; mask < 8; mask++) points.Add(bounds.center + Vector3.Scale(bounds.extents,
            new Vector3((mask & 1) == 0 ? -1 : 1, (mask & 2) == 0 ? -1 : 1, (mask & 4) == 0 ? -1 : 1)));
        Rect area = _ui != null ? _ui.TagWorkspaceRect() : new Rect(0, 0, Screen.width, Screen.height);
        float pad = Mathf.Min(24, Mathf.Min(area.width, area.height) * .08f);
        area = Rect.MinMaxRect(area.xMin + pad, area.yMin + pad, area.xMax - pad, area.yMax - pad);
        float tan = Mathf.Tan(Cam.fieldOfView * Mathf.Deg2Rad * .5f), aspect = Cam.aspect;
        var offset = new Vector2(area.center.x / Screen.width * 2 - 1, area.center.y / Screen.height * 2 - 1);
        float bestScore = float.PositiveInfinity, bestYaw = _yaw, bestDistance = 8;
        Vector3 bestFocus = bounds.center;
        for (int candidate = 0; candidate < 12; candidate++) {
            float yaw = _yaw + candidate * Mathf.PI / 6, pitch = .24f;
            var backward = new Vector3(Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Cos(yaw) * Mathf.Cos(pitch));
            var rotation = Quaternion.LookRotation(-backward, Vector3.up);
            var right = rotation * Vector3.right; var up = rotation * Vector3.up; var forward = -backward;
            float distance = Mathf.Max(3, bounds.size.magnitude); Vector3 focus = bounds.center, eye = Vector3.zero;
            for (int step = 0; step < 100; step++) {
                focus = bounds.center - right * (offset.x * distance * tan * aspect) - up * (offset.y * distance * tan);
                eye = focus + backward * distance; bool fits = true;
                foreach (var point in points) {
                    var delta = point - eye; float depth = Vector3.Dot(delta, forward);
                    var pixel = new Vector2((.5f + Vector3.Dot(delta, right) / (2 * depth * tan * aspect)) * Screen.width,
                        (.5f + Vector3.Dot(delta, up) / (2 * depth * tan)) * Screen.height);
                    if (depth < Cam.nearClipPlane || !area.Contains(pixel)) { fits = false; break; }
                }
                if (fits) break; distance *= 1.08f;
            }
            int blocked = 0;
            foreach (var point in new[] { bounds.center, bounds.center + Vector3.up * bounds.extents.y * .6f,
                bounds.center - Vector3.up * bounds.extents.y * .6f }) {
                var ray = point - eye;
                if (Physics.Raycast(eye, ray.normalized, ray.magnitude, ~0, QueryTriggerInteraction.Ignore)) blocked++;
            }
            float score = blocked * 1000 + distance + Mathf.Abs(Mathf.DeltaAngle(_yaw * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg)) * .002f;
            if (score < bestScore) { bestScore = score; bestYaw = yaw; bestDistance = distance; bestFocus = focus; }
        }
        _framedMetered = null;
        _yaw = bestYaw; _pitch = .24f; _focus = bestFocus; _distT = _dist = bestDistance;
        UpdateCamera(0);
    }
    public void ShowObjectMenu(LabPick.Hit hit, Vector2 screen) {
        if (hit == null || !hit.Any) { PetMenu.Close(); return; }
        SetFocus((object)hit.vessel ?? (object)hit.apparatus ?? hit.testItem);
        var items = new List<(string, Action)>();
        if (hit.vessel != null) {
            var v = hit.vessel;
            _ui?.SelectExternal(v);
            if(v.Kind==VesselKind.Bottle)items.Add((v.Sealed?"打开并释放气相":"密闭模拟气密瓶",()=>{
                v.SetSealed(!v.Sealed);
                Lab.Note(v,v.Sealed?"气密瓶已密闭：气体保留并计入分压；仅为教学模拟，不代表普通玻璃瓶可以承压":"已打开气密瓶，释放头部气相并恢复外界压力","setup");
            }));
            items.Add((v.Heating ? "停止加热" : "加热", () => Lab.SetHeat(v, !v.Heating)));
            DistillationView.AddMenu(v,screen,items);
            items.Add(("搅拌", () => Lab.SetStir(v, true)));
            items.Add(("点燃", () => Lab.Ignite(v)));
            items.Add(("倒空并洗净", () => Lab.Empty(v)));
            items.Add(("解除吸附", () => SnapController.Detach(v)));
            items.Add(("移走", () => {
                SnapController.Detach(v);
                foreach (var other in Lab.Vessels) if (other.AttachedTo == v) SnapController.Detach(other);
                Lab.RemoveVessel(v); SetFocus(null);
            }));
        } else if (hit.apparatus != null) {
            var a = hit.apparatus;
            if (a.Kind == ApparatusKind.BunsenBurner) {
                items.Add((a.Lit ? "熄灭本生灯" : "点燃本生灯", () => a.Lit = !a.Lit && a.CanLight));
                items.Add(("调节火力…", () => _ui.ShowBurnerDialog(a)));
            }
            items.Add(("放回台面", () => a.Pos = new Vector3(a.Pos.x, 0, a.Pos.z)));
            items.Add(("移走", () => {
                foreach (var v in Lab.Vessels) if (v.Burner == a || v.TripodRef == a) SnapController.Detach(v);
                foreach (var test in TestSet.All) if (test.AttachedBurner == a) test.AttachedBurner = null;
                ApparatusSet.All.Remove(a); SetFocus(null);
            }));
        } else {
            var t = hit.testItem;
            if (t.Kind == TestItemKind.Splint) {
                if (t.Splint == SplintState.Burning)
                    items.Add(("熄灭木条（保留余烬20秒）", () => { SplintSnap.MoveAside(t); GasTest.Extinguish(t); }));
                else items.Add(("点燃木条", () => GasTest.Light(t)));
                if (t.AttachedBurner != null) items.Add(("解除吸附", () => SplintSnap.MoveAside(t)));
            }
            else items.Add(("用蒸馏水湿润", () => GasTest.Moisten(t)));
            items.Add(("放回台面", () => { SplintSnap.MoveAside(t); t.LiftY = 0; t.Pos = new Vector3(t.Pos.x, 0, t.Pos.z); }));
            items.Add(("移走", () => { TestSet.Remove(t); SetFocus(null); }));
        }
        PetMenu.BuildMenu(screen, items);
    }



    void HandleInput() {
        if(SuppressDesktopInputForAudit)return;
        /* 计算器开着时实验台不接收输入（那边有自己的交互） */
        if (CalculatorActive) return;
        /* 控制台持有输入时不串扰视角/拖动/选单。LabInput 已整体屏蔽，
           这里是第二道保险，也省掉每帧的拾取射线。 */
        if (DeveloperConsole.InputCaptured) {
            // Reconcile an existing gesture using raw held state, never console clicks.
            // The last laboratory intent remains the destination (not the console pointer).
            bool held = WindowTargetInput.Active ? WindowTargetInput.Button(0) : Input.GetMouseButton(0);
            if (!held && (_dragging != null || _dragApp != null || _dragTest != null || _orbiting))
                FinishDrag(_lastMouse, true);
            return;
        }
        if(_ui!=null && (_ui.IsDosing||_ui.BurnerDialogOpen||_ui.ThermalLedgerOpen))return;

        var selectedUI = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        bool typing = selectedUI != null && selectedUI.GetComponent<UnityEngine.UI.InputField>() != null
            && selectedUI.GetComponent<UnityEngine.UI.InputField>().isFocused;
        if (!typing) {
            Vector3 move = new Vector3((LabInput.GetKey(KeyCode.D) ? 1 : 0) - (LabInput.GetKey(KeyCode.A) ? 1 : 0),
                0, (LabInput.GetKey(KeyCode.W) ? 1 : 0) - (LabInput.GetKey(KeyCode.S) ? 1 : 0));
            MoveView(move.x, move.z, Time.deltaTime);
            if (LabInput.GetKeyDown(KeyCode.E) && _focused is TestItem selectedTest) GasTest.Extinguish(selectedTest);
            if (LabInput.GetKeyDown(KeyCode.F) && _focused is Apparatus burner && burner.Kind == ApparatusKind.BunsenBurner)
                burner.Lit = !burner.Lit && burner.CanLight;
        }

        var m = LabInput.mousePosition;
        /* 鼠标在界面面板上时不参与 3D 拖动 */
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (LabInput.GetMouseButtonDown(1)) {
            var rightHit=LabPick.Pick(m, Cam);
            WindowTargetInput.TraceRightClick(m, overUI, rightHit);
            if (!overUI) {
                ShowObjectMenu(rightHit, m);
                return;
            }
        }
        if (LabInput.GetMouseButtonDown(0)) {
            /* david 的最后一段直线还没跑完时，不接受新的按下，免得刚放下的物件被
               半途改写。 */
            if (_david != null && _david.Finishing) return;
            _clickStart = m; _clickTime = Time.unscaledTimeAsDouble;
            if (overUI) { _orbiting = false; return; }
            _ui?.CloseCompactDrawers();
            var hit = LabPick.Pick(m, Cam);
            var v = hit.vessel;
            _dragApp = hit.apparatus; _dragTest = hit.testItem;
            SetFocus((object)v ?? (object)_dragApp ?? _dragTest);
            _lastMouse = m;
            if (_dragApp != null || _dragTest != null) {
                var pos = _dragApp != null ? _dragApp.Pos : _dragTest.Pos;
                _grabOffset = PickPlane(m) - pos;
                _orbiting = false;
            }
            if (v != null) {
                _dragging = v;
                _dragStartPlacement=new SnapController.Placement(v);
                _dragPlaneY=MeteredTransfer.IsTool(v)?hit.world.y:0f;
                var p = PickPlane(m,_dragPlaneY);
                _grabOffset = p - v.Pos;
                /* 拿起来就解除原有吸附，否则会一直挂在别人上面 */
                if (_ui != null) _ui.SelectExternal(v);
            } else if (_dragApp == null && _dragTest == null) {
                _orbiting = true; _lastMouse = m;
            }
        }
        if (LabInput.GetMouseButtonUp(0) || (!LabInput.GetMouseButton(0)
            && (_dragging != null || _dragApp != null || _dragTest != null || _orbiting))) {
            /* david：松手后先执行最后一段直线运动到正常拖动应到的落点，
               落点到了再走原来的吸附/放下，所以放下逻辑整段延后。 */
            FinishDrag(m, overUI);
        }

        if ((_dragApp != null || _dragTest != null) && LabInput.GetMouseButton(0)) {
            bool davidDrag = DavidMode.Enabled && _david != null;
            /* david 期间突进位置归驱动所有，Shift 微调高度先让位。 */
            if (!davidDrag && (LabInput.GetKey(KeyCode.LeftShift) || LabInput.GetKey(KeyCode.RightShift))) {
                float dy = (m.y - _lastMouse.y) * 0.005f;
                if (_dragTest != null) _dragTest.LiftY = Mathf.Max(0, _dragTest.LiftY + dy);
                else _dragApp.Pos += Vector3.up * Mathf.Max(-_dragApp.Pos.y, dy);
                _grabOffset = PickPlane(m) - (_dragApp != null ? _dragApp.Pos : _dragTest.Pos);
            } else {
                var want = PickPlane(m) - _grabOffset;
                if (_dragApp != null) {
                    /* 器材把抬升记在 Pos.y 上，所以意图落点的 y 单独带过去。 */
                    float intentLift = _dragApp.Pos.y;
                    var intent = new Vector3(want.x, 0f, want.z);
                    if (davidDrag) {
                        if (!_david.Active) BeginDavidApparatusDrag(intent, intentLift);
                        _david.SetIntent(intent, intentLift);
                    } else _dragApp.Pos = new Vector3(want.x, intentLift, want.z);
                } else {
                    float intentLift = _dragTest.LiftY;
                    var intent = new Vector3(want.x, 0f, want.z);
                    if (davidDrag) {
                        if (!_david.Active) BeginDavidTestItemDrag(intent, intentLift);
                        _david.SetIntent(intent, intentLift);
                    } else _dragTest.Pos = new Vector3(want.x, intentLift, want.z);
                }
            }
            if (_dragTest != null && _dragTest.Kind == TestItemKind.Splint &&
                Vector2.Distance(m, _clickStart) >= 6f) {
                _dragTest.AttachedBurner = null;
                _snapVessel = null;
                /* 吸附判定看鼠标意图落点，而不是突进中的视觉位置。 */
                _snapApp = SplintSnap.NearestBurner(DragIntentPosition(_dragTest.Pos));
                UpdateHighlights();
            }
            _lastMouse = m;
        } else if (_dragging != null && LabInput.GetMouseButton(0) && Vector2.Distance(m, _clickStart) >= 6f) {
            if(_dragging.ReceivingDistillation!=null){
                _dragging.ReceivingDistillation.Detach();
                Lab.Note(_dragging,"接收瓶已移离出口，蒸馏连接已拆除并停止","setup");
            }
            float lift = _dragging.LiftY;
            SnapController.Detach(_dragging);
            _dragging.LiftY = lift;
            bool davidDrag = DavidMode.Enabled && _david != null;
            if (!davidDrag && (LabInput.GetKey(KeyCode.LeftShift) || LabInput.GetKey(KeyCode.RightShift))) {
                _dragging.LiftY = Mathf.Max(0, lift + (m.y - _lastMouse.y) * 0.005f);
                _lastMouse = m;
                _grabOffset = PickPlane(m,_dragPlaneY) - _dragging.Pos;
                ClearHighlights();
                return;
            }
            _lastMouse = m;
            var p = PickPlane(m,_dragPlaneY);
            p.y = 0;
            var np = p - _grabOffset;
            var want = new Vector3(np.x, 0f, np.z);   /* 不设边界：台面之外也让拖，用户自己看着办 */

            /* 找吸附目标：优先别的容器，其次本生灯，再次三脚架 */
            _snapVessel = SnapController.NearestVessel(want, _dragging);
            _snapApp = null;
            if (_snapVessel == null) {
                _snapApp = SnapController.NearestApparatus(want, ApparatusKind.BunsenBurner)
                        ?? SnapController.NearestApparatus(want, ApparatusKind.Tripod);
            }
            UpdateHighlights();

            float wantLift;
            if (_snapVessel != null) {
                /* 吸附：水平对齐到目标正上方，并按目标高度抬起 */
                want = new Vector3(_snapVessel.Pos.x, 0f, _snapVessel.Pos.z);
                wantLift = SnapController.LiftOnto(_snapVessel, _dragging);
            } else if (_snapApp != null) {
                want = new Vector3(_snapApp.Pos.x, 0f, _snapApp.Pos.z);
                wantLift = SnapController.LiftOntoApparatus(_snapApp);
            } else {
                wantLift = 0f;
            }
            /* 普通模式直接跟随；david 模式把意图落点交给驱动，模型位置由
               突进位置决定，两者分开保存。 */
            if (davidDrag) {
                if (!_david.Active) BeginDavidVesselDrag(want, wantLift);
                _david.SetIntent(want, wantLift);
            } else {
                _dragging.Pos = want;
                _dragging.LiftY = wantLift;
            }
        } else if (_orbiting && LabInput.GetMouseButton(0)) {
            // 转动时保持相机位置，改变朝向，不绕任何物品公转。
            var d = m - _lastMouse; _lastMouse = m;
            RotateView(d);
        }
        if (!overUI) _focus += Cam.transform.forward * LabInput.GetAxis("Mouse ScrollWheel") * 2f;
    }

    /// <summary>拖动意图落点；david 突进期间视觉位置与它不同，判定都用这个。</summary>
    Vector3 DragIntentPosition(Vector3 visualFallback) =>
        _david != null && _david.Active ? _david.IntentForTest : visualFallback;

    /// <summary>
    /// 松开鼠标后的正常收尾：木条吸附、容器吸附/倾倒/放下、点击聚焦判定。
    /// david 模式下它作为最后一段直线的回调执行 —— 此时物件已经回到意图落点，
    /// 所以吸附/放下用的就是原来那套逻辑。
    /// </summary>
    void FinishDrag(Vector2 m, bool overUI) {
        if (_david != null && _david.Finishing) return;
        if (DavidMode.Enabled && _david != null && _david.Active
            && (_dragging != null || _dragApp != null || _dragTest != null))
            _david.BeginFinish(() => ResolveDragRelease(m, overUI));
        else ResolveDragRelease(m, overUI);
    }

    void ResolveDragRelease(Vector2 m, bool overUI) {
        if (_dragTest != null && _dragTest.Kind == TestItemKind.Splint &&
            Vector2.Distance(m, _clickStart) >= 6f) {
            var burner = SplintSnap.NearestBurner(_dragTest.Pos);
            if (burner != null && SplintSnap.Attach(_dragTest, burner))
                Lab.Note(null, "木条已吸附到本生灯火焰旁", "setup");
        }
        if (_dragging != null) {
            if(_snapVessel!=null&&(_dragging.Kind==VesselKind.Burette||_dragging.Kind==VesselKind.Pipette))CommitSnap(_dragging);
            else if(_snapVessel!=null&&_ui!=null){_dragStartPlacement?.Restore();_ui.BeginStagedPour(_dragging,_snapVessel);}
            else CommitSnap(_dragging);
        }
        _dragging = null; _dragApp = null; _dragTest = null; _orbiting = false;
        _dragPlaneY=0f;
        _dragStartPlacement=null;
        ClearHighlights();
        /* 几乎没动、按得也短 → 算一次点击：点中东西就聚焦它，点空白就取消聚焦 */
        if (!overUI && Vector2.Distance(m, _clickStart) < 6f
            && Time.unscaledTimeAsDouble - _clickTime < 0.45) {
            var hit = LabPick.Pick(m, Cam);
            SetFocus((object)hit.vessel ?? (object)hit.apparatus ?? hit.testItem);
        }
    }

    /* ================================================================
       david：三个入口把突进位置写回模型
         覆盖普通器皿与量器、挂在同一视图下的蒸馏/回流组件，
         以及器材（本生灯/三脚架）和检验道具；内容物随容器一起显示。
       ================================================================ */
    void BeginDavidVesselDrag(Vector3 intent, float intentLift) {
        if (_david == null || _dragging == null) return;
        var vessel = _dragging;
        var view = _views.Find(x => x != null && x.Model == vessel);
        if (view == null) return;
        _david.Begin(view.transform,
            (position, lift) => {
                if (vessel == null) return;
                vessel.Pos = new Vector3(position.x, 0f, position.z);
                vessel.LiftY = Mathf.Max(0f, lift);
            },
            () => Lab.Vessels.Contains(vessel),
            new Vector3(vessel.Pos.x, 0f, vessel.Pos.z), vessel.LiftY, intent, intentLift);
    }

    void BeginDavidApparatusDrag(Vector3 intent, float intentLift) {
        if (_david == null || _dragApp == null) return;
        var apparatus = _dragApp;
        var view = _apViews.Find(x => x != null && x.Model == apparatus);
        if (view == null) return;
        _david.Begin(view.transform,
            (position, lift) => {
                if (apparatus == null) return;
                apparatus.Pos = new Vector3(position.x, Mathf.Max(0f, lift), position.z);
            },
            () => ApparatusSet.All.Contains(apparatus),
            new Vector3(apparatus.Pos.x, 0f, apparatus.Pos.z), apparatus.Pos.y, intent, intentLift);
    }

    void BeginDavidTestItemDrag(Vector3 intent, float intentLift) {
        if (_david == null || _dragTest == null) return;
        var item = _dragTest;
        var view = _testViews.Find(x => x != null && x.Model == item);
        if (view == null) return;
        _david.Begin(view.transform,
            (position, lift) => {
                if (item == null) return;
                item.Pos = new Vector3(position.x, 0f, position.z);
                item.LiftY = Mathf.Max(0f, lift);
            },
            () => TestSet.All.Contains(item),
            new Vector3(item.Pos.x, 0f, item.Pos.z), item.LiftY, intent, intentLift);
    }

    /* ---------------- 审计/诊断只读入口 ---------------- */
    public Vector3 TestFocusPoint => _focus;
    public string TestDragTargetId => _dragging != null ? _dragging.Id : _dragApp != null ? _dragApp.Id : _dragTest != null ? _dragTest.Id : "-";

    /// <summary>屏幕点选器皿：按「屏幕距离 vs 随距离缩放的半径」判定，取最近的</summary>
    Vessel PickVessel(Vector3 screen) {
        Vessel best = null; float bestT = float.MaxValue;
        foreach (var v in Lab.Vessels) {
            var g = VesselMesh.Get(v.Kind);
            var center = v.Pos + Vector3.up * g.H * 0.45f;
            var sc = Cam.WorldToScreenPoint(center);
            if (sc.z <= 0) continue;
            float r = g.R * 2.2f / Mathf.Max(0.2f, sc.z) * (Screen.height * 0.5f / 0.53f);
            float d = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(sc.x, sc.y));
            if (d < Mathf.Max(r, Screen.height * 0.08f) && sc.z < bestT) { bestT = sc.z; best = v; }
        }
        return best;
    }

    /// <summary>屏幕射线打到台面（y=0）上的世界坐标</summary>
    bool PickAnyApparatus(Vector3 screen, out Apparatus found) {
        found = null; float best = float.MaxValue;
        foreach (var a in ApparatusSet.All) {
            var sc = Cam.WorldToScreenPoint(a.Pos + Vector3.up * 0.14f);
            if (sc.z <= 0) continue;
            float d = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(sc.x, sc.y));
            if (d < Screen.height * 0.09f && d < best) { best = d; found = a; }
        }
        return found != null;
    }

    bool PickAnyTestItem(Vector3 screen, out TestItem found) {
        found = null; float best = float.MaxValue;
        foreach (var t in TestSet.All) {
            var sc = Cam.WorldToScreenPoint(t.Pos + Vector3.up * (t.LiftY + 0.08f));
            if (sc.z <= 0) continue;
            float d = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(sc.x, sc.y));
            if (d < Screen.height * 0.07f && d < best) { best = d; found = t; }
        }
        return found != null;
    }

    Vector3 PickPlane(Vector3 screen,float height=0f) {
        var ray = Cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, Vector3.up*height);
        return plane.Raycast(ray, out float t) ? ray.GetPoint(t) : Vector3.zero;
    }


    /* ================================================================
       无头渲染一张图（命令行验证用）
       ================================================================ */
    public string RenderToFile(int w, int h, string path) {
        SyncViews();
        SyncViews();
        /* 粒子的时间必须显式喂进去：无头环境没有 Time.deltaTime 的推进。
           注意必须在 SyncViews() 之后 —— 视图是那一步才建出来的 */
        int fxVerts = 0;
        foreach (var vv in _views) {
            vv.FxCam = Cam;
            vv.FxTime = 1.35f;
            vv.SyncFx();
            foreach (var mf in vv.GetComponentsInChildren<MeshFilter>()) {
                if (mf.sharedMesh == null) continue;
                var nm = mf.gameObject.name;
                if (nm == "Glass" || nm == "Liquid" || nm == "Sediment") continue;
                fxVerts += mf.sharedMesh.vertexCount;
            }
        }
        Debug.Log($"[ChemLab FX] 粒子顶点数={fxVerts}");
        /* 3D 与 UI 分别渲染到两张 RT，再在 C# 里做 alpha 合成。
           不依赖相机叠放 —— 无头环境下那样层序不可控。 */
        var prevAspect = Cam.aspect;
        var rtMain = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        Cam.aspect = (float)w / h;
        UpdateCamera(0f);
        Cam.targetTexture = rtMain;
        Cam.Render();
        Cam.targetTexture = null;

        var t3 = new Texture2D(w, h, TextureFormat.RGB24, false);
        RenderTexture.active = rtMain;
        t3.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        t3.Apply();
        RenderTexture.active = null;
        var px = t3.GetPixels32();

        /* 计算器会把实验台 UI 隐藏；离屏验收应捕获当前真正显示的画布。 */
        Canvas cv = EnthalpyScene.Instance != null && EnthalpyScene.Instance.gameObject.activeInHierarchy
            ? EnthalpyScene.Instance.GetComponent<Canvas>()
            : _ui != null ? _ui.GetComponentInChildren<Canvas>() : null;
        if (UICam != null && cv != null) {
            var rtUi = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var pc = UICam.clearFlags; var pbg = UICam.backgroundColor; var pa = UICam.aspect;
            UICam.aspect = (float)w / h;
            UICam.clearFlags = CameraClearFlags.SolidColor;
            UICam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            UICam.enabled = true;
            UICam.targetTexture = rtUi;

            /* Overlay 由画布系统直接上屏，RenderTexture 截图时暂用 ScreenSpaceCamera。
               只有请求的图片尺寸与实际 Screen 不同时才锁定像素缩放；同尺寸
               截图必须保留真实 CanvasScaler，否则窄窗口画面会被错误放大。 */
            var scaler = cv.GetComponent<CanvasScaler>();
            var prevMode = cv.renderMode;
            var prevUiCam = cv.worldCamera;
            var prevPlane = cv.planeDistance;
            var prevScaleMode = scaler != null ? scaler.uiScaleMode : CanvasScaler.ScaleMode.ConstantPixelSize;
            var prevScaleFactor = scaler != null ? scaler.scaleFactor : 1f;

            cv.renderMode = RenderMode.ScreenSpaceCamera;
            cv.worldCamera = UICam;
            cv.planeDistance = 10f;
            bool resizeCapture=Screen.width!=w||Screen.height!=h;
            if (scaler != null && resizeCapture) {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
            }
            /* 独立计算器画布在 Default 层；截图相机只看 UiLayer，
               因而临时换层，避免把实验台 3D 物体二次画进 UI 通道。 */
            var captureChildren=cv.GetComponentsInChildren<Transform>(true);
            var oldLayers=new int[captureChildren.Length];
            for(int i=0;i<captureChildren.Length;i++){
                oldLayers[i]=captureChildren[i].gameObject.layer;
                captureChildren[i].gameObject.layer=UiLayer;
            }
            Canvas.ForceUpdateCanvases();
            UICam.Render();

            for(int i=0;i<captureChildren.Length;i++)
                if(captureChildren[i]!=null)captureChildren[i].gameObject.layer=oldLayers[i];

            UICam.targetTexture = null;
            UICam.enabled = false;
            cv.renderMode = prevMode;
            cv.worldCamera = prevUiCam;
            cv.planeDistance = prevPlane;
            if (scaler != null) { scaler.uiScaleMode = prevScaleMode; scaler.scaleFactor = prevScaleFactor; }
            UICam.clearFlags = pc; UICam.backgroundColor = pbg; UICam.aspect = pa;

            var tU = new Texture2D(w, h, TextureFormat.RGBA32, false);
            RenderTexture.active = rtUi;
            tU.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tU.Apply();
            RenderTexture.active = null;
            var pu = tU.GetPixels32();

            int nz = 0;
            for (int i = 0; i < px.Length; i++) {
                int a = pu[i].a;
                if (a == 0) continue;
                nz++;
                if (a == 255) { px[i] = new Color32(pu[i].r, pu[i].g, pu[i].b, 255); continue; }
                float k = a / 255f;
                px[i] = new Color32(
                    (byte)(pu[i].r * k + px[i].r * (1f - k) + 0.5f),
                    (byte)(pu[i].g * k + px[i].g * (1f - k) + 0.5f),
                    (byte)(pu[i].b * k + px[i].b * (1f - k) + 0.5f), 255);
            }
            Debug.Log($"[ChemLab] UI 像素 {nz}");
            UnityEngine.Object.DestroyImmediate(tU);
            rtUi.Release();
        }

        var outTex = new Texture2D(w, h, TextureFormat.RGB24, false);
        outTex.SetPixels32(px);
        outTex.Apply();

        Cam.aspect = prevAspect;
        UnityEngine.Object.DestroyImmediate(t3);
        rtMain.Release();

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, outTex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(outTex);
        Debug.Log($"[ChemLab] 已出图 {w}x{h} → {path}");
        return path;
    }
}
}
