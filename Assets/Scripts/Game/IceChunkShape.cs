using System;
using System.Collections.Generic;
using UnityEngine;
namespace ChemLab.Game {
// Closed beveled cube, bounded by unit radius. Volume is measured from the
// triangles so rounded edges cannot silently create or destroy ice inventory.
public static class IceChunkShape {
    public static readonly Vector3[] Vertices;
    public static readonly int[] Faces;
    public static readonly double Volume;
    static IceChunkShape(){
        var vertices=new List<Vector3>();var faces=new List<int>();const int steps=6;
        foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}){
            Vector3 u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up,v=Vector3.Cross(normal,u);
            int start=vertices.Count;
            for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++){
                Vector3 p=normal+u*(2f*x/steps-1)+v*(2f*y/steps-1);
                var inset=new Vector3(Mathf.Clamp(p.x,-.78f,.78f),Mathf.Clamp(p.y,-.78f,.78f),Mathf.Clamp(p.z,-.78f,.78f));
                var rounded=(inset+.22f*(p-inset).normalized)/Mathf.Sqrt(3);
                // Continuous across face seams: irregular melt pits and bulges,
                // not independently displaced triangles with cracks between them.
                float warp=.955f+.025f*Mathf.Cos(p.x*3.1f+p.y*1.7f)*Mathf.Cos(p.z*2.7f-p.y*1.3f)
                    +.018f*Mathf.Cos(p.x*5.3f-p.z*2.2f+p.y*.8f);
                vertices.Add(rounded*warp);
            }
            for(int y=0;y<steps;y++)for(int x=0;x<steps;x++){
                int a=start+y*(steps+1)+x,b=a+1,c=a+steps+1,d=c+1;
                faces.AddRange(new[]{a,b,d,a,d,c});
            }
        }
        Vertices=vertices.ToArray();Faces=faces.ToArray();double volume=0;
        for(int i=0;i<Faces.Length;i+=3)volume+=Vector3.Dot(Vertices[Faces[i]],Vector3.Cross(Vertices[Faces[i+1]],Vertices[Faces[i+2]]))/6;
        Volume=Math.Abs(volume);
    }
}
}
