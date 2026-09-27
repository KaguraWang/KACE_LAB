const fs = require('fs');
const V = 'D:/ChemLabUnity/Assets/Scripts/Game/VesselTag.cs';
let lines = fs.readFileSync(V, 'utf8').split('\n');

/* 定位那段：WorldSpace 画布要摆到世界坐标上，不能塞屏幕像素 */
let from = -1, to = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('位置：把世界坐标投影到屏幕')) { from = i - 1; break; }
}
if (from < 0) { console.log('✗ 找不到定位段'); process.exit(1); }
for (let i = from; i < from + 30; i++) {
  if (lines[i] && lines[i].includes('_line.SetPosition(1, basePt);')) { to = i + 2; break; }
}
console.log('替换范围', from + 1, '..', to + 1);
for (let i = from; i <= to; i++) console.log('  旧:', lines[i]);

const nw = `        /* ---- 位置 ----
           画布是 WorldSpace 的（改用世界空间是为了让两张标签的遮挡关系可控），
           所以这里必须摆**世界坐标**。
           以前这里是「投影到屏幕再写 anchoredPosition」—— 那是 ScreenSpaceOverlay
           时代的写法，画布改成世界空间之后就把人扔到看不见的地方去了：
           标签全都 active、canvas 也 enabled，就是不在画面上。 */
        var g = VesselMesh.Get(Model.Kind);
        Vector3 basePt = Model.Pos + Vector3.up * (Model.LiftY + g.H + 0.10f);   // 容器口
        if (_cam == null) _cam = Camera.main;
        bool vis = TagLayer.Visible(basePt, _cam);
        if (_canvas != null) _canvas.enabled = vis;
        if (!vis) return;

        float worldH = h / CanvasUnitsPerWorld;
        Vector3 anchor = basePt + Vector3.up * (Slot * 0.30f + worldH * 0.5f + 0.06f);
        var ct = _canvas.transform;
        ct.position = anchor;
        /* 正对镜头 —— 世界空间画布不会自己转向 */
        if (_cam != null) ct.rotation = _cam.transform.rotation;
        _panel.anchoredPosition = Vector2.zero;
        _panel.localPosition = Vector3.zero;

        /* ---- 白线：从面板底边连到容器口 ---- */
        bool showLine = TagSettings.ShowLine;
        if (_line != null) {
            _line.enabled = showLine;
            if (showLine) {
                _line.SetPosition(0, anchor - Vector3.up * (worldH * 0.5f));
                _line.SetPosition(1, basePt);
            }
        }
    }`;

lines.splice(from, to - from + 1, ...nw.split('\n'));
fs.writeFileSync(V, lines.join('\n'), 'utf8');
console.log('✓ 已改为世界空间定位');

const c = fs.readFileSync(V, 'utf8');
console.log('  大括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
console.log('  还有 screen 投影吗:', c.includes('_panel.anchoredPosition = pBottom'));
