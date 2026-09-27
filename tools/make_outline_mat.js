const fs = require('fs');

/* 1) 读出轮廓着色器的 guid */
const metaPath = 'D:/ChemLabUnity/Assets/Shaders/ChemLabOutline.shader.meta';
const meta = fs.readFileSync(metaPath, 'utf8');
const m = meta.match(/guid:\s*([0-9a-f]{32})/);
if (!m) { console.log('✗ 读不到着色器 guid'); process.exit(1); }
const shaderGuid = m[1];
console.log('轮廓着色器 guid =', shaderGuid);

/* 2) 造一个引用它的材质资源。
      为什么要这么做：Shader.Find 找不到「没有任何材质引用」的着色器 ——
      构建时会被剔除（内置 Standard 就是这么没的）。
      放一个材质在 Resources 里，着色器就一定会被打进包。 */
const matGuid = require('crypto').randomBytes(16).toString('hex');
const mat = `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 6
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: OutlineMat
  m_Shader: {fileID: 4800000, guid: ${shaderGuid}, type: 3}
  m_ShaderKeywords: 
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats: []
    m_Colors:
    - _Color: {r: 0.35, g: 0.85, b: 1, a: 1}
  m_BuildTextureStacks: []
`;
fs.writeFileSync('D:/ChemLabUnity/Assets/Resources/OutlineMat.mat', mat, 'utf8');
fs.writeFileSync('D:/ChemLabUnity/Assets/Resources/OutlineMat.mat.meta',
`fileFormatVersion: 2
guid: ${matGuid}
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`, 'utf8');
console.log('✓ OutlineMat.mat 已建，guid =', matGuid);

/* 3) OutlineFx 改成从 Resources 取材质 */
const O = 'D:/ChemLabUnity/Assets/Scripts/Game/OutlineFx.cs';
let o = fs.readFileSync(O, 'utf8');
o = o.replace(`    static Material MakeMat(Color c) {
        /* 用项目自己的 ChemLab/Outline —— 内置着色器在构建里会被剔除，
           Shader.Find("Standard") 在播放器里返回 null（已经栽过一次）。 */
        var sh = Shader.Find("ChemLab/Outline");
        if (sh == null) {
            Debug.LogError("[ChemLab] 找不到 ChemLab/Outline 着色器，轮廓高亮失效");
            return null;
        }
        var m = new Material(sh);
        m.SetColor("_Color", c);
        return m;
    }`,
`    static Material MakeMat(Color c) {
        /* 从 Resources 取材质资源，而不是 Shader.Find ——
           Shader.Find 找不到「没有任何材质引用」的着色器，构建时会被剔除
           （内置 Standard 就是这么没的，已经栽过一次）。
           放一个材质在 Resources 里引用它，就一定会被打进包。 */
        var tpl = Resources.Load<Material>("OutlineMat");
        if (tpl == null) {
            Debug.LogError("[ChemLab] 找不到 Resources/OutlineMat，轮廓高亮失效");
            return null;
        }
        var m = new Material(tpl);
        m.SetColor("_Color", c);
        return m;
    }`);
fs.writeFileSync(O, o, 'utf8');
console.log('✓ OutlineFx 改为 Resources.Load');
