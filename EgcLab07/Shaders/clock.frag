#version 330 core
in vec2 vUV;
uniform bool uShowUV;
uniform float uHour, uMinute, uSecond, uNight, uRimWidth, uTickWidth;
uniform vec3 uAccent;
uniform float uMoonPhase;
out vec4 FragColor;
const float TAU = 6.2831853;
float segment(vec2 p, vec2 a, vec2 b)
{
    vec2 ab = b - a;
    return length(p - a - ab * clamp(dot(p-a,ab)/dot(ab,ab),0.0,1.0));
}
float mask(float d, float w, float aa)
{
    return 1.0 - smoothstep(w-aa,w+aa,d);
}
vec2 direction(float a) { return vec2(sin(a),cos(a)); }
// Roman numerals constructed entirely from line segments.
float romanGlyph(vec2 q, int glyph, float aa)
{
    float d;
    if (glyph == 0)
        d = segment(q, vec2(0,-0.5), vec2(0,0.5)); // I
    else if (glyph == 1)
        d = min(segment(q,vec2(-0.3,0.5),vec2(0,-0.5)),
                segment(q,vec2(0,-0.5),vec2(0.3,0.5))); // V
    else
        d = min(segment(q,vec2(-0.3,-0.5),vec2(0.3,0.5)),
                segment(q,vec2(-0.3,0.5),vec2(0.3,-0.5))); // X
    return mask(d,0.065,aa);
}
float roman(vec2 p, int hour, float aa)
{
    // Encodings use base 3: I=0, V=1, X=2; length preserves leading I.
    const int lengths[12] = int[12](1,2,3,2,1,2,3,4,2,1,2,3);
    const int codes[12] = int[12](0,0,0,1,1,3,9,27,2,2,6,18);
    int n = lengths[hour-1];
    int code = codes[hour-1];
    vec2 q = (p-direction(float(hour)*TAU/12.0)*0.60)/0.09;
    float a = 0.0;
    for (int j=0; j<n; ++j)
    {
        float x = (float(n-1)*0.5-float(j))*0.66;
        a = max(a,romanGlyph(q-vec2(x,0),code%3,aa/0.09));
        code /= 3;
    }
    return a;
}
void main()
{
    if (uShowUV) { FragColor = vec4(vUV,0,1); return; }
    vec2 p = (vUV - 0.5) * 2.0;
    float r = length(p);
    float aa = max(fwidth(r), 0.001);
    float alpha = mask(r,0.96,aa);
    vec3 face = mix(vec3(0.95,0.93,0.87),vec3(0.055,0.08,0.14),uNight);
    vec3 ink = mix(vec3(0.10,0.12,0.15),vec3(0.86,0.90,0.97),uNight);
    vec3 rim = mix(vec3(0.25,0.28,0.32),vec3(0.32,0.40,0.55),uNight);
    vec3 color = mix(face,rim,smoothstep(0.96-uRimWidth-aa,0.96-uRimWidth+aa,r));
    for (int i=0; i<60; ++i)
    {
        bool major = i%5==0;
        vec2 dir = direction(float(i)*TAU/60.0);
        float d = segment(p,dir*(major?0.68:0.79),dir*0.85);
        color = mix(color,ink,mask(d,uTickWidth*(major?1.7:0.75),aa));
    }
    for (int h=1; h<=12; ++h)
        color = mix(color,ink,roman(p,h,aa));

    // Moon window: illuminated hemisphere projected onto the lunar disc.
    vec2 m = p - vec2(0,-0.34);
    float mr = length(m);
    color = mix(color,rim,mask(mr,0.185,aa));
    color = mix(color,vec3(0.025,0.045,0.10),mask(mr,0.165,aa));
    vec2 lunar = m / 0.125;
    float z = sqrt(max(0.0,1.0-dot(lunar,lunar)));
    float light = lunar.x*sin(uMoonPhase*TAU)-z*cos(uMoonPhase*TAU);
    float lit = smoothstep(-0.04,0.04,light);
    vec3 moon = mix(vec3(0.10,0.13,0.21),vec3(0.96,0.89,0.67),lit);
    color = mix(color,moon,mask(mr,0.125,aa));

    color = mix(color,ink,mask(segment(p,vec2(0),direction(uHour)*0.48),0.035,aa));
    color = mix(color,ink,mask(segment(p,vec2(0),direction(uMinute)*0.70),0.022,aa));
    color = mix(color,uAccent,mask(segment(p,-direction(uSecond)*0.13,direction(uSecond)*0.79),0.009,aa));
    color = mix(color,uAccent,mask(r,0.045,aa));
    FragColor = vec4(color,alpha);
}
