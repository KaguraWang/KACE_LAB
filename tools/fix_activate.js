const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/ui.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

if (fs.readFileSync(P, 'utf8').includes('点未激活窗口')) { console.log('- 已存在'); process.exit(0); }

let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('function Click-At {')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 Click-At'); process.exit(1); }
console.log('Click-At 在第', at + 1, '行');
for (let i = at; i < at + 6; i++) console.log('   ', lines[i]);

/* 在 Focus-App 之后、Move-Client 之前插入激活处理 */
let ins = -1;
for (let i = at; i < at + 8; i++) {
  if (lines[i] && lines[i].trim() === 'Focus-App $h') { ins = i + 1; break; }
}
if (ins < 0) { console.log('✗ 找不到 Focus-App 那行'); process.exit(1); }

const blk = [
  '    # Windows 会把「点未激活窗口」的第一次点击拿去激活窗口，不传给程序。',
  '    # 先确认窗口真的在前台；不在就点一下无害的角落把它激活。',
  '    if ([Win32UI]::GetForegroundWindow() -ne $h) {',
  '        $n = To-Screen $h 8 8',
  '        [Win32UI]::SetCursorPos($n[0], $n[1]) | Out-Null',
  '        Start-Sleep -Milliseconds 120',
  '        [Win32UI]::mouse_event([Win32UI]::LEFTDOWN, 0, 0, 0, [IntPtr]::Zero)',
  '        Start-Sleep -Milliseconds 60',
  '        [Win32UI]::mouse_event([Win32UI]::LEFTUP, 0, 0, 0, [IntPtr]::Zero)',
  '        Start-Sleep -Milliseconds 400',
  '    }',
];
lines.splice(ins, 0, ...blk);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 激活处理已插在第', ins + 1, '行');
