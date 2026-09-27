const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let c = fs.readFileSync(U, 'utf8');
if (!c.includes('CreateForTest')) {
  const anchor = '    public string TestCat => _cat;';
  if (!c.includes(anchor)) { console.log('✗ 锚点'); process.exit(1); }
  c = c.replace(anchor,
`    /// <summary>建一个只用来测试的界面实例</summary>
    public static LabUI CreateForTest() {
        if (LabUI.UiFont == null) LabUI.UiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("LabUI_Test");
        go.AddComponent<Canvas>();
        var ui = go.AddComponent<LabUI>();
        ui.UiLayer = 31;
        ui.Build();
        return ui;
    }

    public string TestCat => _cat;`);
  fs.writeFileSync(U, c, 'utf8');
  console.log('✓ CreateForTest 已加');
} else console.log('- 已存在');

/* 检查字段名 */
const lines = c.split('\n');
console.log('');
console.log('=== 相关字段声明 ===');
for (let i = 0; i < lines.length; i++) {
  if (/_reagents|_cat\b|_query|_amount|_contentList|_canvas\b/.test(lines[i]) && /=|;/.test(lines[i]) && !/^\s*\/\//.test(lines[i])) {
    const t = lines[i].trim();
    if (/^(IEnumerable|List<|string|int|RectTransform|Canvas|double|readonly|static|Transform|bool)/.test(t))
      console.log('  ' + (i + 1) + ': ' + t.slice(0, 100));
  }
}
