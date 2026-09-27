using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using ChemLab.Game;
namespace ChemLab.Editor {
public static class PaperAudit {
    public static void Run(){
        var mesh=TestItemMesh.BuildLitmus();var p=mesh.vertices;var t=mesh.triangles;
        var lines=new List<string>();int failed=0;
        void Check(string name,bool ok){lines.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failed++;}
        int layer=p.Length/2;bool separated=true;
        for(int i=0;i<layer;i++)separated&=Math.Abs(p[i+layer].z-p[i].z-.001f)<1e-8&&p[i].x==p[i+layer].x&&p[i].y==p[i+layer].y;
        Check("front/back same mid-surface with positive 0.1 mm separation",separated);
        var edges=new Dictionary<(int,int),int>();var orientation=new Dictionary<(int,int),int>();double volume=0;bool nondegenerate=true;
        for(int i=0;i<t.Length;i+=3){
            var a=p[t[i]];var b=p[t[i+1]];var c=p[t[i+2]];
            volume+=Vector3.Dot(a,Vector3.Cross(b,c))/6.0;
            nondegenerate&=Vector3.Cross(b-a,c-a).sqrMagnitude>1e-16;
            for(int j=0;j<3;j++){
                int u=t[i+j],v=t[i+(j+1)%3];var key=(Math.Min(u,v),Math.Max(u,v));
                edges.TryGetValue(key,out int count);edges[key]=count+1;
                orientation.TryGetValue(key,out int direction);orientation[key]=direction+(u<v?1:-1);
            }
        }
        Check("closed manifold including four edges",edges.Values.All(n=>n==2));
        Check("consistent outward face winding",orientation.Values.All(n=>n==0)&&volume>0);
        Check("no degenerate faces",nondegenerate);
        Check("paper volume matches width height thickness",Math.Abs(volume-.055*.34*.001)<1e-10);
        UnityEngine.Object.DestroyImmediate(mesh);
        lines.Add("TOTAL "+(lines.Count-failed)+" passed, "+failed+" failed");
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/paper-audit.txt",lines);EditorApplication.Exit(failed==0?0:1);
    }
}
}
