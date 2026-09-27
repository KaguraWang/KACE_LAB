#ifndef CHEMLAB_REFRACTION_INCLUDED
#define CHEMLAB_REFRACTION_INCLUDED
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
float4 _CameraDepthTexture_TexelSize;
float _LabRefractionAuditBypass; // Regression negative control; production default is zero.
float LabSceneEyeDepth(float2 uv){
    float raw=SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,uv);
    if(unity_OrthoParams.w>0.5){
        #if defined(UNITY_REVERSED_Z)
        raw=1-raw;
        #endif
        return lerp(_ProjectionParams.y,_ProjectionParams.z,raw);
    }
    return LinearEyeDepth(raw);
}
// A displaced screen sample is valid only if its opaque surface is behind
// the refracting surface. Otherwise it copies foreground ink/objects a second time.
float2 LabRefractionUV(float4 clipPosition,float2 grabUV,float2 offset,float eyeDepth) {
    if(_LabRefractionAuditBypass>0.5)return grabUV+offset;
    float4 screen=ComputeScreenPos(clipPosition);
    float2 depthUV=screen.xy/screen.w;
    float2 depthOffset=offset;
    #if UNITY_UV_STARTS_AT_TOP
    depthOffset.y*=-_ProjectionParams.x;
    #else
    depthOffset.y*=_ProjectionParams.x;
    #endif
    float2 candidate=depthUV+depthOffset;
    if(any(candidate<0)||any(candidate>1))return grabUV;
    float sceneDepth=LabSceneEyeDepth(candidate);
    // Grab color is bilinear filtered: reject the whole footprint when any
    // contributor is foreground, including subpixel glyph and scale edges.
    float2 footprint=abs(_CameraDepthTexture_TexelSize.xy)*0.75;
    sceneDepth=min(sceneDepth,LabSceneEyeDepth(candidate+footprint));
    sceneDepth=min(sceneDepth,LabSceneEyeDepth(candidate-footprint));
    sceneDepth=min(sceneDepth,LabSceneEyeDepth(candidate+float2(footprint.x,-footprint.y)));
    sceneDepth=min(sceneDepth,LabSceneEyeDepth(candidate+float2(-footprint.x,footprint.y)));
    return sceneDepth<eyeDepth-0.0002?grabUV:grabUV+offset;
}
#endif
