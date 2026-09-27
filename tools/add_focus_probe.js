const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');

if (u.includes('PROBE|当前选中对象')) { console.log('- 已存在'); process.exit(0); }

const anchor = '            sb.AppendLine("PROBE|该点射线命中 " + res.Count + " 个:");';
if (!u.includes(anchor)) { console.log('✗ 锚点未命中'); process.exit(1); }

const add = [
  '            /* 焦点有没有切过去 —— InputField 要靠这个才收键盘 */',
  '            var sel = es.currentSelectedGameObject;',
  '            sb.AppendLine("PROBE|当前选中对象|" + (sel == null ? "(无)" : sel.name));',
  '            sb.AppendLine("PROBE|输入模块|" + (es.currentInputModule == null ? "(无！)" : es.currentInputModule.GetType().Name));',
  '            sb.AppendLine("PROBE|isFocused(输入框)|" + _searchField.isFocused);',
  '            sb.AppendLine("PROBE|Input.inputString 可用|" + (Input.inputString != null ? "yes" : "no"));',
  '            if (es.currentInputModule is UnityEngine.EventSystems.StandaloneInputModule sim) {',
  '                sb.AppendLine("PROBE|Standalone 轴向|" + sim.horizontalAxis + "/" + sim.verticalAxis',
  '                            + " 提交=" + sim.submitButton + " 取消=" + sim.cancelButton);',
  '            }',
  anchor,
].join('\n');

u = u.replace(anchor, add);
fs.writeFileSync(U, u, 'utf8');
console.log('✓ 焦点诊断已加');
console.log('  括号差:', (fs.readFileSync(U,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(U,'utf8').match(/\}/g)||[]).length);
