const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(p, 'utf8');
if (c.includes('AskManualValue')) { console.log('已存在'); process.exit(0); }

/* 在 AskEnthalpy 之后插入「手动输入数值」对话框 */
const anchor = `    /* ---------------- 求解 ---------------- */`;
const add = `    /* ---------------- 手动输入数值对话框 ---------------- */
    /// <summary>弹出一个小窗口让用户输入 kJ/mol 数值。
    /// 这是「手动输入」这条路径真正缺的一环 —— 之前只是给了个占位值 −100。</summary>
    public void AskManualValue(Edge e, bool firstTime) {
        CloseMenu();
        var save = e.DH;
        var srcSave = e.Source;

        var panel = MkNode("ValueDlg", _menuLayer, new Color(0.12f, 0.16f, 0.22f, 0.99f));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(320, 150);
        panel.SetAsLastSibling();

        MkText(panel, "输入这条连线的焓变 ΔH（kJ/mol）", 14, new Color(0.85f, 0.92f, 1f),
               TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(1, 1),
               new Vector2(10, -34), new Vector2(-10, -8));

        var fld = MkInput(panel, new Vector2(0, 0), new Vector2(150, 30),
                          new Vector2(0.5f, 0.5f), e.DH.ToString("0.#"));
        var frt = (RectTransform)fld.transform;
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.anchoredPosition = new Vector2(0, 4);
        fld.text = e.DH.ToString("0.#");
        fld.ActivateInputField();

        System.Action commit = () => {
            double v;
            if (double.TryParse(fld.text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out v)) {
                e.DH = v; e.Source = EnthalpySource.Manual;
                RedrawEdges();
                _result.text = $"已设为手动值 ΔH = {v:F1} kJ/mol";
            } else {
                _result.text = $"「{fld.text}」不是有效数字，已忽略";
                e.DH = save; e.Source = srcSave;
            }
            Destroy(panel.gameObject);
        };
        System.Action cancel = () => {
            e.DH = save; e.Source = srcSave;
            if (firstTime) RemoveEdge(e);       // 第一次创建时取消 = 撤销这条线
            RedrawEdges();
            _result.text = firstTime ? "已撤销这条连线。" : "已取消编辑。";
            Destroy(panel.gameObject);
        };

        MkBtn(panel, "确定", commit, new Vector2(-14, 12), new Vector2(80, 28), new Vector2(1, 0), new Color(0.20f, 0.55f, 0.35f));
        MkBtn(panel, "取消", cancel, new Vector2(-104, 12), new Vector2(80, 28), new Vector2(1, 0), new Color(0.42f, 0.26f, 0.26f));
        /* 回车 = 确定，Esc 处理不了就用右键取消 */
        fld.onEndEdit.AddListener(s => { if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) commit(); });
    }

    /* ---------------- 求解 ---------------- */`;

if (!c.includes(anchor)) { console.log('✗ 锚点'); process.exit(1); }
c = c.replace(anchor, add);

/* 「手动输入…」改成打开对话框 */
c = c.replace(`        items.Add(("手动输入…", () => {
            e.Source = EnthalpySource.Manual;
            if (e.DH == 0) e.DH = -100;
            RedrawEdges();
            _result.text = "已设为手动值（按住后可用下面的 ± 调整）";
        }));`,
`        items.Add(("手动输入数值…", () => AskManualValue(e, !editing)));`);

/* 「取消（撤销这条线）」保留；未知量那条也保留 */
fs.writeFileSync(p, c, 'utf8');
console.log('✓ 手动输入对话框已加');
