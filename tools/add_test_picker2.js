const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

const src = fs.readFileSync(U, 'utf8');
console.log('  CycleTest 方法:', src.includes('void CycleTest()'));
console.log('  _testPick 字段:', src.includes('Button _testPick;'));
console.log('  _testPick 使用:', src.includes('_testPick = Btn'));

if (src.includes('_testPick = Btn')) { console.log('- 按钮已存在'); process.exit(0); }

/* 加 _testPick 字段 */
let fi = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('void CycleTest() {')) { fi = i; break; }
}
if (fi < 0) { console.log('✗ 找不到 CycleTest'); process.exit(1); }
if (!lines.slice(fi - 4, fi).some(l => l.includes('_testPick'))) {
  lines.splice(fi, 0, '    Button _testPick;');
  console.log('  ✓ _testPick 字段已加在第', fi + 1, '行');
}

/* 把「检验器材」标签换成可点按钮 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('_testLabel = BarLabel(_testBar, "检验器材"')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 _testLabel 那行'); process.exit(1); }
console.log('  _testLabel 在第', at + 1, '行:', lines[at].trim());

const blk = [
  '        /* 「检验器材」做成按钮：点一下切到下一种道具。',
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
  '        }',
];
lines.splice(at, 1, ...blk);
fs.writeFileSync(U, lines.join('\n'), 'utf8');
console.log('✓ 切换按钮已加');

const c = fs.readFileSync(U, 'utf8');
console.log('  括号差:', (c.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
console.log('  含 _testPick = Btn:', c.includes('_testPick = Btn'));
