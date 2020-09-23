float random (float2 st) {
    return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
}

float3 HSVtoRGB(float h, float s, float v) {
    return ((clamp(abs(frac(h + float3(0.0, 2.0, 1.0) / 3.0) * 6.0 - 3.0) - 1.0, 0.0, 1.0) - 1.0) * s + 1.0) * v;
}
