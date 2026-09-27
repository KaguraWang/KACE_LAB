using UnityEngine;
namespace ChemLab.Game {
// Produced together with the visible mesh, so later motion and fluid collision
// cannot silently use a differently sized or differently oriented solid.
public sealed class SolidBodySeed {
    public string SubstanceId;
    public bool IsPrecipitate,IsSpilled,IsStatic;
    public int FirstVertex,VertexCount;
    public Vector3 Center;
    public Vector3[] LocalVertices;
    public int[] Triangles;
    public double VolumeWorldCubed,MassGrams;
    public SubmergedSolidShape ImmersionShape()=>new SubmergedSolidShape(LocalVertices,Triangles,Matrix4x4.identity);
}
}
