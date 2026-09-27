const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/ui.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

if (fs.readFileSync(P, 'utf8').includes('VK 码直接注入')) { console.log('- 已存在'); process.exit(0); }

/* 找到 Type-Text 整段替换成 keybd_event 版本 */
let from = -1, to = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('function Type-Text {')) { from = i; break; }
}
if (from < 0) { console.log('✗ 找不到 Type-Text'); process.exit(1); }
for (let i = from; i < from + 12; i++) {
  if (lines[i] && lines[i].trim() === '}') { to = i; break; }
}
console.log('Type-Text 在', from + 1, '..', to + 1);
for (let i = from; i <= to; i++) console.log('  旧:', lines[i]);

const nw = [
  'function Type-Text {',
  '    param([string]$Text, [int]$SettleMs = 400)',
  '    # 按 VK 码直接注入按键。',
  '    # 原来用 SendKeys.SendWait，实测按键根本到不了程序',
  '    # （游戏里 Update 抓 Input.inputString 一个字符都没收到）。',
  '    # keybd_event 是更底层的注入，可靠得多。',
  '    $VK = @{',
  "        'a'=0x41; 'b'=0x42; 'c'=0x43; 'd'=0x44; 'e'=0x45; 'f'=0x46; 'g'=0x47",
  "        'h'=0x48; 'i'=0x49; 'j'=0x4A; 'k'=0x4B; 'l'=0x4C; 'm'=0x4D; 'n'=0x4E",
  "        'o'=0x4F; 'p'=0x50; 'q'=0x51; 'r'=0x52; 's'=0x53; 't'=0x54; 'u'=0x55",
  "        'v'=0x56; 'w'=0x57; 'x'=0x58; 'y'=0x59; 'z'=0x5A",
  "        '0'=0x30; '1'=0x31; '2'=0x32; '3'=0x33; '4'=0x34",
  "        '5'=0x35; '6'=0x36; '7'=0x37; '8'=0x38; '9'=0x39",
  "        ' '=0x20; '-'=0xBD; '.'=0xBE; '/'=0xBF",
  '    }',
  '    $SHIFT = 0x10',
  '    foreach ($ch in $Text.ToCharArray()) {',
  '        $s = [string]$ch',
  '        $up = $s -cmatch "[A-Z]"',
  '        $vk = $VK[$s.ToLower()]',
  '        if (-not $vk) { continue }',
  '        if ($up) { [Win32UI]::keybd_event([byte]$SHIFT, 0, 0, [IntPtr]::Zero); Start-Sleep -Milliseconds 25 }',
  '        [Win32UI]::keybd_event([byte]$vk, 0, 0, [IntPtr]::Zero)',
  '        Start-Sleep -Milliseconds 45',
  '        [Win32UI]::keybd_event([byte]$vk, 0, [Win32UI]::KEYUP, [IntPtr]::Zero)',
  '        if ($up) { [Win32UI]::keybd_event([byte]$SHIFT, 0, [Win32UI]::KEYUP, [IntPtr]::Zero) }',
  '        Start-Sleep -Milliseconds 70',
  '    }',
  '    Start-Sleep -Milliseconds $SettleMs',
  '}',
];
lines.splice(from, to - from + 1, ...nw);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ Type-Text 已改为 keybd_event');

const err = null;
