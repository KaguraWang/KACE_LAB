const fs = require('fs');
let n = 0;

/* 1) 去掉 EnthalpyScene 里的分段日志 */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let e = fs.readFileSync(E, 'utf8');
e = e.split('\n').filter(l => !l.includes('[HessBuild]')).join('\n');
fs.writeFileSync(E, e, 'utf8'); n++;
console.log('  ✓ 去掉 HessBuild 分段日志');

/* 2) 去掉 SetLabUiVisible 的调试日志 */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
b = b.replace('        Debug.Log("[ChemLab] SetLabUiVisible(" + on + ")  _ui=" + (_ui != null));\n', '');
fs.writeFileSync(B, b, 'utf8'); n++;
console.log('  ✓ 去掉 SetLabUiVisible 日志');

/* 3) 自检里的完整栈改回只打第一行 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
sc = sc.replace('Say("     完整栈：" + (ex.StackTrace ?? "(无)"));',
                'Say("     位置：" + (ex.StackTrace ?? "").Split(\'\\n\')[0]);');
fs.writeFileSync(S, sc, 'utf8'); n++;
console.log('  ✓ 自检的栈输出改回单行');
console.log('改动处数:', n);

const chk = fs.readFileSync(E, 'utf8');
console.log('  HessBuild 残留:', chk.includes('HessBuild'));
console.log('  Build 调用次数:', (chk.match(/s\.Build\(font\);/g) || []).length);
