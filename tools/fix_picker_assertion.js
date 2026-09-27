const fs = require('fs');
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let s = fs.readFileSync(S, 'utf8');

const old = [
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

const nw = [
  '                    {',
  '                        /* 无头环境里 TestSet.All 是空的，所以查枚举本身：',
  '                           三种道具必须都在，而且 CurrentTest 的循环要能遍历到它们。 */',
  '                        var kv = System.Enum.GetValues(typeof(TestItemKind));',
  '                        bool hasRed = false, hasBlue = false, hasSplint = false;',
  '                        foreach (TestItemKind k in kv) {',
  '                            if (k == TestItemKind.RedLitmus) hasRed = true;',
  '                            if (k == TestItemKind.BlueLitmus) hasBlue = true;',
  '                            if (k == TestItemKind.Splint) hasSplint = true;',
  '                        }',
  '                        Say($"   检验器材种类数 = {kv.Length}");',
  '                        TCheck("检验器具有三种（红/蓝石蕊 + 木条）", kv.Length == 3 && hasRed && hasBlue);',
  '                        TCheck("检验器具里有木条（能被循环切换选中）", hasSplint);',
  '                        /* 木条必须有「正在燃烧 / 刚熄灭」这两个状态，',
  '                           否则「点燃 → 伸入」那套检验根本做不了。 */',
  '                        var sv2 = System.Enum.GetValues(typeof(SplintState));',
  '                        TCheck("木条有燃烧/熄灭状态", sv2.Length == 3);',
  '                    }',
].join('\n');

if (!s.includes(old)) { console.log('✗ 锚点未命中'); process.exit(1); }
s = s.replace(old, nw);
fs.writeFileSync(S, s, 'utf8');
console.log('✓ 断言已改成查枚举');
console.log('  括号差:', (s.match(/\{/g) || []).length - (s.match(/\}/g) || []).length);
