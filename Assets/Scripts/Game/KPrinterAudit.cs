using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChemLab.Chem;

namespace ChemLab.Game {

/// <summary>Opt-in hidden-player acceptance. Pointer events are injected into
/// WindowTargetInput inside this process; no desktop keyboard, cursor or focus.</summary>
public sealed class KPrinterAudit : MonoBehaviour {
    readonly List<string> _lines = new List<string>();
    int _passed, _failed;
    LabBootstrap _lab;
    KPrinter _printer;
    LabUI _ui;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch() {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-kprinterTest") >= 0)
            new GameObject("KPrinter acceptance").AddComponent<KPrinterAudit>();
    }

    IEnumerator Start() {
        yield return new WaitForSecondsRealtime(2);
        var routine = Run();
        while (true) {
            bool next;
            try { next = routine.MoveNext(); }
            catch (Exception error) { Debug.LogException(error); Check("audit runs without exception", false); break; }
            if (!next) break;
            yield return routine.Current;
        }
        _lines.Add($"TOTAL {_passed} passed, {_failed} failed");
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/kprinter-audit.txt", _lines);
        Debug.Log("[KPrinterAudit] " + _lines.Last());
        Application.Quit(_failed == 0 ? 0 : 1);
    }

    void Check(string label, bool condition) {
        if (condition) _passed++; else _failed++;
        string line = (condition ? "PASS " : "FAIL ") + label;
        _lines.Add(line); Debug.Log("[KPrinterAudit] " + line);
    }

    IEnumerator Click(string name) {
        var button = FindObjectsOfType<Button>().First(b => b.name == name);
        var rt = button.GetComponent<RectTransform>();
        Vector2 point = rt.TransformPoint(rt.rect.center);
        WindowTargetInput.InjectForTest(FormattableString.Invariant($"MOVE {point.x:F0} {point.y:F0}"));
        yield return null; yield return null;
        WindowTargetInput.InjectForTest("DOWN 0");
        yield return null; yield return null;
        WindowTargetInput.InjectForTest("UP 0");
        yield return null; yield return null;
    }

    IEnumerator WaitForPrint() {
        float deadline = Time.realtimeSinceStartup + 6;
        while (_printer.Busy && Time.realtimeSinceStartup < deadline) yield return null;
        Check("print reaches completion", !_printer.Busy);
    }

    bool AtStation(Vessel vessel) => Vector3.Distance(_lab.transform.TransformPoint(vessel.Pos), KPrinter.PrintPosition) < .001f;

    IEnumerator DragAway(Vessel vessel) {
        var view = _lab.FindVesselView(vessel);
        Vector2 grab = Vector2.zero; bool found = false;
        var hits = new List<RaycastResult>();
        // Find a real, unoccluded shell pixel outside all UI overlays.
        for (float y = .15f; y < .85f && !found; y += .1f) {
            for (float x = -.2f; x <= .2f; x += .05f) {
                Vector2 point = _lab.Cam.WorldToScreenPoint(view.transform.position + Vector3.up * y + _lab.Cam.transform.right * x);
                point = new Vector2(Mathf.Round(point.x), Mathf.Round(point.y));
                hits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
                if (hits.Count == 0 && LabPick.Pick(point, _lab.Cam).vessel == vessel) { grab = point; found = true; break; }
            }
        }
        Check("finished instrument has a pickable shell pixel", found);
        if (!found) yield break;
        var plane = new Plane(Vector3.up, Vector3.zero);
        var ray = _lab.Cam.ScreenPointToRay(grab);
        plane.Raycast(ray, out float along);
        var offset = ray.GetPoint(along) - vessel.Pos;
        // Choose a free patch of bench which is visible between the shelves.
        // Otherwise a legitimate UI drop cancellation would test the wrong path.
        var target = vessel.Pos; Vector2 drop = grab; bool dropFound = false;
        for (int i = 0; i < 24; i++) {
            float angle = i * Mathf.PI / 12;
            var candidate = vessel.Pos + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 1.8f;
            if (Lab.Vessels.Any(v => v != vessel && Vector3.Distance(candidate, v.Pos) < 1)) continue;
            Vector2 pixel = _lab.Cam.WorldToScreenPoint(candidate + offset);
            if (!_ui.TagWorkspaceRect().Contains(pixel)) continue;
            hits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pixel }, hits);
            if (hits.Count > 0) continue;
            target = candidate; drop = pixel; dropFound = true; break;
        }
        Check("drag destination is a visible free bench patch", dropFound);
        if (!dropFound) yield break;
        WindowTargetInput.InjectForTest(FormattableString.Invariant($"MOVE {grab.x:F0} {grab.y:F0}"));
        yield return null; yield return null;
        WindowTargetInput.InjectForTest("DOWN 0");
        yield return null; yield return null;
        for (int step = 1; step <= 12; step++) {
            var point = Vector2.Lerp(grab, drop, step / 12f);
            WindowTargetInput.InjectForTest(FormattableString.Invariant($"MOVE {point.x:F0} {point.y:F0}"));
            yield return null; yield return null;
        }
        WindowTargetInput.InjectForTest("UP 0");
        yield return null; yield return null;
        Check("process-injected drag moves completed instrument off the pad", Vector3.Distance(vessel.Pos, target) < .1f && !AtStation(vessel));
    }

    IEnumerator Run() {
        _lab = LabBootstrap.Instance;
        _printer = _lab.Printer;
        _ui = FindObjectOfType<LabUI>();
        Check("mode defaults to 1", KPrinterMode.Enabled);
        Check("process-local input is active", WindowTargetInput.Active);
        var console = _lab.Console;
        foreach (string value in new[] { "", "2", "-1", "0.5", "NaN", "1 0", "yes" }) {
            console.Submit("kprinter " + value);
            Check("invalid command preserves enabled mode: " + value,
                KPrinterMode.Enabled && console.GetHistorySnapshot().Last().StartsWith("[ERR]"));
        }
        WorldClock.TrySet("0", out _);
        int count = Lab.Vessels.Count;
        yield return Click("Btn_烧杯");
        var first = Lab.Vessels.Last();
        Check("rack click starts construction at fixed world position", _printer.Busy && Lab.Vessels.Count == count + 1 && AtStation(first));
        Check("projection has shell geometry and dense surface points", _printer.HologramParts > 0 && _printer.SurfacePointCount > 100);
        var view = _lab.FindVesselView(first);
        Check("printing instrument is framed inside visible workspace", _ui.TagWorkspaceRect().Contains(
            _lab.Cam.WorldToScreenPoint(view.transform.position + Vector3.up * .45f)));
        Check("unfinished instrument cannot be picked", view.GetComponentsInChildren<Collider>().All(c => !c.enabled));
        _ui.SelectExternal(first);
        Check("unfinished instrument cannot be selected", _ui.SelectedVessel != first);
        yield return Click("Btn_锥形瓶");
        Check("rapid second click refuses concurrent construction", Lab.Vessels.Count == count + 1);
        float previous = _printer.RevealHeight;
        while (_printer.Busy && _printer.Progress < .43f) yield return null;
        Check("solidification advances from bottom upward while chemistry is frozen", _printer.Busy && _printer.RevealHeight > previous && WorldClock.Frozen);
        WindowTargetInput.InjectForTest("CAPTURE kprinter-mid");
        yield return null; yield return null;
        while (_printer.Busy && _printer.Progress < .84f) yield return null;
        WindowTargetInput.InjectForTest("CAPTURE kprinter-sweep");
        yield return null; yield return null;
        yield return WaitForPrint();
        Check("completion restores colliders and selection", view.GetComponentsInChildren<Collider>().All(c => c.enabled) && _ui.SelectedVessel == first);
        Check("completion removes every hologram and photon", _printer.HologramParts == 0 && _printer.SurfacePointCount == 0 && AtStation(first));
        WindowTargetInput.InjectForTest("CAPTURE kprinter-finished");
        yield return null; yield return null;
        yield return Click("Btn_锥形瓶");
        Check("occupied pad refuses another rack item", !_printer.Busy && Lab.Vessels.Count == count + 1);
        Check("refusal explains that existing instrument must be moved", Lab.Log.Last().Text.Contains("挪开"));
        yield return DragAway(first);
        yield return null; yield return null;
        yield return Click("Btn_锥形瓶");
        var second = Lab.Vessels.Last();
        Check("moving finished instrument frees the same fixed position", _printer.Busy && AtStation(second));
        console.Submit("kprinter 0");
        Check("switching off preserves in-flight construction", !KPrinterMode.Enabled && _printer.Busy);
        yield return WaitForPrint();
        count = Lab.Vessels.Count;
        yield return Click("Btn_试管");
        var legacy = Lab.Vessels.Last();
        Check("mode 0 uses immediate original placement even with occupied pad", !_printer.Busy && Lab.Vessels.Count == count + 1 && !AtStation(legacy) && _ui.SelectedVessel == legacy);
        console.Submit("kprinter 1");
        Lab.RemoveVessel(second);
        yield return null; yield return null;
        yield return Click("Btn_量筒");
        var removed = Lab.Vessels.Last();
        Check("removal also frees the printing area", _printer.Busy && AtStation(removed));
        Lab.RemoveVessel(removed);
        yield return null; yield return null;
        Check("deleting unfinished instrument clears the station", !_printer.Busy && _printer.HologramParts == 0);

        // Every shelf vessel must finish with native materials, not a blue clone.
        foreach (VesselKind kind in Enum.GetValues(typeof(VesselKind))) {
            Vessel result = null;
            bool accepted = _printer.Request(kind, v => result = v);
            Check("can print " + kind, accepted && _printer.Busy);
            if (!accepted) continue;
            if (kind == VesselKind.Burette) {
                while (_printer.Busy && _printer.Progress < .42f) yield return null;
                WindowTargetInput.InjectForTest("CAPTURE kprinter-burette");
                yield return null; yield return null;
            }
            yield return WaitForPrint();
            Check("native geometry completed for " + kind, result != null && AtStation(result) &&
                _lab.FindVesselView(result).GetComponentsInChildren<Renderer>().All(r => r.sharedMaterial == null || r.sharedMaterial.shader.name != "ChemLab/KPrinterHologram"));
            Lab.RemoveVessel(result);
            yield return null; yield return null;
        }
        first.Pos = _lab.transform.InverseTransformPoint(KPrinter.PrintPosition + Vector3.right * 3.2f);
        yield return null; yield return null;
        count = Lab.Vessels.Count;
        Check("distillation receiver corridor rejects an existing instrument", !_printer.RequestKit(false, null) &&
            !_printer.Busy && Lab.Vessels.Count == count);
        first.Pos = _lab.transform.InverseTransformPoint(KPrinter.PrintPosition + Vector3.left * 4);
        yield return null; yield return null;
        foreach (bool reflux in new[] { true, false }) {
            Vessel kit = null;
            bool accepted = _printer.RequestKit(reflux, v => kit = v);
            Check("rack assembly starts printing: reflux=" + reflux, accepted && _printer.Busy);
            if (!accepted) continue;
            while (_printer.Busy && _printer.Progress < .4f) yield return null;
            WindowTargetInput.InjectForTest("CAPTURE kprinter-kit-" + (reflux ? "reflux" : "distillation"));
            yield return null; yield return null;
            yield return WaitForPrint();
            Check("assembly retains native connected instrument", kit != null && kit.Distillation != null && AtStation(kit));
            var receiver = kit?.Distillation?.Receiver;
            Lab.RemoveVessel(kit);
            if (receiver != null && receiver != kit) Lab.RemoveVessel(receiver);
            yield return null; yield return null;
        }
        Check("no projection objects remain", FindObjectsOfType<ParticleSystem>().All(p => p.name != "Surface printing photons"));
        WorldClock.TrySet("1", out _);
    }
}
}
