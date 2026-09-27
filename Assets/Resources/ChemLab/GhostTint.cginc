#ifndef CHEMLAB_GHOST_TINT
#define CHEMLAB_GHOST_TINT
float4 _Color, _OutlineColor;
float _Glow, _Saturation, _OutlineStart, _OutlineFalloff, _GhostFade;
float4 GhostTint(float3 normal, float3 view) {
    float rim = 1.0 - saturate(abs(dot(normalize(normal), normalize(view))));
    float outline = smoothstep(_OutlineStart, _OutlineStart + _OutlineFalloff, rim);
    float3 rgb = _Color.rgb * _Glow;
    float luma = dot(rgb, float3(0.299, 0.587, 0.114));
    rgb = saturate(lerp(luma.xxx, rgb, _Saturation));
    // PaintGhost already fades the fill alpha. Fade the outline by the same
    // amount, otherwise a 70%-opaque black silhouette survives until deletion.
    return float4(lerp(rgb, _OutlineColor.rgb, outline),
        lerp(_Color.a, _OutlineColor.a * saturate(_GhostFade), outline));
}
#endif
