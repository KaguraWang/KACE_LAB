const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let lines = fs.readFileSync(U, 'utf8').split('\n');

/* 找出两段测试接口块，删掉靠前的那一段（保留完整的） */
const starts = [];
for (let i = 0; i < lines.length; i++)
  if (lines[i].includes('测试接口')) starts.push(i);
console.log('测试接口块起始行:', starts.map(x => x + 1).join(', '));

if (starts.length >= 2) {
  /* 删掉第一段：从它的注释头到 "public string TestCat => _cat;" 那一段范围的声明与简单成员 */
  /* 更稳的做法：把第一段整块（到 BuildRight / 右：试剂架 之前）删掉 */
  const from = starts[0] - 2;                 // 往上带两行注释
  let to = lines.length - 1;
  for (let i = from; i < lines.length; i++) {
    if (lines[i].includes('右：试剂架') || lines[i].includes('void BuildRight()')) { to = i - 1; break; }
  }
  console.log('删除范围', from + 1, '..', to + 1);
  lines.splice(from, to - from + 1);
  fs.writeFileSync(U, lines.join('\n'), 'utf8');
  console.log('✓ 已删除重复段落');
} else {
  console.log('- 只有一段，无需处理');
}

/* 复查 */
const c = fs.readFileSync(U, 'utf8');
const cnt = k => (c.match(new RegExp(k.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'g')) || []).length;
for (const k of ['public string TestCat', 'public bool TestClickButton', 'public void TestSetFilter',
                 'public List<(string name, Rect2 r)> TestCardRects', 'public string TestRaycastAt',
                 'public int TestFilterCount', 'CreateForTest'])
  console.log('  ' + k + ': ' + cnt(k) + ' 次');
