#ifndef CHEMLAB_BOUNDARY
#define CHEMLAB_BOUNDARY
float _Profile[32];
float _VesselHeight, _ClipContainer, _LiquidHeight, _ParticleKind, _ClosedContainer;
void ClipVessel(float3 local) {
    if(_ClipContainer<0.5) return;
    if(local.y<0) clip(-1);
    if(_ClosedContainer>0.5)clip(_VesselHeight-local.y);
    if(local.y<_VesselHeight) {
        float f=saturate(local.y/max(_VesselHeight,0.001))*31;
        int a=min((int)f,30);
        // 保守内包络：在肩部/窄颈的采样段内也不越过玻璃内壁。
        float r=min(_Profile[a],_Profile[a+1]);
        clip(r-length(local.xz));
    }
}
#endif
