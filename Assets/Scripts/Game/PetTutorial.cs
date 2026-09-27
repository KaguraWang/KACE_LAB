/* =====================================================================
   阿侬的教程系统
     两段：
       · IonTest   离子鉴定实验（约 30 步）—— 首次打开软件自动触发。
                    这是全部功能的总览：器材架、试剂架、加试剂、温度/pH、
                    加热、搅拌、倾倒、吸附、检验材料（石蕊/木条）、
                    容器标签（分子/离子、逐条开关）、方程式标签（中英/离子式）、
                    实验记录、删除器皿。
       · BornHaber 焓变计算器（约 15 步）—— 首次打开计算器时触发。

     AI 助手是这两段教程的「奖励」：只有走完或主动跳过之后才可用
     （见 PetProgress.AiUnlocked）。所以教程必须讲全，不能糊弄。
   ===================================================================== */
using System;
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {

public enum TutorialKind { IonTest, BornHaber }

public class TutorialStep {
    public string Say;
    /// <summary>跳转到哪一页：null 不动，true 计算器，false 实验台</summary>
    public bool? GoCalc;
    public Action Do;
    public Func<bool> WaitFor;
    public float Hold = 5f;
    /// <summary>标题，显示在教程进度条上</summary>
    public string Title = "";
}

public static class PetTutorial {
    public static bool Running { get; private set; }
    public static TutorialKind Current;
    public static int StepIndex => _i;
    public static int StepCount => _steps != null ? _steps.Count : 0;
    public static string StepTitle => (_steps != null && _i < _steps.Count) ? _steps[_i].Title : "";
    public static string EnglishStepTitle => Current == TutorialKind.IonTest
        ? EnglishTitle(LabTitlesEn, _i, StepTitle) : EnglishTitle(CalcTitlesEn, _i, StepTitle);
    /// <summary>当前这一步是在等用户动手吗（决定进度条上显示「等你操作」）</summary>
    public static bool WaitingOnUser => _waiting;

    static List<TutorialStep> _steps;
    static int _i;
    static double _t;
    static bool _waiting;
    static float _delay = -1f;
    static TutorialKind _delayKind;

    // These two arrays intentionally follow BuildLab and BuildCalc step order.
    // Keeping complete English paragraphs here preserves every original Chinese
    // tutorial source while making English mode a true line-by-line translation.
    static readonly string[] LabTitlesEn = {
        "Welcome", "Interface overview", "Get your first vessel", "Meet the glassware",
        "Find reagents", "Choose an amount", "Add the first reagent", "Add the second reagent",
        "Read live values", "Vessel labels", "Switch to ionic view", "Switch back to molecular view",
        "Toggle individual rows", "Equation labels", "Chinese and English conventions",
        "Reversibility and conditions", "Ionic equations", "Display duration", "Get ready to heat",
        "Move a vessel to the Bunsen burner", "Adjust the flame", "Tripod and crucible",
        "Pour between vessels", "Use the pour slider", "Meet the test tools", "Use litmus paper",
        "Use a splint", "Try another reaction", "Identify carbon dioxide", "Experiment log",
        "Clear the bench", "Summary", "Unlock the AI assistant", "One more feature"
    };
    static readonly string[] LabSayEn = {
        "Hi, I'm Anong.\nIs this your first time in the lab? I'll show you around in about five or six minutes.\nWhen we're done, I'll be your AI assistant.",
        "Let's take a look around.\nThe apparatus shelf is on the left, the bench is in the centre, the reagent shelf is on the right, and the experiment log is at the bottom.\nThe language switch and enthalpy calculator are along the top.",
        "Let's get started. The first item on the left apparatus shelf is a beaker. Click it to place one on the bench.",
        "Great. Try the other vessels too:\n· Test tubes — small-scale reactions and gas tests\n· Conical flasks — titrations\n· Round-bottom flasks — heating and reflux\n· Evaporating dishes — evaporation and crystallisation\n· Burettes and measuring cylinders — accurate measurement",
        "The reagent shelf on the right is grouped by category. Use the search box to find a name or formula.\nDrag a reagent onto a vessel and release it when the vessel outline is highlighted; you don't need to select the vessel first.",
        "Releasing the reagent opens an amount dialog. Enter a positive value, choose g or mol, then select OK or press Enter.\nCancel adds nothing. Reagents that only exist in solution need a suitable solvent first. If the amount exceeds the known solubility, the undissolved portion is retained and a notice is shown.",
        "I'll add the first reagent as a demonstration. The beaker already contains water; I'll add 0.001 mol of silver nitrate (AgNO₃) and let it dissolve.\nNo chloride has been added yet, so AgCl should not form. When you try it, use the drag-and-amount dialog we just covered.",
        "Now I'll add 0.001 mol of sodium chloride (NaCl).\nThe dissolved Cl⁻ reacts with Ag⁺ to form a white AgCl precipitate.",
        "The selected-vessel panel on the left shows the current state in real time:\n· Temperature — changes during exothermic or endothermic reactions\n· pH — the colour bar gives a visual indication of acidity or alkalinity\n· Liquid volume — the current volume of liquid",
        "The panel above a vessel is its vessel label. It shows species names, amount in mol, and concentration in mol dm⁻³.\nThe white line points to the vessel, so you can tell which data belongs to it.",
        "The label has a Molecular / Ionic switch. I'll change it to ionic view.\nYou'll see the amounts of Ag⁺, Na⁺, Cl⁻ and NO₃⁻ present in the solution.\nWhat is actually present in a reaction mixture is not always the same as the formula used to describe what was added.",
        "Switch back to molecular substances. The control is in the label bar and you can change it at any time.",
        "Each row on a label has a small checkbox. Use it to hide individual entries you don't need.\nThis is useful when a vessel contains many species.",
        "Whenever a reaction occurs, a reaction card appears beside the vessel with its balanced equation.",
        "There are two display conventions:\n· Chinese — inorganic reactions use =; gases are marked ↑ and precipitates ↓; state symbols are omitted\n· English (CIE) — use → and add (s), (l), (g) or (aq) after every species\nThe EN button along the top switches between them.",
        "Reversible reactions use a two-way arrow, ⇌.\nConditions such as heating, a catalyst or light appear above the arrow. The arrow expands to fit the condition text.",
        "Some reactions can also be shown as ionic equations. Common spectator ions on both sides are cancelled, leaving only the species that take part.\nYou can enable this in the label bar.",
        "Reaction cards disappear after 10 seconds by default.\nChange the duration in the number field on the label bar if you want them to stay longer.",
        "Next, let's learn about heating. I'll place another test tube containing some copper(II) sulfate solution.",
        "Drag a vessel near the Bunsen burner. The burner will highlight, and releasing the vessel attaches it above the flame.\nThat makes it clear which vessel is being heated.",
        "Adjust the gas flow on the Bunsen burner to control the flame.\nA blue flame indicates more complete combustion and a higher temperature; a yellow flame indicates incomplete combustion and a lower temperature.",
        "For strong heating, use a tripod and crucible. Place the crucible on the tripod and light the Bunsen burner below it.\nReactions such as the thermal decomposition of calcium carbonate are carried out this way.",
        "Here's another common operation. Drag one vessel near another and it will attach above it.\nRelease it to leave it in place.",
        "Now drag the slider beside the vessels to pour material from the upper vessel into the lower one.\nThe slider controls how much is transferred. This is useful for titrations and reactions with excess reagent.",
        "The test tools are at the lower left. They include the indicators most often used in exams:\n· Red and blue litmus paper (you can moisten it)\n· A splint in three states: unlit, lit, or recently extinguished",
        "Damp red litmus paper turns blue in an alkaline gas such as NH₃.\nTo test for ammonium ions, add NaOH and heat. Hold damp red litmus paper at the mouth of the tube; a blue colour indicates NH₄⁺.",
        "A splint is used to test gases:\n· Insert it into oxygen → it relights and burns more brightly\n· Bring it near hydrogen → a sharp squeaky pop is heard\nThese are the classic tests for distinguishing oxygen from hydrogen.",
        "For our final reaction, let's make a gas. I'll add sodium carbonate to a fresh beaker and then add hydrochloric acid.",
        "Bubbles are appearing; the gas is CO₂. To confirm it is carbon dioxide, pass it through limewater.\nThe limewater turns cloudy. This reaction also produces an equation card.",
        "The bar at the bottom is the experiment log. It records every operation and observation automatically.\nIf you lose track during a longer experiment, check the log to see what happened.",
        "Right-click a vessel you no longer need to remove it. You can clear the bench whenever you like.",
        "That's the end of the lab tour. You've learned how to:\nplace vessels · add reagents · heat · pour · test gases · read labels · view equations · check the log.",
        "I'm your AI assistant from now on. Right-click me and choose Open chat.\nYou can ask me to set up a silver-mirror reaction or teach you a titration, and I'll build it for you.",
        "There's one more feature to learn: the enthalpy calculator along the top. Open it for the first time and I'll guide you through a problem.\nHave fun in the lab!"
    };
    static readonly string[] CalcTitlesEn = {
        "Enthalpy calculator", "What is a node?", "Automatic formula matching", "Draw an edge",
        "Automatic routing", "Choose an enthalpy source", "Set up an example", "Read the diagram",
        "The enthalpy of formation edge", "Solve", "Read the result", "If the cycle is incomplete",
        "Edit with the right mouse button", "What else can it calculate?", "Finish"
    };
    static readonly string[] CalcSayEn = {
        "This is the enthalpy calculator. It has one job: use Hess's law to find an unknown enthalpy change by following a closed energy cycle.",
        "Each box is a node representing a chemical species or a state.\nEnter the coefficient in the small box on the left and the formula in the box on the right.",
        "Enter a formula and press Enter. I'll match it against the species library and format subscripts and superscripts: co2 becomes CO₂ and fe3+ becomes Fe³⁺.\nIf there's no match, the field turns red so you can check the spelling.",
        "Hold the left mouse button on a node and drag outward to draw a white curved arrow.\nThe arrow follows the pointer; release it over another node to connect them.",
        "The curve is calculated automatically to avoid nodes and other edges.\nWhen several edges share a straight line, they are offset so they remain visible.",
        "After you connect an edge, choose where its enthalpy change comes from:\n· Lookup — use a value from the library\n· Enter manually — type a value in the dialog\n· Unknown — leave it to be solved (shown as a yellow ΔH = ?)\nRight-click to cancel a mistaken choice; the edge will be removed too.",
        "Let's set up a real example: find the lattice enthalpy of potassium chloride, KCl.",
        "This is a Born–Haber cycle:\n· K(s) → K⁺(g): atomisation of potassium +89, then first ionisation energy +418\n· ½Cl₂(g) → Cl⁻(g): atomisation of chlorine +121, then electron affinity −349\n· K⁺(g) + Cl⁻(g) → KCl(s): the unknown lattice enthalpy",
        "There is also an edge directly from K(s) to KCl(s), with the standard enthalpy of formation, −437 kJ mol⁻¹.\nIt closes the cycle. Hess's law works because the total enthalpy change is the same along every route.",
        "Select Solve for unknown enthalpy change in the upper-right corner.\nThe calculator sums the known enthalpy changes around the cycle, sets the total to zero and works backwards to find the unknown.",
        "The lattice enthalpy is −716 kJ mol⁻¹. The negative sign means the process is exothermic: ions release energy when they combine to form the crystal.\nThis agrees with the standard value in textbooks.",
        "If the cycle cannot be closed, the calculator tells you where it is disconnected.\nIf there is more than one unknown, it reports insufficient information instead of guessing an answer.",
        "Right-click an edge to change its enthalpy value or delete it. Right-click a node to edit its formula, duplicate it or delete it.\nYou can correct the diagram at any time.",
        "The same method can be used for:\n· enthalpies of formation, combustion and neutralisation\n· bond enthalpies, atomisation and electron affinities\n· hydration, solution and lattice enthalpies\nAny quantity that forms an energy cycle can be solved this way.",
        "That's the end of the calculator tutorial. Your AI assistant is ready whenever you need it. Right-click me and choose Open chat.\nAsk me to calculate the lattice enthalpy of magnesium sulfate and I'll build the cycle for you."
    };
    static string EnglishTitle(string[] titles, int index, string source) {
        if (index < 0 || index >= titles.Length) return source;
        if (index == 0) return titles[index];
        int dot = source != null ? source.IndexOf(". ", StringComparison.Ordinal) : -1;
        return dot >= 0 ? source.Substring(0, dot + 2) + titles[index] : titles[index];
    }

    public static void StartDelayed(TutorialKind kind, float seconds) {
        _delay = seconds; _delayKind = kind;
    }
    public static void TickDelay(float dt) {
        if (_delay < 0f) return;
        _delay -= dt;
        if (_delay <= 0f) { _delay = -1f; Start(_delayKind); }
    }

    public static void Start(TutorialKind kind) {
        Current = kind;
        _steps = kind == TutorialKind.IonTest ? BuildLab() : BuildCalc();
        var titles = kind == TutorialKind.IonTest ? LabTitlesEn : CalcTitlesEn;
        var says = kind == TutorialKind.IonTest ? LabSayEn : CalcSayEn;
        if (titles.Length != _steps.Count || says.Length != _steps.Count)
            Debug.LogError($"[Localization] Tutorial translation count mismatch: {kind}, steps={_steps.Count}, titles={titles.Length}, say={says.Length}");
        for (int i = 0; i < _steps.Count && i < titles.Length && i < says.Length; i++) {
            Localization.RegisterTranslation(_steps[i].Title, titles[i]);
            Localization.RegisterTranslation(_steps[i].Say, says[i]);
        }
        Localization.RegisterTranslation("慢慢来，等你做完这一步～", "Take your time. I'll wait while you finish this step.");
        Localization.RegisterTranslation("那我先安静待着。想继续的话右键我，选「继续教程」。", "I'll stay quiet for now. To continue, right-click me and choose “Continue tutorial”.");
        Localization.RegisterTranslation("好，跳过也行。AI 助手已经给你解锁了 —— 右键我点「打开聊天」，随时找我。\n（想重新学的话，右键我还能再开一遍。）",
            "That's fine; you can skip it. The AI assistant is now unlocked. Right-click me and choose Open chat whenever you need me.\n(Right-click me and choose the tutorial again if you'd like to go through it later.)");
        Localization.RegisterTranslation("实验部分教完啦！从现在起我就是你的助手了 —— 右键我点「打开聊天」，想做什么直接说。\n顶栏那个『焓变计算器』你什么时候点开，我再教你那个。",
            "That's the end of the lab tutorial! I'm your assistant now. Right-click me and choose Open chat, then tell me what you'd like to do.\nOpen the enthalpy calculator along the top whenever you're ready, and I'll show you how it works.");
        Localization.RegisterTranslation("计算器也学会了！以后搭任何能量循环都能这样解。", "You know how to use the calculator now! You can solve other energy cycles in the same way.");
        _i = 0; _t = 0; _waiting = false;
        Running = true;
        PetView.Instance?.Unhide();
        TutorialBar.Show();
        Enter();
        if (TutorialBar.Instance != null) Debug.Log(TutorialBar.Instance.Audit());
    }

    /// <summary>结束教程。skipped=true 表示用户主动跳过 —— 同样解锁 AI。</summary>
    public static void Stop(bool skipped) {
        var wasKind = Current;
        Running = false;
        _steps = null;
        TutorialBar.Instance?.Hide();
        if (wasKind == TutorialKind.IonTest) {
            PetProgress.FinishLab(skipped);
            PetView.Instance?.Say(skipped
                ? "好，跳过也行。AI 助手已经给你解锁了 —— 右键我点「打开聊天」，随时找我。\n（想重新学的话，右键我还能再开一遍。）"
                : "实验部分教完啦！从现在起我就是你的助手了 —— 右键我点「打开聊天」，想做什么直接说。\n顶栏那个『焓变计算器』你什么时候点开，我再教你那个。", 12f);
        } else {
            PetProgress.FinishCalc();
            PetView.Instance?.Say("计算器也学会了！以后搭任何能量循环都能这样解。", 9f);
        }
    }

    public static void Tick() {
        // The disposable bilingual acceptance player intentionally advances each
        // tutorial step with Next so every localized screen is sampled exactly once.
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-bilingualUiTest") >= 0) return;
        TickDelay(Time.unscaledDeltaTime);
        if (!Running || _steps == null) return;
        var s = _steps[_i];
        _t += Time.unscaledDeltaTime;

        if (_waiting) {
            if (s.WaitFor != null && s.WaitFor()) { _i++; Enter(); return; }
            if (_t > 30) { _t = 0; var repeat = "慢慢来，等你做完这一步～\n" + s.Say;
                Localization.RegisterTranslation(repeat, "Take your time. I'll wait while you finish this step.\n" + (_i < LabSayEn.Length && Current == TutorialKind.IonTest ? LabSayEn[_i] : _i < CalcSayEn.Length ? CalcSayEn[_i] : s.Say));
                PetView.Instance?.Say(repeat, 12f); }
            return;
        }
        if (_t >= s.Hold) { _i++; Enter(); }
    }

    public static void Next() {
        if (!Running || _steps == null) return;
        _i++; Enter();
    }
    public static void Skip() { if (Running) Stop(true); }
    public static void Abort() {
        Running = false; _steps = null;
        TutorialBar.Instance?.Hide();
        PetView.Instance?.Say("那我先安静待着。想继续的话右键我，选「继续教程」。", 7f);
    }

    static void Enter() {
        if (_steps == null) return;
        if (_i >= _steps.Count) { Stop(false); return; }
        var s = _steps[_i];
        _t = 0; _waiting = s.WaitFor != null;
        if (s.GoCalc.HasValue) SwitchScene(s.GoCalc.Value);
        TutorialBar.Instance?.Refresh();
        PetView.Instance?.Say(s.Say, s.WaitFor != null ? 9999f : s.Hold + 1.5f);
        try { s.Do?.Invoke(); } catch (Exception e) { Debug.LogWarning("[Tutorial] 动作失败: " + e.Message); }
    }

    static void SwitchScene(bool toCalc) {
        var scene = EnthalpyScene.Instance;
        if (toCalc) {
            if (scene == null) EnthalpyScene.Show(LabBootstrap.Instance.gameObject, LabUI.UiFont);
            else scene.gameObject.SetActive(true);
        } else {
            if (scene != null) scene.gameObject.SetActive(false);
        }
    }

    /* ---------------- 工具 ---------------- */
    static Vessel First() => Lab.Vessels.Count > 0 ? Lab.Vessels[0] : null;
    // Automatic demonstrations use the same validation and mol units as the drag dialog.
    static void DemoDose(Vessel vessel, string id, double mol) {
        if (vessel == null) throw new InvalidOperationException("示范器皿不存在，请先放置器皿");
        if (!DoseService.TryAdd(vessel, id, mol, true, out string message))
            throw new InvalidOperationException(message);
    }
    static bool AnyHas(string id) {
        foreach (var v in Lab.Vessels)
            if (v.Get(id) > 1e-6 || v.HasIon(id) || v.HasPrecip(id)) return true;
        return false;
    }
    static bool AnyGas() {
        foreach (var v in Lab.Vessels)
            foreach (var kv in v.Gases) if (kv.Value > 1e-6) return true;
        return false;
    }
    static bool VesselCount(int k) => Lab.Vessels.Count >= k;

    /* ================================================================
       第一段：离子鉴定实验（详细版）
       ================================================================ */
    static List<TutorialStep> BuildLab() {
        var L = new List<TutorialStep>();
        int n = 0;
        TutorialStep S(string title, string say, Action d = null, Func<bool> w = null, float hold = 6f) {
            n++;
            return new TutorialStep { Title = n + ". " + title, Say = say, Do = d, WaitFor = w, Hold = hold };
        }

        /* --- 开场 --- */
        L.Add(new TutorialStep {
            Title = "欢迎", Hold = 8f,
            Say = "你好呀，我是阿侬。\n第一次用这个实验室吧？我带你完整走一遍，大概五六分钟。\n走完之后我就是你的 AI 助手了。",
            Do = () => { SwitchScene(false); Lab.Reset(); },
        });
        L.Add(S("界面总览", "先看一圈界面：\n左边是「器材架」，中间是实验台，右边是「试剂架」，底下是「实验记录」。\n顶上还有中英文切换和焓变计算器。", hold: 11f));

        /* --- 器皿 --- */
        L.Add(S("拿第一件器皿", "现在动手。左边器材架第一行是「烧杯」，点它就会往台面上放一个。",
            () => { if (Lab.Vessels.Count == 0) { var v = Lab.AddVessel(VesselKind.Beaker); v.AddWater(80); } },
            () => VesselCount(1), 8f));
        L.Add(S("认识各种器皿", "放好了。你还可以自己试试其它器皿：\n· 试管 —— 小量反应、气体检验\n· 锥形瓶 —— 滴定\n· 圆底烧瓶 —— 加热回流\n· 蒸发皿 —— 蒸发结晶\n· 滴定管、量筒 —— 精确量取",
            hold: 12f));

        /* --- 试剂 --- */
        L.Add(S("认识试剂架", "右边试剂架按类别分组，搜索栏可以查名称或化学式。\n按住试剂拖到目标器皿，轮廓高亮后松开；不需要先选中器皿。", hold: 12f));
        L.Add(S("设置加入量", "松开后会弹出数量框：输入正数，选择 g 或 mol，再点击确定或按 Enter。\n取消不会加入。只能以溶液存在的条目需要先放入合适溶剂；超过已知溶解度时会提示并保留未溶部分。", hold: 15f));
        L.Add(S("加第一种试剂", "下面是自动示范：烧杯里已有水，我加入 0.001 mol 硝酸银（AgNO₃），使它溶解。\n此时还没有加入氯化物，不应出现 AgCl 沉淀。自己操作时，请使用刚才的拖动和数量弹窗。",
            () => DemoDose(First(), "agno3", 0.001), hold: 10f));
        L.Add(S("加第二种试剂", "现在示范加入 0.001 mol 氯化钠（NaCl）。\n溶解后的 Cl⁻ 与 Ag⁺ 反应，生成白色 AgCl 沉淀。",
            () => DemoDose(First(), "nacl", 0.001), hold: 10f));

        /* --- 读数与标签 --- */
        L.Add(S("看实时数值", "左边「选中容器」那一块显示当前容器的实时状态：\n· 温度 —— 放热/吸热反应会变化\n· pH —— 用颜色条直观表示酸碱\n· 液量 —— 当前液体体积",
            () => { TagSettings.ShowTags = true; }, hold: 12f));
        L.Add(S("容器标签", "容器上方的牌子是「容器标签」：\n物质名、物质的量（mol）、浓度（mol/dm³）都写在上面。\n白线指着容器，一看就知道是谁的数据。", hold: 11f));
        L.Add(S("切到离子显示", "标签上有「具体物质 / 离子」开关。我帮你切到离子态 ——\n你会看到溶液里其实是 Ag⁺、Na⁺、Cl⁻、NO₃⁻ 各自有多少。\n化学里「实际存在什么」和「配方上写了什么」是两回事。",
            () => { TagSettings.Molecular = false; }, hold: 13f));
        L.Add(S("切回分子显示", "切回具体物质。这个开关在标签栏上，随时能切。",
            () => { TagSettings.Molecular = true; }, hold: 8f));
        L.Add(S("逐条开关", "标签上每一条前面都有小勾选框，可以单独关掉不想看的条目。\n物质多了以后很有用。", hold: 10f));

        /* --- 方程式标签 --- */
        L.Add(S("方程式标签", "每次发生反应，容器旁边会弹出一张「方程式卡片」，写着配平好的方程式。",
            () => { EqTagSettings.Show = true; }, hold: 10f));
        L.Add(S("中英两套规范", "写法分两套：\n· 中文 —— 无机反应用等号 = ，气体标 ↑ ，沉淀标 ↓ ，不标状态\n· 英文（CIE）—— 一律用 → ，每种物质后面标 (s)/(l)/(g)/(aq)\n顶栏那个 EN 按钮就是切换。", hold: 14f));
        L.Add(S("可逆与条件", "可逆反应的连接符会变成双向箭头 ⇌ 。\n条件（加热、催化剂、光照）写在连接符正上方 —— 而且连接符会按条件的长度自己画宽，保证写得下。", hold: 13f));
        L.Add(S("离子方程式", "有些反应还能写成「离子方程式」—— 把两边都有的旁观离子消掉，只留真正参与的。\n标签栏上有这个勾选项。", hold: 12f));
        L.Add(S("显示时长", "卡片默认显示 10 秒后自动消失。\n这个时长能在标签栏的数字框里改，嫌快就调长。", hold: 11f));

        /* --- 加热 --- */
        L.Add(S("准备加热", "接下来学加热。我再放一个试管，里面装点硫酸铜溶液。",
            () => { var v = Lab.AddVessel(VesselKind.Tube); v.AddWater(20); DemoDose(v, "cuso4", 0.001); },
            () => VesselCount(2), 8f));
        L.Add(S("拖到本生灯上", "把器皿拖到本生灯附近，本生灯会高亮；松手就自动吸附到火焰上方。\n这样就知道「谁在火上烤」。", hold: 12f));
        L.Add(S("调火力", "本生灯能调出气量来控制火力：\n蓝色火焰是充分燃烧、温度高；黄色火焰是不完全燃烧、温度低。", hold: 12f));
        L.Add(S("三脚架与坩埚", "需要高温灼烧时用「三脚架 + 坩埚」：坩埚架在三脚架上，底下点本生灯。\n碳酸钙分解这类反应就得这么做。", hold: 12f));

        /* --- 倾倒 --- */
        L.Add(S("器皿互倒", "再学一个常用操作：把一个器皿拖到另一个器皿附近，会自动吸附到它上方，\n松手后位置就固定了。", hold: 12f));
        L.Add(S("倾倒滑条", "此时拖动旁边的滑条，就能把上面那个器皿里的物质倒进下面那个 ——\n滑条控制倒多少。做滴定、做过量反应都靠这个。", hold: 13f));

        /* --- 检验材料 --- */
        L.Add(S("认识检验材料", "左下角有「检验材料」，是考试里最常考的那几样：\n· 红色石蕊试纸 / 蓝色石蕊试纸（可以点「润湿」）\n· 木条（正常 / 点燃 / 刚熄灭三种状态）", hold: 13f));
        L.Add(S("石蕊怎么用", "湿润的红色石蕊试纸遇到碱性气体（比如 NH₃）会变蓝。\n所以检验铵根离子就是：加 NaOH 加热，把湿润红色石蕊试纸放管口，变蓝就说明有 NH₄⁺。", hold: 14f));
        L.Add(S("木条怎么用", "木条用来检验气体：\n· 伸进氧气 → 复燃，烧得更旺\n· 靠近氢气 → 发出尖锐的爆鸣声\n这是区分氧气和氢气最经典的办法。", hold: 14f));

        /* --- 换一个反应 --- */
        L.Add(S("再做一个反应", "最后做一个「冒泡」的：我往新烧杯里加碳酸钠，再滴盐酸。",
            () => { var v = Lab.AddVessel(VesselKind.Beaker); v.AddWater(80); Lab.AddReagent(v, "na2co3", 3.0); Lab.AddLiquid(v, "hcl", 8); },
            () => AnyGas(), 10f));
        L.Add(S("认出二氧化碳", "冒气泡了，那是 CO₂。要确认它是二氧化碳，就把气体通进石灰水 ——\n石灰水变浑浊就是 CO₂。这个反应也会弹方程式卡片。", hold: 13f));

        /* --- 记录与整理 --- */
        L.Add(S("实验记录", "底下那条是「实验记录」：每一步操作、每一个现象都自动记下来。\n做复杂实验忘了走到哪一步，回头看这里就行。", hold: 12f));
        L.Add(S("整理台面", "不想要的器皿右键就能删掉，台面随时可以清理。", hold: 9f));

        /* --- 收尾 --- */
        L.Add(S("功能总结", "实验部分教完了。你学过的：\n取器皿 · 加试剂 · 加热 · 倾倒 · 检验 · 读标签 · 看方程式 · 查记录。", hold: 12f));
        L.Add(S("解锁 AI 助手", "从现在起我就是你的 AI 助手了 —— 右键点我，选「打开聊天」。\n你可以直接说「帮我搭一个银镜反应」或者「教我做滴定」，我来搭给你看。", hold: 13f));
        L.Add(S("还剩一个", "顶栏那个『焓变计算器』还没教。等你第一次点开它，我再带你做一道题。\n祝实验愉快～", hold: 10f));

        return L;
    }

    /* ================================================================
       第二段：焓变计算器
       例子：KCl 的 Born-Haber 循环
         K(s) → K(g)            +89
         K(g) → K⁺(g) + e⁻      +418
         ½Cl₂(g) → Cl(g)        +121
         Cl(g) + e⁻ → Cl⁻(g)    −349
         K(s) + ½Cl₂(g) → KCl(s) 生成焓 −437
         → 晶格能 = −437 − (89 + 418 + 121 − 349) = −716 kJ/mol
       ================================================================ */
    static List<TutorialStep> BuildCalc() {
        var L = new List<TutorialStep>();
        int n = 0;
        TutorialStep S(string title, string say, Action d = null, Func<bool> w = null, float hold = 8f) {
            n++;
            return new TutorialStep { Title = n + ". " + title, Say = say, Do = d, WaitFor = w, Hold = hold };
        }

        L.Add(new TutorialStep {
            Title = "焓变计算器", GoCalc = true, Hold = 9f,
            Say = "这就是焓变计算器。它只做一件事：按盖斯定律，沿着闭合的能量循环解出未知的焓变。",
            Do = () => { if (EnthalpyScene.Instance != null) EnthalpyScene.Instance.ClearAll(); },
        });
        L.Add(S("什么是节点", "每个方框是一个「节点」，代表一个化学物种或一个状态。\n左边小框填系数，右边填化学式。", hold: 11f));
        L.Add(S("回车自动匹配", "在化学式框里输入后按回车，我会自动匹配库里的物种，\n并把上下标写成规范形式 —— 输入 co2 变成 CO₂，fe3+ 变成 Fe³⁺。\n匹配不上会变红，提醒你检查拼写。", hold: 14f));
        L.Add(S("拉出连线", "从节点上按住左键往外拖，会拉出一根带箭头的白色曲线。\n箭头跟着鼠标走，松到另一个节点上就连起来了。", hold: 12f));
        L.Add(S("自动避让", "连线的弧度是自动算的：绕开所有节点和其它连线，不会压在谁身上。\n同一条直线上有多条线时也会自动错开。", hold: 13f));
        L.Add(S("选焓变来源", "连好之后我会问你这条边的焓变从哪来：\n· 查表 —— 库里有的直接取\n· 手动输入 —— 弹框让你填数值\n· 未知量 —— 留作待求（显示成黄色的 ΔH = ?）\n选错了点右键取消，这条线也会跟着撤销。", hold: 16f));
        L.Add(S("摆一道真题", "现在摆一道真的：求氯化钾 KCl 的晶格能。",
            () => EnthalpyScene.Instance?.SeedBornHaberDemo(), hold: 11f));
        L.Add(S("读懂这张图", "这张图就是 Born-Haber 循环：\n· K(s) → K⁺(g)：钾原子化 +89 加第一电离能 +418\n· ½Cl₂(g) → Cl⁻(g)：氯原子化 +121 加电子亲和能 −349\n· K⁺(g) + Cl⁻(g) → KCl(s)：这就是要求的晶格能，留成未知量", hold: 18f));
        L.Add(S("生成焓那条边", "还有一条从 K(s) 直接到 KCl(s) 的边，值是标准生成焓 −437 kJ/mol。\n有了它回路才闭合 —— 盖斯定律的关键就是「条条大路通罗马」。", hold: 16f));
        L.Add(S("求解", "点右上角『求解未知焓变』。\n它会沿回路把已知焓变加起来，令总和为零，反推未知量。",
            () => EnthalpyScene.Instance?.Solve(),
            () => EnthalpyScene.Instance != null && EnthalpyScene.Instance.LastSolvedOk, 9f));
        L.Add(S("看结果", "晶格能解出来是 −716 kJ/mol。负号说明放热 —— 离子结合成晶体本来就该放热。\n这个数和课本上的标准值是对得上的。", hold: 14f));
        L.Add(S("回路不闭合怎么办", "要是圈闭不上，它会告诉你哪里断了；\n未知量多于一个时它会说「条件不足」，不会硬凑答案给你。", hold: 14f));
        L.Add(S("右键编辑", "连线右键可以改焓变值或删掉；节点右键能改化学式、复制、删除。\n画错了随时改。", hold: 12f));
        L.Add(S("还能算什么", "同一个套路可以算：\n· 生成焓、燃烧焓、中和焓\n· 键能、原子化焓、电子亲和能\n· 水合焓、溶解焓、晶格能\n只要是能量循环都能这么解。", hold: 15f));
        L.Add(S("结束", "计算器就教到这儿。AI 助手随时待命 —— 右键我点「打开聊天」，\n你说「帮我算硫酸镁的晶格能」我就能给你搭出来。", hold: 11f));

        return L;
    }
}

}
