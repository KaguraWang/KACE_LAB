Shader "ChemLab/Ice" {
Properties {
    _Distortion("Surface refraction",Range(0,0.06))=0.018
    _Frost("Internal frost",Range(0,0.3))=0.045
}
SubShader {
    Tags {"Queue"="Transparent+2" "RenderType"="Transparent"}
    GrabPass {"_IceBackground"}
    Pass {
        Cull Back ZWrite Off
        // Transmission is explicitly sampled below; alpha-blending it again
        // would dilute the refracted image with an undistorted second image.
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 3.0
        #include "UnityCG.cginc"
        #include "../Shaders/ChemLabCommon.cginc"
        #include "../Shaders/ChemLabRefraction.cginc"
        sampler2D _IceBackground;
        float _Distortion,_Frost;
        struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
        struct v2f {float4 pos:SV_POSITION;float4 grab:TEXCOORD0;float3 world:TEXCOORD1;float3 normal:TEXCOORD2;float3 local:TEXCOORD3;};
        v2f vert(appdata v){
            v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.grab=ComputeGrabScreenPos(o.pos);
            o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.local=v.vertex.xyz;return o;
        }
        float3 rippleGradient(float3 p){
            // Derivative of a continuous, stationary melt-surface height field.
            return float3(cos(p.x*37+p.z*19)*sin(p.y*31),
                sin(p.x*37+p.z*19)*cos(p.y*31),
                cos(p.z*43-p.y*17)*sin(p.x*29));
        }
        fixed4 frag(v2f i):SV_Target {
            float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos-i.world);
            float3 perturb=mul((float3x3)unity_ObjectToWorld,rippleGradient(i.local));
            n=normalize(n+.14*(perturb-n*dot(perturb,n)));
            // Ice/air dielectric reflectance near 2%; Schlick grazing reflection.
            float fres=.018+ .982*pow(1-saturate(dot(n,v)),5);
            float3 incident=mul((float3x3)UNITY_MATRIX_V,-v);
            float3 transmitted=refract(incident,mul((float3x3)UNITY_MATRIX_V,n),1/1.31);
            float2 bend=transmitted.xy/max(.2,abs(transmitted.z))-incident.xy/max(.2,abs(incident.z));
            float2 uv=i.grab.xy/i.grab.w;
            float eyeDepth=-mul(UNITY_MATRIX_V,float4(i.world,1)).z;
            float2 refractedUV=LabRefractionUV(mul(UNITY_MATRIX_VP,float4(i.world,1)),uv,bend*_Distortion,eyeDepth);
            float3 transmission=tex2D(_IceBackground,refractedUV).rgb;
            float veins=pow(saturate(.5+.5*sin(i.local.x*71+i.local.y*49+sin(i.local.z*59))),18);
            transmission=lerp(transmission*float3(.975,.994,1),float3(.82,.93,.98),_Frost*(.3+veins));
            float3 reflection=LabSky(reflect(-v,n));
            float3 col=lerp(transmission,reflection,fres);
            float3 h=normalize(normalize(LAB_LIGHT_DIR)+v);
            col+=pow(saturate(dot(n,h)),180)*float3(1,.99,.96)*.55;
            return fixed4(col,1);
        }
        ENDCG
    }
}
}
