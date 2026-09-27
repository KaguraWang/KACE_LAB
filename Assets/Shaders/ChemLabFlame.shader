Shader "ChemLab/Flame" {
Properties { _Color("Color",Color)=(0.15,0.45,1,1) _CoreColor("Core Color",Color)=(0.65,0.85,1,1) _Height("Height",Float)=1 _SrcBlend("Source Blend",Float)=5 _DstBlend("Destination Blend",Float)=1 }
SubShader { Tags {"Queue"="Transparent+2" "RenderType"="Transparent"} Blend [_SrcBlend] [_DstBlend] ZWrite Off Cull Off
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
float4 _Color,_CoreColor; float _Height;
struct v2f {float4 pos:SV_POSITION;float3 local:TEXCOORD0;};
v2f vert(appdata_base v){v2f o;float t=saturate(v.vertex.y/_Height);v.vertex.x+=sin(_Time.y*13+t*8)*0.014*t*t;v.vertex.z+=sin(_Time.y*17+t*11)*0.009*t*t;o.local=v.vertex.xyz;o.pos=UnityObjectToClipPos(v.vertex);return o;}
fixed4 frag(v2f i):SV_Target {float t=saturate(i.local.y/_Height);float pulse=0.82+0.18*sin(_Time.y*19+t*23);float3 c=lerp(_CoreColor.rgb,_Color.rgb,saturate(t*1.45));return float4(c,(1-t)*pulse*_Color.a);}
ENDCG }
} }
