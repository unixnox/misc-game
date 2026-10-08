Shader "CatRoom/ToonOutline"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Tint", Color) = (0.74,0.68,0.86,1)
        _RampThreshold ("Ramp Threshold", Range(0,1)) = 0.5
        _RampSmooth ("Ramp Smoothness", Range(0.001,0.5)) = 0.04
        _RimColor ("Rim Color (A = strength)", Color) = (1,1,1,0.25)
        _Emission ("Emission", Color) = (0,0,0,0)
        _OutlineColor ("Outline Color", Color) = (0.28,0.19,0.16,1)
        _OutlineWidth ("Outline Width (world units)", Float) = 0.015
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // Inverted-hull outline: draw back faces pushed out along the normal.
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata_base v)
            {
                v2f o;
                float3 n = UnityObjectToWorldNormal(v.normal);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz + n * _OutlineWidth;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert_toon
            #pragma fragment frag_toon
            #pragma multi_compile_fwdbase
            #include "CatToonCommon.cginc"
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert_shadow
            #pragma fragment frag_shadow
            #pragma multi_compile_shadowcaster
            #include "CatToonCommon.cginc"
            ENDCG
        }
    }
    Fallback Off
}
