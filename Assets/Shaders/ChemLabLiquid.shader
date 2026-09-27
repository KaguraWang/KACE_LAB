Shader "ChemLab/Liquid" {
    Properties {
        _Color       ("液体色", Color) = (0.2, 0.55, 0.9, 0.75)
        _Turbidity   ("浑浊度", Range(0,1)) = 0
        _Thickness   ("厚度吸收", Range(0,6)) = 2.2
        _Gloss       ("表面光泽", Range(0,1)) = 0.9
        _Surface     ("是否液面", Range(0,1)) = 0
        _ClipContainer ("Container clipping", Float) = 0
    }
    SubShader {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" }
        GrabPass { "_LiquidGrab" }
        Pass {
            ZWrite On
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "ChemLabCommon.cginc"
            #include "ChemLabRefraction.cginc"
            #include "ChemLabBoundary.cginc"
            sampler2D _LiquidGrab;

            fixed4 _Color;
            float _Turbidity, _Thickness, _Gloss, _Surface;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f {
                float4 pos  : SV_POSITION;
                float3 wpos : TEXCOORD0;
                float3 wnor : TEXCOORD1;
                float4 grab : TEXCOORD2;
                float3 local : TEXCOORD3;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos  = UnityObjectToClipPos(v.vertex);
                o.grab = ComputeGrabScreenPos(o.pos);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wnor = UnityObjectToWorldNormal(v.normal);
                o.local = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float3 n = normalize(i.wnor);
                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);
                if (dot(n, v) < 0.0) n = -n;

                /* 斜看时穿透更厚 → 颜色更深（Beer-Lambert 的近似） */
                float thick = saturate(1.0 - dot(n, v));
                float3 base = _Color.rgb;
                float3 deep = base * base;
                float2 uv=i.grab.xy/max(i.grab.w,0.001);
                float2 bend=mul((float3x3)UNITY_MATRIX_V,n).xy*0.003;
                float4 surfaceClip=mul(UNITY_MATRIX_VP,float4(i.wpos,1));
                float eyeDepth=-mul(UNITY_MATRIX_V,float4(i.wpos,1)).z;
                float3 background=tex2D(_LiquidGrab,LabRefractionUV(surfaceClip,uv,bend,eyeDepth)).rgb;
                float3 transmission=exp(-(1-base)*(_Thickness*0.4+thick));
                float3 col=lerp(background*transmission,base,_Turbidity*0.65);

                /* 上方柔光箱的散射，液体才会有"透亮"的感觉 */
                float3 l = normalize(LAB_LIGHT_DIR);
                float3 hv = normalize(l + v);
                float spec = pow(saturate(dot(n, hv)), 90.0) * _Gloss;
                col += float3(1.0, 0.98, 0.94) * spec * 0.3;

                /* 浑浊：悬浮颗粒把光散射成白 */
                col = lerp(col, float3(0.88, 0.90, 0.94), _Turbidity * 0.45);

                float fres = pow(1.0 - saturate(dot(n, v)), 4.0);
                float reflectance=0.02+0.98*fres;
                col=lerp(col,LabSky(reflect(-v,n)),reflectance*0.32);
                float alpha = 0.92;

                /* 液面：提亮一圈，这是判断液位最直观的线索 */
                if (_Surface > 0.5) {
                    col += float3(0.5, 0.55, 0.6) * fres;
                    alpha = saturate(alpha + 0.15);
                }
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
