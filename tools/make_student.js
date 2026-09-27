/* =====================================================================
   建立学生端
     把教师端新增的宠物素材、脚本、skill 同步过去，
     但把 AI 配置清空（学生分发版默认没有接口）。
   ===================================================================== */
const fs = require('fs');
const path = require('path');

const T = 'D:/ChemLabUnity';
const S = 'D:/ChemLabUnity-Student';

/* 要同步的文件（教师端新增的） */
/* 要同步的文件。
   原来是写死的一张清单 —— 结果我后来新建的 OutlineFx.cs 没加进去，
   教师端能编译、学生端却报「找不到 OutlineFx」，而且很难发现。
   改成：Assets/Scripts 下的 .cs 全量同步（自动带上新建的文件），
   再补上 Editor、Resources 里那几个明确要的。 */
function walk(dir, out) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walk(p, out);
    else if (/\.(cs|shader|cginc|mat|meta)$/.test(e.name)) out.push(path.relative(T, p).replace(/\\/g, "/"));
  }
  return out;
}
const files = [
  ...walk(path.join(T, "Assets/Scripts"), []),
  ...walk(path.join(T, "Assets/Shaders"), []),
  ...walk(path.join(T, "Assets/Resources/ChemLab"), []),
  "Assets/Resources/Fluid3D.compute",
  "Assets/Resources/Fluid3D.compute.meta",
  "Assets/Editor/SpecRunner.cs",
  "Assets/Editor/ChemLabTools.cs",
  // Both editions must be checked against the same maintained CIE cases.
  "caie/spec/cases_all.json",
  "Assets/Resources/anon_sheet_a.png",
  "Assets/Resources/anon_sheet_a.png.meta",
  "Assets/Resources/anon_sheet_b.png",
  "Assets/Resources/anon_sheet_b.png.meta",
  "Assets/Resources/anon_skill.txt",
];

let n = 0, miss = [];
for (const f of files) {
  const src = path.join(T, f), dst = path.join(S, f);
  if (!fs.existsSync(src)) { miss.push(f); continue; }
  fs.mkdirSync(path.dirname(dst), { recursive: true });
  fs.copyFileSync(src, dst);
  n++;
}
console.log('同步文件:', n, '个');
if (miss.length) console.log('  缺失:', miss.join(', '));

/* 学生端的 AI 配置：留空 */
const studentCfg = {
  baseUrl: '',
  apiKey: '',
  model: 'deepseek-chat',
  isTeacher: false,
  timeout: 60,
  maxHistory: 12,
};
const resDir = path.join(S, 'Assets/Resources');
fs.writeFileSync(path.join(resDir, 'ai_config.json'), JSON.stringify(studentCfg, null, 2), 'utf8');
console.log('✓ 学生端 ai_config.json 已置空（baseUrl / apiKey 都是空串）');

/* Resources 里的 .txt/.json 需要 .meta 才会被 Unity 当 TextAsset 导入 */
function textMeta(guid) {
  return `fileFormatVersion: 2
guid: ${guid}
TextScriptImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
}
const { execSync } = require('child_process');
for (const [name, file] of [['anon_skill', 'anon_skill.txt'], ['ai_config', 'ai_config.json']]) {
  for (const base of [T, S]) {
    const p = path.join(base, 'Assets/Resources', file);
    if (!fs.existsSync(p)) continue;
    const metaPath = p + '.meta';
    if (!fs.existsSync(metaPath)) {
      const g = require('crypto').randomBytes(16).toString('hex');
      fs.writeFileSync(metaPath, textMeta(g), 'utf8');
      console.log('  写 meta:', path.relative(base, metaPath));
    }
  }
}

/* 教师端也要有一份 ai_config.json（从 js 版转过来） */
const teacherCfg = JSON.parse(fs.readFileSync(path.join(T, 'Assets/Resources/ai_config.json'), 'utf8'));
console.log('');
console.log('教师端 AI 配置：保留原配置，不输出密钥');
console.log('学生端 AI 配置：', JSON.stringify(studentCfg));
