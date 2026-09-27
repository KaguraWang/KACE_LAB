/* =====================================================================
   宠物聊天窗 + AI 助手
     · 消息列表：只有「收到任务」「任务完成」和对话内容 ——
       不显示中间思考、不显示改了哪些文件。用户要的是干净的对话。
     · 输入框：多行、内容高度自适应、支持无极滚动
     · AI：OpenAI 兼容的 /chat/completions
         - 教师端默认接 DeepSeek
         - 学生端 baseUrl/apiKey 为空时会提示去配置
     · 助手的人格与「如何掌控本项目」写在 skill/anon-assistant.md 里，
       每次请求都会把它作为 system 消息带上。
   ===================================================================== */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;

namespace ChemLab.Game {

/* ---------------- 配置 ---------------- */
[Serializable]
public class AiConfig {
    public string baseUrl = "";
    public string apiKey = "";
    public string model = "deepseek-chat";
    public bool isTeacher = false;
    public int timeout = 60;
    public int maxHistory = 12;

    const string Key = "anon.ai.config";

    public static AiConfig Load() {
        var c = new AiConfig();
        /* 先试 Resources 里的 ai_config.json（部署时替换这个文件即可） */
        var ta = Resources.Load<TextAsset>("ai_config");
        if (ta != null) {
            try { JsonUtility.FromJsonOverwrite(ta.text, c); } catch (Exception e) {
                Debug.LogWarning("[Anon] ai_config.json 解析失败：" + e.Message);
            }
        }
        /* 用户自己在设置里填过的优先 */
        if (PlayerPrefs.HasKey(Key)) {
            try { JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(Key), c); } catch { }
        }
        return c;
    }
    public void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(this)); PlayerPrefs.Save(); }

    public bool Ready => !string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(apiKey);
}

/* ---------------- 一条消息 ---------------- */
public class ChatMsg {
    public enum Who { Me, Anon, System }
    public Who From;
    public string Text;
    public double At;
    public bool Localizable;
    public ChatMsg(Who w, string t, bool localizable = false) { From = w; Text = t; Localizable = localizable; At = Time.unscaledTimeAsDouble; }
}

/* ---------------- 聊天窗 ---------------- */
public class PetChat : MonoBehaviour {
    public static PetChat Instance;

    public static readonly List<ChatMsg> History = new List<ChatMsg>();
    public static AiConfig Cfg;

    Canvas _canvas;
    RectTransform _root, _panel, _listContent, _inputRt;
    ScrollRect _scroll;
    InputField _input;
    Text _inputText, _status;
    bool _busy;
    float _inputH = 34f;
    const float InputMinH = 34f, InputMaxH = 120f, InputLineH = 19f;

    public static void RefreshForLanguage() {
        if (Instance != null && Instance.gameObject.activeInHierarchy) Instance.Refresh();
    }

    public static PetChat Toggle() {
        /* AI 助手是教程的奖励：没走完（也没跳过）之前不给开 */
        if (!PetProgress.AiUnlocked) {
            PetView.Instance?.Say(
                "聊天还没解锁 —— 先把教程走完，或者右键我选「跳过教程」。\n" +
                "（我不是故意为难你，教程里教的都是这软件的用法，你会用得上。）", 10f);
            return null;
        }
        if (Instance == null) {
            var host = LabBootstrap.Instance != null ? LabBootstrap.Instance.gameObject : null;
            var go = new GameObject("PetChat");
            go.transform.SetParent(host.transform, false);
            Instance = go.AddComponent<PetChat>();
            Instance.Build(LabUI.UiFont);
        }
        if (Instance.gameObject.activeSelf) { Instance.gameObject.SetActive(false); return null; }
        Instance.gameObject.SetActive(true);
        Instance.Refresh();
        return Instance;
    }

    void Build(Font font) {
        if (Cfg == null) Cfg = AiConfig.Load();
        Localization.RegisterAiUiTranslations();

        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = PetView.SortingOrder + 2;
        var sc = gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(LabUI.DesignW, LabUI.DesignH);
        sc.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        _root = (RectTransform)transform;

        /* 面板：贴在右下角，不挡左侧器材架与右侧试剂架 */
        var pgo = new GameObject("Panel", typeof(RectTransform));
        pgo.transform.SetParent(_root, false);
        var pimg = pgo.AddComponent<Image>();
        pimg.color = new Color(0.08f, 0.11f, 0.16f, 0.97f);
        _panel = (RectTransform)pgo.transform;
        _panel.anchorMin = _panel.anchorMax = new Vector2(1, 0);
        _panel.pivot = new Vector2(1, 0);
        _panel.anchoredPosition = new Vector2(-16, 62);
        _panel.sizeDelta = new Vector2(420, 420);

        /* 标题栏 */
        var bar = MkText(_panel, "阿侬 · 助手", 15, new Color(0.65f, 0.92f, 0.78f), TextAnchor.MiddleLeft,
                         new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -32), new Vector2(-120, -6));
        MkBtn(_panel, "设置", () => ShowSettings(), new Vector2(-64, -6), new Vector2(60, 26));
        MkBtn(_panel, "清空", () => { History.Clear(); Refresh(); }, new Vector2(-4, -6), new Vector2(54, 26));

        /* 消息列表（可滚动） */
        var viewGo = new GameObject("View", typeof(RectTransform));
        viewGo.transform.SetParent(_panel, false);
        var vimg = viewGo.AddComponent<Image>();
        vimg.color = new Color(0.05f, 0.07f, 0.10f, 1f);
        var viewRt = (RectTransform)viewGo.transform;
        viewRt.anchorMin = new Vector2(0, 0); viewRt.anchorMax = new Vector2(1, 1);
        viewRt.offsetMin = new Vector2(10, 96); viewRt.offsetMax = new Vector2(-10, -38);
        var mask = viewGo.AddComponent<RectMask2D>();
        _scroll = viewGo.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.movementType = ScrollRect.MovementType.Elastic;
        _scroll.elasticity = 0.08f;
        _scroll.inertia = true;
        _scroll.decelerationRate = 0.12f;
        _scroll.scrollSensitivity = 28f;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewRt, false);
        _listContent = (RectTransform)contentGo.transform;
        _listContent.anchorMin = new Vector2(0, 1);
        _listContent.anchorMax = new Vector2(1, 1);
        _listContent.pivot = new Vector2(0.5f, 1);
        _listContent.sizeDelta = new Vector2(0, 0);
        var fit = contentGo.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var lay = contentGo.AddComponent<VerticalLayoutGroup>();
        lay.childForceExpandHeight = false;
        lay.childForceExpandWidth = true;
        lay.childControlHeight = true;
        lay.childControlWidth = true;
        lay.spacing = 6;
        lay.padding = new RectOffset(8, 8, 8, 8);
        _scroll.content = _listContent;
        _scroll.viewport = viewRt;

        /* 输入框：多行、随内容长高、能无极滚动 */
        var inGo = new GameObject("Input", typeof(RectTransform));
        inGo.transform.SetParent(_panel, false);
        var inImg = inGo.AddComponent<Image>();
        inImg.color = new Color(0.04f, 0.06f, 0.09f, 1f);
        _inputRt = (RectTransform)inGo.transform;
        _inputRt.anchorMin = new Vector2(0, 0); _inputRt.anchorMax = new Vector2(1, 0);
        _inputRt.pivot = new Vector2(0.5f, 0);
        _inputRt.anchoredPosition = new Vector2(-46, 46);
        _inputRt.sizeDelta = new Vector2(-72, InputMinH);

        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(_inputRt, false);
        _inputText = txtGo.AddComponent<Text>();
        _inputText.font = font; _inputText.fontSize = 14;
        _inputText.color = new Color(0.94f, 0.96f, 1f);
        _inputText.alignment = TextAnchor.UpperLeft;
        _inputText.supportRichText = false;
        _inputText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _inputText.verticalOverflow = VerticalWrapMode.Overflow;
        var trt = (RectTransform)txtGo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10, 5); trt.offsetMax = new Vector2(-10, -5);

        _input = inGo.AddComponent<InputField>();
        _input.targetGraphic = inImg;
        _input.textComponent = _inputText;
        _input.lineType = InputField.LineType.MultiLineNewline;   // 多行，回车换行
        _input.text = "";

        MkBtn(_panel, "发送", Send, new Vector2(-4, 46), new Vector2(58, 34));
        MkBtn(_panel, "收起", () => gameObject.SetActive(false), new Vector2(-4, 46 + 38), new Vector2(58, 30));

        _status = MkText(_panel, "", 12, new Color(0.7f, 0.75f, 0.85f), TextAnchor.MiddleLeft,
                         new Vector2(0, 0), new Vector2(1, 0), new Vector2(12, 14), new Vector2(-70, 40));

        Refresh();
    }

    /* ---------------- 消息 ---------------- */
    public static void Add(ChatMsg.Who who, string text, bool localizable = false) {
        History.Add(new ChatMsg(who, text, localizable));
        if (Instance != null) Instance.Refresh();
    }

    void Refresh() {
        if (_listContent == null) return;
        for (int i = _listContent.childCount - 1; i >= 0; i--) Destroy(_listContent.GetChild(i).gameObject);
        foreach (var m in History) AddBubble(m);
        if (History.Count == 0) {
            AddBubble(new ChatMsg(ChatMsg.Who.Anon,
                Cfg != null && Cfg.Ready
                ? "我是阿侬。想要什么实验、想算什么，直接说就行。"
                : "我是阿侬。不过还没配置 AI 接口，先点右上角「设置」把地址和密钥填上吧。\n（教程和宠物功能不受影响，随时能用）", true));
        }
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);
        _scroll.verticalNormalizedPosition = 0f;      // 滚到最新
        UpdateStatus();
    }

    void AddBubble(ChatMsg m) {
        var go = new GameObject("Msg", typeof(RectTransform));
        go.transform.SetParent(_listContent, false);
        var bg = go.AddComponent<Image>();
        bool me = m.From == ChatMsg.Who.Me;
        bg.color = me ? new Color(0.16f, 0.30f, 0.26f, 1f)
                 : m.From == ChatMsg.Who.System ? new Color(0.20f, 0.18f, 0.12f, 1f)
                 : new Color(0.14f, 0.18f, 0.26f, 1f);
        var tgo = new GameObject("T", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var t = tgo.AddComponent<Text>();
        t.font = LabUI.UiFont; t.fontSize = 14; t.color = new Color(0.95f, 0.97f, 1f);
        string speaker = !Localization.English ? (m.From == ChatMsg.Who.Anon ? "阿侬" : "系统")
            : (m.From == ChatMsg.Who.Anon ? "Anon" : "System");
        string body = m.Localizable ? Localization.TChatMessage(m.Text) : m.Text;
        t.text = (me ? "" : "<b>" + speaker + "</b>  ") + body;
        Localization.ProtectFromTranslation(t);
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10, 7); trt.offsetMax = new Vector2(-10, -7);
        var fit = go.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 30;
    }

    void UpdateStatus() {
        if (_status == null) return;
        if (Cfg == null) Cfg = AiConfig.Load();
        if (_busy) { Localization.Apply(_status, "处理中…"); _status.color = new Color(1f, 0.85f, 0.5f); return; }
        if (!Cfg.Ready) {
            Localization.Apply(_status, "未配置 AI 接口 —— 点「设置」填写 baseURL 与 API Key");
            _status.color = new Color(1f, 0.7f, 0.5f);
        } else {
            Localization.Apply(_status, $"已连接 {Cfg.model}"); _status.color = new Color(0.6f, 0.85f, 0.7f);
        }
    }

    /* ---------------- 输入框高度自适应 ---------------- */
    void Update() {
        if (_input == null || _inputText == null) return;
        float w = _inputRt.rect.width - 20f;
        if (w > 10f) {
            var settings = _inputText.GetGenerationSettings(new Vector2(w, 0f));
            settings.generateOutOfBounds = true;
            settings.horizontalOverflow = HorizontalWrapMode.Wrap;
            float pref = _inputText.cachedTextGeneratorForLayout.GetPreferredHeight(
                string.IsNullOrEmpty(_input.text) ? " " : _input.text, settings) / _inputText.pixelsPerUnit;
            float want = Mathf.Clamp(pref + 12f, InputMinH, InputMaxH);
            if (Mathf.Abs(want - _inputH) > 0.6f) {
                _inputH = want;
                _inputRt.sizeDelta = new Vector2(-72, _inputH);
                _inputRt.anchoredPosition = new Vector2(-46, 46);
            }
        }
        /* 回车（不带 Shift）发送 */
        if (_input.isFocused &&
            (LabInput.GetKeyDown(KeyCode.Return) || LabInput.GetKeyDown(KeyCode.KeypadEnter)) &&
            (LabInput.GetKey(KeyCode.LeftShift) || LabInput.GetKey(KeyCode.RightShift))) {
            Send();
        } else if (_input.isFocused &&
            (LabInput.GetKeyDown(KeyCode.Return) || LabInput.GetKeyDown(KeyCode.KeypadEnter))) {
            Send();
        }
    }

    /* ---------------- 发送 ---------------- */
    void Send() {
        if (_input == null) return;
        string msg = (_input.text ?? "").Trim();
        if (msg.Length == 0) return;
        _input.text = "";
        _inputH = InputMinH; _inputRt.sizeDelta = new Vector2(-72, InputMinH);
        Add(ChatMsg.Who.Me, msg);

        PetProgress.ChatUsed = true;
        if (Cfg == null) Cfg = AiConfig.Load();
        if (!Cfg.Ready) {
            Add(ChatMsg.Who.System,
                "还没配置 AI 接口。这是学生分发版的默认状态 —— AI 功能需要你自己填。\n" +
                "点标题栏的「设置」填入 baseURL 与 API Key 就能用了。\n" +
                "（宠物、教程、实验台、焓变计算器全都不受影响，都不需要联网。）", true);
            return;
        }
        StartCoroutine(Ask(msg));
    }

    IEnumerator Ask(string userMsg) {
        _busy = true; UpdateStatus();
        /* 她进入「专注处理」状态 */
        PetView.Instance?.Play("focus");

        var body = BuildBody(userMsg);
        var req = new UnityEngine.Networking.UnityWebRequest(
            Cfg.baseUrl.TrimEnd('/') + "/chat/completions", "POST");
        byte[] raw = Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(raw);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + Cfg.apiKey);
        req.timeout = Mathf.Max(10, Cfg.timeout);

        yield return req.SendWebRequest();

        _busy = false;
        PetView.Instance?.Play("review");
        string reply = null;
        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success) {
            reply = $"请求失败：{req.responseCode} {req.error}";
            PetView.Instance?.Play("fail");
            Add(ChatMsg.Who.System, reply, true);
        } else {
            reply = ExtractContent(req.downloadHandler.text);
            if (string.IsNullOrEmpty(reply)) {
                PetView.Instance?.Play("fail");
                Add(ChatMsg.Who.System, "接口返回了内容，但没解析出回复。", true);
            } else {
                Add(ChatMsg.Who.Anon, reply);
                PetView.Instance?.Play("wave");
            }
        }
        req.Dispose();
        UpdateStatus();
    }

    string BuildBody(string userMsg) {
        var sb = new StringBuilder();
        sb.Append("{\"model\":\"").Append(Esc(Cfg.model)).Append("\",\"messages\":[");
        /* 人格与「如何掌控本项目」全部来自 skill 文件 */
        sb.Append("{\"role\":\"system\",\"content\":\"").Append(Esc(SkillText())).Append("\"}");
        int from = Mathf.Max(0, History.Count - Mathf.Max(2, Cfg.maxHistory));
        for (int i = from; i < History.Count; i++) {
            var m = History[i];
            if (m.From == ChatMsg.Who.System) continue;
            sb.Append(",{\"role\":\"").Append(m.From == ChatMsg.Who.Me ? "user" : "assistant")
              .Append("\",\"content\":\"").Append(Esc(m.Text)).Append("\"}");
        }
        /* 最后一条一定是用户刚发的，避免重复 */
        if (History.Count == 0 || History[History.Count - 1].From != ChatMsg.Who.Me)
            sb.Append(",{\"role\":\"user\",\"content\":\"").Append(Esc(userMsg)).Append("\"}");
        sb.Append("],\"temperature\":0.4,\"stream\":false}");
        return sb.ToString();
    }

    static string _skill;
    public static string SkillText() {
        if (_skill != null) return _skill;
        var ta = Resources.Load<TextAsset>("anon_skill");
        if (ta != null) { _skill = ta.text; return _skill; }
        _skill = "你是 ChemLab 实验室里的助手阿侬。回答要简短、口语化，用中文。" +
                 "用户让你做实验时，先复述一遍你理解的任务（一句话），做完再报告结果（一句话）。" +
                 "不要描述中间过程、不要提改了哪些文件。";
        return _skill;
    }

    static string Esc(string s) {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length + 16);
        foreach (char c in s) {
            switch (c) {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append(' ');
                    else sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>从 OpenAI 格式的响应里抠出 message.content。
    /// 不引第三方 JSON 库，按字段名定位并手工处理转义。</summary>
    public static string ExtractContent(string json) {
        if (string.IsNullOrEmpty(json)) return null;
        int i = json.IndexOf("\"content\"", StringComparison.Ordinal);
        if (i < 0) return null;
        i = json.IndexOf(':', i); if (i < 0) return null;
        i++;
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        if (i >= json.Length || json[i] != '"') return null;
        i++;
        var sb = new StringBuilder();
        while (i < json.Length) {
            char c = json[i];
            if (c == '\\' && i + 1 < json.Length) {
                char d = json[i + 1];
                switch (d) {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': break;
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'u':
                        if (i + 5 < json.Length) {
                            int code;
                            if (int.TryParse(json.Substring(i + 2, 4),
                                    System.Globalization.NumberStyles.HexNumber,
                                    System.Globalization.CultureInfo.InvariantCulture, out code)) {
                                sb.Append((char)code);
                                i += 6; continue;
                            }
                        }
                        break;
                    default: sb.Append(d); break;
                }
                i += 2; continue;
            }
            if (c == '"') break;
            sb.Append(c); i++;
        }
        return sb.ToString();
    }

    /* ---------------- 设置 ---------------- */
    void ShowSettings() {
        PetMenu.BuildMenu(LabInput.mousePosition,
            new List<(string, Action)> {
                ("当前状态：" + (Cfg.Ready ? "已配置" : "未配置"), null),
                ("baseURL = " + (string.IsNullOrEmpty(Cfg.baseUrl) ? "(空)" : Cfg.baseUrl), null),
                ("model = " + Cfg.model, null),
                ("——", null),
                ("改为教师端默认（DeepSeek）", () => {
                    Cfg.baseUrl = "https://api.deepseek.com/v1";
                    Cfg.model = "deepseek-chat";
                    Cfg.Save(); UpdateStatus();
                    Add(ChatMsg.Who.System, "已切到 DeepSeek 端点。若还没有密钥，请在 Resources/ai_config.json 或这里补上 apiKey。", true);
                }),
                ("清空 API Key（学生分发模式）", () => {
                    Cfg.apiKey = ""; Cfg.isTeacher = false; Cfg.Save(); UpdateStatus();
                    Add(ChatMsg.Who.System, "已清空密钥，现在等同于学生分发版：AI 不可用，宠物与教程照常。", true);
                }),
                ("关闭", null),
            });
    }

    /* ---------------- 小工具 ---------------- */
    static Text MkText(RectTransform parent, string s, int size, Color col, TextAnchor anc,
                       Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax) {
        var go = new GameObject("T", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = LabUI.UiFont; t.fontSize = size; t.color = col; t.alignment = anc;
        Localization.Apply(t, s); t.raycastTarget = false; t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.offsetMin = omin; rt.offsetMax = omax;
        return t;
    }
    static Button MkBtn(RectTransform parent, string label, Action act, Vector2 pos, Vector2 size) {
        var go = new GameObject("B", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.20f, 0.28f, 0.38f, 1f);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        if (act != null) b.onClick.AddListener(() => act());
        var t = MkText(rt, label, 13, Color.white, TextAnchor.MiddleCenter,
                       Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return b;
    }
}

}
