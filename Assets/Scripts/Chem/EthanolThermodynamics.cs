using System;
using System.Collections.Generic;
namespace ChemLab.Chem {
// Pure liquid only. The low-temperature branch is an explicit classroom
// constant-Cp approximation; the 25--40 C bridge interpolates two sourced Cp
// endpoints. No excess mixing enthalpy or vapor heat capacity is implied.
public static class EthanolThermodynamics {
    static LiquidEnthalpyCurve condenserCurve;
    public static LiquidEnthalpyCurve CondenserCurve=>condenserCurve??(condenserCurve=BuildCondenserCurve(.25));
    public static LiquidEnthalpyCurve BuildCondenserCurve(double maxStepC){
        if(double.IsNaN(maxStepC)||double.IsInfinity(maxStepC)||maxStepC<.01||maxStepC>10)throw new ArgumentOutOfRangeException(nameof(maxStepC));
        var temperatures=new List<double>();var enthalpies=new List<double>();
        // Include the two property-branch boundaries exactly.
        double start=MinC;
        temperatures.Add(start);enthalpies.Add(H(start)*1000);
        foreach(double end in new[]{25d,40d,NormalBoilingC}){
            int count=(int)Math.Ceiling((end-start)/maxStepC);
            for(int i=1;i<=count;i++){double t=i==count?end:start+(end-start)*i/count;temperatures.Add(t);enthalpies.Add(H(t)*1000);}
            start=end;
        }
        return new LiquidEnthalpyCurve(temperatures.ToArray(),enthalpies.ToArray());
    }
    public const double MinC=-15,NormalBoilingC=78.35;
    public const double ReferenceMolarMass=46.0684;
    public const double NormalBoilingLatentJPerGram=38560/ReferenceMolarMass;
    // NIST SRD69: Cp(298.15 K)=112.4 J/(mol K), latent=38.56 kJ/mol at 351.5 K.
    // https://webbook.nist.gov/cgi/cbook.cgi?ID=C64175&Mask=16
    const double LowCp=112.4/ReferenceMolarMass;
    // Fiock, Ginnings & Holton (1931), Table 7, p.893, measured 40--110 C.
    // F(t)=22.648(t/10)+.3053(t/10)^2+.014233(t/10)^3, int.J/g.
    // Table 8 and p.894 permit Cp ~ dH_sat/dT below normal boiling point.
    // https://nvlpubs.nist.gov/nistpubs/jres/6/jresv6n5p881_A2b.pdf
    // NBS SP300 vol.3: US international joule = 1.000165 absolute joule.
    const double InternationalJoule=1.000165;
    static double HighH(double t){double x=t/10;return (22.648*x+.3053*x*x+.014233*x*x*x)*InternationalJoule;}
    static double HighCp(double t){double x=t/10;return (2.2648+.06106*x+.0042699*x*x)*InternationalJoule;}
    static double BridgeSlope=>(HighCp(40)-LowCp)/15;
    static double H40=>15*LowCp+.5*BridgeSlope*225;
    public static bool ValidTemperature(double t)=>!double.IsNaN(t)&&!double.IsInfinity(t)&&t>=MinC&&t<=NormalBoilingC;
    static double H(double t){
        if(t<=25)return LowCp*(t-25);
        if(t<=40){double d=t-25;return LowCp*d+.5*BridgeSlope*d*d;}
        return H40+HighH(t)-HighH(40);
    }
    public static bool TryLiquidEnthalpy(double t,out double joulesPerGram){
        joulesPerGram=0;if(!ValidTemperature(t))return false;
        joulesPerGram=H(t);return true;
    }
    public static bool TryLiquidHeatCapacity(double t,out double joulesPerGramK){
        joulesPerGramK=0;if(!ValidTemperature(t))return false;
        joulesPerGramK=t<=25?LowCp:t<=40?LowCp+BridgeSlope*(t-25):HighCp(t);return true;
    }
    public static bool TryLiquidTemperature(double joulesPerGram,out double t){
        t=0;
        if(double.IsNaN(joulesPerGram)||double.IsInfinity(joulesPerGram)||joulesPerGram<H(MinC)||joulesPerGram>H(NormalBoilingC))return false;
        double low=MinC,high=NormalBoilingC;
        for(int i=0;i<64;i++){double mid=(low+high)*.5;if(H(mid)<joulesPerGram)low=mid;else high=mid;}
        t=(low+high)*.5;return true;
    }
}
}
