Shader "ChemLab/Glass" {
    Properties {
        _Tint        ("玻璃色", Color) = (0.86, 0.93, 0.97, 0.06)
        _Smoothness  ("光滑度", Range(0,1)) = 0.97
        _Refraction  ("折射强度", Range(0,0.25)) = 0.055
        _FresnelPow  ("菲涅尔指数", Range(1,8)) = 3.2
        _ReflStrength("反射强度", Range(0,2)) = 0.85
        _Highlight   ("聚焦高亮色", Color) = (0.30, 0.85, 1.0, 1)
        _HighlightAmt("聚焦高亮强度", Range(0,4)) = 0
    }
    SubShader {
        Tags { "Queue"="Transparent+4" "RenderType"="Transparent" "IgnoreProjector"="True" }

        GrabPass { "_LabGrab" }

        Pass {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "ChemLabCommon.cginc"
            #include "ChemLabRefraction.cginc"
            #include "ChemLabPrinter.cginc"

            sampler2D _LabGrab;
            float4 _LabGrab_TexelSize;
            fixed4 _Tint;
            float _Smoothness, _Refraction, _FresnelPow, _ReflStrength;
            fixed4 _Highlight;
            float _HighlightAmt;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct v2f {
                float4 pos    : SV_POSITION;
                float4 grab   : TEXCOORD0;
                float3 wpos   : TEXCOORD1;
                float3 wnor   : TEXCOORD2;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos  = UnityObjectToClipPos(v.vertex);
                o.grab = ComputeGrabScreenPos(o.pos);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wnor = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                KPrinterClip(i.wpos);
                float3 n = normalize(i.wnor);
                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);
                if (dot(n, v) < 0.0) n = -n;

                float fres = pow(1.0 - saturate(dot(n, v)), _FresnelPow);

                /* --- 折射：屏幕空间偏移采样背景 --- */
                float2 uv = i.grab.xy / max(i.grab.w, 1e-4);
                float2 offset = mul((float3x3)UNITY_MATRIX_V,n).xy * _Refraction * 0.12;
                /* 边缘处加强偏移，中心处几乎不偏，避免整体糊掉 */
                offset *= (0.35 + 0.65 * fres);
                float4 surfaceClip=mul(UNITY_MATRIX_VP,float4(i.wpos,1));
                float eyeDepth=-mul(UNITY_MATRIX_V,float4(i.wpos,1)).z;
                float3 refr = tex2D(_LabGrab, LabRefractionUV(surfaceClip,uv,offset,eyeDepth)).rgb;

                /* --- 反射：直接用同一个环境函数，保证和天空一致 --- */
                float3 rr = reflect(-v, n);
                float3 refl = LabSky(rr) * _ReflStrength;

                float reflectance=0.04+0.96*fres;
                float3 col = lerp(refr, refl, saturate(reflectance * 0.7));

                /* --- 玻璃自身极淡的色与厚度感 --- */
                col = lerp(col, _Tint.rgb, _Tint.a);

                /* --- 高光：主柔光箱在玻璃上拉出的长条亮痕 --- */
                float3 l = normalize(LAB_LIGHT_DIR);
                float3 hv = normalize(l + v);
                float spec = pow(saturate(dot(n, hv)), _Smoothness * 260.0 + 8.0);
                col += float3(1.0, 0.98, 0.94) * spec * _Smoothness * 0.65;

                /* 侧光补一点冷色反光 */
                float3 hv2 = normalize(normalize(float3(-1,0.28,0.18)) + v);
                col += float3(0.55, 0.72, 1.0) * pow(saturate(dot(n, hv2)), 80.0) * 0.5;

                float alpha = saturate(0.32 + _Tint.a + fres * 0.5 + spec * 0.2);

                /* --- 聚焦高亮：沿菲涅尔（轮廓最强）叠一层色 ---
                   这样描边是玻璃自己画出来的，不会像反向壳那样被透出来。 */
                if (_HighlightAmt > 0.001) {
                    float rim = pow(saturate(fres), 0.55);
                    col += _Highlight.rgb * rim * _HighlightAmt;
                    /* 边缘同时补一点不透明度，否则太透的玻璃上高亮看不出来 */
                    alpha = saturate(alpha + rim * _HighlightAmt * 0.45);
                }

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
