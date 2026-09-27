// 轮廓描边用的无光照着色器。
//
// 为什么要自带 shader 而不是用 Standard：
//   内置着色器在构建里会被剔除（Shader.Find 返回 null），
//   除非有材质引用它。项目里 Bench/Glass/Liquid 都自带 shader 就是这个原因。
//
// 描边厚度怎么控制：
//   顶点沿法线在**物体空间**外扩固定距离，所以不管物体多大，
//   描边看起来都是一样粗。之前用「整体放大若干百分比」做过一版，
//   结果器皿被放大成一整块实心色 —— 百分比放大对小物体来说太粗暴了。
Shader "ChemLab/Outline" {
    Properties {
        _Color   ("OutlineColor", Color) = (0.35, 0.85, 1.0, 1)
        _Outline ("OutlineWidth", Float) = 0.006
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" }

        Pass {
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float  _Outline;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v) {
                v2f o;
                /* 沿法线外扩。用物体空间法线而不是世界法线，
                   这样描边会跟着物体的旋转一起转，不会在斜面上变细。 */
                v.vertex.xyz += v.normal * _Outline;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return _Color;
            }
            ENDCG
        }
    }
    Fallback Off
}
