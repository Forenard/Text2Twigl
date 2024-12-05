precision highp float;
uniform vec2 resolution;
uniform vec2 mouse;
uniform float frame;
uniform float time;
uniform sampler2D backbuffer;
layout(location = 0) out vec4 outColor;

const int HEXCOUNT = 26;
const int TEXTCOUNT = 2;
const int MAXTEXTLEN = 17;
const uint[] HEX = uint[HEXCOUNT](0x00000000u,//Null
0x0E0021E0u,//こ(l)
0x03000070u,//こ(r)
0x884C2210u,//ん(l)
0x00005520u,//ん(r)
0x19119590u,//に(l)
0x4F4A60C0u,//ち(l)
0x07034210u,//ち(r)
0x1D11D3D0u,//は(l)
0x17111350u,//は(r)
0x35535550u,//R
0x00257160u,//e
0x00355550u,//n
0x00346560u,//a
0x00531110u,//r
0x44655560u,//d
0x444C21E0u,//と(l)
0x00300070u,//と(r)
0x8F9F9F80u,//申(l)
0x07474700u,//申(r)
0x222222C0u,//し(l)
0x00004210u,//し(r)
0x8F8FA9E0u,//ま(l)
0x07070160u,//ま(r)
0x0F42C0C0u,//す(l)
0x17111100u//す(r)
);
const int[] TEXT0 = int[MAXTEXTLEN](1,2,3,4,5,2,6,7,8,9,0,0,0,0,0,0,0);
const int[] TEXT1 = int[MAXTEXTLEN](10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,0);
int[] TEXTLEN = int[TEXTCOUNT](10,17);
int[MAXTEXTLEN] getText(int i)
{
    int[MAXTEXTLEN] t;
    if(i == 0)
        t = TEXT0;
    else if(i == 1)
        t = TEXT1;
    return t;
}

bool inuv(vec2 p)
{
    return all(lessThanEqual(vec2(0),p)) && all(lessThan(p,vec2(1)));
}
float printHalf(vec2 p,int t)
{
    ivec2 i = ivec2(p * vec2(4,8));
    return float(inuv(p) && ((HEX[t] >> (i.x + i.y * 4)) & 1u) == 1u);
}

void main()
{
    vec2 uv = gl_FragCoord.xy / resolution;
    vec3 c = vec3(0);
    uv *= vec2(MAXTEXTLEN,TEXTCOUNT);
    c += printHalf(fract(uv),getText(int(uv.y))[int(uv.x)]);
    outColor = vec4(c,1);
}