using UnityEngine;
namespace ChemLab.Game {
// Conservative oriented envelope for placement only. All original shape points
// are enclosed; a separated envelope guarantees separated visible solids.
public sealed class SolidPackingBox {
    readonly Vector3 x,y,z,half,offset,worldExtents;
    readonly Vector3[] originalPoints;
    public SolidPackingBox(Vector3[] vertices,Vector3 scale,Quaternion rotation){
        var min=Vector3.one*float.PositiveInfinity;var max=-min;
        foreach(var v in vertices){var p=Vector3.Scale(v,scale);min=Vector3.Min(min,p);max=Vector3.Max(max,p);}
        half=(max-min)*.5f;offset=rotation*((max+min)*.5f);
        originalPoints=new Vector3[vertices.Length];
        for(int i=0;i<vertices.Length;i++)originalPoints[i]=rotation*Vector3.Scale(vertices[i],scale)-offset;
        x=rotation*Vector3.right;y=rotation*Vector3.up;z=rotation*Vector3.forward;
        worldExtents=new Vector3(Support(Vector3.right),Support(Vector3.up),Support(Vector3.forward));
    }
    float Support(Vector3 axis)=>Mathf.Abs(Vector3.Dot(x,axis))*half.x+Mathf.Abs(Vector3.Dot(y,axis))*half.y+Mathf.Abs(Vector3.Dot(z,axis))*half.z;
    bool ClipAxis(SolidPackingBox other,Vector3 difference,Vector3 motion,Vector3 axis,ref float enter,ref float leave){
        if(axis.sqrMagnitude<1e-12f)return true;
        float radius=Support(axis)+other.Support(axis),d=Vector3.Dot(difference,axis),speed=Vector3.Dot(motion,axis);
        if(Mathf.Abs(speed)<1e-12f)return Mathf.Abs(d)<radius;
        float lo=(-radius-d)/speed,hi=(radius-d)/speed;if(lo>hi){float swap=lo;lo=hi;hi=swap;}
        enter=Mathf.Max(enter,lo);leave=Mathf.Min(leave,hi);return enter<=leave;
    }
    // Continuous translation against another fixed-orientation envelope. This
    // settles on the first support crossed, rather than teleporting through it.
    public bool Sweep(Vector3 position,SolidPackingBox other,Vector3 otherPosition,Vector3 motion,out float contact){
        float enter=0,leave=1;var d=position+offset-otherPosition-other.offset;
        if(!ClipAxis(other,d,motion,x,ref enter,ref leave)||!ClipAxis(other,d,motion,y,ref enter,ref leave)||!ClipAxis(other,d,motion,z,ref enter,ref leave)
          ||!ClipAxis(other,d,motion,other.x,ref enter,ref leave)||!ClipAxis(other,d,motion,other.y,ref enter,ref leave)||!ClipAxis(other,d,motion,other.z,ref enter,ref leave)
          ||!ClipAxis(other,d,motion,Vector3.Cross(x,other.x),ref enter,ref leave)||!ClipAxis(other,d,motion,Vector3.Cross(x,other.y),ref enter,ref leave)||!ClipAxis(other,d,motion,Vector3.Cross(x,other.z),ref enter,ref leave)
          ||!ClipAxis(other,d,motion,Vector3.Cross(y,other.x),ref enter,ref leave)||!ClipAxis(other,d,motion,Vector3.Cross(y,other.y),ref enter,ref leave)||!ClipAxis(other,d,motion,Vector3.Cross(y,other.z),ref enter,ref leave)
          ||!ClipAxis(other,d,motion,Vector3.Cross(z,other.x),ref enter,ref leave)||!ClipAxis(other,d,motion,Vector3.Cross(z,other.y),ref enter,ref leave)||!ClipAxis(other,d,motion,Vector3.Cross(z,other.z),ref enter,ref leave)){contact=1;return false;}
        contact=enter;return true;
    }
    public bool Overlaps(Vector3 position,SolidPackingBox other,Vector3 otherPosition)=>Sweep(position,other,otherPosition,Vector3.zero,out _);
    public bool MayOverlapVertical(Vector3 position,SolidPackingBox other,Vector3 otherPosition){
        var difference=position+offset-otherPosition-other.offset;var limits=worldExtents+other.worldExtents;
        return Mathf.Abs(difference.x)<=limits.x&&Mathf.Abs(difference.z)<=limits.z;
    }
    public Pair Prepare(SolidPackingBox other)=>new Pair(this,other);
    public Pair PrepareTight(SolidPackingBox other)=>new Pair(this,other,true);
    void OriginalRange(Vector3 axis,out float min,out float max){
        min=float.PositiveInfinity;max=float.NegativeInfinity;
        for(int i=0;i<originalPoints.Length;i++){
            var point=originalPoints[i];float projection=point.x*axis.x+point.y*axis.y+point.z*axis.z;
            if(projection<min)min=projection;if(projection>max)max=projection;
        }
    }
    // A horizontal separating support plane gives a repeatable placement
    // candidate beside an existing envelope, instead of hoping random points
    // discover its narrow free neighborhood. No shape is shrunk or rotated.
    public Vector3 TouchingOffset(SolidPackingBox other,Vector3 direction){
        direction.Normalize();
        return other.offset-offset+direction*(Support(direction)+other.Support(direction)+.000002f);
    }
    public Vector3 AdjacentOffset(SolidPackingBox other,int candidate){
        var box=candidate<4?this:other;
        var direction=(candidate%4)<2?box.x:box.z;
        direction.y=0;if((candidate&1)!=0)direction=-direction;
        return TouchingOffset(other,direction);
    }
    // Pair data is invariant while trying many candidate positions. Keep the
    // uncached implementation above as an independent arithmetic reference.
    public sealed class Pair {
        struct Plane {public Vector3 Axis;public float Lower,Upper;}
        readonly Plane[] planes=new Plane[15];
        readonly Vector3 offsetA,offsetB;
        readonly int count;
        public Pair(SolidPackingBox a,SolidPackingBox b,bool originalSupport=false){
            offsetA=a.offset;offsetB=b.offset;
            var axes=new[]{a.x,a.y,a.z,b.x,b.y,b.z,Vector3.Cross(a.x,b.x),Vector3.Cross(a.x,b.y),Vector3.Cross(a.x,b.z),Vector3.Cross(a.y,b.x),Vector3.Cross(a.y,b.y),Vector3.Cross(a.y,b.z),Vector3.Cross(a.z,b.x),Vector3.Cross(a.z,b.y),Vector3.Cross(a.z,b.z)};
            int next=0;for(int axisIndex=0;axisIndex<axes.Length;axisIndex++){
                var axis=axes[axisIndex];if(axis.sqrMagnitude<1e-12f)continue;
                float lower,upper;
                if(originalSupport){
                    // Original-vertex support intervals are tighter than a box,
                    // but still enclose the whole visible shape. A separating
                    // interval proves no collision; overlapping intervals may
                    // remain conservative because this is not a full hull SAT.
                    a.OriginalRange(axis,out float aMin,out float aMax);
                    b.OriginalRange(axis,out float bMin,out float bMax);
                    // Include a 0.01 mm contact allowance for independently
                    // cooked collision hulls; visible geometry stays unchanged.
                    float pad=axis.magnitude*.0001f;
                    lower=bMin-aMax-pad;upper=bMax-aMin+pad;
                }else{upper=a.Support(axis)+b.Support(axis);lower=-upper;}
                planes[next++]=new Plane{Axis=axis,Lower=lower,Upper=upper};
            }
            count=next;
        }
        public bool Overlaps(Vector3 position,Vector3 otherPosition){
            var difference=position+offsetA-otherPosition-offsetB;
            for(int i=0;i<count;i++){var plane=planes[i];float distance=Vector3.Dot(difference,plane.Axis);if(distance<=plane.Lower||distance>=plane.Upper)return false;}
            return true;
        }
        public bool Sweep(Vector3 position,Vector3 otherPosition,Vector3 motion,out float contact){
            var difference=position+offsetA-otherPosition-offsetB;float enter=0,leave=1;
            for(int i=0;i<count;i++){
                var plane=planes[i];float d=Vector3.Dot(difference,plane.Axis),speed=Vector3.Dot(motion,plane.Axis);
                if(Mathf.Abs(speed)<1e-12f){if(d<=plane.Lower||d>=plane.Upper){contact=1;return false;}continue;}
                float lo=(plane.Lower-d)/speed,hi=(plane.Upper-d)/speed;if(lo>hi){float swap=lo;lo=hi;hi=swap;}
                enter=Mathf.Max(enter,lo);leave=Mathf.Min(leave,hi);if(enter>leave){contact=1;return false;}
            }
            contact=enter;return true;
        }
    }
}
}
