const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

/* 找到整个精确行块，按预设按钮那套写法重做 */
let a = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('/* ---- 精确加量 ----')) { a = i; break; }
}
if (a < 0) { console.log('✗ 找不到精确加量块'); process.exit(1); }
let b = -1;
for (let i = a; i < a + 60; i++) {
  if (lines[i] !== undefined && lines[i].trim() === '}' && lines[i - 1] && lines[i - 1].includes('SyncPreciseUi();')) { b = i; break; }
}
if (b < 0) { console.log('✗ 找不到块尾'); process.exit(1); }
console.log('精确行块:', a + 1, '..', b + 1);
for (let i = a; i <= Math.min(a + 6, b); i++) console.log('   ', lines[i]);

const nw = [
  '        /* ---- 精确加量 ----',
  '           三个档位只能做定性实验；定量实验必须能写准确的量。',
  '           完全照预设按钮那套写法（Node 摆位 + Btn 填充），不用 BarLabel ——',
  '           BarLabel 是按 _testBar 那种整条栏设计的，塞进小容器会跑位。 */',
  '        {',
  '            string[] rowSpec = { "精确", "0.000", "g ▸", "用这个" };',
  '            float[] rowX = { 12, 66, 176, 244 };',
  '            float[] rowW = { 50, 106, 62, 78 };',
  '            Transform prow = null;',
  '            for (int i = 0; i < rowSpec.Length; i++) {',
  '                var go = Node("Pr" + i, root, new Vector2(0,1), new Vector2(0,1),',
  '                               Vector2.zero, Vector2.zero, new Color(0,0,0,0));',
  '                var rt = (RectTransform)go;',
  '                rt.pivot = new Vector2(0,1);',
  '                rt.anchoredPosition = new Vector2(rowX[i], -134);',
  '                rt.sizeDelta = new Vector2(rowW[i], 28);',
  '                if (i == 0) {',
  '                    var lb = Label(rt, "精确", 11, DIM, TextAnchor.MiddleLeft,',
  '                                   Vector2.zero, Vector2.one, new Vector2(2,0), new Vector2(-2,0));',
  '                    lb.raycastTarget = false;',
  '                } else if (i == 1) {',
  '                    /* 数值输入框 */',
  '                    var img = go.AddComponent<Image>();',
  '                    img.color = new Color(0.06f, 0.10f, 0.15f, 1f);',
  '                    var ph2 = Label(rt, "0.000", 11, DIM, TextAnchor.MiddleLeft,',
  '                                    Vector2.zero, Vector2.one, new Vector2(8,0), new Vector2(-8,0));',
  '                    var tx2 = Label(rt, "", 11, INK, TextAnchor.MiddleLeft,',
  '                                    Vector2.zero, Vector2.one, new Vector2(8,0), new Vector2(-8,0));',
  '                    ph2.raycastTarget = false; tx2.raycastTarget = false;',
  '                    _preciseField = go.AddComponent<InputField>();',
  '                    _preciseField.textComponent = tx2; _preciseField.placeholder = ph2;',
  '                    _preciseField.targetGraphic = img;',
  '                    _preciseField.text = "1.00";',
  '                    _preciseField.onValueChanged.AddListener(s => {',
  '                        double.TryParse(s, System.Globalization.NumberStyles.Float,',
  '                                        System.Globalization.CultureInfo.InvariantCulture, out _preciseVal);',
  '                    });',
  '                } else if (i == 2) {',
  '                    var ub = Btn(rt, "g ▸", () => { _preciseUnit = (_preciseUnit + 1) % PreciseUnits.Length; SyncPreciseUi(); }, BG2, 11);',
  '                    _preciseUnitLabel = ub.GetComponentInChildren<Text>();',
  '                } else {',
  '                    var ab = Btn(rt, "用这个", () => { _preciseOn = true; SyncPreciseUi(); Hint("精确模式已开：下一步点试剂就按这个量加"); }, ACC, 11);',
  '                    _preciseBtnLabel = ab.GetComponentInChildren<Text>();',
  '                }',
  '            }',
  '            SyncPreciseUi();',
  '        }',
];
lines.splice(a, b - a + 1, ...nw);
fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('✓ 精确行已重做');

/* 分类行与滚动列表再下移一点，留足间距 */
let c = fs.readFileSync(U, 'utf8');
c = c.replace('rt.anchoredPosition = new Vector2(12 + col * 60, -174 - row * 36); rt.sizeDelta = new Vector2(52, 30);',
              'rt.anchoredPosition = new Vector2(12 + col * 60, -182 - row * 38); rt.sizeDelta = new Vector2(52, 30);');
c = c.replace('new Vector2(12, 12), new Vector2(-12, -260), new Color(0,0,0,0.001f));',
              'new Vector2(12, 12), new Vector2(-12, -272), new Color(0,0,0,0.001f));');
fs.writeFileSync(U, c, 'utf8');
console.log('  ✓ 分类与列表已再下移');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
