const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(E, 'utf8');
if (c.includes('TestClickTargets')) { console.log('- 已存在'); process.exit(0); }

c = c.replace('    /// <summary>最近一次求解是否成功（教程用它判断用户点没点对）</summary>',
`    /// <summary>把计算器里关键控件的中心点按「游戏内屏幕坐标」打出来，供外部自动化点击。
    /// 输出格式与 LabUI.TestClickTargets 一致：CLICK|名字|x|y</summary>
    public string TestClickTargets() {
        Canvas.ForceUpdateCanvases();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("SCREEN|" + Screen.width + "|" + Screen.height);
        void Emit(string name, RectTransform rt) {
            if (rt == null) return;
            var c = rt.TransformPoint(rt.rect.center);
            sb.AppendLine("CLICK|" + name + "|" + Mathf.RoundToInt(c.x) + "|" + Mathf.RoundToInt(c.y));
        }
        /* 画布上的按钮（清空 / 求解 / 返回） */
        foreach (var rt in GetComponentsInChildren<RectTransform>(true)) {
            if (rt.name == "B") {
                var txt = rt.GetComponentInChildren<Text>();
                if (txt == null) continue;
                string t = txt.text;
                if (t == "清空") Emit("calc_clear", rt);
                else if (t == "求解未知焓变") Emit("calc_solve", rt);
                else if (t == "返回实验台") Emit("calc_back", rt);
                else if (t == "确定") Emit("dlg_ok", rt);
                else if (t == "取消") Emit("dlg_cancel", rt);
            }
        }
        /* 节点 */
        for (int i = 0; i < _nodes.Count; i++) Emit("node" + i, _nodes[i].Rt);
        /* 连线 */
        for (int i = 0; i < _edges.Count; i++) {
            if (_edges[i].Rt == null) continue;
            var t = _edges[i].Rt.GetComponentInChildren<Text>();
            if (t == null) continue;
            Emit("edge" + i, (RectTransform)t.transform);
        }
        return sb.ToString();
    }

    /// <summary>最近一次求解是否成功（教程用它判断用户点没点对）</summary>`);
fs.writeFileSync(E, c, 'utf8');
console.log('✓ EnthalpyScene.TestClickTargets 已加');
