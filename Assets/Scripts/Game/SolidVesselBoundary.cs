using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
// Static, axisymmetric vessel contact. Tests every actual transformed vertex;
// translates a body as a whole rather than deforming it or clamping its vertices.
public sealed class SolidVesselBoundary {
    readonly List<Vector2> profile;
    readonly float height,wall,floor;
    readonly int[] radiusSegment=new int[513];
    readonly bool straightWall;
    readonly double radiusBase,radiusSlope;
    sealed class BodyBounds {
        public float Low,High,Radius;
        public bool HasQuery,QueryResult,HasValid,HasNormals;
        public Vector3 QueryPosition,ValidPosition,NormalPosition;
        public float QueryTolerance,ValidTolerance;
        public Vector3[] Normals;
        public int NormalCount;
    }
    readonly Dictionary<Vector3[],BodyBounds> boundsCache=new Dictionary<Vector3[],BodyBounds>();
    public SolidVesselBoundary(VesselKind kind,float minimumFloor=0){
        var g=VesselMesh.Get(kind);height=g.H;wall=Mathf.Max(.012f,g.R*.052f);
        floor=Mathf.Max(minimumFloor,Mathf.Max(wall,kind==VesselKind.Cylinder?.04f:0));
        profile=VesselMesh.Profile(g.Type,g.R,g.H);
        straightWall=g.Type==0||g.Type==5;
        radiusBase=profile[0].x-wall;radiusSlope=(profile[profile.Count-1].x-profile[0].x)/(double)height;
        int segment=1;
        for(int i=0;i<=512;i++){
            float y=height*i/512f;
            while(segment<profile.Count-1&&profile[segment].y<y)segment++;
            radiusSegment[i]=segment;
        }
    }
    float Radius(float y){
        // Beaker/bowl profiles are exactly straight segments. Evaluate their
        // shared line directly instead of searching/interpolating the same
        // collinear rings for every mesh vertex in every physics substep.
        if(straightWall)return Mathf.Max(.0005f,(float)(radiusBase+radiusSlope*Mathf.Clamp(y,0,height)));
        float fraction=Mathf.Clamp01(y/height),sample=height*fraction;
        int segment=radiusSegment[Mathf.Min(512,(int)(fraction*512))];
        // The table only locates the original interval: interpolate the actual
        // profile, not a resampled wall that might cut corners or change volume.
        while(segment<profile.Count-1&&profile[segment].y<sample)segment++;
        while(segment>1&&profile[segment-1].y>=sample)segment--;
        var a=profile[segment-1];var b=profile[segment];
        float k=Mathf.Approximately(b.y,a.y)?0:(sample-a.y)/(b.y-a.y);
        return Mathf.Max(.0005f,Mathf.Lerp(a.x,b.x,k)-wall);
    }
    BodyBounds BoundsFor(Vector3[] vertices){
        if(boundsCache.TryGetValue(vertices,out var value))return value;
        value=new BodyBounds{Low=float.PositiveInfinity,High=float.NegativeInfinity,Normals=new Vector3[vertices.Length]};
        foreach(var p in vertices){value.Low=Mathf.Min(value.Low,p.y);value.High=Mathf.Max(value.High,p.y);value.Radius=Mathf.Max(value.Radius,new Vector2(p.x,p.z).magnitude);}
        boundsCache.Add(vertices,value);return value;
    }
    // For one small solver substep with an already-valid pose. If a narrow
    // nozzle cannot accept the entire body, stop at the last feasible pose
    // instead of trying to squeeze or teleport it through the nozzle.
    public bool ConstrainStep(Vector3[] vertices,Vector3 previous,ref Vector3 proposed,ref Vector3 velocity){
        if(!Contains(vertices,previous))return false;
        if(Contains(vertices,proposed))return true;
        if(TryStraightClip(vertices,previous,proposed,out var clipped)){
            ClipVelocity(vertices,clipped,ref velocity);proposed=clipped;return true;
        }
        var target=proposed;float fraction=1;
        // Clip only a violating vertex's trajectory. Re-testing every vertex at
        // each bisection made dense contact against glass quadratic in mesh size.
        foreach(var local in vertices){
            if(PointContained(local+Vector3.LerpUnclamped(previous,target,fraction),1e-6f))continue;
            float lo=0,hi=fraction;
            for(int i=0;i<24;i++){
                float mid=(lo+hi)*.5f;
                if(PointContained(local+Vector3.LerpUnclamped(previous,target,mid),1e-6f))lo=mid;else hi=mid;
            }
            fraction=lo;
        }
        var safe=Vector3.LerpUnclamped(previous,target,fraction);
        // Non-convex profile transitions can invalidate an earlier vertex when
        // a later constraint shortens the motion. Keep the whole-body fallback.
        if(!Contains(vertices,safe,1e-6f)){
            float lo=0,hi=fraction;
            for(int i=0;i<24;i++){
                float mid=(lo+hi)*.5f;
                if(Contains(vertices,Vector3.LerpUnclamped(previous,target,mid),1e-6f))lo=mid;else hi=mid;
            }
            safe=Vector3.LerpUnclamped(previous,target,lo);
        }
        ClipVelocity(vertices,safe,ref velocity);
        proposed=safe;return true;
    }
    bool TryStraightClip(Vector3[] vertices,Vector3 previous,Vector3 target,out Vector3 safe){
        safe=previous;if(!straightWall)return false;
        var bounds=BoundsFor(vertices);
        // Open-mouth entry/departure is non-convex; keep the general path there.
        if(previous.y+bounds.High>height||target.y+bounds.High>height)return false;
        var delta=target-previous;double fraction=1;
        if(delta.y<0)fraction=System.Math.Min(fraction,(previous.y+bounds.Low-floor+1e-6)/-delta.y);
        if(fraction<0)return false;
        foreach(var local in vertices){
            var p=previous+local;
            double r=radiusBase+radiusSlope*p.y+1e-6,dr=radiusSlope*delta.y;
            double a=(double)delta.x*delta.x+(double)delta.z*delta.z-dr*dr;
            double b=2*((double)p.x*delta.x+(double)p.z*delta.z-r*dr);
            double c=(double)p.x*p.x+(double)p.z*p.z-r*r;
            if(c>0)return false; // Do not analytically extend an invalid start.
            if((a*fraction+b)*fraction+c<=0)continue;
            double root=double.PositiveInfinity;
            if(System.Math.Abs(a)<1e-24){if(b>0)root=-c/b;}
            else{
                double disc=b*b-4*a*c;if(disc<0)return false;
                double q=-.5*(b+(b>=0?1:-1)*System.Math.Sqrt(disc));
                double r1=q/a,r2=q==0?double.PositiveInfinity:c/q;
                if(r1>=0&&2*a*r1+b>0)root=r1;
                if(r2>=0&&2*a*r2+b>0)root=System.Math.Min(root,r2);
            }
            if(double.IsInfinity(root)||double.IsNaN(root))return false;
            fraction=System.Math.Min(fraction,root);
        }
        safe=Vector3.LerpUnclamped(previous,target,(float)System.Math.Max(0,fraction));
        // Original piecewise float profile remains authoritative. No approximate
        // analytical boundary is allowed to bypass the actual-vertex check.
        return Contains(vertices,safe,2e-6f);
    }
    void ClipVelocity(Vector3[] vertices,Vector3 safe,ref Vector3 velocity){
        var cached=BoundsFor(vertices);
        // Reuse normals only for bit-identical geometry/position. Any movement
        // rebuilds the same ordered set, preserving sequential velocity clipping.
        if(!cached.HasNormals||!cached.NormalPosition.Equals(safe)){
          cached.NormalCount=0;
          foreach(var local in vertices){
            var p=safe+local;var n=Vector3.zero;
            if(p.y<=floor+3e-5f)n=Vector3.up;
            else if(p.y<=height){
                float d=new Vector2(p.x,p.z).magnitude;
                if(d>1e-8f&&d>=Radius(p.y)-3e-5f){
                    float slope=(Radius(p.y+.0005f)-Radius(p.y-.0005f))/.001f;
                    n=new Vector3(-p.x/d,slope,-p.z/d).normalized;
                }
            }
            if(n.sqrMagnitude>0)cached.Normals[cached.NormalCount++]=n;
          }
          cached.HasNormals=true;cached.NormalPosition=safe;
        }
        for(int i=0;i<cached.NormalCount;i++){
            var n=cached.Normals[i];float incoming=Vector3.Dot(velocity,n);if(incoming<0)velocity-=n*incoming;
        }
    }
    bool PointContained(Vector3 p,float tolerance)=>p.y>=floor-tolerance&&
        (p.y>height||new Vector2(p.x,p.z).magnitude<=Radius(p.y)+tolerance);
    public bool Contains(Vector3[] vertices,Vector3 position,float tolerance=2e-5f){
        if(vertices==null||vertices.Length==0||float.IsNaN(position.sqrMagnitude)||float.IsInfinity(position.sqrMagnitude))return false;
        var bounds=BoundsFor(vertices);
        if(bounds.HasValid&&bounds.ValidPosition.Equals(position)&&tolerance>=bounds.ValidTolerance)return true;
        if(bounds.HasQuery&&bounds.QueryPosition.Equals(position)&&tolerance==bounds.QueryTolerance)return bounds.QueryResult;
        bool result=ContainsUncached(vertices,position,tolerance,bounds);
        bounds.HasQuery=true;bounds.QueryPosition=position;bounds.QueryTolerance=tolerance;bounds.QueryResult=result;
        if(result){bounds.HasValid=true;bounds.ValidPosition=position;bounds.ValidTolerance=tolerance;}
        return result;
    }
    bool ContainsUncached(Vector3[] vertices,Vector3 position,float tolerance,BodyBounds bounds){
        // A conservative cylinder test accepts the overwhelmingly common case
        // wholly away from walls. It never rejects a shape: near boundaries we
        // still use all of the actual visible mesh vertices below.
        float low=position.y+bounds.Low,high=position.y+bounds.High;
        if(low>=floor-tolerance){
            if(low>height)return true;
            float top=Mathf.Min(high,height),room=Mathf.Min(Radius(low),Radius(top));
            // A straight wall has no interior radial minimum. Its two endpoints
            // already bound the entire interval; scanning all render rings here
            // repeats work for every body at every collision substep.
            if(!straightWall)foreach(var point in profile)if(point.y>low&&point.y<top)room=Mathf.Min(room,Mathf.Max(.0005f,point.x-wall));
            if(new Vector2(position.x,position.z).magnitude+bounds.Radius<=room+tolerance)return true;
        }
        foreach(var local in vertices){var p=position+local;
            if(p.y<floor-tolerance)return false;
            // The mouth is open. A particle above it must be able to leave.
            if(p.y<=height){float radius=Radius(p.y)+tolerance;if(p.x*p.x+p.z*p.z>radius*radius)return false;}
        }
        return true;
    }
    public bool TryResolve(Vector3[] vertices,ref Vector3 position,ref Vector3 velocity){
        if(vertices==null||vertices.Length==0)return false;
        var candidate=position;var resolvedVelocity=velocity;
        for(int iteration=0;iteration<128;iteration++){
            float deepest=0;var normal=Vector3.zero;
            foreach(var local in vertices){
                var p=candidate+local;
                if(floor-p.y>deepest){deepest=floor-p.y;normal=Vector3.up;}
                if(p.y<floor||p.y>height)continue;
                float radius=Radius(p.y),squared=p.x*p.x+p.z*p.z;
                // Interior points cannot be the deepest wall contact. Avoid
                // sqrt and two derivative lookups for every interior vertex.
                if(squared<=radius*radius||squared<1e-16f)continue;
                float d=Mathf.Sqrt(squared);
                float slope=straightWall?(float)radiusSlope:(Radius(p.y+.0005f)-Radius(p.y-.0005f))/.001f;
                float length=Mathf.Sqrt(1+slope*slope),penetration=(d-radius)/length;
                if(penetration>deepest){deepest=penetration;normal=new Vector3(-p.x/d,slope,-p.z/d)/length;}
            }
            if(deepest<=1e-6f){
                if(!Contains(vertices,candidate))return false;
                position=candidate;velocity=resolvedVelocity;return true;
            }
            candidate+=normal*Mathf.Min(.05f,deepest+1e-6f);
            float incoming=Vector3.Dot(resolvedVelocity,normal);
            if(incoming<0)resolvedVelocity-=normal*incoming;
        }
        // An oversized or trapped shape is not forcibly squashed into the cavity.
        // The caller can retain its last valid pose; failed projection is atomic.
        return false;
    }
}
}
