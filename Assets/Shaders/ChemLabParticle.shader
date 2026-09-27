/* =====================================================================
   粒子：软圆点，无需贴图（径向衰减在片元里算）
   混合模式由材质属性控制，同一份 shader 兼顾普通 alpha 与叠加发光
   ===================================================================== */
Shader "ChemLab/Particle" {
    Properties {
        _Color    ("染色", Color) = (1,1,1,1)
        _Softness ("边缘硬度", Range(0.6,6)) = 2.2
        _SrcBlend ("SrcBlend", Float) = 5     /* SrcAlpha */
        _DstBlend ("DstBlend", Float) = 10    /* OneMinusSrcAlpha */
        _ParticleKind ("0 Bubble / 1 Fume / 2 Flame", Float) = 1
        _ClipContainer ("Container clipping", Float) = 0
        _ClosedContainer ("Closed container lid", Float) = 0
    }
    SubShader {
        Tags { "Queue"="Transparent+2" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass {
            ZWrite Off
            Cull Off
            Blend [_SrcBlend] [_DstBlend]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "ChemLabBoundary.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float4 col : COLOR;
                float2 uv  : TEXCOORD0;
                float3 local : TEXCOORD1;
            };

            fixed4 _Color;
            float _Softness;

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.col = v.color;
                o.uv  = v.uv;
                o.local = v.vertex.xyz;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                ClipVessel(i.local);
                float2 d = i.uv - 0.5;
                float r = length(d) * 2.0;
                float a; float3 color=i.col.rgb*_Color.rgb;
                if(_ParticleKind<0.5) {
                    if(_ClipContainer>0.5) clip(_LiquidHeight-i.local.y);
                    float rim=exp(-pow((r-0.78)*13,2));
                    float glint=exp(-dot(d-float2(-0.17,0.19),d-float2(-0.17,0.19))*180);
                    a=(rim*0.42+glint*0.8+0.025)*smoothstep(1,0.88,r);
                    color=lerp(float3(0.32,0.43,0.49),float3(0.95,0.98,1),saturate(glint+rim*0.65));
                } else if(_ParticleKind<1.5) {
                    float n=sin(i.local.x*39+_Time.y*1.5+sin(i.local.y*21))*sin(i.local.z*33-i.local.y*18+_Time.y);
                    a=pow(saturate(1-r*r),2.5)*saturate(0.64+n*0.3);
                    color*=0.9+0.1*n;
                } else {
                    float wav=sin(i.uv.y*17-_Time.y*12+i.local.x*9)*0.09*i.uv.y;
                    float width=max(0.035,(1-i.uv.y)*0.44);
                    float flame=saturate(1-abs(d.x+wav)/width)*sin(i.uv.y*3.14159);
                    a=pow(flame,1.2);
                    color=lerp(color,float3(1,0.88,0.48),pow(flame,3))*1.35;
                }
                return fixed4(color, a*i.col.a*_Color.a);
            }
            ENDCG
        }
    }
}
