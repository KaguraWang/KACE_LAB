Shader "ChemLab/KPrinterHologram" {
    Properties {
        _RevealY("Printed height", Float) = -1
        _ScanY("Scan height", Float) = 0
        _Phase("Animation phase", Float) = 0
        _Fade("Projection intensity", Float) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent+7" "RenderType"="Transparent" }
        Pass {
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _RevealY, _ScanY, _Phase, _Fade;
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
            v2f vert(appdata_base v) {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal); return o;
            }
            float grid(float coordinate) {
                float width=max(fwidth(coordinate),.004);
                float distance=abs(frac(coordinate-.5)-.5);
                return 1-smoothstep(width*.25,width*.9,distance);
            }
            fixed4 frag(v2f i):SV_Target {
                float3 n=normalize(i.normal);
                float rim=pow(1-saturate(abs(dot(n,normalize(_WorldSpaceCameraPos-i.world)))),2.2);
                float horizontal=grid(i.world.y*23-_Phase*2.8);
                float vertical=max(grid(i.world.x*22+_Phase*.3),grid(i.world.z*22-_Phase*.4));
                float lattice=max(horizontal,vertical)*.16;
                float band=exp(-abs(i.world.y-_ScanY)*65);
                float lanes=pow(saturate(.5+.5*sin(i.world.y*110-_Phase*32)),12)*.07;
                float printed=lerp(.075,1,smoothstep(_RevealY-.025,_RevealY+.025,i.world.y));
                float scanVisibility=lerp(printed,1,step(1.78,_Phase));
                float alpha=((.045+rim*.25+lattice+lanes)*printed+band*.48*scanVisibility)*_Fade;
                float3 color=lerp(float3(.025,.30,.95),float3(.10,.83,1),band*.8+rim*.2);
                return fixed4(color,alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
