const fs = require('fs');
const U = 'D:/ChemLabUnity/Assets/Scripts/Game/LabUI.cs';
let u = fs.readFileSync(U, 'utf8');
if (!u.includes('TestReagentTotal')) {
  u = u.replace('    public string TestCat => _cat;',
`    public int TestReagentTotal => _reagents != null ? _reagents.Count : -1;
    public List<string> TestListButtons() => TestAllButtons();
    public string TestCat => _cat;`);
  fs.writeFileSync(U, u, 'utf8');
  console.log('✓ TestReagentTotal / TestListButtons 已加');
} else console.log('- 已存在');
