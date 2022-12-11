    v2f vert(appdata_full v, uint instanceID: SV_InstanceID)
    {

        float4 entireGrassPos = float4(0, 0, 0, 0);
    
        //Random heights...
        float heightHigher      = _BaseHeight * randFromInstanceID(instanceID) * _HeightVariance;
        v.vertex.y              += setHeight(heightHigher, v.vertex.y);
        float heightLower       = _BaseHeight * randFromInstanceID(instanceID*instanceID*instanceID) * _HeightVariance + (noise(float2(instanceID*3,instanceID*6))*0.5);
        v.vertex.y              -= setHeight(heightLower, v.vertex.y);

        ////Wind...
        v.vertex.x              += getWind(heightLower, v.vertex.y);
        v.vertex.z              += getWind(heightLower, v.vertex.y);

        //Global height mod
        v.vertex.y              *= _GlobalHeightMod;

        //Get entire grass pos...
        entireGrassPos          += mul(_Properties[instanceID].matrice, v.vertex);
        float4 uv               = _Properties[instanceID].uv;

        v2f o;
        o.pos = UnityObjectToClipPos(entireGrassPos);
        o.worldPosition = mul(unity_ObjectToWorld, entireGrassPos);
        o.uv.xy = v.texcoord;
        half3 worldNormal = UnityObjectToWorldNormal(v.normal);
        half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
        o.diff = nl * _LightColor0.rgb;
        o.ambient = ShadeSH9(half4(worldNormal, 1));
        o.alpha.x = _Properties[instanceID].alpha;

        TRANSFER_VERTEX_TO_FRAGMENT(o);
        UNITY_TRANSFER_FOG(o, o.pos);

        return o;

    }




    //Position variation...
   /* v.vertex.x              += randFromInstanceID(instanceID) * lerp(-_RandPosVariance, _RandPosVariance, clamp(randFromInstanceID(instanceID), 0, 1));*/
    //No z here right now?? Is that something to fix?

    //Offset positions randomly
   /* entireGrassPos.x    += randFromInstanceID(instanceID) * _RandPosVariance;
    entireGrassPos.x    -= randFromInstanceID(instanceID) * _RandPosVariance;
    entireGrassPos.z    += randFromInstanceID(instanceID) * _RandPosVariance;
    entireGrassPos.z    -= randFromInstanceID(instanceID) * _RandPosVariance;*/

    //Random rotation of blades...
/*     v.vertex = RotateVertexY(v.vertex, noise(float2(instanceID*3,instanceID*6)) * 360.0f);
     v.vertex = RotateVertexY(v.vertex, randFromInstanceID(instanceID * randFromInstanceID(instanceID * instanceID)) * UNITY_PI);*/
