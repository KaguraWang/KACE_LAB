const fs = require('fs');
let n = 0;
const P = (p, a, b) => {
  let c = fs.readFileSync(p, 'utf8');
  if (!c.includes(a)) { console.log('  ✗ 锚点:', a.slice(0, 48).replace(/\n/g, '⏎')); return false; }
  fs.writeFileSync(p, c.replace(a, b), 'utf8'); n++; return true;
};
const E = 'D:/ChemLabUnity/Assets/Scripts/Chem/ChemEngine.cs';

/* ---- 1) 反应事件模型 ---- */
P(E, '    public class VesselEffects {',
`    /// <summary>一次刚发生的反应 —— 界面据此在容器处弹出方程式标签</summary>
    public class ReactionEvent {
        public Vessel Vessel;
        public Rules.Rule Rule;
        public double Extent;
        /// <summary>发生时刻（Lab.Clock）</summary>
        public double At;
        /// <summary>容器当时的世界位置（容器被移走后标签仍留在原处）</summary>
        public Vector3 Pos;
    }

    public class VesselEffects {`);

/* ---- 2) 事件清单 ---- */
P(E, '    public static readonly List<Vessel> Vessels = new List<Vessel>();',
`    public static readonly List<Vessel> Vessels = new List<Vessel>();

    /// <summary>最近发生的反应（按时间先后）。界面消费后会自行清掉超时的。</summary>
    public static readonly List<ReactionEvent> RecentReactions = new List<ReactionEvent>();
    /// <summary>事件清单上限，防止长时间运行堆积</summary>
    const int MaxReactionEvents = 24;

    public static void PushReaction(Vessel v, Rules.Rule r, double extent) {
        if (r == null || extent <= 1e-9) return;
        RecentReactions.Add(new ReactionEvent { Vessel = v, Rule = r, Extent = extent,
                                                At = Clock, Pos = v.Pos });
        while (RecentReactions.Count > MaxReactionEvents) RecentReactions.RemoveAt(0);
    }`);

/* ---- 3) 规则触发时记录 ---- */
P(E, `            Apply(v, r, extent);
            used.Add(key);
            did = true;
            if (v.Fx.NotedRules.Add(key)) Note(v, r.Obs, "rxn");`,
`            Apply(v, r, extent);
            used.Add(key);
            did = true;
            PushReaction(v, r, extent);
            if (v.Fx.NotedRules.Add(key)) Note(v, r.Obs, "rxn");`);

/* ---- 4) Reset 清空 ---- */
P(E, '    public static void Reset() {',
`    public static void Reset() {
        RecentReactions.Clear();`);

console.log('补丁处数:', n);
