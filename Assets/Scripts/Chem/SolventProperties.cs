using System.Collections.Generic;
namespace ChemLab.Chem {
// Solvents remain ordinary molecular inventory entries; no duplicate mass ledger.
public static class SolventProperties {
    // Recognition is a guard against applying pure-water coefficients, NOT a claim
    // that these liquids are mutually miscible or have known solubility curves.
    // Room-temperature phases/sources for the expansion live in PhaseProperties.Data.
    public static readonly HashSet<string> Organic=new HashSet<string>{
        "c2h5oh","acetone","c6h6","cyclohexane","ch3oh","ch3ch2ch2oh",
        "n_pentane","n_hexane","n_heptane","n_octane",
        "pent_1_ene","hex_1_ene","hept_1_ene","oct_1_ene",
        "butan_1_ol","pentan_1_ol","hexan_1_ol","heptan_1_ol","octan_1_ol",
        "butanal","pentanal","hexanal","heptanal","octanal",
        "butanoic_acid","pentanoic_acid","hexanoic_acid","heptanoic_acid","octanoic_acid"
    };
    public static double OrganicMoles(Vessel v){
        double n=0;foreach(var id in Organic)n+=LiquidMoles(v,id);return n;
    }
    public static double LiquidMoles(Vessel v,string id){
        v.Solutes.TryGetValue(id,out double liquid);
        // A deposited or undissolved substance can melt before its ledger entry
        // is reconciled, including alongside other solutes. Match LiquidVolume's
        // phase-aware displacement; never mistake that mixture for pure water.
        // Actual frozen deposits are not solvents. Solutes may be pressure-retained.
        if(PhaseProperties.Phase(id,v.Temp)=="l"){
            if(v.Undissolved.TryGetValue(id,out double pending))liquid+=pending;
            if(v.Precipitates.TryGetValue(id,out double melted))liquid+=melted;
        }
        return liquid;
    }
    public static bool HasSolvent(Vessel v)=>v.Water>1e-9||OrganicMoles(v)>1e-9;
    public static bool IsAqueousModelValid(Vessel v)=>OrganicMoles(v)<1e-9;
    public static string Description(Vessel v)=>OrganicMoles(v)>1e-9?(v.Water>1e-9?"混合溶剂":"非水溶剂"):"水";
}
}
