const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

if (fs.readFileSync(U, 'utf8').includes('_preciseOn')) { console.log('- 已存在'); process.exit(0); }

/* =====================================================================
   精确加量：在「少许/适量/过量」三个预设下面加一行
     [ 数值输入框 ] [ 单位（点一下循环 mg→g→mmol→mL） ] [ 用这个 ]
   做完定量实验必须能指定准确的量，三个模糊档位不够。
   ===================================================================== */

/* 1) 字段 */
let fi = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('string _amount = "normal";')) { fi = i; break; }
}
if (fi < 0) { console.log('✗ 找不到 _amount'); process.exit(1); }
lines.splice(fi + 1, 0,
  '    /* 精确加量：单位在 mg / g / mmol / mL 之间循环 */',
  '    bool _preciseOn;',
  '    double _preciseVal = 1.0;',
  '    int _preciseUnit;                       // 0=mg 1=g 2=mmol 3=mL',
  '    static readonly string[] PreciseUnits = { "mg", "g", "mmol", "mL" };',
  '    InputField _preciseField;',
  '    Text _preciseUnitLabel;',
  '    Text _preciseBtnLabel;');
console.log('  ✓ 字段已加');

/* 2) UI：预设下面加一行，并把分类与滚动列表下移 34 */
let pi = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('Btn(rt, amts[i], () => { _amount = id;')) { pi = i; break; }
}
if (pi < 0) { console.log('✗ 找不到预设按钮'); process.exit(1); }
let pend = pi;
for (let i = pi; i < pi + 5; i++) { if (lines[i].trim() === '}') { pend = i; break; } }
console.log('  预设按钮块:', pi + 1, '..', pend + 1);
for (let i = pi - 1; i <= pend; i++) console.log('   ', lines[i]);

/* 预设按钮点了就关掉精确模式 */
lines[pi] = lines[pi].replace('() => { _amount = id; _amountLabel.text = amts[Array.IndexOf(amtIds, id)]; }',
                              '() => { _amount = id; _preciseOn = false; SyncPreciseUi(); _amountLabel.text = amts[Array.IndexOf(amtIds, id)]; }');

const blk = [
  '',
  '        /* ---- 精确加量 ----',
  '           三个档位只能做定性实验；定量实验必须能写准确的量。 */',
  '        {',
  '            var prow = Node("PreciseRow", root, new Vector2(0,1), new Vector2(0,1),',
  '                            Vector2.zero, Vector2.zero, new Color(0,0,0,0));',
  '            var prt = (RectTransform)prow.transform;',
  '            prt.pivot = new Vector2(0,1);',
  '            prt.anchoredPosition = new Vector2(12, -132);',
  '            prt.sizeDelta = new Vector2(368, 30);',
  '',
  '            BarLabel(prow, "精确", 30, 0, 6);',
  '',
  '            /* 数值输入框 */',
  '            var fgo = Node("PreciseVal", prow, new Vector2(0,1), new Vector2(0,1),',
  '                           Vector2.zero, Vector2.zero, BG2);',
  '            var frt = (RectTransform)fgo.transform;',
  '            frt.pivot = new Vector2(0,1);',
  '            frt.anchoredPosition = new Vector2(36, 2);',
  '            frt.sizeDelta = new Vector2(84, 26);',
  '            var fimg = fgo.GetComponent<Image>();',
  '            var fph = Label(fgo, "0.000", 11, DIM, TextAnchor.MiddleLeft,',
  '                            Vector2.zero, Vector2.one, new Vector2(8,0), new Vector2(-8,0));',
  '            var ftx = Label(fgo, "", 11, INK, TextAnchor.MiddleLeft,',
  '                            Vector2.zero, Vector2.one, new Vector2(8,0), new Vector2(-8,0));',
  '            /* 必须关掉 raycastTarget，否则盖住输入框、点不进去 */',
  '            fph.raycastTarget = false; ftx.raycastTarget = false;',
  '            _preciseField = fgo.AddComponent<InputField>();',
  '            _preciseField.textComponent = ftx; _preciseField.placeholder = fph;',
  '            _preciseField.targetGraphic = fimg;',
  '            _preciseField.text = "1.00";',
  '            _preciseField.onValueChanged.AddListener(s => {',
  '                double.TryParse(s, System.Globalization.NumberStyles.Float,',
  '                                System.Globalization.CultureInfo.InvariantCulture, out _preciseVal);',
  '            });',
  '',
  '            /* 单位：点一下循环 */',
  '            var ugo = Node("PreciseUnit", prow, new Vector2(0,1), new Vector2(0,1),',
  '                           Vector2.zero, Vector2.zero, new Color(0,0,0,0));',
  '            var urt = (RectTransform)ugo.transform;',
  '            urt.pivot = new Vector2(0,1);',
  '            urt.anchoredPosition = new Vector2(126, 2);',
  '            urt.sizeDelta = new Vector2(56, 26);',
  '            var ub = Btn(ugo, "g ▸", () => { _preciseUnit = (_preciseUnit + 1) % PreciseUnits.Length; SyncPreciseUi(); }, BG2, 11);',
  '            _preciseUnitLabel = ub.GetComponentInChildren<Text>();',
  '',
  '            /* 用这个量加药：加完自动关掉精确模式（和预设按钮的行为一致） */',
  '            var ago = Node("PreciseGo", prow, new Vector2(0,1), new Vector2(0,1),',
  '                           Vector2.zero, Vector2.zero, new Color(0,0,0,0));',
  '            var art = (RectTransform)ago.transform;',
  '            art.pivot = new Vector2(0,1);',
  '            art.anchoredPosition = new Vector2(188, 2);',
  '            art.sizeDelta = new Vector2(72, 26);',
  '            var ab = Btn(ago, "用这个", () => { _preciseOn = true; SyncPreciseUi(); Hint("精确模式已开：下一步点试剂就按这个量加"); }, ACC, 11);',
  '            _preciseBtnLabel = ab.GetComponentInChildren<Text>();',
  '            SyncPreciseUi();',
  '        }',
];
lines.splice(pend + 1, 0, ...blk);
console.log('  ✓ 精确加量行已加');

/* 3) 分类与滚动列表下移 34 */
let c = lines.join('\n');
c = c.replace('rt.anchoredPosition = new Vector2(12 + col * 60, -140 - row * 36); rt.sizeDelta = new Vector2(52, 30);',
              'rt.anchoredPosition = new Vector2(12 + col * 60, -174 - row * 36); rt.sizeDelta = new Vector2(52, 30);');
c = c.replace('new Vector2(12, 12), new Vector2(-12, -226), new Color(0,0,0,0.001f));',
              'new Vector2(12, 12), new Vector2(-12, -260), new Color(0,0,0,0.001f));');
lines = c.split('\n');
console.log('  ✓ 分类与列表已下移');

/* 4) SyncPreciseUi + 精确换算 */
let m = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('void AddReagentTo(string id, string state, char sol) {')) { m = i; break; }
}
if (m < 0) { console.log('✗ 找不到 AddReagentTo'); process.exit(1); }
lines.splice(m, 0,
  '    /// <summary>刷新精确加量那一行的显示</summary>',
  '    void SyncPreciseUi() {',
  '        if (_preciseUnitLabel != null)',
  '            _preciseUnitLabel.text = PreciseUnits[_preciseUnit] + " ▸";',
  '        if (_preciseBtnLabel != null)',
  '            _preciseBtnLabel.text = _preciseOn ? "已开启" : "用这个";',
  '        if (_amountLabel != null)',
  '            _amountLabel.text = _preciseOn',
  '                ? $"{_preciseVal:0.###} {PreciseUnits[_preciseUnit]}"',
  '                : (_amount == "little" ? "少许" : _amount == "lots" ? "过量" : "适量");',
  '    }',
  '',
  '    /// <summary>精确模式下的量换算成「克」（mL 走另一条路，返回 -1）</summary>',
  '    double PreciseGrams(string id) {',
  '        double v = _preciseVal;',
  '        switch (_preciseUnit) {',
  '            case 0: return v / 1000.0;                       // mg',
  '            case 1: return v;                                // g',
  '            case 2:                                          // mmol → g',
  '                double mm = ChemCore.MolarMass(id);',
  '                return mm > 0 ? v / 1000.0 * mm : v / 1000.0;',
  '            default: return -1;                              // mL，调用方特殊处理',
  '        }',
  '    }',
  '',
  '    double PreciseMl() => _preciseVal;',
  '');
console.log('  ✓ SyncPreciseUi / PreciseGrams 已加');

fs.writeFileSync(U, lines.join('\n'), 'utf8');
const fin = fs.readFileSync(U, 'utf8');
console.log('  括号差:', (fin.match(/\{/g) || []).length - (fin.match(/\}/g) || []).length);
