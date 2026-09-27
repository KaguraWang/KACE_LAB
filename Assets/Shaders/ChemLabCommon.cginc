/* =====================================================================
   白色实验室环境
   玻璃之所以像玻璃，九成靠这个函数 —— 环境里必须有亮光源能被反射出来。
   反差是关键：白色台面（下半球亮）当反光板，墙和天花板压到中灰。
   ===================================================================== */
#ifndef CHEMLAB_COMMON_INCLUDED
#define CHEMLAB_COMMON_INCLUDED

float3 LabSky(float3 rd) {
    rd = normalize(rd);
    float h = rd.y;

    float3 ceilC = float3(0.115, 0.128, 0.150);
    float3 wallC = float3(0.068, 0.079, 0.095);
    float3 tblC  = float3(0.400, 0.418, 0.442);

    float3 col = (h > 0.0) ? lerp(wallC, ceilC, pow(h, 0.80))
                           : lerp(wallC, tblC,  pow(-h, 0.50));
    col += float3(0.20, 0.21, 0.23) * exp(-pow(h * 12.0, 2.0));

    float3 L1 = normalize(float3( 0.10, 1.00,  0.28));   // 顶部主柔光箱
    float3 L2 = normalize(float3(-1.00, 0.28,  0.18));   // 左侧冷光板
    float3 L3 = normalize(float3( 0.80, 0.16, -0.55));   // 右后轮廓光

    col += float3(1.00, 0.99, 0.96) * smoothstep(0.90, 0.998, dot(rd, L1)) * 4.5;
    col += float3(1.00, 0.99, 0.96) * smoothstep(0.10, 0.96,  dot(rd, L1)) * 0.35;
    col += float3(0.95, 0.97, 1.00) * smoothstep(0.86, 0.997, dot(rd, L2)) * 2.0;
    col += float3(0.98, 0.96, 1.00) * smoothstep(0.88, 0.997, dot(rd, L3)) * 1.6;
    col += float3(0.22, 0.23, 0.25) * smoothstep(0.10, -0.60, h);
    return col;
}

/* 主光源方向，与天空里的柔光箱一致 */
static const float3 LAB_LIGHT_DIR = float3(0.0985, 0.9850, 0.2760);

#endif