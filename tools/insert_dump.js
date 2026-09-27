const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let lines = fs.readFileSync(B, 'utf8').split('\n');

/* 找 BuildUI 的结束位置 */
let start = -1;
for (let i = 0; i < lines.length; i++) if (lines[i].includes('void BuildUI()')) { start = i; break; }
if (start < 0) { console.log('✗ 找不到 BuildUI'); process.exit(1); }
let depth = 0, started = false, end = -1;
for (let k = start; k < lines.length; k++) {
  depth += (lines[k].match(/\{/g) || []).length;
  depth -= (lines[k].match(/\}/g) || []).length;
  if (depth > 0) started = true;
  if (started && depth === 0) { end = k; break; }
}
console.log('BuildUI:', start + 1, '..', end + 1);

if (lines.slice(start, end).some(l => l.includes('dumpLayout'))) {
  console.log('- 已有 dump 代码');
} else {
  lines.splice(end, 0,
    '        foreach (var arg in System.Environment.GetCommandLineArgs())',
    '            if (arg == "-dumpLayout" && _ui != null) Debug.Log("DUMPSTART\\n" + _ui.TestDumpLayout() + "\\nDUMPEND");');
  fs.writeFileSync(B, lines.join('\n'), 'utf8');
  console.log('✓ dump 代码已插入 BuildUI 末尾');
}
