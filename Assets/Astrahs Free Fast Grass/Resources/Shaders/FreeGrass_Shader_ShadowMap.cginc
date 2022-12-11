UNITY_DECLARE_SHADOWMAP(_SunCascadedShadowMap);
float4 _SunCascadedShadowMap_TexelSize;

inline float4 getShadowCoord( float4 wpos, fixed4 cascadeWeights )
{
    float3 sc0                  = mul (unity_WorldToShadow[0], wpos).xyz;
    float3 sc1                  = mul (unity_WorldToShadow[1], wpos).xyz;
    float3 sc2                  = mul (unity_WorldToShadow[2], wpos).xyz;
    float3 sc3                  = mul (unity_WorldToShadow[3], wpos).xyz;
    float4 shadowMapCoordinate  = float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);
#if defined(UNITY_REVERSED_Z)
    float  noCascadeWeights     = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
    shadowMapCoordinate.z       += noCascadeWeights;
#endif
    return shadowMapCoordinate;
}

inline fixed4 getCascadeWeights_splitSpheres(float3 wpos)
{
    float3 fromCenter0          = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
    float3 fromCenter1          = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
    float3 fromCenter2          = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
    float3 fromCenter3          = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
    float4 distances2           = float4(dot(fromCenter0,fromCenter0), dot(fromCenter1,fromCenter1), dot(fromCenter2,fromCenter2), dot(fromCenter3,fromCenter3));
    fixed4 weights              = float4(distances2 < unity_ShadowSplitSqRadii);
    weights.yzw                 = saturate(weights.yzw - weights.xyz);
    return weights;
}

#define GET_CASCADE_WEIGHTS(wpos, z)    getCascadeWeights_splitSpheres(wpos)
#define GET_SHADOW_FADE(wpos, z)		getShadowFade_SplitSpheres(wpos)

#define GET_SHADOW_COORDINATES(wpos,cascadeWeights)	getShadowCoord(wpos,cascadeWeights)

half sampleTheShadowMap( float4 coord )
{
	half shadow                 = UNITY_SAMPLE_SHADOW(_SunCascadedShadowMap,coord);
	shadow                      = lerp(_LightShadowData.r, 1.0, shadow);
	return shadow;
}

half MainLight_ShadowAttenuation(float3 worldPositions, float screenDepth)
{
	fixed4 cascadeWeights       = GET_CASCADE_WEIGHTS(worldPositions.xyz, screenDepth);
	return sampleTheShadowMap(GET_SHADOW_COORDINATES(float4(worldPositions, 1), cascadeWeights));
}



