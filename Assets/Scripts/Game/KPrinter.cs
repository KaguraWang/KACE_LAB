using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ChemLab.Chem;

namespace ChemLab.Game {

public static class KPrinterMode {
    public static bool Enabled { get; private set; } = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Enabled = true; }

    public static bool TrySet(string argument, out string message) {
        string value = (argument ?? "").Trim();
        if (value != "0" && value != "1") {
            message = "[ERR] kprinter 只能设为 0 或 1，例如：kprinter 1";
            return false;
        }
        Enabled = value == "1";
        message = Enabled ? "kprinter = 1：固定位置全息打印已启用" : "kprinter = 0：使用原来的即时生成方式";
        return true;
    }
}

/// <summary>A process-local construction station. Timing is presentation time,
/// so world_speed 0 does not strand an unfinished instrument.</summary>
public sealed class KPrinter : MonoBehaviour {
    public static readonly Vector3 PrintPosition = new Vector3(0f, 0f, -1.65f);
    public const float Duration = 2.25f;
    public bool Busy => _members.Count > 0;
    public float Progress { get; private set; }
    public float RevealHeight { get; private set; }
    public int HologramParts => _ghosts.Count;
    public int SurfacePointCount => _points.Length;
    public Vessel PrintingVessel => Busy ? _members[0] : null;

    readonly List<Vessel> _members = new List<Vessel>();
    readonly List<Renderer> _surfaces = new List<Renderer>();
    readonly List<MaterialPropertyBlock> _originalBlocks = new List<MaterialPropertyBlock>();
    readonly List<ShadowCastingMode> _shadows = new List<ShadowCastingMode>();
    readonly List<Collider> _colliders = new List<Collider>();
    readonly List<GameObject> _ghosts = new List<GameObject>();
    readonly List<Vector3> _samples = new List<Vector3>();
    readonly List<Mesh> _ownedMeshes = new List<Mesh>();
    readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
    LabBootstrap _lab;
    GameObject _device, _projection;
    Material _hologram, _light, _housing;
    Renderer _locator, _scan;
    ParticleSystem _particles;
    ParticleSystem.Particle[] _points = Array.Empty<ParticleSystem.Particle>();
    Light _lamp;
    float _elapsed, _bottom, _top;
    Action<Vessel> _completed;

    public bool IsPrinting(Vessel vessel) => vessel != null && _members.Contains(vessel);

    public static KPrinter Create(LabBootstrap lab) {
        var go = new GameObject("Portable KPrinter");
        go.transform.SetParent(lab.transform, false);
        // The station is anchored in world space, independently of the camera.
        go.transform.position = PrintPosition;
        var printer = go.AddComponent<KPrinter>();
        printer._lab = lab;
        printer.BuildDevice();
        return printer;
    }

    public bool Request(VesselKind kind, Action<Vessel> completed) {
        if (!KPrinterMode.Enabled) { completed?.Invoke(Lab.AddVessel(kind)); return true; }
        if (!CanPrint(false)) return false;
        Begin(() => {
            var vessel = Lab.AddVessel(kind);
            vessel.Pos = _lab.transform.InverseTransformPoint(PrintPosition);
            return vessel;
        }, completed);
        return true;
    }

    public bool RequestKit(bool reflux, Action<Vessel> completed) {
        if (!KPrinterMode.Enabled) { DistillationView.CreateKit(reflux); return true; }
        if (!CanPrint(!reflux)) return false;
        Begin(() => {
            var vessel = Lab.AddVessel(VesselKind.RoundBottom);
            vessel.Pos = _lab.transform.InverseTransformPoint(PrintPosition);
            DistillationView.Install(vessel, reflux);
            return vessel;
        }, completed);
        return true;
    }

    bool CanPrint(bool wide) {
        if (Busy) return Refuse("建造装置正在打印，请等待当前仪器完成。");
        // Reserve the pad and, for distillation, the receiver/condenser corridor.
        var area = new Rect(PrintPosition.x - .70f, PrintPosition.z - .70f, wide ? 4.6f : 1.4f, 1.4f);
        Physics.SyncTransforms();
        foreach (var vessel in Lab.Vessels) {
            var view = _lab.FindVesselView(vessel);
            if (view != null) {
                // Smoke, bubbles and painted labels are not occupied bench space.
                foreach (var collider in view.GetComponentsInChildren<Collider>())
                    if (collider.enabled && Overlaps(area, collider.bounds))
                        return Refuse("打印区已有仪器，请先把已有的仪器挪开，再生成新的仪器。");
            } else {
                var g = VesselMesh.Get(vessel.Kind);
                var p = _lab.transform.TransformPoint(vessel.Pos);
                if (Overlaps(area, new Bounds(p, new Vector3(g.R * 2, g.H, g.R * 2))))
                    return Refuse("打印区已有仪器，请先把已有的仪器挪开，再生成新的仪器。");
            }
        }
        foreach (var apparatus in ApparatusSet.All) {
            var p = _lab.transform.TransformPoint(apparatus.Pos);
            float width = apparatus.Kind == ApparatusKind.Tripod ? 1.2f : .65f;
            if (Overlaps(area, new Bounds(p, new Vector3(width, 1, width))))
                return Refuse("打印区已有仪器，请先把已有的仪器挪开，再生成新的仪器。");
        }
        foreach (var item in TestSet.All) {
            var p = _lab.transform.TransformPoint(item.Pos);
            if (Overlaps(area, new Bounds(p, new Vector3(.30f, 1, .30f))))
                return Refuse("打印区已有物品，请先把它挪开，再生成新的仪器。");
        }
        return true;
    }

    static bool Overlaps(Rect area, Bounds bounds) => area.Overlaps(
        Rect.MinMaxRect(bounds.min.x - .04f, bounds.min.z - .04f, bounds.max.x + .04f, bounds.max.z + .04f));

    bool Refuse(string message) {
        Lab.Note(null, message, "obs");
        _lab.ShowHint(message);
        return false;
    }

    void Begin(Func<Vessel> factory, Action<Vessel> completed) {
        var vessel = factory();
        _members.Add(vessel);
        if (vessel.Distillation?.Receiver != null && vessel.Distillation.Receiver != vessel)
            _members.Add(vessel.Distillation.Receiver);
        _completed = completed;
        _lab.SyncViews();
        _elapsed = 0;
        Progress = 0;
        _bottom = PrintPosition.y;
        _top = _bottom + VesselMesh.Get(vessel.Kind).H;
        _projection = new GameObject("KPrinter holographic projection");
        _projection.transform.SetParent(transform, false);
        foreach (var member in _members) Capture(_lab.FindVesselView(member));
        _top += .03f;
        var bounds = new Bounds(PrintPosition + Vector3.up * .1f, new Vector3(2.1f, .2f, 1.5f));
        foreach (var surface in _surfaces) bounds.Encapsulate(surface.bounds);
        _lab.FrameConstruction(bounds);
        BuildPoints();
        _locator.enabled = _scan.enabled = true;
        _lamp.enabled = true;
        ApplyFrame(0);
        Lab.Note(null, "便携式建造装置启动：正在打印" + VesselNames.Name(vessel.Kind), "setup");
    }

    void Capture(VesselView view) {
        foreach (var collider in view.GetComponentsInChildren<Collider>()) {
            if (!collider.enabled) continue;
            _colliders.Add(collider);
            collider.enabled = false;
        }
        foreach (var renderer in view.GetComponentsInChildren<Renderer>()) {
            if (!renderer.enabled || !(renderer is MeshRenderer)) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            var mesh = filter != null ? filter.sharedMesh : null;
            // Empty liquid renderers and inactive feedback are not instrument surfaces.
            if (renderer.name == "Liquid" || renderer.name == "Glow") continue;
            _surfaces.Add(renderer);
            var original = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(original);
            _originalBlocks.Add(original);
            _shadows.Add(renderer.shadowCastingMode);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            _top = Mathf.Max(_top, renderer.bounds.max.y);
            // Keep painted scales/decals in the native reveal only: their
            // texture-backed rectangular patches are not physical shell geometry.
            if (mesh == null || mesh.vertexCount == 0 || renderer.name.StartsWith("Scale_") ||
                renderer.name == "Graduations" || renderer.GetComponent<GlasswareBranding>() != null) continue;
            var ghost = new GameObject("Hologram " + renderer.name);
            ghost.transform.SetParent(_projection.transform, false);
            ghost.transform.position = renderer.transform.position;
            ghost.transform.rotation = renderer.transform.rotation;
            ghost.transform.localScale = renderer.transform.lossyScale;
            ghost.AddComponent<MeshFilter>().sharedMesh = mesh;
            var copy = ghost.AddComponent<MeshRenderer>();
            var materials = new Material[mesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = _hologram;
            copy.sharedMaterials = materials;
            copy.shadowCastingMode = ShadowCastingMode.Off;
            copy.receiveShadows = false;
            _ghosts.Add(ghost);
            // Uniform triangle-area sampling keeps dense dots on the actual shell,
            // rather than on an unrelated bounding cylinder or on font glyphs.
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var random = new System.Random(137 + _surfaces.Count);
            int partSamples = 0;
            for (int i = 0; i + 2 < triangles.Length && _samples.Count < 2600; i += 3) {
                var a = renderer.transform.TransformPoint(vertices[triangles[i]]);
                var b = renderer.transform.TransformPoint(vertices[triangles[i + 1]]);
                var c = renderer.transform.TransformPoint(vertices[triangles[i + 2]]);
                float area = Vector3.Cross(b - a, c - a).magnitude * .5f;
                int count = Mathf.FloorToInt(area * 220);
                if (random.NextDouble() < area * 220 - count) count++;
                for (int j = 0; j < count && _samples.Count < 2600 && partSamples < 600; j++) {
                    float u = Mathf.Sqrt((float)random.NextDouble()), v = (float)random.NextDouble();
                    _samples.Add((1 - u) * a + u * (1 - v) * b + u * v * c);
                    partSamples++;
                }
            }
        }
    }

    void BuildPoints() {
        var go = new GameObject("Surface printing photons");
        go.transform.SetParent(_projection.transform, false);
        _particles = go.AddComponent<ParticleSystem>();
        _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = _particles.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 2600;
        var emission = _particles.emission; emission.enabled = false;
        var shape = _particles.shape; shape.enabled = false;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = _light;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        _points = new ParticleSystem.Particle[_samples.Count];
    }

    void Update() {
        transform.position = PrintPosition;
        _device.SetActive(KPrinterMode.Enabled || Busy);
        if (!Busy) return;
        foreach (var member in _members) {
            if (!Lab.Vessels.Contains(member)) { Finish(false); return; }
        }
        _elapsed += Time.unscaledDeltaTime;
        ApplyFrame(_elapsed);
        if (_elapsed >= Duration) Finish(true);
    }

    void ApplyFrame(float seconds) {
        Progress = Mathf.Clamp01(seconds / Duration);
        float build = Mathf.Clamp01((seconds - .28f) / 1.5f);
        RevealHeight = Mathf.Lerp(_bottom - .025f, _top + .025f, build);
        float sweep = seconds < 1.78f ? build : Mathf.Clamp01((seconds - 1.78f) / .30f);
        float scanHeight = Mathf.Lerp(_bottom, _top, sweep);
        float fade = 1 - Mathf.Clamp01((seconds - 2.03f) / .22f);
        float flicker = seconds < 2.03f ? 1 : .65f + .35f * Mathf.Sin(seconds * 173);
        _hologram.SetFloat("_RevealY", RevealHeight);
        _hologram.SetFloat("_ScanY", scanHeight);
        _hologram.SetFloat("_Phase", seconds);
        _hologram.SetFloat("_Fade", fade * flicker * Mathf.Clamp01(seconds / .10f));
        for (int i = 0; i < _surfaces.Count; i++) {
            if (_surfaces[i] == null) continue;
            _surfaces[i].GetPropertyBlock(_block);
            _block.SetFloat("_KPrinterActive", 1);
            _block.SetFloat("_KPrinterHeight", RevealHeight);
            _surfaces[i].SetPropertyBlock(_block);
        }
        _scan.transform.position = new Vector3(PrintPosition.x, scanHeight, PrintPosition.z);
        _scan.transform.localScale = Vector3.one * (1 + .045f * Mathf.Sin(seconds * 24));
        _lamp.intensity = fade * (.7f + .12f * Mathf.Sin(seconds * 29));
        _light.SetColor("_Color", new Color(.035f, .58f, 1f, fade));
        for (int i = 0; i < _points.Length; i++) {
            var p = _samples[i];
            float pulse = .5f + .5f * Mathf.Sin(i * 4.13f + seconds * 33);
            float band = Mathf.Exp(-Mathf.Abs(p.y - scanHeight) * 18);
            float visible = p.y > RevealHeight - .08f ? 1 : .12f;
            _points[i].position = p + Vector3.up * (.015f * Mathf.Sin(seconds * 27 + i));
            _points[i].startSize = .006f + band * .009f;
            _points[i].startColor = new Color(.05f, .45f + band * .35f, 1, fade * visible * (pulse * .5f + band * .5f));
            _points[i].startLifetime = _points[i].remainingLifetime = 10;
        }
        _particles.SetParticles(_points, _points.Length);
    }

    void Finish(bool success) {
        var vessel = PrintingVessel;
        for (int i = 0; i < _surfaces.Count; i++) {
            if (_surfaces[i] == null) continue;
            _surfaces[i].SetPropertyBlock(_originalBlocks[i]);
            _surfaces[i].shadowCastingMode = _shadows[i];
        }
        foreach (var collider in _colliders) if (collider != null) collider.enabled = true;
        if (_projection != null) { _projection.SetActive(false); Destroy(_projection); }
        _locator.enabled = _scan.enabled = _lamp.enabled = false;
        _light.SetColor("_Color", new Color(.035f, .58f, 1f, .25f));
        _members.Clear(); _surfaces.Clear(); _originalBlocks.Clear(); _shadows.Clear();
        _colliders.Clear(); _ghosts.Clear(); _samples.Clear();
        _points = Array.Empty<ParticleSystem.Particle>();
        var callback = _completed; _completed = null;
        if (success) {
            Lab.Note(vessel, "打印完成，请将仪器挪离打印区后再生成下一件。", "setup");
            callback?.Invoke(vessel);
        }
    }

    void BuildDevice() {
        _hologram = new Material(Resources.Load<Shader>("ChemLab/KPrinterHologram"));
        _light = new Material(Resources.Load<Shader>("ChemLab/KPrinterLight"));
        _housing = new Material(Resources.Load<Material>("ChemLab/Metal"));
        _housing.SetColor("_Color", new Color(.07f, .085f, .10f));
        _device = new GameObject("Portable construction projector");
        _device.transform.SetParent(transform, false);
        Part("Projector housing", PrimitiveType.Cube, new Vector3(-.88f, .085f, .05f), new Vector3(.36f, .17f, .55f), _housing);
        Part("Projector lens", PrimitiveType.Cylinder, new Vector3(-.88f, .18f, .05f), new Vector3(.15f, .025f, .15f), _light);
        for (int i = 0; i < 3; i++) {
            float angle = i * Mathf.PI * 2 / 3;
            var p = new Vector3(Mathf.Cos(angle) * .67f, .026f, Mathf.Sin(angle) * .67f);
            Part("Emitter foot", PrimitiveType.Cylinder, p, new Vector3(.11f, .026f, .11f), _housing);
            Part("Emitter aperture", PrimitiveType.Cylinder, p + Vector3.up * .031f, new Vector3(.045f, .005f, .045f), _light);
        }
        _locator = Ring("Cyan positioning reticle", .58f, .595f, .007f);
        _scan = Ring("Horizontal blue scanning band", .61f, .625f, 0);
        _locator.enabled = _scan.enabled = false;
        var lamp = new GameObject("Printing bounce light"); lamp.transform.SetParent(_device.transform, false);
        lamp.transform.localPosition = new Vector3(0, .28f, 0);
        _lamp = lamp.AddComponent<Light>(); _lamp.type = LightType.Point;
        _lamp.color = new Color(.04f, .6f, 1); _lamp.range = 2.4f; _lamp.shadows = LightShadows.None;
        _lamp.enabled = false;
        _light.SetColor("_Color", new Color(.035f, .58f, 1f, .65f));
    }

    void Part(string name, PrimitiveType type, Vector3 position, Vector3 size, Material material) {
        var go = GameObject.CreatePrimitive(type); go.name = name;
        go.transform.SetParent(_device.transform, false);
        go.transform.localPosition = position; go.transform.localScale = size;
        var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
        go.GetComponent<Renderer>().sharedMaterial = material;
    }

    Renderer Ring(string name, float inner, float outer, float height) {
        var vertices = new Vector3[194]; var triangles = new int[96 * 6];
        for (int i = 0; i <= 96; i++) {
            float a = i * Mathf.PI * 2 / 96;
            vertices[i * 2] = new Vector3(Mathf.Cos(a) * inner, 0, Mathf.Sin(a) * inner);
            vertices[i * 2 + 1] = new Vector3(Mathf.Cos(a) * outer, 0, Mathf.Sin(a) * outer);
            if (i == 96) continue;
            int n = i * 2, t = i * 6;
            triangles[t] = n; triangles[t + 1] = n + 2; triangles[t + 2] = n + 1;
            triangles[t + 3] = n + 1; triangles[t + 4] = n + 2; triangles[t + 5] = n + 3;
        }
        var mesh = new Mesh { name = name }; mesh.vertices = vertices; mesh.triangles = triangles;
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); _ownedMeshes.Add(mesh);
        var go = new GameObject(name); go.transform.SetParent(_device.transform, false);
        go.transform.localPosition = Vector3.up * height;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = _light;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        return renderer;
    }

    void OnDisable() { if (Busy) Finish(false); }
    void OnDestroy() {
        foreach (var mesh in _ownedMeshes) if (mesh != null) Destroy(mesh);
        if (_hologram != null) Destroy(_hologram);
        if (_light != null) Destroy(_light);
        if (_housing != null) Destroy(_housing);
    }
}
}
