const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/calctest.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

if (fs.readFileSync(P, 'utf8').includes('预热点击')) { console.log('- 已存在'); process.exit(0); }

/* 在 Start-App 之后、收集坐标之前插一次预热点击 */
let at = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('$h = Start-App -ExtraArgs')) { at = i; break; }
}
if (at < 0) { console.log('✗ 找不到 Start-App'); process.exit(1); }
console.log('Start-App 在第', at + 1, '行');

const blk = [
  '',
  '# 预热点击：程序刚起来时的第一次点击会被 Windows 拿去激活窗口，传不到程序里。',
  '# 先点一下无害的角落把它消耗掉，否则测试的第一步会被吞（第一次跑就是这么被骗的）。',
  '[Win32UI]::SetForegroundWindow($h) | Out-Null',
  'Start-Sleep -Milliseconds 500',
  '$w0 = To-Screen $h 8 8',
  '[Win32UI]::SetCursorPos($w0[0], $w0[1]) | Out-Null',
  'Start-Sleep -Milliseconds 200',
  '[Win32UI]::mouse_event([Win32UI]::LEFTDOWN, 0, 0, 0, [IntPtr]::Zero)',
  'Start-Sleep -Milliseconds 70',
  '[Win32UI]::mouse_event([Win32UI]::LEFTUP, 0, 0, 0, [IntPtr]::Zero)',
  'Start-Sleep -Milliseconds 800',
];
lines.splice(at + 1, 0, ...blk);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ 预热点击已插在第', at + 2, '行');
