/* =====================================================================
   实验台交互（整体替换原来的 HandleInput）
     目标：
       · 左键点任何东西 → 聚焦到它（相机平滑看向它）
       · 点桌面/空白 → 取消聚焦
       · WASD → 在世界坐标里平移（相机相对），同时取消聚焦
       · 长按左键拖动 → 旋转视角（短按不算，避免和「点击聚焦」冲突）
       · 右键器皿 → 弹出针对它的操作选单
       · 器皿之外的东西（本生灯、三脚架、石蕊试纸、木条）现在也能拖
   ===================================================================== */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using ChemLab.Chem;

namespace ChemLab.Game {

public partial class LabInteraction : MonoBehaviour { }

public static class LabPick {

    public class Hit {
        public Vessel vessel;
        public Apparatus apparatus;
        public TestItem testItem;
        public Vector3 world;
        public bool Any => vessel != null || apparatus != null || testItem != null;
        public string Label {
            get {
                if (vessel != null) return VesselNames.Name(vessel.Kind);
                if (apparatus != null) return apparatus.Kind == ApparatusKind.BunsenBurner ? "本生灯" : "三脚架";
                if (testItem != null) return testItem.Name;
                return "";
            }
        }
        public Vector3 Center {
            get {
                if (vessel != null) { var g = VesselMesh.Get(vessel.Kind); return vessel.Pos + Vector3.up * (vessel.LiftY + g.H * 0.45f); }
                if (apparatus != null) return apparatus.Pos + Vector3.up * (apparatus.Kind == ApparatusKind.BunsenBurner ? 0.14f : 0.22f);
                if (testItem != null) return testItem.Pos + Vector3.up * (testItem.LiftY + 0.10f);
                return Vector3.zero;
            }
        }
    }

    /// <summary>屏幕点选：器皿优先，其次器材，最后检验道具。
    /// 用「屏幕距离 vs 按距离缩放后的半径」判定，近的优先。</summary>
    public static Hit Pick(Vector3 screen, Camera cam) {
        var result = new Hit();
        if (cam == null) return result;
        Physics.SyncTransforms();
        var ray = cam.ScreenPointToRay(screen);
        var hits = Physics.RaycastAll(ray);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        // Keep stopcock colliders in ordinary depth order: a glass vessel in
        // front must block picking a stopcock behind it.
        foreach (var h in hits) {
            var tv = h.collider.GetComponentInParent<TestItemView>();
            var av = h.collider.GetComponentInParent<ApparatusView>();
            var vv = h.collider.GetComponentInParent<VesselView>();
            // 玻璃中的细小检验道具仍可被直接拿起。
            if (tv != null) { result = new Hit { testItem = tv.Model, world = h.point }; break; }
            if (result.Any) continue;
            if (av != null) result = new Hit { apparatus = av.Model, world = h.point };
            else if (vv != null) result = new Hit { vessel = vv.Model, world = h.point };
        }
        return result;
    }

    static Hit LegacyPick(Vector3 screen, Camera cam) {
        var hit = new Hit();
        if (cam == null) return hit;
        float bestScore = float.MaxValue;

        foreach (var v in Lab.Vessels) {
            var g = VesselMesh.Get(v.Kind);
            var c = v.Pos + Vector3.up * (v.LiftY + g.H * 0.45f);
            var sc = cam.WorldToScreenPoint(c);
            if (sc.z <= 0) continue;
            float r = g.R * 2.2f / Mathf.Max(0.2f, sc.z) * (Screen.height * 0.5f / 0.53f);
            float d = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(sc.x, sc.y));
            float thresh = Mathf.Max(r, Screen.height * 0.08f);
            if (d < thresh) {
                float score = d - 10000f;            // 器皿优先
                if (score < bestScore) { bestScore = score; hit.vessel = v; hit.apparatus = null; hit.testItem = null; }
            }
        }
        foreach (var a in ApparatusSet.All) {
            var c = a.Pos + Vector3.up * 0.14f;
            var sc = cam.WorldToScreenPoint(c);
            if (sc.z <= 0) continue;
            float d = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(sc.x, sc.y));
            float thresh = Screen.height * 0.09f;
            if (d < thresh) {
                float score = d - 1000f;
                if (score < bestScore) { bestScore = score; hit.apparatus = a; hit.vessel = null; hit.testItem = null; }
            }
        }
        foreach (var t in TestSet.All) {
            var c = t.Pos + Vector3.up * (t.LiftY + 0.08f);
            var sc = cam.WorldToScreenPoint(c);
            if (sc.z <= 0) continue;
            float d = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(sc.x, sc.y));
            float thresh = Screen.height * 0.07f;
            if (d < thresh) {
                float score = d;
                if (score < bestScore) { bestScore = score; hit.testItem = t; hit.vessel = null; hit.apparatus = null; }
            }
        }

        /* 桌面交点，用作「点空白」的位置 */
        var ray = cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, Vector3.zero);
        hit.world = plane.Raycast(ray, out float tt) ? ray.GetPoint(tt) : Vector3.zero;
        return hit;
    }
}

}
