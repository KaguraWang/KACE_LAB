const fs = require('fs');
let n = 0;

/* 1) _shells 改成 List<Renderer>（要能装 SkinnedMeshRenderer） */
const O = 'D:/ChemLabUnity/Assets/Scripts/Game/OutlineFx.cs';
let o = fs.readFileSync(O, 'utf8');
o = o.replace('readonly List<MeshRenderer> _shells = new List<MeshRenderer>();',
              'readonly List<Renderer> _shells = new List<Renderer>();');
fs.writeFileSync(O, o, 'utf8'); n++;
console.log('  ✓ _shells 改为 List<Renderer>');

/* 2) LabBootstrap：把 SetFocus 那一整块插到 HandleInput 之前 */
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

let at = -1;
for (let i = 0; i < lines.length; i++) if (lines[i].includes('void HandleInput()')) { at = i; break; }
if (at < 0) { console.log('✗ 找不到 HandleInput'); process.exit(1); }
console.log('  HandleInput 在第', at + 1, '行');

/* 往上找那段注释的开头，把旧注释清掉 */
let ins = at;
while (ins > 0 && (lines[ins - 1].trim().startsWith('/*') || lines[ins - 1].trim().startsWith('*')
       || lines[ins - 1].trim().startsWith('·') || lines[ins - 1].trim().endsWith('*/')
       || lines[ins - 1].trim() === '')) {
  if (lines[ins - 1].includes('========') && lines[ins - 2] && lines[ins - 2].includes('聚焦')) { ins = ins - 1; break; }
  ins--;
}

const blk = `    /* ================================================================
       聚焦与轮廓高亮
         · 左键点任何东西（器皿 / 器材 / 检验道具）→ 聚焦它并高亮轮廓
         · 点空白处 → 取消聚焦、取消高亮
       ================================================================ */
    object _focused;
    OutlineFx _focusOutline;

    /// <summary>设聚焦对象；传 null 取消聚焦（同时取消高亮）</summary>
    public void SetFocus(object target) {
        if (ReferenceEquals(_focused, target)) return;
        if (_focusOutline != null) { _focusOutline.SetOn(false); _focusOutline = null; }
        _focused = target;
        if (target == null) return;

        Transform tr = null;
        if (target is Vessel v) {
            var vv = _views.Find(x => x != null && x.Model == v);
            tr = vv != null ? vv.transform : null;
        } else if (target is Apparatus a) {
            var av = _apViews.Find(x => x != null && x.Model == a);
            tr = av != null ? av.transform : null;
        } else if (target is TestItem t) {
            var tv = _testViews.Find(x => x != null && x.Model == t);
            tr = tv != null ? tv.transform : null;
        }
        if (tr == null) return;
        _focusOutline = OutlineFx.Attach(tr, new Color(0.35f, 0.85f, 1f), 0.02f);
        if (_focusOutline != null) _focusOutline.SetOn(true);
    }
    public object Focused => _focused;

`;
lines.splice(ins, 0, ...blk.split('\n'));
fs.writeFileSync(B, lines.join('\n'), 'utf8');
n++;
console.log('  ✓ SetFocus 块已插在第', ins + 1, '行');
console.log('改动处数:', n);
