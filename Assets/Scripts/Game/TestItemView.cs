/* =====================================================================
   检验器材视图：石蕊试纸条、木条（含火焰与余烬）
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {

public class TestItemView : MonoBehaviour {
    public TestItem Model;
    public MeshRenderer Paper, Flame, Wood;
    Material _paperMat, _flameMat, _woodMat;
    public bool EmberVisible => Wood != null && Wood.gameObject.activeInHierarchy &&
        _woodMat != null && _woodMat.GetFloat("_Ember") > 0;

    static readonly Color RedCol  = new Color(0.85f, 0.23f, 0.23f);
    static readonly Color BlueCol = new Color(0.23f, 0.35f, 0.85f);
    static readonly Color WhiteCol = new Color(0.94f, 0.94f, 0.94f);
    static readonly Color WoodCol  = new Color(0.62f, 0.47f, 0.29f);

    public static TestItemView Create(TestItem t, Transform parent, Material paperTpl, Material woodTpl, Material flameTpl) {
        var go = new GameObject("Test_" + t.Kind + "_" + t.Id);
        go.transform.SetParent(parent, false);
        var tv = go.AddComponent<TestItemView>();
        tv.Model = t;

        if (t.Kind == TestItemKind.Splint) {
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = TestItemMesh.BuildSplint();
            tv._woodMat = new Material(woodTpl);
            tv._woodMat.SetFloat("_Seed", Mathf.Abs(t.Id.GetHashCode()%1000));
            mr.sharedMaterial = tv._woodMat;
            tv.Wood = mr;

            var fg = new GameObject("Flame");
            fg.transform.SetParent(go.transform, false);
            fg.transform.localPosition = new Vector3(0f, 0.605f, 0f);
            var fmf = fg.AddComponent<MeshFilter>();
            var fmr = fg.AddComponent<MeshRenderer>();
            fmf.sharedMesh = TestItemMesh.BuildFlame();
            tv._flameMat = new Material(flameTpl);
            tv._flameMat.SetFloat("_Height", 0.16f);
            tv._flameMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            tv._flameMat.SetColor("_CoreColor", new Color(1f,0.78f,0.20f,1f));
            tv._flameMat.SetColor("_Color", new Color(1f,0.24f,0.025f,0.88f));
            fmr.sharedMaterial = tv._flameMat;
            fmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tv.Flame = fmr;
            fg.SetActive(false);

        } else {
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = TestItemMesh.BuildLitmus();
            /* 试纸站直一点，便于看见 */
            go.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            tv._paperMat = new Material(paperTpl);
            tv._paperMat.SetColor("_Color", t.Kind == TestItemKind.RedLitmus ? RedCol : BlueCol);
            mr.sharedMaterial = tv._paperMat;
            tv.Paper = mr;
        }
        var collider = go.AddComponent<BoxCollider>();
        var bounds = go.GetComponent<MeshFilter>().sharedMesh.bounds;
        collider.center = bounds.center;
        collider.size = Vector3.Max(bounds.size, new Vector3(0.035f, 0.01f, 0.035f));
        return tv;
    }

    public void Sync() {
        var t = Model;
        transform.localPosition = t.Pos + Vector3.up * t.LiftY;
        if (t.Kind == TestItemKind.Splint) {
            if (_woodMat != null) {
                _woodMat.SetFloat("_Char", t.CharLevel);
                // The wood surface itself glows; an extra flame-shaped mesh
                // above the tip made extinguished splints look as if still alight.
                _woodMat.SetFloat("_Ember",t.Splint==SplintState.JustOut?Mathf.Clamp01(t.GlowLeft/GasTest.EmberSeconds):0);
            }
            if (Flame != null) {
                bool burning = t.Splint == SplintState.Burning;
                if (Flame.gameObject.activeSelf != burning) Flame.gameObject.SetActive(burning);
                if (burning) {
                    /* 火焰随剩余燃烧时间轻微抖动，看起来更真 */
                    float k = 0.85f + Mathf.PingPong(Time.time * 6f, 0.3f);
                    Flame.transform.localScale = new Vector3(k, k * (0.9f + t.BurnLeft * 0.01f), k);
                }
            }
        } else if (_paperMat != null) {
            /* 石蕊：变色后纸片跟着变；未湿润时颜色偏暗（干的纸） */
            Color baseCol = t.Kind == TestItemKind.RedLitmus ? RedCol : BlueCol;
            Color c = baseCol;
            if (t.Tint == "#3a5ad8") c = BlueCol;
            else if (t.Tint == "#d83a3a") c = RedCol;
            else if (t.Tint == "#f0f0f0") c = WhiteCol;
            if (!t.Moist) c *= 0.82f;
            _paperMat.SetColor("_Color", c);
        }
    }

    void OnDestroy() {
        if (_woodMat != null) Destroy(_woodMat);
        if (_paperMat != null) Destroy(_paperMat);
        if (_flameMat != null) Destroy(_flameMat);
        foreach(var filter in GetComponentsInChildren<MeshFilter>(true))
            if(filter.sharedMesh != null) Destroy(filter.sharedMesh);
    }
}

}
