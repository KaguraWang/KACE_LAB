Shader "ChemLab/GlassMark" {
Properties { _MainTex("Transparent mark",2D)="white" {} _Color("Ink",Color)=(1,1,1,1) _Cutoff("Alpha cutoff",Range(0,1))=.38 }
SubShader {
Tags {"Queue"="Transparent+5" "RenderType"="TransparentCutout"}
Cull Back
ZWrite On
CGPROGRAM
#pragma surface surf Lambert alphatest:_Cutoff addshadow
sampler2D _MainTex; fixed4 _Color;
struct Input {float2 uv_MainTex;};
void surf(Input i,inout SurfaceOutput o){fixed4 pixel=tex2D(_MainTex,i.uv_MainTex)*_Color;o.Albedo=pixel.rgb;o.Alpha=pixel.a;}
ENDCG
}
Fallback Off
}
