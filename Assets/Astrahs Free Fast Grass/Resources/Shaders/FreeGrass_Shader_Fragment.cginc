    //Vertex function (vert) is located in the Values.cginc file
    #pragma vertex vert
    #pragma fragment frag

    fixed4 frag(v2f i) : SV_Target
    {

        // - Color
        fixed4 col                      = _MainColor;

        // - Gradient
        col = lerp(col,_SecondColor,_GradientMix * i.uv.y);

        // - Shadows using 'GetSunShadowsAttenuation from the ShadowMap include file
        float zDepth                    = i.pos.z / i.pos.w;
        float shadow                    = MainLight_ShadowAttenuation(i.worldPosition.xyz,zDepth);
        fixed3 lighting                 = i.diff * shadow + i.ambient;
        col.rgb                         *= lighting;    
        
        // - Apply Unity Fog
        UNITY_APPLY_FOG(i.fogCoord, col);

        // - Alpha updated by C# script
        col.a = i.alpha;

        // - Depth Blend
        float timeX = i.worldPosition.x + _Time * _WindStrength * 15;
        float timeZ = i.worldPosition.z + _Time * _WindStrength * 12;
        col.rgb += noise(float2(timeX,timeZ)) * _DepthBlendColor * _DepthBlendStrength;
        col.rgb += noise(float2(timeX+50,timeZ+100)) * _DepthBlendColor * _DepthBlendStrength;
        
        return col;
    }