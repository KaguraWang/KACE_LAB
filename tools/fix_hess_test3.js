const fs = require('fs');
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EnthalpyScene.cs';
let lines = fs.readFileSync(E, 'utf8').split('\n');

/* 用祖先查找替掉字符串前缀判断 */
for (let i = 0; i < lines.length; i++) {
  if (lines[i].includes('string path = t.name;')) {
    lines.splice(i, 3,
      '        /* 直接往上找有没有叫 World 的祖先。',
      '           拼路径的话，从下往上拼出来的开头是根节点名，',
      '           拿 StartsWith("World/") 判会把全部节点误判为「不在」。 */',
      '        while (t != null) {',
      '            if (t.name == "World") return true;',
      '            t = t.parent;',
      '        }',
      '        return false;');
    fs.writeFileSync(E, lines.join('\n'), 'utf8');
    console.log('✓ TestNodeUnderWorld 已改成祖先查找');
    break;
  }
}

/* 自检里断言改对：只断言「跟着被移动节点的那一端」位移 */
const S = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemSelfTest.cs';
let sc = fs.readFileSync(S, 'utf8');
sc = sc.replace(`                        float d0 = Vector3.Distance(e0Before[0], e0After[0]);
                        float d1 = Vector3.Distance(e0Before[1], e0After[1]);
                        Say($"   起点位移 {d0:F1} px，箭尖位移 {d1:F1} px");
                        TCheck("拖节点时连线起点跟着变", d0 > 10f);
                        TCheck("拖节点时连线箭尖跟着变", d1 > 10f);
                    } else {
                        TCheck("拖节点时连线起点跟着变", false);
                        TCheck("拖节点时连线箭尖跟着变", false);
                    }`,
`                        float d0 = Vector3.Distance(e0Before[0], e0After[0]);
                        float d1 = Vector3.Distance(e0Before[1], e0After[1]);
                        Say($"   起点位移 {d0:F1} px，箭尖位移 {d1:F1} px");
                        /* 边 0 连的是节点 0 → 节点 1，我移动的是节点 1：
                           所以「箭尖」（挂在被移动的节点上）必须动，
                           「起点」（挂在没动的节点 0 上）保持不动才是对的。 */
                        TCheck("拖节点时，连到它那一端跟着变", d1 > 10f);
                        TCheck("另一端保持不动（说明连线确实是按节点位置重算的）", d0 < 2f);
                    } else {
                        TCheck("拖节点时，连到它那一端跟着变", false);
                        TCheck("另一端保持不动（说明连线确实是按节点位置重算的）", false);
                    }`);
fs.writeFileSync(S, sc, 'utf8');
console.log('✓ 自检断言已修正');
