const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 找到 HandleInput 的完整范围 */
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
console.log('HandleInput:', start + 1, '..', end + 1);

const nw = `    /* ================================================================
       交互
         · 左键点任何东西 → 聚焦到它；点空白 → 取消聚焦
         · WASD → 世界坐标平移（相机相对），同时取消聚焦
         · 长按左键拖动 → 旋转视角（短按不算，免得和「点击聚焦」打架）
         · 右键器皿 → 针对它的操作选单
         · 器皿之外的东西（本生灯、三脚架、石蕊、木条）也能拖
       ================================================================ */
    Vector3 _focusTarget;                 // 聚焦目标（相机会看着它）
    bool _hasFocus;
    double _pressTime;
    Vector2 _pressPos;
    LabPick.Hit _pressHit;
    Apparatus _dragApp;
    TestItem _dragTest;

    void FocusOn(Vector3 p) { _focusTarget = p; _hasFocus = true; }
    void ClearFocus() { _hasFocus = false; }

    void HandleInput() {
        var m = Input.mousePosition;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        /* ---------- 右键：器皿操作选单 ---------- */
        if (Input.GetMouseButtonDown(1) && !overUI) {
            var rh = LabPick.Pick(m, Cam);
            if (rh.vessel != null) { ShowVesselMenu(rh.vessel, m); return; }
        }

        /* ---------- WASD：世界坐标平移（相机相对），并取消聚焦 ---------- */
        float ax = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float az = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        if (ax != 0f || az != 0f) {
            ClearFocus();
            var fwd = Cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            var rgt = Cam.transform.right;   rgt.y = 0f; rgt.Normalize();
            float speed = Mathf.Clamp(_dist * 0.85f, 1.2f, 9f);
            _focus += (rgt * ax + fwd * az) * speed * Time.deltaTime;
            _focus.x = Mathf.Clamp(_focus.x, -16f, 16f);
            _focus.z = Mathf.Clamp(_focus.z, -16f, 16f);
        }

        /* ---------- 左键按下 ---------- */
        if (Input.GetMouseButtonDown(0)) {
            _pressTime = Time.unscaledTimeAsDouble;
            _pressPos = m;
            _orbiting = false;
            _dragApp = null; _dragTest = null;
            if (overUI) { _pressHit = null; return; }

            var hit = LabPick.Pick(m, Cam);
            _pressHit = hit;
            if (hit.vessel != null) {
                _dragging = hit.vessel;
                var p = PickPlane(m);
                _grabOffset = p - hit.vessel.Pos;
                SnapController.Detach(hit.vessel);
                if (_ui != null) _ui.SelectExternal(hit.vessel);
            } else if (hit.apparatus != null) {
                _dragApp = hit.apparatus;
                _grabOffset = PickPlane(m) - hit.apparatus.Pos;
            } else if (hit.testItem != null) {
                _dragTest = hit.testItem;
                _grabOffset = PickPlane(m) - hit.testItem.Pos;
            }
        }

        /* ---------- 左键松开：区分「点击」与「拖动」 ---------- */
        if (Input.GetMouseButtonUp(0)) {
            float moved = Vector2.Distance(m, _pressPos);
            double held = Time.unscaledTimeAsDouble - _pressTime;

            if (_dragging != null) CommitSnap(_dragging);
            _dragging = null; _dragApp = null; _dragTest = null; _orbiting = false;
            ClearHighlights();

            /* 几乎没动、按得也不久 → 算一次点击 */
            if (moved < 6f && held < 0.45) {
                if (_pressHit != null && _pressHit.Any) {
                    FocusOn(_pressHit.Center);          // 聚焦到被点的东西
                    if (_ui != null && _pressHit.vessel != null) _ui.SelectExternal(_pressHit.vessel);
                } else {
                    ClearFocus();                        // 点空白 = 取消聚焦
                    if (_ui != null) _ui.SelectExternal(null);
                }
            }
            _pressHit = null;
        }

        /* ---------- 拖动：器皿 / 器材 / 道具 ---------- */
        bool holding = Input.GetMouseButton(0);
        if (holding && _dragging != null) {
            var p = PickPlane(m); p.y = 0;
            var np = p - _grabOffset;
            var want = new Vector3(Mathf.Clamp(np.x, -3.5f, 3.5f), 0f, Mathf.Clamp(np.z, -2.2f, 2.2f));
            _snapVessel = SnapController.NearestVessel(want, _dragging);
            _snapApp = null;
            if (_snapVessel == null)
                _snapApp = SnapController.NearestApparatus(want, ApparatusKind.BunsenBurner)
                        ?? SnapController.NearestApparatus(want, ApparatusKind.Tripod);
            UpdateHighlights();
            if (_snapVessel != null) {
                _dragging.Pos = new Vector3(_snapVessel.Pos.x, 0f, _snapVessel.Pos.z);
                _dragging.LiftY = SnapController.LiftOnto(_snapVessel, _dragging);
            } else if (_snapApp != null) {
                _dragging.Pos = new Vector3(_snapApp.Pos.x, 0f, _snapApp.Pos.z);
                _dragging.LiftY = SnapController.LiftOntoApparatus(_snapApp);
            } else {
                _dragging.Pos = want;
                _dragging.LiftY = 0f;
            }
        } else if (holding && _dragApp != null) {
            var p = PickPlane(m); p.y = 0;
            var np = p - _grabOffset;
            _dragApp.Pos = new Vector3(Mathf.Clamp(np.x, -3.5f, 3.5f), 0f, Mathf.Clamp(np.z, -2.2f, 2.2f));
        } else if (holding && _dragTest != null) {
            var p = PickPlane(m); p.y = 0;
            var np = p - _grabOffset;
            _dragTest.Pos = new Vector3(Mathf.Clamp(np.x, -3.5f, 3.5f), 0f, Mathf.Clamp(np.z, -2.2f, 2.2f));
        } else if (holding) {
            /* 按住空白处：超过 0.25 秒才开始旋转，
               这样「短按 = 点击聚焦」和「长按拖 = 转视角」不会互相抢 */
            if (!_orbiting && Time.unscaledTimeAsDouble - _pressTime > 0.25) {
                _orbiting = true; _lastMouse = m;
            }
            if (_orbiting) {
                var d = m - _lastMouse; _lastMouse = m;
                _yaw += d.x * 0.007f;
                _pitch = Mathf.Clamp(_pitch + d.y * 0.006f, 0.05f, 1.25f);
            }
        }
        _distT = Mathf.Clamp(_distT * (1f - Input.GetAxis("Mouse ScrollWheel") * 1.6f), 1.5f, 20f);
    }

    /// <summary>右键器皿：针对它的操作选单</summary>
    void ShowVesselMenu(Vessel v, Vector2 screenPos) {
        var items = new List<(string, System.Action)>();
        items.Add((VesselNames.Name(v.Kind) + " #" + v.Id, null));
        items.Add(("——", null));
        items.Add((v.Heating ? "停止加热" : "开始加热", () => { Lab.SetHeat(v, !v.Heating); Select(v); }));
        items.Add((v.Stirring ? "停止搅拌" : "开始搅拌", () => { Lab.SetStir(v, !v.Stirring); Select(v); }));
        if (v.Burner != null || v.TripodRef != null || v.AttachedTo != null)
            items.Add(("脱离吸附", () => { SnapController.Detach(v); v.Burner = null; v.TripodRef = null; Select(v); }));
        items.Add(("倒空", () => { Lab.Empty(v); Select(v); }));
        items.Add(("聚焦", () => { var g = VesselMesh.Get(v.Kind); FocusOn(v.Pos + Vector3.up * (v.LiftY + g.H * 0.45f)); Select(v); }));
        items.Add(("——", null));
        items.Add(("删除", () => { Lab.RemoveVessel(v); if (_ui != null) _ui.SelectExternal(null); }));
        PetMenu.BuildMenu(screenPos, items);
    }
`;

lines.splice(start, end - start + 1, ...nw.split('\n'));
fs.writeFileSync(B, lines.join('\n'), 'utf8');
console.log('✓ HandleInput 已替换');

/* 让相机朝聚焦目标平滑靠拢 —— 在 UpdateCamera 之前插一段 */
let c = fs.readFileSync(B, 'utf8');
if (!c.includes('_hasFocus')) { console.log('✗ 替换失败'); process.exit(1); }
if (!c.includes('// 聚焦：相机平滑看向目标')) {
  c = c.replace('        UpdateCamera(Time.deltaTime);',
`        /* 聚焦：相机平滑看向目标 */
        if (_hasFocus) {
            _focus = Vector3.Lerp(_focus, _focusTarget, Mathf.Min(1f, Time.deltaTime * 4f));
        }
        UpdateCamera(Time.deltaTime);`);
  fs.writeFileSync(B, c, 'utf8');
  console.log('✓ 聚焦插值已加');
}
