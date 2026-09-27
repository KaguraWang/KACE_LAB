using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChemLab.Game {

/// <summary>
/// A small in-game console. It owns presentation and command dispatch only;
/// experiment commands are registered by the laboratory bootstrap.
/// </summary>
[DefaultExecutionOrder(-9000)]
public sealed class DeveloperConsole : MonoBehaviour {
    const int MaxHistory = 180;
    const int MaxPending = 256;
    const int MaxMessageCharacters = 800;
    const int MaxStackCharacters = 600;

    struct LogEntry {
        public string Message;
        public string Stack;
        public LogType Type;
        public DateTime Time;
    }

    public static DeveloperConsole Instance { get; private set; }
    public static bool InputCaptured => Instance != null && Instance._open;
    public bool IsOpen => _open;
    /// <summary>Test-only handle on the command line field (the pipe cannot type characters).</summary>
    public InputField TestInputField => _input;

    readonly Dictionary<string, Func<string, string>> _commands =
        new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase);
    readonly Queue<LogEntry> _pending = new Queue<LogEntry>();
    readonly Queue<string> _history = new Queue<string>();
    readonly List<LogEntry> _drainBatch = new List<LogEntry>(64);
    readonly object _pendingLock = new object();

    GameObject _overlay;
    InputField _input;
    Text _output;
    ScrollRect _scroll;
    GameObject _previousSelection;
    bool _open;
    bool _dirty;
    int _droppedPending;
    int _toggleFrame = -1;

    /// <summary>Call after the EventSystem and UI font exist. Repeated calls return the same console.</summary>
    public static DeveloperConsole Create(Transform host, Font font) {
        if (Instance != null) return Instance;
        if (host == null) throw new ArgumentNullException(nameof(host));
        var root = new GameObject("DeveloperConsoleHost", typeof(RectTransform));
        root.transform.SetParent(host, false);
        root.layer = LabBootstrap.UiLayerConst;
        var console = root.AddComponent<DeveloperConsole>();
        Instance = console;
        console.Build(font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        return console;
    }

    /// <summary>
    /// Registers one command name. Duplicate names (case insensitive), empty names,
    /// names containing whitespace, and null handlers are rejected without replacing an existing handler.
    /// The handler receives the unparsed argument tail with surrounding whitespace removed.
    /// A non-empty return value is displayed as the command result.
    /// </summary>
    public bool RegisterCommand(string name, Func<string, string> handler) {
        if (string.IsNullOrWhiteSpace(name) || handler == null) return false;
        for (int i = 0; i < name.Length; i++) if (char.IsWhiteSpace(name[i])) return false;
        if (_commands.ContainsKey(name)) return false;
        _commands.Add(name, handler);
        return true;
    }

    public bool UnregisterCommand(string name) => !string.IsNullOrEmpty(name) && _commands.Remove(name);

    /// <summary>Read-only copy for process-local assertions and diagnostics.</summary>
    public string[] GetHistorySnapshot() => _history.ToArray();

    /// <summary>Submits a line through the same path as Enter; also usable by process-local tests.</summary>
    public void Submit(string line) {
        line = (line ?? string.Empty).Trim();
        if (line.Length == 0) return;
        AddHistory("> " + Clip(line, MaxMessageCharacters));

        int split = 0;
        while (split < line.Length && !char.IsWhiteSpace(line[split])) split++;
        string name = line.Substring(0, split);
        string arguments = split < line.Length ? line.Substring(split).Trim() : string.Empty;
        if (!_commands.TryGetValue(name, out var handler)) {
            AddHistory("[ERR] Unknown command: " + Clip(name, MaxMessageCharacters));
            if (_open) RenderHistory(false);
            return;
        }

        try {
            string result = handler(arguments);
            if (!string.IsNullOrEmpty(result)) AddHistory(Clip(result, MaxMessageCharacters));
        } catch (Exception ex) {
            AddHistory("[ERR] " + Clip(name, 80) + ": " + Clip(ex.Message, MaxMessageCharacters));
        }
        if (_open) RenderHistory(false);
    }

    public void Open() {
        if (_open || _overlay == null) return;
        _previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        _open = true;
        _overlay.SetActive(true);
        RenderHistory(true);
        FocusInput();
    }

    public void Close() {
        if (!_open) return;
        _open = false;
        if (_input != null) _input.DeactivateInputField();
        if (_overlay != null) _overlay.SetActive(false);
        if (EventSystem.current != null) {
            var prior = _previousSelection;
            EventSystem.current.SetSelectedGameObject(prior != null && prior.activeInHierarchy ? prior : null);
        }
        _previousSelection = null;
    }

    public void Toggle() { if (_open) Close(); else Open(); }

    void OnEnable() { Application.logMessageReceivedThreaded += CaptureLog; }
    void OnDisable() {
        Application.logMessageReceivedThreaded -= CaptureLog;
        Close();
    }
    void OnDestroy() {
        Application.logMessageReceivedThreaded -= CaptureLog;
        if (Instance == this) Instance = null;
    }

    void CaptureLog(string condition, string stackTrace, LogType type) {
        lock (_pendingLock) {
            if (_pending.Count >= MaxPending) {
                _pending.Dequeue();
                _droppedPending++;
            }
            _pending.Enqueue(new LogEntry {
                Message = Clip(condition, MaxMessageCharacters),
                Stack = Clip(stackTrace, MaxStackCharacters),
                Type = type,
                Time = DateTime.Now
            });
        }
    }

    void Update() {
        // The laboratory's LabInput can be gated while this is open, so the
        // console reads its toggle and submit keys from the underlying input.
        if (RawKeyDown(KeyCode.BackQuote)) { Toggle(); _toggleFrame = Time.frameCount; return; }
        DrainLogs();
    }

    // InputField receives typed characters in EventSystem.Update. Submit after
    // that update so an Enter on the same frame cannot lose the final character.
    void LateUpdate() {
        if (!_open || _toggleFrame == Time.frameCount) return;
        if (RawKeyDown(KeyCode.Return) || RawKeyDown(KeyCode.KeypadEnter)) {
            string line = _input != null ? _input.text : string.Empty;
            if (_input != null) _input.text = string.Empty;
            Submit(line);
            FocusInput();
        }
    }

    static bool RawKeyDown(KeyCode code) => WindowTargetInput.Active
        ? WindowTargetInput.KeyDown(code) : Input.GetKeyDown(code);

    void DrainLogs() {
        int dropped;
        _drainBatch.Clear();
        lock (_pendingLock) {
            dropped = _droppedPending;
            _droppedPending = 0;
            for (int i = 0; i < 64 && _pending.Count > 0; i++) _drainBatch.Add(_pending.Dequeue());
        }
        if (dropped > 0) AddHistory("[WRN] " + dropped + " buffered log entries discarded");
        foreach (var entry in _drainBatch) {
            string level = entry.Type == LogType.Warning ? "WRN" :
                entry.Type == LogType.Error || entry.Type == LogType.Exception || entry.Type == LogType.Assert ? "ERR" : "LOG";
            var message = new StringBuilder();
            message.Append('[').Append(entry.Time.ToString("HH:mm:ss")).Append("] [").Append(level).Append("] ");
            message.Append(entry.Message);
            if (level == "ERR" && !string.IsNullOrEmpty(entry.Stack))
                message.Append('\n').Append(entry.Stack);
            AddHistory(message.ToString());
        }
        if (_open && _dirty) RenderHistory(false);
    }

    static string Clip(string value, int limit) {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= limit ? value : value.Substring(0, limit) + "…";
    }

    void AddHistory(string line) {
        if (_history.Count >= MaxHistory) _history.Dequeue();
        _history.Enqueue(line);
        _dirty = true;
    }

    void RenderHistory(bool forceBottom) {
        if (_output == null || _scroll == null || !_dirty && !forceBottom) return;
        bool atBottom = forceBottom || _scroll.verticalNormalizedPosition <= 0.02f;
        _output.text = string.Join("\n", System.Linq.Enumerable.Select(_history, FormatHistoryLine));
        _dirty = false;
        if (atBottom) {
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
        }
    }

    internal static string FormatHistoryLine(string line) {
        // Log/command text must never inject UI rich-text tags.
        string safe = line.Replace("<", "‹").Replace(">", "›");
        return line.StartsWith("[ERR]", StringComparison.Ordinal)
            ? "<color=#FF6464>" + safe + "</color>" : safe;
    }

    void FocusInput() {
        if (!_open || _input == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(_input.gameObject);
        _input.ActivateInputField();
        _input.MoveTextEnd(false);
    }

    void Build(Font font) {
        _overlay = new GameObject("DeveloperConsoleOverlay", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        _overlay.transform.SetParent(transform, false);
        _overlay.layer = LabBootstrap.UiLayerConst;
        var canvas = _overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = _overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var blocker = Rect("InputBlocker", _overlay.transform, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.42f));
        blocker.GetComponent<Image>().raycastTarget = true;
        var panel = Rect("ConsolePanel", blocker, new Vector2(.025f, .42f), new Vector2(.975f, .975f),
            Vector2.zero, Vector2.zero, new Color(.025f, .035f, .045f, .97f));

        Text title = Label("ConsoleTitle", panel, font, 19, new Color(.44f, .86f, .98f),
            TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(18, -42), new Vector2(-18, -6));
        title.text = "CHEMLAB  /  DEVELOPER CONSOLE    ~ CLOSE";

        var viewport = Rect("LogViewport", panel, new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(16, 62), new Vector2(-36, -48), new Color(.012f, .02f, .027f, 1f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var logRect = Rect("LogContent", viewport, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(10, 0), new Vector2(-10, 0), Color.clear);
        _output = logRect.gameObject.AddComponent<Text>();
        _output.font = font;
        _output.fontSize = 15;
        _output.color = new Color(.83f, .91f, .88f);
        _output.alignment = TextAnchor.UpperLeft;
        _output.horizontalOverflow = HorizontalWrapMode.Wrap;
        _output.verticalOverflow = VerticalWrapMode.Overflow;
        _output.supportRichText = true;
        _output.raycastTarget = false;
        var fitter = logRect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scroll = viewport.gameObject.AddComponent<ScrollRect>();
        _scroll.content = logRect;
        _scroll.viewport = viewport;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 26f;

        var scrollBar = Rect("LogScrollbar", panel, new Vector2(1, 0), new Vector2(1, 1),
            new Vector2(-29, 62), new Vector2(-16, -48), new Color(.12f, .18f, .21f));
        var handle = Rect("Handle", scrollBar, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Color(.35f, .60f, .64f));
        var bar = scrollBar.gameObject.AddComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.BottomToTop;
        bar.handleRect = handle;
        bar.targetGraphic = handle.GetComponent<Image>();
        _scroll.verticalScrollbar = bar;

        var inputBackground = Rect("CommandInput", panel, new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(16, 12), new Vector2(-16, 54), new Color(.07f, .10f, .12f, 1f));
        Label("Prompt", inputBackground, font, 19, new Color(.43f, .88f, .62f), TextAnchor.MiddleCenter,
            Vector2.zero, new Vector2(0, 1), new Vector2(10, 0), new Vector2(29, 0)).text = ">";
        var inputRect = Rect("InputTextArea", inputBackground, Vector2.zero, Vector2.one,
            new Vector2(34, 3), new Vector2(-8, -3), Color.clear);
        var inputText = Label("InputText", inputRect, font, 17, Color.white, TextAnchor.MiddleLeft,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var placeholder = Label("Placeholder", inputRect, font, 17, new Color(.43f, .49f, .51f),
            TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        placeholder.text = "enter command";
        _input = inputBackground.gameObject.AddComponent<InputField>();
        _input.textComponent = inputText;
        _input.placeholder = placeholder;
        _input.targetGraphic = inputBackground.GetComponent<Image>();
        _input.lineType = InputField.LineType.SingleLine;
        _input.characterLimit = 512;
        _input.onValidateInput = (text, index, added) => added == '`' || added == '~' ? '\0' : added;

        _overlay.SetActive(false);
    }

    static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max,
        Vector2 offsetsMin, Vector2 offsetsMax, Color color) {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LabBootstrap.UiLayerConst;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetsMin;
        rect.offsetMax = offsetsMax;
        if (color.a > 0f) go.AddComponent<Image>().color = color;
        return rect;
    }

    static Text Label(string name, Transform parent, Font font, int size, Color color,
        TextAnchor alignment, Vector2 min, Vector2 max, Vector2 offsetsMin, Vector2 offsetsMax) {
        var rect = Rect(name, parent, min, max, offsetsMin, offsetsMax, Color.clear);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
}
}
