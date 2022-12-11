//Vertex shader is in the Vertex.cginc, while all the fragment shader passes should go here. 

//Right now there is only one pass. It doesn't support point lights. When it's using the 
    // shadowmap from the main directional light...to be honest, I don't know that 
    // it's even possible for it to use point lights! Maybe if the first pass is disabled, 
    // there could be a point light mode. It MIGHT be possible to get point lights working...
    // Somehow....please e-mail me at astralojia@gmail.com if you get point lights working!!

Shader "Fast Free Grass/Grass"
{

    Properties
    {
        _MainColor("_MainColor", Color)                         = (0.10,0.35,0.11,1)
        _SecondColor("_SecondColor", Color)                     = (0.45,0.1,0.58,0.8)
        _DepthBlendColor("_DepthBlendColor", Color)             = (1,1,1,1)
        _DepthBlendStrength("_DepthBlendStrength", float)       = 1.0
        _GradientMix("_GradientMix", float)                     = 0.5
        _MoveSpeed("_MoveSpeed", float)                         = 50.0
        _WindStrength("_WindStrength", float)                   = 0.25
        _HeightVariance("_HeightVariance", float)               = 0.5
        _RandPosVariance("_RandPosVariance", float)             = 0.25
        _BaseHeight("_BaseHeight", float)                       = 0.5
        _ShadowHarshness("_ShadowHarshness", float)             = 0.8
        _CamWorldPos("_CamWorldPos",vector)                     = (1,1,1,1)
        _MaxDistance("_MaxDistance", float)                     = 12.0
        _FadeStrength("_FadeStrength", float)                   = 0.5
        _GlobalHeightMod("_GlobalHeightMod", float)             = 1.0
    }

    CGINCLUDE
        #include "FreeGrass_Shader_Values.cginc"
        #include "FreeGrass_Shader_Vertex.cginc" 
        #include "FreeGrass_Shader_ShadowMap.cginc"
    ENDCG

    SubShader
    {
      
        Pass { 

            Blend SrcAlpha OneMinusSrcAlpha

            Tags { "Queue" = "Overlay" "LightMode" = "ForwardBase" "RenderMode" = "Opaque"}

            CGPROGRAM

            #include "FreeGrass_Shader_Fragment.cginc"

            ENDCG

        } //END PASS


    } //END SUBSHADER

} //END SHADER