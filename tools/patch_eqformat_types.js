const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Scripts/Chem/EqFormat.cs';
let c = fs.readFileSync(p, 'utf8');
let n = 0;
const R = (a, b) => { if (c.includes(a)) { c = c.replace(a, b); n++; return true; } console.log('  ✗', a.slice(0,40)); return false; };

/* Rule / Species 嵌套在 Rules 类里，且 Coef 是 int */
c = c.replace(/\bRule r\b/g, 'Rules.Rule r');
c = c.replace(/List<Species>/g, 'List<Rules.Species>');
c = c.replace(/new Species \{/g, 'new Rules.Species {');
c = c.replace(/var sp = new Rules\.Species \{ Id = kv\.Key, Coef = System\.Math\.Abs\(kv\.Value\) \};/g,
              'var sp = new Rules.Species { Id = kv.Key, Coef = (int)System.Math.Round(System.Math.Abs(kv.Value)) };');
c = c.replace(/outList\.Add\(new Rules\.Species \{ Id = id, Coef = coef \}\);/g,
              'outList.Add(new Rules.Species { Id = id, Coef = (int)System.Math.Round(coef) });');
c = c.replace(/net\[sp\.Id\] = a - sp\.Coef;/g, 'net[sp.Id] = a - sp.Coef;');
c = c.replace(/net\[sp\.Id\] = a \+ sp\.Coef;/g, 'net[sp.Id] = a + sp.Coef;');
c = c.replace(/double g = 0;\s*\n\s*foreach \(var sp in left\.Concat\(right\)\) g = Gcd\(g, sp\.Coef\);\s*\n\s*if \(g > 1e-9\) \{ foreach \(var sp in left\) sp\.Coef \/= g; foreach \(var sp in right\) sp\.Coef \/= g; \}/g,
`double g = 0;
        foreach (var sp in left.Concat(right)) g = Gcd(g, sp.Coef);
        if (g > 1.5) {
            foreach (var sp in left)  sp.Coef = (int)System.Math.Round(sp.Coef / g);
            foreach (var sp in right) sp.Coef = (int)System.Math.Round(sp.Coef / g);
        }`);
c = c.replace(/if \(System\.Math\.Abs\(sp\.Coef - 1\.0\) > 1e-9\) sb\.Append\(Num\(sp\.Coef\)\);/g,
              'if (sp.Coef != 1) sb.Append(sp.Coef.ToString());');
c = c.replace(/return c\.ToString\("0\.##"\);/g, 'return c.ToString("0.##");');

fs.writeFileSync(p, c, 'utf8');
console.log('替换处数:', n);

/* 检查残留 */
const bad = [];
if (/[^.]\bRule r\b/.test(c)) bad.push('裸 Rule');
if (/List<Species>/.test(c)) bad.push('裸 Species');
if (bad.length) console.log('残留:', bad.join(', ')); else console.log('✓ 类型引用已全部限定');
