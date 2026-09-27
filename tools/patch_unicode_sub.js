const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Chem/EqFormat.cs';
let c = fs.readFileSync(p, 'utf8');

const startMark = '    /// <summary>把 SO4^2- 变成';
const endMark = '    /* ---------------- 状态符号';
const i0 = c.indexOf(startMark), i1 = c.indexOf(endMark);
if (i0 < 0 || i1 < 0) { console.log('✗ 锚点', i0, i1); process.exit(1); }

const newBlock = [
'    /* ---------------- 分子式 → 带上下标的显示形式 ----------------',
'       注意：Unity 的 legacy Text **不支持 <sub>/<sup>**（只认 b/i/size/color/material/quad），',
'       所以不能靠富文本。这里改用 Unicode 的上下标字符，效果也更接近教科书排版。 */',
"    static readonly char[] SUB = { '₀','₁','₂','₃','₄','₅','₆','₇','₈','₉' };",
"    static readonly char[] SUP = { '⁰','¹','²','³','⁴','⁵','⁶','⁷','⁸','⁹' };",
'',
'    static string Sub(string digits) {',
'        var sb = new StringBuilder();',
"        foreach (var ch in digits) sb.Append(ch >= '0' && ch <= '9' ? SUB[ch - '0'] : ch);",
'        return sb.ToString();',
'    }',
'    static string Sup(string s) {',
'        var sb = new StringBuilder();',
'        foreach (var ch in s) {',
"            if (ch >= '0' && ch <= '9') sb.Append(SUP[ch - '0']);",
"            else if (ch == '-') sb.Append('⁻');",
"            else if (ch == '+') sb.Append('⁺');",
'            else sb.Append(ch);',
'        }',
'        return sb.ToString();',
'    }',
'',
'    /// <summary>把 SO4^2- 排成 SO₄²⁻、把 Ca(OH)2 排成 Ca(OH)₂</summary>',
'    public static string Pretty(string formula) {',
'        if (string.IsNullOrEmpty(formula)) return "";',
'        string f = formula, charge = "";',
"        int caret = f.IndexOf('^');",
'        if (caret >= 0) { charge = f.Substring(caret + 1); f = f.Substring(0, caret); }',
'        else {',
'            /* 没有 ^ 的离子（如 OH-、H+、Mn2+）把结尾的电荷拆出来 */',
'            var m = System.Text.RegularExpressions.Regex.Match(f, @"(\\d*[+-])$");',
'            if (m.Success && m.Length < f.Length) { charge = m.Value; f = f.Substring(0, f.Length - m.Length); }',
'        }',
'        var sb = new StringBuilder();',
'        int i = 0;',
'        while (i < f.Length) {',
'            char ch = f[i];',
'            sb.Append(ch);',
'            /* 元素符号或右括号后面的数字是下标 */',
"            if (char.IsLetter(ch) || ch == ')') {",
'                int j = i + 1;',
'                while (j < f.Length && char.IsDigit(f[j])) j++;',
'                if (j > i + 1) { sb.Append(Sub(f.Substring(i + 1, j - i - 1))); i = j; continue; }',
'            }',
'            i++;',
'        }',
'        if (!string.IsNullOrEmpty(charge)) sb.Append(Sup(charge));',
'        return sb.ToString();',
'    }',
'',
].join('\n');

c = c.slice(0, i0) + newBlock + c.slice(i1);

/* Normalize：把 Unicode 上下标还原成 ASCII，便于反查物种 */
c = c.replace('    /* ---------------- 供自检交叉验证',
`    /// <summary>把 Unicode 上下标还原成 ASCII，便于按分子式反查</summary>
    public static string Normalize(string s) {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder();
        foreach (var ch in s) {
            int k = System.Array.IndexOf(SUB, ch); if (k >= 0) { sb.Append((char)('0' + k)); continue; }
            k = System.Array.IndexOf(SUP, ch);
            if (k >= 0) { sb.Append((char)('0' + k)); continue; }
            if (ch == '⁻') { sb.Append('-'); continue; }
            if (ch == '⁺') { sb.Append('+'); continue; }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    /* ---------------- 供自检交叉验证`);

/* ParseSide 里用 Normalize 代替富文本清理 */
c = c.replace(/            \/\* 去掉富文本标记 \*\/[\s\S]*?\.Replace\("<sub>", ""\);/,
`            /* 把 Unicode 上下标还原成普通字符，方便按分子式反查物种 */
            t = Normalize(t);`);

fs.writeFileSync(p, c, 'utf8');
console.log('✓ Pretty 已改为 Unicode 上下标');
console.log('  Sub/Sup:', /static string Sup\(string s\)/.test(c));
console.log('  Normalize:', /public static string Normalize/.test(c));
console.log('  ParseSide 用 Normalize:', /t = Normalize\(t\);/.test(c));
