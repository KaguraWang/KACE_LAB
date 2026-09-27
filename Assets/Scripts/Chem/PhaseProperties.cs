using System;
using System.Collections.Generic;
using System.Linq;
namespace ChemLab.Chem {
// Pure-species room phase is distinct from the historical stock-reagent State field.
// Never infer a temperature curve from the qualitative k/s/n solubility flag.
public static class PhaseProperties {
    public sealed class Entry {
        public string Phase,Source;public double Melt=double.NaN,Boil=double.NaN,SolidDensity=double.NaN,LiquidDensity=double.NaN;
        public double[] Temperature,GramsPer100Water;
        public double Henry=double.NaN,HenryTemperature;
    }
    public static readonly Dictionary<string,Entry> Data=new Dictionary<string,Entry>{
        {"butanoic_acid",new Entry{Phase="l",Melt=267.89-273.15,Boil=436-273.15,Source="https://webbook.nist.gov/cgi/cbook.cgi?ID=C107926&Mask=4 ; https://www.fishersci.com/shop/products/butyric-acid-analytical-standard-99-5-gc-milliporesigma-supelco/111019490"}},
        // Ranges use midpoint teaching thresholds: pentanoic 186–187 C,
        // heptanoic 223–223.4 C, octanoic fusion 15–17 C.
        {"pentanoic_acid",new Entry{Phase="l",Melt=-34.5,Boil=186.5,Source="https://www.inchem.org/documents/icsc/icsc/eics0346.htm"}},
        {"hexanoic_acid",new Entry{Phase="l",Melt=-3,Boil=205,Source="https://chemicalsafety.ilo.org/dyn/icsc/showcard.display?p_card_id=1167&p_lang=en"}},
        {"heptanoic_acid",new Entry{Phase="l",Melt=-10.5,Boil=223.2,Source="https://www.fishersci.com/shop/products/heptanoic-acid-98-thermo-scientific-1/AC164170010"}},
        {"octanoic_acid",new Entry{Phase="l",Melt=16,Boil=237,Source="https://www.sigmaaldrich.com/TM/en/product/aldrich/o3907"}},
        {"butanal",new Entry{Phase="l",Melt=-99,Boil=75,Source="https://labchem-wako.fujifilm.com/us/product/detail/W01W0102-1346.html"}},
        {"pentanal",new Entry{Phase="l",Melt=-91,Boil=103,Source="https://chemicalsafety.ilo.org/dyn/icsc/showcard.display?p_card_id=1417&p_lang=en&p_version=2"}},
        {"hexanal",new Entry{Phase="l",Melt=-56,Boil=131,Source="https://labchem-wako.fujifilm.com/us/product/detail/W01W0108-0263.html"}},
        {"heptanal",new Entry{Phase="l",Melt=-42,Boil=153,Source="https://www.fishersci.com/store/msds?countryCode=US&language=en&partNumber=AAB23830AU&productDescription=HEPTANAL+97%25+1L&vendorId=VN00024248"}},
        // NIST TRC fusion 247.72 K, boiling 441.65 K. Supplier mp 12–15 C
        // conflicts with fusion/transition records and is deliberately not used.
        {"octanal",new Entry{Phase="l",Melt=247.72-273.15,Boil=441.65-273.15,Source="https://webbook.nist.gov/cgi/cbook.cgi?ID=C124130&Mask=4 ; https://www.sigmaaldrich.com/MG/en/product/mm/806901"}},
        {"but_1_ene",new Entry{Phase="g",Melt=-185,Boil=-6.3,Source="https://www.sigmaaldrich.com/US/en/product/aldrich/744042"}},
        {"pent_1_ene",new Entry{Phase="l",Melt=-165,Boil=30,Source="https://www.fishersci.com/shop/products/1-pentene-97-thermo-scientific-1/AC268661000"}},
        {"hex_1_ene",new Entry{Phase="l",Melt=-139.82,Boil=63.478,Source="https://www.cpchem.com/sites/default/files/2020-04/1-Hexene%201-Octene%202019%20Final.pdf"}},
        {"hept_1_ene",new Entry{Phase="l",Melt=-119,Boil=94,Source="https://www.fishersci.com/shop/products/1-heptene-98-thermo-scientific-1/AC120390250"}},
        {"oct_1_ene",new Entry{Phase="l",Melt=-101.74,Boil=121.286,Source="https://www.cpchem.com/sites/default/files/2020-04/1-Hexene%201-Octene%202019%20Final.pdf"}},
        {"butan_1_ol",new Entry{Phase="l",Melt=-89,Boil=118,Source="https://inchem.org/documents/hsg/hsg/hsg003.htm"}},
        {"pentan_1_ol",new Entry{Phase="l",Melt=-79,Boil=138,Source="https://inchem.org/documents/icsc/icsc/eics0535.htm"}},
        {"hexan_1_ol",new Entry{Phase="l",Melt=-44.6,Boil=157,Source="https://inchem.org/documents/icsc/icsc/eics1084.htm"}},
        {"heptan_1_ol",new Entry{Phase="l",Melt=-36,Boil=176,Source="https://labchem-wako.fujifilm.com/us/product/detail/W01W0108-0020.html ; https://www.merckmillipore.com/INTL/en/product/1-Heptanol,MDA_CHEM-820624"}},
        // ICSC reports 194–195 C; midpoint is a teaching threshold, not precision data.
        {"octan_1_ol",new Entry{Phase="l",Melt=-15.5,Boil=194.5,Source="https://inchem.org/documents/icsc/icsc/eics1030.htm"}},
        {"n_pentane",new Entry{Phase="l",Melt=-129,Boil=36,Source="https://inchem.org/documents/icsc/icsc/eics0534.htm"}},
        {"n_hexane",new Entry{Phase="l",Melt=-95.35,Boil=68.74,Source="https://inchem.org/documents/ehc/ehc/ehc122.htm"}},
        {"n_heptane",new Entry{Phase="l",Melt=-90.7,Boil=98.4,Source="https://inchem.org/documents/icsc/icsc/eics0657.htm"}},
        {"n_octane",new Entry{Phase="l",Melt=-56.8,Boil=126,Source="https://inchem.org/documents/icsc/icsc/eics0933.htm"}},
        {"hcl",new Entry{Phase="g",Melt=-114.2,Boil=-85.1,Source="https://inchem.org/documents/icsc/icsc/eics0163.htm"}},
        {"hbr",new Entry{Phase="g",Melt=-87,Boil=-67,Source="https://inchem.org/documents/icsc/icsc/eics0282.htm"}},
        {"hi",new Entry{Phase="g",Melt=-50.8,Boil=-35.5,Source="https://inchem.org/documents/icsc/icsc/eics1326.htm"}},
        // These IDs were historically labelled as aqueous stock reagents. The
        // shelf now supplies the pure species; the old state remains available
        // to reactions that explicitly prepare an aqueous solution.
        {"h2so4",new Entry{Phase="l",Melt=10,LiquidDensity=1.8,Source="https://inchem.org/documents/icsc/icsc/eics0362.htm"}},
        {"hno3",new Entry{Phase="l",LiquidDensity=1.5129,Source="https://pubchem.ncbi.nlm.nih.gov/compound/944"}},
        {"h2s",new Entry{Phase="g",Melt=-85,Boil=-60,Source="https://inchem.org/documents/icsc/icsc/eics0165.htm"}},
        {"hf",new Entry{Phase="g",Melt=-83,Boil=20,Source="https://inchem.org/documents/icsc/icsc/eics0283.htm"}},
        {"ch3cho",new Entry{Phase="g",Melt=-123,Boil=20.2,Source="https://inchem.org/documents/icsc/icsc/eics0009.htm"}},
        {"c2h5nh2",new Entry{Phase="g",Melt=-81,Boil=16.6,Source="https://inchem.org/documents/icsc/icsc/eics0153.htm"}},
        {"ch3oh",new Entry{Phase="l",Melt=-97.15,Boil=64.55,Source="https://webbook.nist.gov/cgi/cbook.cgi?ID=C67561&Mask=4"}},
        {"c2h5oh",new Entry{Phase="l",Melt=-114,Boil=EthanolThermodynamics.NormalBoilingC,Source="https://www.inchem.org/documents/icsc/icsc/eics0044.htm ; https://webbook.nist.gov/cgi/cbook.cgi?ID=C64175&Mask=16"}},
        {"acetone",new Entry{Phase="l",Melt=-95,Boil=56,Source="https://www.inchem.org/documents/icsc/icsc/eics0087.htm"}},
        {"nh3",new Entry{Phase="g",Melt=-77.73,Boil=-33.35,Source="https://www.ncbi.nlm.nih.gov/books/NBK598719/table/ch4.tab2/"}},
        {"ch3cooh",new Entry{Phase="l",Melt=16.7,Boil=118,Source="https://inchem.org/documents/icsc/icsc/eics0363.htm"}},
        {"ch3cooh_glacial",new Entry{Phase="l",Melt=16.7,Boil=118,Source="https://inchem.org/documents/icsc/icsc/eics0363.htm"}},
        {"h3po4",new Entry{Phase="s",Melt=42,SolidDensity=1.9,Source="https://inchem.org/documents/icsc/icsc/eics1008.htm"}},
        {"kno3",new Entry{Phase="s",Temperature=new[]{0d,25,100},GramsPer100Water=new[]{13d,38.3,247},Source="https://www.ncbi.nlm.nih.gov/books/NBK326541/"}},
        {"nacl",new Entry{Phase="s",Temperature=new[]{0d,9,15,20,25,30},GramsPer100Water=new[]{100*26.25/73.75,100*26.31/73.69,100*26.28/73.72,100*26.35/73.65,100*26.37/73.63,100*26.47/73.53},Source="https://iupac.github.io/SolubilityDataSeries/volumes/SDS-47.pdf"}},
        {"h2",new Entry{Phase="g",Henry=0.00078,HenryTemperature=500,Source="https://webbook.nist.gov/cgi/cbook.cgi?ID=C1333740&Mask=10"}},
        {"o2",new Entry{Phase="g",Henry=0.0013,HenryTemperature=1500,Source="https://webbook.nist.gov/cgi/cbook.cgi?ID=C7782447&Mask=10"}},
        {"co2",new Entry{Phase="g",Henry=0.034,HenryTemperature=2400,Source="https://webbook.nist.gov/cgi/cbook.cgi?ID=C124389&Mask=10"}},
    };
    public static string Phase(string id,double temperature=25){
        if(Data.TryGetValue(id,out var p)){
            if(!double.IsNaN(p.Boil)&&temperature>=p.Boil)return "g";
            if(!double.IsNaN(p.Melt))return temperature<p.Melt?"s":"l";
            return p.Phase;
        }
        return ChemData.Sub.TryGetValue(id,out var s)?s.State:"unknown";
    }
    public static string ShelfName(string id){
        switch(id){case "nh3":return "氨（气体）";case "hcl":return "氯化氢（气体）";case "hbr":return "溴化氢（气体）";case "hi":return "碘化氢（气体）";case "h2s":return "硫化氢（气体）";case "hf":return "氟化氢（气体）";case "h2so4":return "硫酸（液体）";case "hno3":return "硝酸（液体）";case "ch3cho":return "乙醛（25℃气体）";case "c2h5nh2":return "乙胺（气体）";default:return ChemData.NameOf(id);}
    }
    public static double Density(string id,double temperature=25){
        if(Data.TryGetValue(id,out var p)){
            string phase=Phase(id,temperature);
            if(phase=="s"&&!double.IsNaN(p.SolidDensity))return p.SolidDensity;
            if(phase=="l"&&!double.IsNaN(p.LiquidDensity))return p.LiquidDensity;
        }
        return ChemData.Sub.TryGetValue(id,out var s)?s.Density:double.NaN;
    }
    public static bool TryCapacity(Vessel v,string id,out double capacity){
        if(id=="nh3")return AmmoniaEquilibrium.TryCapacity(v,out capacity);
        if(id=="caoh2")return CalciumHydroxideEquilibrium.TryCapacity(v,out capacity);
        capacity=double.NaN;
        if(!SolventProperties.IsAqueousModelValid(v))return PureSolubility.TryCapacity(v,id,out capacity)||(id=="nacl"&&MixedSolubility.TryNaCl(v,out capacity));
        if(!Data.TryGetValue(id,out var p))return false;
        if(!double.IsNaN(p.Henry)){
            // NIST dilute aqueous Henry model; pressure is partial pressure in bar.
            if(v.Temp<0||v.Temp>30||v.PressureBar<=0||v.PressureBar>2)return false;
            capacity=p.Henry*Math.Exp(p.HenryTemperature*(1/(v.Temp+273.15)-1/298.15))*v.PressureBar*v.Water/1000;return true;
        }
        if(p.Temperature==null||v.Temp<p.Temperature[0]||v.Temp>p.Temperature[p.Temperature.Length-1])return false;
        double sol=p.GramsPer100Water.Last();
        for(int i=1;i<p.Temperature.Length;i++)if(v.Temp<=p.Temperature[i]){double t=(v.Temp-p.Temperature[i-1])/(p.Temperature[i]-p.Temperature[i-1]);sol=p.GramsPer100Water[i-1]*(1-t)+p.GramsPer100Water[i]*t;break;}
        capacity=sol*v.Water/100/ChemData.Sub[id].Mass;return true;
    }
    public static double Dissolved(Vessel v,string id){
        if(v.Water<=1e-9&&!SolventProperties.IsAqueousModelValid(v))return v.Solutes.TryGetValue(id,out double neutral)?neutral:0;
        if(ChemData.Diss.TryGetValue(id,out var dis)&&!dis.None&&dis.Ions.Count>0&&!dis.Weak){
            double mol=double.PositiveInfinity;foreach(var ion in dis.Ions){v.Solutes.TryGetValue(ion.Key,out var n);mol=Math.Min(mol,n/ion.Value);}return mol;
        }
        return v.Solutes.TryGetValue(id,out double value)?value:0;
    }
    static void Add(Dictionary<string,double> map,string id,double n){map.TryGetValue(id,out var existing);map[id]=existing+n;}
    public static void Reconcile(Vessel v,bool vent=false){
        WaterIce.Reconcile(v);
        ReconcileNeatCondensed(v);
        PureSolubility.Reconcile(v);
        AmmoniaEquilibrium.Reconcile(v,vent);
        if(v.Sealed){ReconcileClosedGas(v);vent=false;}
        foreach(var pair in Data){string id=pair.Key;if(id=="nh3"||!TryCapacity(v,id,out double capacity))continue;
            if(v.Water<=1e-9&&!SolventProperties.IsAqueousModelValid(v))continue;
            double dissolved=Dissolved(v,id);bool gas=Phase(id,v.Temp)=="g";
            if(gas&&v.Sealed)continue;
            if(dissolved>capacity+1e-9){
                double excess=dissolved-capacity;
                if(ChemData.Diss.TryGetValue(id,out var dis)&&!dis.None&&!dis.Weak&&dis.Ions.Count>0)
                    foreach(var ion in dis.Ions)v.TakeAny(ion.Key,excess*ion.Value);
                else v.TakeAny(id,excess);
                if(!gas)Add(v.Precipitates,id,excess);else{Add(v.Gases,id,excess);v.Fx.Bubble=Math.Max(v.Fx.Bubble,0.8f);}
                Notice(v,id,gas);dissolved=capacity;
            }
            if(gas&&v.Water>0&&v.Gases.TryGetValue(id,out double feed)){
                double take=Math.Min(feed,Math.Max(0,capacity-dissolved));if(take>0)Add(v.Solutes,id,take);
                double rest=feed-take;if(vent){v.Gases.Remove(id);v.EscapedMoles+=rest;}else if(rest>1e-12)v.Gases[id]=rest;else v.Gases.Remove(id);
                if(vent&&rest>1e-9)Notice(v,id,true,true);
            }
            if(!gas&&dissolved<capacity-1e-9&&v.Precipitates.TryGetValue(id,out double solid)){
                double take=Math.Min(solid,capacity-dissolved);if(take>1e-9){v.Precipitates[id]-=take;if(v.Precipitates[id]<=1e-12)v.Precipitates.Remove(id);v.Introduce(id,take,true);}
            }
        }
    }
    static void ReconcileNeatCondensed(Vessel v){
        // Pure condensed material can freeze/melt without becoming an aqueous ion.
        // Mixture freezing requires its own phase diagram and must not use this path.
        if(v.Water>1e-9||v.Solutes.Count+v.Undissolved.Count+v.Precipitates.Count!=1)return;
        foreach(var id in v.Solutes.Keys.ToList())if(Data.ContainsKey(id)&&Phase(id,v.Temp)=="s"){
            Add(v.Undissolved,id,v.Solutes[id]);v.Solutes.Remove(id);
        }
        foreach(var map in new[]{v.Undissolved,v.Precipitates})foreach(var id in map.Keys.ToList())if(Data.ContainsKey(id)&&Phase(id,v.Temp)=="l"){
            Add(v.Solutes,id,map[id]);map.Remove(id);
        }
    }
    // Dilute ideal-gas/Henry equilibrium, solved algebraically using each gas's
    // partial pressure, not the total pressure (which includes trapped air).
    static void ReconcileClosedGas(Vessel v){
        if(v.Volume()+v.SolidVolume()>=v.InteriorCapacityMl){
            v.PhaseNotice="内容物已占满内腔，无法应用气相空间模型；保留物质量并暂停压力平衡，请打开并减量";
            return;
        }
        VaporEquilibrium.Reconcile(v);
        double rt=v.GasRT,head=v.HeadspaceLitres;
        foreach(var pair in Data){
            var p=pair.Value;if(double.IsNaN(p.Henry)||v.Water<=0||v.Temp<0||v.Temp>30||!SolventProperties.IsAqueousModelValid(v))continue;
            string id=pair.Key;v.Gases.TryGetValue(id,out double gas);v.Solutes.TryGetValue(id,out double dissolved);
            double total=gas+dissolved;if(total<=0)continue;
            double k=p.Henry*Math.Exp(p.HenryTemperature*(1/(v.Temp+273.15)-1/298.15))*v.Water/1000;
            double partial=total/(head/rt+k);
            if(partial>2){v.PhaseNotice="气体分压超出已验证的稀溶液溶解度范围；保留物质量，暂停该气体的溶解平衡；压力仍按 PV=nRT 计算";continue;}
            double solution=k*partial,remaining=total-solution;
            if(solution>1e-12)v.Solutes[id]=solution;else v.Solutes.Remove(id);
            if(remaining>1e-12)v.Gases[id]=remaining;else v.Gases.Remove(id);
        }
        v.PressureBar=(v.TrappedAirMoles+v.Gases.Values.Sum())*rt/head;
    }
    public static void Notice(Vessel v,string id,bool gas,bool escaped=false){
        string message=ShelfName(id)+" 超过当前条件下的饱和量："+(gas?(escaped?"过量气体已逸散":"过量部分转入气相，尚未逸散"):"仅溶解可溶部分，余量以固体保留");
        v.PhaseNotice=message;if(v.PhaseNotices.Add(id+":"+gas+":"+escaped))Lab.Note(v,message,"obs");
    }
}
// Imposed-temperature pure-water phase partition, not a heat/latent-energy solver.
// Melting threshold rounded to 0 C from IAPWS R10-06(2009), 273.152519 K.
// https://iapws.org/technical-guidance/release/Ice-2009
// Near-zero ice density 916.7 kg/m3: Ginnings/Corruccini measurement cited in
// https://www.cambridge.org/core/services/aop-cambridge-core/content/view/8C97F8B5C589D84AEDF5099675AB3C15/S0022143000010376a.pdf
public static class WaterIce {
    public const string Id="water_ice";
    // Near-zero constant-property teaching approximation:
    // NBS RP1260 (1939), fusion 333.5 J/g, rounded at teaching precision.
    // https://nvlpubs.nist.gov/nistpubs/jres/23/jresv23n6p643_A1b.pdf
    // Ice Cp 2.1 J/(g K): Annals of Glaciology, An efficient melting probe
    // for glacial research:
    // https://www.cambridge.org/core/journals/annals-of-glaciology/article/an-efficient-melting-probe-for-glacial-research/3ABD914E78A7FA140BF9E0671DDFA5A9
    public const double FusionJPerGram=333.5,IceCpJPerGramK=2.1;
    public static double IceMass(Vessel v){
        v.Undissolved.TryGetValue(Id,out double a);v.Precipitates.TryGetValue(Id,out double b);
        return (a+b)*ChemData.Sub[Id].Mass;
    }
    public static double Enthalpy(Vessel v)=>v.Water*FusionJPerGram+
        (v.Water*ThermalMixing.WaterCpJPerGramK+IceMass(v)*IceCpJPerGramK)*v.Temp;
    public static bool ApplyHeat(Vessel v,double joules){
        if(double.IsNaN(joules)||double.IsInfinity(joules)||!CanPartition(v))return false;
        double mass=v.Water+IceMass(v);if(mass<=1e-12)return false;
        double energy=Enthalpy(v)+joules,liquid,temp;
        if(energy<0){liquid=0;temp=energy/(mass*IceCpJPerGramK);}
        else if(energy<=mass*FusionJPerGram){liquid=energy/FusionJPerGram;temp=0;}
        else {liquid=mass;temp=(energy-mass*FusionJPerGram)/(mass*ThermalMixing.WaterCpJPerGramK);}
        if(temp< -10||temp>40)return false;
        v.Water=liquid;v.Temp=(float)temp;
        v.Precipitates.Remove(Id);
        if(mass-liquid>1e-12)v.Undissolved[Id]=(mass-liquid)/ChemData.Sub[Id].Mass;
        else v.Undissolved.Remove(Id);
        Warn(v,"纯水常压热量相变近似：冰水共存时保持0℃，热量用于熔化/冻结；定比热容，未计容器热容和过冷成核");
        return true;
    }
    // Existing apparatus rates are qualitative degrees/s, not calibrated watts.
    // Convert each step using the current sensible heat capacity so it cannot
    // skip fusion energy. This does not claim a measured burner power.
    public static void AdvanceTemperature(Vessel v,float target){
        if(CanPartition(v)&&(IceMass(v)>1e-12||v.Temp<0)){
            double capacity=v.Water*ThermalMixing.WaterCpJPerGramK+IceMass(v)*IceCpJPerGramK;
            if(capacity>1e-12){
                if(!ApplyHeat(v,(target-v.Temp)*capacity))Warn(v,"本步热量超出冰水相变近似范围，保留原状态；未跳过潜热直接升温");
                return;
            }
        }
        v.Temp=target;
    }
    const string Approximation="纯水常压定温相变近似：冰密度采用近0℃常数；未模拟相变潜热、过冷成核或混合物凝固点";
    public static bool CanPartition(Vessel v)=>Math.Abs(v.PressureBar-1.01325)<=.005&&v.Temp>=-10&&v.Temp<=40&&
        v.Solutes.Values.All(n=>n<=1e-12)&&v.Undissolved.All(p=>p.Key==Id||p.Value<=1e-12)&&
        v.Precipitates.All(p=>p.Key==Id||p.Value<=1e-12)&&v.Gases.Values.All(n=>n<=1e-12);
    public static void Reconcile(Vessel v){
        v.Undissolved.TryGetValue(Id,out double ice);v.Precipitates.TryGetValue(Id,out double deposit);
        ice+=deposit;
        if(v.Water<=1e-12&&ice<=1e-12)return;
        if(v.Temp>=0&&ice<=1e-12)return;
        if(!CanPartition(v)){
            Warn(v,"当前温压或混合物缺少适用的水/冰相平衡模型；保留原库存，未套用纯水0℃冻结结论");return;
        }
        double mass=ChemData.Sub[Id].Mass;
        if(v.Temp<0){
            ice+=v.Water/mass;v.Water=0;
            v.Undissolved[Id]=ice;v.Precipitates.Remove(Id);
        }else if(v.Temp>0){
            v.Water+=ice*mass;v.Undissolved.Remove(Id);v.Precipitates.Remove(Id);
        }
        // At exactly 0 C, preserve the current ice/liquid split: temperature alone
        // does not determine the latent-energy-dependent phase fraction.
        if(v.Temp==0&&ice>1e-12&&v.Water>1e-12)
            Warn(v,"纯水常压冰水共存：保持0℃及当前相比例；加热按熔化潜热处理，未计容器热容和过冷成核");
        else Warn(v,Approximation);
    }
    static void Warn(Vessel v,string message){
        v.PhaseNotice=message;
        if(v.PhaseNotices.Add("water-ice:"+message))Lab.Note(v,message,"obs");
    }
}
}
