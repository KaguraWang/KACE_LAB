using System;
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
// Owns persistent per-grain states and collision proxies. The caller retains the
// mesh/materials. This batch can update that exact mesh without regenerating grain
// shapes every frame. GPU liquid coupling and angular dynamics are separate.
public sealed class SolidBodyBatch:IDisposable {
    readonly SolidBodySeed[] seeds;
    readonly SolidTranslationState[] states;
    readonly SolidContactShape[] contacts;
    readonly Vector3[] vertices;
    readonly Mesh mesh;
    readonly List<SolidPairContact.Constraint> constraints=new List<SolidPairContact.Constraint>();
    readonly SolidLiquidLevel liquidLevel;
    readonly SolidVesselBoundary boundary;
    NativeSolidWorld nativeWorld;
    public bool UsesNativeContacts=>nativeWorld!=null;
    public float NativePenetrationForTest()=>nativeWorld!=null?nativeWorld.MaximumPenetrationForTest():0;
    public string NativeContactReportForTest()=>nativeWorld!=null?nativeWorld.ContactReportForTest():"native disabled";
    public int BoundaryViolationsForTest(){
        int count=0;
        for(int i=0;i<states.Length;i++)if(states[i]!=null&&!boundary.Contains(seeds[i].LocalVertices,states[i].Position,.0002f))count++;
        return count;
    }
    public float LiquidSurfaceY {get;private set;}
    public double ExcessLiquidMl {get;private set;}
    bool dirty,disposed;
    long integrationTicks,contactTicks,levelTicks;
    public double IntegrationMilliseconds=>integrationTicks*1000.0/System.Diagnostics.Stopwatch.Frequency;
    public double ContactMilliseconds=>contactTicks*1000.0/System.Diagnostics.Stopwatch.Frequency;
    public double LevelMilliseconds=>levelTicks*1000.0/System.Diagnostics.Stopwatch.Frequency;
    public int Count=>states.Length;
    public SolidTranslationState State(int i)=>states[i];
    public IReadOnlyList<SolidBodySeed> Seeds=>seeds;
    public void CopyActiveObstacles(List<SolidBodySeed> destination){
        destination.Clear();for(int i=0;i<states.Length;i++)if(states[i]!=null||seeds[i].IsStatic){if(states[i]!=null)seeds[i].Center=states[i].Position;destination.Add(seeds[i]);}
    }
    public void CopyActivePositions(Vector3[] destination){
        int next=0;for(int i=0;i<states.Length;i++)if(states[i]!=null||seeds[i].IsStatic){
            if(next>=destination.Length)throw new ArgumentException("Solid pose buffer too small");
            destination[next++]=states[i]!=null?states[i].Position:seeds[i].Center;
        }
        if(next!=destination.Length)throw new ArgumentException("Solid pose buffer too large");
    }
    public SolidBodyBatch(VesselKind kind,Mesh mesh,List<SolidBodySeed> source,SolidBodyBatch previous=null,bool useNativeContacts=false){
        if(mesh==null||source==null)throw new ArgumentException("Solid mesh and body seeds required");
        this.mesh=mesh;vertices=mesh.vertices;seeds=source.ToArray();
        liquidLevel=new SolidLiquidLevel(kind);
        states=new SolidTranslationState[seeds.Length];contacts=new SolidContactShape[seeds.Length];
        float bedTop=0;
        foreach(var seed in seeds)if(seed.IsStatic){
            liquidLevel.AddStaticBed(seed);foreach(var p in seed.LocalVertices)bedTop=Mathf.Max(bedTop,p.y);
        }
        boundary=new SolidVesselBoundary(kind,bedTop);
        try{
            var used=previous==null?null:new bool[previous.Count];bool changedShape=false;
            for(int i=0;i<seeds.Length;i++){
                var seed=seeds[i];if(seed.IsSpilled||seed.IsStatic)continue;
                var position=seed.Center;var velocity=Vector3.zero;
                if(previous!=null&&!previous.disposed){
                    for(int j=0;j<previous.Count;j++){
                        var old=previous.seeds[j];var oldState=previous.states[j];
                        if(used[j]||oldState==null||old.SubstanceId!=seed.SubstanceId||old.IsPrecipitate!=seed.IsPrecipitate)continue;
                        used[j]=true;
                        if(old.LocalVertices.Length!=seed.LocalVertices.Length||old.Triangles.Length!=seed.Triangles.Length)break;
                        float scale=(float)Math.Pow(seed.VolumeWorldCubed/old.VolumeWorldCubed,1.0/3);
                        var retained=new Vector3[old.LocalVertices.Length];
                        for(int v=0;v<retained.Length;v++)retained[v]=old.LocalVertices[v]*scale;
                        // If growth cannot fit, retain the generated safe placement;
                        // never deform the old body against a wall or mutate it.
                        var candidate=oldState.Position;var candidateVelocity=oldState.Velocity;
                        if(boundary.Contains(retained,candidate)||boundary.TryResolve(retained,ref candidate,ref candidateVelocity)){
                            seed.LocalVertices=retained;seed.Triangles=(int[])old.Triangles.Clone();
                            position=candidate;velocity=candidateVelocity;changedShape=true;
                        }
                        break;
                    }
                }
                if(!boundary.Contains(seed.LocalVertices,position)&&!boundary.TryResolve(seed.LocalVertices,ref position,ref velocity))
                    throw new InvalidOperationException("Solid body cannot fit initial vessel pose: "+seed.SubstanceId);
                seed.Center=position;
                var state=new SolidTranslationState(seed.ImmersionShape(),seed.MassGrams,position){Velocity=velocity};
                if(!state.AttachBoundary(boundary,seed.LocalVertices))throw new InvalidOperationException("Solid boundary attachment failed");
                states[i]=state;contacts[i]=new SolidContactShape(seed.LocalVertices,seed.Triangles);
            }
            if(useNativeContacts)nativeWorld=new NativeSolidWorld(kind,seeds,states);
            dirty=true;
            if(changedShape){
                UpdateMesh();mesh.RecalculateNormals();
                // UV0 encodes material traits, not a texture mapping.
                var normals=mesh.normals;var tangents=new Vector4[normals.Length];
                for(int v=0;v<normals.Length;v++){
                    var axis=Mathf.Abs(normals[v].y)>.9f?Vector3.right:Vector3.up;
                    var tangent=Vector3.Cross(axis,normals[v]).normalized;
                    tangents[v]=new Vector4(tangent.x,tangent.y,tangent.z,1);
                }
                mesh.tangents=tangents;
            }
        }catch{Dispose();throw;}
    }
    public void Step(float dt,float surfaceY,double liquidDensity,float drag,bool paused=false){
        if(disposed)throw new ObjectDisposedException(nameof(SolidBodyBatch));
        if(paused||dt==0)return;
        if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<0||dt>.25f)throw new ArgumentOutOfRangeException(nameof(dt));
        int steps=Math.Max(1,Mathf.CeilToInt(dt*240));float h=dt/steps;
        for(int step=0;step<steps;step++){
            if(nativeWorld!=null){
                long nativeStarted=System.Diagnostics.Stopwatch.GetTimestamp();nativeWorld.Step(h,surfaceY,liquidDensity,drag);
                contactTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-nativeStarted;continue;
            }
            long phaseStart=System.Diagnostics.Stopwatch.GetTimestamp();
            for(int i=0;i<states.Length;i++)states[i]?.Step(h,surfaceY,liquidDensity,drag);
            integrationTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-phaseStart;
            phaseStart=System.Diagnostics.Stopwatch.GetTimestamp();
            constraints.Clear();
            for(int i=0;i<states.Length;i++)if(states[i]!=null)
                for(int j=i+1;j<states.Length;j++)if(states[j]!=null&&SolidPairContact.BuildConstraint(contacts[i],states[i],seeds[i].MassGrams,contacts[j],states[j],seeds[j].MassGrams,out var constraint))constraints.Add(constraint);
            for(int iteration=0;iteration<8;iteration++){
                bool contact=false;
                for(int i=0;i<constraints.Count;i++){
                    var constraint=constraints[i];contact|=SolidPairContact.ProjectConstraint(ref constraint);constraints[i]=constraint;
                }
                if(!contact)break;
            }
            contactTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-phaseStart;
        }
        for(int i=0;i<states.Length;i++)states[i]?.UpdateHydrostatics(surfaceY);
        dirty=true;
    }
    public void UpdateMesh(){
        if(disposed||!dirty)return;
        for(int i=0;i<states.Length;i++)if(states[i]!=null){
            var seed=seeds[i];
            for(int v=0;v<seed.VertexCount;v++)vertices[seed.FirstVertex+v]=states[i].Position+seed.LocalVertices[seed.Triangles[v]];
        }
        mesh.vertices=vertices;mesh.RecalculateBounds();dirty=false;
    }
    public void StepWithLiquidVolume(float dt,double liquidMl,double liquidDensity,float drag,bool paused=false){
        integrationTicks=contactTicks=levelTicks=0;
        nativeWorld?.ResetStepTimings();
        SolidPairContact.ResetDiagnostics();
        if(disposed)throw new ObjectDisposedException(nameof(SolidBodyBatch));
        if(paused||dt==0)return;
        if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<0||dt>.25f)throw new ArgumentOutOfRangeException(nameof(dt));
        int count=Math.Max(1,Mathf.CeilToInt(dt*240));float h=dt/count;
        for(int i=0;i<count;i++){
            long phaseStart=System.Diagnostics.Stopwatch.GetTimestamp();
            LiquidSurfaceY=liquidLevel.Solve(liquidMl,states,out double excess);ExcessLiquidMl=excess;
            levelTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-phaseStart;
            Step(h,LiquidSurfaceY,liquidDensity,drag);
        }
        if(nativeWorld!=null){
            long phaseStart=System.Diagnostics.Stopwatch.GetTimestamp();nativeWorld.ResolveResidualContacts();
            contactTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-phaseStart;
        }
        LiquidSurfaceY=liquidLevel.Solve(liquidMl,states,out double finalExcess);ExcessLiquidMl=finalExcess;
        foreach(var state in states)state?.UpdateHydrostatics(LiquidSurfaceY);
    }
    public float MaximumPenetrationForTest(){
        float max=0;
        for(int i=0;i<states.Length;i++)if(states[i]!=null)for(int j=i+1;j<states.Length;j++)if(states[j]!=null&&
            contacts[i].Penetration(states[i].Position,contacts[j],states[j].Position,out _,out float depth))max=Mathf.Max(max,depth);
        return max;
    }
    public void Dispose(){if(disposed)return;disposed=true;nativeWorld?.Dispose();foreach(var contact in contacts)contact?.Dispose();}
}
}
