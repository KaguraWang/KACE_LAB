// White lab bench surface.
// Shader.Find("Standard") returns null in a build (built-in shaders are
// stripped unless referenced), so the bench needs its own shader.
Shader "ChemLab/Bench" {
    Properties {
        _Color ("BenchColor", Color) = (0.90, 0.91, 0.93, 1)
        _Grid  ("GridDepth", Range(0,1)) = 0.10
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "ChemLabCommon.cginc"
            #include "ChemLabPrinter.cginc"

            fixed4 _Color;
            float _Grid;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f {
                float4 pos  : SV_POSITION;
                float3 wnor : TEXCOORD0;
                float3 wpos : TEXCOORD1;
                SHADOW_COORDS(2)
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos  = UnityObjectToClipPos(v.vertex);
                o.wnor = UnityObjectToWorldNormal(v.normal);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                KPrinterClip(i.wpos);
                float3 n = normalize(i.wnor);
                float3 vdir = normalize(_WorldSpaceCameraPos - i.wpos);
                float3 ldir = normalize(LAB_LIGHT_DIR);

                float fx = i.wpos.x * 2.0;
                float fz = i.wpos.z * 2.0;
                float gx = smoothstep(0.0, 0.02, frac(fx)) * smoothstep(1.0, 0.98, frac(fx));
                float gz = smoothstep(0.0, 0.02, frac(fz)) * smoothstep(1.0, 0.98, frac(fz));
                float grid = 1.0 - gx * gz;

                float3 base = _Color.rgb - _Grid * grid;
                float d = max(dot(n, ldir), 0.0);
                float3 c = base * (d * 0.72 * SHADOW_ATTENUATION(i) + 0.26);

                float3 hv = normalize(ldir + vdir);
                c += pow(max(dot(n, hv), 0.0), 90.0) * 0.09;
                c += pow(1.0 - max(dot(n, vdir), 0.0), 4.0) * 0.03;
                return fixed4(c, 1.0);
            }
            ENDCG
        }
    }
}
