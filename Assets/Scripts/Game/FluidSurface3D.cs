using System;
using System.Collections.Generic;
using UnityEngine;
namespace ChemLab.Game {
// A volumetric density grid reconstructed with marching tetrahedra. No height field:
// surfaces can contain overhangs and disconnected components.
public sealed class FluidSurface3D {
    readonly List<Vector3> verts=new List<Vector3>(),normals=new List<Vector3>();
    readonly List<int> triangles=new List<int>();
    float[] field,boundary;Vector3[] gradient;
    int nx,ny,nz;float cell,iso;Vector3 origin;
    static readonly int[,] tetra={{0,5,1,6},{0,1,2,6},{0,2,3,6},{0,3,7,6},{0,7,4,6},{0,4,5,6}};
    static readonly Vector3Int[] corners={new Vector3Int(0,0,0),new Vector3Int(1,0,0),new Vector3Int(1,1,0),new Vector3Int(0,1,0),new Vector3Int(0,0,1),new Vector3Int(1,0,1),new Vector3Int(1,1,1),new Vector3Int(0,1,1)};
    readonly Vector3[] cp=new Vector3[8],cn=new Vector3[8];readonly float[] cf=new float[8];
    readonly int[] inside=new int[4],outside=new int[4];
    public double MeshVolumeMl {get;private set;}
    int Id(int x,int y,int z)=>(z*ny+y)*nx+x;
    public void Rebuild(Fluid3D fluid,Mesh mesh) {
        Compute(fluid);Upload(mesh);
    }
    public void Compute(Fluid3D fluid) {
        if(fluid.Positions.Length==0){verts.Clear();normals.Clear();triangles.Clear();return;}
        cell=Mathf.Max(0.009f,fluid.Radius/18);
        origin=new Vector3(-fluid.Radius-cell*2,-cell*2,-fluid.Radius-cell*2);
        float kernel=Mathf.Max(fluid.Spacing*2.15f,cell*2),k2=kernel*kernel;
        float activeHeight=0;foreach(var p in fluid.Positions)activeHeight=Mathf.Max(activeHeight,p.y);
        activeHeight=Mathf.Min(fluid.Height,activeHeight+kernel);
        nx=Mathf.CeilToInt((fluid.Radius*2+cell*4)/cell)+1;nz=nx;ny=Mathf.CeilToInt((activeHeight+cell*4)/cell)+1;
        int count=nx*ny*nz;if(field==null||field.Length!=count){field=new float[count];boundary=new float[count];gradient=new Vector3[count];}else Array.Clear(field,0,count);
        for(int z=0;z<nz;z++)for(int y=0;y<ny;y++)for(int x=0;x<nx;x++)boundary[Id(x,y,z)]=fluid.BoundaryDistance(origin+new Vector3(x,y,z)*cell)/cell;
        float weight=315f/(64*Mathf.PI*kernel*kernel*kernel)*(float)fluid.VolumeMl/1000/fluid.Positions.Length;
        foreach(var p in fluid.Positions) {
            var a=(p-origin)/cell;int reach=Mathf.CeilToInt(kernel/cell);
            for(int z=Mathf.Max(0,(int)a.z-reach);z<=Mathf.Min(nz-1,(int)a.z+reach);z++)
            for(int y=Mathf.Max(0,(int)a.y-reach);y<=Mathf.Min(ny-1,(int)a.y+reach);y++)
            for(int x=Mathf.Max(0,(int)a.x-reach);x<=Mathf.Min(nx-1,(int)a.x+reach);x++) {
                float q=1-(origin+new Vector3(x,y,z)*cell-p).sqrMagnitude/k2;
                if(q>0)field[Id(x,y,z)]+=weight*q*q*q;
            }
        }
        // Enforce the physical profile in the scalar field, not fragment clipping of
        // an already coincident wall mesh (which produced stripes and missing walls).
        float target=(float)fluid.VolumeMl/1000;
        float low=0.02f,high=1.5f;
        for(int it=0;it<13;it++) {
            float threshold=(low+high)*0.5f,volume=0;
            for(int id=0;id<count;id++) {
                float f=field[id];if(f<0.005f)continue;
                float wall=boundary[id];
                volume+=Mathf.Min(Mathf.Clamp01((f-threshold)/0.35f+0.5f),Mathf.Clamp01(wall+0.5f));
            }
            if(volume*cell*cell*cell>target)low=threshold;else high=threshold;
        }
        iso=(low+high)*0.5f;
        for(int z=0;z<nz;z++)for(int y=0;y<ny;y++)for(int x=0;x<nx;x++) {
            int id=Id(x,y,z);field[id]=Mathf.Min(field[id]-iso,boundary[id]*0.35f);
        }
        for(int z=1;z<nz-1;z++)for(int y=1;y<ny-1;y++)for(int x=1;x<nx-1;x++)
            gradient[Id(x,y,z)]=-new Vector3(field[Id(x+1,y,z)]-field[Id(x-1,y,z)],field[Id(x,y+1,z)]-field[Id(x,y-1,z)],field[Id(x,y,z+1)]-field[Id(x,y,z-1)]).normalized;
        verts.Clear();normals.Clear();triangles.Clear();MeshVolumeMl=0;
        for(int z=0;z<nz-1;z++)for(int y=0;y<ny-1;y++)for(int x=0;x<nx-1;x++) {
            int positive=0;
            for(int c=0;c<8;c++){var q=corners[c];cf[c]=field[Id(q.x+x,q.y+y,q.z+z)];if(cf[c]>0)positive++;}
            if(positive==0||positive==8)continue;
            for(int c=0;c<8;c++){var q=corners[c]+new Vector3Int(x,y,z);int id=Id(q.x,q.y,q.z);cp[c]=origin+(Vector3)q*cell;cn[c]=gradient[id];}
            for(int t=0;t<6;t++) {
                int ni=0,no=0;
                for(int k=0;k<4;k++){int c=tetra[t,k];if(cf[c]>0)inside[ni++]=c;else outside[no++]=c;}
                if(ni==0||ni==4)continue;
                if(ni==1){Edge(inside[0],outside[0],out var a,out var an);Edge(inside[0],outside[1],out var b,out var bn);Edge(inside[0],outside[2],out var c,out var cn0);Triangle(a,b,c,an,bn,cn0);}
                else if(ni==3){Edge(outside[0],inside[0],out var a,out var an);Edge(outside[0],inside[1],out var b,out var bn);Edge(outside[0],inside[2],out var c,out var cn0);Triangle(a,b,c,an,bn,cn0);}
                else {Edge(inside[0],outside[0],out var a,out var an);Edge(inside[0],outside[1],out var b,out var bn);Edge(inside[1],outside[0],out var c,out var cn0);Edge(inside[1],outside[1],out var d,out var dn);Triangle(a,b,c,an,bn,cn0);Triangle(b,d,c,bn,dn,cn0);}
            }
        }
        MeshVolumeMl=Math.Abs(MeshVolumeMl)*1000;
    }
    public void Upload(Mesh mesh){mesh.Clear();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();}
    void Edge(int a,int b,out Vector3 p,out Vector3 normal){float t=cf[a]/(cf[a]-cf[b]);p=Vector3.Lerp(cp[a],cp[b],t);normal=Vector3.Lerp(cn[a],cn[b],t).normalized;}
    void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector3 an,Vector3 bn,Vector3 cn0){
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),an+bn+cn0)<0){var p=b;b=c;c=p;var n=bn;bn=cn0;cn0=n;}
        int start=verts.Count;verts.Add(a);verts.Add(b);verts.Add(c);normals.Add(an);normals.Add(bn);normals.Add(cn0);triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
        MeshVolumeMl+=Vector3.Dot(a,Vector3.Cross(b,c))/6.0;
    }
}
}
