Shader "ChemLab/Wood" {
Properties {
    _Color("Wood tint",Color)=(0.78,0.62,0.40,1)
    _Char("Charred tip",Range(0,1))=0
    _Ember("Ember heat",Range(0,1))=0
    _Length("Splint length",Float)=0.62
    _Seed("Grain variation",Float)=0
}
SubShader {
    Tags {"RenderType"="Opaque"}
    CGPROGRAM
    #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
    #pragma target 3.0
    struct Input {float3 localPos;float3 localNormal;};
    float4 _Color;
    float _Char,_Ember,_Length,_Seed;
    float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
    float noise(float2 p){
        float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
        return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),
                    lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
    }
    void vert(inout appdata_full v,out Input o){
        UNITY_INITIALIZE_OUTPUT(Input,o);o.localPos=v.vertex.xyz;o.localNormal=v.normal;
    }
    void surf(Input i,inout SurfaceOutputStandard o){
        float3 p=i.localPos;
        // Fibres follow the length of the wood, with slow irregular waviness.
        float crossAxis=lerp(p.x,p.z,abs(normalize(i.localNormal).x));
        float warp=noise(float2(crossAxis*65+_Seed,p.y*9));
        float fibre=(crossAxis+(.0028*warp))*900;
        float grain=noise(float2(fibre,p.y*3.5+_Seed));
        float fine=sin(fibre*6.28318+noise(float2(fibre*.4,p.y*16))*2);
        float aa=1-smoothstep(.3,1.5,fwidth(fibre));
        float endR=length(p.xz+float2(.06,.025))*520;
        float endGrain=.5+.5*sin(endR+warp*2);
        float endFace=smoothstep(.65,.95,abs(normalize(i.localNormal).y));
        float grainTint=lerp(.77+.26*grain+.035*fine*aa,.78+.21*endGrain,endFace);
        float edgeNoise=noise(float2(p.x*230+p.z*157,p.y*90+_Seed));
        float front=_Length-lerp(.015,.145,saturate(_Char));
        float charMask=saturate(_Char)*smoothstep(front-.017,front+.006,p.y+(edgeNoise-.5)*.016);
        float toasted=saturate(_Char)*smoothstep(front-.04,front+.005,p.y);
        float3 raw=_Color.rgb*grainTint;
        float3 brown=raw*float3(.48,.29,.15);
        float cracks=noise(float2(crossAxis*600+_Seed,p.y*75));
        float3 charcoal=lerp(float3(.009,.008,.006),float3(.040,.033,.026),cracks);
        o.Albedo=lerp(lerp(raw,brown,toasted),charcoal,charMask);
        float hotTip=smoothstep(_Length-.035,_Length-.003,p.y)*(.35+.65*cracks);
        o.Emission=float3(1.2,.12,.008)*sqrt(saturate(_Ember))*hotTip;
        o.Metallic=0;o.Smoothness=lerp(.23,.045,charMask);o.Occlusion=1;o.Alpha=1;
    }
    ENDCG
}
Fallback "Diffuse"
}
