 
    
    float noise_hash(float2 p)  // replace this by something better
    {
        p  = 50.0*frac( p*0.3183099 + float2(0.71,0.113));
        return -1.0+2.0*frac( p.x*p.y*(p.x+p.y) );
    }
    float noise(float2 p)
    {
        float2 i = floor( p );
        float2 f = frac( p );
        
        float2 u = f*f*(3.0-2.0*f);

        return lerp( lerp( noise_hash( i + float2(0.0,0.0) ), 
                        noise_hash( i + float2(1.0,0.0) ), u.x),
                    lerp( noise_hash( i + float2(0.0,1.0) ), 
                        noise_hash( i + float2(1.0,1.0) ), u.x), u.y);
    }

    #include    "UnityCG.cginc"
    #include    "Lighting.cginc"
    #pragma     multi_compile_fwdbase
    #pragma     multi_compile_instancing
    #include    "AutoLight.cginc"
    #pragma     multi_compile_fog
    float       _WindStrength;
    float       _MoveSpeed;
    float       _HeightVariance;
    float       _RandPosVariance;
    float       _BaseHeight;
    float4      _MainColor;
    float4      _SecondColor;
    float       _ShadowHarshness;
    float       _GradientMix;
    float4      _CamWorldPos;
    float       _MaxDistance;
    float       _FadeStrength;
    float4      _DepthBlendColor;
    float       _DepthBlendStrength;
    float       _GlobalHeightMod;

    struct MeshProperties {
        float4x4    matrice;
        float4      worldPosition;
        float4      uv;
        float4      color;
        float       alpha;
    };
    
    struct v2f
    {
        float2 uv               : TEXCOORD0;
        SHADOW_COORDS(1)        // put shadows data into TEXCOORD1
        fixed3 diff             : COLOR0;
        fixed3 ambient          : COLOR1;
        float4 pos              : SV_POSITION;
        UNITY_FOG_COORDS(2)
        float4 worldPosition    : TEXCOORD4;
        float4 distanceFromCam  : TEXCOORD5;
        float4 alpha            : TEXCOORD6;
    };

    StructuredBuffer<MeshProperties> _Properties;

    float4 worldPos;

    float setHeight(float height, float posY) {
        return smoothstep(0, 1, posY) * height;
    }

    float getWind(float height, float posY) {
        return sin(_Time * (_MoveSpeed - height)) * _WindStrength * smoothstep(0, 4.0, posY); // test this later: * cos(_Time * 22.32423)
    }

    float randFromInstanceID(float instanceID) {
        float2 noise = (frac(sin(dot(instanceID + 1, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
        return abs(noise.x + noise.y) * 0.5;
    }

    float4 RotateVertexY(float4 vertex, float degrees)
    {
        float halfEuler                 = 180.0;
        float alpha                     = degrees * UNITY_PI / halfEuler;
        float sinValue;
        float cosValue;
        sincos(alpha, sinValue, cosValue);
        float2x2 matrice2by2            = float2x2(cosValue, -sinValue, sinValue, cosValue);
        return float4(mul(matrice2by2, vertex.xz), vertex.yw).xzyw;
    }


 
