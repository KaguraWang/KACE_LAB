const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
let n = 0;

/* 用一个静态字段持有测试用的 EventSystem，避免 headless 下 current 拿不到 */
if (!u.includes('static EventSystem _testES')) {
  u = u.replace('    public static LabUI CreateForTest() {',
`    static UnityEngine.EventSystems.EventSystem _testES;
    static UnityEngine.EventSystems.EventSystem EnsureTestES() {
        if (_testES != null) return _testES;
        _testES = UnityEngine.EventSystems.EventSystem.current;
        if (_testES == null) {
            var ego = new GameObject("EventSystem_Test");
            _testES = ego.AddComponent<UnityEngine.EventSystems.EventSystem>();
            ego.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return _testES;
    }

    public static LabUI CreateForTest() {`);
  n++;
}
u = u.replace(`        if (UnityEngine.EventSystems.EventSystem.current == null) {
            var ego = new GameObject("EventSystem_Test");
            ego.AddComponent<UnityEngine.EventSystems.EventSystem>();
            ego.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
`, '        EnsureTestES();\n');
u = u.replace(`        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return "✗ 场景里没有 EventSystem";`,
`        var es = EnsureTestES();`);
fs.writeFileSync(U, u, 'utf8');
console.log('✓ EventSystem 处理已改');
console.log('  静态字段:', u.includes('static EventSystem _testES'));
