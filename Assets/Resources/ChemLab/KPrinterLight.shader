Shader "ChemLab/KPrinterLight" {
    Properties { _Color("Cyan light", Color)=(.035,.58,1,.65) }
    SubShader {
        Tags { "Queue"="Transparent+8" "RenderType"="Transparent" }
        Pass {
            ZWrite Off
            Cull Off
            Blend SrcAlpha One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            v2f vert(appdata v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o; }
            fixed4 frag(v2f i):SV_Target {
                // Procedural soft photon discs; reticle meshes use zero UVs.
                float photon=1-smoothstep(.16,.5,length(i.uv-.5));
                float ring=1-step(.001,dot(i.uv,i.uv));
                return fixed4(_Color.rgb*i.color.rgb,_Color.a*i.color.a*max(photon,ring));
            }
            ENDCG
        }
    }
    Fallback Off
}
