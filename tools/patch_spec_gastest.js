const fs = require('fs');
const p = 'D:/ChemLabUnity/Assets/Editor/SpecRunner.cs';
let c = fs.readFileSync(p, 'utf8');

// 报告里带上检验器材的终态
if (!c.includes('"tests"')) {
  c = c.replace('            d["turbidity"] = Math.Round(v.Turbidity, 4);',
`            d["turbidity"] = Math.Round(v.Turbidity, 4);
            var tarr = new Arr();
            foreach (var ti in TestSet.All) {
                var td = new Obj();
                td["id"] = ti.Id;
                td["kind"] = ti.Kind.ToString();
                td["name"] = ti.Name;
                td["moist"] = ti.Moist;
                td["splint"] = ti.Splint.ToString();
                td["tint"] = ti.Tint ?? "";
                td["result"] = ti.LastResult ?? "";
                tarr.Add(td);
            }
            d["tests"] = tarr;
            d["vessel_pop"] = Math.Round(v.Fx.Pop, 3);`);
}

// 新操作
if (!c.includes('case "add_test"')) {
  c = c.replace('                    case "settle": Lab.Settle(v); break;',
`                    case "settle": Lab.Settle(v); break;
                    /* ---- 离子检验器材 ---- */
                    case "add_test": {
                        var kind = a["kind"].Str("red_litmus") switch {
                            "blue_litmus" => TestItemKind.BlueLitmus,
                            "splint"      => TestItemKind.Splint,
                            _             => TestItemKind.RedLitmus,
                        };
                        var ti = TestSet.Add(kind, new Vector3(0f, 0f, 0f));
                        var lt = new Obj();
                        lt["id"] = ti.Id; lt["kind"] = ti.Kind.ToString();
                        lastTest = ti; testVars[a["as"].Str("t")] = ti;
                        break;
                    }
                    case "moisten": {
                        var ti = FindTest(a, testVars, lastTest);
                        GasTest.Moisten(ti);
                        break;
                    }
                    case "light_splint": {
                        var ti = FindTest(a, testVars, lastTest);
                        GasTest.Light(ti);
                        break;
                    }
                    case "extinguish_splint": {
                        var ti = FindTest(a, testVars, lastTest);
                        GasTest.Extinguish(ti);
                        break;
                    }
                    case "insert": {
                        /* 把检验器材伸到容器口（对准气相） */
                        var ti = FindTest(a, testVars, lastTest);
                        string r = GasTest.Insert(v, ti);
                        string vn = a["as"].Str("res");
                        if (!string.IsNullOrEmpty(vn)) varVals[vn] = r;
                        lastInsertResult = r;
                        break;
                    }
                    case "advance": {
                        /* 无头环境手动推进器材状态（木条烧尽、余烬熄灭） */
                        double secs2 = a["seconds"].Num(1.0);
                        const double dt2 = 0.05;
                        for (double t2 = 0; t2 < secs2; t2 += dt2) TestSet.Tick((float)dt2);
                        break;
                    }`);
}

// 辅助变量与方法
if (!c.includes('TestItem lastTest')) {
  c = c.replace('        var vars = new Obj();',
`        var vars = new Obj();
        var testVars = new System.Collections.Generic.Dictionary<string, TestItem>();
        TestItem lastTest = null;
        string lastInsertResult = "";`);
}
if (!c.includes('TestItem FindTest(')) {
  c = c.replace('        // 断言前先统一 Settle',
`        TestItem FindTest(Obj a, System.Collections.Generic.Dictionary<string, TestItem> map, TestItem last) {
            string n = a["test"].Str("");
            if (!string.IsNullOrEmpty(n) && map.ContainsKey(n)) return map[n];
            return last;
        }

        // 断言前先统一 Settle`);
}
fs.writeFileSync(p, c, 'utf8');
console.log('SpecRunner 补丁:',
  'tests报告=' + c.includes('"tests"'),
  'add_test=' + c.includes('case "add_test"'),
  'insert=' + c.includes('case "insert"'),
  '辅助=' + c.includes('TestItem lastTest'));
