// Integral of normalized 3D poly6 over a solid half-space at distance d.
// At a plane the missing kernel mass is exactly 1/2, and vanishes at h.
// Tangent-plane approximation only: curved/corner boundaries need volume maps.
float4 SolidBoundaryKernelFromSurface(float4 surface,float h){
 if(surface.w>=h)return 0;
 float s=saturate(surface.w/h),s2=s*s;
 float integral=s*(1+s2*(-4.0/3+s2*(6.0/5+s2*(-4.0/7+s2/9))));
 float fraction=max(0,0.5-(315.0/256)*integral);
 float q=max(0,1-s2);float derivative=-(315.0/256)/h*q*q*q*q;
 return float4(surface.xyz*derivative,fraction);
}
float4 SolidBoundaryKernel(float3 position,float h){
 return SolidBoundaryKernelFromSurface(SolidNearestSurface(position,h),h);
}
