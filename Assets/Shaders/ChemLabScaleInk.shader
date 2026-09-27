Shader "ChemLab/ScaleInk" {
Properties { _MainTex("Font atlas",2D)="white" {} _Color("Ink",Color)=(1,1,1,1) _Cutoff("Glyph cutoff",Range(0,1))=0.3 }
SubShader {
Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"}
Cull Back
CGPROGRAM
#pragma surface surf Lambert alphatest:_Cutoff addshadow
sampler2D _MainTex;fixed4 _Color;
struct Input {float2 uv_MainTex;float4 color:COLOR;};
void surf(Input i,inout SurfaceOutput o){o.Albedo=i.color.rgb*_Color.rgb;o.Alpha=tex2D(_MainTex,i.uv_MainTex).a*i.color.a*_Color.a;}
ENDCG
}
}
