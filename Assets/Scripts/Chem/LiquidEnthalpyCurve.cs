using System;
namespace ChemLab.Chem {
// Immutable, monotonic liquid enthalpy table. Interpolation is linear in h(T),
// not Cp(T). Its maximum temperature is the solver's saturation temperature;
// enthalpies are normalized to saturated liquid = 0 J/kg.
public sealed class LiquidEnthalpyCurve {
    readonly double[] temperatures,enthalpies;
    public int Count=>temperatures.Length;
    public double MinC=>temperatures[0];
    public double MaxC=>temperatures[Count-1];
    public double KnotEnthalpy(int index)=>enthalpies[index];
    public double KnotTemperature(int index)=>temperatures[index];
    public LiquidEnthalpyCurve(double[] temperaturesC,double[] joulesPerKg){
        if(temperaturesC==null||joulesPerKg==null||temperaturesC.Length<2||temperaturesC.Length!=joulesPerKg.Length)
            throw new ArgumentException("Liquid enthalpy curve needs matching arrays with at least two knots");
        temperatures=(double[])temperaturesC.Clone();enthalpies=(double[])joulesPerKg.Clone();
        for(int i=0;i<Count;i++){
            if(double.IsNaN(temperatures[i])||double.IsInfinity(temperatures[i])||temperatures[i]<=-273.15||
               double.IsNaN(enthalpies[i])||double.IsInfinity(enthalpies[i])||
               (i>0&&(temperatures[i]<=temperatures[i-1]||enthalpies[i]<=enthalpies[i-1])))
                throw new ArgumentException("Liquid enthalpy knots must be finite and strictly increasing");
        }
        double saturated=enthalpies[Count-1];
        for(int i=0;i<Count;i++){
            enthalpies[i]-=saturated;
            if(double.IsInfinity(enthalpies[i])||(i>0&&enthalpies[i]<=enthalpies[i-1]))
                throw new ArgumentException("Liquid enthalpy normalization overflows or loses knot precision");
        }
    }
    static double Interpolate(double[] x,double[] y,double value){
        // Solver bounds are validated before use. Clamp endpoint roundoff only;
        // this object is deliberately internal to a bounded property contract.
        if(value<=x[0])return y[0];
        if(value>=x[x.Length-1])return y[y.Length-1];
        int low=0,high=x.Length-1;
        while(high-low>1){int mid=(low+high)/2;if(x[mid]<=value)low=mid;else high=mid;}
        double fraction=(value-x[low])/(x[high]-x[low]);return y[low]+fraction*(y[high]-y[low]);
    }
    internal double Enthalpy(double temperature)=>Interpolate(temperatures,enthalpies,temperature);
    internal double Temperature(double enthalpy)=>Interpolate(enthalpies,temperatures,enthalpy);
}
}
