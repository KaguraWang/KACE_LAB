const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

if (fs.readFileSync(U, 'utf8').includes('CycleTest')) { console.log('- 已存在'); process.exit(0); }

/* 1) 把「检验器材」标签改成可点按钮：点一下切到下一种道具。
      原来 CurrentTest() 的注释写着「按顺序循环」，但 _testIdx 从来没被改过，
      永远停在 0 = 红色石蕊试纸 —— 木条根本切不到。 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('_testLabel = BarLabel(_testBar, "检验器材", 74, 0, 2);')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 _testLabel 那行'); process.exit(1); }
console.log('第', at + 1, '行:', lines[at].trim());

lines[at] = '        /* 「检验器材」做成按钮：点一下切到下一种道具。';
lines.splice(at + 1, 0,
  '           原来 CurrentTest() 的注释写着「按顺序循环」，但 _testIdx 从来没被改过，',
  '           永远停在 0 = 红色石蕊试纸 —— 木条根本切不到。 */',
  '        {',
  '            var tslot = Node("TestPick", _testBar, new Vector2(0,0), new Vector2(0,0),',
  '                             Vector2.zero, Vector2.zero, new Color(0,0,0,0));',
  '            var tr2 = (RectTransform)tslot.transform;',
  '            tr2.pivot = new Vector2(0,0);',
  '            tr2.anchoredPosition = new Vector2(4, 2);',
  '            tr2.sizeDelta = new Vector2(70, 22);',
  '            _testPick = Btn(tslot, "检验器材", CycleTest, BG2, 11);',
  '            _testLabel = _testPick.GetComponentInChildren<Text>();',
  '        }');

/* 2) 加 CycleTest 方法 */
let m = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('int _testIdx;')) { m = i; break; }
}
if (m < 0) { console.log('✗ 找不到 _testIdx'); process.exit(1); }
lines.splice(m + 1, 0,
  '',
  '    Button _testPick;',
  '    /// <summary>切到下一种检验器材</summary>',
  '    void CycleTest() {',
  '        if (TestSet.All.Count == 0) return;',
  '        _testIdx = (_testIdx + 1) % TestSet.All.Count;',
  '        RefreshVesselPanel();',
  '        if (_selected == null) Refresh();',
  '    }');

fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('✓ 检验器材切换按钮已加');
const c = fs.readFileSync(U, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
