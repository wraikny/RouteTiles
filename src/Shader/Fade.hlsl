struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 UV1 : UV0;
    float2 UV2 : UV1;
};

cbuffer Consts : register(b1)
{
  float4 fadeRate;
  float4 fadeColor;
};


Texture2D mainTex : register(t0);
SamplerState mainSamp : register(s0);

float4 main(PS_INPUT input) : SV_TARGET
{
    float4 color = mainTex.Sample(mainSamp, input.UV1);
    return float4(lerp(color.xyz, fadeColor.xyz, fadeRate.x), color.w);
}
