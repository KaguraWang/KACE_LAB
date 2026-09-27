const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Editor/SpecRunner.cs';
let c = fs.readFileSync(p, 'utf8');
let n = 0;
const R = (a, b) => { if (c.includes(a)) { c = c.replace(a, b); n++; return true; } return false; };

// ---- 1) ObjState 末尾补上检验器材终态 ----
R(`        if (Lab.TrySolutionColor(v, out c)) {
            o.Put("rgb", MiniJson.Arr().Push(MiniJson.Num(Math.Round(c.r * 255)))
                                 .Push(MiniJson.Num(Math.Round(c.g * 255)))
                                 .Push(MiniJson.Num(Math.Round(c.b * 255))));
        }
        return o;`,
`        if (Lab.TrySolutionColor(v, out c)) {
            o.Put("rgb", MiniJson.Arr().Push(MiniJson.Num(Math.Round(c.r * 255)))
                                 .Push(MiniJson.Num(Math.Round(c.g * 255)))
                                 .Push(MiniJson.Num(Math.Round(c.b * 255))));
        }
        /* 离子检验器材的终态 —— 石蕊是否变色、木条处于哪一态、是否发生爆鸣 */
        var tarr = MiniJson.Arr();
        foreach (var ti in TestSet.All) {
            var td = MiniJson.Obj();
            td.Put("id", MiniJson.Str(ti.Id));
            td.Put("kind", MiniJson.Str(ti.Kind.ToString()));
            td.Put("name", MiniJson.Str(ti.Name));
            td.Put("moist", MiniJson.Num(ti.Moist ? 1 : 0));
            td.Put("splint", MiniJson.Str(ti.Splint.ToString()));
            td.Put("tint", MiniJson.Str(ti.Tint ?? ""));
            td.Put("result", MiniJson.Str(ti.LastResult ?? ""));
            tarr.Push(td);
        }
        o.Put("tests", tarr);
        o.Put("pop", MiniJson.Num(v.Fx.Pop));
        return o;`);

// ---- 2) 用例级变量：加检验器材表 ----
R(`            var vs = new Dictionary<string, Vessel>();
            var vars = new Dictionary<string, double>();`,
`            var vs = new Dictionary<string, Vessel>();
            var vars = new Dictionary<string, double>();
            var testVars = new Dictionary<string, TestItem>();
            var strVars = new Dictionary<string, string>();
            TestItem lastTest = null;`);

// ---- 3) FindTest 本地函数 ----
R(`            Vessel pick(JVal a) {`,
`            TestItem FindTest(JVal a) {
                string n2 = a["test"].Str("");
                if (n2.Length > 0 && testVars.ContainsKey(n2)) return testVars[n2];
                return lastTest;
            }

            Vessel pick(JVal a) {`);

// ---- 4) 修正插入的操作块 ----
R(`                        var ti = TestSet.Add(kind, new Vector3(0f, 0f, 0f));
                        var lt = new Obj();
                        lt["id"] = ti.Id; lt["kind"] = ti.Kind.ToString();
                        lastTest = ti; testVars[a["as"].Str("t")] = ti;
                        break;`,
`                        var ti = TestSet.Add(kind, new Vector3(0f, 0f, 0f));
                        lastTest = ti;
                        string tn2 = a["as"].Str("t");
                        if (!string.IsNullOrEmpty(tn2)) testVars[tn2] = ti;
                        break;`);

c = c.replace(/var ti = FindTest\(a, testVars, lastTest\);/g, 'var ti = FindTest(a);');
c = c.replace(`                        string vn = a["as"].Str("res");
                        if (!string.IsNullOrEmpty(vn)) varVals[vn] = r;
                        lastInsertResult = r;`,
`                        string vn = a["as"].Str("res");
                        if (!string.IsNullOrEmpty(vn)) strVars[vn] = r;
                        lastTestResult = r;`);

R('        string lastInsertResult = "";', '        string lastTestResult = "";');
c = c.replace(/lastInsertResult/g, 'lastTestResult');

fs.writeFileSync(p, c, 'utf8');
console.log(`补丁 ${n} 处  ObjState=${c.includes('"tests"')}  testVars=${c.includes('var testVars')}  FindTest=${c.includes('TestItem FindTest(')}`);
