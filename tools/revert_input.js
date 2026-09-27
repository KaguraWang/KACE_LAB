const fs = require('fs');
let n = 0;

/* ---- 1) EventSystem 挂到根上，别做 LabUI 的子节点 ---- */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
if (u.includes('ego.transform.SetParent(transform, false);')) {
  u = u.replace(`        var es = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (es == null) {
            var ego = new GameObject("EventSystem");
            ego.transform.SetParent(transform, false);
            ego.AddComponent<EventSystem>();
            ego.AddComponent<StandaloneInputModule>();
        }`,
`        var es = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (es == null) {
            /* 关键：EventSystem **不能**做 LabUI 的子节点。
               进焓变计算器时要把 LabUI 整个 SetActive(false)，
               挂在它下面的话 EventSystem 会被一起禁用 ——
               结果就是计算器页面所有控件都收不到输入（点不动、打不了字）。 */
            var ego = new GameObject("EventSystem");
            ego.transform.SetParent(transform.root, false);   // 挂到场景根
            ego.AddComponent<EventSystem>();
            ego.AddComponent<StandaloneInputModule>();
        }`);
  fs.writeFileSync(U, u, 'utf8'); n++;
  console.log('  ✓ EventSystem 改挂场景根');
}

/* ---- 2) 还原 3D 的 HandleInput ---- */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 定位现在的 HandleInput */
let start = -1, end = -1;
for (let i = 0; i < lines.length; i++) if (lines[i].includes('void HandleInput()')) { start = i; break; }
if (start < 0) { console.log('✗ 找不到 HandleInput'); process.exit(1); }
let depth = 0, started = false;
for (let k = start; k < lines.length; k++) {
  depth += (lines[k].match(/\{/g) || []).length;
  depth -= (lines[k].match(/\}/g) || []).length;
  if (depth > 0) started = true;
  if (started && depth === 0) { end = k; break; }
}
console.log('  HandleInput: 第', start + 1, '..', end + 1, '行');

/* 还原成原来那套：点器皿拖动、点空白转视角。
   只保留一个「计算器开着时直接返回」的守卫 —— 那个是必要的。 */
const original = `    void HandleInput() {
        /* 计算器开着时实验台不接收输入（那边有自己的交互） */
        if (CalculatorActive) return;

        var m = Input.mousePosition;
        /* 鼠标在界面面板上时不参与 3D 拖动 */
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0)) {
            if (overUI) { _orbiting = false; return; }
            var v = PickVessel(m);
            if (v != null) {
                _dragging = v;
                var p = PickPlane(m);
                _grabOffset = p - v.Pos;
                /* 拿起来就解除原有吸附，否则会一直挂在别人上面 */
                SnapController.Detach(v);
                if (_ui != null) _ui.SelectExternal(v);
            } else {
                _orbiting = true; _lastMouse = m;
            }
        }
        if (Input.GetMouseButtonUp(0)) {
            if (_dragging != null) CommitSnap(_dragging);
            _dragging = null; _orbiting = false;
            ClearHighlights();
        }

        if (_dragging != null && Input.GetMouseButton(0)) {
            var p = PickPlane(m);
            p.y = 0;
            var np = p - _grabOffset;
            var want = new Vector3(Mathf.Clamp(np.x, -3.5f, 3.5f), 0f, Mathf.Clamp(np.z, -2.2f, 2.2f));

            /* 找吸附目标：优先别的容器，其次本生灯，再次三脚架 */
            _snapVessel = SnapController.NearestVessel(want, _dragging);
            _snapApp = null;
            if (_snapVessel == null) {
                _snapApp = SnapController.NearestApparatus(want, ApparatusKind.BunsenBurner)
                        ?? SnapController.NearestApparatus(want, ApparatusKind.Tripod);
            }
            UpdateHighlights();

            if (_snapVessel != null) {
                /* 吸附：水平对齐到目标正上方，并按目标高度抬起 */
                _dragging.Pos = new Vector3(_snapVessel.Pos.x, 0f, _snapVessel.Pos.z);
                _dragging.LiftY = SnapController.LiftOnto(_snapVessel, _dragging);
            } else if (_snapApp != null) {
                _dragging.Pos = new Vector3(_snapApp.Pos.x, 0f, _snapApp.Pos.z);
                _dragging.LiftY = SnapController.LiftOntoApparatus(_snapApp);
            } else {
                _dragging.Pos = want;
                _dragging.LiftY = 0f;
            }
        } else if (_orbiting && Input.GetMouseButton(0)) {
            var d = m - _lastMouse; _lastMouse = m;
            _yaw += d.x * 0.007f;
            _pitch = Mathf.Clamp(_pitch + d.y * 0.006f, 0.05f, 1.25f);
        }
        _distT = Mathf.Clamp(_distT * (1f - Input.GetAxis("Mouse ScrollWheel") * 1.6f), 1.5f, 20f);
    }`;

lines.splice(start, end - start + 1, ...original.split('\n'));
fs.writeFileSync(B, lines.join('\n'), 'utf8');
n++;
console.log('  ✓ HandleInput 已还原');

console.log('改动处数:', n);
