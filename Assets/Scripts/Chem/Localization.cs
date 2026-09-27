/* =====================================================================
   本地化：中英双语切换
   用户要用英文学 A-Level，中文命名反而看不懂，所以界面 + 物质名 +
   现象描述都要能整体切换。
   译文表由 js/i18n_en.js 生成，这里只做查表。
   ===================================================================== */
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ChemLab.Chem;

namespace ChemLab.Chem {
public static class Localization {
    public static bool English;
    static readonly Dictionary<Text, string> SourceText = new Dictionary<Text, string>();
    static readonly Dictionary<Text, string> RenderedText = new Dictionary<Text, string>();
    static readonly List<Text> DeadText = new List<Text>();
    static readonly List<KeyValuePair<Text, string>> ChangedText = new List<KeyValuePair<Text, string>>();
    static readonly Dictionary<string, string> RuntimeTranslations = new Dictionary<string, string>();
    static readonly HashSet<Text> UserInputText = new HashSet<Text>();
    static readonly HashSet<Text> ProtectedText = new HashSet<Text>();
    static float _nextDiscovery;

    /// <summary>界面 / 现象文本：按中文原文查英文，查不到就原样返回</summary>
    public static string T(string zh) {
        if (!English || string.IsNullOrEmpty(zh)) return zh;
        // Console command echoes contain verbatim user input and must not be localized.
        if (zh.StartsWith("> ", System.StringComparison.Ordinal)) return zh;
        if (RuntimeTranslations.TryGetValue(zh, out var runtimeTranslation)) return runtimeTranslation;
        string en;
        if (ChemData.EnText.TryGetValue(zh, out en) && !string.IsNullOrEmpty(en)) return en;
        /* 物质名走 id 查表 */
        string id;
        if (ChemData.NameToId.TryGetValue(zh, out id)) {
            if (ChemData.EnSub.TryGetValue(id, out en) && !string.IsNullOrEmpty(en)) return en;
        }
        if (zh.IndexOf('\n') >= 0) {
            var lines = zh.Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = T(lines[i]);
            return string.Join("\n", lines);
        }
        if (TryTranslateDisplayFormat(zh, out var formatted)) return formatted;
        if (TryTranslatePressureLabel(zh, out formatted)) return formatted;
        if (TryTranslatePrefixedLine(zh, out var prefixed)) return prefixed;
        return TranslateVisibleTerms(zh);
    }

    static bool TryTranslatePressureLabel(string value, out string translated) {
        translated = null;
        const string absSuffix = " bar（绝压）";
        const string sealedPrefix = "密闭 · ";
        if (value.StartsWith(sealedPrefix, System.StringComparison.Ordinal) && value.EndsWith(absSuffix, System.StringComparison.Ordinal)) {
            var pressure = value.Substring(sealedPrefix.Length, value.Length - sealedPrefix.Length - absSuffix.Length);
            translated = "Sealed · " + pressure + " bar (absolute)";
            return true;
        }
        const string tagPrefix = "  密闭 ";
        const string barSuffix = " bar";
        if (value.StartsWith(tagPrefix, System.StringComparison.Ordinal) && value.EndsWith(barSuffix, System.StringComparison.Ordinal)) {
            var pressure = value.Substring(tagPrefix.Length, value.Length - tagPrefix.Length - barSuffix.Length);
            translated = "  Sealed " + pressure + " bar";
            return true;
        }
        return false;
    }

    /// <summary>Translate only known application-authored chat strings. User prompts and model replies stay verbatim.</summary>
    public static string TChatMessage(string source) {
        if (!English || string.IsNullOrEmpty(source)) return source;
        if (RuntimeTranslations.TryGetValue(source, out var exact)) return exact;
        ChemData.EnsureBuilt();
        if (ChemData.EnText.TryGetValue(source, out exact) && !string.IsNullOrEmpty(exact)) return exact;
        const string failed = "请求失败：";
        if (source.StartsWith(failed, System.StringComparison.Ordinal)) return "Request failed: " + source.Substring(failed.Length);
        const string connected = "已连接 ";
        if (source.StartsWith(connected, System.StringComparison.Ordinal)) return "Connected · " + source.Substring(connected.Length);
        return source;
    }

    /// <summary>Keep free-form user or model content out of automatic UI localization.</summary>
    public static void ProtectFromTranslation(Text text) {
        if (text == null) return;
        ProtectedText.Add(text);
        SourceText.Remove(text);
        RenderedText.Remove(text);
    }

    static bool TryTranslateDisplayFormat(string value, out string translated) {
        translated = null;
        int molecular = value.IndexOf("  具体物质", System.StringComparison.Ordinal);
        if (molecular >= 0) { translated = T(value.Substring(0, molecular)) + "  Molecular"; return true; }
        int ionic = value.IndexOf("  离子状态", System.StringComparison.Ordinal);
        if (ionic >= 0) { translated = T(value.Substring(0, ionic)) + "  Ionic"; return true; }
        if (value.StartsWith("圆底烧瓶", System.StringComparison.Ordinal) && value.EndsWith("mL", System.StringComparison.Ordinal)) {
            translated = "Round flask · " + value.Substring("圆底烧瓶".Length).Trim(); return true;
        }
        if (value.StartsWith("放置了一个", System.StringComparison.Ordinal)) {
            translated = "Placed a " + T(value.Substring("放置了一个".Length)); return true;
        }
        if (value.StartsWith("加入 ", System.StringComparison.Ordinal)) {
            string rest = value.Substring(3);
            int arrow = rest.IndexOf(" → ", System.StringComparison.Ordinal);
            if (arrow >= 0) { translated = "Add " + T(rest.Substring(0, arrow)) + " to " + T(rest.Substring(arrow + 3)); return true; }
            int unit = rest.LastIndexOf(" mol", System.StringComparison.Ordinal);
            string suffix = " mol";
            if (unit < 0) { unit = rest.LastIndexOf(" g", System.StringComparison.Ordinal); suffix = " g"; }
            if (unit > 0) {
                int split = rest.LastIndexOf(' ', unit - 1);
                if (split > 0) translated = "Added " + T(rest.Substring(0, split)) + " " + rest.Substring(split + 1);
                else translated = "Added " + T(rest.Substring(0, unit)) + " " + suffix.Trim();
                return true;
            }
        }
        if (value.StartsWith("火力 ", System.StringComparison.Ordinal)) { translated = "Flame: " + value.Substring(3); return true; }
        if (value.StartsWith("已加入 ", System.StringComparison.Ordinal)) { translated = "Added " + T(value.Substring("已加入 ".Length)); return true; }
        if (value.StartsWith("倾倒量 ", System.StringComparison.Ordinal) && value.EndsWith("%", System.StringComparison.Ordinal)) {
            translated = "Pour amount: " + value.Substring("倾倒量 ".Length); return true;
        }
        const string pourRollback = "；已恢复倾倒前状态，请重新选量";
        if (value.EndsWith(pourRollback, System.StringComparison.Ordinal)) {
            translated = T(value.Substring(0, value.Length - pourRollback.Length)) + "; the pre-pour state was restored. Please select the amount again";
            return true;
        }
        if (value == "本生灯 已点燃") { translated = "Bunsen burner · Lit"; return true; }
        if (value == "本生灯 未点燃") { translated = "Bunsen burner · Unlit"; return true; }
        if (value.StartsWith("已点燃 · 拖动滑条立即生效\n", System.StringComparison.Ordinal)) {
            translated = "Lit · Drag the slider to adjust immediately\n" + T(value.Substring("已点燃 · 拖动滑条立即生效\n".Length)); return true;
        }
        if (value.StartsWith("未点燃 · 拖动滑条立即生效\n", System.StringComparison.Ordinal)) {
            translated = "Unlit · Drag the slider to adjust immediately\n" + T(value.Substring("未点燃 · 拖动滑条立即生效\n".Length)); return true;
        }
        if ((value.StartsWith("回流：收集 ", System.StringComparison.Ordinal) || value.StartsWith("蒸馏：收集 ", System.StringComparison.Ordinal))) {
            bool reflux = value.StartsWith("回流：", System.StringComparison.Ordinal);
            string rest = value.Substring(5);
            const string mid = " mL，回流 ";
            int at = rest.IndexOf(mid, System.StringComparison.Ordinal);
            if (at >= 0) {
                int tail = rest.IndexOf(" mL；", at + mid.Length, System.StringComparison.Ordinal);
                if (tail >= 0) translated = (reflux ? "Reflux" : "Distillation") + ": collected " + rest.Substring(0, at)
                    + " mL, returned " + rest.Substring(at + mid.Length, tail - at - mid.Length) + " mL; " + T(rest.Substring(tail + 5));
                return translated != null;
            }
        }
        const string attachedAbove = " 吸附在 ";
        int attachedAt = value.IndexOf(attachedAbove, System.StringComparison.Ordinal);
        if (attachedAt >= 0 && value.EndsWith(" 上方", System.StringComparison.Ordinal)) {
            translated = value.Substring(0, attachedAt) + " attached above " + T(value.Substring(attachedAt + attachedAbove.Length, value.Length - attachedAt - attachedAbove.Length - 3));
            return true;
        }
        if (value.StartsWith("液体 ", System.StringComparison.Ordinal) && value.Contains("固体 ")) {
            translated = value.Replace("液体", "Liquid").Replace("固体", "Solid"); return true;
        }
        if (value.StartsWith("回路：", System.StringComparison.Ordinal)) { translated = "Cycle: " + value.Substring("回路：".Length); return true; }
        const string missingOrganicModel = "该有机转换缺少已配平的产物模型：";
        if (value.StartsWith(missingOrganicModel, System.StringComparison.Ordinal)) {
            string conversion = value.Substring(missingOrganicModel.Length);
            int arrow = conversion.IndexOf(" → ", System.StringComparison.Ordinal);
            if (arrow >= 0) {
                translated = "No balanced product model is available for this organic conversion: "
                    + T(conversion.Substring(0, arrow)) + " → " + T(conversion.Substring(arrow + 3));
                return true;
            }
        }
        if (value.StartsWith("查表：", System.StringComparison.Ordinal)) {
            var parts = value.Substring("查表：".Length).Split(new[]{"（"}, 2, System.StringSplitOptions.None);
            if (parts.Length == 2 && parts[1].EndsWith("）", System.StringComparison.Ordinal)) {
                translated = "Lookup: " + parts[0] + " (" + T(parts[1].Substring(0, parts[1].Length - 1)) + ")";
                return true;
            }
        }
        const string manualValuePrefix = "已设为手动值 ΔH = ";
        if (value.StartsWith(manualValuePrefix, System.StringComparison.Ordinal)) {
            translated = "Set manually: ΔH = " + value.Substring(manualValuePrefix.Length);
            return true;
        }
        const string invalidNumber = "」不是有效数字，已忽略";
        if (value.StartsWith("「", System.StringComparison.Ordinal) && value.EndsWith(invalidNumber, System.StringComparison.Ordinal)) {
            translated = "“" + value.Substring(1, value.Length - 1 - invalidNumber.Length) + "” is not a valid number; ignored";
            return true;
        }
        if (TryTranslatePureThermalNotice(value, out translated)) return true;
        if (value.StartsWith("纯甲醇按焓守恒", System.StringComparison.Ordinal) || value.StartsWith("纯乙醇按焓守恒", System.StringComparison.Ordinal)) {
            bool ethanol = value.StartsWith("纯乙醇", System.StringComparison.Ordinal);
            string rest = value.Substring(2);
            rest = rest.Replace("按焓守恒计算", "is calculated by enthalpy conservation")
                       .Replace("按焓守恒混温", "is mixed using enthalpy conservation")
                       .Replace("有界变热容液相焓", "bounded variable-heat-capacity liquid enthalpy")
                       .Replace("用于0–64.55℃的定热容教学近似", "a constant-heat-capacity teaching approximation for 0–64.55 °C")
                       .Replace("未计玻璃蓄热", "glass heat storage is not included");
            translated = (ethanol ? "Pure ethanol " : "Pure methanol ") + rest; return true;
        }
        if (value.StartsWith("打开冷凝水（", System.StringComparison.Ordinal) && value.EndsWith("℃）", System.StringComparison.Ordinal)) {
            translated = "Turn on coolant (" + value.Substring("打开冷凝水（".Length, value.Length - "打开冷凝水（".Length - 2) + " °C)"; return true;
        }
        if (value == "新建250 mL锥形接收瓶") { translated = "New 250 mL conical receiver"; return true; }
        const string continueTutorial = "▶ 继续教程（";
        if (value.StartsWith(continueTutorial, System.StringComparison.Ordinal) && value.EndsWith("）", System.StringComparison.Ordinal)) {
            translated = "▶ Continue tutorial (" + T(value.Substring(continueTutorial.Length, value.Length - continueTutorial.Length - 1)) + ")";
            return true;
        }
        if (value.StartsWith("滴定管：", System.StringComparison.Ordinal) || value.StartsWith("移液管：", System.StringComparison.Ordinal)
            || value.StartsWith("burette：", System.StringComparison.Ordinal) || value.StartsWith("pipette：", System.StringComparison.Ordinal)) {
            int colon = value.IndexOf('：'); translated = T(value.Substring(0, colon)) + ": " + value.Substring(colon + 1); return true;
        }
        const string noTarget = "请拖到目标器皿上方";
        if (value.Contains(noTarget)) translated = value.Replace(noTarget, "Drag over a target vessel");
        if (translated != null) return true;
        if (value.EndsWith("（气体）", System.StringComparison.Ordinal) || value.EndsWith("(气体)", System.StringComparison.Ordinal)) {
            int suffix = value.LastIndexOf('（'); if (suffix < 0) suffix = value.LastIndexOf('(');
            translated = T(value.Substring(0, suffix)) + " (gas)"; return true;
        }
        if (value.EndsWith("（液体）", System.StringComparison.Ordinal)) { translated = T(value.Substring(0, value.Length - 4)) + " (liquid)"; return true; }
        if (value.EndsWith("（25℃气体）", System.StringComparison.Ordinal)) { translated = T(value.Substring(0, value.Length - 7)) + " (gas at 25 °C)"; return true; }
        const string sat = " 超过当前条件下的饱和量：";
        int satAt = value.IndexOf(sat, System.StringComparison.Ordinal);
        if (satAt > 0) {
            var species = T(value.Substring(0, satAt));
            var outcome = value.Substring(satAt + sat.Length);
            outcome = outcome == "过量气体已逸散" ? "excess gas escaped"
                : outcome == "过量部分转入气相，尚未逸散" ? "excess transferred to the gas phase and has not escaped"
                : outcome == "仅溶解可溶部分，余量以固体保留" ? "only the soluble portion dissolved; the remainder remains solid"
                : T(outcome);
            translated = species + " exceeds its solubility under current conditions: " + outcome;
            return true;
        }
        if (value.StartsWith("标称容量 ", System.StringComparison.Ordinal)) {
            translated = "Capacity " + value.Substring("标称容量 ".Length);
            return true;
        }
        if (value == "未选择容器") { translated = "No vessel selected"; return true; }
        if (value.StartsWith("沉 ", System.StringComparison.Ordinal)) { translated = "ppt " + T(value.Substring(2)); return true; }
        if (value.StartsWith("固 ", System.StringComparison.Ordinal)) { translated = "solid " + T(value.Substring(2)); return true; }
        if (value.StartsWith("气 ", System.StringComparison.Ordinal)) { translated = "gas " + T(value.Substring(2)); return true; }
        if (value == "实验台已就绪。左侧选器材；从右侧拖试剂到器皿，松开后输入数量并确认加入。") {
            translated = "The lab is ready. Select apparatus on the left, then drag a reagent from the right into a vessel. Release it, enter an amount, and confirm to add it.";
            return true;
        }
        return false;
    }

    static bool TryTranslatePureThermalNotice(string value, out string translated) {
        translated = null;
        if (!value.StartsWith("纯", System.StringComparison.Ordinal)) return false;
        int calcAt = value.IndexOf("按焓守恒", System.StringComparison.Ordinal);
        if (calcAt > 1) {
            string name = value.Substring(1, calcAt - 1);
            string rest = value.Substring(calcAt);
            bool mixing = rest.StartsWith("按焓守恒混温；", System.StringComparison.Ordinal);
            const string tail = "；未计玻璃蓄热";
            int tailAt = rest.IndexOf(tail, System.StringComparison.Ordinal);
            if (tailAt < 0) return false;
            int semicolon = rest.IndexOf('；');
            string assumption = rest.Substring(semicolon + 1, tailAt - semicolon - 1);
            translated = "Pure " + T(name) + (mixing ? " is mixed by enthalpy conservation; " : " is calculated by enthalpy conservation; ")
                + T(assumption) + "; glass heat storage is not included";
            return true;
        }
        const string transientMarker = "暂态冷凝：";
        const string energyMarker = "能量冷凝：";
        int markerAt = value.IndexOf(transientMarker, System.StringComparison.Ordinal);
        bool transient = markerAt >= 0;
        string marker = transient ? transientMarker : energyMarker;
        if (markerAt < 0) markerAt = value.IndexOf(marker, System.StringComparison.Ordinal);
        if (markerAt < 0) return false;
        string liquid = value.Substring(1, markerAt - 1);
        string details = value.Substring(markerAt + marker.Length);
        if (transient) {
            int separator = details.IndexOf('；');
            if (separator < 0) return false;
            translated = "Pure " + T(liquid) + " transient condensation: " + T(details.Substring(0, separator))
                + "; wall/jacket storage, constant glass heat capacity, and heat-transfer coefficients are teaching approximations";
            return true;
        }
        const string separatorText = "，常压饱和蒸气；";
        int split = details.IndexOf(separatorText, System.StringComparison.Ordinal);
        if (split < 0) return false;
        translated = "Pure " + T(liquid) + " energy-based condensation: " + T(details.Substring(0, split))
            + "; atmospheric-pressure saturated vapor; power and UA are teaching settings, the outer wall is adiabatic, and glass heat storage is omitted";
        return true;
    }

    public static void RegisterTranslation(string original, string english) {
        if (!string.IsNullOrEmpty(original) && !string.IsNullOrEmpty(english))
            RuntimeTranslations[original] = english;
    }

    public static void RegisterAiUiTranslations() {
        RegisterTranslation("阿侬 · 助手", "Anon · Assistant");
        RegisterTranslation("设置", "Settings");
        RegisterTranslation("清空", "Clear");
        RegisterTranslation("发送", "Send");
        RegisterTranslation("收起", "Hide");
        RegisterTranslation("处理中…", "Working…");
        RegisterTranslation("未配置 AI 接口 —— 点「设置」填写 baseURL 与 API Key", "AI is not configured — open Settings to enter the base URL and API key.");
        RegisterTranslation("当前状态：已配置", "Status: configured");
        RegisterTranslation("当前状态：未配置", "Status: not configured");
        RegisterTranslation("已连接 {model}", "Connected · {model}");
        RegisterTranslation("改为教师端默认（DeepSeek）", "Teacher defaults (DeepSeek)");
        RegisterTranslation("清空 API Key（学生分发模式）", "Clear API key (student mode)");
        RegisterTranslation("关闭", "Close");
        RegisterTranslation("已切到 DeepSeek 端点。若还没有密钥，请在 Resources/ai_config.json 或这里补上 apiKey。", "Switched to the DeepSeek endpoint. If no key is configured, add apiKey here or in Resources/ai_config.json.");
        RegisterTranslation("已清空密钥，现在等同于学生分发版：AI 不可用，宠物与教程照常。", "The key was cleared. This is now equivalent to the student build: AI is unavailable, while the pet and tutorials remain usable.");
        RegisterTranslation("我是阿侬。想要什么实验、想算什么，直接说就行。", "I'm Anon. Tell me what experiment you'd like to do or what you'd like to calculate.");
        RegisterTranslation("我是阿侬。不过还没配置 AI 接口，先点右上角「设置」把地址和密钥填上吧。\n（教程和宠物功能不受影响，随时能用）", "I'm Anon. The AI endpoint is not configured yet. Use Settings at the top right to enter the address and key.\n(Tutorials and pet features work without it.)");
        RegisterTranslation("还没配置 AI 接口。这是学生分发版的默认状态 —— AI 功能需要你自己填。\n点标题栏的「设置」填入 baseURL 与 API Key 就能用了。\n（宠物、教程、实验台、焓变计算器全都不受影响，都不需要联网。）", "The AI endpoint is not configured. This is the default for student builds; enter a base URL and API key in Settings to enable AI.\nThe pet, tutorials, lab, and enthalpy calculator work offline and are unaffected.");
        RegisterTranslation("请求失败：", "Request failed:");
        RegisterTranslation("接口返回了内容，但没解析出回复。", "The endpoint returned a response, but no reply could be parsed.");
        RegisterTranslation("已配置", "Configured");
        RegisterTranslation("未配置", "Not configured");
        RegisterTranslation("⏭ 跳过教程并解锁 AI 助手", "⏭ Skip tutorial and unlock AI assistant");
        RegisterTranslation("⏭ 直接跳过教程并解锁 AI 助手", "⏭ Skip ahead and unlock AI assistant");
        RegisterTranslation("打开聊天（AI 助手）", "Open chat (AI assistant)");
        RegisterTranslation("回到右下角", "Return to the lower-right corner");
        RegisterTranslation("聊天还没解锁 —— 先把教程走完，或者右键我选「跳过教程」。\n（我不是故意为难你，教程里教的都是这软件的用法，你会用得上。）", "Chat is still locked. Finish the tutorial, or right-click me and choose “Skip tutorial.”\n(I promise I'm not trying to make things difficult; the tutorial covers useful parts of this app.)");
        RegisterTranslation("聊天要等你把教程走完、或者跳过之后才解锁哦。\n右键我再选一次「跳过教程」就可以立刻用。", "Chat unlocks when you finish or skip the tutorial.\nRight-click me and choose “Skip tutorial” to unlock it now.");
        RegisterTranslation("好，那 AI 助手给你解锁了。想学的话随时右键我。", "All right, the AI assistant is unlocked. Right-click me any time if you'd like to learn.");
        RegisterTranslation("好，我回来啦。", "I'm back!");
        RegisterTranslation("需要我帮忙搭个实验吗？", "Would you like me to set up an experiment?");
        RegisterTranslation("右键我可以重新开始教程～", "Right-click me to restart the tutorial~");
        RegisterTranslation("焓变计算器在顶栏，要不要试试？", "The enthalpy calculator is in the top bar. Want to try it?");
        RegisterTranslation("我在这儿陪着你就好。", "I'll stay here with you.");
        RegisterTranslation("点住我可以把我拖到别处哦。", "Click and hold to drag me somewhere else.");
        RegisterTranslation("▶ 开始教程 · 离子鉴定实验", "▶ Start tutorial · Ion identification");
        RegisterTranslation("重新开始教程 · 离子鉴定实验", "Restart tutorial · Ion identification");
        RegisterTranslation("重新开始教程 · 焓变计算器", "Restart tutorial · Enthalpy calculator");
        RegisterTranslation("🔒 聊天（教程完成后解锁）", "🔒 Chat (unlocks after tutorial)");
        RegisterTranslation("变小一点", "Make smaller");
        RegisterTranslation("变大一点", "Make larger");
        RegisterTranslation("先躲起来", "Hide for now");
        RegisterTranslation("出气量太小，点不着", "Gas flow is too low to light the burner.");
        RegisterTranslation("无效的物质或数量", "Invalid substance or amount.");
        RegisterTranslation("容器已密闭，请先打开再加入液体或固体；气体可经模拟进气口加入", "The vessel is sealed. Unseal it before adding a liquid or solid; gas can be added through the simulated inlet.");
        RegisterTranslation("该条目只能以溶液形式加入，请先放入溶剂", "This reagent can only be added as a solution. Add a solvent first.");
        RegisterTranslation("已通入气体；当前溶剂/温压缺少该气体的定量溶解模型，未假定完全溶解", "Gas was admitted. No quantitative dissolution model is available for this gas in the current solvent, temperature, and pressure; complete dissolution is not assumed.");
        RegisterTranslation("当前溶剂/温压缺少该物质的定量溶解度，暂保留固体，未假定完全溶解", "Quantitative solubility data for this substance are unavailable for the current solvent, temperature, and pressure. The solid is retained; complete dissolution is not assumed.");
        RegisterTranslation("已吸附，可使用专用量器面板", "Attached. Use the dedicated volumetric-tool panel.");
        RegisterTranslation("WASD 移动 · 拖空白转视角 · 拽物品移动 · Shift 拖动升降 · E 熄木条 · F 开关本生灯", "WASD move · drag empty space to rotate view · drag objects to move · hold Shift while dragging to raise/lower · E extinguishes splints · F toggles the Bunsen burner");
        RegisterTranslation("Esc 关闭，保留当前设置。", "Esc closes the panel and keeps the current setting.");
        RegisterTranslation("已点燃 · 拖动滑条立即生效", "Lit · slider changes apply immediately");
        RegisterTranslation("未点燃 · 拖动滑条立即生效", "Unlit · slider changes apply immediately");
        RegisterTranslation("回流：收集 ", "Reflux: collected ");
        RegisterTranslation("蒸馏：收集 ", "Distillation: collected ");
        RegisterTranslation("▶ 继续教程（", "▶ Continue tutorial (");
        RegisterTranslation("↻ 继续教程", "↻ Continue tutorial");
        RegisterTranslation("在此新建节点", "Add node here");
        RegisterTranslation("编辑化学式", "Edit formula");
        RegisterTranslation("编辑系数", "Edit coefficient");
        RegisterTranslation("编辑焓变值", "Edit enthalpy change");
        RegisterTranslation("输入这条连线的焓变 ΔH（kJ/mol）", "Enter ΔH (kJ/mol)");
        RegisterTranslation("手动输入…", "Enter manually…");
        RegisterTranslation("设为未知量（待求）", "Set as unknown");
        RegisterTranslation("取消（撤销这条线）", "Cancel (remove this edge)");
        RegisterTranslation("已撤销这条连线。", "Edge removed.");
        RegisterTranslation("已取消编辑。", "Edit cancelled.");
        RegisterTranslation("还没有连线。", "There are no edges yet.");
        RegisterTranslation("已清空。", "Cleared.");
        RegisterTranslation("返回实验台", "Return to lab");
    }

    public static void RegisterChemUiTranslations() {
        RegisterTranslation("氨/铵、乙酸/乙酸根、苯胺/苯胺鎓定量平衡目前覆盖 25℃ 水溶剂；当前条件未覆盖，保留物质量", "Quantitative equilibrium for ammonia/ammonium, acetic acid/acetate, and aniline/anilinium is currently supported only in aqueous solution at 25 °C. Current conditions are outside this range; inventory is retained.");
        RegisterTranslation("密闭氨体系超出当前稀溶液近似（0.1 mol/L）；保留气液物质量，压力仍按 PV=nRT 计算", "The sealed ammonia system is outside the current dilute-solution approximation (0.1 mol/L). Gas and liquid inventories are retained; pressure is still calculated using PV = nRT.");
        RegisterTranslation("当前混合溶剂/温压缺少该固体的定量溶解度，暂保留固体，未假定完全溶解", "Quantitative solubility data for this solid are unavailable for the current solvent mixture, temperature, and pressure. The solid is retained; complete dissolution is not assumed.");
        RegisterTranslation("冷凝传热输入必须是有限数值", "Condenser heat-transfer inputs must be finite.");
        RegisterTranslation("冷凝传热需要非负流量/热导、正热容/潜热，以及不低于饱和温度的入口蒸气", "Condenser heat transfer requires non-negative flow and conductance, positive heat capacity and latent heat, and vapor entering at or above its saturation temperature.");
        RegisterTranslation("液相焓曲线必须覆盖冷却剂入口至饱和温度，禁止超出数据范围外推", "The liquid enthalpy curve must cover the range from coolant inlet to saturation temperature; extrapolation beyond the data is not allowed.");
        RegisterTranslation("可用蒸气热量必须为非负有限值", "Available vapor energy must be finite and non-negative.");
        RegisterTranslation("指定壁面供热时须禁用热侧热导，且热量有限、零步长不注入热量", "When wall heating is specified, hot-side conductance must be disabled. Energy must be finite, and a zero-duration step must add no heat.");
        RegisterTranslation("冷凝器热惯性参数必须为有限数值", "Condenser thermal-inertia parameters must be finite.");
        RegisterTranslation("热容必须为正；时间、流量热容率与热导非负，温度高于绝对零度", "Heat capacity must be positive; time, flow heat-capacity rate, and conductance must be non-negative; temperatures must be above absolute zero.");
        RegisterTranslation("冷凝器热惯性计算超出数值范围；未修改状态", "Condenser thermal-inertia calculation exceeded the numeric range; state was not modified.");
        RegisterTranslation("内容物已占满内腔，无法应用气相空间模型；保留物质量并暂停压力平衡，请打开并减量", "The contents fill the vessel, so a headspace model cannot be applied. Inventory is retained and pressure equilibration is paused; unseal the vessel and reduce the amount.");
        RegisterTranslation("气体分压超出已验证的稀溶液溶解度范围；保留物质量，暂停该气体的溶解平衡；压力仍按 PV=nRT 计算", "The gas partial pressure is outside the validated dilute-solution solubility range. Inventory is retained and dissolution equilibrium for that gas is paused; pressure is still calculated using PV = nRT.");
        RegisterTranslation("冷凝器尺寸无效", "Invalid condenser dimensions.");
        RegisterTranslation("热态冷凝器尺寸改变，请重新连接装置再运行", "Condenser dimensions changed while hot. Reconnect the apparatus before running it again.");
        RegisterTranslation("器皿蓄热暂仅支持玻璃主体，不将玻璃物性套用于陶瓷", "Vessel heat storage currently supports glass bodies only; glass properties are not applied to ceramic vessels.");
        RegisterTranslation("器皿蓄热缺少当前混合物液相焓数据", "Liquid-phase enthalpy data for the current mixture are unavailable for vessel heat storage.");
        RegisterTranslation("玻璃蓄热当前仅支持0–100℃", "Glass heat storage currently supports 0–100 °C only.");
        RegisterTranslation("当前能量冷凝需要0–100℃纯水源瓶/接收瓶", "Energy-based condensation currently requires pure-water source and receiver vessels at 0–100 °C.");
        RegisterTranslation("源瓶与接收瓶需为同种已登记纯液体，或使用空接收瓶", "The source and receiver must contain the same registered pure liquid, or the receiver must be empty.");
        RegisterTranslation("水/乙醇混温使用近室温热容近似，未计混合焓与热容温变", "Water/ethanol mixing uses a near-room-temperature heat-capacity approximation; enthalpy of mixing and temperature-dependent heat capacity are omitted.");
        RegisterTranslation("联合冷凝使用蒸气侧热导，不能再叠加第二份壁面热源", "Combined condensation uses vapor-side conductance; a second wall heat source cannot be added.");
        RegisterTranslation("联合冷凝热量超出有限范围", "Combined condensation energy exceeded the finite range.");
        RegisterTranslation("联合冷凝热流未收敛，未提交状态", "Combined condenser heat flow did not converge; state was not committed.");
        RegisterTranslation("器皿相变换热输入无效", "Invalid vessel phase-change heat-transfer input.");
        RegisterTranslation("有限液体热预算溢出", "Finite liquid heat budget overflowed.");
        RegisterTranslation("器皿相变换热解溢出", "Vessel phase-change heat-transfer solution overflowed.");
        RegisterTranslation("器皿换热输入无效或超出液相焓曲线范围", "Invalid vessel heat-transfer input or outside the liquid enthalpy-curve range.");
        RegisterTranslation("器皿换热能量超出有限范围", "Vessel heat-transfer energy exceeded the finite range.");
        RegisterTranslation("器皿换热解超出有限范围", "Vessel heat-transfer solution exceeded the finite range.");
        RegisterTranslation("有界变热容液相焓", "bounded variable-heat-capacity liquid enthalpy");
        RegisterTranslation("25℃热容用于0–64.55℃的定热容教学近似", "25 °C heat capacity used as a constant-Cp teaching approximation for 0–64.55 °C");
    }

    static bool TryTranslatePrefixedLine(string value, out string translated) {
        translated = null;
        if (value.StartsWith("<color=", System.StringComparison.Ordinal)) {
            int openEnd = value.IndexOf('>');
            int close = value.LastIndexOf("</color>", System.StringComparison.Ordinal);
            if (openEnd > 0 && close > openEnd) {
                translated = value.Substring(0, openEnd + 1)
                    + T(value.Substring(openEnd + 1, close - openEnd - 1))
                    + value.Substring(close);
                return true;
            }
        }
        int recordMarker = value.IndexOf("s   ", System.StringComparison.Ordinal);
        if (recordMarker > 0 && recordMarker < 12) {
            int body = recordMarker + 4;
            string prefix = value.Substring(0, body);
            if (body + 1 < value.Length && value[body] == '#') {
                int idEnd = value.IndexOf("  ", body, System.StringComparison.Ordinal);
                if (idEnd >= 0 && idEnd - body < 20) {
                    prefix += value.Substring(body, idEnd + 2 - body);
                    body = idEnd + 2;
                }
            }
            if (body + 1 < value.Length && value[body] == '▸' && value[body + 1] == ' ') {
                prefix += "▸ "; body += 2;
            }
            translated = prefix + T(value.Substring(body));
            return true;
        }
        // Developer-console lines carry a timestamp/severity prefix. Translate the
        // user-facing message while preserving the original diagnostic framing.
        if (value.Length > 12 && value[0] == '[') {
            int close = value.IndexOf("] ", System.StringComparison.Ordinal);
            if (close > 0 && close < 48) {
                int next = value.IndexOf("] ", close + 2, System.StringComparison.Ordinal);
                if (next > 0 && next < 80) close = next;
                translated = value.Substring(0, close + 2) + T(value.Substring(close + 2));
                return true;
            }
        }
        return false;
    }

    // Explicit display vocabulary used inside formatted UI strings. This is a
    // bounded terminology map, not character-level replacement: chemical names,
    // IDs, equations, and unrelated diagnostic text remain intact.
    static string TranslateVisibleTerms(string value) {
        string[,] terms = {
            {"已停止", "Stopped"}, {"加热输入", "Heat input"}, {"器皿环境散热", "Vessel heat loss"},
            {"密闭", "Sealed"}, {"绝压", "absolute pressure"},
            {"冷却水带走", "Removed by coolant"}, {"壁面 / 水套储热", "Wall / jacket storage"},
            {"器皿蓄热", "Vessel heat storage"},
            {"圆底烧瓶", "round-bottom flask"}, {"蒸发皿", "evaporating dish"},
            {"锥形瓶", "conical flask"}, {"烧杯", "beaker"}, {"试管", "test tube"},
            {"量筒", "measuring cylinder"}, {"滴定管", "burette"}, {"移液管", "pipette"},
            {"坩埚", "crucible"}, {"试剂瓶", "reagent bottle"}, {"容器", "vessel"},
            {"本生灯", "Bunsen burner"}, {"三脚架", "tripod"},
            {"蒸馏套件", "distillation kit"}, {"回流套件", "reflux kit"},
            {"石蕊试纸", "litmus paper"}, {"木条", "splint"},
            {"物质的量", "amount of substance"}, {"实验记录", "experiment log"},
            {"试剂架", "reagent shelf"}, {"器材架", "apparatus shelf"},
            {"输入数量", "enter an amount"}, {"液面", "liquid level"},
            {"温度", "temperature"}, {"倾倒", "pour"}, {"吸附", "attached"},
            {"调节火力", "adjust flame"}, {"检验器材", "test tool"},
            {"具体/离子", "molecular / ionic"}, {"具体物质", "molecular substances"},
            {"离子状态", "ionic species"}, {"离子式", "ionic equation"},
            {"方程式", "equation"}, {"标签", "labels"}, {"浓度", "concentration"},
            {"全部详情", "all details"}, {"选中详情", "selected details"},
            {"物质", "species"}, {"加热", "heat"}, {"搅拌", "stir"},
            {"点燃", "ignite"}, {"熄灭", "extinguish"}, {"倒空", "empty"},
            {"移走", "remove"}, {"湿润", "moisten"}, {"伸入", "insert"},
            {"抽取", "aspirate"}, {"释放", "dispense"}, {"停止", "stop"},
            {"撤销", "undo"}, {"取消", "cancel"}, {"完成操作", "finish"},
            {"预览", "preview"}, {"查看", "view"}, {"装置", "apparatus"},
            {"状态", "status"}, {"设置", "settings"}, {"传热", "heat transfer"},
            {"冷凝水", "coolant"}, {"打开", "open"}, {"关闭", "close"},
            {"运行中", "Running"}, {"已停止", "Stopped"}, {"管长", "Length"},
            {"加热输入", "Heat input"}, {"冷却水带走", "Removed by coolant"},
            {"壁面 / 水套储热", "Wall / jacket storage"}, {"环境散热", "Ambient heat loss"},
            {"壁温", "Wall temp"}, {"水套", "jacket"}, {"器皿蓄热", "Vessel heat storage"},
            {"器皿玻璃储热", "Vessel glass storage"},
            {"器皿环境散热", "Vessel heat loss"}, {"源瓶壁", "Source wall"},
            {"接收壁", "Receiver wall"}, {"逸散", "Escaped"}, {"入口", "inlet"},
            {"冷凝器", "condenser"}, {"温度计", "thermometer"}, {"蒸馏", "distillation"},
            {"回流", "reflux"}, {"收集", "collected"}, {"设置", "settings"},
            {"基准功率", "Baseline power"}, {"冷却水流量", "Coolant flow"},
            {"冷却水入口", "Coolant inlet"}, {"总热导", "Overall conductance"},
            {"UA", "UA"}, {"传热", "heat transfer"}, {"下进上出", "bottom-in, top-out"},
            {"已启用", "enabled"}, {"未启用", "disabled"}, {"尚未", "not yet"},
            {"传热教学参数", "Heat-transfer teaching parameter"}, {"热量账本", "heat ledger"},
            {"凝结", "condensation"}, {"冷凝", "condensation"}, {"水套体积", "water-jacket volume"},
            {"求解", "solve"}, {"未知焓变", "unknown enthalpy change"},
            {"焓变", "enthalpy change"}, {"新建节点", "add node"},
            {"编辑", "edit"}, {"化学式", "formula"}, {"系数", "coefficient"},
            {"复制节点", "duplicate node"}, {"删除节点", "delete node"},
            {"删除连线", "delete edge"}, {"手动输入", "enter manually"},
            {"查表", "lookup"}, {"设为未知量", "set as unknown"},
            {"至少要", "need at least"}, {"没有找到闭合回路", "No closed cycle found"}
        };
        for (int i = 0; i < terms.GetLength(0); i++)
            value = value.Replace(terms[i, 0], terms[i, 1]);
        return value;
    }

    public static void Register(Text text, string original) {
        if (text == null) return;
        SourceText[text] = original ?? "";
        Apply(text, original);
    }

    public static void Apply(Text text, string original) {
        if (text == null) return;
        var source = original ?? "";
        SourceText[text] = source;
        var localized = T(source);
        if (text.text != localized) text.text = localized;
        RenderedText[text] = localized;
    }

    public static void RefreshTrackedTexts() {
        RefreshInputFieldCache();
        foreach (var pair in SourceText) {
            if (pair.Key == null) continue;
            if (UserInputText.Contains(pair.Key) || ProtectedText.Contains(pair.Key)) continue;
            var localized = T(pair.Value);
            if (pair.Key.text != localized) pair.Key.text = localized;
            RenderedText[pair.Key] = localized;
        }
    }

    /// <summary>
    /// Keeps scene Text components in sync with the selected language, including text
    /// assigned later by dynamic status, error, tutorial, and log code. Original text
    /// is retained so switching back to Chinese is lossless.
    /// </summary>
    public static void Tick() {
        RefreshInputFieldCache();
        if (Time.unscaledTime >= _nextDiscovery) {
            _nextDiscovery = Time.unscaledTime + .35f;
            var found = Object.FindObjectsOfType<Text>(true);
            foreach (var text in found) if (text != null && !UserInputText.Contains(text) && !ProtectedText.Contains(text) && !SourceText.ContainsKey(text)) {
                SourceText[text] = text.text ?? "";
                RenderedText[text] = text.text ?? "";
            }
            foreach (var text in UserInputText) { SourceText.Remove(text); RenderedText.Remove(text); }
        }
        DeadText.Clear();
        ChangedText.Clear();
        foreach (var pair in SourceText) {
            var text = pair.Key;
            if (UserInputText.Contains(text) || ProtectedText.Contains(text)) continue;
            if (text == null) { DeadText.Add(text); continue; }
            var current = text.text ?? "";
            var source = pair.Value;
            if (!RenderedText.TryGetValue(text, out var rendered) || current != rendered) {
                if (current != T(source)) {
                    source = current;
                    ChangedText.Add(new KeyValuePair<Text, string>(text, current));
                }
            }
            var localized = T(source);
            if (current != localized) text.text = localized;
            RenderedText[text] = localized;
        }
        foreach (var change in ChangedText) SourceText[change.Key] = change.Value;
        foreach (var text in DeadText) { SourceText.Remove(text); RenderedText.Remove(text); }
    }

    static void RefreshInputFieldCache() {
        UserInputText.Clear();
        foreach (var field in Object.FindObjectsOfType<InputField>(true))
            if (field != null && field.textComponent != null) UserInputText.Add(field.textComponent);
        foreach (var text in UserInputText) { SourceText.Remove(text); RenderedText.Remove(text); }
    }

    /// <summary>Returns untranslated Han-bearing active UI strings for a leak audit.</summary>
    public static string[] UntranslatedVisibleText() {
        var result = new List<string>();
        foreach (var pair in SourceText) {
            if (pair.Key == null || !pair.Key.gameObject.activeInHierarchy) continue;
            if (UserInputText.Contains(pair.Key) || ProtectedText.Contains(pair.Key)) continue;
            var source = pair.Value ?? "";
            if (English && ContainsHan(T(source)) && !result.Contains(source)) result.Add(source);
        }
        result.Sort(System.StringComparer.Ordinal);
        return result.ToArray();
    }

    public static string[] UntranslatedSpeciesNames() {
        ChemData.EnsureBuilt();
        var result = new List<string>();
        foreach (var item in ChemData.Sub) {
            if (!ChemData.EnSub.TryGetValue(item.Key, out var translated) || string.IsNullOrEmpty(translated))
                result.Add(item.Key + "\t" + item.Value.Name);
        }
        result.Sort(System.StringComparer.Ordinal);
        return result.ToArray();
    }

    public static bool ContainsHan(string value) {
        if (string.IsNullOrEmpty(value)) return false;
        foreach (var c in value) if (c >= '\u3400' && c <= '\u9fff') return true;
        return false;
    }

    /// <summary>Small executable smoke check; caller supplies no scene or chemistry changes.</summary>
    public static bool RunSwitchAudit(out string report) {
        bool prior = English;
        ChemData.EnsureBuilt();
        string zh = "实验记录";
        string en = null;
        bool hasTranslation = ChemData.EnText.TryGetValue(zh, out en) && !string.IsNullOrEmpty(en);
        English = false;
        bool chineseDefault = T(zh) == zh;
        English = true;
        bool translated = hasTranslation && T(zh) == en && !ContainsHan(T(zh));
        bool dynamicVessel = T("烧杯 250 mL") == "beaker 250 mL";
        English = false;
        bool roundTrip = T(zh) == zh;
        English = prior;
        int speciesGaps = UntranslatedSpeciesNames().Length;
        report = $"switch={translated && roundTrip}; dynamicVessel={dynamicVessel}; chineseDefault={chineseDefault}; speciesGaps={speciesGaps}; textEntries={SourceText.Count}; untranslatedVisible={UntranslatedVisibleText().Length}";
        return translated && roundTrip && dynamicVessel && chineseDefault && speciesGaps == 0;
    }

    /// <summary>物质名</summary>
    public static string Sub(string id) => ChemData.SubName(id);

    /// <summary>类别名</summary>
    public static string Cls(string key) {
        if (English && ChemData.EnCls.TryGetValue(key, out var en)) return en;
        return key;
    }

    /// <summary>容器名</summary>
    public static string VesselName(VesselKind k) {
        string zh = VesselNames.Name(k);
        if (English && ChemData.EnVessel.TryGetValue(k.ToString().ToLowerInvariant(), out var en)) return en;
        if (English && ChemData.EnText.TryGetValue(zh, out var en2)) return en2;
        return zh;
    }
}
}

namespace ChemLab.Game {
/* 语言切换按钮的落点 */
public static class Lang {
    public static void Set(bool english) {
        Localization.RegisterAiUiTranslations();
        Localization.RegisterChemUiTranslations();
        Localization.English = english;
        Localization.RefreshTrackedTexts();
        var ui = UnityEngine.Object.FindObjectOfType<ChemLab.Game.LabUI>();
        if (ui != null) ui.RefreshLogLayout();
        ChemLab.Game.PetChat.RefreshForLanguage();
        PlayerPrefs.SetInt("chemlab.lang", english ? 1 : 0);
        PlayerPrefs.Save();
    }
    public static void Load() {
        Localization.RegisterAiUiTranslations();
        Localization.RegisterChemUiTranslations();
        Localization.English = PlayerPrefs.GetInt("chemlab.lang", 0) == 1;
    }
    public static void Toggle() => Set(!Localization.English);
}
}
