Shader "ChemLab/Condensate" {
    Properties { _FlowPhase("Flow phase",Float)=0 }
    SubShader {
        Tags { "Queue"="Transparent+3" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _FlowPhase;
            struct v2f { float4 clip:SV_POSITION; float3 local:TEXCOORD0; float3 normal:TEXCOORD1; float3 world:TEXCOORD2; };
            v2f vert(appdata_base v){
                v2f o;o.clip=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;
            }
            fixed4 frag(v2f i):SV_Target {
                float around=atan2(i.local.z,i.local.x);
                float travel=i.local.y*8-_FlowPhase;
                float ripple=.5+.5*sin(travel*6.283+sin(around*3+travel)*.6);
                float rivulet=pow(saturate(.5+.5*cos(around*5+sin(travel)*.25)),7);
                float fres=pow(1-saturate(abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world)))),3);
                float glint=pow(ripple,14)*rivulet;
                return fixed4(lerp(float3(.55,.78,.84),float3(.96,.99,1),fres*.6+glint*.4),.12+fres*.20+rivulet*.16+glint*.3);
            }
            ENDCG
        }
    }
}
