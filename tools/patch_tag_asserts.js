const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 48).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};
const S = 'D:/ChemLabUnity/Assets/Editor/SpecRunner.cs';

/* ---- 标签视图的断言 ---- */
P(S, '                        case "water": {',
`                        /* ---- 容器标签的两种视图 ---- */
                        case "view_has": {
                            /* 指定视图里是否有某物种，且 mol / 浓度对得上 */
                            bool mol = asrt["molecular"].Bool(true);
                            var vv = Chem.VesselView.Lines(v, mol);
                            string sp2 = asrt["species"].Str("");
                            var hit = vv.FirstOrDefault(x => x.Id == sp2);
                            bool ok = hit != null;
                            string msg = ok ? $"{hit.Formula} {hit.Mol:G4} mol {hit.Conc:F4} M" : "未找到";
                            if (ok && asrt["mol"] != null) {
                                double want = asrt["mol"].Num(), tol = asrt["tol"].Num(1e-9);
                                if (Math.Abs(hit.Mol - want) > tol) { ok = false; msg += $" (mol 期望 {want:G4})"; }
                            }
                            if (ok && asrt["conc"] != null) {
                                double want = asrt["conc"].Num(), tol = asrt["tol"].Num(1e-4);
                                if (Math.Abs(hit.Conc - want) > tol) { ok = false; msg += $" (浓度期望 {want:F4})"; }
                            }
                            pass = ok;
                            detail = $"{(mol ? "具体物质" : "离子状态")}视图 " + msg;
                            break;
                        }
                        case "view_absent": {
                            bool mol = asrt["molecular"].Bool(true);
                            var vv = Chem.VesselView.Lines(v, mol);
                            string sp2 = asrt["species"].Str("");
                            bool found = vv.Any(x => x.Id == sp2);
                            pass = !found;
                            detail = $"{(mol ? "具体物质" : "离子状态")}视图里{(found ? "仍出现" : "不含")} {sp2}";
                            break;
                        }
                        case "view_count": {
                            bool mol = asrt["molecular"].Bool(true);
                            int cnt = Chem.VesselView.Lines(v, mol).Count;
                            int want = (int)asrt["expect"].Num(0);
                            pass = cnt == want;
                            detail = $"{(mol ? "具体物质" : "离子状态")}视图共 {cnt} 条（期望 {want}）";
                            break;
                        }
                        case "water": {`);

console.log('SpecRunner 补丁处数:', n);
