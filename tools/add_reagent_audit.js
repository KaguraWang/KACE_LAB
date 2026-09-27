const fs = require('fs');
let n = 0;

/* =====================================================================
   试剂架改走「状态自报」而不是「像素取样」
     像素取样在这个用例上一直不可靠（列表行与行长得像、取样区域难对准），
     调了四轮反而越来越差。改成让游戏自己报：当前分类、搜索词、可见条目数。
   ===================================================================== */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');

if (!u.includes('ReagentAudit')) {
  const anchor = '    public string TestCat => _cat;';
  if (!u.includes(anchor)) { console.log('✗ 找不到 TestCat'); process.exit(1); }
  const add = [
    '    /// <summary>试剂架当前状态，给窗口级测试当客观证据。格式：REAGENT|键|值</summary>',
    '    public string ReagentAudit() {',
    '        var sb = new System.Text.StringBuilder();',
    '        sb.AppendLine("REAGENT|cat|" + _cat);',
    '        sb.AppendLine("REAGENT|search|" + (_search != null ? _search : ""));',
    '        int n = 0;',
    '        foreach (var kv in _reagents) {',
    '            if (kv.Value == null || !kv.Value.gameObject.activeSelf) continue;',
    '            n++;',
    '        }',
    '        sb.AppendLine("REAGENT|visible|" + n);',
    '        sb.AppendLine("REAGENT|total|" + _reagents.Count);',
    '        return sb.ToString();',
    '    }',
    '',
    anchor,
  ].join('\n');
  u = u.replace(anchor, add);
  fs.writeFileSync(U, u, 'utf8');
  n++;
  console.log('  ✓ ReagentAudit 已加');
}

/* 接到 StateTick */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let b = fs.readFileSync(B, 'utf8');
if (!b.includes('ReagentAudit')) {
  const a = '            Debug.Log("[PetState]\\n" + PetAudit());';
  if (b.includes(a)) {
    b = b.replace(a, a + '\n            if (_ui != null) Debug.Log("[ReagentState]\\n" + _ui.ReagentAudit());');
    fs.writeFileSync(B, b, 'utf8');
    n++;
    console.log('  ✓ 已接进 StateTick');
  } else console.log('  ✗ PetState 锚点未命中');
}
console.log('改动处数:', n);
console.log('  LabUI 括号差:', (fs.readFileSync(U,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(U,'utf8').match(/\}/g)||[]).length);
console.log('  LabBootstrap 括号差:', (fs.readFileSync(B,'utf8').match(/\{/g)||[]).length - (fs.readFileSync(B,'utf8').match(/\}/g)||[]).length);
