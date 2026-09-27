Shader "ChemLab/Metal" {
Properties {_Color("Metal tint",Color)=(.32,.34,.37,1) _Metallic("Metallic",Range(0,1))=.8 _Smoothness("Smoothness",Range(0,1))=.38}
SubShader {
Tags {"RenderType"="Opaque"}
CGPROGRAM
#pragma surface surf Standard fullforwardshadows
#pragma target 3.0
struct Input {float3 worldPos;};
fixed4 _Color;half _Metallic,_Smoothness;
void surf(Input i,inout SurfaceOutputStandard o){o.Albedo=_Color.rgb;o.Metallic=_Metallic;o.Smoothness=_Smoothness;o.Alpha=1;}
ENDCG
}
Fallback "Standard"
}
