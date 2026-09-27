using System.Collections.Generic;
using UnityEngine;
namespace ChemLab.Game {
// Swept, capped stream for the deliberately force-driven pour presentation.
// This is an animation mesh, not a second liquid solver. Its volume is normalized
// to the transaction's in-flight liquid inventory every frame.
public static class PourStreamMesh {
    public static void Update(Mesh mesh,Vector3[] path,double volumeWorld,float time){
        const int sides=16;int rings=path.Length;
        var verts=new List<Vector3>();var indices=new List<int>();
        var centers=new List<Vector3>();var offsets=new List<Vector3>();
        Vector3 previousRight=Vector3.zero;
        for(int i=0;i<rings;i++){
            Vector3 tangent=(path[Mathf.Min(i+1,rings-1)]-path[Mathf.Max(0,i-1)]).normalized;
            Vector3 axis=Mathf.Abs(tangent.y)>.9f?Vector3.right:Vector3.up;
            Vector3 right=i==0?Vector3.Cross(tangent,axis).normalized:Vector3.ProjectOnPlane(previousRight,tangent).normalized;
            if(right.sqrMagnitude<.5f)right=Vector3.Cross(tangent,axis).normalized;
            previousRight=right;Vector3 up=Vector3.Cross(tangent,right).normalized;
            float q=i/(float)(rings-1);
            // Sin(PI) can round slightly negative; fractional powers then yield NaN.
            float taper=.24f+.76f*Mathf.Pow(Mathf.Clamp01(Mathf.Sin(q*Mathf.PI)),.28f);
            for(int j=0;j<sides;j++){
                float angle=j*Mathf.PI*2/sides;
                float ripple=1+.07f*Mathf.Sin(q*53-time*24+angle*3)+.035f*Mathf.Sin(q*97+time*31-angle*5);
                Vector3 offset=(right*Mathf.Cos(angle)+up*Mathf.Sin(angle))*taper*ripple;
                centers.Add(path[i]);offsets.Add(offset);verts.Add(path[i]+offset);
            }
        }
        for(int i=0;i<rings-1;i++)for(int j=0;j<sides;j++){
            int a=i*sides+j,b=i*sides+(j+1)%sides,c=a+sides,d=b+sides;
            indices.AddRange(new[]{a,b,c,b,d,c});
        }
        int first=verts.Count;verts.Add(path[0]);centers.Add(path[0]);offsets.Add(Vector3.zero);
        int last=verts.Count;verts.Add(path[rings-1]);centers.Add(path[rings-1]);offsets.Add(Vector3.zero);
        for(int j=0;j<sides;j++){int next=(j+1)%sides;indices.AddRange(new[]{first,next,j,last,(rings-1)*sides+j,(rings-1)*sides+next});}
        double SignedVolume(){double v=0;for(int i=0;i<indices.Count;i+=3)v+=Vector3.Dot(verts[indices[i]],Vector3.Cross(verts[indices[i+1]],verts[indices[i+2]]))/6.0;return v;}
        double raw=SignedVolume();float scale=(float)System.Math.Sqrt(volumeWorld/System.Math.Max(1e-12,System.Math.Abs(raw)));
        for(int i=0;i<verts.Count;i++)verts[i]=centers[i]+offsets[i]*scale;
        // Curved tapered rings introduce a small higher-order volume term.
        // Correct radius iteratively rather than changing path length or mass.
        for(int pass=0;pass<3;pass++){
            double actual=System.Math.Abs(SignedVolume());float correction=(float)System.Math.Sqrt(volumeWorld/System.Math.Max(1e-12,actual));
            for(int i=0;i<verts.Count;i++)verts[i]=centers[i]+(verts[i]-centers[i])*correction;
        }
        if(SignedVolume()<0)for(int i=0;i<indices.Count;i+=3){int swap=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=swap;}
        mesh.Clear();mesh.SetVertices(verts);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
    }
}
}
