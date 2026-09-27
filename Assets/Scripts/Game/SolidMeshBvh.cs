using System;
using System.Collections.Generic;
using UnityEngine;
namespace ChemLab.Game {
// Stackless binary BVHs. Leaves reference the existing body/triangle buffers;
// no remeshing, convex approximation, or visual-volume changes are introduced.
public sealed class SolidMeshBvh {
    public struct Node {public Vector4 MinEscape,MaxLeaf;}
    struct Item {public Vector3 Min,Max;public int Leaf;}
    sealed class AxisComparer:IComparer<Item> {
        readonly int axis;public AxisComparer(int axis){this.axis=axis;}
        public int Compare(Item a,Item b){int result=(a.Min[axis]+a.Max[axis]).CompareTo(b.Min[axis]+b.Max[axis]);return result!=0?result:a.Leaf.CompareTo(b.Leaf);}
    }
    static readonly IComparer<Item>[] comparers={new AxisComparer(0),new AxisComparer(1),new AxisComparer(2)};
    public Node[] Bodies {get;private set;}
    public Node[] Triangles {get;private set;}
    public Vector2Int[] TriangleRanges {get;private set;}
    readonly Vector3[] localMin,localMax;
    public SolidMeshBvh(IReadOnlyList<SolidBodySeed> seeds){
        localMin=new Vector3[seeds.Count];localMax=new Vector3[seeds.Count];TriangleRanges=new Vector2Int[seeds.Count];
        var bodyItems=new Item[seeds.Count];var triangleNodes=new List<Node>();int firstVertex=0;
        for(int body=0;body<seeds.Count;body++){
            var seed=seeds[body];var min=Vector3.one*float.PositiveInfinity;var max=-min;
            foreach(var p in seed.LocalVertices){min=Vector3.Min(min,p);max=Vector3.Max(max,p);}
            localMin[body]=min;localMax[body]=max;bodyItems[body]=new Item{Min=min+seed.Center,Max=max+seed.Center,Leaf=body};
            var items=new Item[seed.Triangles.Length/3];
            for(int triangle=0;triangle<items.Length;triangle++){
                var a=seed.LocalVertices[seed.Triangles[triangle*3]];var b=seed.LocalVertices[seed.Triangles[triangle*3+1]];var c=seed.LocalVertices[seed.Triangles[triangle*3+2]];
                items[triangle]=new Item{Min=Vector3.Min(a,Vector3.Min(b,c)),Max=Vector3.Max(a,Vector3.Max(b,c)),Leaf=firstVertex+triangle*3};
            }
            int root=triangleNodes.Count;if(items.Length>0)Build(items,0,items.Length,triangleNodes);
            TriangleRanges[body]=new Vector2Int(root,triangleNodes.Count);firstVertex+=seed.Triangles.Length;
        }
        var bodyNodes=new List<Node>();if(bodyItems.Length>0)Build(bodyItems,0,bodyItems.Length,bodyNodes);
        Bodies=bodyNodes.ToArray();Triangles=triangleNodes.ToArray();
    }
    static void Build(Item[] items,int start,int count,List<Node> nodes){
        var min=items[start].Min;var max=items[start].Max;
        for(int i=start+1;i<start+count;i++){min=Vector3.Min(min,items[i].Min);max=Vector3.Max(max,items[i].Max);}
        int node=nodes.Count;nodes.Add(default);
        if(count>1){
            var extent=max-min;int axis=extent.x>=extent.y&&extent.x>=extent.z?0:extent.y>=extent.z?1:2;
            Array.Sort(items,start,count,comparers[axis]);int leftCount=count/2;
            Build(items,start,leftCount,nodes);Build(items,start+leftCount,count-leftCount,nodes);
        }
        nodes[node]=new Node{MinEscape=new Vector4(min.x,min.y,min.z,nodes.Count),MaxLeaf=new Vector4(max.x,max.y,max.z,count==1?items[start].Leaf:-1)};
    }
    public void RefitBodies(Vector3[] positions){
        if(positions.Length!=localMin.Length)throw new ArgumentException("BVH body pose count differs");
        for(int i=Bodies.Length-1;i>=0;i--){
            var node=Bodies[i];int leaf=(int)node.MaxLeaf.w;Vector3 min,max;
            if(leaf>=0){min=localMin[leaf]+positions[leaf];max=localMax[leaf]+positions[leaf];}
            else{int left=i+1,right=(int)Bodies[left].MinEscape.w;min=Vector3.Min(Bodies[left].MinEscape,Bodies[right].MinEscape);max=Vector3.Max(Bodies[left].MaxLeaf,Bodies[right].MaxLeaf);}
            node.MinEscape=new Vector4(min.x,min.y,min.z,node.MinEscape.w);node.MaxLeaf=new Vector4(max.x,max.y,max.z,node.MaxLeaf.w);Bodies[i]=node;
        }
    }
}
}
