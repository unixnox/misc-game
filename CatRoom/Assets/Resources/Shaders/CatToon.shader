Shader "CatRoom/Toon"
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
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

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
