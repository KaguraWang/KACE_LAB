const fs = require('fs');
let n = 0;
const P = (p, a, b, label) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 46).replace(/\n/g,'⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};

/* ---- 1) 顶栏加按钮 ---- */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
P(U, '    /* ------------------------------------------------------------ 右：试剂架 */',
`    /// <summary>顶端「焓变计算器」入口</summary>
    void BuildCalcButton(Transform parent) {
        var go = Node("CalcBtn", parent, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero, new Color(0.16f, 0.42f, 0.32f));
        var rt = (RectTransform)go.transform;
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(310, -12);
        rt.sizeDelta = new Vector2(130, 30);
        var b = go.AddComponent<Button>();
        b.targetGraphic = go.GetComponent<Image>();
        b.onClick.AddListener(() => OpenCalculator());
        Label(go.transform, "焓变计算器", 14, Color.white, TextAnchor.MiddleCenter,
              Vector2.zero, Vector2.one, new Vector2(2, 0), new Vector2(-2, 0));
    }

    void OpenCalculator() {
        var host = LabBootstrap.Instance != null ? LabBootstrap.Instance.gameObject : gameObject;
        EnthalpyScene.Show(host, _font);
    }

    /* ------------------------------------------------------------ 右：试剂架 */`);

P(U, '        BuildLangButton(right);',
`        BuildLangButton(right);
        BuildCalcButton(right);`);

/* ---- 2) LabBootstrap 暴露实例 ---- */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
P(B, '    public const int UiLayerConst = 31;',
`    public const int UiLayerConst = 31;
    /// <summary>供其它界面模块找到宿主</summary>
    public static LabBootstrap Instance;`);

P(B, '        ChemAudio.Ensure(gameObject);',
`        ChemAudio.Ensure(gameObject);
        Instance = this;`);

/* ---- 3) SUT 加焓变求解算子 ---- */
const S = 'D:/ChemLabUnity/Assets/Editor/SpecRunner.cs';
P(S, '                        case "water": {',
`                        /* ---- 焓变计算：盖斯定律求解 ---- */
                        case "hess_edges": {
                            /* 用一条回路解未知焓变。
                               nodes: 节点名数组；edges: [from,to,dh,src] 其中 src 为
                               'table'/'manual'/'unknown'。断言 kind=var 取回结果。 */
                            ChemLab.Chem.HessLaw.Init();
                            var names = (asrt["nodes"].A ?? new List<JVal>()).Select(x => x.Str("")).ToList();
                            var hessEdges = new List<ChemLab.Chem.HessEdge>();
                            foreach (var ev in (asrt["edges"].A ?? new List<JVal>())) {
                                var arr = ev.A;
                                int f = names.IndexOf(arr[0].Str("")), t2 = names.IndexOf(arr[1].Str(""));
                                var src = arr[3].Str("manual");
                                hessEdges.Add(new ChemLab.Chem.HessEdge {
                                    From = f, To = t2, DH = arr[2].Num(0),
                                    Source = src == "unknown" ? ChemLab.Chem.EnthalpySource.Unknown
                                           : src == "table"   ? ChemLab.Chem.EnthalpySource.Table
                                           : ChemLab.Chem.EnthalpySource.Manual,
                                });
                            }
                            var cyc = ChemLab.Chem.HessLaw.FindCycle(names.Count, hessEdges);
                            bool sok = ChemLab.Chem.HessLaw.SolveCycle(cyc, hessEdges, out double unk, out string swhy);
                            string vn = asrt["as"].Str("dh");
                            if (!string.IsNullOrEmpty(vn)) vars[vn] = unk;
                            pass = sok;
                            detail = (cyc.Count >= 4 ? "回路 " + string.Join("→", cyc) + "：" : "") + swhy;
                            break;
                        }
                        case "bond_energy": {
                            ChemLab.Chem.HessLaw.Init();
                            double got = ChemLab.Chem.HessLaw.BondEnergy(
                                asrt["a"].Str(""), asrt["b"].Str(""), asrt["order"].Num(1));
                            double exp = asrt["expect"].Num(0), tol = asrt["tol"].Num(0.5);
                            pass = !double.IsNaN(got) && Math.Abs(got - exp) <= tol;
                            detail = $"{asrt["a"].Str("")}-{asrt["b"].Str("")} 键能 = {got:F0}（期望 {exp:F0}）";
                            break;
                        }
                        case "atomisation": {
                            ChemLab.Chem.HessLaw.Init();
                            double got = ChemLab.Chem.HessLaw.AtomisationOf(asrt["el"].Str(""));
                            double exp = asrt["expect"].Num(0), tol = asrt["tol"].Num(0.5);
                            pass = !double.IsNaN(got) && Math.Abs(got - exp) <= tol;
                            detail = $"{asrt["el"].Str("")} 原子化焓 = {got:F0}（期望 {exp:F0}）";
                            break;
                        }
                        case "water": {`);

console.log('补丁处数:', n);
