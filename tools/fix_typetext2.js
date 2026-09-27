const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/ui.ps1';
let lines = fs.readFileSync(P, 'utf8').split('\n');

let from = -1, to = -1;
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('function Type-Text {')) { from = i; break; }
}
if (from < 0) { console.log('✗ 找不到 Type-Text'); process.exit(1); }
for (let i = from; i < from + 40; i++) {
  if (lines[i] !== undefined && lines[i] === '}') { to = i; break; }
}
console.log('Type-Text 在', from + 1, '..', to + 1);

const nw = [
  '# 字符 -> VK 码。放脚本作用域，避免函数里构造哈希表时被换行搞坏。',
  "$script:VKCHAR = 'abcdefghijklmnopqrstuvwxyz0123456789 -./'",
  '$script:VKCODE = @(0x41,0x42,0x43,0x44,0x45,0x46,0x47,0x48,0x49,0x4A,0x4B,0x4C,0x4D,',
  '                     0x4E,0x4F,0x50,0x51,0x52,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5A,',
  '                     0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,',
  '                     0x20,0xBD,0xBE,0xBF)',
  '',
  'function Type-Text {',
  '    param([string]$Text, [int]$SettleMs = 400)',
  '    # 按 VK 码直接注入按键。',
  '    # 原来用 SendKeys.SendWait，实测按键根本到不了程序',
  '    # （游戏里 Update 抓 Input.inputString 一个字符都没收到）。',
  '    $SHIFT = 0x10',
  '    foreach ($raw in $Text.ToCharArray()) {',
  '        $s = ([string]$raw).ToLower()',
  '        $i = $script:VKCHAR.IndexOf($s)',
  '        if ($i -lt 0) { continue }',
  '        $vk = $script:VKCODE[$i]',
  '        $isUpper = ([string]$raw) -cmatch "[A-Z]"',
  '        if ($isUpper) { [Win32UI]::keybd_event([byte]$SHIFT, 0, 0, [IntPtr]::Zero); Start-Sleep -Milliseconds 25 }',
  '        [Win32UI]::keybd_event([byte]$vk, 0, 0, [IntPtr]::Zero)',
  '        Start-Sleep -Milliseconds 45',
  '        [Win32UI]::keybd_event([byte]$vk, 0, [Win32UI]::KEYUP, [IntPtr]::Zero)',
  '        if ($isUpper) { [Win32UI]::keybd_event([byte]$SHIFT, 0, [Win32UI]::KEYUP, [IntPtr]::Zero) }',
  '        Start-Sleep -Milliseconds 70',
  '    }',
  '    Start-Sleep -Milliseconds $SettleMs',
  '}',
];
lines.splice(from, to - from + 1, ...nw);
fs.writeFileSync(P, lines.join('\n'), 'utf8');
console.log('✓ Type-Text 已重写');
