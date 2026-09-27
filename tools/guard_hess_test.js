const fs = require('fs');
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');

/* 把焓变场景那一段包进 try/catch，并保证 UiFont 已就绪 */
if (!sc.includes('HessTestHost')) { console.log('✗ 找不到测试段'); process.exit(1); }

sc = sc.replace(`                var host = new GameObject("HessTestHost");
                var scn = ChemLab.Game.EnthalpyScene.Show(host, ChemLab.Game.LabUI.UiFont);`,
`                if (ChemLab.Game.LabUI.UiFont == null)
                    ChemLab.Game.LabUI.UiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var host = new GameObject("HessTestHost");
                ChemLab.Game.EnthalpyScene scn = null;
                try {
                    scn = ChemLab.Game.EnthalpyScene.Show(host, ChemLab.Game.LabUI.UiFont);
                } catch (System.Exception ex) {
                    Say("   ✗ 建立焓变场景时抛异常：" + ex.GetType().Name + " " + ex.Message);
                    Say("     位置：" + (ex.StackTrace ?? "").Split('\\n')[0]);
                }`);

/* 把 else 分支里的内容整体再包一层 try */
sc = sc.replace(`                if (scn == null) Say("   ✗ 无法建立焓变场景");
                else {`,
`                if (scn == null) { Say("   ✗ 无法建立焓变场景"); TCheck("焓变场景可建立", false); }
                else { try {`);

sc = sc.replace(`                    UnityEngine.Object.Destroy(host);
                }
            }

            /* ---------- 宠物精灵图 ---------- */`,
`                    UnityEngine.Object.Destroy(host);
                } catch (System.Exception ex2) {
                    Say("   ✗ 测试过程抛异常：" + ex2.GetType().Name + " " + ex2.Message);
                    Say("     位置：" + (ex2.StackTrace ?? "").Split('\\n')[0]);
                    TCheck("焓变场景交互测试不抛异常", false);
                } }
            }

            /* ---------- 宠物精灵图 ---------- */`);

fs.writeFileSync(S, sc, 'utf8');
console.log('✓ 已加 try/catch');
