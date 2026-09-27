using System;
using System.Collections.Generic;
using UnityEngine;
using ChemLab.Chem;
namespace ChemLab.Game {
public static class SolidGeometry {
    public static double VolumeMl(Vessel v) => v.SolidVolume();
    public static int Signature(Vessel v) {
        unchecked {int h=17;foreach(var set in new[]{v.Undissolved,v.Precipitates}) {
            h=h*31+7919;foreach(var kv in set){
                h=h*31+kv.Key.GetHashCode();h=h*31+kv.Value.GetHashCode();
                // A temperature edit can melt a stored deposit before the solver
                // moves its inventory. Cache geometry by the displayed phase and
                // solid density, without rebuilding for every unchanged degree.
                string phase=PhaseProperties.Phase(kv.Key,v.Temp);
                h=h*31+phase.GetHashCode();
                if(phase=="s")h=h*31+PhaseProperties.Density(kv.Key,v.Temp).GetHashCode();
            }
        }return h;}
    }
    // 每粒使用封闭多面体的实际体积定标；空隙不计入物质体积。
    public static Mesh Build(Vessel v) => Build(v,null);
    public static Mesh Build(Vessel v,List<SolidBodySeed> bodySeeds,IReadOnlyList<SolidBodySeed> previous=null) => BuildCore(v,bodySeeds,previous,0);
    internal static Mesh BuildForPackingAudit(Vessel v,List<SolidBodySeed> bodySeeds,int searchSalt) => BuildCore(v,bodySeeds,null,searchSalt);
    static Mesh BuildCore(Vessel v,List<SolidBodySeed> bodySeeds,IReadOnlyList<SolidBodySeed> previous,int searchSalt) {
        bodySeeds?.Clear();
        var verts=new List<Vector3>();var tris=new List<int>();var colors=new List<Color>();var uv=new List<Vector2>();
        var centers=new List<Vector3>();var sizes=new List<float>();
        var packedExtents=new List<Vector3>();
        var packedBoxes=new List<SolidPackingBox>();
        var packedTops=new List<float>();
        var g=VesselMesh.Get(v.Kind);var profile=VesselMesh.Profile(g.Type,g.R,g.H);
        float wall=Mathf.Max(0.012f,g.R*0.052f);
        float floor=Mathf.Max(wall,g.Type==3?.04f:0);
        var packingBoundary=new SolidVesselBoundary(v.Kind);
        var rng=new System.Random(v.Id.GetHashCode());
        float Rand()=> (float)rng.NextDouble();
        // Placement effort must not change the size, orientation or colour of
        // the next chunk. A separate stream also allows fixed-geometry audits
        // of different searches instead of accidentally comparing different ice.
        var placementRng=new System.Random(unchecked(v.Id.GetHashCode()^0x5317a2^searchSalt));
        float PlacementRand()=> (float)placementRng.NextDouble();
        double powderBelow=0;float powderTop=0;
        // Reserve the complete fine-powder bed before packing coarse particles.
        // Dictionary insertion order must not put later powder through earlier grains.
        for(int pass=0;pass<2;pass++)
        foreach(var set in new[]{v.Undissolved,v.Precipitates}) foreach(var kv in set) {
            if(!ChemData.Sub.TryGetValue(kv.Key,out var s)||kv.Value<=0||PhaseProperties.Phase(kv.Key,v.Temp)!="s") continue;
            bool precip=ReferenceEquals(set,v.Precipitates);
            bool ice=kv.Key==WaterIce.Id;
            bool floc=precip && (s.Formula.Contains("(OH)")||(s.Note??"").Contains("絮")||(s.Note??"").Contains("胶"));
            bool powder=!ice&&(precip||s.Form=="powder"||s.Form=="frozen");
            if((powder&&!floc)!=(pass==0))continue;
            double vol=kv.Value*s.Mass/Math.Max(0.01,PhaseProperties.Density(kv.Key,v.Temp))/1000;
            int count=Mathf.Clamp((int)(vol*1000/(powder?0.002:0.25)),powder?80:1,powder?1200:80);
            if(ice)count=Mathf.Clamp(Mathf.CeilToInt((float)(vol*1000)),8,160);
            // During dissolution/melting keep each surviving body's identity.
            // Re-counting by total mL would periodically delete and enlarge chunks.
            if(previous!=null){
                int oldCount=0;double oldVolume=0;
                foreach(var old in previous)if(old.SubstanceId==kv.Key&&old.IsPrecipitate==precip){oldCount++;oldVolume+=old.VolumeWorldCubed;}
                if(oldCount>0&&vol<=oldVolume*(1+1e-8))count=oldCount;
            }
            var shapeVertices=ice?IceChunkShape.Vertices:GrainShape.Vertices;
            var shapeFaces=ice?IceChunkShape.Faces:GrainShape.Faces;
            double shapeVolume=ice?IceChunkShape.Volume:GrainShape.Volume;
            Color color=Color.gray;
            string hex=s.SolidColor;
            if(precip&&ChemData.Precip.TryGetValue(s.Formula,out var pi)&&!string.IsNullOrEmpty(pi.Color)) hex=pi.Color;
            ColorUtility.TryParseHtmlString(string.IsNullOrEmpty(hex)?"#dddddd":hex,out color);
            if(ice)color=new Color(.72f,.88f,.94f);
            if(powder && !floc) {
                int bedIndex=bodySeeds?.Count??0;
                var bed=PowderBed.Build(v.Kind,vol*1000,powderBelow,bodySeeds);powderBelow+=vol*1000;
                if(bodySeeds!=null&&bodySeeds.Count>bedIndex){
                    var seed=bodySeeds[bedIndex];seed.SubstanceId=kv.Key;seed.IsPrecipitate=precip;
                    seed.MassGrams=kv.Value*s.Mass*(seed.VolumeWorldCubed/vol);
                }
                powderTop=Mathf.Max(powderTop,Mathf.Min(g.H,bed.bounds.max.y));
                int offset=verts.Count;
                foreach(var point in bed.vertices){verts.Add(point);colors.Add(color);uv.Add(new Vector2(s.Cls=="metal"?0.65f:0,0.02f));}
                foreach(int index in bed.triangles)tris.Add(offset+index);
                if(Application.isPlaying)UnityEngine.Object.Destroy(bed);else UnityEngine.Object.DestroyImmediate(bed);
                continue;
            }
            for(int j=0;j<count;j++) {
                Vector3 scale=floc?new Vector3(1.5f,0.35f,1.2f):powder?new Vector3(1,0.8f,1):new Vector3(0.75f+Rand()*0.5f,0.7f+Rand()*0.5f,0.75f+Rand()*0.5f);
                float r=(float)Math.Pow(vol/count/(shapeVolume*scale.x*scale.y*scale.z),1.0/3);
                var rotation=ice?Quaternion.Euler(Rand()*14-7,Rand()*360,Rand()*14-7):floc?Quaternion.Euler(0,Rand()*360,0):Quaternion.Euler(Rand()*360,Rand()*360,Rand()*360);
                var packingBox=new SolidPackingBox(shapeVertices,scale*r,rotation);
                var packingPairs=ice?new SolidPackingBox.Pair[packedBoxes.Count]:null;
                float bound=r*Mathf.Max(scale.x,Mathf.Max(scale.y,scale.z));
                float lowestOffset=0,highestOffset=0;
                Vector3 extent=Vector3.zero;float horizontal=0;
                var packingVertices=new Vector3[shapeVertices.Length];
                for(int vertexIndex=0;vertexIndex<shapeVertices.Length;vertexIndex++){
                    var vertex=shapeVertices[vertexIndex];
                    var offset=rotation*Vector3.Scale(vertex*r,scale);
                    packingVertices[vertexIndex]=offset;
                    lowestOffset=Mathf.Min(lowestOffset,offset.y);
                    highestOffset=Mathf.Max(highestOffset,offset.y);
                    extent=Vector3.Max(extent,new Vector3(Mathf.Abs(offset.x),Mathf.Abs(offset.y),Mathf.Abs(offset.z)));
                    horizontal=Mathf.Max(horizontal,new Vector2(offset.x,offset.z).magnitude);
                }
                float angle=PlacementRand()*Mathf.PI*2, radial=Mathf.Sqrt(PlacementRand())*Mathf.Max(0,g.R-wall-bound)*0.72f;
                var p=new Vector3(Mathf.Cos(angle)*radial,wall+r*scale.y,Mathf.Sin(angle)*radial);
                // 从底部向上找空位，逐层依照内腔收缩；不能把窄颈外的点一路推到瓶口外。
                bool placed=false;
                // Bounding spheres leave a visible air gap under irregular grains.
                // Anchor the lowest actual vertex to the support surface instead.
                var levels=new List<float>();float firstY=Mathf.Max(floor,powderTop)-lowestOffset;
                for(float y=firstY;y<g.H-highestOffset;y+=Mathf.Max(.003f,bound*.7f))levels.Add(y);
                if(ice){foreach(float top in packedTops){float y=top-lowestOffset+.000002f;if(y>=firstY&&y<g.H-highestOffset)levels.Add(y);}levels.Sort();}
                var neighbors=new List<int>();
                foreach(float level in levels) {
                    if(placed)break;
                    float y=level;
                    float vertical=ice?extent.y:bound,radialBound=ice?horizontal:bound;
                    float room=Mathf.Min(VesselMesh.RadiusAt(profile,Mathf.Max(0,y-vertical)/g.H),VesselMesh.RadiusAt(profile,(y+vertical)/g.H))-wall-(ice?0:radialBound);
                    if(room<=0) continue;
                    if(ice){neighbors.Clear();for(int k=0;k<centers.Count;k++)if(Mathf.Abs(y-centers[k].y)<extent.y+packedExtents[k].y)neighbors.Add(k);}
                    for(int attempt=0;attempt<24;attempt++) {
                        float theta=PlacementRand()*Mathf.PI*2,rr=Mathf.Sqrt(PlacementRand())*room;
                        var candidate=new Vector3(Mathf.Cos(theta)*rr,y,Mathf.Sin(theta)*rr);
                        bool free=true;
                        for(int neighbor=0;neighbor<(ice?neighbors.Count:centers.Count);neighbor++){
                            int k=ice?neighbors[neighbor]:neighbor;
                            var delta=candidate-centers[k];var limits=extent+packedExtents[k];
                            bool intersects=ice?Mathf.Abs(delta.x)<limits.x&&Mathf.Abs(delta.y)<limits.y&&Mathf.Abs(delta.z)<limits.z:
                                delta.sqrMagnitude<(bound+sizes[k])*(bound+sizes[k]);
                            if(ice&&intersects){if(packingPairs[k]==null)packingPairs[k]=packingBox.PrepareTight(packedBoxes[k]);intersects=packingPairs[k].Overlaps(candidate,centers[k]);}
                            if(intersects){free=false;break;}
                        }
                        if(free){
                            if(ice&&!packingBoundary.Contains(packingVertices,candidate))continue;
                            // Search levels are only a packing aid, not physical supports.
                            // Settle ice onto the floor or an underlying chunk instead of
                            // leaving it suspended at the first collision-free scan level.
                            if(ice){
                                float floorY=Mathf.Max(floor,powderTop)-lowestOffset;
                                var fall=Vector3.down*Mathf.Max(0,candidate.y-floorY);float fraction=1;
                                for(int k=0;k<centers.Count;k++){
                                    if(!packingBox.MayOverlapVertical(candidate,packedBoxes[k],centers[k]))continue;
                                    if(packingPairs[k]==null)packingPairs[k]=packingBox.PrepareTight(packedBoxes[k]);
                                    if(packingPairs[k].Sweep(candidate,centers[k],fall,out float hit))fraction=Mathf.Min(fraction,hit);
                                }
                                float settledY=candidate.y+fall.y*fraction;
                                if(fraction<1)settledY+=.000002f;
                                // A curved vessel may narrow below the search position.
                                var settled=new Vector3(candidate.x,settledY,candidate.z);
                                var settlingVelocity=Vector3.zero;
                                if(packingBoundary.ConstrainStep(packingVertices,candidate,ref settled,ref settlingVelocity))candidate=settled;
                            }
                            p=candidate;placed=true;break;
                        }
                    }
                }
                // 旧存档超量时将溢出物放在台面；绝不能沿 Y 轴生成悬空通天柱。
                if(!placed) {
                    float spillAngle=j*2.399963f;
                    float spillRadius=g.R+bound*2+Mathf.Sqrt(j+1)*bound*2;
                    p=new Vector3(Mathf.Cos(spillAngle)*spillRadius,-lowestOffset,Mathf.Sin(spillAngle)*spillRadius);
                }
                // Tabletop spill is not part of the vessel packing domain. In
                // particular it must not supply support heights or adjacency
                // candidates for subsequent chunks inside the vessel.
                if(placed){
                    centers.Add(p);sizes.Add(bound);
                    packedBoxes.Add(packingBox);
                    packedTops.Add(p.y+highestOffset);
                    packedExtents.Add(ice?extent:Vector3.one*bound);
                }
                var unit=shapeVertices;var faces=shapeFaces;
                if(bodySeeds!=null){
                    var local=new Vector3[unit.Length];
                    for(int i=0;i<unit.Length;i++)local[i]=rotation*Vector3.Scale(unit[i]*r,scale);
                    bodySeeds.Add(new SolidBodySeed{SubstanceId=kv.Key,IsPrecipitate=precip,IsSpilled=!placed,
                        FirstVertex=verts.Count,VertexCount=faces.Length,Center=p,LocalVertices=local,Triangles=faces,
                        VolumeWorldCubed=vol/count,MassGrams=kv.Value*s.Mass/count});
                }
                Color c=color*Mathf.Lerp(0.86f,1.04f,Rand());c.a=1;
                foreach(int index in faces){tris.Add(verts.Count);verts.Add(p+rotation*Vector3.Scale(unit[index]*r,scale));colors.Add(c);
                    uv.Add(new Vector2(s.Cls=="metal"?0.85f:0,ice?0.94f:powder?0.15f:0.6f));}
            }
        }
        var mesh=new Mesh{name="Mass calibrated solids",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        var opaqueTriangles=new List<int>();var iceTriangles=new List<int>();
        foreach(int index in tris)(uv[index].y>.9f?iceTriangles:opaqueTriangles).Add(index);
        mesh.SetVertices(verts);mesh.subMeshCount=2;
        mesh.SetTriangles(opaqueTriangles,0);mesh.SetTriangles(iceTriangles,1);
        mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.RecalculateNormals();
        // UV0 stores material traits, not texture coordinates. Build a valid tangent
        // basis explicitly so procedural micro-normals don't use degenerate UV tangents.
        var normals=mesh.normals;var tangents=new Vector4[normals.Length];
        for(int i=0;i<normals.Length;i++){
            var axis=Mathf.Abs(normals[i].y)>0.9f?Vector3.right:Vector3.up;
            var tangent=Vector3.Cross(axis,normals[i]).normalized;
            tangents[i]=new Vector4(tangent.x,tangent.y,tangent.z,1);
        }
        mesh.tangents=tangents;mesh.RecalculateBounds();return mesh;
    }
}
}
