float sdSphere2(float2 p, float s)
{
  return length(p)-s;
}

float sdRoundBox2(float2 p, float2 b, float r)
{
  float2 q = abs(p) - b;
  return length(max(q,0.0)) + min(max(q.x,q.y),0.0) - r;
}

float sdBox2(float2 p, float2 b)
{
  return sdRoundBox2(p, b, 0.0);
}

float2 mod2(float2 a, float2 b)
{
    return frac(abs(a / b)) * abs(b);
}

float smoothMin(float d1, float d2, float k)
{
    float h = exp(-k * d1) + exp(-k * d2);
    return -log(h) / k;
}

float2 repeat2(float2 pos, float2 span)
{
    return mod2(pos, span) - span * 0.5;
}

float2 rotate(float2 p, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2(c*p.x+s*p.y, -s*p.x+c*p.y);
}

#ifndef PI
static const float PI = 3.1415926535;
#endif
