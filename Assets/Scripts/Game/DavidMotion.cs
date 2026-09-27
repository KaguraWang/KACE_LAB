using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ChemLab.Game {

/// <summary>
/// `david 0|1 [seconds]` — the dash-drive drag mode.
///
/// While it is on, a dragged instrument does not follow the pointer directly:
/// it jumps through random straight zig-zag segments around the pointer and
/// leaves coloured afterimages of the poses it passed through. Releasing the
/// mouse still runs one last straight segment to the point the ordinary drag
/// would have dropped it at, then normal snapping/dropping resumes.
///
/// The optional second argument is the stillness between two dashes in seconds;
/// without it the default below is used. The mode is a developer tool, so its own
/// timing runs on real frames even when `world_speed 0` has frozen the experiment.
/// </summary>
public static class DavidMode {
    /// <summary>50 cm; the project uses 1 world unit = 100 mm.</summary>
    public const float MaximumDashUnits = 5f;
    /// <summary>Stillness between two dashes when the command does not name one.</summary>
    public const float DefaultPauseSeconds = 0.2f;
    public const float MinimumPauseSeconds = 0f;
    /// <summary>Rejects absurd values instead of quietly clamping them.</summary>
    public const float MaximumPauseSeconds = 10f;
    /// <summary>Dash speed in world units per second.</summary>
    public const float DashSpeed = 16f;
    /// <summary>Shortest useful dash: far enough to leave an overlapping trail.</summary>
    public const float MinimumDashUnits = 0.6f;

    static float _pauseSeconds = DefaultPauseSeconds;

    /// <summary>Seconds between two dashes; set by `david 1 <seconds>`.</summary>
    public static float PauseSeconds => _pauseSeconds;

    public static bool Enabled { get; private set; }

    /// <summary>
    /// Console entry point. Accepts `1`, `1 &lt;seconds&gt;`, `0` and `0 &lt;seconds&gt;`;
    /// anything else is refused with a reason, and a refused line leaves both the
    /// mode and the interval exactly as they were.
    /// </summary>
    public static bool TrySet(string argument, out string message) {
        string text = (argument ?? string.Empty).Trim();
        string[] parts = text.Length == 0
            ? Array.Empty<string>()
            : text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Length > 2) {
            message = "[ERR] david needs 0 or 1, optionally followed by the seconds between dashes, for example: david 1 0.1";
            return false;
        }
        if (parts[0] != "0" && parts[0] != "1") {
            message = "[ERR] david needs 0 or 1, optionally followed by the seconds between dashes, for example: david 1 0.1";
            return false;
        }
        float interval = DefaultPauseSeconds;
        if (parts.Length == 2) {
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out interval)
                || float.IsNaN(interval) || float.IsInfinity(interval)) {
                message = "[ERR] david: '" + parts[1] + "' is not a number of seconds";
                return false;
            }
            if (interval < MinimumPauseSeconds || interval > MaximumPauseSeconds) {
                message = "[ERR] david: interval " + parts[1] + " is outside 0-"
                        + MaximumPauseSeconds.ToString("0.###", CultureInfo.InvariantCulture) + " seconds";
                return false;
            }
        }
        if (parts[0] == "0") {
            Enabled = false;
            DavidDriver.Instance?.CancelAndClear();
            message = "david 0  (normal dragging restored)";
            return true;
        }
        Enabled = true;
        _pauseSeconds = interval;
        message = "david 1  (dash drive on, " + Format(interval) + " s between dashes)";
        return true;
    }

    static string Format(float seconds) => seconds.ToString("0.###", CultureInfo.InvariantCulture);

    public static void ResetForTest() {
        Enabled = false;
        _pauseSeconds = DefaultPauseSeconds;
        DavidDriver.Instance?.CancelAndClear();
    }
}

/// <summary>
/// Owns the david dash state for the one instrument currently being dragged:
/// the pointer's intended drop pose, the separate visual dash pose, the random
/// segment plan and the snapshot afterimages along the swept path.
///
/// Afterimages age on their own clock instead of disappearing at the end of the
/// dash that produced them, so several copies of one instrument stay on screen
/// at once: bold when fresh, fading as they age, gone after their lifetime.
/// </summary>
public sealed class DavidDriver : MonoBehaviour {
    public static DavidDriver Instance { get; private set; }

    /// <summary>One random straight segment, kept for diagnostics and tests.</summary>
    public struct SegmentRecord {
        public int Index;
        public float StartTime;
        public float EndTime;
        public float Length;
        public int Ghosts;
        public bool Final;
    }

    /// <summary>A single afterimage: where it sits on the segment and how it looks.</summary>
    public struct GhostInfo {
        public int Segment;
        public float PathDistance;
        public float PathLength;
        public float ColorT;
        public Color Color;
        public Vector3 Position;
        public bool Visible;
        public bool Scheduled;
        public float Intensity;
        public float RetireIn;
        public int PartCount;
        public string Parts;
    }

    sealed class GhostPart {
        public Mesh Mesh;
        public FluidVisualSnapshot Fluid;
        public Matrix4x4 Relative;
        public string Name;
        public Bounds Bounds;
    }

    /// <summary>
    /// One segment's copied geometry. Copies are shared by every afterimage the
    /// segment produces and released when the last of them fades out.
    /// </summary>
    sealed class SegmentVisual {
        public readonly List<GhostPart> Parts = new List<GhostPart>();
        public int LiveGhosts;
    }

    sealed class Ghost {
        public GameObject Root;
        public MeshFilter[] Filters;
        public MeshRenderer[] Renderers;
        public MaterialPropertyBlock Block;
        public SegmentVisual Visual;
        public int Segment;
        public float PathDistance;
        public float ColorT;
        /// <summary>When this copy is due to disappear; set when its dash ends.</summary>
        public float RetireAt;
        public bool Scheduled;
        public float Intensity;
        public bool Visible;
    }

    enum Phase { Idle, Dash, Pause, Finish }

    // Deep cyan-to-green. A light tint over a near-white bench reads as a pale
    // grey wash, so the hues are deep and the fill is firm: the copies must look
    // like coloured silhouettes, not like faded glass.
    static readonly Color DashStartColor = new Color(0.02f, 0.55f, 0.95f);
    static readonly Color DashEndColor = new Color(0.05f, 0.85f, 0.20f);
    const float BaseAlpha = 0.52f;
    /// <summary>How long a copy takes to disappear once its turn in the pause comes.</summary>
    const float RetireFadeSeconds = 0.12f;
    const float ViewMargin = 6f;
    static readonly string[] ExcludedNames = { "Fx", "Glow", "GlowRing", "Flame", "FocusOutline", "Outline" };

    static Material _sharedMaterial;
    static int _ghostsCreated, _ghostsDestroyed, _copiedMeshes, _disposedMeshes;

    readonly List<Ghost> _live = new List<Ghost>();
    readonly Stack<Ghost> _pool = new Stack<Ghost>();
    readonly List<SegmentVisual> _visuals = new List<SegmentVisual>();
    readonly List<SegmentRecord> _segments = new List<SegmentRecord>();
    System.Random _random = new System.Random();

    SegmentVisual _currentVisual;
    Transform _source;
    Action<Vector3, float> _write;
    Vector3 _anchorFix;
    Quaternion _anchorRotation = Quaternion.identity;

    Phase _phase = Phase.Idle;
    Vector3 _intent;
    float _intentLift;
    Vector3 _offset;
    Vector3 _visual;
    Vector3 _segmentFrom, _segmentTo;
    float _segmentDistance, _segmentTimer, _segmentDuration, _pauseTimer;
    float _sampledDistance;
    float _spacing = 0.25f;
    int _segmentIndex = -1;
    int _visibleGhosts;
    float _nextSegmentLength = -1f;
    Vector3 _finishFrom;
    float _finishDuration, _finishTimer;
    Action _finishCallback;

    public bool Active => _phase != Phase.Idle;
    public bool Finishing => _phase == Phase.Finish;

    public static DavidDriver Ensure(GameObject host) {
        if (Instance != null) return Instance;
        if (host == null) return null;
        Instance = host.AddComponent<DavidDriver>();
        return Instance;
    }

    /* ---------------------------------------------------------------- */
    /* console-facing state                                              */
    /* ---------------------------------------------------------------- */

    public Vector3 IntentForTest => _intent;
    public Vector3 VisualForTest => _visual;
    public float OffsetMagnitudeForTest => _offset.magnitude;
    public int SegmentIndexForTest => _segmentIndex;
    public int VisibleGhostCountForTest => _visibleGhosts;
    public int LiveGhostCountForTest => _live.Count;
    public int PooledGhostCountForTest => _pool.Count;
    public int CopiedMeshCountForTest => _copiedMeshes - _disposedMeshes;
    public static int GhostsCreatedForTest => _ghostsCreated;
    public static int GhostsDestroyedForTest => _ghostsDestroyed;
    public static int CopiedMeshesCreatedForTest => _copiedMeshes;
    public static int CopiedMeshesDestroyedForTest => _disposedMeshes;
    public IList<SegmentRecord> SegmentsForTest => _segments;
    public float GhostSpacingForTest => _spacing;
    public string PhaseForTest => _phase.ToString();
    public int VisualSetCountForTest => _visuals.Count;
    /// <summary>Copies already retired-scheduled, i.e. still alive from a finished dash.</summary>
    public int ScheduledGhostCountForTest {
        get {
            int total = 0;
            foreach (var ghost in _live) if (ghost.Scheduled) total++;
            return total;
        }
    }

    public float SegmentProgressForTest {
        get {
            if (_phase == Phase.Dash) return _segmentDuration <= 0f ? 1f : Mathf.Clamp01(_segmentTimer / _segmentDuration);
            if (_phase == Phase.Finish) return _finishDuration <= 0f ? 1f : Mathf.Clamp01(_finishTimer / _finishDuration);
            return 0f;
        }
    }

    public void TestSeed(int seed) { _random = new System.Random(seed); }
    public void TestClearSegmentLog() { _segments.Clear(); }
    /// <summary>Test hook: forces the length of the next random segment.</summary>
    public void TestSetNextSegmentLength(float units) { _nextSegmentLength = Mathf.Clamp(units, 0.01f, DavidMode.MaximumDashUnits); }

    /* ---------------------------------------------------------------- */
    /* drag lifecycle                                                    */
    /* ---------------------------------------------------------------- */

    /// <summary>
    /// Starts the dash for one instrument.
    /// <paramref name="source"/> is the view whose visible meshes are copied,
    /// <paramref name="write"/> applies the dashed pose to the model, and
    /// <paramref name="alive"/> reports whether the instrument still exists.
    /// </summary>
    public void Begin(Transform source, Action<Vector3, float> write, Func<bool> alive,
                      Vector3 modelPosition, float modelLift, Vector3 intent, float intentLift) {
        if (source == null || write == null) return;
        CancelAndClear();
        _source = source;
        _write = write;
        // Views are synced from their model before input runs, so the view pose is
        // the model pose here; the difference is the small standing offset a view
        // adds (for example the 3 mm lift that keeps glass off the bench).
        Vector3 modelWorld = source.parent != null
            ? source.parent.TransformPoint(ModelWorld(modelPosition, modelLift))
            : ModelWorld(modelPosition, modelLift);
        _anchorFix = source.position - modelWorld;
        _anchorRotation = source.rotation;
        _intent = intent;
        _intentLift = intentLift;
        _offset = new Vector3(modelPosition.x - intent.x, modelLift - intentLift, modelPosition.z - intent.z);
        _visual = ModelWorld(modelPosition, modelLift);
        _segments.Clear();
        _segmentIndex = -1;
        _visibleGhosts = 0;
        StartDash();
        ApplyVisual();
    }

    /// <summary>Feeds the pointer's intended pose; advances the dash and samples the trail.</summary>
    public void SetIntent(Vector3 position, float lift) {
        if (_phase == Phase.Idle) return;
        _intent = position;
        _intentLift = lift;
        Advance(Time.deltaTime);
        ApplyVisual();
    }

    /// <summary>
    /// Releasing the mouse runs one last straight segment to the pose the ordinary
    /// drag would have dropped at, then hands control back through
    /// <paramref name="onArrived"/> (where the original snap/drop logic lives).
    /// </summary>
    public void BeginFinish(Action onArrived) {
        if (_phase == Phase.Idle || _phase == Phase.Finish) { onArrived?.Invoke(); return; }
        _finishFrom = _offset;
        _finishCallback = onArrived;
        if (_finishFrom.magnitude < 0.02f) {
            _offset = Vector3.zero;
            ApplyVisual();
            CompleteFinish();
            return;
        }
        _finishDuration = Mathf.Clamp(_finishFrom.magnitude / DavidMode.DashSpeed, 0.06f, 0.45f);
        _finishTimer = 0f;
        _segmentFrom = _finishFrom;
        _segmentTo = Vector3.zero;
        _segmentDistance = _finishFrom.magnitude;
        _segmentIndex++;
        _sampledDistance = 0f;
        _currentVisual = CaptureVisual();
        _segments.Add(new SegmentRecord {
            Index = _segmentIndex, StartTime = Time.unscaledTime, EndTime = -1f,
            Length = _segmentDistance, Ghosts = 0, Final = true
        });
        _phase = Phase.Finish;
    }

    /// <summary>Used by `david 0` and by teardown: drop the effect, keep the instrument where it now is.</summary>
    public void CancelAndClear() {
        if (_phase != Phase.Idle) {
            _offset = Vector3.zero;
            ApplyVisual();
        }
        _phase = Phase.Idle;
        _finishCallback = null;
        ClearTrail();
        ReleaseAllVisuals();
        _source = null;
        _write = null;
        _anchorFix = Vector3.zero;
        _visibleGhosts = 0;
    }

    /// <summary>Destroys every pooled object and copied mesh; used by tests and shutdown.</summary>
    public void Dispose() {
        CancelAndClear();
        while (_pool.Count > 0) {
            var ghost = _pool.Pop();
            DestroyGhost(ghost);
        }
        if (Instance == this) Instance = null;
    }

    void OnDestroy() { Dispose(); }

    void Update() {
        // Afterimages age on their own clock: they must keep fading while the
        // instrument rests between two dashes and while the release run plays.
        AgeTrail();
        // The final straight run happens after the mouse button is already up, so
        // it cannot rely on SetIntent being called from the drag path.
        if (_phase != Phase.Finish) return;
        Advance(Time.deltaTime);
        ApplyVisual();
    }

    /* ---------------------------------------------------------------- */
    /* motion                                                            */
    /* ---------------------------------------------------------------- */

    void Advance(float dt) {
        if (dt <= 0f) dt = 1f / 60f;
        switch (_phase) {
            case Phase.Dash: {
                _segmentTimer += dt;
                float t = _segmentDuration <= 0f ? 1f : Mathf.Clamp01(_segmentTimer / _segmentDuration);
                _offset = Vector3.Lerp(_segmentFrom, _segmentTo, Ease(t));
                SamplePath();
                if (t >= 1f) {
                    _offset = _segmentTo;
                    CompleteSegmentRecord(false);
                    // The trail now starts retiring: oldest copy first, spread across
                    // the whole pause, so the next dash begins on a clean screen.
                    ScheduleRetirement();
                    _phase = Phase.Pause;
                    _pauseTimer = 0f;
                }
                break;
            }
            case Phase.Pause:
                _pauseTimer += dt;
                if (_pauseTimer >= DavidMode.PauseSeconds) StartDash();
                break;
            case Phase.Finish: {
                _finishTimer += dt;
                float t = _finishDuration <= 0f ? 1f : Mathf.Clamp01(_finishTimer / _finishDuration);
                _offset = Vector3.Lerp(_finishFrom, Vector3.zero, Ease(t));
                SamplePath();
                if (t >= 1f) {
                    _offset = Vector3.zero;
                    CompleteSegmentRecord(true);
                    ApplyVisual();
                    CompleteFinish();
                }
                break;
            }
        }
    }

    /// <summary>
    /// Hands every copy of the finished dash a retirement time inside the coming
    /// pause: oldest first, evenly spaced, so the trail drains from its tail and is
    /// gone exactly when the next dash starts.
    /// </summary>
    void ScheduleRetirement() {
        int count = _live.Count;
        if (count == 0) return;
        float pause = Mathf.Max(0.05f, DavidMode.PauseSeconds);
        float now = Time.unscaledTime;
        for (int i = 0; i < count; i++) {
            // _live is in spawn order, so index 0 is the oldest copy.
            var ghost = _live[i];
            ghost.Scheduled = true;
            ghost.RetireAt = now + pause * (i + 1f) / count;
        }
    }

    void CompleteFinish() {
        var callback = _finishCallback;
        _finishCallback = null;
        _phase = Phase.Idle;
        ClearTrail();
        ReleaseAllVisuals();
        _source = null;
        _write = null;
        _visibleGhosts = 0;
        callback?.Invoke();
    }

    void CompleteSegmentRecord(bool final) {
        for (int i = _segments.Count - 1; i >= 0; i--) {
            if (_segments[i].EndTime >= 0f) continue;
            var record = _segments[i];
            record.EndTime = Time.unscaledTime;
            record.Ghosts = CountGhostsForSegment(record.Index);
            _segments[i] = record;
            return;
        }
    }

    int CountGhostsForSegment(int segment) {
        int total = 0;
        foreach (var ghost in _live) if (ghost.Segment == segment) total++;
        return total;
    }

    void StartDash() {
        if (_currentVisual != null && _currentVisual.LiveGhosts == 0) ReleaseVisual(_currentVisual);
        // The trail is deliberately not cleared here: the copies of the previous
        // dash keep fading while this one plays, which is what makes the dash read
        // as a trail of distinct poses instead of a single moving object.
        _segmentIndex++;
        _segmentFrom = _offset;
        float length = _nextSegmentLength > 0f
            ? _nextSegmentLength
            : Mathf.Lerp(DavidMode.MinimumDashUnits, DavidMode.MaximumDashUnits, (float)_random.NextDouble());
        _nextSegmentLength = -1f;
        double angle = _random.NextDouble() * Math.PI * 2d;
        float vertical = Mathf.Lerp(-0.35f, 0.35f, (float)_random.NextDouble());
        Vector3 direction = new Vector3((float)Math.Cos(angle), 0f, (float)Math.Sin(angle));
        // A pure random walk drifts away and then sits at the 50 cm limit. Bias the
        // new segment back toward the pointer so the zig-zag stays in its vicinity
        // while the direction and length of each dash remain random.
        if (_segmentFrom.sqrMagnitude > 0.04f) {
            float pull = Mathf.Clamp01(_segmentFrom.magnitude / DavidMode.MaximumDashUnits) * 1.4f;
            direction = Vector3.Normalize(direction - _segmentFrom.normalized * pull);
        }
        var candidate = _segmentFrom
            + direction * length
            + Vector3.up * vertical;
        _segmentTo = ConstrainDash(candidate);
        _segmentDistance = Vector3.Distance(_segmentFrom, _segmentTo);
        if (_segmentDistance < 0.02f) {
            // Nothing legal to sweep (for example the pointer sits outside the
            // viewport): hold this pose for one pause and try again.
            _segmentTo = _segmentFrom;
            _segmentDistance = 0f;
        }
        _segmentTimer = 0f;
        _segmentDuration = Mathf.Max(0.05f, _segmentDistance / DavidMode.DashSpeed);
        _sampledDistance = 0f;
        _currentVisual = CaptureVisual();
        _segments.Add(new SegmentRecord {
            Index = _segmentIndex, StartTime = Time.unscaledTime, EndTime = -1f,
            Length = _segmentDistance, Ghosts = 0, Final = false
        });
        _phase = Phase.Dash;
    }

    /// <summary>Keeps the dash within 50 cm of the pointer and inside the rendered view.</summary>
    Vector3 ConstrainDash(Vector3 candidate) {
        if (candidate.magnitude > DavidMode.MaximumDashUnits)
            candidate = candidate.normalized * DavidMode.MaximumDashUnits;
        candidate.y = Mathf.Clamp(candidate.y, -Mathf.Max(0f, _intentLift), 1.2f);
        var camera = Camera.main != null ? Camera.main : LabBootstrap.Instance != null ? LabBootstrap.Instance.Cam : null;
        if (camera == null) return candidate;
        for (int attempt = 0; attempt < 8; attempt++) {
            Vector3 world = ModelWorld(_intent + candidate, _intentLift + candidate.y);
            Vector3 screen = camera.WorldToScreenPoint(world);
            bool visible = screen.z > camera.nearClipPlane
                && screen.x >= ViewMargin && screen.x <= Screen.width - ViewMargin
                && screen.y >= ViewMargin && screen.y <= Screen.height - ViewMargin;
            if (visible) return candidate;
            Vector3 shortened = _segmentFrom + (candidate - _segmentFrom) * 0.6f;
            if (Vector3.Distance(_segmentFrom, shortened) < 0.05f) break;
            candidate = shortened;
        }
        // Even the segment origin is outside the rendered view (the pointer sits at
        // the very edge): keep the instrument on the pointer instead of dashing
        // further off screen.
        return Vector3.zero;
    }

    void ApplyVisual() {
        if (_write == null) return;
        // The pointer (and therefore the whole segment's origin) can move after
        // StartDash validated its endpoint. Recheck the actual pose every frame,
        // including pauses and the final return, instead of trusting old bounds.
        _offset = ConstrainDash(_offset);
        Vector3 position = _intent + _offset;
        float lift = Mathf.Max(0f, _intentLift + _offset.y);
        _visual = ModelWorld(position, lift);
        _write(_visual, lift);
    }

    static Vector3 ModelWorld(Vector3 position, float lift) => new Vector3(position.x, lift, position.z);
    static float Ease(float t) => t * t * (3f - 2f * t);

    /* ---------------------------------------------------------------- */
    /* afterimages                                                       */
    /* ---------------------------------------------------------------- */

    /// <summary>
    /// Copies the instrument's currently visible meshes so every afterimage of
    /// this segment is a snapshot of the state it was taken from. The copies are
    /// geometry only: they carry no model, no solver and no chemistry.
    /// </summary>
    SegmentVisual CaptureVisual() {
        var visual = new SegmentVisual();
        if (_source == null) return visual;
        var filters = _source.GetComponentsInChildren<MeshFilter>(true);
        var bounds = new Bounds();
        bool hasBounds = false;
        foreach (var filter in filters) {
            var mesh = filter.sharedMesh;
            if (mesh == null || mesh.vertexCount == 0) continue;
            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || !renderer.enabled || !filter.gameObject.activeInHierarchy) continue;
            if (Excluded(filter.transform)) continue;
            FluidVisualSnapshot fluid = null;
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name == "ChemLab/FluidVolume") {
                fluid = filter.GetComponentInParent<VesselView>()?.Fluid?.CaptureVisualSnapshot();
                // Never tint the raymarch box as if it were actual liquid geometry.
                if (fluid == null) continue;
            }
            var part = new GhostPart {
                Mesh = CopyMesh(mesh),
                Fluid = fluid,
                Relative = _source.worldToLocalMatrix * filter.transform.localToWorldMatrix,
                Name = filter.gameObject.name,
                Bounds = mesh.bounds
            };
            if (!hasBounds) { bounds = part.Bounds; hasBounds = true; } else bounds.Encapsulate(part.Bounds);
            visual.Parts.Add(part);
        }
        if (hasBounds) {
            float extent = Mathf.Max(bounds.size.x, bounds.size.z);
            // 0.7 of the object's own width: neighbouring copies still touch and
            // overlap, but each one stays countable instead of merging into a smear.
            _spacing = Mathf.Clamp(extent * 0.7f, 0.1f, 0.7f);
        }
        _visuals.Add(visual);
        return visual;
    }

    bool Excluded(Transform transform) {
        for (var current = transform; current != null && current != _source.parent; current = current.parent) {
            foreach (var name in ExcludedNames)
                if (current.name == name) return true;
            if (current == _source) break;
        }
        return false;
    }

    static Mesh CopyMesh(Mesh source) {
        var copy = new Mesh { name = source.name + " (snapshot)" };
        copy.indexFormat = source.indexFormat;
        var vertices = source.vertices;
        copy.SetVertices(vertices);
        var normals = source.normals;
        if (normals != null && normals.Length == vertices.Length) copy.SetNormals(normals);
        var uv = source.uv;
        if (uv != null && uv.Length == vertices.Length) copy.SetUVs(0, uv);
        var colors = source.colors;
        if (colors != null && colors.Length == vertices.Length) copy.SetColors(colors);
        copy.subMeshCount = source.subMeshCount;
        for (int i = 0; i < source.subMeshCount; i++) copy.SetTriangles(source.GetTriangles(i), i);
        copy.RecalculateBounds();
        _copiedMeshes++;
        return copy;
    }

    void ReleaseVisual(SegmentVisual visual) {
        if (visual == null) return;
        foreach (var part in visual.Parts) {
            part.Fluid?.Dispose();
            if (part.Mesh == null) continue;
            if (Application.isPlaying) Destroy(part.Mesh); else DestroyImmediate(part.Mesh);
            _disposedMeshes++;
        }
        visual.Parts.Clear();
        _visuals.Remove(visual);
        if (_currentVisual == visual) _currentVisual = null;
    }

    void ReleaseAllVisuals() {
        var copy = new List<SegmentVisual>(_visuals);
        foreach (var visual in copy) ReleaseVisual(visual);
        _visuals.Clear();
        _currentVisual = null;
    }

    void SamplePath() {
        var visual = _currentVisual;
        if (visual == null || visual.Parts.Count == 0) return;
        float travelled = Vector3.Distance(_segmentFrom, _offset);
        // Copies are always spawned at the head of the swept path, so the trail
        // stays attached to the instrument for the whole dash.
        while (_sampledDistance + _spacing <= travelled) {
            _sampledDistance += _spacing;
            SpawnGhost(_sampledDistance, visual);
        }
    }

    void SpawnGhost(float pathDistance, SegmentVisual visual) {
        var ghost = RentGhost();
        ghost.Visual = visual;
        visual.LiveGhosts++;
        ghost.Segment = _segmentIndex;
        ghost.PathDistance = pathDistance;
        ghost.ColorT = _segmentDistance <= 1e-5f ? 0f : Mathf.Clamp01(pathDistance / _segmentDistance);
        ghost.Scheduled = false;
        ghost.RetireAt = 0f;
        ghost.Intensity = 1f;
        ghost.Visible = true;
        float t = ghost.ColorT;
        Vector3 local = Vector3.Lerp(_segmentFrom, _segmentTo, t);
        Vector3 position = _intent + local;
        float lift = Mathf.Max(0f, _intentLift + local.y);
        ghost.Root.transform.SetPositionAndRotation(ModelWorld(position, lift) + _anchorFix, _anchorRotation);
        BindParts(ghost);
        PaintGhost(ghost, Color.Lerp(DashStartColor, DashEndColor, t), 1f);
        ghost.Root.SetActive(true);
        // The live list is the trail: without it a copy would never age out, never
        // be recycled and never be counted as an afterimage.
        _live.Add(ghost);
    }

    /// <summary>
    /// Keeps the trail up to date. Copies of the running dash stay at full strength
    /// and are unlimited; copies of a finished dash fade out on their scheduled turn,
    /// oldest first, until the pause has drained the whole trail.
    /// </summary>
    void AgeTrail() {
        float now = Time.unscaledTime;
        int visible = 0;
        for (int i = _live.Count - 1; i >= 0; i--) {
            var ghost = _live[i];
            if (ghost.Scheduled) {
                float remaining = ghost.RetireAt - now;
                if (remaining <= 0f) { RecycleGhost(i); continue; }
                float intensity = Mathf.Clamp01(remaining / RetireFadeSeconds);
                ghost.Intensity = intensity;
                bool on = intensity > 0.03f;
                ghost.Visible = on;
                PaintGhost(ghost, Color.Lerp(DashStartColor, DashEndColor, ghost.ColorT), intensity);
                if (ghost.Root.activeSelf != on) ghost.Root.SetActive(on);
                if (on) visible++;
            } else {
                if (!ghost.Visible) {
                    ghost.Visible = true;
                    ghost.Intensity = 1f;
                    PaintGhost(ghost, Color.Lerp(DashStartColor, DashEndColor, ghost.ColorT), 1f);
                    if (ghost.Root != null && !ghost.Root.activeSelf) ghost.Root.SetActive(true);
                }
                visible++;
            }
        }
        _visibleGhosts = visible;
    }

    void PaintGhost(Ghost ghost, Color tint, float intensity) {
        tint.a = BaseAlpha * Mathf.Clamp01(intensity);
        ghost.Block.SetColor("_Color", tint);
        ghost.Block.SetFloat("_GhostFade", Mathf.Clamp01(intensity));
        for (int i = 0; i < ghost.Renderers.Length; i++) {
            if (ghost.Renderers[i] == null) continue;
            ghost.Renderers[i].SetPropertyBlock(ghost.Block);
        }
    }

    void BindParts(Ghost ghost) {
        var parts = ghost.Visual != null ? ghost.Visual.Parts : null;
        int count = parts != null ? parts.Count : 0;
        EnsurePartSlots(ghost, count);
        for (int i = 0; i < ghost.Filters.Length; i++) {
            bool used = i < count;
            if (ghost.Filters[i].gameObject.activeSelf != used) ghost.Filters[i].gameObject.SetActive(used);
            if (!used) continue;
            ghost.Filters[i].sharedMesh = parts[i].Mesh;
            var materials = new Material[Mathf.Max(1, parts[i].Mesh.subMeshCount)];
            for (int sub = 0; sub < materials.Length; sub++)
                materials[sub] = parts[i].Fluid != null ? parts[i].Fluid.Material : GhostMaterial();
            ghost.Renderers[i].sharedMaterials = materials;
            // Keep the visible part names so a snapshot can be compared with the
            // live instrument by name (and so the afterimage is readable in-scene).
            ghost.Filters[i].gameObject.name = parts[i].Name;
            PostTransform(ghost.Filters[i].transform, parts[i].Relative);
        }
    }

    static void PostTransform(Transform transform, Matrix4x4 matrix) {
        transform.localPosition = new Vector3(matrix.m03, matrix.m13, matrix.m23);
        transform.localRotation = matrix.rotation;
        transform.localScale = matrix.lossyScale;
    }

    Ghost RentGhost() {
        while (_pool.Count > 0) {
            var pooled = _pool.Pop();
            if (pooled != null && pooled.Root != null) return pooled;
        }
        var ghost = new Ghost { Block = new MaterialPropertyBlock() };
        ghost.Root = new GameObject("DavidAfterimage");
        ghost.Root.transform.SetParent(transform, false);
        _ghostsCreated++;
        return ghost;
    }

    void EnsurePartSlots(Ghost ghost, int count) {
        if (ghost.Filters != null && ghost.Filters.Length >= count) return;
        var filters = new List<MeshFilter>();
        var renderers = new List<MeshRenderer>();
        if (ghost.Filters != null) {
            filters.AddRange(ghost.Filters);
            renderers.AddRange(ghost.Renderers);
        }
        for (int i = filters.Count; i < count; i++) {
            var part = new GameObject("Part" + i);
            part.transform.SetParent(ghost.Root.transform, false);
            filters.Add(part.AddComponent<MeshFilter>());
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GhostMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderers.Add(renderer);
        }
        ghost.Filters = filters.ToArray();
        ghost.Renderers = renderers.ToArray();
        ghost.Root.SetActive(false);
    }

    void RecycleGhost(int index) {
        var ghost = _live[index];
        _live.RemoveAt(index);
        if (ghost.Root != null) ghost.Root.SetActive(false);
        // The mesh belongs to the segment visual and is released with its last copy.
        if (ghost.Filters != null)
            foreach (var filter in ghost.Filters)
                if (filter != null) filter.sharedMesh = null;
        ghost.Visible = false;
        ghost.Intensity = 0f;
        if (ghost.Visual != null) {
            ghost.Visual.LiveGhosts--;
            if (ghost.Visual.LiveGhosts <= 0) ReleaseVisual(ghost.Visual);
            ghost.Visual = null;
        }
        _pool.Push(ghost);
    }

    void ClearTrail() {
        for (int i = _live.Count - 1; i >= 0; i--) RecycleGhost(i);
        _visibleGhosts = 0;
    }

    void DestroyGhost(Ghost ghost) {
        if (ghost?.Root == null) return;
        // Deactivate before the deferred Destroy so a teardown check in the same
        // frame cannot still see a live afterimage in the hierarchy.
        ghost.Root.SetActive(false);
        if (Application.isPlaying) Destroy(ghost.Root); else DestroyImmediate(ghost.Root);
        _ghostsDestroyed++;
    }

    static Material GhostMaterial() {
        if (_sharedMaterial != null) return _sharedMaterial;
        var shader = Resources.Load<Shader>("ChemLab/Ghost");
        if (shader == null) shader = Shader.Find("ChemLab/Ghost");
        if (shader == null) {
            // Never let a missing import turn the dash mode into a crash.
            Debug.LogError("[ChemLab] 找不到 ChemLab/Ghost 着色器，残影回退到 Unlit/Color");
            shader = Shader.Find("Unlit/Color");
        }
        _sharedMaterial = new Material(shader) { name = "ChemLabGhostSnapshot", hideFlags = HideFlags.DontSave };
        return _sharedMaterial;
    }

    /* ---------------------------------------------------------------- */
    /* diagnostics                                                       */
    /* ---------------------------------------------------------------- */

    public List<GhostInfo> GhostsForTest() {
        float now = Time.unscaledTime;
        var list = new List<GhostInfo>(_live.Count);
        foreach (var ghost in _live) {
            var info = new GhostInfo {
                Segment = ghost.Segment,
                PathDistance = ghost.PathDistance,
                PathLength = _segmentDistance,
                ColorT = ghost.ColorT,
                Position = ghost.Root != null ? ghost.Root.transform.position : Vector3.zero,
                Visible = ghost.Visible,
                Scheduled = ghost.Scheduled,
                Intensity = ghost.Intensity,
                RetireIn = ghost.Scheduled ? ghost.RetireAt - now : -1f,
                PartCount = ghost.Visual != null ? ghost.Visual.Parts.Count : 0,
                Parts = PartNames(ghost)
            };
            var block = ghost.Block;
            info.Color = block != null ? block.GetColor("_Color") : Color.clear;
            list.Add(info);
        }
        return list;
    }

    static string PartNames(Ghost ghost) {
        if (ghost.Filters == null) return string.Empty;
        var names = new List<string>();
        foreach (var filter in ghost.Filters) {
            if (filter == null) continue;
            names.Add(filter.gameObject.activeSelf ? filter.gameObject.name : "-");
        }
        return string.Join(",", names);
    }

    public string AuditLine() {
        return "phase=" + _phase + " segment=" + _segmentIndex
             + " offset=" + _offset.magnitude.ToString("F3") + " intent=" + Fmt(_intent) + " visual=" + Fmt(_visual)
             + " ghosts=" + _live.Count + " visible=" + _visibleGhosts
             + " visibleParts=" + (_currentVisual != null ? _currentVisual.Parts.Count : 0)
             + " visualSets=" + _visuals.Count
             + " copiedMeshes=" + CopiedMeshCountForTest
             + " spacing=" + _spacing.ToString("F3")
             + " pause=" + DavidMode.PauseSeconds.ToString("0.###");
    }

    static string Fmt(Vector3 v) => v.x.ToString("F2") + "," + v.y.ToString("F2") + "," + v.z.ToString("F2");
}
}
