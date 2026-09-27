using System;
namespace ChemLab.Chem {
// Explicitly registered pure-liquid models, never inferred from chemical family.
// Methanol uses a declared constant-Cp teaching approximation; ethanol retains
// its sourced temperature-dependent enthalpy curve.
public sealed class PureLiquidThermalModel {
    public readonly string Id,Name,Assumptions;
    public readonly double MinC,BoilingC,LatentJPerGram;
    readonly double cp;
    LiquidEnthalpyCurve curve;
    PureLiquidThermalModel(string id,string name,double min,double boil,double latent,double heatCapacity,string assumptions){
        Id=id;Name=name;MinC=min;BoilingC=boil;LatentJPerGram=latent;cp=heatCapacity;Assumptions=assumptions;
    }
    public static readonly PureLiquidThermalModel Ethanol=new PureLiquidThermalModel("c2h5oh","乙醇",-15,
        EthanolThermodynamics.NormalBoilingC,EthanolThermodynamics.NormalBoilingLatentJPerGram,0,"有界变热容液相焓");
    // NIST SRD69: Cp(298.15 K)=81.11 J/mol/K (Khasanshin & Zykova 1989);
    // latent=35.21 kJ/mol at 337.7 K (Majer & Svoboda 1985), M=32.0419.
    // https://webbook.nist.gov/cgi/cbook.cgi?ID=C67561&Mask=7EF
    // The 25 C Cp is held constant over 0--64.55 C for classroom use, NOT
    // a measured temperature-dependent fit. No gas Cp or mixture data implied.
    public static readonly PureLiquidThermalModel Methanol=new PureLiquidThermalModel("ch3oh","甲醇",0,
        64.55,35210/32.0419,81.11/32.0419,"25℃热容用于0–64.55℃的定热容教学近似");
    public static PureLiquidThermalModel For(string id)=>id==Ethanol.Id?Ethanol:id==Methanol.Id?Methanol:null;
    public bool Valid(double t)=>!double.IsNaN(t)&&!double.IsInfinity(t)&&t>=MinC-1e-5&&t<=BoilingC+1e-5;
    public double H(double t){
        if(!Valid(t))throw new ArgumentOutOfRangeException(nameof(t));
        t=Math.Max(MinC,Math.Min(BoilingC,t)); // float vessel endpoint roundoff only
        if(Id==Ethanol.Id){EthanolThermodynamics.TryLiquidEnthalpy(t,out double h);return h;}
        return cp*(t-25);
    }
    public double T(double h){
        if(double.IsNaN(h)||double.IsInfinity(h)||h<H(MinC)-1e-8||h>H(BoilingC)+1e-8)
            throw new ArgumentOutOfRangeException(nameof(h));
        h=Math.Max(H(MinC),Math.Min(H(BoilingC),h));
        if(Id==Ethanol.Id){EthanolThermodynamics.TryLiquidTemperature(h,out double t);return t;}
        return 25+h/cp;
    }
    public double HeatCapacity(double t){
        if(!Valid(t))throw new ArgumentOutOfRangeException(nameof(t));
        if(Id==Ethanol.Id){EthanolThermodynamics.TryLiquidHeatCapacity(Math.Max(MinC,Math.Min(BoilingC,t)),out double c);return c;}
        return cp;
    }
    public LiquidEnthalpyCurve Curve=>Id==Ethanol.Id?EthanolThermodynamics.CondenserCurve:
        curve??(curve=new LiquidEnthalpyCurve(new[]{MinC,BoilingC},new[]{H(MinC)*1000,H(BoilingC)*1000}));
}
}
