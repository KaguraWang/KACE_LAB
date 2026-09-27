using System;
using UnityEngine;
namespace ChemLab.Game {
// Persistent translational state for a single fixed-orientation body. Contact
// constraints and angular dynamics belong to the surrounding solid solver.
// Immersion is evaluated against the same mesh used to render the body.
public sealed class SolidTranslationState {
    readonly SubmergedSolidShape shape;
    readonly double massGrams;
    readonly SubmergedSolidProfile profile;
    SolidVesselBoundary boundary;
    Vector3[] contactVertices;
    public Vector3 Position,Velocity;
    public Vector3 BuoyancyCenter {get;private set;}
    public double DisplacedWorldVolume {get;private set;}
    public SolidTranslationState(SubmergedSolidShape shape,double massGrams,Vector3 position,bool useCachedImmersion=true){
        if(shape==null||!Finite(massGrams)||massGrams<=0||!Finite(position))throw new ArgumentException("Positive solid mass and finite position required");
        this.shape=shape;this.massGrams=massGrams;Position=position;
        if(useCachedImmersion)profile=new SubmergedSolidProfile(shape);
    }
    static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
    static bool Finite(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z);
    public bool AttachBoundary(SolidVesselBoundary vessel,Vector3[] localVertices){
        if(vessel==null||localVertices==null||localVertices.Length==0||!vessel.Contains(localVertices,Position))return false;
        boundary=vessel;contactVertices=(Vector3[])localVertices.Clone();return true;
    }
    public void ApplyContactCorrection(Vector3 offset){
        if(!Finite(offset))throw new ArgumentOutOfRangeException(nameof(offset));
        var proposed=Position+offset;var velocity=Velocity;
        if(boundary==null||boundary.ConstrainStep(contactVertices,Position,ref proposed,ref velocity)){
            Position=proposed;Velocity=velocity;
        }
    }
    // Correct solver drift along contact normals while retaining tangential
    // displacement. Sweeping the whole displacement back would pin a body that
    // is simultaneously rising and moving a tiny distance into the side wall.
    public void AcceptProjectedPose(Vector3 proposed,Vector3 velocity){
        if(!Finite(proposed)||!Finite(velocity))throw new ArgumentOutOfRangeException(nameof(proposed));
        if(boundary==null||boundary.Contains(contactVertices,proposed)||boundary.TryResolve(contactVertices,ref proposed,ref velocity)){
            Position=proposed;Velocity=velocity;
        }
    }
    public void Step(float dt,float liquidSurfaceY,double liquidDensityGramsPerMl,float dragPerSecond,bool paused=false){
        if(!Finite(dt)||dt<0||dt>.25f||!Finite(liquidSurfaceY)||!Finite(liquidDensityGramsPerMl)||liquidDensityGramsPerMl<0||!Finite(dragPerSecond)||dragPerSecond<0||!Finite(Position)||!Finite(Velocity))
            throw new ArgumentOutOfRangeException("Invalid solid integration input");
        if(paused||dt==0)return;
        // 1 world unit = 100 mm; 1 world cubic unit = 1000 mL.
        const double gravity=VesselMesh.GravityWorldUnitsPerSecondSquared;
        int steps=Math.Max(1,(int)Math.Ceiling(dt*240));double h=dt/steps;
        for(int i=0;i<steps;i++){
            var wet=Measure(liquidSurfaceY-Position.y);
            DisplacedWorldVolume=wet.Volume;BuoyancyCenter=Position+wet.Centroid;
            double buoyantMass=liquidDensityGramsPerMl*1000*wet.Volume;
            var acceleration=new Vector3(0,(float)(gravity*(buoyantMass/massGrams-1)),0);
            // Exact constant-force, linear-drag step. Unlike 1-k*dt this cannot
            // flip velocity or amplify energy merely because drag is large.
            double x=dragPerSecond*h,decay=Math.Exp(-x);
            // Stable series also covers exactly zero drag without cancellation.
            double response=x<.001?h*(1-x/2+x*x/6-x*x*x/24):(1-decay)/dragPerSecond;
            double forcePosition=x<.001?h*h*(.5-x/6+x*x/24-x*x*x/120):(h-response)/dragPerSecond;
            var proposed=Position+Velocity*(float)response+acceleration*(float)forcePosition;
            var nextVelocity=Velocity*(float)decay+acceleration*(float)response;
            if(boundary==null||boundary.ConstrainStep(contactVertices,Position,ref proposed,ref nextVelocity)){
                Position=proposed;Velocity=nextVelocity;
            }
        }
        UpdateHydrostatics(liquidSurfaceY);
    }
    public void UpdateHydrostatics(float liquidSurfaceY){
        var finalWet=Measure(liquidSurfaceY-Position.y);
        DisplacedWorldVolume=finalWet.Volume;BuoyancyCenter=Position+finalWet.Centroid;
    }
    public double VolumeBelow(float surfaceY)=>Measure(surfaceY-Position.y).Volume;
    SubmergedSolidShape.Sample Measure(float height)=>profile!=null?profile.Measure(height):shape.Measure(height);
}
}
