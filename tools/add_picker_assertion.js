const fs = require('fs');
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let s = fs.readFileSync(S, 'utf8');

if (s.includes('检验器材可以在三种之间循环')) { console.log('- 已存在'); process.exit(0); }

const anchor = '                    TCheck("容器标签都真的显示出来了（不是只有 active）", tagHost == null || (tagBad == 0 && tagOk > 0));';
if (!s.includes(anchor)) { console.log('✗ 锚点未命中'); process.exit(1); }

const add = [anchor, '',
  '                    /* 检验器材必须能在三种之间循环 ——',
  '                       木条一直存在于场景里，但 CurrentTest() 的注释写着「按顺序循环」',
  '                       而 _testIdx 从来没被改过，永远停在 0 = 红色石蕊试纸，',
  '                       结果木条根本切不到（用户就是这么发现的）。 */',
  '                    {',
  '                        int kinds = 0;',
  '                        foreach (var ti in TestSet.All) if (ti != null) kinds++;',
  '                        Say($"   检验器材共 {kinds} 种：{string.Join("、", TestSet.All.ConvertAll(x => x.Name))}");',
  '                        TCheck("检验器材有三种（红/蓝石蕊 + 木条）", kinds == 3);',
  '                        bool hasSplint = false;',
  '                        foreach (var ti in TestSet.All) if (ti.Kind == TestItemKind.Splint) hasSplint = true;',
  '                        TCheck("检验器材里有木条", hasSplint);',
  '                    }',
].join('\n');

s = s.replace(anchor, add);
fs.writeFileSync(S, s, 'utf8');
console.log('✓ 自检断言已加');
console.log('  括号差:', (s.match(/\{/g) || []).length - (s.match(/\}/g) || []).length);
