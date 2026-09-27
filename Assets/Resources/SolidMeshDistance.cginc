// Actual triangle geometry in vessel-local coordinates, shared by GPU particle
// collision and density masking. No collider hull/bounding-box volume surrogate.
struct SolidMeshBody {float4 centerFirst;float4 minCount;float4 maxRadius;};
StructuredBuffer<SolidMeshBody> SolidBodies;
StructuredBuffer<float3> SolidTriangles;
int SolidCount;
float3 SolidClosestTriangle(float3 p,float3 a,float3 b,float3 c){
 float3 ab=b-a,ac=c-a,ap=p-a;float d1=dot(ab,ap),d2=dot(ac,ap);
 if(d1<=0&&d2<=0)return a;
 float3 bp=p-b;float d3=dot(ab,bp),d4=dot(ac,bp);
 if(d3>=0&&d4<=d3)return b;
 float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+ab*(d1/(d1-d3));
 float3 cp=p-c;float d5=dot(ab,cp),d6=dot(ac,cp);
 if(d6>=0&&d5<=d6)return c;
 float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+ac*(d2/(d2-d6));
 float va=d3*d6-d5*d4;if(va<=0&&d4>=d3&&d5>=d6)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
 float inverse=1/max(va+vb+vc,1e-30);return a+ab*(vb*inverse)+ac*(vc*inverse);
}
bool SolidRayHit(float3 p,float3 a,float3 b,float3 c){
 const float3 ray=float3(.812381,.331793,.478931);
 float3 e1=b-a,e2=c-a,h=cross(ray,e2);float det=dot(e1,h);
 if(abs(det)<1e-12)return false;
 float inverse=1/det;float3 s=p-a;float u=dot(s,h)*inverse;
 if(u<0||u>1)return false;
 float3 q=cross(s,e1);float v=dot(ray,q)*inverse;
 return v>=0&&u+v<=1&&dot(e2,q)*inverse>1e-7;
}
bool SolidNearBounds(SolidMeshBody body,float3 p,float margin){return all(p>=body.minCount.xyz-margin)&&all(p<=body.maxRadius.xyz+margin);}
#if defined(SOLID_MESH_BVH)
struct SolidBvhNode {float4 minEscape;float4 maxLeaf;};
StructuredBuffer<SolidBvhNode> SolidBodyNodes,SolidTriangleNodes;
StructuredBuffer<int2> SolidTriangleRanges;
int SolidBodyNodeCount;
bool SolidRayBounds(float3 p,SolidBvhNode node){
 const float3 inverseRay=1/float3(.812381,.331793,.478931);
 float3 near=(node.minEscape.xyz-1e-7-p)*inverseRay,far=(node.maxLeaf.xyz+1e-7-p)*inverseRay;
 return min(far.x,min(far.y,far.z))>=max(1e-7,max(near.x,max(near.y,near.z)));
}
bool InsideSolidBvh(float3 samplePosition){
 int bodyNode=0;
 [loop]while(bodyNode<SolidBodyNodeCount){
  SolidBvhNode node=SolidBodyNodes[bodyNode];
  if(any(samplePosition<node.minEscape.xyz-1e-7)||any(samplePosition>node.maxLeaf.xyz+1e-7)){bodyNode=(int)node.minEscape.w;continue;}
  int body=(int)node.maxLeaf.w;bodyNode++;
  if(body<0)continue;
  float3 p=samplePosition-SolidBodies[body].centerFirst.xyz;
  if(!SolidNearBounds(SolidBodies[body],p,0))continue;
  int2 range=SolidTriangleRanges[body];int triangleNode=range.x;uint crossings=0;
  [loop]while(triangleNode<range.y){
   SolidBvhNode triangleBox=SolidTriangleNodes[triangleNode];
   if(!SolidRayBounds(p,triangleBox)){triangleNode=(int)triangleBox.minEscape.w;continue;}
   int first=(int)triangleBox.maxLeaf.w;triangleNode++;
   if(first>=0)crossings+=SolidRayHit(p,SolidTriangles[first],SolidTriangles[first+1],SolidTriangles[first+2])?1:0;
  }
  if((crossings&1)!=0)return true;
 }
 return false;
}
#endif
bool InsideAnySolid(float3 samplePosition){
#if defined(SOLID_MESH_BVH)
 return InsideSolidBvh(samplePosition);
#else
 [loop]for(int bodyIndex=0;bodyIndex<SolidCount;bodyIndex++){
  SolidMeshBody body=SolidBodies[bodyIndex];float3 p=samplePosition-body.centerFirst.xyz;
  if(!SolidNearBounds(body,p,0))continue;
  uint crossings=0;int first=(int)body.centerFirst.w,count=(int)body.minCount.w;
  [loop]for(int t=0;t<count;t++){
   int offset=first+t*3;
   crossings+=SolidRayHit(p,SolidTriangles[offset],SolidTriangles[offset+1],SolidTriangles[offset+2])?1:0;
  }
  if((crossings&1)!=0)return true;
 }
 return false;
#endif
}
// Nearest actual triangle within a finite kernel support, for boundary density
// queries at fluid particles that have already been projected outside solids.
// The hierarchy only prunes by a lower bound; no bounding-box surface is used.
float4 SolidNearestSurface(float3 samplePosition,float support){
 float best=support*support;float3 nearestDelta=0;bool found=false;
#if defined(SOLID_MESH_BVH)
 int bodyNode=0;
 [loop]while(bodyNode<SolidBodyNodeCount){
  SolidBvhNode box=SolidBodyNodes[bodyNode];
  float3 gap=max(0,max(box.minEscape.xyz-samplePosition,samplePosition-box.maxLeaf.xyz));
  if(dot(gap,gap)>best){bodyNode=(int)box.minEscape.w;continue;}
  int body=(int)box.maxLeaf.w;bodyNode++;if(body<0)continue;
  float3 p=samplePosition-SolidBodies[body].centerFirst.xyz;
  int2 range=SolidTriangleRanges[body];int triangleNode=range.x;
  [loop]while(triangleNode<range.y){
   SolidBvhNode triangleBox=SolidTriangleNodes[triangleNode];
   float3 separation=max(0,max(triangleBox.minEscape.xyz-p,p-triangleBox.maxLeaf.xyz));
   if(dot(separation,separation)>best){triangleNode=(int)triangleBox.minEscape.w;continue;}
   int first=(int)triangleBox.maxLeaf.w;triangleNode++;if(first<0)continue;
   float3 q=SolidClosestTriangle(p,SolidTriangles[first],SolidTriangles[first+1],SolidTriangles[first+2]);
   float distance=dot(p-q,p-q);
   if(distance<best){best=distance;nearestDelta=p-q;found=true;}
  }
 }
#endif
 if(!found)return float4(0,0,0,support);
 float distance=sqrt(best);return float4(nearestDelta/max(distance,1e-10),distance);
}
float3 ProjectSolidMeshesLinear(float3 samplePosition,float margin){
 [loop]for(int bodyIndex=0;bodyIndex<SolidCount;bodyIndex++){
  SolidMeshBody body=SolidBodies[bodyIndex];float3 p=samplePosition-body.centerFirst.xyz;
  if(!SolidNearBounds(body,p,margin))continue;
  uint crossings=0;float best=1e30;float3 closest=0,outward=float3(0,1,0);
  int first=(int)body.centerFirst.w,count=(int)body.minCount.w;
  [loop]for(int t=0;t<count;t++){
   int offset=first+t*3;float3 a=SolidTriangles[offset],b=SolidTriangles[offset+1],c=SolidTriangles[offset+2];
   crossings+=SolidRayHit(p,a,b,c)?1:0;
   float3 q=SolidClosestTriangle(p,a,b,c);float d=dot(p-q,p-q);
   if(d<best){best=d;closest=q;outward=normalize(cross(b-a,c-a));}
  }
  bool inside=(crossings&1)!=0;
  if(inside||best<margin*margin){
   float3 direction=inside||best<1e-16?outward:(p-closest)*rsqrt(best);
   samplePosition=body.centerFirst.xyz+closest+direction*margin;
  }
 }
 return samplePosition;
}
float3 ProjectSolidMeshes(float3 samplePosition,float margin){
#if defined(SOLID_MESH_BVH)
 // Preserve original body order: projection against one body changes the
 // query position for the next. Only skip triangles that cannot affect either
 // ray parity or the nearest surface; ties retain original triangle order.
 [loop]for(int bodyIndex=0;bodyIndex<SolidCount;bodyIndex++){
  SolidMeshBody body=SolidBodies[bodyIndex];float3 p=samplePosition-body.centerFirst.xyz;
  if(!SolidNearBounds(body,p,margin))continue;
  uint crossings=0;float best=1e30;int bestFirst=2147483647;
  float3 closest=0,outward=float3(0,1,0);
  int2 range=SolidTriangleRanges[bodyIndex];int nodeIndex=range.x;
  [loop]while(nodeIndex<range.y){
   SolidBvhNode node=SolidTriangleNodes[nodeIndex];
   float3 separation=max(0,max(node.minEscape.xyz-1e-7-p,p-node.maxLeaf.xyz-1e-7));
   if(dot(separation,separation)>best+1e-12&&!SolidRayBounds(p,node)){nodeIndex=(int)node.minEscape.w;continue;}
   int first=(int)node.maxLeaf.w;nodeIndex++;
   if(first<0)continue;
   float3 a=SolidTriangles[first],b=SolidTriangles[first+1],c=SolidTriangles[first+2];
   crossings+=SolidRayHit(p,a,b,c)?1:0;
   float3 q=SolidClosestTriangle(p,a,b,c);float distance=dot(p-q,p-q);
   if(distance<best||(distance==best&&first<bestFirst)){best=distance;bestFirst=first;closest=q;outward=normalize(cross(b-a,c-a));}
  }
  bool inside=(crossings&1)!=0;
  if(inside||best<margin*margin){
   float3 direction=inside||best<1e-16?outward:(p-closest)*rsqrt(best);
   samplePosition=body.centerFirst.xyz+closest+direction*margin;
  }
 }
 return samplePosition;
#else
 return ProjectSolidMeshesLinear(samplePosition,margin);
#endif
}
