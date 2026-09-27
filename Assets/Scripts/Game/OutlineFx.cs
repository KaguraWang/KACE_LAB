/* =====================================================================
   轮廓高亮
     做法：反向壳（inverted hull）——
       把目标物体下每个 MeshFilter 的网格复制一份，稍微放大，
       用「只画背面（Cull Front）+ 纯色」的材质渲染。
       正面被原物体挡住，于是只在边缘露出一圈描边。
       这是 Built-in RP 下最省事、也最稳的轮廓做法（不需要后处理）。

     用法：
       var fx = OutlineFx.Attach(某个物体);
       fx.SetOn(true);    // 高亮
       fx.SetOn(false);   // 取消高亮

     注意：轮廓对象挂成目标的子节点，所以目标一移动轮廓自动跟随。
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChemLab.Game {

public class OutlineFx : MonoBehaviour {
    readonly List<Renderer> _shells = new List<Renderer>();
    /* 源 → 壳 的配对：器皿的网格每帧重建，外壳必须跟着换网格，
       否则描边会停留在第一次复制时的形状。 */
    readonly List<(MeshFilter src, MeshFilter dst, Renderer sr, Renderer dr, Transform st, Transform dt)> _pairs
        = new List<(MeshFilter, MeshFilter, Renderer, Renderer, Transform, Transform)>();
    Transform _root;
    Material _mat;
    bool _on;

    public static OutlineFx Attach(GameObject target, Color color, float thickness = 0.006f) {
        if (target == null) return null;
        var fx = target.GetComponent<OutlineFx>();
        if (fx == null) fx = target.AddComponent<OutlineFx>();
        fx.Build(color, thickness);
        return fx;
    }

    public static OutlineFx Attach(Transform target, Color color, float thickness = 0.006f) {
        return target == null ? null : Attach(target.gameObject, color, thickness);
    }

    void Build(Color color, float thickness) {
        _width = thickness;
        if (_root != null) {                 /* 重建前先清掉旧的 */
            Destroy(_root.gameObject);
            _shells.Clear();
        }
        _mat = MakeMat(color, thickness);

        var rgo = new GameObject("Outline");
        rgo.transform.SetParent(transform, false);
        _root = rgo.transform;
        _root.gameObject.SetActive(false);
        _pairs.Clear();

        /* 把每个带 MeshFilter 的子物体都复制一份外壳 */
        foreach (var mf in GetComponentsInChildren<MeshFilter>(true)) {
            if (mf == null || mf.sharedMesh == null) continue;
            if (mf.transform.IsChildOf(_root)) continue;
            if (SkipMesh(mf.transform)) continue;
            BuildShell(mf, thickness);
        }
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
            if (smr == null || smr.sharedMesh == null) continue;
            if (SkipMesh(smr.transform)) continue;
            BuildShellSkinned(smr, thickness);
        }
    }

    /// <summary>哪些网格不参与描边。</summary>
    /// <remarks>
    /// Fx 下的粒子是每帧重建的公告板四边形，描出来会变成一堆飘在空中的方块；
    /// Liquid / Sediment 在容器内部，描了会被玻璃透出来、把整个容器染蓝。
    /// 只描外壳就够表达「哪个东西被聚焦了」。
    /// </remarks>
    static bool SkipMesh(Transform t) {
        while (t != null) {
            var nm = t.name;
            if (nm == "Fx" || nm == "Liquid" || nm == "Sediment") return true;
            t = t.parent;
        }
        return false;
    }

    void BuildShell(MeshFilter src, float t) {
        var go = new GameObject("Shell_" + src.gameObject.name);
        go.transform.SetParent(_root, false);
        /* 保持和源同样的局部变换，然后按自身尺寸放大一点 */
        go.transform.localPosition = transform.InverseTransformPoint(src.transform.position);
        go.transform.localRotation = Quaternion.Inverse(transform.rotation) * src.transform.rotation;
        /* 缩放保持 1:1 —— 描边粗细由着色器沿法线外扩控制，
           按百分比放大会把小物体放大成一整块实心色。 */
        go.transform.localScale = src.transform.lossyScale;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = src.sharedMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = _mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _shells.Add(mr);
        _pairs.Add((src, mf, src.GetComponent<Renderer>(), mr, src.transform, go.transform));
    }

    void BuildShellSkinned(SkinnedMeshRenderer src, float t) {
        var go = new GameObject("Shell_" + src.gameObject.name);
        go.transform.SetParent(_root, false);
        var smr = go.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = src.sharedMesh;
        smr.bones = src.bones;
        smr.rootBone = src.rootBone;
        smr.sharedMaterial = _mat;
        smr.shadowCastingMode = ShadowCastingMode.Off;
        smr.receiveShadows = false;
        _shells.Add(smr);
    }

    float _width = 0.006f;

    static Material MakeMat(Color c, float width) {
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
        m.SetFloat("_Outline", width);
        return m;
    }

    void LateUpdate() {
        if (!_on) return;
        for (int i = 0; i < _pairs.Count; i++) {
            var p = _pairs[i];
            if (p.src == null || p.dst == null) continue;
            /* 网格换了就换过去（器皿的液体/玻璃网格是动态生成的） */
            if (p.dst.sharedMesh != p.src.sharedMesh) p.dst.sharedMesh = p.src.sharedMesh;
            /* 变换也跟着走 —— 源物体的局部变换可能被重建过程改掉 */
            if (p.dt != null && p.st != null) {
                p.dt.localPosition = transform.InverseTransformPoint(p.st.position);
                p.dt.localRotation = Quaternion.Inverse(transform.rotation) * p.st.rotation;
                p.dt.localScale = p.st.lossyScale;
                var rd = p.dr as MeshRenderer;
                if (rd != null && p.sr != null) rd.enabled = p.sr.enabled;
            }
        }
    }

    public void SetOn(bool on) {
        _on = on;
        if (_root == null) return;
        if (_root.gameObject.activeSelf != on) _root.gameObject.SetActive(on);
    }
    public bool IsOn => _on;

    /// <summary>目标重建过网格之后调它刷新外壳</summary>
    public void Rebuild(Color color, float thickness = 0.006f) {
        var keep = _on;
        Build(color, thickness);
        SetOn(keep);
    }
}

}
