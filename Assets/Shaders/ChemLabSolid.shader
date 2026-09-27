Shader "ChemLab/Solid" {
Properties {_Color("Tint",Color)=(1,1,1,1)}
SubShader {
Tags {"RenderType"="Opaque"}
CGPROGRAM
#pragma surface surf Standard fullforwardshadows vertex:vert addshadow
#pragma target 3.0
struct Input {float4 color:COLOR;float3 worldPos;float3 localPos;float2 traits;};
fixed4 _Color;
void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.traits=v.texcoord.xy;o.localPos=v.vertex.xyz;o.color=v.color;}
void surf(Input i,inout SurfaceOutputStandard o){
    float grain=frac(sin(dot(floor(i.localPos*2400),float3(12.989,78.233,37.719)))*43758.5453);
    float coarse=frac(sin(dot(floor(i.localPos*430),float3(39.34,18.71,95.13)))*15473.31);
    float fade=saturate(1-length(fwidth(i.localPos))*1600);
    float detail=lerp(1,lerp(0.86,1.08,grain),fade);
    float powder=(1-step(0.1,i.traits.y))*(1-step(0.5,i.traits.x));
    o.Albedo=i.color.rgb*_Color.rgb*detail*lerp(0.96,1.02,coarse)*lerp(1,0.72,powder);
    float2 micro=float2(sin(i.localPos.x*1800+i.localPos.z*930),cos(i.localPos.z*2100-i.localPos.x*710));
    o.Normal=normalize(float3(micro*0.18*fade*powder,1));
    o.Metallic=i.traits.x;o.Smoothness=i.traits.y;o.Occlusion=0.97;o.Alpha=1;
    // Ice is a glossy frosted dielectric, not a fine powder bed. This is a
    // material cue only; refraction and two-way fluid/ice coupling are separate.
    if(i.traits.y>0.9){
        float frost=.5+.5*sin(i.localPos.x*81+i.localPos.y*67)*sin(i.localPos.z*93-i.localPos.y*43);
        o.Albedo=i.color.rgb*_Color.rgb*lerp(.85,1.04,frost);
        o.Metallic=0;o.Smoothness=lerp(.72,.95,frost);o.Normal=float3(0,0,1);
    }
}
ENDCG
}
Fallback "Diffuse"
}
