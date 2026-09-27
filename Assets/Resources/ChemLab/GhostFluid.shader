Shader "ChemLab/GhostFluidSnapshot" {
Properties {
_Color("Tint",Color)=(0.02,0.55,0.95,0.52)
_FluidDensity("Frozen density",3D)="white" {}
_Glow("Brightness",Range(0,2))=1
_Saturation("Saturation",Range(0,2))=1.25
_OutlineColor("Outline",Color)=(0.07,0.08,0.10,0.7)
_OutlineStart("Outline start",Range(0,1))=0.55
_OutlineFalloff("Outline falloff",Range(0.01,1))=0.3
_GhostFade("Fade",Range(0,1))=1
}
SubShader {Tags {"Queue"="Transparent+8" "RenderType"="Transparent"}

Pass {Cull Front ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
CGPROGRAM
#pragma target 4.5
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "GhostTint.cginc"

#define SOLID_MESH_BVH 1
#include "../SolidMeshDistance.cginc"
sampler3D _FluidDensity;
StructuredBuffer<float> _FluidProfile;
StructuredBuffer<int> _FluidOccupiedBounds;
float _FluidHeight;
float3 _FluidMin,_FluidSize,_FluidTexel;
float _AuditSurface,_AuditDisableOccupiedBounds;
struct v2f {float4 pos:SV_POSITION;float3 local:TEXCOORD0;};
v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;return o;}
float Density(float3 p){
 if(p.y<0||p.y>_FluidHeight)return 0;
 float row=saturate(p.y/_FluidHeight)*512;int k=min(511,(int)row);
 float radius=lerp(_FluidProfile[k],_FluidProfile[k+1],row-k);
 if(dot(p.xz,p.xz)>radius*radius)return 0;
 float3 uv=(p-_FluidMin)/_FluidSize;if(any(uv<0)||any(uv>1))return 0;
 // The compute grid stores endpoint samples at texel centres, not at texture edges.
 uv=uv*(1-_FluidTexel)+0.5*_FluidTexel;
 float density=tex3Dlod(_FluidDensity,float4(uv,0)).r;
 // Interpolate the continuous particle kernel before exact mesh clipping.
 // Interpolating pre-zeroed solid voxels erodes the neighboring liquid too.
 if(density>0&&SolidCount>0&&InsideAnySolid(p))return 0;
 return density;
}
struct output {float4 color:SV_Target;float depth:SV_Depth;};
output frag(v2f i){
 float3 eye=mul(unity_WorldToObject,float4(_WorldSpaceCameraPos,1)).xyz,dir=normalize(i.local-eye);
 if(unity_OrthoParams.w>0.5){
  dir=normalize(mul((float3x3)unity_WorldToObject,-UNITY_MATRIX_V[2].xyz));
  eye=i.local-dir*dot(i.local-eye,dir);
 }
 float3 inv=1/(dir+1e-8);float3 a=(_FluidMin-eye)*inv,b=(_FluidMin+_FluidSize-eye)*inv;
 float3 mn=min(a,b),mx=max(a,b);float start=max(0,max(mn.x,max(mn.y,mn.z))),end=min(mx.x,min(mx.y,mx.z));
 clip(end-start);float step=(end-start)/96;clip(step-1e-10);int firstSample=0,lastSample=95;
 if(_AuditDisableOccupiedBounds<0.5){
  // GPU splat bounds include every potentially nonzero grid sample. Expand
  // one more cell for trilinear support; no CPU readback or guessed fill plane.
  float3 grid=1/_FluidTexel;
  float3 lower=float3(_FluidOccupiedBounds[0],_FluidOccupiedBounds[1],_FluidOccupiedBounds[2]);
  float3 upper=float3(_FluidOccupiedBounds[3],_FluidOccupiedBounds[4],_FluidOccupiedBounds[5]);
  float3 occupiedMin=_FluidMin+max(0,lower-1)/(grid-1)*_FluidSize;
  float3 occupiedMax=_FluidMin+min(grid-1,upper+1)/(grid-1)*_FluidSize;
  float3 ta=(occupiedMin-eye)*inv,tb=(occupiedMax-eye)*inv;
  float3 near=min(ta,tb),far=max(ta,tb);
  float entered=max(start,max(near.x,max(near.y,near.z))),left=min(end,min(far.x,min(far.y,far.z)));
  clip(left-entered);
  // Keep the original 96-sample lattice and bisection bracket exactly; skip
  // only lattice points proven outside the expanded density-support bounds.
  firstSample=max(0,(int)ceil((entered-start)/step-0.5001));
  lastSample=min(95,(int)floor((left-start)/step-0.4999));clip(lastSample-firstSample);
 }
 float hit=-1,previous=firstSample>0?start+(firstSample-0.5)*step:start;float depth=0;
 [loop]for(int k=firstSample;k<=lastSample;k++){float t=start+(k+0.5)*step;float d=Density(eye+dir*t);if(d>0.42){if(hit<0){float lo=previous,hi=t;for(int j=0;j<5;j++){float mid=(lo+hi)*0.5;if(Density(eye+dir*mid)>0.42)hi=mid;else lo=mid;}hit=hi;}depth+=step*saturate(d);}previous=t;}
 clip(hit);
 // Keep the reported position on the confirmed fluid side of a solid interface
 // despite float rounding (2e-7 world units = 0.00002 mm, not a visible gap).
 if(SolidCount>0){float forward=hit+2e-7;if(forward<end&&Density(eye+dir*forward)>0.42)hit=forward;}
 float3 p=eye+dir*hit;
 float3 e=_FluidTexel*_FluidSize;
 float3 n=-normalize(float3(Density(p+float3(e.x,0,0))-Density(p-float3(e.x,0,0)),Density(p+float3(0,e.y,0))-Density(p-float3(0,e.y,0)),Density(p+float3(0,0,e.z))-Density(p-float3(0,0,e.z)))+1e-7);
 // Use the actual ray for both projection modes. Orthographic rays are parallel;
 // a vector to the camera position incorrectly makes their reflections converge.
 float3 wn=UnityObjectToWorldNormal(n),view=-normalize(mul((float3x3)unity_ObjectToWorld,dir));
 float4 clipPos=UnityObjectToClipPos(float4(p,1));
 output o;o.color=GhostTint(wn,view);o.depth=clipPos.z/clipPos.w;
 #if !defined(UNITY_REVERSED_Z)
 o.depth=lerp(UNITY_NEAR_CLIP_VALUE,1,o.depth);
 #endif
 if(_AuditSurface>0.5)o.color=float4(p,1); // Explicit off-screen regression readback only.
 return o;
}
ENDCG
}
} }
