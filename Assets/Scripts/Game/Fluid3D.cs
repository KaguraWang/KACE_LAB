using System;
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Game {
// CPU position-based fluid: 3D Lagrangian particles, iterative density constraints,
// SPH kernels, XSPH viscosity, local-frame inertia, and vessel collision projection.
// Reference: Macklin & Muller, Position Based Fluids (2013), equations 1–12.
public sealed class Fluid3D {
    public Vector3[] Positions { get; private set; } = new Vector3[0];
    Vector3[] predicted, velocity, correction;
    float[] lambda;
    int[][] neighbors;
    readonly Dictionary<Vector3Int,List<int>> cells=new Dictionary<Vector3Int,List<int>>();
    readonly Stack<List<int>> pool=new Stack<List<int>>();
    List<Vector2> profile;
    readonly float[] radii=new float[513];
    float h, h2, poly, spiky, particleVolume, accumulator;
    float radius,height,wall,spacing,floor;
    Vector3 previousPosition, previousVelocity;
    bool initialized;
    public double VolumeMl {get; private set;}
    public float KineticEnergy {get; private set;}
    public float MaxCompression {get; private set;}
    public float Spacing=>spacing;
    public float Height=>height;
    public float Floor=>floor;
    public float Radius=>radius;
    public int Steps {get;private set;}
    public float MassVolumeMl=>particleVolume*Positions.Length*1000;
    public void Configure(VesselKind kind,double ml) {
        if(profile!=null && Math.Abs(VolumeMl-ml)<Math.Max(0.05,ml*0.015)) return;
        VolumeMl=Math.Max(0,ml);
        var g=VesselMesh.Get(kind);radius=g.R;height=g.H;wall=Mathf.Max(0.012f,radius*0.052f);
        floor=kind==VesselKind.Cylinder?.04f:0;
        profile=VesselMesh.Profile(g.Type,radius,height);
        for(int i=0;i<radii.Length;i++)radii[i]=Mathf.Max(0.0005f,VesselMesh.RadiusAt(profile,i/512f)-wall);
        if(ml<=0.05){Positions=new Vector3[0];return;}
        float top=VesselMesh.FillFractionFor(kind,ml)*height;
        float volume=(float)ml/1000;
        spacing=Mathf.Pow(volume/850f,1f/3);
        spacing=Mathf.Clamp(spacing,0.008f,0.075f);
        var points=new List<Vector3>();
        for(float y=floor+spacing*0.5f;y<Mathf.Min(top,height-spacing*0.25f);y+=spacing) {
            float r=Mathf.Max(0,InnerRadius(y)-spacing*0.3f);
            for(float x=-radius+spacing*0.5f;x<radius;x+=spacing)
                for(float z=-radius+spacing*0.5f;z<radius;z+=spacing)
                    if(x*x+z*z<r*r) points.Add(new Vector3(x,y,z));
        }
        if(points.Count==0) points.Add(new Vector3(0,Mathf.Max(top*0.5f,spacing),0));
        Positions=points.ToArray();int n=Positions.Length;
        predicted=new Vector3[n];velocity=new Vector3[n];correction=new Vector3[n];lambda=new float[n];neighbors=new int[n][];
        particleVolume=volume/n;
        spacing=Mathf.Pow(particleVolume,1f/3);
        h=spacing*1.8f;h2=h*h;
        poly=315f/(64f*Mathf.PI*Mathf.Pow(h,9));spiky=-45f/(Mathf.PI*Mathf.Pow(h,6));
        accumulator=0;initialized=false;Steps=0;
    }
    public float InnerRadius(float y){float q=Mathf.Clamp01(y/height)*512;int i=Mathf.Min(511,(int)q);return Mathf.Lerp(radii[i],radii[i+1],q-i);}
    public float BoundaryDistance(Vector3 p) => Mathf.Min(p.y-floor,Mathf.Min(height-p.y,InnerRadius(p.y)-new Vector2(p.x,p.z).magnitude));
    Vector3 Project(Vector3 p) {
        p.y=Mathf.Clamp(p.y,floor+spacing*0.22f,height-spacing*0.22f);
        // Correct along the sloping wall normal, not just radially: otherwise gravity
        // crushes all particles into the pole of a rounded bottom.
        for(int k=0;k<6;k++) {
            float r=InnerRadius(p.y),margin=spacing*0.22f;
            float d=Mathf.Sqrt(p.x*p.x+p.z*p.z);
            float slope=(InnerRadius(p.y+0.002f)-InnerRadius(p.y-0.002f))/0.004f;
            float length=Mathf.Sqrt(1+slope*slope),distance=(r-d)/length;
            if(distance>=margin)break;
            float move=margin-distance;
            if(d>0.00001f){float radialMove=Mathf.Min(d,move/length);p.x-=radialMove*p.x/d;p.z-=radialMove*p.z/d;}
            p.y=Mathf.Clamp(p.y+move*slope/length,floor+margin,height-margin);
        }
        // Sharp profile corners may not converge in six normal iterations.
        // Never overshoot the axis or return a particle outside the final radius.
        float radial=Mathf.Sqrt(p.x*p.x+p.z*p.z),limit=Mathf.Max(0,InnerRadius(p.y)-spacing*.22f);
        if(radial>limit&&radial>1e-8f){p.x*=limit/radial;p.z*=limit/radial;}
        return p;
    }
    Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.FloorToInt(p.x/h),Mathf.FloorToInt(p.y/h),Mathf.FloorToInt(p.z/h));
    void Neighborhood() {
        foreach(var list in cells.Values){list.Clear();pool.Push(list);}cells.Clear();
        for(int i=0;i<predicted.Length;i++) {
            var key=Cell(predicted[i]);if(!cells.TryGetValue(key,out var list)){list=pool.Count>0?pool.Pop():new List<int>(20);cells.Add(key,list);}list.Add(i);
        }
        var scratch=new List<int>(80);
        for(int i=0;i<predicted.Length;i++) {
            scratch.Clear();var key=Cell(predicted[i]);
            for(int x=-1;x<=1;x++) for(int y=-1;y<=1;y++) for(int z=-1;z<=1;z++)
                if(cells.TryGetValue(key+new Vector3Int(x,y,z),out var list)) foreach(int j in list)
                    if((predicted[i]-predicted[j]).sqrMagnitude<h2) scratch.Add(j);
            neighbors[i]=scratch.ToArray();
        }
    }
    float W(float r2){float q=Mathf.Max(0,h2-r2);return poly*q*q*q;}
    Vector3 Grad(Vector3 d){float r=d.magnitude;if(r<0.00001f||r>=h)return Vector3.zero;return d*(spiky*(h-r)*(h-r)/r*particleVolume);}
    public void Step(Vector3 position,Quaternion rotation,float dt,bool stir,float bubble) {
        if(Positions.Length==0)return;
        if(!initialized){previousPosition=position;initialized=true;}
        float frameDt=Mathf.Max(dt,0.001f);
        var frameV=(position-previousPosition)/frameDt;
        var acceleration=Quaternion.Inverse(rotation)*Vector3.ClampMagnitude((frameV-previousVelocity)/frameDt,18);
        previousPosition=position;previousVelocity=frameV;
        var gravity=Quaternion.Inverse(rotation)*new Vector3(0,-(float)VesselMesh.GravityWorldUnitsPerSecondSquared,0);
        accumulator+=Mathf.Min(dt,0.05f);
        const float step=1f/90;
        int substeps=0;
        while(accumulator>=step && substeps++<4) {
            accumulator-=step;Steps++;
            for(int i=0;i<Positions.Length;i++) {
                var p=Positions[i];var force=gravity-acceleration;
                if(stir)force+=new Vector3(-p.z,0,p.x)*4;
                if(bubble>0)force.y+=Mathf.Min(2,bubble)*Mathf.Exp(-(p.x*p.x+p.z*p.z)/(radius*radius*0.15f))*2;
                velocity[i]+=force*step;predicted[i]=Project(p+velocity[i]*step);
            }
            Neighborhood();MaxCompression=0;
            for(int iteration=0;iteration<4;iteration++) {
                for(int i=0;i<Positions.Length;i++) {
                    float density=0,denom=0;Vector3 sum=Vector3.zero;
                    foreach(int j in neighbors[i]) {
                        var d=predicted[i]-predicted[j];density+=particleVolume*W(d.sqrMagnitude);
                        if(j==i)continue;var grad=Grad(d);denom+=grad.sqrMagnitude;sum+=grad;
                    }
                    // Boundary deficiency compensation (stationary solid volume).
                    float wallDensity=0.42f*Mathf.Pow(Mathf.Clamp01(1-BoundaryDistance(predicted[i])/h),2);
                    float constraint=Mathf.Max(0,density+wallDensity-1);
                    MaxCompression=Mathf.Max(MaxCompression,constraint);
                    lambda[i]=-constraint/(denom+sum.sqrMagnitude+0.01f/(spacing*spacing));
                }
                for(int i=0;i<Positions.Length;i++) {
                    Vector3 delta=Vector3.zero;
                    foreach(int j in neighbors[i]) if(i!=j) {
                        var d=predicted[i]-predicted[j];
                        float q=W(d.sqrMagnitude)/W(h2*0.09f);
                        float pressure=-0.0001f*spacing*spacing*q*q*q*q;
                        delta+=(lambda[i]+lambda[j]+pressure)*Grad(d);
                    }
                    correction[i]=Vector3.ClampMagnitude(delta,spacing*0.25f);
                }
                for(int i=0;i<Positions.Length;i++) predicted[i]=Project(predicted[i]+correction[i]);
            }
            for(int i=0;i<Positions.Length;i++) velocity[i]=Vector3.ClampMagnitude((predicted[i]-Positions[i])/step,4);
            KineticEnergy=0;
            for(int i=0;i<Positions.Length;i++) {
                Vector3 smooth=Vector3.zero;
                foreach(int j in neighbors[i]) smooth+=(velocity[j]-velocity[i])*(particleVolume*W((predicted[i]-predicted[j]).sqrMagnitude));
                correction[i]=(velocity[i]+smooth*0.06f)*0.998f;
            }
            for(int i=0;i<Positions.Length;i++){velocity[i]=correction[i];Positions[i]=predicted[i];KineticEnergy+=velocity[i].sqrMagnitude/Positions.Length;}
        }
    }
    public void Impulse(Vector3 impulse){for(int i=0;i<velocity.Length;i++)velocity[i]+=impulse;}
}
}
