const fs = require('fs');
let n = 0;

/* =====================================================================
   InputField 的 textComponent / placeholder 必须关掉 raycastTarget
     Label() 建出来的 Text 默认 raycastTarget = true，
     它铺满整个输入框、盖在 InputField 的 targetGraphic 上面，
     点击全被 Text 吃掉 —— 输入框永远拿不到焦点，onValueChanged 不触发。
     表现就是：往里打字看着有字，但 _query 一直是空、列表不过滤。
   ===================================================================== */

/* 1) 试剂架搜索框 */
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
const oldU = `        inp.textComponent = txt; inp.placeholder = ph;
        inp.targetGraphic = sf.GetComponent<Image>();`;
const newU = `        /* 这两个 Text 必须关掉 raycastTarget —— Label() 默认是 true，
           它们铺满输入框、盖在 targetGraphic 上面，
           点击全被 Text 吃掉，输入框拿不到焦点。 */
        txt.raycastTarget = false;
        ph.raycastTarget = false;
        inp.textComponent = txt; inp.placeholder = ph;
        inp.targetGraphic = sf.GetComponent<Image>();`;
if (u.includes(oldU)) { u = u.replace(oldU, newU); fs.writeFileSync(U, u, 'utf8'); n++; console.log('  ✓ 搜索框的 Text 已关 raycastTarget'); }
else console.log('  ✗ 搜索框锚点未命中');

/* 2) 计算器手动输入对话框的输入框 */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let e = fs.readFileSync(E, 'utf8');
const oldE = `        inp.textComponent = txt; inp.placeholder = ph;`;
if (e.includes(oldE) && !e.includes('txt.raycastTarget = false;')) {
  e = e.replace(oldE, `        /* 同上：输入框里的 Text 必须关掉 raycastTarget，否则点不进去 */
        txt.raycastTarget = false; ph.raycastTarget = false;
        inp.textComponent = txt; inp.placeholder = ph;`);
  fs.writeFileSync(E, e, 'utf8');
  n++;
  console.log('  ✓ 计算器对话框的 Text 已关 raycastTarget');
} else console.log('  - 计算器对话框:', e.includes(oldE) ? '已有' : '锚点未命中');

/* 3) 全项目扫一遍：还有没有 InputField 的 Text 没关 raycastTarget */
console.log('');
console.log('  检查其它 InputField：');
const dirs = ['D:/ChemLabUnity/Assets/Scripts/Game', 'D:/ChemLabUnity/Assets/Scripts/Chem'];
for (const d of dirs) {
  for (const f of fs.readdirSync(d)) {
    if (!f.endsWith('.cs')) continue;
    const p = d + '/' + f;
    const t = fs.readFileSync(p, 'utf8');
    const cnt = (t.match(/AddComponent<InputField>/g) || []).length;
    if (cnt > 0) {
      const guarded = t.includes('raycastTarget = false');
      console.log('   ', f, '有', cnt, '个 InputField，', guarded ? '已处理' : '✗ 未处理');
    }
  }
}
console.log('改动处数:', n);
