#include "Utils.hlsl"
#include "Distance.hlsl"

struct PS_INPUT
{
  float4  Position : SV_POSITION;
  float4  Color    : COLOR0;
  float2  UV1 : UV0;
  float2  UV2 : UV1;
};

static const float4 backgroundColor = float4(0.8, 0.8, 1.0, 1.0);
static const float cell = 10.0;
static const float2 speedRange = float2(0.1, 0.9);
static const float speed = 0.1;
static const float rorateSpeed = 20.0;
static const float timeOffset = 123.45;

cbuffer Consts : register(b1)
{
  float4 time;
  float4 windowSize;
};

static const float2 resolution = windowSize.xy / windowSize.x;

float2 toCelledUV(float2 uv) {
    return floor(uv * resolution * cell) / cell;
}

float3 getColor(float2 p) {
    float2 argP = p;
    float t = (time.x + timeOffset) * speed;

    float2 celledPos = floor(p * cell) / cell;
    float cellXRand1 = random(float2(celledPos.x, 0.5)) * 0.8 + 0.2;
    float cellXRand2 = random(float2(celledPos.x, 10.2)) * 0.9 + 0.1;
    float cellXRand3 = random(float2(celledPos.x, 53.5)) * 0.9 + 0.1;

    float offset = 1.0 * cellXRand1 * t + 0.5 * cellXRand2 * sin(t * cellXRand3);

    p = p + float2(0.0, offset);

    float2 repSpan = float2(0.1, 0.1 + cellXRand2 * 0.05);
    float2 repP = repeat2(p, repSpan);
    int2 repId = p / repSpan;
    float rotation = random(repId) + random(repId * 123.45) * t * rorateSpeed * 0.1;
    repP = rotate(repP, random(repId) * t * rorateSpeed + sin(rotation) * PI * 2.0);
    p = repP / (0.2 + (1.0 - repId.y * repSpan.y) * random(repId) * 0.015);

    float d = sdBox2(p, float2(0.1, 0.1));

    float a = argP.y * argP.y;

    float3 baseColor = HSVtoRGB(0.66 + time.x * 0.02, 1.0, 0.2);

    float3 color = d < 0.1 ? (1 - a) * baseColor.xyz + (p.y + 0.5) * 0.1 : a * baseColor.xyz;

    return color;
}

float4 main(PS_INPUT input) : SV_TARGET
{
    float2 p = input.UV1 * resolution;

    float3 color = getColor(p);
    // float3 color = d < 0.1 ? 1.0 : 0.0;
    return float4(color, 1.0);
}
