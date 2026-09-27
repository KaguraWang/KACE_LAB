/* =====================================================================
   Ghost snapshot: the coloured copy of an instrument that `david 1` leaves
   behind along its dash path.

   Flat and saturated, like a dash-drive afterimage: one colour per copy, no
   lighting, no additive blending. Additive copies pile up into a solid white
   blob wherever a trail overlaps, which is what makes such an effect read as
   an opaque white shape instead of a coloured translucent one.

   Each copy also gets a dark outline at roughly 70% opacity. It is drawn as a
   grazing-angle rim inside the same pass rather than as a second hull: an
   inverted hull would also fill the interior of the hollow glassware these
   copies are made of, and it would need its own resources per afterimage.
   ===================================================================== */
Shader "ChemLab/Ghost" {
    Properties {
        _Color          ("Tint", Color) = (0.02, 0.55, 0.95, 0.52)
        _Glow           ("Brightness", Range(0,2)) = 1
        _Saturation     ("Saturation", Range(0,2)) = 1.25
        _OutlineColor   ("Outline", Color) = (0.07, 0.08, 0.10, 0.7)
        _OutlineStart   ("Outline start", Range(0,1)) = 0.55
        _OutlineFalloff ("Outline falloff", Range(0.01,1)) = 0.3
        _GhostFade      ("Fade", Range(0,1)) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent+8" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            #include "GhostTint.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float3 n = normalize(i.worldNormal);
                float3 v = normalize(i.viewDir);
                return GhostTint(n, v);
            }
            ENDCG
        }
    }
    Fallback Off
}
