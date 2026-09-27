const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Editor/SpecRunner.cs';
let c = fs.readFileSync(p, 'utf8');
let n = 0;
const R = (a, b) => { if (c.includes(a)) { c = c.replace(a, b); n++; return true; } return false; };

R(`                        case "water": {`,
`                        /* ---- 离子检验器材 ---- */
                        case "litmus_turned": {
                            /* 石蕊试纸是否变成了指定方向的颜色 */
                            var ti = FindTest(asrt);
                            string want = asrt["to"].Str("blue");   // blue / red / none
                            bool changedBlue = ti != null && ti.Tint == "#3a5ad8";
                            bool changedRed  = ti != null && ti.Tint == "#d83a3a";
                            bool bleached    = ti != null && ti.Tint == "#f0f0f0";
                            pass = want == "blue" ? changedBlue
                                 : want == "red"  ? changedRed
                                 : want == "bleach" ? bleached
                                 : !(changedBlue || changedRed || bleached);
                            detail = ti == null ? "没有这块试纸"
                                   : $"{ti.Name}({ti.StateText}) 变色={ti.Tint} → {(pass ? "符合" : "不符合")}期望 {want}";
                            break;
                        }
                        case "splint_state": {
                            var ti = FindTest(asrt);
                            string want = asrt["expect"].Str("Burning");
                            pass = ti != null && ti.Splint.ToString() == want;
                            detail = ti == null ? "没有木条" : $"木条状态 {ti.Splint}（期望 {want}）";
                            break;
                        }
                        case "popped": {
                            /* 是否发生了爆鸣 */
                            double pv = v.Fx.Pop;
                            if (asrt["min"].Num(0) > 0) { pass = pv >= asrt["min"].Num(0); }
                            else { pass = pv > 1e-9; }
                            detail = pass ? $"发生了爆鸣（强度 {pv:F2}）" : "没有发生爆鸣";
                            break;
                        }
                        case "test_result": {
                            /* 观察描述里是否含有指定关键词 */
                            var ti = FindTest(asrt);
                            string kw = asrt["contains"].Str("");
                            string got = ti != null ? (ti.LastResult ?? "") : lastTestResult;
                            pass = kw.Length == 0 || got.Contains(kw);
                            detail = got;
                            break;
                        }
                        case "water": {`);

fs.writeFileSync(p, c, 'utf8');
console.log('断言类型补丁:', n, '处   litmus_turned=', c.includes('case "litmus_turned"'),
            ' popped=', c.includes('case "popped"'));
