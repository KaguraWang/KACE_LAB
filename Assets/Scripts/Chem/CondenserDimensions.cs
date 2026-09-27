using System;
namespace ChemLab.Chem {
// Shared SI dimensions for the currently rendered Liebig tube. One scene unit
// is 0.1 m. Capacities are geometry, not fitted thermal response parameters.
public static class CondenserDimensions {
    public const double SceneUnitMeters=.1;
    public const double BoreOuterMeters=.0055,BoreInnerMeters=.00451;
    public const double JacketOuterMeters=.014,JacketInnerMeters=.01148;
    public const double JacketEndInsetMeters=.018;
    public struct Measures {
        public double InnerGlassMl,JacketGlassMl,CoolantMl,ContactAreaM2,OuterAreaM2;
        public double GlassMl=>InnerGlassMl+JacketGlassMl;
        public double GlassCapacity(double densityGramsPerMl,double cpJPerGramK){
            if(!(densityGramsPerMl>0)||double.IsInfinity(densityGramsPerMl)||!(cpJPerGramK>0)||double.IsInfinity(cpJPerGramK))
                throw new ArgumentOutOfRangeException("Glass properties must be positive and finite");
            return GlassMl*densityGramsPerMl*cpJPerGramK;
        }
    }
    public static bool TryMeasure(double totalLengthMeters,out Measures result){
        result=default;
        if(double.IsNaN(totalLengthMeters)||double.IsInfinity(totalLengthMeters)||totalLengthMeters<=2*JacketEndInsetMeters)return false;
        double length=totalLengthMeters-2*JacketEndInsetMeters;
        result=new Measures{
            InnerGlassMl=Math.PI*(BoreOuterMeters*BoreOuterMeters-BoreInnerMeters*BoreInnerMeters)*totalLengthMeters*1e6,
            JacketGlassMl=Math.PI*(JacketOuterMeters*JacketOuterMeters-JacketInnerMeters*JacketInnerMeters)*length*1e6,
            CoolantMl=Math.PI*(JacketInnerMeters*JacketInnerMeters-BoreOuterMeters*BoreOuterMeters)*length*1e6,
            ContactAreaM2=2*Math.PI*BoreOuterMeters*length,OuterAreaM2=2*Math.PI*JacketOuterMeters*length
        };
        if(double.IsInfinity(result.GlassMl)||double.IsInfinity(result.CoolantMl)){result=default;return false;}
        return true;
    }
}
}
