using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ChemLab.Chem;

namespace ChemLab.Game {

/// <summary>
/// Player-side acceptance for the developer console and its two commands.
/// Started only by `-consoleTest`, and it drives the real input path by injecting
/// the same protocol lines the external pipe test sends, so nothing here touches
/// the desktop mouse or keyboard.
///
/// Requires `-windowTargetInput` as well, plus `-noTut` so the tutorial does not
/// take over the scene. Intended to run in a hidden player:
///   Build\ChemLab.exe -batchmode -consoleTest -windowTargetInput -noTut -logFile Logs\console-audit-player.log
/// </summary>
public sealed class ConsoleCommandAuditRunner : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch() {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-consoleTest") < 0) return;
        Application.runInBackground = true;
        new GameObject("Console command audit").AddComponent<ConsoleCommandAuditRunner>();
    }

    IEnumerator Start() {
        yield return new WaitForSeconds(2f);
        int exitCode = 0;
        var audit = new ConsoleCommandAudit();
        var routine = audit.Run();
        while (true) {
            bool next;
            try { next = routine.MoveNext(); }
            catch (Exception e) { Debug.LogException(e); exitCode = 1; break; }
            if (!next) break;
            yield return routine.Current;
        }
        try {
            audit.Lines.Add("TOTAL " + audit.Passed + " passed, " + audit.Failed + " failed");
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/console-audit.txt", audit.Lines);
        } catch (Exception e) { Debug.LogException(e); exitCode = 1; }
        Debug.Log("[ConsoleAudit] TOTAL " + audit.Passed + " passed, " + audit.Failed + " failed");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/console-audit.png"));
        yield return new WaitForSeconds(0.5f);
        Application.Quit(exitCode == 0 && audit.Failed == 0 ? 0 : 1);
    }
}

public sealed class ConsoleCommandAudit {
    public int Passed { get; private set; }
    public int Failed { get; private set; }
    public List<string> Lines { get; } = new List<string>();

    LabBootstrap _lab;
    DeveloperConsole _console;
    DavidDriver _david;
    Camera _cam;
    LabUI _ui;

    void Check(string label, bool ok) {
        if (ok) Passed++; else Failed++;
        Lines.Add((ok ? "PASS " : "FAIL ") + label);
        // Only failures are echoed to the player log; the console history stays
        // readable and the log file is the authoritative record.
        if (!ok) Debug.LogError("[ConsoleAudit] FAIL " + label);
    }

    void Metric(string text) { Lines.Add("METRIC " + text); }
    void Perf(string text) { Lines.Add("PERF " + text); }

    static void Inject(string line) { WindowTargetInput.InjectForTest(line); }

    static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }

    static IEnumerator PressKey(KeyCode key) {
        Inject("KEYDOWN " + key);
        yield return null;
        Inject("KEYUP " + key);
        yield return Frames(2);
    }

    /* ================================================================ */
    public IEnumerator Run() {
        _lab = LabBootstrap.Instance;
        _console = DeveloperConsole.Instance;
        _david = DavidDriver.Instance;
        _cam = _lab != null ? _lab.Cam : null;
        _ui = _lab != null ? UnityEngine.Object.FindObjectOfType<LabUI>() : null;
        bool inputReady = WindowTargetInput.Active;

        Check("console instance exists in the live scene", _console != null);
        Check("laboratory UI is available for drop behaviour", _ui != null);
        Check("console starts closed", _console != null && !_console.IsOpen && !DeveloperConsole.InputCaptured);
        Check("david driver is installed on the lab host", _david != null);
        Check("afterimage shader ships in Resources", Resources.Load<Shader>("ChemLab/Ghost") != null);
        Check("audit drives the real input path", inputReady);
        if (_lab == null || _console == null || _david == null || _cam == null || !inputReady) {
            yield return null;
            yield break;
        }

        // Deterministic fixture: clear the seeded demo vessels and rebuild views.
        Lab.Reset();
        _lab.SyncViews();
        yield return Frames(3);

        yield return ConsoleShellSection();
        yield return ConsoleInterruptedDragSection();
        yield return WorldSpeedSection();
        yield return DavidSection();

        WorldClock.Set(1d);
        DavidMode.ResetForTest();
        yield return null;
    }

    /* ================================================================
       1. 控制台接入：~ 开关、日志、焦点隔离
       ================================================================ */
    IEnumerator ConsoleShellSection() {
        Lines.Add("--- console shell ---");
        var fixture = MakeVessel(VesselKind.Beaker, new Vector3(-0.72f, 0f, -0.5f), 60, false);
        yield return Frames(4);
        Vector3 focusBefore = _lab.TestFocusPoint;
        // The pick point must be resolved while the console is closed: its overlay
        // deliberately blocks every UI raycast.
        Vector2 grab = FindPickPoint(h => h.vessel == fixture, fixture);
        Check("console fixture vessel is pickable", grab.x >= 0);

        // ① `~` opens and the console takes the command field focus.
        yield return PressKey(KeyCode.BackQuote);
        Check("~ opens the console", _console.IsOpen && DeveloperConsole.InputCaptured);
        Check("console command field takes focus",
            _console.TestInputField != null && EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == _console.TestInputField.gameObject);

        // ② while open, keys and pointer must not reach the laboratory at all.
        Inject("KEYDOWN W");
        yield return Frames(12);
        Check("open console swallows camera keys",
            Vector3.Distance(_lab.TestFocusPoint, focusBefore) <= 1e-4f);
        Inject("MOVE " + (int)grab.x + " " + (int)grab.y);
        yield return null;
        Inject("DOWN 0");
        bool draggedWhileOpen = false;
        for (int i = 0; i < 10; i++) {
            Inject("MOVE " + (int)(grab.x + i * 9) + " " + (int)(grab.y + i * 4));
            yield return null;
            draggedWhileOpen |= _lab.TestDragTargetId != "-";
        }
        Inject("UP 0");
        Inject("KEYUP W");
        yield return Frames(2);
        Check("open console swallows pointer dragging", !draggedWhileOpen);

        // ③ negative control: the very same injection works once it is closed.
        yield return PressKey(KeyCode.BackQuote);
        Check("~ closes the console", !_console.IsOpen && !DeveloperConsole.InputCaptured);
        Inject("KEYDOWN W");
        yield return Frames(12);
        Inject("KEYUP W");
        Check("closed console lets camera keys through again",
            Vector3.Distance(_lab.TestFocusPoint, focusBefore) > 1e-4f);
        Inject("KEYDOWN S");
        yield return Frames(12);
        Inject("KEYUP S");
        yield return Frames(2);

        // negative control for the pointer as well: the same drag must work again.
        Inject("MOVE " + (int)grab.x + " " + (int)grab.y);
        yield return Frames(2);
        Inject("DOWN 0");
        bool draggedWhileClosed = false;
        for (int i = 0; i < 10; i++) {
            Inject("MOVE " + (int)(grab.x + i * 9) + " " + (int)(grab.y + i * 4));
            yield return null;
            draggedWhileClosed |= _lab.TestDragTargetId != "-";
        }
        Inject("UP 0");
        yield return Frames(2);
        Check("closed console lets pointer dragging through again", draggedWhileClosed);

        // ④ log capture, unknown commands, command echo.
        Debug.Log("[ConsoleAudit] log-capture-probe-1735");
        yield return Frames(4);
        Check("console mirrors Debug.Log output",
            string.Join("\n", _console.GetHistorySnapshot()).Contains("log-capture-probe-1735"));
        _console.Submit("no_such_command");
        Check("unknown command is reported",
            string.Join("\n", _console.GetHistorySnapshot()).Contains("[ERR] Unknown command"));
        _console.Submit("world_speed 1");
        Check("console echoes submitted commands",
            string.Join("\n", _console.GetHistorySnapshot()).Contains("> world_speed 1"));

        // ⑤ the real Enter path: field text (the pipe cannot type characters) plus
        //    a genuine Return key event, which LateUpdate turns into a submission.
        yield return PressKey(KeyCode.BackQuote);
        Inject("CONSOLETEXT world_speed 1.5");
        yield return Frames(2);
        Inject("KEYDOWN Return");
        yield return Frames(3);
        Inject("KEYUP Return");
        yield return Frames(2);
        Check("Enter submits the typed command line", Math.Abs(WorldClock.Rate - 1.5d) < 1e-9);
        Check("submitted line clears the input field",
            _console.TestInputField != null && string.IsNullOrEmpty(_console.TestInputField.text));
        Check("console stays open while a command runs", _console.IsOpen);
        Inject("CONSOLETEXT world_speed 3.0001");
        yield return Frames(2);
        yield return PressKey(KeyCode.Return);
        Check("Enter rejects a value above 3 without changing the rate", WorldClock.Rate == 1.5d);
        Check("Enter rejection reports an error", _console.GetHistorySnapshot().Any(
            line => line.StartsWith("[ERR] world_speed: 3.0001", StringComparison.Ordinal)));
        yield return PressKey(KeyCode.BackQuote);
        Check("~ closes after commands", !_console.IsOpen);

        _console.Submit("world_speed 1");
        Lab.RemoveVessel(fixture);
        yield return Frames(2);
    }

    /* ================================================================
       2. world_speed：0–3、小数、统一时间速率、冻结与恢复
       ================================================================ */
    IEnumerator ConsoleInterruptedDragSection() {
        foreach (bool david in new[] { false, true }) {
            _console.Submit("david " + (david ? "1" : "0"));
            var vessel = MakeVessel(VesselKind.Beaker, new Vector3(0f, 0f, -0.5f), 30, false);
            _lab.SyncViews();
            yield return Frames(3);
            Vector2 grab = FindPickPoint(h => h.vessel == vessel, vessel);
            Inject("MOVE " + (int)grab.x + " " + (int)grab.y);
            yield return Frames(2);
            Inject("DOWN 0");
            yield return Frames(2);
            Inject("MOVE " + (int)(grab.x + 65) + " " + (int)(grab.y + 25));
            yield return Frames(12);
            Check("interrupted drag starts david=" + david, _lab.TestDragTargetId == vessel.Id);
            yield return PressKey(KeyCode.BackQuote);
            Check("console retains held gesture david=" + david, _lab.TestDragTargetId == vessel.Id);
            Vector3 intent = david ? _david.IntentForTest : vessel.Pos;
            Inject("UP 0");
            for (int i = 0; i < 180 && _lab.TestDragTargetId != "-"; i++) yield return null;
            Check("release inside console ends drag david=" + david, _lab.TestDragTargetId == "-" && !_david.Active);
            Check("release keeps final destination david=" + david,
                Vector2.Distance(new Vector2(vessel.Pos.x, vessel.Pos.z), new Vector2(intent.x, intent.z)) < 0.05f);
            yield return PressKey(KeyCode.BackQuote);
            yield return Frames(2);
            Check("closing console cannot revive drag david=" + david, _lab.TestDragTargetId == "-");
            Lab.RemoveVessel(vessel);
            yield return Frames(2);
        }
        _console.Submit("david 0");
    }

    IEnumerator WorldSpeedSection() {
        Lines.Add("--- world_speed ---");

        // ① invalid input is refused, and refused without clamping.
        _console.Submit("world_speed 1");
        foreach (string bad in new[] { "", "abc", "-1", "3.0001", "3.01", "4", "5", "6", "5.5", "NaN", "Infinity", "1,5", "0x2", "2 3" }) {
            _console.Submit("world_speed " + bad);
            Check("world_speed rejects '" + bad + "' instead of clamping",
                Math.Abs(WorldClock.Rate - 1d) < 1e-12);
        }
        Check("rejection explains itself",
            string.Join("\n", _console.GetHistorySnapshot()).Contains("[ERR] world_speed"));

        // ② every valid spelling is applied exactly as typed.
        foreach (var pair in new[] {
            Tuple.Create("0", 0d), Tuple.Create("0.25", 0.25d), Tuple.Create("1", 1d),
            Tuple.Create("2.5", 2.5d), Tuple.Create("3.00", 3d), Tuple.Create("2.75", 2.75d) }) {
            _console.Submit("world_speed " + pair.Item1);
            Check("world_speed accepts " + pair.Item1, Math.Abs(WorldClock.Rate - pair.Item2) < 1e-12);
        }

        _console.Submit("world_speed 3.01");
        Check("rejected rate preserves the previous fractional value", WorldClock.Rate == 2.75d);
        yield return PressKey(KeyCode.BackQuote);
        yield return Frames(2);
        var output = GameObject.Find("DeveloperConsoleOverlay").GetComponentsInChildren<Text>(true)
            .FirstOrDefault(t => t.text.Contains("outside the supported range"));
        Check("range error is rendered red", output != null && output.supportRichText
            && output.text.Contains("<color=#FF6464>[ERR] world_speed: 3.01"));
        Check("logs cannot inject rich text", !DeveloperConsole.FormatHistoryLine("<size=99>log</size>").Contains("<size"));
        yield return PressKey(KeyCode.BackQuote);

        // ③ 0 freezes every experiment subsystem. The fixture carries enough ice to
        //    outlast the whole sequence and is left to warm from the room alone:
        //    heating it would melt the solids away before they could be observed.
        var freeze = MakeVessel(VesselKind.Beaker, new Vector3(-0.35f, 0f, -0.55f), 100, true);
        freeze.Undissolved[WaterIce.Id] = 3.0;
        _lab.SyncViews();
        yield return Frames(4);
        var freezeView = ViewFor(freeze);
        Check("freeze fixture runs the solid solver", freezeView != null && freezeView.TestSolidSolverActive);
        _console.Submit("world_speed 1");
        yield return Frames(10);

        _console.Submit("world_speed 0");
        yield return Frames(3);
        double clock0 = Lab.Clock, temp0 = freeze.Temp, water0 = freeze.Water;
        var solids0 = freezeView != null ? freezeView.TestSolidPositions() : Array.Empty<Vector3>();
        int builds0 = freezeView != null ? freezeView.Fluid.FieldBuildCount : 0;
        int reactions0 = Lab.RecentReactions.Count;
        yield return Frames(50);
        Check("world_speed 0 freezes the experiment clock", Lab.Clock == clock0);
        Check("world_speed 0 freezes heating and cooling", freeze.Temp == temp0);
        Check("world_speed 0 freezes inventory", freeze.Water == water0);
        Check("world_speed 0 freezes reactions", Lab.RecentReactions.Count == reactions0);
        Check("world_speed 0 freezes the solid solver",
            freezeView != null && solids0.Length > 0 && PositionsEqual(solids0, freezeView.TestSolidPositions()));
        Check("world_speed 0 freezes the GPU fluid solver",
            freezeView != null && freezeView.Fluid.FieldBuildCount == builds0);

        // ④ the same frames at 1x must move those quantities again.
        _console.Submit("world_speed 1");
        yield return Frames(50);
        Check("world_speed 1 resumes heating", freeze.Temp > temp0);
        Check("world_speed 1 resumes the solid solver",
            freezeView != null && !PositionsEqual(solids0, freezeView.TestSolidPositions()));

        // ⑤ the clock and an independent time consumer both scale with the rate.
        double baselineClock = 0, baselineDrain = 0;
        yield return MeasureRate(1d, (clock, drain) => { baselineClock = clock; baselineDrain = drain; });
        foreach (double rate in new[] { 0.5d, 2d, 3d }) {
            double clockPerSecond = 0, drainPerSecond = 0;
            yield return MeasureRate(rate, (clock, drain) => { clockPerSecond = clock; drainPerSecond = drain; });
            double clockRatio = clockPerSecond / baselineClock;
            double drainRatio = drainPerSecond / Math.Max(1e-9, baselineDrain);
            Check("world_speed " + rate + " scales the experiment clock",
                Math.Abs(clockRatio - rate) <= Math.Max(0.05d, rate * 0.08d));
            Check("world_speed " + rate + " scales an independent time consumer (boil-off)",
                Math.Abs(drainRatio - rate) <= Math.Max(0.15d, rate * 0.3d));
            Metric("world_speed " + rate + " clockPerSecond=" + clockPerSecond.ToString("F4")
                 + " ratio=" + clockRatio.ToString("F4") + " boilOffPerSecond=" + drainPerSecond.ToString("F6")
                 + " drainRatio=" + drainRatio.ToString("F4"));
        }

        // ⑥ pause, then drive the console while the world is frozen. A fresh vessel
        //    keeps the resume check clear of the 105 degree ceiling.
        _console.Submit("world_speed 1");
        var pause = MakeVessel(VesselKind.Beaker, new Vector3(0f, 0f, -0.65f), 100, false);
        pause.Heating = true;
        yield return Frames(25);
        _console.Submit("world_speed 0");
        yield return Frames(3);
        double frozenTemp = pause.Temp;
        yield return Frames(30);
        Check("world_speed 0 pauses a running experiment", pause.Temp == frozenTemp);
        Check("world_speed 0 pauses an experiment that was warming up", frozenTemp > 25.5);
        yield return PressKey(KeyCode.BackQuote);
        Check("console still opens while the experiment is frozen", _console.IsOpen);
        _console.Submit("world_speed 1");
        Check("console restores world speed while frozen", Math.Abs(WorldClock.Rate - 1d) < 1e-12);
        yield return PressKey(KeyCode.BackQuote);
        yield return Frames(30);
        Check("restored rate resumes the frozen experiment", pause.Temp > frozenTemp);

        _console.Submit("world_speed 1");
        Lab.RemoveVessel(pause);
        Lab.RemoveVessel(freeze);
        yield return Frames(2);
    }

    /// <summary>Measures experiment seconds and boiled-off mL per wall-clock second.</summary>
    IEnumerator MeasureRate(double rate, Action<double, double> report) {
        var boiler = MakeVessel(VesselKind.Beaker, new Vector3(0.9f, 0f, 1.05f), 120, false);
        boiler.Temp = 105f;
        boiler.Heating = true;
        _console.Submit("world_speed " + rate.ToString(CultureInfo.InvariantCulture));
        yield return Frames(3);
        double clockStart = Lab.Clock, waterStart = boiler.Water, wall = 0;
        for (int i = 0; i < 45; i++) {
            yield return null;
            wall += Time.unscaledDeltaTime;
        }
        double clockDelta = Lab.Clock - clockStart;
        double drained = waterStart - boiler.Water;
        Lab.RemoveVessel(boiler);
        report(wall > 0 ? clockDelta / wall : 0, wall > 0 ? drained / wall : 0);
    }

    /* ================================================================
       3. david：突进、残影、松手落点
       ================================================================ */
    IEnumerator DavidSection() {
        Lines.Add("--- david ---");

        foreach (string bad in new[] { "", "2", "-1", "yes", "1.0", "true" }) {
            _console.Submit("david " + bad);
            Check("david rejects '" + bad + "'", !DavidMode.Enabled);
        }
        _console.Submit("david 1");
        Check("david 1 enables the dash drive", DavidMode.Enabled);

        var target = MakeVessel(VesselKind.Beaker, new Vector3(-0.62f, 0f, -0.35f), 70, false);
        var mover = MakeVessel(VesselKind.Beaker, new Vector3(0.62f, 0f, -0.35f), 80, false);
        mover.Undissolved["cu"] = 0.01;
        yield return Frames(6);
        var moverView = ViewFor(mover);
        Check("david fixture renders liquid and solid contents",
            moverView != null && moverView.transform.Find("Liquid") != null && moverView.transform.Find("Sediment") != null);
        Check("the live instrument keeps its own approved material",
            moverView != null && moverView.transform.Find("Glass") != null &&
            moverView.transform.Find("Glass").GetComponent<MeshRenderer>().sharedMaterial.shader.name != "ChemLab/Ghost");

        double waterBefore = mover.Water, massBefore = mover.Mass(), tempBefore = mover.Temp;
        int reactionsBefore = Lab.RecentReactions.Count;
        int componentsBefore = ComponentsNow();

        Vector2 grab = FindPickPoint(h => h.vessel == mover, mover);
        Check("david fixture vessel is pickable", grab.x >= 0);
        // Aim the drag so the beaker's base lands exactly on the other beaker: the
        // ordinary drag holds the grabbed surface point under the pointer, so the
        // pointer has to travel to that point's screen position.
        Vector3 grabOffsetWorld = PickPlanePoint(grab, 0f) - mover.Pos;
        // Land on empty bench: dropping one beaker onto another is the staged-pour
        // modal, which is exercised separately below.
        Vector3 dropPoint = new Vector3(0.05f, 0f, -0.78f);
        Vector2 targetPoint = _cam.WorldToScreenPoint(dropPoint + grabOffsetWorld);

        _david.TestSeed(20260926);
        _david.TestClearSegmentLog();
        _david.TestSetNextSegmentLength(0.6f);

        int framesWithGhosts = 0, framesActive = 0, framesWithOffset = 0, framesFinishing = 0;
        int dragFrameCount = 0, lingeringDuringPause = 0, maxLiveGhosts = 0;
        bool observedFade = false;
        float maxOffset = 0f;
        bool insideView = true;
        var frameTimes = new List<float>();
        var ghostFirstSeen = new Dictionary<int, float>();
        int pauseSamples = 0, pausesObserved = 0, pauseTailVisible = 0;
        float pauseOldest = float.MaxValue;
        bool retireOrderOk = true, dashStartedWithLeftovers = false;
        var firstGhostOfSegment = new Dictionary<int, GhostObservation>();
        var ghostDistances = new Dictionary<int, List<float>>();
        var ghostColorT = new Dictionary<int, List<float>>();
        Vector3 lastIntent = Vector3.zero;
        int observedComponents = componentsBefore;
        int framesDragging = 0;
        bool sawDriver = false;
        Vector2 moverStart = new Vector2(mover.Pos.x, mover.Pos.z);

        Inject("MOVE " + (int)grab.x + " " + (int)grab.y);
        yield return Frames(2);
        Inject("DOWN 0");
        yield return Frames(2);

        const int dragFrames = 150;
        for (int frame = 0; frame < dragFrames; frame++) {
            float t = (frame + 1f) / dragFrames;
            Vector2 point = Vector2.Lerp(grab, targetPoint, t);
            Inject("MOVE " + Mathf.RoundToInt(point.x) + " " + Mathf.RoundToInt(point.y));
            if (frame == 40 || frame == 90) _david.TestSetNextSegmentLength(5f);
            yield return null;

            frameTimes.Add(Time.unscaledDeltaTime * 1000f);
            dragFrameCount++;
            if (_lab.TestDragTargetId != "-") framesDragging++;
            if (_david.Active) { framesActive++; sawDriver = true; }
            if (_david.Finishing) framesFinishing++;
            float offset = _david.OffsetMagnitudeForTest;
            maxOffset = Mathf.Max(maxOffset, offset);
            if (offset > 0.05f) framesWithOffset++;
            Vector3 screen = _cam.WorldToScreenPoint(_david.VisualForTest);
            // The dash must never push the instrument out of the rendered view. A
            // pointer already past the edge is allowed to leave it at the pointer.
            bool visualInside = screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width
                             && screen.y >= 0 && screen.y <= Screen.height;
            bool collapsed = Vector3.Distance(_david.VisualForTest, _david.IntentForTest) < 1e-4f;
            if (!visualInside && !collapsed) insideView = false;
            lastIntent = _david.IntentForTest;

            int segment = _david.SegmentIndexForTest;
            if (_david.LiveGhostCountForTest > 0) framesWithGhosts++;
            maxLiveGhosts = Mathf.Max(maxLiveGhosts, _david.LiveGhostCountForTest);
            // During a pause the copies retire oldest first and the whole trail is
            // gone before the next dash; during a dash nothing is capped.
            string phase = _david.PhaseForTest;
            var ghostsNow = _david.GhostsForTest();
            if (phase == "Pause") {
                int visibleNow = 0;
                float oldestVisible = float.MaxValue;
                foreach (var ghost in ghostsNow) {
                    if (!ghost.Visible) continue;
                    visibleNow++;
                    oldestVisible = Mathf.Min(oldestVisible, ghost.PathDistance);
                }
                if (visibleNow > 0) {
                    // The oldest copy must go first: the oldest survivor's distance
                    // can only grow while the pause drains the trail.
                    if (pauseSamples > 0 && oldestVisible < pauseOldest - 0.05f) retireOrderOk = false;
                    pauseOldest = oldestVisible;
                    pauseSamples++;
                    lingeringDuringPause++;
                }
                pauseTailVisible = visibleNow;
            } else if (pauseSamples > 0) {
                pausesObserved++;
                pauseSamples = 0;
                pauseOldest = float.MaxValue;
            }
            if (phase == "Dash" && _david.ScheduledGhostCountForTest != 0) dashStartedWithLeftovers = true;

            float now = Time.unscaledTime;
            foreach (var ghost in ghostsNow) {
                if (ghost.PathLength <= 0f) continue;
                if (ghost.Visible && ghost.Intensity < 0.6f) observedFade = true;
                if (!ghostDistances.TryGetValue(ghost.Segment, out var distances)) {
                    distances = new List<float>(); ghostDistances[ghost.Segment] = distances;
                }
                distances.Add(ghost.PathDistance);
                if (!ghostColorT.TryGetValue(ghost.Segment, out var colors)) {
                    colors = new List<float>(); ghostColorT[ghost.Segment] = colors;
                }
                colors.Add(ghost.ColorT);
                if (!firstGhostOfSegment.ContainsKey(ghost.Segment))
                    firstGhostOfSegment[ghost.Segment] = ObserveGhost(moverView);
            }
            observedComponents = Mathf.Max(observedComponents, ComponentsNow());
            // The dash only starts once the pointer leaves the click slop, so the
            // first quiet frames are not a finished drag.
            if (sawDriver && !_david.Active) break;
            if (!sawDriver && frame > 60) break;
        }

        Metric("david drag frames=" + dragFrameCount + " driverFrames=" + framesActive
             + " draggingFrames=" + framesDragging + " grab=" + grab.x + "," + grab.y
             + " pickNow=" + (LabPick.Pick(grab, _cam).vessel == mover) + " view=" + (moverView != null)
             + " moved=" + (Vector2.Distance(new Vector2(mover.Pos.x, mover.Pos.z), moverStart) > 0.01f));

        Check("david engaged for the whole drag", framesActive > dragFrames / 2);
        Check("david dashes away from the pointer", framesWithOffset > 20 && maxOffset > 0.15f);
        Check("david stays within 50 cm of the intended point", maxOffset <= DavidMode.MaximumDashUnits + 1e-3f);
        Check("david keeps the instrument inside the view", insideView);
        Check("david leaves afterimages along the swept path", framesWithGhosts > 20);
        Check("david never spawns extra chemical components", observedComponents == componentsBefore);
        Check("david afterimages carry no chemistry, collider or physics",
            AfterimagesCarryNothingButMeshes());

        // spacing: consecutive snapshots must overlap
        float horizontalExtent = HorizontalExtent(moverView);
        float worstSpacing = 0f;
        foreach (var pair in ghostDistances) {
            var distances = pair.Value;
            distances.Sort();
            for (int i = 1; i < distances.Count; i++)
                worstSpacing = Mathf.Max(worstSpacing, distances[i] - distances[i - 1]);
        }
        Check("david snapshots overlap each other", worstSpacing > 0f && worstSpacing <= horizontalExtent * 0.9f);
        Metric("david spacing=" + _david.GhostSpacingForTest.ToString("F4") + " worstObserved="
             + worstSpacing.ToString("F4") + " objectExtent=" + horizontalExtent.ToString("F4"));

        // Sample count follows the swept distance, and the copies must cover the whole
        // swept path: stopping part way would leave a bare gap between the last
        // afterimage and the instrument itself.
        var segments = new List<DavidDriver.SegmentRecord>(_david.SegmentsForTest);
        float spacing = _david.GhostSpacingForTest;
        bool countsFollowDistance = true;
        int completed = 0, shortCount = 0, longCount = 0, gapsChecked = 0;
        float shortLength = 0f, longLength = 0f, worstGap = 0f;
        foreach (var segment in segments) {
            if (segment.EndTime < 0f || segment.Final) continue;
            completed++;
            int expected = Mathf.FloorToInt(segment.Length / Mathf.Max(0.01f, spacing));
            if (Mathf.Abs(segment.Ghosts - expected) > 1) countsFollowDistance = false;
            if (segment.Length > longLength) { longLength = segment.Length; longCount = segment.Ghosts; }
            if (shortLength <= 0f || segment.Length < shortLength) { shortLength = segment.Length; shortCount = segment.Ghosts; }

            if (!ghostDistances.TryGetValue(segment.Index, out var samples)) continue;
            var positions = new List<float>();
            foreach (var value in samples) {
                bool seen = false;
                foreach (var existing in positions)
                    if (Mathf.Abs(existing - value) < 0.05f) { seen = true; break; }
                if (!seen) positions.Add(value);
            }
            if (positions.Count < 2) continue;
            positions.Sort();
            gapsChecked++;
            float previous = positions[0];
            for (int i = 1; i < positions.Count; i++) {
                worstGap = Mathf.Max(worstGap, positions[i] - previous);
                previous = positions[i];
            }
            // head gap: distance from the last copy to the tip of that dash
            worstGap = Mathf.Max(worstGap, segment.Length - previous);
        }
        Check("david sampled the path at a distance-dependent rate",
            completed >= 2 && countsFollowDistance && longLength > shortLength + 0.5f);
        Check("david leaves no bare gap in front of the instrument or between copies",
            gapsChecked >= 2 && worstGap <= spacing * 1.6f);
        Check("david does not cap the number of copies along a dash",
            longCount >= Mathf.FloorToInt(longLength / Mathf.Max(0.01f, spacing)));
        Metric("david segments=" + completed + " shortest=" + shortLength.ToString("F3") + "u/" + shortCount
             + " ghosts longest=" + longLength.ToString("F3") + "u/" + longCount + " ghosts"
             + " worstGap=" + worstGap.ToString("F3") + " spacing=" + spacing.ToString("F3")
             + " maxLiveCopies=" + maxLiveGhosts);

        // roughly 0.1 s of stillness between segments
        bool pauseOk = true;
        int pausesChecked = 0;
        float worstPause = 0f;
        for (int i = 1; i < segments.Count; i++) {
            if (segments[i - 1].EndTime < 0f || segments[i - 1].Final) continue;
            float gap = segments[i].StartTime - segments[i - 1].EndTime;
            pausesChecked++;
            worstPause = Mathf.Max(worstPause, Mathf.Abs(gap - DavidMode.PauseSeconds));
            float expectedGap = DavidMode.PauseSeconds;
            if (gap < expectedGap - 0.03f || gap > expectedGap + 0.12f) pauseOk = false;
        }
        Check("david pauses for the configured interval between segments", pausesChecked >= 1 && pauseOk);
        Metric("david pauses=" + pausesChecked + " worstDeviation=" + worstPause.ToString("F4"));

        // colour: blue at the start of a segment, green along it, semi transparent
        bool gradientOk = true, colorOk = true, alphaOk = true, saturationOk = true;
        float worstColorDelta = 0f, smallestSpread = float.MaxValue;
        int colorSegments = 0, colorGhosts = 0;
        foreach (var ghost in _david.GhostsForTest()) {
            colorGhosts++;
            var expected = Color.Lerp(new Color(0.02f, 0.55f, 0.95f), new Color(0.05f, 0.85f, 0.20f), ghost.ColorT);
            float delta = Mathf.Max(Mathf.Abs(ghost.Color.r - expected.r),
                          Mathf.Max(Mathf.Abs(ghost.Color.g - expected.g), Mathf.Abs(ghost.Color.b - expected.b)));
            worstColorDelta = Mathf.Max(worstColorDelta, delta);
            colorOk &= delta < 0.03f;
            if (ghost.Visible) {
                alphaOk &= ghost.Color.a > 0.01f && ghost.Color.a <= 0.6f;
                // Saturated copies, not washed toward white.
                float chroma = Mathf.Max(ghost.Color.r, Mathf.Max(ghost.Color.g, ghost.Color.b))
                             - Mathf.Min(ghost.Color.r, Mathf.Min(ghost.Color.g, ghost.Color.b));
                saturationOk &= chroma > 0.4f;
            }
        }
        foreach (var pair in ghostColorT) {
            // Copies are re-observed every frame they stay alive, so count distinct
            // positions instead of samples: a single long-lived copy has no gradient.
            var distinct = new List<float>();
            foreach (var value in pair.Value) {
                bool seen = false;
                foreach (var existing in distinct)
                    if (Mathf.Abs(existing - value) < 0.02f) { seen = true; break; }
                if (!seen) distinct.Add(value);
            }
            if (distinct.Count < 3) continue;
            colorSegments++;
            float colorMin = float.MaxValue, colorMax = float.MinValue;
            foreach (var value in distinct) { colorMin = Mathf.Min(colorMin, value); colorMax = Mathf.Max(colorMax, value); }
            smallestSpread = Mathf.Min(smallestSpread, colorMax - colorMin);
            gradientOk &= colorMax - colorMin > 0.15f;
        }
        Check("david afterimages run blue to green along a segment", colorSegments >= 2 && gradientOk && colorOk);
        Check("david afterimages are semi transparent", alphaOk);
        Check("david afterimages stay saturated instead of washing white", saturationOk);
        Metric("david colour segments=" + colorSegments + " ghosts=" + colorGhosts
             + " worstDelta=" + worstColorDelta.ToString("F5")
             + " smallestSpread=" + (smallestSpread == float.MaxValue ? -1f : smallestSpread).ToString("F3"));

        // Afterimages retire inside the pause: oldest first, spread across the whole
        // interval, and the screen is clean again when the next dash starts.
        Check("david retires its copies oldest first during the pause", pausesObserved >= 2 && retireOrderOk);
        Check("david drains the whole trail before the next dash",
            pausesObserved >= 2 && !dashStartedWithLeftovers);
        Check("david fades its copies as they retire", observedFade);
        Metric("david pauses=" + pausesObserved + " lingerFrames=" + lingeringDuringPause
             + " tailVisible=" + pauseTailVisible + " maxLiveCopies=" + maxLiveGhosts
             + " pause=" + DavidMode.PauseSeconds.ToString("F2"));

        // snapshots reproduce the instrument and its contents as they were
        bool contentOk = firstGhostOfSegment.Count > 0, materialOk = true;
        foreach (var pair in firstGhostOfSegment) {
            var observation = pair.Value;
            contentOk &= observation.HasGlass && observation.HasLiquid && observation.HasSediment;
            contentOk &= observation.LiquidVertexMatch && observation.SolidVertexMatch;
            materialOk &= observation.Material == "ChemLab/GhostFluidSnapshot" && observation.FrozenLiquidField
                       && observation.OutlineAlpha >= 0.6f && observation.OutlineAlpha <= 0.8f;
            Metric("david snapshot segment=" + pair.Key + " glass=" + observation.HasGlass
                 + " liquidVerts=" + observation.LiquidVertices + " solidVerts=" + observation.SolidVertices
                 + " shader=" + observation.Material + " outlineAlpha=" + observation.OutlineAlpha.ToString("F2"));
        }
        Check("david snapshots capture the instrument and its contents", contentOk);
        Check("david snapshots render with the ghost shader and its dark outline", materialOk);

        // wait until the dash is clearly away from the pointer, then release
        int guard = 0;
        while (_david.Active && _david.OffsetMagnitudeForTest < 0.2f && guard < 90) { yield return null; guard++; }
        float releaseOffset = _david.OffsetMagnitudeForTest;
        Check("david is away from the pointer when the mouse is released", releaseOffset > 0.05f);
        Inject("UP 0");
        int waited = 0;
        while (_david.Active && waited < 120) { yield return null; waited++; if (_david.Finishing) framesFinishing++; }
        Check("david finished the release run", !_david.Active);
        Check("david ran a final straight segment after release", framesFinishing > 0);
        float landingError = Vector2.Distance(new Vector2(mover.Pos.x, mover.Pos.z),
                                             new Vector2(lastIntent.x, lastIntent.z));
        Check("david lands exactly on the intended drop point", landingError < 2e-3f);
        Check("david restores the ordinary drop behaviour",
            mover.AttachedTo == null && !_ui.IsPouring && !PourTransaction.IsActive
            && Vector2.Distance(new Vector2(mover.Pos.x, mover.Pos.z), new Vector2(dropPoint.x, dropPoint.z)) < 0.05f);
        Check("david clears its afterimages on release",
            _david.LiveGhostCountForTest == 0 && ActiveAfterimages() == 0);
        Check("david frees every copied snapshot mesh", _david.CopiedMeshCountForTest == 0);
        Check("david keeps no half finished drag", _lab.TestDragTargetId == "-");
        Metric("david release waitedFrames=" + waited + " releaseOffset=" + releaseOffset.ToString("F4")
             + " landingError=" + landingError.ToString("F5")
             + " attachedTo=" + (mover.AttachedTo != null ? mover.AttachedTo.Id : "-"));

        // the dragged instrument's own chemistry never advanced because of the effect
        Check("david snapshots never advance chemistry or add matter",
            mover.Water == waterBefore && mover.Mass() == massBefore && mover.Temp == tempBefore
            && Lab.RecentReactions.Count == reactionsBefore);
        Metric("david chemistry water=" + mover.Water.ToString("F6") + " mass=" + mover.Mass().ToString("F9")
             + " temp=" + mover.Temp.ToString("F4") + " reactions=" + (Lab.RecentReactions.Count - reactionsBefore));

        // `david 0` in the middle of a drag hands control straight back
        _david.TestClearSegmentLog();
        Vector2 grab2 = FindPickPoint(h => h.vessel == mover, mover);
        Inject("MOVE " + (int)grab2.x + " " + (int)grab2.y);
        yield return Frames(2);
        Inject("DOWN 0");
        yield return Frames(2);
        for (int i = 0; i < 24; i++) {
            Inject("MOVE " + (int)(grab2.x - i * 4) + " " + (int)grab2.y);
            yield return null;
        }
        float offsetBeforeOff = _david.OffsetMagnitudeForTest;
        _console.Submit("david 0");
        Check("david 0 disables the dash drive", !DavidMode.Enabled);
        yield return Frames(3);
        Check("david 0 clears the afterimages immediately",
            _david.LiveGhostCountForTest == 0 && ActiveAfterimages() == 0);
        Check("david 0 returns the instrument to the pointer", _david.OffsetMagnitudeForTest <= 0.02f);
        Vector3 beforeMove = mover.Pos;
        for (int i = 0; i < 8; i++) {
            Inject("MOVE " + (int)(grab2.x - 110f - i * 5f) + " " + (int)(grab2.y - 60f));
            yield return null;
        }
        Check("dragging follows the pointer again after david 0",
            Vector2.Distance(new Vector2(mover.Pos.x, mover.Pos.z), new Vector2(beforeMove.x, beforeMove.z)) > 0.05f);
        Inject("UP 0");
        yield return Frames(3);
        Check("ordinary release still resolves after david 0", _lab.TestDragTargetId == "-");
        Check("david 0 leaves no stray driver state", !_david.Active && _david.CopiedMeshCountForTest == 0);
        Metric("david offsetBeforeDisable=" + offsetBeforeOff.ToString("F4"));

        // the same drag mode must cover other instruments, not just glassware
        yield return DragApparatusWithDavid();

        // and the optional interval argument must actually change the pacing
        yield return IntervalArgumentChecks(mover);

        // and the ordinary vessel-onto-vessel drop must still reach the pour path
        yield return PourDropWithDavid(mover, target);

        // frame cost of the effect, recorded rather than gated (project convention)
        frameTimes.Sort();
        float p50Ms = frameTimes[frameTimes.Count / 2];
        float p95Ms = frameTimes[Mathf.Min(frameTimes.Count - 1, Mathf.RoundToInt(frameTimes.Count * 0.95f))];
        float maxMs = frameTimes[frameTimes.Count - 1];
        Perf("david drag frame ms p50=" + p50Ms.ToString("F2") + " p95=" + p95Ms.ToString("F2")
           + " max=" + maxMs.ToString("F2") + " frames=" + frameTimes.Count + " (recorded, not a gate)");

        // resource accounting: everything the effect created is reclaimed
        int createdGhosts = DavidDriver.GhostsCreatedForTest;
        int createdMeshes = DavidDriver.CopiedMeshesCreatedForTest;
        _david.Dispose();
        yield return null;
        Check("afterimage pool is destroyed on teardown",
            DavidDriver.GhostsDestroyedForTest == createdGhosts && ActiveAfterimages() == 0);
        Check("snapshot meshes are all released",
            DavidDriver.CopiedMeshesDestroyedForTest == createdMeshes && _david.CopiedMeshCountForTest == 0);
        Check("snapshot GPU textures and clipping buffers are all released", FluidVisualSnapshot.LiveCount == 0);
        Metric("david cleanup ghosts=" + createdGhosts + " meshes=" + createdMeshes);

        Lab.RemoveVessel(mover);
        Lab.RemoveVessel(target);
        yield return Frames(2);
    }

    /// <summary>
    /// Dropping one vessel onto another still has to reach the ordinary staged-pour
    /// path after a david drag, and cancelling it must restore the laboratory.
    /// </summary>
    IEnumerator PourDropWithDavid(Vessel mover, Vessel target) {
        if (mover == null || target == null || _ui == null) { Check("david pour fixture exists", false); yield break; }
        _console.Submit("david 1");
        Vector2 grab = FindPickPoint(h => h.vessel == mover, mover);
        Check("david pour fixture is pickable", grab.x >= 0);
        if (grab.x < 0) yield break;
        Vector3 grabOffsetWorld = PickPlanePoint(grab, 0f) - mover.Pos;
        Vector2 dropPoint = _cam.WorldToScreenPoint(target.Pos + grabOffsetWorld);
        Vector3 targetPosition = target.Pos;
        Inject("MOVE " + (int)grab.x + " " + (int)grab.y);
        yield return Frames(2);
        Inject("DOWN 0");
        yield return Frames(2);
        for (int i = 1; i <= 40; i++) {
            Vector2 point = Vector2.Lerp(grab, dropPoint, i / 40f);
            Inject("MOVE " + Mathf.RoundToInt(point.x) + " " + Mathf.RoundToInt(point.y));
            yield return null;
        }
        Inject("UP 0");
        bool poured = false;
        for (int i = 0; i < 120 && !poured; i++) {
            yield return null;
            poured = _ui.IsPouring || PourTransaction.IsActive;
        }
        Check("david release onto another vessel reaches the ordinary staged pour", poured);
        Inject("KEYDOWN Escape");
        yield return Frames(2);
        Inject("KEYUP Escape");
        int waited = 0;
        while (_ui.IsPouring && waited < 600) {
            yield return null;
            waited++;
            // The fly-back is a coroutine; nudge it again if it is still open.
            if (waited % 180 == 179) { Inject("KEYDOWN Escape"); yield return null; Inject("KEYUP Escape"); }
        }
        float restoreError = Vector3.Distance(target.Pos, targetPosition);
        Check("cancelling that pour restores the laboratory",
            !_ui.IsPouring && !PourTransaction.IsActive && restoreError < 0.01f && _lab.TestDragTargetId == "-");
        Metric("david pour frames=" + waited + " pouring=" + _ui.IsPouring + " transaction=" + PourTransaction.IsActive
             + " targetRestoreError=" + restoreError.ToString("F5") + " drag=" + _lab.TestDragTargetId);
        _console.Submit("david 0");
        yield return Frames(2);
    }

    /// <summary>`david 1 &lt;seconds&gt;` sets the stillness between two dashes.</summary>
    IEnumerator IntervalArgumentChecks(Vessel mover) {
        Lines.Add("--- david interval argument ---");
        _console.Submit("david 1");
        Check("david without an interval uses the 0.2 s default",
            Math.Abs(DavidMode.PauseSeconds - DavidMode.DefaultPauseSeconds) < 1e-6f);
        foreach (string bad in new[] { "abc", "-1", "20", "0.2 0.3", "1.5x" }) {
            _console.Submit("david 1 " + bad);
            Check("david rejects interval '" + bad + "'",
                Math.Abs(DavidMode.PauseSeconds - DavidMode.DefaultPauseSeconds) < 1e-6f && DavidMode.Enabled);
        }
        _console.Submit("david 1 0.45");
        Check("david accepts a 0.45 s interval", Math.Abs(DavidMode.PauseSeconds - 0.45f) < 1e-6f);

        Vector2 grab = FindPickPoint(h => h.vessel == mover, mover);
        Check("david interval fixture is pickable", grab.x >= 0);
        if (grab.x < 0) { _console.Submit("david 0"); yield break; }
        _david.TestClearSegmentLog();
        Inject("MOVE " + (int)grab.x + " " + (int)grab.y);
        yield return Frames(2);
        Inject("DOWN 0");
        yield return Frames(2);
        for (int i = 0; i < 100; i++) {
            Inject("MOVE " + Mathf.RoundToInt(grab.x + Mathf.Sin(i * 0.2f) * 60f) + " " + Mathf.RoundToInt(grab.y + i * 0.5f));
            yield return null;
        }
        Inject("UP 0");
        int waited = 0;
        while (_david.Active && waited < 150) { yield return null; waited++; }

        var gaps = new List<float>();
        var records = new List<DavidDriver.SegmentRecord>(_david.SegmentsForTest);
        for (int i = 1; i < records.Count; i++) {
            if (records[i - 1].EndTime < 0f || records[i - 1].Final) continue;
            gaps.Add(records[i].StartTime - records[i - 1].EndTime);
        }
        float median = 0f;
        if (gaps.Count > 0) { gaps.Sort(); median = gaps[gaps.Count / 2]; }
        Check("david waits the requested 0.45 s between dashes",
            gaps.Count >= 2 && Math.Abs(median - 0.45f) <= 0.09f);
        Metric("david interval gaps=" + gaps.Count + " median=" + median.ToString("F3") + " requested=0.450");

        _console.Submit("david 1");
        Check("a later david 1 without an interval returns to 0.2 s",
            Math.Abs(DavidMode.PauseSeconds - DavidMode.DefaultPauseSeconds) < 1e-6f);
        _console.Submit("david 0");
        yield return Frames(2);
    }

    /// <summary>`david` must cover apparatus and test items, not only vessels.</summary>
    IEnumerator DragApparatusWithDavid() {
        // A dedicated burner on a clear part of the bench: the seeded apparatus can
        // sit under a HUD panel, where no pick point is reachable.
        var burner = ApparatusSet.Add(ApparatusKind.BunsenBurner, new Vector3(0.35f, 0f, -0.62f));
        _lab.SyncViews();
        yield return Frames(4);
        _console.Submit("david 1");
        _david.TestClearSegmentLog();
        Vector2 grab = FindPickPoint(h => h.apparatus == burner, burner);
        if (grab.x < 0) {
            // Some bench slots sit behind a HUD panel; move the fixture, not the test.
            burner.Pos = new Vector3(0f, 0f, -1.0f);
            _lab.SyncViews();
            yield return Frames(4);
            grab = FindPickPoint(h => h.apparatus == burner, burner);
        }
        Check("david apparatus is pickable", grab.x >= 0);
        Metric("david apparatus grab=" + grab.x + "," + grab.y + " at=" + burner.Pos.x.ToString("F2") + "," + burner.Pos.z.ToString("F2"));
        if (grab.x < 0) yield break;
        Vector3 start = burner.Pos;
        Inject("MOVE " + (int)grab.x + " " + (int)grab.y);
        yield return Frames(2);
        Inject("DOWN 0");
        yield return Frames(2);
        bool dashed = false, ghosts = false;
        for (int i = 0; i < 70; i++) {
            Inject("MOVE " + (int)(grab.x + Mathf.Sin(i * 0.11f) * 120f) + " " + (int)(grab.y - i * 1.4f));
            yield return null;
            dashed |= _david.Active && _david.OffsetMagnitudeForTest > 0.05f;
            ghosts |= _david.LiveGhostCountForTest > 0;
        }
        Check("david dashes a dragged apparatus too", dashed);
        Check("david leaves afterimages for apparatus", ghosts);
        Inject("UP 0");
        int waited = 0;
        while (_david.Active && waited < 120) { yield return null; waited++; }
        Check("david releases an apparatus onto the bench", !_david.Active
            && Vector2.Distance(new Vector2(burner.Pos.x, burner.Pos.z), new Vector2(start.x, start.z)) > 0.05f);
        Check("david cleans apparatus afterimages", _david.LiveGhostCountForTest == 0 && ActiveAfterimages() == 0);
        _console.Submit("david 0");
        ApparatusSet.All.Remove(burner);
        _lab.SyncViews();
        yield return Frames(2);
    }

    struct GhostObservation {
        public bool HasGlass, HasLiquid, HasSediment;
        public int LiquidVertices, SolidVertices;
        public bool LiquidVertexMatch, SolidVertexMatch;
        public string Material;
        public float OutlineAlpha;
        public bool FrozenLiquidField;
    }

    /// <summary>Inspects the afterimage of a segment: it must mirror the live instrument.</summary>
    GhostObservation ObserveGhost(VesselView live) {
        var observation = new GhostObservation { Material = "-" };
        var driver = _david.transform;
        Transform root = null;
        for (int i = 0; i < driver.childCount; i++) {
            var child = driver.GetChild(i);
            if (child.name == "DavidAfterimage" && child.gameObject.activeSelf && child.Find("Glass") != null) {
                root = child;
                break;
            }
        }
        if (root == null) return observation;
        observation.HasGlass = true;
        var liquid = root.Find("Liquid");
        var sediment = root.Find("Sediment");
        observation.HasLiquid = liquid != null;
        observation.HasSediment = sediment != null;
        if (liquid != null) {
            var ghostMesh = liquid.GetComponent<MeshFilter>().sharedMesh;
            var liveMesh = live != null && live.transform.Find("Liquid") != null
                ? live.transform.Find("Liquid").GetComponent<MeshFilter>().sharedMesh : null;
            observation.LiquidVertices = ghostMesh != null ? ghostMesh.vertexCount : 0;
            observation.LiquidVertexMatch = ghostMesh != null && liveMesh != null
                && ghostMesh.vertexCount == liveMesh.vertexCount
                && (ghostMesh.bounds.size - liveMesh.bounds.size).magnitude < 5e-3f;
            var renderer = liquid.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null) {
                observation.Material = renderer.sharedMaterial.shader.name;
                var original = live.transform.Find("Liquid").GetComponent<MeshRenderer>().sharedMaterial;
                observation.FrozenLiquidField = renderer.sharedMaterial.GetTexture("_FluidDensity") != null
                    && renderer.sharedMaterial.GetTexture("_FluidDensity") != original.GetTexture("_FluidDensity");
                if (renderer.sharedMaterial.HasProperty("_OutlineColor"))
                    observation.OutlineAlpha = renderer.sharedMaterial.GetColor("_OutlineColor").a;
            }
        }
        if (sediment != null) {
            var ghostMesh = sediment.GetComponent<MeshFilter>().sharedMesh;
            var liveMesh = live != null && live.transform.Find("Sediment") != null
                ? live.transform.Find("Sediment").GetComponent<MeshFilter>().sharedMesh : null;
            observation.SolidVertices = ghostMesh != null ? ghostMesh.vertexCount : 0;
            observation.SolidVertexMatch = ghostMesh != null && liveMesh != null
                && ghostMesh.vertexCount == liveMesh.vertexCount;
        }
        return observation;
    }

    /* ================================================================ */
    /* helpers                                                           */
    /* ================================================================ */

    int ActiveAfterimages() {
        var driver = _david != null ? _david.transform : null;
        if (driver == null) return 0;
        int count = 0;
        for (int i = 0; i < driver.childCount; i++) {
            var child = driver.GetChild(i);
            if (child.name == "DavidAfterimage" && child.gameObject.activeSelf) count++;
        }
        return count;
    }

    bool AfterimagesCarryNothingButMeshes() {
        var driver = _david != null ? _david.transform : null;
        if (driver == null) return false;
        int seen = 0;
        for (int i = 0; i < driver.childCount; i++) {
            var child = driver.GetChild(i);
            if (child.name != "DavidAfterimage") continue;
            seen++;
            if (child.GetComponentsInChildren<VesselView>(true).Length > 0) return false;
            if (child.GetComponentsInChildren<Collider>(true).Length > 0) return false;
            if (child.GetComponentsInChildren<Rigidbody>(true).Length > 0) return false;
            if (child.GetComponentsInChildren<DistillationView>(true).Length > 0) return false;
        }
        return seen > 0;
    }

    int ComponentsNow() {
        int total = 0;
        foreach (var vessel in Lab.Vessels)
            total += vessel.Solutes.Count + vessel.Undissolved.Count + vessel.Precipitates.Count + vessel.Gases.Count;
        return total;
    }

    Vessel MakeVessel(VesselKind kind, Vector3 position, double waterMl, bool ice, float temp = 25f) {
        var vessel = Lab.AddVessel(kind);
        vessel.Pos = position;
        vessel.LiftY = 0f;
        vessel.Water = waterMl;
        // Ice only exists as a solid below its melting point; a warm vessel would
        // have melted it before the solver ever saw it.
        vessel.Temp = ice ? Mathf.Min(temp, 0f) : temp;
        if (ice) vessel.Undissolved[WaterIce.Id] = 0.05;
        _lab.SyncViews();
        return vessel;
    }

    VesselView ViewFor(Vessel vessel) {
        foreach (var view in UnityEngine.Object.FindObjectsOfType<VesselView>())
            if (view != null && view.Model == vessel) return view;
        return null;
    }

    static bool PositionsEqual(Vector3[] a, Vector3[] b) {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    /// <summary>Bench-plane point under a screen position (the drag plane at y=0).</summary>
    Vector3 PickPlanePoint(Vector2 screen, float height) {
        var ray = _cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, Vector3.up * height);
        return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : Vector3.zero;
    }

    static float HorizontalExtent(VesselView view) {        if (view == null) return 0.5f;
        bool any = false;
        var bounds = new Bounds();
        foreach (var renderer in view.GetComponentsInChildren<MeshRenderer>(true)) {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!any) { bounds = renderer.bounds; any = true; } else bounds.Encapsulate(renderer.bounds);
        }
        return any ? Mathf.Max(bounds.size.x, bounds.size.z) : 0.5f;
    }

    /// <summary>Screen point that picks this object, is not covered by UI, and is in range.</summary>
    Vector2 FindPickPoint(Func<LabPick.Hit, bool> match, Vessel vessel) {
        var view = ViewFor(vessel);
        return view == null ? new Vector2(-1, -1)
            : FindPickPointFrom(view.GetComponentsInChildren<Collider>(), match);
    }

    Vector2 FindPickPoint(Func<LabPick.Hit, bool> match, Apparatus apparatus) {
        var view = UnityEngine.Object.FindObjectsOfType<ApparatusView>()
            .FirstOrDefault(candidate => candidate != null && candidate.Model == apparatus);
        return view == null ? new Vector2(-1, -1)
            : FindPickPointFrom(view.GetComponentsInChildren<Collider>(), match);
    }

    Vector2 FindPickPointFrom(Collider[] colliders, Func<LabPick.Hit, bool> match) {
        if (_cam == null || EventSystem.current == null) return new Vector2(-1, -1);
        foreach (var collider in colliders) {
            Vector2 seed = _cam.WorldToScreenPoint(collider.bounds.center);
            for (int radius = 0; radius <= 84; radius += 6) {
                int samples = radius == 0 ? 1 : Mathf.Max(12, Mathf.CeilToInt(2f * Mathf.PI * radius / 10f));
                for (int i = 0; i < samples; i++) {
                    float angle = 2f * Mathf.PI * i / samples;
                    var point = new Vector2(Mathf.Round(seed.x + radius * Mathf.Cos(angle)),
                                            Mathf.Round(seed.y + radius * Mathf.Sin(angle)));
                    if (point.x < 4 || point.y < 4 || point.x > Screen.width - 5 || point.y > Screen.height - 5) continue;
                    if (!match(LabPick.Pick(point, _cam))) continue;
                    var pointer = new PointerEventData(EventSystem.current) { position = point };
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointer, hits);
                    if (hits.Count > 0) continue;
                    return point;
                }
            }
        }
        return new Vector2(-1, -1);
    }
}
}
