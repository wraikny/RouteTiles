cbuffer Consts : register(b1)
{
  float4 time;
};

struct PS_INPUT
{
  float4  Position : SV_POSITION;
  float4  Color    : COLOR0;
  float2  UV1 : UV0;
  float2  UV2 : UV1;
};

float calc(float2 p, float x, float y, float z, float w)
{
  float t = time.x;
  float a = (sin(x - t * y) + 2.0 * pow(cos(x * 2.0 + t * y),3.0)) * 0.3 * z + 0.5;

  return lerp(0.0, w, p.y < a);
}


float4 main(PS_INPUT input) : SV_TARGET
{
  float2 p = input.UV1;
  float3 o = float3(0.2, 0.4, 0.8);
  o += calc(p, p.x, 0.4, 0.2, -0.1);
  o += calc(p, p.x + 1.0, 1.0, 0.3, 0.2);
  o += calc(p, p.x * 2.0 - 2.0, -0.5, 0.1, 0.1);

  return float4(o, 1.0);
}