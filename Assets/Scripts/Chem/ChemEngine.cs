/* =====================================================================
   容器模型 + 反应引擎
   —— 从 D:\ChemLab\js\engine.js 移植
   ===================================================================== */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChemLab.Chem {

/// <summary>加热方式：回流保留一切，蒸馏边生成边把易挥发产物拿走 —— 这决定了伯醇氧化成醛还是酸</summary>
public enum HeatMode { None, Heat, Reflux, Distil }

public enum VesselKind { Beaker, Flask, Tube, Cylinder, RoundBottom, Crucible, Dish, Bottle, Burette, Pipette }

/// <summary>一次刚发生的反应 —— 界面据此在容器处弹出方程式标签</summary>
public class ReactionEvent {
    /// <summary>Monotonic event id: the bounded history may discard its oldest entries.</summary>
    public long Sequence;
    public Vessel Vessel;
    public Rules.Rule Rule;
    public double Extent;
    /// <summary>发生时刻（Lab.Clock）</summary>
    public double At;
    /// <summary>容器当时的世界位置</summary>
    public Vector3 Pos;
}

public class VesselEffects {
    public float Bubble, Smoke, Glow;
        /// <summary>爆鸣强度（>0 表示刚发生一次爆鸣，界面据此播放声音并闪烁）</summary>
        public float Pop;
    public bool Mirror, Flame;
    /* 火焰剩余时长（秒）。Flame 原来是个 bool，设上之后没有任何地方清掉，
       一次反应之后容器会一直「烧」下去。 */
    public float FlameT;
    public string FlameColor;
    public string Deposit;
    public int Explode;
    public HashSet<string> NotedRules = new HashSet<string>();
    public HashSet<string> FiredSpecials = new HashSet<string>();
}

public class Vessel {
    // CIE teaching model: all gases use PV=nRT, including at elevated pressure.
    // Units: bar, litres, mol, kelvin. No real-gas compressibility correction.
    public const double GasConstant = 0.08314462618;
    public double GasRT => GasConstant*Math.Max(1,Temp+273.15);
    static int _seq;
    public string Id;
    public VesselKind Kind;

    /* 台面上的位置（米） */
    public Vector3 Pos;

    public readonly Dictionary<string,double> Solutes      = new Dictionary<string,double>();
    public readonly Dictionary<string,double> Undissolved  = new Dictionary<string,double>();
    public readonly Dictionary<string,double> Precipitates = new Dictionary<string,double>();
    public readonly Dictionary<string,double> Gases        = new Dictionary<string,double>();

    /// <summary>化合物账本：记录「以中性化合物形式投入、且还没被消耗掉」的量。
    /// 「显示成具体物质」就是靠它 —— 光看离子表是**推不出**原本是什么化合物的：
    /// NaCl + CuSO4 和 CuCl2 + Na2SO4 给出的离子完全相同，必须记账。</summary>
    public readonly Dictionary<string, double> Ledger = new Dictionary<string, double>();
    public double Water;          // mL
    public float  Temp = 25f;
    // Physical vessel state survives disconnecting/reconnecting an apparatus.
    // Kept separate from contents so a transfer does not carry the glass heat.
    public double GlassTemperatureC=25;
    public double PressureBar=1.01325,EscapedMoles;
    public bool Sealed;
    public double TrappedAirMoles;
    // Nominal working capacity is not the total enclosed cavity volume.
    public double InteriorCapacityMl => Game.VesselMesh.InnerCapacity(Kind)*1000.0;
    public double HeadspaceLitres => Math.Max(1e-9,(InteriorCapacityMl-Volume()-SolidVolume())/1000);
    public void SetSealed(bool value) {
        if(value==Sealed)return;
        if(value)TrappedAirMoles=Math.Max(0,PressureBar*HeadspaceLitres/GasRT-Gases.Values.Sum());
        Sealed=value;
        if(!value){TrappedAirMoles=0;PressureBar=1.01325;}
        PhaseProperties.Reconcile(this,!value);
        if(!value){EscapedMoles+=Gases.Values.Sum();Gases.Clear();}
    }
    public string PhaseNotice="";
    public readonly HashSet<string> PhaseNotices=new HashSet<string>();
    public bool   Stirring, Lit;

    /* ---- 器材交互状态 ---- */
    /// <summary>悬停在另一个容器上方（用于倾倒）：A.AttachedTo = B</summary>
    public Vessel AttachedTo;
    public DistillationAssembly Distillation;
    public DistillationAssembly ReceivingDistillation {get; internal set;}
    /// <summary>吸附在本生灯上</summary>
    public Apparatus Burner;
    /// <summary>吸附在三脚架上</summary>
    public Apparatus TripodRef;
    /// <summary>倾倒滑动条的值 0–1（界面显示用）</summary>
    public float PourAmount = 0.5f;
    public double MeteredFlowMlPerSecond;
    /// <summary>离台面的抬升高度（吸附到别的器皿/器材上方时用）</summary>
    public float LiftY;

        public HeatMode Mode = HeatMode.None;
        public bool Heating {
            get { return Mode != HeatMode.None; }
            set { Mode = value ? HeatMode.Heat : HeatMode.None; }
        }
    public float  Turbidity;
    public string Tint;
    public bool   ConcAcid;

    public VesselEffects Fx = new VesselEffects();
    internal bool _saidHot, _saidBoil, _hasLastColor;
    internal bool HasCO2SettleSnapshot;
    internal double LastAvailableCO2AtSettle;
    internal Color _lastColor;
    internal bool _settleGuard;

    public Vessel(VesselKind kind, Vector3 pos) {
        _seq++;
        Id = "v" + _seq;
        Kind = kind;
        Pos = pos;
    }

    public double Capacity => NominalCapacity(Kind);
    public static double NominalCapacity(VesselKind kind) => kind switch {
        VesselKind.Tube => 34, VesselKind.Cylinder => 100, VesselKind.Crucible => 50,
        VesselKind.Burette => 50, VesselKind.Pipette => 25,
        VesselKind.Dish => 80, _ => 250,
    };

    public double Volume() => Water + LiquidVolume();

    // Displacement is material volume, not the bounding volume of a porous powder bed.
    public double SolidVolume() {
        double total=0;
        foreach(var set in new[]{Undissolved,Precipitates})foreach(var kv in set)
            if(ChemData.Sub.TryGetValue(kv.Key,out var s)&&PhaseProperties.Phase(kv.Key,Temp)=="s")
                total+=Math.Max(0,kv.Value)*s.Mass/Math.Max(0.01,PhaseProperties.Density(kv.Key,Temp));
        return total;
    }

    /// <summary>容器内残留物质的总质量（g）。气体逸出不计入 ——
    /// 坩埚灼烧测 Ar 这类「质量损失」实验就靠它。</summary>
    public double Mass() {
        double m = Water;                       // 水按 g 计
        foreach (var kv in Solutes)      m += ChemData.MassOf(kv.Key, kv.Value);
        foreach (var kv in Undissolved)  m += ChemData.MassOf(kv.Key, kv.Value);
        foreach (var kv in Precipitates) m += ChemData.MassOf(kv.Key, kv.Value);
        if(Sealed)foreach(var kv in Gases)m+=ChemData.MassOf(kv.Key,kv.Value);
        return m;
    }

    public double LiquidVolume() {
        double v = 0;
        // A deposit may melt before the next phase reconciliation, including in
        // mixtures. Count its current liquid displacement in capacity/transfer.
        foreach (var map in new[]{Undissolved,Precipitates})foreach (var kv in map) {
            var s = ChemData.Sub[kv.Key];
            if (PhaseProperties.Phase(kv.Key,Temp) == "l" && !s.IsIon) v += kv.Value * s.Mass / PhaseProperties.Density(kv.Key,Temp);
        }
        foreach (var kv in Solutes) {
            var s = ChemData.Sub[kv.Key];
            if (!s.IsIon && (PhaseProperties.Phase(kv.Key,Temp)=="l"||(Sealed&&kv.Key=="c2h5oh"))) v += kv.Value * s.Mass / Math.Max(0.01, PhaseProperties.Density(kv.Key,Temp));
        }
        return v;
    }

    public float FillFraction() => (float)Math.Min(1.0, Volume() / Capacity);

    public bool Has(string id) =>
        Solutes.ContainsKey(id) || Undissolved.ContainsKey(id) ||
        Gases.ContainsKey(id) || Precipitates.ContainsKey(id);
    public bool HasIon(string id)   => Solutes.ContainsKey(id);
    public bool HasPrecip(string id)=> Precipitates.ContainsKey(id);

    public double Get(string id) {
        /* 水是溶剂，本身不是 Solutes 里的物种。TakeAny 早就会处理它，
           但 Get 不会 —— 这个不对称导致任何把水写在反应物一侧的规则
           都会因为「反应物量为 0」而永远不反应。按体积折算成摩尔。 */
        if (id == "h2o") return Water / Formula.MolarMass("H2O");
        double v = 0;
        Solutes.TryGetValue(id, out double a); v += a;
        Undissolved.TryGetValue(id, out double b); v += b;
        Precipitates.TryGetValue(id, out double c); v += c;
        Gases.TryGetValue(id, out double d); v += d;
        return v;
    }

    public double Ph {
        get {
            double V = Math.Max(1e-12, Water / 1000.0);
            Solutes.TryGetValue("H+", out double hp);
            Solutes.TryGetValue("OH-", out double ohp);
            double h = hp / V, oh = ohp / V;
            if (h > oh && h > 1e-7) return Math.Max(0, -Math.Log10(Math.Max(1e-14, h - oh)));
            if (oh > h && oh > 1e-7) return Math.Min(14, 14 + Math.Log10(Math.Max(1e-14, oh - h)));
            return 7;
        }
    }

    /* ---------------- 加入物质 ---------------- */
    public void AddSubstance(string id, double mol) {
        if (!ChemData.Sub.TryGetValue(id, out var s)) return;
        if (s.State == "g") { AddTo(Gases, id, mol); return; }
        Introduce(id, mol, Water > 1e-9);
    }

    /// <summary>加入体系。返回 false 表示物质不存在</summary>
    public bool Introduce(string id, double mol, bool inWater, bool aqueousStock = true) {
        if (!ChemData.Sub.TryGetValue(id, out var s)) return false;
        if(double.IsNaN(mol)||double.IsInfinity(mol)||mol<=0) return false;
        // aq 是试剂自身的水溶液状态，不依赖接收容器预先有水。
        inWater |= aqueousStock && s.State == "aq";
        if (s.IsIon) { AddTo(Solutes, id, mol); return true; }
        if (id.EndsWith("_conc")) ConcAcid = true;

        ChemData.Diss.TryGetValue(id, out var dis);
        /* 把「浓/冰态变体」的分子形式统一到规范 id。
           ch3cooh_glacial 的 MoleculeAs = ch3cooh：有水时它走电离路径会被统一，
           无水时（酯化这类无水实验）却原样存成 ch3cooh_glacial，
           而规则里写的是 CH3COOH —— 双方对不上，反应就永远不触发。
           必须在存入之前就规范化。 */
        if (dis != null && !string.IsNullOrEmpty(dis.MoleculeAs) && dis.MoleculeAs.Length > 1)
            id = dis.MoleculeAs;
        bool solubleHere = inWater && (s.Sol == 'k' || s.Sol == 's' || (dis != null && !dis.None)) && s.Sol != 'n';
        bool reactsWithWater = s.Sol == 'r';

        if (reactsWithWater && inWater) { AddTo(s.State=="s"?Undissolved:Solutes, id, mol); return true; }

        // Solid portlandite is reconciled together with Ca2+/OH- by its
        // temperature-dependent saturation relation, including common ions.
        if(id=="caoh2"&&inWater){AddTo(Undissolved,id,mol);return true;}

        // Water being present does not make an organic mixture pure water. The
        // legacy qualitative solubility flag cannot stand in for a missing
        // solvent-specific saturation curve for a solid reagent.
        if(solubleHere&&PhaseProperties.Phase(id,Temp)=="s"&&!SolventProperties.IsAqueousModelValid(this)
            &&!PhaseProperties.TryCapacity(this,id,out _)){
            AddTo(Undissolved,id,mol);
            PhaseNotice="当前混合溶剂/温压缺少该固体的定量溶解度，暂保留固体，未假定完全溶解";
            return true;
        }

        /* 溶解焓：固体溶入水中会吸热或放热（P3 热化学题的核心） */
        double dhDis = ChemData.DissolveHeat(id);

        if (solubleHere && s.State != "g") {
            if(id!="nh3"&&PhaseProperties.TryCapacity(this,id,out double capacity)) {
                double dissolve=Math.Min(mol,Math.Max(0,capacity-PhaseProperties.Dissolved(this,id)));
                double excess=mol-dissolve;
                if(excess>1e-12){AddTo(Precipitates,id,excess);PhaseProperties.Notice(this,id,false);}
                mol=dissolve;if(mol<=1e-12)return true;
            }
            if(Math.Abs(dhDis)>1e-9&&Water>1&&SolventProperties.IsAqueousModelValid(this))Lab.ApplyReactionHeat(this,mol,dhDis);
            if (dis != null && !dis.None && dis.Ions.Count > 0) {
                /* 记入账本：这是「具体物质」视图的数据来源 */
                AddTo(Ledger, id, mol);
                double d = dis.Weak ? dis.Degree : 1.0;
                if(dis.WaterProduced<0)d=Math.Min(d,Water/(mol*-dis.WaterProduced*Formula.MolarMass("H2O")));
                Water=Math.Max(0,Water+mol*d*dis.WaterProduced*Formula.MolarMass("H2O"));
                foreach (var kv in dis.Ions) AddTo(Solutes, kv.Key, kv.Value * mol * d);
                if (d < 1.0) {
                    var mid = string.IsNullOrEmpty(dis.MoleculeAs) ? id : dis.MoleculeAs;
                    AddTo(Solutes, mid, mol * (1.0 - d));
                }
            } else AddTo(Solutes, id, mol);
            return true;
        }
        /* 气态物质要进气相。原来这里没有出口，CH4/O2 这类直接被塞进「未溶物」，
           气相永远是空的 —— 木条、石蕊这些气体检验就什么也检不出来。 */
        if (s.State == "g") { AddTo(Gases, id, mol); return true; }
        if (s.State == "l" || s.State == "aq") { AddTo(Solutes,id,mol); return true; }
        AddTo(Undissolved, id, mol);
        return true;
    }

    static void AddTo(Dictionary<string,double> map, string id, double mol) {
        map.TryGetValue(id, out double cur);
        map[id] = cur + mol;
    }
    static void DelFrom(Dictionary<string,double> map, string id) {
        if (map.TryGetValue(id, out double v) && v <= 1e-12) map.Remove(id);
    }

    // Recover only recorded, fully dissociated anhydrous solids. Never invent
    // counterions or combine arbitrary ions into a guessed salt after drying.
    public void RecoverDrySalts() {
        if(SolventProperties.HasSolvent(this))return;
        foreach(var entry in Ledger.ToArray()) {
            if(!ChemData.Sub.TryGetValue(entry.Key,out var salt)||salt.State!="s"
                ||!ChemData.Diss.TryGetValue(entry.Key,out var dis)||dis.None||dis.Weak
                ||dis.WaterProduced!=0||dis.Ions.Count==0)continue;
            double amount=entry.Value;
            foreach(var ion in dis.Ions){Solutes.TryGetValue(ion.Key,out double available);amount=Math.Min(amount,available/ion.Value);}
            if(amount<=1e-12)continue;
            foreach(var ion in dis.Ions){Solutes[ion.Key]-=amount*ion.Value;DelFrom(Solutes,ion.Key);}
            Ledger[entry.Key]-=amount;DelFrom(Ledger,entry.Key);
            AddTo(Precipitates,entry.Key,amount);
        }
    }

    public void AddWater(double ml) { Water += ml; WaterIce.Reconcile(this); DissolvePending(); }

    public void DissolvePending() {
        // Repair legacy/incoming state before solubility rules: a liquid is never sediment.
        foreach(var map in new[]{Undissolved,Precipitates}) foreach(var id in map.Keys.ToList()) {
            if(ChemData.Sub.TryGetValue(id,out var phase)&&PhaseProperties.Phase(id,Temp)!="s") {
                double quantity=map[id];map.Remove(id);
                if(!SolventProperties.HasSolvent(this))AddTo(PhaseProperties.Phase(id,Temp)=="g"?Gases:Solutes,id,quantity);
                else Introduce(id,quantity,Water>0);
            }
        }
        if (Water <= 1e-9) return;
        foreach (var id in Undissolved.Keys.ToList()) {
            if(id=="caoh2")continue;
            var s = ChemData.Sub[id];
            ChemData.Diss.TryGetValue(id, out var dis);
            bool can = s.Sol == 'k' || s.Sol == 's' || (dis != null && !dis.None);
            if (!can || s.State == "g" || s.Sol == 'r') continue;
            double mol = Undissolved[id];
            Undissolved.Remove(id);
            Introduce(id, mol, true);
        }
    }

    /// <summary>取用某物种，返回实际取到的量</summary>
    /// <summary>某种化合物按电离表能提供多少目标离子</summary>
    double LedgerIonShare(string compound, string ion, out double perMol) {
        perMol = 0;
        ChemData.Diss.TryGetValue(compound, out var dis);
        if (dis == null || dis.Ions == null) return 0;
        /* 电离表里的系数是「每个分子解离出几个该离子」，是整型 */
        int iv; if (!dis.Ions.TryGetValue(ion, out iv)) return 0;
        double v = iv;
        perMol = v;
        double have; Ledger.TryGetValue(compound, out have);
        return have * v;
    }

    /// <summary>消耗了某个离子就按贡献比例回扣账本里的化合物，
    /// 这样已经反应掉的物质不会继续出现在「具体物质」视图里。</summary>
    void ChargeLedger(string ion, double mol) {
        if (mol <= 1e-12 || Ledger.Count == 0) return;
        var shares = new List<KeyValuePair<string, double>>();
        double total = 0;
        foreach (var kv in Ledger) {
            double per; double s = LedgerIonShare(kv.Key, ion, out per);
            if (s > 1e-12) { shares.Add(new KeyValuePair<string, double>(kv.Key, s)); total += s; }
        }
        if (total <= 1e-12) return;
        double use = Math.Min(mol, total);
        foreach (var kv in shares) {
            double per; LedgerIonShare(kv.Key, ion, out per);
            double take = use * (kv.Value / total) / Math.Max(per, 1e-12);
            double have; Ledger.TryGetValue(kv.Key, out have);
            double left = have - take;
            if (left <= 1e-12) Ledger.Remove(kv.Key); else Ledger[kv.Key] = left;
        }
    }

    public double TakeAny(string id, double mol) {
        if (id == "h2o") {
            double use = Math.Min(Water / Formula.MolarMass("H2O"), mol);
            Water -= use * Formula.MolarMass("H2O"); return use;
        }
        if (ChemData.Sub.TryGetValue(id, out var vst) && vst.IsIon) ChargeLedger(id, mol);
        double left = mol;
        foreach (var map in new[]{ Solutes, Undissolved, Gases, Precipitates }) {
            if (left <= 1e-12) break;
            if (!map.TryGetValue(id, out double has) || has <= 0) continue;
            double use = Math.Min(has, left);
            if (has - use < 1e-12) map.Remove(id); else map[id] = has - use;
            left -= use;
        }
        return mol - left;
    }

    public void Produce(string id, double mol) {
        if (mol <= 1e-12) return;
        if (id == "h2o") { Water += mol * Formula.MolarMass("H2O"); return; }
        if (!ChemData.Sub.TryGetValue(id, out var s)) return;
        if (s.IsIon || s.State == "aq") { AddTo(Solutes, id, mol); return; }
        if (s.State == "g") { AddTo(Gases, id, mol); return; }
        if (s.State == "s" && (ChemData.Precip.ContainsKey(s.Formula) || (s.Sol == 'n' && Water > 0))) {
            AddTo(Precipitates, id, mol); return;
        }
        Introduce(id, mol, Water > 1e-9);
    }
}

/* ===================================================================== */
public class LogEntry {
    public float T; public string VesselId; public string Text; public string Kind;
}

public static class Lab {
    public static readonly List<Vessel> Vessels = new List<Vessel>();

    /// <summary>最近发生的反应（按时间先后）。界面消费后会自行清掉超时的。</summary>
    public static readonly List<ReactionEvent> RecentReactions = new List<ReactionEvent>();
    /// <summary>事件清单上限，防止长时间运行堆积</summary>
    const int MaxReactionEvents = 24;
    static long _reactionSequence;

    public static void PushReaction(Vessel v, Rules.Rule r, double extent) {
        if (r == null || extent <= 1e-9) return;
        RecentReactions.Add(new ReactionEvent { Sequence = ++_reactionSequence,
                                                Vessel = v, Rule = r, Extent = extent,
                                                At = Clock, Pos = v.Pos });
        while (RecentReactions.Count > MaxReactionEvents) RecentReactions.RemoveAt(0);
    }
    public static readonly List<LogEntry> Log = new List<LogEntry>();
    public static float Clock;
    public static int RxnCount;

    static bool _ready;
    public static void EnsureReady() {
        if (_ready) return;
        _ready = true;
        ChemData.EnsureBuilt();
    }

    public static void Reset() {
        foreach(var v in Vessels)v.Distillation?.Detach();
        RecentReactions.Clear(); Vessels.Clear(); Log.Clear(); Clock = 0; RxnCount = 0; }

    public static Vessel AddVessel(VesselKind kind) {
        EnsureReady();
        int n = Vessels.Count;
        /* 在台面中央的**可见区**里循环摆放。
           原来沿 x = -0.95 + (n%5)*0.92 一直往一边排，
           相机是侧看的，排到第 5 个就跑到屏幕左侧、被左边的界面面板整个挡住
           （实测第 5 个落在屏幕 x=504，而面板占到 500）。 */
        int k = n;
        float fx = -0.90f + (k % 3) * 0.85f;      // -0.90 / -0.05 / 0.80
        float fz = 0.55f - (k / 3) * 0.40f;       // 前排 → 后排，避免叠在一起
        var pos = new Vector3(fx, 0f, Mathf.Clamp(fz, -2.0f, 1.4f));
        var v = new Vessel(kind, pos);
        Vessels.Add(v);
        Note(v, $"放置了一个{VesselNames.Name(kind)}", "setup");
        return v;
    }

    public static void RemoveVessel(Vessel v) {
        if(v==null||!Vessels.Remove(v))return;
        v.Distillation?.Detach();
        v.ReceivingDistillation?.Detach();
        foreach(var other in Vessels)if(other.Distillation?.Receiver==v)other.Distillation.Detach();
        // Removal also occurs outside the context menu (reset/tests/other UI).
        // Never leave a live dispenser holding a removed receiver reference.
        foreach(var other in Vessels)if(other.AttachedTo==v){
            other.AttachedTo=null;other.LiftY=0;other.MeteredFlowMlPerSecond=0;
        }
        if(v.Burner!=null&&v.Burner.Holds==v)v.Burner.Holds=null;
        if(v.TripodRef!=null&&v.TripodRef.Holds==v)v.TripodRef.Holds=null;
        v.AttachedTo=null;v.Burner=null;v.TripodRef=null;v.MeteredFlowMlPerSecond=0;
    }

    public static void Note(Vessel v, string text, string kind = "obs") {
        Log.Add(new LogEntry { T = Clock, VesselId = v != null ? v.Id : null, Text = text, Kind = kind });
        if (Log.Count > 400) Log.RemoveAt(0);
    }

    /* ---------------- 加试剂 ---------------- */
    public static void AddReagent(Vessel v, string subId, double grams) {
        EnsureReady();
        if (!ChemData.Sub.TryGetValue(subId, out var s)) return;
        double mol = grams / s.Mass;
        if (ChemData.WaterReactive.Contains(subId) && v.Water > 1e-9) { WaterReaction(v, subId, mol); return; }
        v.Introduce(subId, mol, v.Water > 1e-9);
        string amt = grams >= 1 ? $"{grams:F1} g" : $"{(grams * 1000):F0} mg";
        Note(v, $"加入 {amt} {s.Name}（{s.Formula}）", "add");
        Settle(v);
    }

    public static void AddLiquid(Vessel v, string subId, double ml) {
        EnsureReady();
        if(double.IsNaN(ml)||double.IsInfinity(ml)||ml<=0)return;
        if (!ChemData.Sub.TryGetValue(subId, out var s)) return;
        if (subId == "h2o") { v.AddWater(ml); Note(v, $"加入 {ml:F0} mL 水", "add"); Settle(v); return; }
        double mol = MolesFor(subId, ml);
        // 储备水溶液同时带入溶剂体积；此前只加入溶质，空烧杯里因此没有水。
        if(s.State=="aq") v.Water+=ml;
        if (ChemData.WaterReactive.Contains(subId) && v.Water > 1e-9) { WaterReaction(v, subId, mol); return; }
        v.Introduce(subId, mol, v.Water > 1e-9);
        Note(v, $"加入 {ml:F1} mL {s.Name}", "add");
        Settle(v);
    }

    /// <summary>水溶液试剂按储备液摩尔浓度折算；纯液体用密度</summary>
    public static double MolesFor(string id, double ml) {
        var s = ChemData.Sub[id];
        if (ChemData.Molarity.TryGetValue(id, out double m)) return ml / 1000.0 * m;
        if (s.State == "aq") return ml / 1000.0 * 1.0;
        return ml * s.Density / s.Mass;
    }

    /* 活泼金属与水 */
    static readonly Dictionary<string,(string eq, string text)> WaterRxn = new() {
        {"na",   ("2Na + 2H2O -> 2Na+ + 2OH- + H2", "钠浮在水面上熔成小球快速游动，发出嘶嘶声，很快消失")},
        {"k",    ("2K + 2H2O -> 2K+ + 2OH- + H2",   "钾遇水立即燃烧，发出紫色火焰并发生轻微爆炸")},
        {"ca",   ("Ca + 2H2O -> Ca2+ + 2OH- + H2",  "钙粒表面剧烈产生气泡，逐渐溶解，溶液变浑浊")},
        {"na2o", ("Na2O + H2O -> 2Na+ + 2OH-",      "固体剧烈水解放出大量热，溶液显强碱性")},
        {"cao",  ("CaO + H2O -> Ca(OH)2",           "氧化钙与水反应，生成氢氧化钙；只有可溶部分形成石灰水")},
        {"na2o2",("2Na2O2 + 2H2O -> 4Na+ + 4OH- + O2", "淡黄色固体溶解并放出大量热，产生气泡")},
        {"al2s3",("Al2S3 + 6H2O -> 2Al(OH)3 + 3H2S","立即水解，产生有臭鸡蛋气味的气体并析出白色沉淀")},
        {"c2h5ona",("C2H5ONa + H2O -> C2H5OH + Na+ + OH-", "固体溶解，溶液显强碱性")},
    };

    public static void WaterReaction(Vessel v, string subId, double mol) {
        if(double.IsNaN(mol)||double.IsInfinity(mol)||mol<=0)return;
        if (!WaterRxn.TryGetValue(subId, out var t)) {
            v.Introduce(subId, mol, false); Settle(v); return;
        }
        // This API receives NEW reagent. Register it before limiting the reaction
        // against both reagent and water; otherwise the incoming mass disappears.
        v.Introduce(subId,mol,false,false);
        var waterRule=new Rules.Rule{Eq=t.eq};Rules.ParseEquationPublic(waterRule);
        double extent=Apply(v,waterRule,mol);
        if(extent>0){
            Note(v, t.text, "rxn");
            if(Math.Abs(ChemData.RuleHeat(t.eq))<=1e-9)
                ThermalMixing.RecordNotice(v,"该遇水反应缺少已核对的定量焓变；未按固定摄氏度修改温度");
            if(waterRule.Products.Any(p=>ChemData.Sub.TryGetValue(p.Id,out var product)&&product.State=="g"))
                v.Fx.Bubble = Math.Max(v.Fx.Bubble, 3f);
            if (subId == "k") { v.Fx.Explode = 1; v.Fx.Flame = true; v.Fx.FlameT = 6f; v.Fx.FlameColor = "#c060ff"; }
        }
        Settle(v);
    }

    /* ---------------- 加热 / 点燃 ---------------- */
    public static void SetHeat(Vessel v, bool on) {
        if(v.Distillation!=null){
            if(!on)v.Distillation.Stop();
            else if(!v.Distillation.Start(out string message)){Note(v,message,"obs");return;}
        }else v.Heating=on;
        Note(v,on?"开始加热":"停止加热","setup");
    }
    public static void SetStir(Vessel v, bool on)  { v.Stirring = on; if (on) Note(v, "搅拌中", "setup"); }

    static double KnownIgnitionHeat(string equation){
        double direct=ChemData.RuleHeat(equation);
        if(Math.Abs(direct)>1e-9)return direct;
        // The Hess-law teaching table includes selected measured combustion
        // enthalpies; the general reaction-rule table does not duplicate them.
        double table=HessLaw.Lookup(equation,out _);
        return double.IsNaN(table)?0:table;
    }

    public static void Ignite(Vessel v) {
        v.Lit = true;
        bool fired = false;
        bool oxygenLimited = false;
        foreach (var r in Rules.All) {
            if (!r.Parsed || r.Cond != "ignite") continue;
            // O2 alone used to match every ignition rule. Check the actual fuel,
            // then allow an open vessel to take oxygen from ambient air.
            double fuelExtent=double.PositiveInfinity;
            bool hasFuel=false;
            int oxygenCoefficient=0;
            foreach(var reactant in r.Reactants){
                if(reactant.Id=="o2"){oxygenCoefficient+=reactant.Coef;continue;}
                hasFuel=true;
                fuelExtent=Math.Min(fuelExtent,v.Get(reactant.Id)/reactant.Coef);
            }
            if(!hasFuel||fuelExtent<=1e-10)continue;
            double ambientOxygen=0;
            if(!v.Sealed&&oxygenCoefficient>0){
                ambientOxygen=Math.Max(0,fuelExtent*oxygenCoefficient-v.Get("o2"));
                if(ambientOxygen>1e-10){
                    v.Gases.TryGetValue("o2",out double storedOxygen);
                    v.Gases["o2"]=storedOxygen+ambientOxygen;
                }
            }
            double extent=Apply(v,r,double.PositiveInfinity,dryRun:true);
            if(extent<=1e-10){
                if(ambientOxygen>0){
                    double remaining=v.Gases["o2"]-ambientOxygen;
                    if(remaining<=1e-12)v.Gases.Remove("o2");else v.Gases["o2"]=remaining;
                }
                if(oxygenCoefficient>0)oxygenLimited=true;
                continue;
            }
            double applied=Apply(v,r,extent);
            if(applied<=1e-10)continue;
            if(ambientOxygen>1e-10)Note(v,"燃烧所需氧气来自周围空气","setup");
            Note(v,r.Obs,"rxn");
            fired=true;
            if(oxygenCoefficient>0&&applied<fuelExtent-1e-10)oxygenLimited=true;
        }
        /* 通用 C/H/O 有机物完全燃烧：C_a H_b O_c +
           (a+b/4-c/2)O2 -> aCO2 + (b/2)H2O。
           不为含卤/氮/硫的燃料臆造副产物或定量焓变。 */
        foreach(var map in new[]{v.Undissolved,v.Solutes,v.Gases}){
            foreach(var id in map.Keys.ToList()){
                if(!map.TryGetValue(id,out double mol)||mol<=1e-10||!ChemData.Sub.TryGetValue(id,out var s))continue;
                if(s.IsIon||!s.Atoms.Keys.All(atom=>atom=="C"||atom=="H"||atom=="O"))continue;
                s.Atoms.TryGetValue("C",out int nc);s.Atoms.TryGetValue("H",out int nh);s.Atoms.TryGetValue("O",out int no);
                if(nc<=0||nh<=0)continue;
                double oxygenPerMol=nc+nh/4.0-no/2.0;
                if(oxygenPerMol<=0)continue;
                double availableOxygen=v.Get("o2");
                double extent=v.Sealed?Math.Min(mol,availableOxygen/oxygenPerMol):mol;
                if(extent<=1e-10){oxygenLimited=true;continue;}
                if(v.Sealed&&extent<mol-1e-10)oxygenLimited=true;
                double oxygenUsed=extent*oxygenPerMol;
                double ambientUsed=Math.Max(0,oxygenUsed-availableOxygen);
                map[id]=mol-extent;
                if(map[id]<=1e-12)map.Remove(id);
                v.TakeAny("o2",Math.Min(oxygenUsed,availableOxygen));
                v.Produce("co2",extent*nc);
                v.Produce("h2o",extent*nh/2.0);
                if(ambientUsed>1e-10)Note(v,"燃烧所需氧气来自周围空气","setup");
                Note(v,$"{s.Name}{(extent<mol-1e-10?"因氧气不足仅部分":"")}燃烧，生成二氧化碳和水","rxn");
                v.Fx.Flame=true;v.Fx.FlameT=8f;v.Fx.FlameColor="#ffbb44";
                string Coef(double n)=>Math.Abs(n-1)<1e-10?"":n.ToString("0.########",System.Globalization.CultureInfo.InvariantCulture);
                string equation=s.Formula+" + "+Coef(oxygenPerMol)+"O2 -> "+Coef(nc)+"CO2 + "+Coef(nh/2.0)+"H2O";
                double enthalpy=KnownIgnitionHeat(equation);
                if(Math.Abs(enthalpy)>1e-9)ApplyReactionHeat(v,extent,enthalpy);
                else ThermalMixing.RecordNotice(v,"该燃烧缺少已核对的定量焓变；未按固定摄氏度修改温度");
                fired=true;
            }
        }
        if (!fired) {
            var ion = v.Solutes.Keys.FirstOrDefault(k => ChemData.FlameColor.ContainsKey(k));
            if (ion != null) {
                v.Fx.Flame = true; v.Fx.FlameT = 6f; v.Fx.FlameColor = ChemData.FlameColor[ion];
                Note(v, $"火焰呈特征颜色（{ion} 的焰色反应）", "rxn");
                fired = true;
            }
        }
        if (!fired) Note(v, oxygenLimited?"氧气不足，未发生燃烧":"没有可燃烧的物质", "obs");
    }

    /* ---------------- 热化学 ---------------- */
    public const double HeatCapacity = ThermalMixing.WaterCpJPerGramK; // 旧水溶液教学常量

    /// <summary>按反应/溶解焓改变液体焓，反求温度；未知模型不外推。</summary>
    public static void ApplyReactionHeat(Vessel v, double mol, double dhKJ) {
        if(v==null||mol<=0||double.IsNaN(mol)||double.IsInfinity(mol)||double.IsNaN(dhKJ)||double.IsInfinity(dhKJ))return;
        /* ΔH 是「体系吸收的热」。吸热过程（ΔH>0）体系从溶液里拿热 → 溶液降温，
           所以温度变化要取负号。放热过程（ΔH<0）则升温。 */
        if(ThermalMixing.TryTemperatureAfterHeat(v,-mol*dhKJ*1000,out float temperature,out string notice))v.Temp=temperature;
        ThermalMixing.RecordNotice(v,notice);
    }

    /// <summary>按温度变化反算焓变（kJ/mol）—— 测试从 ΔT 正向反推时用</summary>
    public static double EnthalpyFromTemp(Vessel v, double dT, double mol) {
        if(double.IsNaN(mol)||double.IsInfinity(mol))return double.NaN;
        if (mol <= 1e-12) return 0;
        return -EnergyFromTemp(v,dT)/1000/mol;
    }

    /// <summary>以v.Temp为初温、dT为温差，给出对应液体能量（J），不改库存。</summary>
    public static double EnergyFromTemp(Vessel v, double dT) {
        if(!ThermalMixing.TryEnergyForTemperatureChange(v,dT,out double energy,out string notice)){
            ThermalMixing.RecordNotice(v,notice);return double.NaN;
        }
        ThermalMixing.RecordNotice(v,notice);
        return energy;
    }

    /* ---------------- 倾倒 / 清空 ---------------- */

    /// <summary>按比例倾倒。frac 由界面上的滑动条给出（0–1）。
    /// 用倾倒量而不是「一次倒 72%」，这样学生能像真实操作一样控制倒多少。
    /// 返回转移的凝聚相近似体积(mL)，包括非水液体及夹带固体，不包含头部气体。</summary>
    public static double PourFraction(Vessel from, Vessel to, double frac, bool quiet = false) {
        if (from == null || to == null || from == to) return 0;
        if(MeteredTransfer.IsTool(from)||to.Kind==VesselKind.Pipette)return 0;
        if(double.IsNaN(frac)||double.IsInfinity(frac))return 0;
        if(from.Sealed||to.Sealed){if(!quiet)Note(from,"密闭容器不能直接倾倒，请先打开容器","setup");return 0;}
        frac = Math.Max(0, Math.Min(1, frac));
        double sourceVolume=from.Volume()+from.SolidVolume();
        double room=Math.Max(0,(to.Kind==VesselKind.Burette?1:.95)*Math.Min(to.Capacity,to.InteriorCapacityMl)-to.Volume()-to.SolidVolume());
        if(sourceVolume>1e-9)frac=Math.Min(frac,room/sourceVolume);
        if(frac<=0)return 0;
        ThermalMixing.TryTargetTemperature(from,to,frac,out float mixedTemp,out string thermalNotice);
        bool iceTransfer=IceWaterTransfer.TryPredict(from,to,frac,frac,out var iceState);
        if(iceTransfer&&iceState.VolumeMl>(to.Kind==VesselKind.Burette?1:.95)*Math.Min(to.Capacity,to.InteriorCapacityMl)+1e-9)return 0;
        double w = from.Water * frac;
        if (w <= 1e-9) w = 0;
        bool moved = false;

        // Headspace gas does not follow the poured condensed inventory. Gas
        // transfer requires a gas connection; open venting is a separate process.
        foreach (var map in new[]{ from.Solutes, from.Precipitates, from.Undissolved }) {
            foreach (var id in map.Keys.ToList()) {
                double mv = map[id] * frac;
                if (mv <= 1e-12) continue;
                map[id] -= mv;
                if (map[id] <= 1e-12) map.Remove(id);
                var dst = map == from.Precipitates ? to.Precipitates
                        : map == from.Undissolved ? to.Undissolved
                        : to.Solutes;
                dst.TryGetValue(id, out double cur); dst[id] = cur + mv;
                moved = true;
            }
        }
        foreach(var id in from.Ledger.Keys.ToList()){
            double amount=from.Ledger[id]*frac;from.Ledger[id]-=amount;
            if(from.Ledger[id]<=1e-12)from.Ledger.Remove(id);
            to.Ledger.TryGetValue(id,out double existing);to.Ledger[id]=existing+amount;
        }
        from.Water -= w; to.Water += w;
        to.Temp=mixedTemp;
        if(iceTransfer){iceState.Apply(to);thermalNotice=IceWaterTransfer.Notice;}
        ThermalMixing.RecordNotice(to,thermalNotice);
        double transferredVolume=(moved||w>0)?sourceVolume*frac:0;
        if (!quiet && (moved || w > 0)) {
            Note(to, $"倒入约 {transferredVolume:G4} mL（{frac * 100:F0}%）", "add");
            Settle(to); Settle(from);
        }
        return transferredVolume;
    }

    public static void Pour(Vessel from, Vessel to) => PourFraction(from, to, 0.72);

    public static void Empty(Vessel v) {
        v.Sealed=false;v.TrappedAirMoles=0;v.PressureBar=1.01325;v.EscapedMoles=0;
        v.PhaseNotice="";v.PhaseNotices.Clear();
        v.Solutes.Clear(); v.Undissolved.Clear(); v.Gases.Clear(); v.Precipitates.Clear(); v.Ledger.Clear();
        v.Water = 0; v.Tint = null; v.Turbidity = 0; v.Temp = 25; v.ConcAcid = false;
        v.Fx.Bubble = 0; v.Fx.Smoke = 0; v.Fx.Mirror = false; v.Fx.Flame = false;
        v.HasCO2SettleSnapshot=false;v.LastAvailableCO2AtSettle=0;
        v.Fx.NotedRules.Clear(); v.Fx.FiredSpecials.Clear();
        Note(v, "倒空并洗净容器", "setup");
    }

    /* ================================================================
       反应主循环
       ================================================================ */
    public static void Settle(Vessel v) {
        if(PourTransaction.IsActive)return;
        EnsureReady();
        if (v._settleGuard) return;
        v._settleGuard = true;
        try {
            WaterIce.Reconcile(v);
            v.Solutes.TryGetValue("co2",out double dissolvedCO2);
            v.Gases.TryGetValue("co2",out double headspaceCO2);
            double availableCO2=dissolvedCO2+headspaceCO2;
            bool carbonateBefore=v.Precipitates.TryGetValue("caco3",out double carbonateAmount)&&carbonateAmount>1e-9;
            bool continuedCO2=carbonateBefore&&availableCO2>1e-9&&
                              (!v.HasCO2SettleSnapshot||availableCO2>v.LastAvailableCO2AtSettle+1e-9);
            v.DissolvePending();
            var used = new HashSet<string>();
            for (int round = 0; round < 8; round++) if (!OneRound(v, used, continuedCO2)) break;
            PhaseProperties.Reconcile(v);
            v.Solutes.TryGetValue("co2",out dissolvedCO2);
            v.Gases.TryGetValue("co2",out headspaceCO2);
            v.LastAvailableCO2AtSettle=dissolvedCO2+headspaceCO2;
            v.HasCO2SettleSnapshot=true;
            UpdateAppearance(v);
            AutoObservations(v);
            RunSpecials(v);
        } finally { v._settleGuard = false; }
    }

    static bool OneRound(Vessel v, HashSet<string> used, bool continuedCO2) {
        bool did = false;

        if(CalciumHydroxideEquilibrium.Reconcile(v))did=true;

        /* 1) 沉淀生成 */
        foreach (var pr in ChemData.PrecipRules) {
            if(pr.Id=="caoh2")continue; // handled by the measured saturation model above
            v.Solutes.TryGetValue(pr.Cat, out double c);
            v.Solutes.TryGetValue(pr.An, out double a);
            if (c <= 1e-9 || a <= 1e-9) continue;
            if (!ChemData.Sub.TryGetValue(pr.Id, out var s)) continue;
            if (!ChemData.Precip.TryGetValue(s.Formula, out var info)) continue;
            double extent = Math.Min(c / pr.NC, a / pr.NA);
            if (extent <= 1e-9) continue;
            v.TakeAny(pr.Cat, extent * pr.NC);
            v.TakeAny(pr.An,  extent * pr.NA);
            v.Precipitates.TryGetValue(pr.Id, out double cur);
            v.Precipitates[pr.Id] = cur + extent;
            v.Turbidity = Math.Min(1f, v.Turbidity + 0.45f);
            Note(v, $"产生{info.Desc}的{s.Name}（{s.Formula}）", "rxn");
            var precipRule = new Rules.Rule {
                Eq = (pr.NC == 1 ? "" : pr.NC.ToString()) + pr.Cat + " + "
                   + (pr.NA == 1 ? "" : pr.NA.ToString()) + pr.An + " -> " + s.Formula,
                Precip = s.Formula
            };
            Rules.ParseEquationPublic(precipRule);
            PushReaction(v, precipRule, extent);
            did = true;
        }

        /* 2) 沉淀被酸溶解 */
        v.Solutes.TryGetValue("H+", out double hp);
        if (hp > 1e-7) {
            foreach (var pid in v.Precipitates.Keys.ToList()) {
                var s = ChemData.Sub[pid];
                if (!ChemData.Precip.TryGetValue(s.Formula, out var info) || !info.Acid) continue;
                var rule = Rules.All.FirstOrDefault(r => r.Parsed && r.Reactants.Any(x => x.Id == pid));
                if (rule == null) continue;
                double mol = v.Precipitates[pid];
                if (Apply(v, rule, mol) > 1e-9) {
                    Note(v, $"{s.Name}沉淀逐渐溶解", "rxn");
                    v.Turbidity = Math.Max(0, v.Turbidity - 0.3f);
                    did = true;
                }
            }
        }

        /* 3) CIE 9701 通用有机规则 —— 必须排在按分子硬编码的旧规则之前。
           否则像「C2H5Br + NaOH → C2H5OH」这种旧写法会先把底物吃掉，
           消去那一路永远轮不到，条件分支就形同虚设。 */
        if (OrganicRound(v, used)) { did = true; }

        /* 4) 普通反应规则 */
        foreach (var r in Rules.All) {
            if (!r.Parsed) continue;
            if (!CheckCond(v, r)) continue;
            string key = r.Eq + "|" + r.Obs;
            if (used.Contains(key)) continue;
            if(r.Cond=="wet_bicarbonate"&&!continuedCO2)continue;
            double extent = Apply(v, r, double.PositiveInfinity, dryRun: true);
            if (extent <= 1e-9) continue;
            Apply(v, r, extent);
            used.Add(key);
            did = true;
            if (v.Fx.NotedRules.Add(key)) Note(v, r.Obs, "rxn");
        }

        return did;
    }

    /* =================================================================
       CIE 9701 通用有机规则
       规则给出「反应物家族 + 条件 + 可用试剂 + 产物家族」，
       具体产物由 ChemData.OrgMap 查表（键含产物家族 —— 同一反应物
       在不同条件下产物不同，比如卤代烃水解成醇、消去成烯烃）。
       ================================================================= */

    /* 反应物存在的判定下限。UI 里一次加量只有 1.5 mmol 量级，
       用 0.03 mol 做门槛的话什么都不会反应 */
    const double Eps = 1e-4;

    static bool HasTok(Vessel v, string t) {
        switch (t) {
            case "oh-":    return v.Get("OH-") > Eps;
            case "h+":     return v.Get("H+")  > Eps;
            case "cn-":    return v.Get("CN-") > Eps;
            case "halide": return v.Get("Cl-") > Eps || v.Get("Br-") > Eps || v.Get("I-") > Eps;
        }
        if (v.Get(t) > Eps) return true;

        /* 关键：试剂在水里可能是以离子形式存在的。
           k2cr2o7 会电离成 K+ 和 Cr2O7^2-，按分子式查永远是 0。
           用现成的电离表泛化处理，不要硬编码别名。 */
        ChemData.Dissoc d;
        if (ChemData.Diss.TryGetValue(t, out d) && d.Ions.Count > 0
            && d.Ions.All(kv=>v.Get(kv.Key)>Eps*kv.Value))return true;

        /* 浓/稀变体视为同一试剂 */
        if (v.Get(t + "_conc") > Eps || v.Get(t + "_dil") > Eps) return true;
        if (t.EndsWith("_conc") && v.Get(t.Substring(0, t.Length - 5)) > Eps) return true;
        return false;
    }

    static bool CheckOrgCond(Vessel v, string cond) {
        if (string.IsNullOrEmpty(cond)) return true;
        foreach (var c in cond.Split(',')) {
            switch (c.Trim()) {
                case "reflux":  if (v.Mode != HeatMode.Reflux) return false; break;
                case "distil":  if (v.Mode != HeatMode.Distil) return false; break;
                case "heat":    if (v.Temp < 62) return false; break;
                /* 水多走取代（水解成醇），醇多走消去（成烯烃）。
                   两个条件必须互补，否则水解会永远抢先 */
                /* 注意 v.Water 的单位是毫升，不是摩尔 */
                case "aq":      if (v.Water <= 3.0) return false; break;
                /* 乙醇溶液条件：水必须很少，否则水解会抢先，消去根本轮不到 */
                /* 乙醇溶液：乙醇的体积要盖过水的体积（mol × 46 / 0.789 = mL） */
                case "ethanol": if (v.Get("c2h5oh") * 58.3 <= v.Water) return false; break;
                default:        break;   /* conc / catal 不额外设限 */
            }
        }
        return true;
    }

    static bool OrganicRound(Vessel v, HashSet<string> used) {
        foreach (var r in ChemData.OrgRules) {
            var ids = new List<string>(v.Solutes.Keys);
            foreach (var k in v.Undissolved.Keys) if (!ids.Contains(k)) ids.Add(k);

            foreach (var fromId in ids) {
                string fam;
                if (!ChemData.Family.TryGetValue(fromId, out fam) || fam != r.From) continue;
                string toId;
                if (!ChemData.OrgMap.TryGetValue(fam + "|" + fromId + "|" + r.To, out toId)) continue;
                if (!ChemData.Sub.ContainsKey(fromId) || !ChemData.Sub.ContainsKey(toId)) continue;
                if (v.Get(fromId) <= Eps) continue;
                // Acyl substitution consumes its actual nucleophile, not an
                // arbitrary minimum of 3 mL water (alcoholysis can be nonaqueous).
                if (fam!="acyl_chloride"&&!CheckOrgCond(v, r.Cond)) continue;

                string key = "org|" + r.From + "|" + fromId + "|" + r.To;
                if (used.Contains(key)) continue;

                bool ok = false;
                foreach (var tok in r.Reagents.Split(',')) {
                    string t = tok.Trim();
                    if (t.Length == 0) continue;
                    if (t == "h2o" || t == "o2") { ok = true; break; }
                    if (HasTok(v, t)) { ok = true; break; }
                }
                if (!ok && !(fam=="carboxylic_acid"&&r.To=="ester"&&v.Get("c2h5oh")>Eps)
                    && !(fam=="diazonium"&&r.To=="azo"&&v.Get("phoh")>Eps)) continue;

                if((fam=="aldehyde"&&r.To=="primary_alcohol")||(fam=="ketone"&&r.To=="secondary_alcohol")){
                    // A carbonyl cannot become an alcohol merely because a reducer is
                    // present. This aqueous NaBH4 net equation accounts for boron,
                    // sodium, oxygen and the four transferable hydrogens. LiAlH4
                    // needs a separate anhydrous reaction/work-up model.
                    // https://openstax.org/books/organic-chemistry/pages/19-7-nucleophilic-addition-of-hydride-and-grignard-reagents-alcohol-formation
                    if(v.Get("nabh4")<=1e-10){
                        if(v.Get("lialh4")>1e-10){
                            string hydrideWarning="氢化铝锂需要无水还原及单独水解后处理模型；当前水相条件不执行转化";
                            v.PhaseNotice=hydrideWarning;
                            if(v.PhaseNotices.Add("organic-lialh4-unsupported"))Note(v,hydrideWarning,"obs");
                        }
                        continue;
                    }
                    var reduction=new Rules.Rule{Eq="4"+ChemData.Sub[fromId].Formula+" + NaBH4 + 2H2O -> 4"+ChemData.Sub[toId].Formula+" + NaBO2",Organic=true};
                    Rules.ParseEquationPublic(reduction);
                    if(!reduction.Parsed||Apply(v,reduction,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,r.Obs,"rxn");return true;
                }

                if(fam=="arene"&&r.To=="halogenoarene"){
                    // Benzene bromination requires bromine and an iron catalyst.
                    // https://openstax.org/books/organic-chemistry/pages/16-1-electrophilic-aromatic-substitution-reactions-bromination
                    if(v.Get("br2")<=1e-10||(v.Get("fe")<=1e-10&&v.Get("fecl3")<=1e-10))continue;
                    var bromination=new Rules.Rule{Eq=ChemData.Sub[fromId].Formula+" + Br2 -> "+ChemData.Sub[toId].Formula+" + HBr",Organic=true};
                    Rules.ParseEquationPublic(bromination);
                    if(!bromination.Parsed||Apply(v,bromination,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,r.Obs,"rxn");return true;
                }

                if(fam=="primary_amine"&&r.To=="diazonium"){
                    // Aqueous nitrous acid plus chloride-containing acid, at 0-5 C.
                    // https://openstax.org/books/organic-chemistry/pages/24-8-reactions-of-arylamines
                    if(v.Temp<0||v.Temp>5||v.Get("hno2")<=1e-10||v.Get("H+")<=1e-10||v.Get("Cl-")<=1e-10)continue;
                    var diazotization=new Rules.Rule{Eq=ChemData.Sub[fromId].Formula+" + HNO2 + H+ + Cl- -> "+ChemData.Sub[toId].Formula+" + 2H2O",Organic=true};
                    Rules.ParseEquationPublic(diazotization);
                    if(!diazotization.Parsed||Apply(v,diazotization,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,r.Obs,"rxn");return true;
                }

                if(fam=="diazonium"&&r.To=="azo"){
                    // Phenol coupling retains the phenolic oxygen: the mapped
                    // product is p-hydroxyazobenzene, not azobenzene.
                    // https://openstax.org/books/organic-chemistry/pages/24-8-reactions-of-arylamines
                    if(v.Temp<0||v.Temp>5||v.Get("phoh")<=1e-10)continue;
                    var coupling=new Rules.Rule{Eq=ChemData.Sub[fromId].Formula+" + C6H5OH -> "+ChemData.Sub[toId].Formula+" + HCl",Organic=true};
                    Rules.ParseEquationPublic(coupling);
                    if(!coupling.Parsed||Apply(v,coupling,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,r.Obs,"rxn");return true;
                }

                if(fam=="carboxylic_acid"&&r.To=="ester"){
                    // Fischer esterification consumes alcohol and makes water;
                    // H2SO4 is a catalyst rather than a fictitious reactant.
                    // https://openstax.org/books/organic-chemistry/pages/21-3-reactions-of-carboxylic-acids
                    if(v.Temp<55||v.Mode!=HeatMode.Reflux||(!v.ConcAcid&&v.Get("h2so4_conc")<=1e-10)||v.Get("c2h5oh")<=1e-10)continue;
                    var esterification=new Rules.Rule{Eq=ChemData.Sub[fromId].Formula+" + C2H5OH -> "+ChemData.Sub[toId].Formula+" + H2O",Organic=true};
                    Rules.ParseEquationPublic(esterification);
                    if(esterification.Parsed)esterification.Reactants[0].Id=fromId;
                    if(!esterification.Parsed||Apply(v,esterification,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,r.Obs,"rxn");return true;
                }

                if(fam=="nitroarene"&&r.To=="primary_amine"){
                    // Iron/acid reduction (OpenStax 24.6), net redox balance.
                    // Free-amine skeleton only; protonation/work-up is a separate model.
                    if(v.Temp<62||v.Water<=1e-9||v.Get("fe")<=1e-10)continue;
                    var reduction=new Rules.Rule{Eq=ChemData.Sub[fromId].Formula+" + 3Fe + 6H+ -> "+ChemData.Sub[toId].Formula+" + 3Fe2+ + 2H2O",Organic=true};
                    Rules.ParseEquationPublic(reduction);
                    if(!reduction.Parsed||Apply(v,reduction,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,"铁/酸还原硝基，生成胺骨架和亚铁离子","rxn");return true;
                }

                if(fam=="arene"&&r.To=="nitroarene"){
                    // OpenStax 16.2: nitric/sulfuric mixed acid, sulfuric acid catalytic.
                    // 50 C is the existing CIE demonstration gate, not a kinetic law.
                    if(!v.ConcAcid||v.Temp<50||!HasTok(v,"h2so4"))continue;
                    string acidId=v.Get("hno3")>1e-10?"hno3":v.Get("hno3_conc")>1e-10?"hno3_conc":null;
                    var nitration=new Rules.Rule{Eq=ChemData.Sub[fromId].Formula+" + "+(acidId==null?"H+ + NO3-":"HNO3")+" -> "+ChemData.Sub[toId].Formula+" + H2O",Organic=true};
                    Rules.ParseEquationPublic(nitration);
                    if(acidId!=null&&nitration.Parsed)nitration.Reactants[1].Id=acidId;
                    if(!nitration.Parsed||Apply(v,nitration,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,r.Obs,"rxn");return true;
                }

                if((fam=="primary_alcohol"&&(r.To=="aldehyde"||r.To=="carboxylic_acid"))||(fam=="secondary_alcohol"&&r.To=="ketone")||(fam=="aldehyde"&&r.To=="carboxylic_acid")){
                    if(v.Temp<62||v.Water<=1e-9)continue;
                    // Balanced acid-medium half reactions, never substrate -> product.
                    // Organic transformations: OpenStax 17.7 and 19.3. Coefficients
                    // follow electron/atom balance; 62 C remains the teaching heat gate.
                    bool aldehyde=fam=="aldehyde";int d=fam=="primary_alcohol"&&r.To=="carboxylic_acid"?2:1;
                    string substrate=ChemData.Sub[fromId].Formula,product=ChemData.Sub[toId].Formula,eq=null;
                    if(v.Get("Cr2O7^2-")>1e-10)eq=$"3{substrate} + {d}Cr2O7^2- + {8*d}H+ -> 3{product} + {2*d}Cr^3+ + {(aldehyde?4:d==2?11:7)}H2O";
                    else if(v.Get("MnO4-")>1e-10)eq=$"5{substrate} + {2*d}MnO4- + {6*d}H+ -> 5{product} + {2*d}Mn^2+ + {(aldehyde?3:d==2?11:8)}H2O";
                    else if(v.Get("cro3")>1e-10)eq=$"3{substrate} + {2*d}CrO3 + {6*d}H+ -> 3{product} + {2*d}Cr^3+ + {(aldehyde?3:d==2?9:6)}H2O";
                    else if(v.Get("k2cr2o7")>1e-10)eq=$"3{substrate} + {d}K2Cr2O7 + {8*d}H+ -> 3{product} + {2*d}K+ + {2*d}Cr3+ + {(aldehyde?4:d==2?11:7)}H2O";
                    else if(v.Get("kmno4")>1e-10)eq=$"5{substrate} + {2*d}KMnO4 + {6*d}H+ -> 5{product} + {2*d}K+ + {2*d}Mn2+ + {(aldehyde?3:d==2?11:8)}H2O";
                    else if(aldehyde&&v.Get("o2")>1e-10)eq=$"2{substrate} + O2 -> 2{product}";
                    if(eq==null)continue;
                    eq=eq.Replace("Cr^3+","Cr3+").Replace("Mn^2+","Mn2+");
                    var oxidation=new Rules.Rule{Eq=eq,Organic=true};Rules.ParseEquationPublic(oxidation);
                    if(!oxidation.Parsed||Apply(v,oxidation,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,r.Obs,"rxn");return true;
                }

                if(fam=="acyl_chloride"){
                    // OpenStax 21.4: RCOCl + H2O/ROH/NH3 -> acid/ester/amide + HCl.
                    // Subsequent acid-base rules neutralize HCl if excess NH3 exists.
                    string nucleophile=r.To=="carboxylic_acid"?"H2O":r.To=="ester"&&toId=="ch3cooc2h5"?"C2H5OH":r.To=="amide"&&toId=="ch3conh2"?"NH3":null;
                    if(nucleophile==null)continue;
                    var substitution=new Rules.Rule{Eq=ChemData.Sub[fromId].Formula+" + "+nucleophile+" -> "+ChemData.Sub[toId].Formula+" + HCl",Organic=true};
                    Rules.ParseEquationPublic(substitution);
                    if(!substitution.Parsed||Apply(v,substitution,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,r.Obs,"rxn");return true;
                }

                if(fam=="halogenoalkane"&&(r.To=="primary_alcohol"||r.To=="alkene"||r.To=="nitrile"||r.To=="primary_amine")){
                    // RX + OH- -> ROH + X-; elimination also produces H2O.
                    // Limit BOTH reactants, unlike the old RX -> product shortcut.
                    string halide=null;
                    foreach(string element in new[]{"Cl","Br","I"})
                        if(ChemData.Sub[fromId].Atoms.TryGetValue(element,out int count)&&count==1)halide=element+"-";
                    if(halide==null||v.Temp<62)continue;
                    // CN substitution adds one carbon; ammonia also accepts the released proton.
                    // OpenStax Organic Chemistry 20.7 / 24.6. Primary-amine branch is
                    // a teaching net equation, not a prediction of overalkylation selectivity.
                    string reagent=r.To=="nitrile"?"CN-":r.To=="primary_amine"?"2NH3":"OH-";
                    string coproduct=r.To=="alkene"?" + H2O":r.To=="primary_amine"?" + NH4+":"";
                    var conversion=new Rules.Rule{Eq=ChemData.Sub[fromId].Formula+" + "+reagent+" -> "+ChemData.Sub[toId].Formula+" + "+halide+coproduct,Organic=true};
                    Rules.ParseEquationPublic(conversion);
                    if(!conversion.Parsed||Apply(v,conversion,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,r.Obs,"rxn");return true;
                }

                if(fam=="nitrile"&&r.To=="carboxylic_acid"){
                    // Acid hydrolysis net equation (OpenStax Organic Chemistry 20.7):
                    // RCN + 2H2O + H+ -> RCOOH + NH4+. Reflux mode alone is not heat.
                    if(v.Temp<62||v.Water<=1e-9)continue;
                    var hydrolysis=new Rules.Rule{Eq=ChemData.Sub[fromId].Formula+" + 2H2O + H+ -> "+ChemData.Sub[toId].Formula+" + NH4+",Organic=true};
                    Rules.ParseEquationPublic(hydrolysis);
                    if(!hydrolysis.Parsed||Apply(v,hydrolysis,double.PositiveInfinity)<=1e-10)continue;
                    used.Add(key);Note(v,r.Obs,"rxn");return true;
                }

                // No substrate-only fallback: in particular the legacy
                // diazonium + phenol -> azobenzene mapping omits the phenol's
                // oxygen and must not silently destroy it. Unsupported
                // product chemistry remains unchanged until it is modeled.
                string notice="该有机转换缺少已配平的产物模型："+ChemData.NameOf(fromId)+" → "+ChemData.NameOf(toId);
                v.PhaseNotice=notice;
                if(v.PhaseNotices.Add("organic-unsupported:"+key))Note(v,notice,"obs");
            }
        }
        return false;
    }

    static bool CheckCond(Vessel v, Rules.Rule r) {
        // These legacy rows used broad conditions that also admitted MnO2 as
        // a benzene-bromination catalyst and warm diazonium chemistry.
        if(r.Eq=="C6H6 + Br2 -> C6H5Br + HBr"&&v.Get("fe")<=1e-10)return false;
        if((r.Eq=="aniline + HNO2 + HCl -> C6H5N2Cl + 2H2O"||
            r.Eq=="C6H5N2Cl + C6H5OH -> C6H5N=NC6H4OH + HCl")&&(v.Temp<0||v.Temp>5))return false;
        switch (r.Cond) {
            case "wet_slurry": return v.Water>1e-9&&SolventProperties.IsAqueousModelValid(v)&&v.Temp>=2&&v.Temp<=80;
            case "wet_bicarbonate": return v.Water>1e-9&&SolventProperties.IsAqueousModelValid(v)&&v.Temp>=2&&v.Temp<=80&&
                                           v.Get("co2")>1e-9;
            case "nickel_hydrogenation": return v.Temp>=150&&v.Get("ni")>1e-10;
            case "organic_cr_distil": return v.Temp>=62&&v.Water>1e-9&&v.Mode==HeatMode.Distil;
            case "organic_cr_reflux": return v.Temp>=62&&v.Water>1e-9&&v.Mode==HeatMode.Reflux;
            /* 回流保留全部产物；蒸馏边生成边把易挥发产物拿走 —— 决定伯醇氧化成醛还是酸 */
            case "reflux":   return v.Mode == HeatMode.Reflux;
            case "distil":   return v.Mode == HeatMode.Distil;
            case "heat":   return v.Temp >= 62;
            case "ignite": return v.Lit;
            case "light":  return v.Lit;
            case "catal":  return v.Has("mno2") || v.Has("fe") || v.Has("h2so4") ||
                                  v.Has("alcl3") || v.Has("pt") || v.Has("fecl3") || v.ConcAcid;
            case "conc":   return v.ConcAcid;
            /* 氧化还原介质判据：像 MnO4- 这种在不同 pH 下还原产物不同的物种，
               必须能按酸度分支，否则「忘记酸化」也会错误地得到 Mn2+ */
            case "acid":    return v.Get("H+") > 1e-3;
            case "neutral": return v.Get("H+") <= 1e-3;
            /* 醇的脱水需要浓硫酸且要 170 ℃。只判「有催化剂」不够 ——
               80 ℃ 下应该走酯化而不是脱水，不加温度门槛就会在酯化实验里
               错误地生成烯烃。 */
            case "catal_hot": return v.ConcAcid && v.Temp >= 140;
            /* 强热（焰上直接灼烧）：碳酸盐分解、坩埚灼烧这一档 */
            case "hot": return v.Temp >= 300;
            /* 气体溶解度随温度下降：热的时候 CO2 早就被赶跑了。
               没有这条，CO2 会在「溶解 ⇄ 分解」之间循环，
               把水反复加回来，坩埚法的质量损失就算不对。 */
            case "cool": return v.Temp < 60;
            /* 卤化银在氨水中的溶解性差别是经典鉴别点：
               AgCl 溶于稀氨水，AgBr 只有浓氨水才溶得动。
               用浓度门槛体现这个差别，而不是让两者都无条件溶解。 */
            case "conc_nh3": {
                if(v.Water<=1e-9||!SolventProperties.IsAqueousModelValid(v))return false;
                double vol = Math.Max(1e-12, v.Water / 1000.0);   // dm3; no invented 1 mL minimum
                v.Solutes.TryGetValue("nh3",out double dissolved);
                return dissolved / vol >= 6.0;                 // mol/dm3; headspace gas is not aqueous ammonia
            }
            /* 硫代硫酸根的酸分解在有碘在场时不该抢跑：碘量法里它先被 I2 氧化
               （快），只有在没有碘时才会缓慢分解出硫。 */
            case "no_i2": return !v.Has("i2") && !v.Has("iodine_sol");
            /* 酯化要浓硫酸催化并且回流加热；室温下几乎不发生 */
            case "catal_warm": return v.ConcAcid && v.Temp >= 55;
            default:       return true;
        }
    }

    /// <summary>套用一条规则，返回反应进度（mol）</summary>
    static double Apply(Vessel v, Rules.Rule r, double limit, bool dryRun = false) {
        if (!r.Parsed) return 0;
        double extent = limit;
        foreach (var sp in r.Reactants) {
            double have = v.Get(sp.Id);
            if (have <= 1e-10) return 0;
            extent = Math.Min(extent, have / sp.Coef);
        }
        if (extent <= 1e-10) return 0;
        if (dryRun) {
            foreach (var p in r.Products) if (!ChemData.Sub.ContainsKey(p.Id)) return 0;
            return extent;
        }
        foreach (var sp in r.Reactants) v.TakeAny(sp.Id, extent * sp.Coef);
        foreach (var p in r.Products) {
            if(p.Id==r.Gas) {v.Gases.TryGetValue(p.Id,out double gas);v.Gases[p.Id]=gas+extent*p.Coef;}
            else v.Produce(p.Id, extent * p.Coef);
        }
        /* 反应焓按实际反应量和当前已知液相热容结算。旧数据列的 Heat
           只是“明显放/吸热”定性标记，不能按每次反应固定摄氏度加温。 */
        double dh = r.Cond=="ignite"?KnownIgnitionHeat(r.Eq):ChemData.RuleHeat(r.Eq);
        if (Math.Abs(dh) > 1e-9) ApplyReactionHeat(v, extent, dh);
        else if(r.Heat!=0)ThermalMixing.RecordNotice(v,"该反应只有定性热效应记录，缺少已核对的定量焓变；未按固定摄氏度修改温度");
        if (r.Bubble > 0) v.Fx.Bubble = Math.Max(v.Fx.Bubble, r.Bubble);
        if (r.Smoke  > 0) v.Fx.Smoke  = Math.Max(v.Fx.Smoke,  r.Smoke);
        if (!string.IsNullOrEmpty(r.Gas)) {
            /* gas 字段只用来触发气泡/逸出这类视觉特效，**不能**再按 0.5 倍加产量 ——
               气体的物质的量已经由上面的 Produce(化学计量) 给足了。
               这里原来重复加了一次，导致所有放气反应产量虚高 50%
               （Mg+酸 的 H2 实测 0.0105 mol，而 Mg 只有 0.0070 mol）。
               只有方程式里没写该气体时才需要补。 */
            bool inEq = false;
            foreach (var pp in r.Products) if (pp.Id == r.Gas) { inEq = true; break; }
            /* 反应物列也必须查：CO2 + H2O -> H2CO3 里 CO2 是反应物、带的 gas 只是视觉特效。
               只查产物列的话，每溶解一次又会凭空加回一份 CO2，
               「溶解 → 分解」就成了放大回路，CO2 能涨到理论值的 4 倍。 */
            if (!inEq) foreach (var rr in r.Reactants) if (rr.Id == r.Gas) { inEq = true; break; }
            if (!inEq) {
                v.Gases.TryGetValue(r.Gas, out double g); v.Gases[r.Gas] = g + extent;
            }
        }
        if (!string.IsNullOrEmpty(r.Precip)) v.Turbidity = Math.Min(1f, v.Turbidity + 0.35f);
        if(r.Eq=="CaCO3 + CO2 + H2O -> Ca2+ + 2HCO3-"&&v.Precipitates.Count==0)
            v.Turbidity=0;
        if (r.Mirror)  v.Fx.Mirror = true;
        if (r.Explode) v.Fx.Explode = 1;
        if (!string.IsNullOrEmpty(r.Flame)) { v.Fx.Flame = true; v.Fx.FlameT = 6f; v.Fx.FlameColor = r.Flame; }
        if (!string.IsNullOrEmpty(r.Tint)) v.Tint = r.Tint;
        if (r.ClearTint) v.Tint = null;
        if (!string.IsNullOrEmpty(r.Deposit)) v.Fx.Deposit = r.Deposit;
        PushReaction(v, r, extent);
        return extent;
    }

    /// <summary>直接套用一条方程式（给活泼金属遇水这类即时反应用）</summary>
    static void ApplyRaw(Vessel v, string eq, double mol) {
        var r = new Rules.Rule { Eq = eq };
        Rules.ParseEquationPublic(r);
        if (r.Parsed) Apply(v, r, mol);
    }

    /// <summary>每帧调用，按**时间**衰减视觉强度。</summary>
    public static void UpdateAppearance(Vessel v, float dt = 1f / 60f) {
        /* 这里的衰减必须按时间算，而且要每帧都跑。
           原来它只在 Apply()（发生反应时）里被调用 ——
           于是**不反应的容器，粒子强度永远停在最后一次的值**，
           烧杯倒空之后还在冒黄色的气泡和烟雾。 */
        float kb = Mathf.Pow(0.90f, dt * 60f);
        float ks = Mathf.Pow(0.94f, dt * 60f);
        v.Turbidity *= Mathf.Pow(0.985f, dt * 60f);
        if (v.Precipitates.Count > 0)
            v.Turbidity = Mathf.Max(v.Turbidity, Mathf.Min(0.85f, 0.25f + v.Precipitates.Count * 0.12f));

        v.Fx.Bubble *= kb;
        v.Fx.Smoke *= ks;
        v.Fx.Glow *= kb;
        v.Fx.Pop *= ks;
        if (v.Fx.Bubble < 0.02f) v.Fx.Bubble = 0;
        if (v.Fx.Smoke < 0.02f) v.Fx.Smoke = 0;
        if (v.Fx.Glow < 0.02f) v.Fx.Glow = 0;
        if (v.Fx.Pop < 0.02f) v.Fx.Pop = 0;

        /* 容器里没有液体就不可能有气泡 —— 空烧杯冒泡是说不通的。
           原来只要 Gases 里还剩一点东西，气泡就被钉在 0.3 不降。 */
        bool hasLiquid = v.Volume() > 0.5;
        if (!hasLiquid) { v.Fx.Bubble = 0; }
        else if (v.Gases.Count > 0 && v.Fx.Bubble < 0.3f) v.Fx.Bubble = 0.3f;

        /* 火焰：让它自然熄灭。原来 Flame 是个 bool，设上之后没有任何地方清掉，
           除非整体 Reset —— 一次反应之后容器会一直「烧」下去。 */
        if (v.Fx.Flame) {
            v.Fx.FlameT -= dt;
            if (v.Fx.FlameT <= 0f) { v.Fx.Flame = false; v.Fx.FlameColor = null; }
        }
    }

    static void AutoObservations(Vessel v) {
        if (!TrySolutionColor(v, out Color c)) return;
        if (v._hasLastColor && !Approx(v._lastColor, c)) {
            string n1 = ColorName(v._lastColor), n2 = ColorName(c);
            if (n1 != n2) Note(v, $"溶液颜色由{n1}变为{n2}", "obs");
        }
        v._lastColor = c; v._hasLastColor = true;
        if (v.Temp > 60 && !v._saidHot) { Note(v, $"溶液温度升高到约 {v.Temp:F0} ℃", "obs"); v._saidHot = true; }
        if (v.Temp < 45) v._saidHot = false;
    }

    static bool Approx(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.06f && Mathf.Abs(a.g - b.g) < 0.06f && Mathf.Abs(a.b - b.b) < 0.06f;

    /* ---------------- 特殊规则（指示剂、显色） ---------------- */
    static void RunSpecials(Vessel v) {
        foreach (var sp in Rules.Specials) {
            if (v.Fx.FiredSpecials.Contains(sp.Id)) continue;
            if (!SpecialWhen(v, sp.Id)) continue;
            v.Fx.FiredSpecials.Add(sp.Id);
            Note(v, sp.Obs, "rxn");
            if (!string.IsNullOrEmpty(sp.Tint)) v.Tint = sp.Tint;
            if (sp.ClearTint) v.Tint = null;
        }
    }

    static bool SpecialWhen(Vessel v, string id) {
        double ph = v.Ph;
        switch (id) {
            case "starch-iodine":       return v.Has("starch") && (v.Has("i2") || v.Has("iodine_sol"));
            case "phenolphthalein":     return v.Has("phenolphthalein") && ph > 8.2;
            case "phenolphthalein-acid":return v.Has("phenolphthalein") && ph < 8.0;
            case "litmus-acid":         return v.Has("litmus") && ph < 5.0;
            case "litmus-base":         return v.Has("litmus") && ph > 8.0;
            case "methylorange-acid":   return v.Has("methylorange") && ph < 3.1;
            case "methylorange-base":   return v.Has("methylorange") && ph > 4.4;
            case "phenol-fecl3":        return v.Has("phoh") && v.HasIon("Fe3+");
            case "feoh2-oxidize":       return v.HasPrecip("feoh2");
            case "fe-scn":              return v.HasIon("Fe3+") && v.HasIon("SCN-");
            default: return false;
        }
    }

    /* ---------------- 时间推进 ---------------- */
    /// <summary>沸点（℃）</summary>
    const float BoilPoint = 100f;

    /// <summary>超过沸点的水分会逸出。坩埚敞口灼烧、蒸发皿蒸干都靠它 ——
    /// 否则分解生成的水会被算进残留质量，坩埚法的质量损失就不对了。</summary>
    public static void Evaporate(Vessel v, float dt) {
        if(v.Distillation?.RetainsVapor==true)return;
        if(v.Sealed)return; // No mass may disappear through a sealed boundary.
        if (v.Temp <= BoilPoint || v.Water <= 0) return;
        double rate = Math.Min(40.0, (v.Temp - BoilPoint) / 10.0 * 0.6);   // mL/s
        v.Water = Math.Max(0, v.Water - rate * dt);
        v.RecoverDrySalts();
    }

    public static void Tick(float dt) {
        if(PourTransaction.IsActive)return;
        Clock += dt;
        MeteredTransfer.Tick(dt);
        foreach (var v in Vessels) {
            bool thermalOwner=DistillationAssembly.OwnsThermal(v);
            var heatingBurner=ApparatusSet.HeatingBurner(v);
            bool burnerHeating=ApparatusSet.BurnerHeats(v,heatingBurner);
            bool physicallySupported=v.TripodRef!=null||v.Burner!=null||heatingBurner!=null;
            if (!thermalOwner&&v.Heating && !physicallySupported) WaterIce.AdvanceTemperature(v,Math.Min(105f, v.Temp + dt * 26f));
            // Coarse ambient exchange: approach room temperature from either
            // side without an instantaneous jump or a fixed cooling rate that
            // erases the heat delivered by a slow burette drip in one frame.
            else if(!thermalOwner)WaterIce.AdvanceTemperature(v,25f + (v.Temp - 25f) * Mathf.Exp(-0.06f * dt));

            if (!thermalOwner&&!v.Sealed && v.Distillation?.RetainsVapor!=true && v.Temp >= 99.5f && v.Water > 0) {
                v.Water = Math.Max(0, v.Water - dt * 2.6);
                v.Fx.Bubble = Math.Max(v.Fx.Bubble, 2.4f);
                v.Fx.Smoke  = Math.Max(v.Fx.Smoke, 1.2f);
                if (!v._saidBoil) { Note(v, "液体沸腾，大量水蒸气逸出", "obs"); v._saidBoil = true; }
            } else if (v.Temp < 95f) v._saidBoil = false;

            Evaporate(v, dt);
            v.Distillation?.Tick(dt);
            v.RecoverDrySalts();
            PhaseProperties.Reconcile(v,true);
            // A later CO2 dose, whether initially gaseous or dissolved, can
            // turn an already observed limewater precipitate into bicarbonate.
            if(v.Water>1e-9&&v.Precipitates.ContainsKey("caco3")&&
               v.Get("co2")>v.LastAvailableCO2AtSettle+1e-9)
                Settle(v);
            /* 本生灯也算「在加热」：否则吸附到灯上的容器温度上去了，
               反应却一次都不评估（分解、灼烧全靠最后那一下 Settle）。 */
            bool heated = v.Heating || burnerHeating;
            if (heated && v.Temp > 62f && UnityEngine.Random.value < dt * 3.2f) Settle(v);
            /* 每帧衰减视觉强度。原来只在发生反应时才跑，
               所以不反应的容器粒子强度永远停在最后一次的值 ——
               烧杯倒空了还在冒气泡、火焰也一直不灭。 */
            UpdateAppearance(v, dt);
        }
    }

    /* ================================================================
       颜色
       ================================================================ */
    /* 摩尔吸光系数（近似值，单位 mol^-1 dm3 cm^-1）。
       键必须是「溶液里实际存在的物种」—— 原来只写了 kmno4，
       可它在水里是 MnO4- 离子，那个乘数从来没生效过。 */
    static readonly Dictionary<string,float> Absorb = new() {
        {"fescn3",5000},{"mno4-",2200},{"kmno4",2200},
        {"cr2o7^2-",300},{"k2cr2o7",300},
        {"i2",700},{"iodine_sol",700},{"br2",150},
        {"[cu(nh3)4]2+",60},{"cunh34",60},{"no2",40},
        {"mno2",400},
    };
    const double DefaultEps = 12.0;   // 其余有色离子的默认吸光系数

    public static bool TrySolutionColor(Vessel v, out Color col) {
        col = Color.white;
        if (v == null) return false;
        double V = Math.Max(1e-12, v.Water / 1000.0);

        /* ---- Beer-Lambert：吸光度按通道相加 ----
           之前是把颜色「加权平均」再除以权重和，结果颜色只取决于各组分
           的比值、与绝对浓度无关 —— 终点后高锰酸根涨 5 倍颜色纹丝不动。
           物理上应该是：A = Σ ε c，透射率 T = 10^-A，看到的就是透射光。 */
        double aR = 0, aG = 0, aB = 0;
        bool any = false;
        void Add(string id, double mol) {
            var s = ChemData.Sub[id];
            var hex = !string.IsNullOrEmpty(s.SolColor) ? s.SolColor : s.SolidColor;
            if (string.IsNullOrEmpty(hex) || !ColorUtility.TryParseHtmlString(hex, out Color c)) return;
            double conc = Math.Max(0, mol) / V;                 // mol/dm3
            double eps = Absorb.TryGetValue(id.ToLowerInvariant(), out float e) ? e : DefaultEps;
            double d = conc * eps;                              // 光学深度
            if (d <= 1e-9) return;
            /* 物种的颜色是它「透过」的光，所以在 (1-c) 大的通道吸收得多 */
            aR += d * (1.0 - c.r);
            aG += d * (1.0 - c.g);
            aB += d * (1.0 - c.b);
            any = true;
        }
        foreach (var kv in v.Solutes) Add(kv.Key, kv.Value);
        foreach (var kv in v.Undissolved) {
            var s = ChemData.Sub[kv.Key];
            if (s.State == "l" && !s.IsIon) Add(kv.Key, kv.Value);
        }
        Color? bcol = any
            ? new Color((float)Math.Pow(10, -aR), (float)Math.Pow(10, -aG), (float)Math.Pow(10, -aB))
            : (Color?)null;

        if (v.Precipitates.Count > 0) {
            double pr = 0, pg = 0, pb = 0, pw = 0;
            foreach (var kv in v.Precipitates) {
                var s = ChemData.Sub[kv.Key];
                var hex = ChemData.Precip.TryGetValue(s.Formula, out var pi) && !string.IsNullOrEmpty(pi.Color)
                        ? pi.Color : s.SolidColor;
                if (string.IsNullOrEmpty(hex) || !ColorUtility.TryParseHtmlString(hex, out Color c)) continue;
                double w = Math.Min(1.0, kv.Value * 400);
                pr += c.r*w; pg += c.g*w; pb += c.b*w; pw += w;
            }
            if (pw > 0) {
                var pc = new Color((float)(pr/pw), (float)(pg/pw), (float)(pb/pw));
                float k = (float)Math.Min(0.72, 0.22 + pw * 0.18);
                bcol = bcol.HasValue ? Color.Lerp(bcol.Value, pc, k) : pc;
            }
        }
        if (!string.IsNullOrEmpty(v.Tint) && ColorUtility.TryParseHtmlString(v.Tint, out Color tc)) {
            /* 着色剂（指示剂、淀粉-碘络合物）本该盖过溶液本身的颜色。
               原来固定 0.5 的混合，遇到碘水这种颜色很深的底液就不够了 ——
               「淀粉遇碘变蓝」会被碘自身的棕红盖成紫红。
               改为按着色剂自身的深度决定权重：越深的着色剂越应该占主导。 */
            float lum = 0.299f * tc.r + 0.587f * tc.g + 0.114f * tc.b;
            float k = Mathf.Lerp(0.92f, 0.45f, Mathf.Clamp01(lum));
            bcol = bcol.HasValue ? Color.Lerp(bcol.Value, tc, k) : tc;
        }

        if (!bcol.HasValue) return false;
        col = bcol.Value; return true;
    }

    public static string ColorName(Color c) {
        float r = c.r, g = c.g, b = c.b;
        float max = Mathf.Max(r, Mathf.Max(g, b)), min = Mathf.Min(r, Mathf.Min(g, b));
        if (max < 0.12f) return "无色";
        if (max - min < 0.07f) return max > 0.72f ? "白色" : "灰色";
        bool light = (r + g + b) / 3f > 0.58f;
        /* 品红/紫红：红与蓝都明显高于绿。高锰酸钾溶液正是这一类，
           原来的色名表没有这一档，会掉到兜底的「深色」。 */
        /* 品红要红蓝「彼此相当」；若蓝远高于红那是深蓝（淀粉-碘络合物就是这样），
           应落到下面的蓝色分支，不能一律判成品红。 */
        if (r > g * 1.30f && b > g * 1.20f && r > b * 0.75f)
            return light ? "淡紫红色" : "紫红色";
        if (r >= g && g >= b) {
            if (g < 0.46f && b < 0.30f) return "橙红色";
            if (g >= 0.46f && b < 0.46f) return light ? "浅棕黄色" : "棕黄色";
            return light ? "橙黄色" : "橙色";
        }
        /* 粉红应按「色相」判定：红通道明显压过绿蓝、且绿≈蓝。
           原来写成 g<0.42 且 b<0.42，淡粉红（g/b 还偏高）会被误判成橙黄 ——
           高锰酸钾滴定终点就是淡粉红，这个判据必须对。 */
        if (r > g * 1.18f && r > b * 1.18f && Mathf.Abs(g - b) < 0.13f)
            return light ? "粉红色" : "红色";
        if (b >= r && b >= g) return (g > r * 0.9f) ? "蓝绿色" : (light ? "淡蓝色" : "蓝色");
        if (g >= r && g >= b) {
            if (r > 0.55f && b > 0.55f) return "浅绿色";
            if (r > b * 1.35f) return "黄绿色";
            return light ? "浅绿色" : "绿色";
        }
        if (r > 0.5f && g > 0.5f) return "黄色";
        return light ? "淡色" : "深色";
    }
}

public static class VesselNames {
    public static string Name(VesselKind k) => k switch {
        VesselKind.Beaker => "烧杯", VesselKind.Flask => "锥形瓶",
        VesselKind.Tube => "试管", VesselKind.Cylinder => "量筒",
        VesselKind.RoundBottom => "圆底烧瓶", VesselKind.Crucible => "坩埚",
        VesselKind.Dish => "蒸发皿", VesselKind.Bottle => "试剂瓶", VesselKind.Burette => "滴定管", VesselKind.Pipette => "移液管", _ => "容器",
    };
}

}
