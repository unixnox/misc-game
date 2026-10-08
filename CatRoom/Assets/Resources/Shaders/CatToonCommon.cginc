// Shared cel-shading code for the Cat Room cartoon look (Built-in Render Pipeline).
#ifndef CAT_TOON_COMMON_INCLUDED
#define CAT_TOON_COMMON_INCLUDED

#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
fixed4 _Color;
fixed4 _ShadowColor;
fixed4 _RimColor;
fixed4 _Emission;
float _RampThreshold;
float _RampSmooth;

struct v2f_toon
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    SHADOW_COORDS(3)
    half3 ambient : TEXCOORD4;
};

v2f_toon vert_toon(appdata_base v)
{
    v2f_toon o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.ambient = ShadeSH9(half4(o.worldNormal, 1));
    TRANSFER_SHADOW(o);
    return o;
}

fixed4 frag_toon(v2f_toon i) : SV_Target
{
    float3 n = normalize(i.worldNormal);
    float3 l = normalize(_WorldSpaceLightPos0.xyz);
    float halfLambert = dot(n, l) * 0.5 + 0.5;
    fixed atten = SHADOW_ATTENUATION(i);
    float lit = smoothstep(_RampThreshold - _RampSmooth, _RampThreshold + _RampSmooth, halfLambert * lerp(0.35, 1.0, atten));

    fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
    fixed3 light = lerp(_ShadowColor.rgb, fixed3(1, 1, 1), lit) * _LightColor0.rgb;
    fixed3 col = albedo.rgb * (light + i.ambient * 0.25);

    float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
    float rim = pow(1.0 - saturate(dot(viewDir, n)), 3.0) * _RimColor.a * lit;
    col += _RimColor.rgb * rim;
    col += _Emission.rgb;
    return fixed4(col, 1);
}

struct v2f_shadow
{
    V2F_SHADOW_CASTER;
};

v2f_shadow vert_shadow(appdata_base v)
{
    v2f_shadow o;
    TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
    return o;
}

float4 frag_shadow(v2f_shadow i) : SV_Target
{
    SHADOW_CASTER_FRAGMENT(i)
}

#endif
