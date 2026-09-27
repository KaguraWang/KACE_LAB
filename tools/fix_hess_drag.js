/* =====================================================================
   修焓变计算器的拖动逻辑
     两个 bug，同一个根因：节点和连线不在同一个坐标系里。

     1) 节点挂在 _root 下，连线挂在 _world/_lineLayer 下。
        拖背景移动的是 _world —— 于是只有线动、节点不动。
        → 节点也挂到 _world 下。

     2) NodeDrag.OnDrag 只改节点位置，从不重画连线。
        → 移动后立即 RedrawEdges()，线的长度/弧度/箭头实时跟随。

     另外把连线的锚点从「节点中心」改到「节点边框上」，
     箭头和线条端点才贴着节点边缘，不会戳进节点里。
   ===================================================================== */
const fs = require('fs');
const P = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let c = fs.readFileSync(P, 'utf8');
let n = 0;
const R = (a, b, label) => {
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 50).replace(/\n/g, '⏎')); return; }
  c = c.replace(a, b); n++;
};

/* ---- 1) 节点挂到 _world 下 ---- */
R(`        var go = MkNode("Node" + n.Id, _root, new Color(0.12f, 0.18f, 0.26f, 0.97f));`,
  `        /* 必须挂在 _world 下 —— 和连线同一个坐标系。
           挂在 _root 下的话，拖背景只会移动 _world（线跟着动），
           节点留在原地不动，两者就脱节了。 */
        var go = MkNode("Node" + n.Id, _world, new Color(0.12f, 0.18f, 0.26f, 0.97f));`, '节点父节点');

/* ---- 2) 拖节点时重画连线 ---- */
R(`    public void OnDrag(PointerEventData e) {
        _moved = true;
        node.Pos += e.delta;
        node.Rt.anchoredPosition = node.Pos;
    }`,
  `    public void OnDrag(PointerEventData e) {
        _moved = true;
        node.Pos += e.delta;
        node.Rt.anchoredPosition = node.Pos;
        /* 关键：节点一动，连到它上面的所有线要立刻重画，
           否则线还停在旧位置，看起来就是「线不跟着节点走」。 */
        scene.NodeMoved();
    }`, '拖节点重画');

/* ---- 3) 拖背景：整层一起动（节点+线），并在松手时更新鼠标坐标基准 ---- */
R(`    public void OnDrag(PointerEventData e) {
        if (e.button == PointerEventData.InputButton.Middle || e.button == PointerEventData.InputButton.Left) {
            /* 空白处拖动 = 平移画布 */
            var w = scene.transform.Find("World") as RectTransform;
            if (w != null) w.anchoredPosition += e.delta;
        }
    }`,
  `    public void OnDrag(PointerEventData e) {
        if (e.button == PointerEventData.InputButton.Middle || e.button == PointerEventData.InputButton.Left) {
            /* 空白处拖动 = 平移整个画布。
               节点和连线都是 World 的子节点，所以整层一起走，天然保持整体。 */
            var w = scene.transform.Find("World") as RectTransform;
            if (w != null) w.anchoredPosition += e.delta;
        }
    }`, '拖背景');

/* ---- 4) Scene 暴露 NodeMoved ---- */
R(`    public void BeginEdge(Node from) { _dragFrom = from; _dragTo = from.Pos; }`,
  `    /// <summary>节点被移动后调它 —— 重画所有连线，并刷新正在拉的那根</summary>
    public void NodeMoved() {
        RedrawEdges();
        RefreshDragEdge();
    }

    public void BeginEdge(Node from) { _dragFrom = from; _dragTo = from.Pos; }`, 'NodeMoved');

fs.writeFileSync(P, c, 'utf8');
console.log('补丁处数:', n);
