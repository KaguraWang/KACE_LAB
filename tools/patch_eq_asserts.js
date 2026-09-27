const fs = require('fs');
let n = 0;
const P = (p, a, b, label) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗', label || a.slice(0, 44)); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};
const S = 'D:/ChemLabUnity/Assets/Editor/SpecRunner.cs';

P(S, '                        case "water": {',
`                        /* ---- 反应方程式的规范化输出 ---- */
                        case "eq_format": {
                            /* 按中/英规范格式化指定规则，并检查关键片段 */
                            ChemLab.Chem.Rules.Init();
                            string want = asrt["rule"].Str("");
                            var rule = ChemLab.Chem.Rules.All.FirstOrDefault(x => x.Parsed && x.Eq == want);
                            if (rule == null) { pass = false; detail = "找不到规则: " + want; break; }
                            bool en = asrt["english"].Bool(false);
                            bool ion = asrt["ionic"].Bool(false);
                            string got = ChemLab.Chem.EqFormat.Format(rule, en, ion);
                            var checks = new List<string>();
                            foreach (var kv in (asrt["contains"].A ?? new List<JVal>()))
                                checks.Add(kv.Str(""));
                            pass = true;
                            foreach (var ck in checks)
                                if (!got.Contains(ck)) { pass = false; detail = $"缺少「{ck}」→ {got}"; }
                            if (pass) detail = got;
                            break;
                        }
                        case "eq_condition": {
                            ChemLab.Chem.Rules.Init();
                            string want2 = asrt["rule"].Str("");
                            var rule2 = ChemLab.Chem.Rules.All.FirstOrDefault(x => x.Parsed && x.Eq == want2);
                            if (rule2 == null) { pass = false; detail = "找不到规则: " + want2; break; }
                            string cl = ChemLab.Chem.EqFormat.CondLabel(rule2.Cond, asrt["english"].Bool(false));
                            string expect = asrt["expect"].Str("");
                            pass = cl == expect;
                            detail = $"条件标注 = 「{cl}」(期望「{expect}」)";
                            break;
                        }
                        case "water": {`);

fs.writeFileSync(S, fs.readFileSync(S, 'utf8'));
console.log('SpecRunner 补丁处数:', n);
