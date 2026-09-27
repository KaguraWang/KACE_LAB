const fs = require('fs');
const P = 'D:/ChemLabUnity/tools/ui.ps1';
let c = fs.readFileSync(P, 'utf8');

/* Type-Text 改用 SendInput 的 Unicode 路径 */
const a = c.indexOf('function Type-Text {');
const b = c.indexOf('function Shot {');
if (a < 0 || b < 0) { console.log('✗ 定位失败', a, b); process.exit(1); }
console.log('Type-Text 在', a, '.. Shot 在', b);
console.log('--- 旧内容 ---');
console.log(c.substring(a, b));

const nw = [
  'function Type-Text {',
  '    param([string]$Text, [int]$SettleMs = 500)',
  '    # 用 SendInput 的 Unicode 路径逐字发送。',
  '    # 试过 SendKeys.SendWait 和 keybd_event，两者在游戏里一个字符都收不到；',
  '    # Unicode 路径不依赖键盘布局，也不用手工处理 Shift。',
  '    foreach ($ch in $Text.ToCharArray()) {',
  '        [Win32UI]::TypeChar($ch) | Out-Null',
  '        Start-Sleep -Milliseconds 90',
  '    }',
  '    Start-Sleep -Milliseconds $SettleMs',
  '}',
  '',
  '',
].join('\n');

c = c.substring(0, a) + nw + c.substring(b);
fs.writeFileSync(P, c, 'utf8');
console.log('✓ Type-Text 已换成 SendInput');
console.log('  用 TypeChar:', c.includes('TypeChar'));
