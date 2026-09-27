using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using ChemLab.Chem;
namespace ChemLab.Editor {
public static class IonMetadataAudit {
    public static void Run(){
        Lab.EnsureReady();int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){if(ok)pass++;else fail++;lines.Add((ok?"PASS ":"FAIL ")+label);}
        foreach(var s in ChemData.Sub.Values.Where(s=>s.Cls=="ion")){
            Check("ion classification and charge "+s.Id,s.IsIon&&s.Charge!=0&&s.State=="aq");
        }
        var expected=new Dictionary<string,int>{
            {"[Al(OH)4]-",-1},{"[CuCl4]2-",-2},{"[Cu(H2O)6]2+",2},{"[Co(H2O)6]2+",2},
            {"[CoCl4]2-",-2},{"scn-",-1},{"CH3CH(OH)COO-",-1},{"CH3CHBrCOO-",-1},
            {"Cr(OH)4-",-1},{"[Zn(NH3)4]2+",2},{"NO2-",-1},{"IO3-",-1},{"S4O6^2-",-2},
            {"s2o8^2-",-2},{"benzenediazonium",1},{"phenoxide",-1},{"c2o4^2-",-2},
            {"Co2+",2},{"[Co(NH3)6]2+",2},{"C2H5NH3+",1},{"glycinate",-1},{"glycine_h",1}
        };
        foreach(var item in expected)Check("explicit stock-table ionic charge "+item.Key,
            ChemData.Sub.TryGetValue(item.Key,out var s)&&s.IsIon&&s.Charge==item.Value);
        Check("shelf filter excludes every ion-classified substance",!ChemData.Sub.Values.Any(s=>!s.IsIon&&s.Cls=="ion"));
        Check("neutral sodium phenoxide reagent remains on shelf",ChemData.Sub.TryGetValue("c6h5ona",out var salt)&&!salt.IsIon&&salt.Charge==0);
        Check("neutral oxidation product resolves to molecular stock",ChemData.Resolve("C6H6O6")=="c6h6o6"&&!ChemData.Sub["c6h6o6"].IsIon&&!ChemData.Sub.ContainsKey("C6H6O6"));
        Check("phenoxide is not named sodium phenoxide",ChemData.Sub["phenoxide"].Name=="苯氧根离子"&&ChemData.SpeciesIndex["苯酚钠"]=="c6h5ona");
        var formulas=new Dictionary<string,int>{{"NH4+",1},{"HCO3-",-1},{"Fe3+",3},{"SO4^2-",-2},{"[Cu(NH3)4]2+",2},{"C6H6O6",0},{"C2H5NH3+",1},{"[H3NCH2COOH]+",1},{"NH4+(aq)",1}};
        foreach(var item in formulas)Check("formula charge "+item.Key,Formula.IonicCharge(item.Key)==item.Value);
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/ion-metadata-audit.txt",lines);
        EditorApplication.Exit(fail==0?0:1);
    }
}
}
