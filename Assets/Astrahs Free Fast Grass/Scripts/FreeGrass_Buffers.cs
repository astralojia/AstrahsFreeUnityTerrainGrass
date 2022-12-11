using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Threading.Tasks;

namespace Astrah
{

    public class Buffers
    {

        public static MeshPropertiesJob meshPropertiesJob = new MeshPropertiesJob();
        public static NativeArray<Vector4> splatMapArray1DNative;
        public static NativeArray<Vector3> posMapArr1D;
        public static NativeArray<Vector3> rotMapArr1D;
        public static NativeArray<Vector3> szeMapArr1D;

        public static bool objectsInCamsView = true;

        public static int MP_squObjCount;
        public static Vector3 MP_terPos;
        public static float MP_terSizeX;
        public static float MP_terSizeZ;
        public static int MP_heightMapRes;
        public static float MP_Xincrement;
        public static float MP_Zincrement;
        public static int MP_total;
        public static float MP_curIncrement_X;
        public static float MP_curIncrement_Z;
        public static Vector3 MP_meshFinalScale;
        public static Texture2D MP_splatMap;
        public static int MP_splatTextureWidth;
        public static int MP_splatTextureHeight;
        public static Vector3 MP_mainCamPosition;


        public static void UpdateBigArrays()
        {
            MP_splatTextureWidth = FreeGrass.fg.terrain.terrainData.GetAlphamapTexture(0).width;
            Texture2D splatMapTex = FreeGrass.fg.terrain.terrainData.GetAlphamapTexture(0);
            int arrayLength = MP_splatTextureWidth * MP_splatTextureWidth;
            splatMapArray1DNative = new Unity.Collections.NativeArray<Vector4>(arrayLength, Unity.Collections.Allocator.Persistent);

            float splatMapTime = Time.realtimeSinceStartup;

            // - S P L A T   M A P   T O   1 D   A R R A Y
            for (int x = 0; x < MP_splatTextureWidth; x++)
            {
                for (int y = 0; y < MP_splatTextureWidth; y++)
                {
                    int deb = x * MP_splatTextureWidth + y;
                    splatMapArray1DNative[x * MP_splatTextureWidth + y] = splatMapTex.GetPixel(x, y);
                }
            }

            //Debug.Log("SPLAT MAP TIME: " + ((Time.realtimeSinceStartup - splatMapTime) * 1000f) + "ms");

            float heightMapStartTime = Time.realtimeSinceStartup;

            //  - H E I G H T   M A P   T O  1 D   A R R A Y  (Not Default, Custom Height Map)
            //int htMpRes             = FreeGrass.fg.terrain.terrainData.heightmapResolution - 1;
            int bldPerRw = FreeGrass.fg.bladesPerRow;
            int ttlArSize = bldPerRw * bldPerRw;
            //hghtMapArr1D            = new NativeArray<float>(ttlArSize, Allocator.Persistent);
            posMapArr1D = new NativeArray<Vector3>(ttlArSize, Allocator.Persistent);
            rotMapArr1D = new NativeArray<Vector3>(ttlArSize, Allocator.Persistent);
            szeMapArr1D = new NativeArray<Vector3>(ttlArSize, Allocator.Persistent);
            float smpTer_Xinc = FreeGrass.fg.terrain.terrainData.size.x / (bldPerRw - 1);
            float smpTer_Zinc = FreeGrass.fg.terrain.terrainData.size.z / (bldPerRw - 1);
            float smpTer_XincCur = 0f;
            float smpTer_ZincCur = 0f;
            Terrain ter = FreeGrass.fg.terrain;
            Vector3 terPos = new Vector3(FreeGrass.fg.terrain.transform.position.x, 0f, FreeGrass.fg.terrain.transform.position.z);
            rotMapArr1D = new NativeArray<Vector3>(ttlArSize, Allocator.Persistent);

            for (int z = 0; z < bldPerRw; z++)
            {
                for (int x = 0; x < bldPerRw; x++)
                {
                    // - H E I G H T
                    Vector3 pos = terPos + new Vector3(smpTer_XincCur, 0f, smpTer_ZincCur);
                    int indm = x * bldPerRw + z;
                    //hghtMapArr1D[indm]   = ter.SampleHeight(pos);

                    // - P O S
                    posMapArr1D[indm] = new Vector3(pos.x, ter.SampleHeight(pos), pos.z);
                    rotMapArr1D[indm] = new Vector3(0f, UnityEngine.Random.Range(0f, 359f), 0f);
                    szeMapArr1D[indm] = new Vector3(1f, 1f, 1f) * FreeGrass.fg.meshScale;

                    smpTer_XincCur += smpTer_Xinc;
                }
                smpTer_ZincCur += smpTer_Zinc;
                smpTer_XincCur = 0;
            }


        }

        // - [ Initialize Mesh Properties Buffer (Terrain Version) ]
        public static void UpdateBuffers(bool updateBigArrays = false)
        {

            // - F I R S T   T I M E   I N I T
            if (updateBigArrays == true || FreeGrass.fg.UpdBigArrays == true)
            {
                UpdateBigArrays();
            }


            float startTime                                 = Time.realtimeSinceStartup;
            MP_squObjCount                                  = (int)Mathf.Sqrt(FreeGrass.fg.ObjCount);
            MP_terPos                                       = FreeGrass.fg.terrain.transform.position;
            MP_terSizeX                                     = FreeGrass.fg.terrain.terrainData.size.x;
            MP_terSizeZ                                     = FreeGrass.fg.terrain.terrainData.size.z;
            MP_heightMapRes                                 = FreeGrass.fg.terrain.terrainData.heightmapResolution;
            MP_Xincrement                                   = MP_terSizeX / (MP_squObjCount - 1);
            MP_Zincrement                                   = MP_terSizeZ / (MP_squObjCount - 1);
            MP_total                                        = 0;
            MP_curIncrement_X                               = 0f;
            MP_curIncrement_Z                               = 0f;
            MP_meshFinalScale                               = Vector3.one * FreeGrass.fg.meshScale;
            MP_mainCamPosition                              = FreeGrass.fg.mainCam.transform.position;
            meshPropertiesJob                               = new MeshPropertiesJob();
            meshPropertiesJob.meshProperties                = new NativeList<MeshPropertiesJob.MeshProperties>(0, Allocator.Persistent);
            meshPropertiesJob.splatMapArray1DNative         = new NativeArray<Vector4>(splatMapArray1DNative,Allocator.Persistent);
            meshPropertiesJob.bladesPerRow                  = FreeGrass.fg.bladesPerRow;
            meshPropertiesJob.terPos                        = MP_terPos;
            meshPropertiesJob.curIncrement_X                = MP_curIncrement_X;
            meshPropertiesJob.curIncrement_Z                = MP_curIncrement_Z;
            meshPropertiesJob.Xincrement                    = MP_Xincrement;
            meshPropertiesJob.Zincrement                    = MP_Zincrement;
            meshPropertiesJob.mainCamPosition               = FreeGrass.fg.mainCam.transform.position;
            meshPropertiesJob.meshFinalScale                = MP_meshFinalScale;
            meshPropertiesJob.cullDistance                  = FreeGrass.fg.cullDistance;
            meshPropertiesJob.SplatMapGrassThreshold        = FreeGrass.fg.SplatMapGrassThreshold;
            meshPropertiesJob.splatMapTextureWidth          = MP_splatTextureWidth;
            meshPropertiesJob.fadeStrength                  = FreeGrass.fg.fadeStrength;
            meshPropertiesJob.randomPosOffset_X             = UnityEngine.Random.Range(0.0f, 1.0f);
            meshPropertiesJob.randomPosOffset_Z             = UnityEngine.Random.Range(0.0f, 1.0f);
            meshPropertiesJob.randomPosOffsetStrength       = FreeGrass.fg.randomPosOffsetStrength;
            meshPropertiesJob.meshProperties                = new NativeList<MeshPropertiesJob.MeshProperties>(0, Allocator.Persistent);
            meshPropertiesJob.posMapArr1D                   = new NativeArray<Vector3>(posMapArr1D, Allocator.Persistent);
            meshPropertiesJob.rotMapArr1D                   = new NativeArray<Vector3>(rotMapArr1D, Allocator.Persistent);
            meshPropertiesJob.szeMapArr1D                   = new NativeArray<Vector3>(szeMapArr1D, Allocator.Persistent);

            startTime = Time.realtimeSinceStartup;

            JobHandle jobHandle                 = meshPropertiesJob.Schedule();
            jobHandle.Complete();

            //Debug.Log("UPDATE JOB RUN: " + ((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");

            startTime = Time.realtimeSinceStartup;

            if (meshPropertiesJob.meshProperties.Length > 0)
            {
                // - Set cB_meshProperties
                objectsInCamsView = true;
                FreeGrass.fg.cB_meshProperties = new ComputeBuffer(meshPropertiesJob.meshProperties.Length, MeshPropertiesJob.MeshProperties.Size());
                FreeGrass.fg.cB_meshProperties.SetData(meshPropertiesJob.meshProperties.ToArray());
                FreeGrass.fg.material.SetBuffer("_Properties", FreeGrass.fg.cB_meshProperties);
                // - Set cB_Args
                uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
                args[0] = (uint)FreeGrass.fg.mesh.GetIndexCount(0);         //0 - > Number of Triangle Indices
                args[1] = (uint)meshPropertiesJob.meshProperties.Length;                       //1 - > Object count to instantiate
                args[2] = (uint)FreeGrass.fg.mesh.GetIndexStart(0);
                args[3] = (uint)FreeGrass.fg.mesh.GetBaseVertex(0);
                FreeGrass.fg.cB_Args = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
                FreeGrass.fg.cB_Args.SetData(args);
            }

            Buffers.meshPropertiesJob.meshProperties.Dispose();

            //Debug.Log("UPDATE MESH PROPERTIES TO ARGS: " + ((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");

        }


        [BurstCompile(CompileSynchronously = false, OptimizeFor = OptimizeFor.Performance)]
    public struct MeshPropertiesJob : IJob
    {

        // - [ Mesh Properties Struct For Buffer ]
        public struct MeshProperties
        {
            public Matrix4x4 matrix;
            public Vector4 worldPosition;
            public Vector4 uv;
            public Vector4 color;
            public float alpha;
            public static int Size()
            {
                return
                    sizeof(float) * 4 * 4 +     //matrix
                    sizeof(float) * 4 +         //worldPos
                    sizeof(float) * 4 +         //uv
                    sizeof(float) * 4 +         //color
                    sizeof(float);              //alpha
            }
        }

        public float bladesPerRow;
        public Vector3 terPos;
        public float curIncrement_X;
        public float curIncrement_Z;
        public float Xincrement;
        public float Zincrement;
        public Vector3 mainCamPosition;
        public Vector3 meshFinalScale;
        public NativeList<MeshProperties> meshProperties;
        public NativeArray<Vector4> splatMapArray1DNative;
        public NativeArray<Vector3> posMapArr1D;
        public NativeArray<Vector3> rotMapArr1D;
        public NativeArray<Vector3> szeMapArr1D;
        public float SplatMapGrassThreshold;
        public float cullDistance;
        public int splatMapTextureWidth; //512 for example not 1293102391023123
        public float fadeStrength;
        public float randomPosOffset_X;
        public float randomPosOffset_Z;
        public float randomPosOffsetStrength;

        public void Execute()
        {

            for (float z = 0; z < bladesPerRow; z++) //yes we do put in 'bladesPer 'X'' here, that's not a mistake!
            {
                for (float x = 0; x < bladesPerRow; x++)
                {

                    // - - - H O W   T H I N G S   A R E   S T O R E D
                    // - > Pos: posMapArr1D[arrInd]
                    // - > Rot: rotMapArr1D[arrInd]
                    // - > Sze: rotMapArr1D[arrInd]

                    // - - - C U L L
                        // - - I F   D I S T A N C E
                        int arrInd = (int)x * (int)bladesPerRow + (int)z;
                        float distFromCam = Vector3.Distance(mainCamPosition, posMapArr1D[arrInd]);
                        if (distFromCam > cullDistance)
                        {
                            curIncrement_X += Xincrement; continue;
                        }
                        // - - I F  B E H I N D   C A M E R A
                        

                    // - - - O N L Y   O N   G R A S S
                    if (!isGrass(x, z))
                    {
                        curIncrement_X += Xincrement; continue;
                    }

                    //// - - - C U L L   B Y   A L P H A   F A D E 
                    float curDistP = distFromCam / cullDistance;
                    float curDistPInvert = (1.0f - curDistP) / 1.0f;
                    float alpha = Mathf.SmoothStep(0.0f, 1.0f, (curDistPInvert * Mathf.PI) / fadeStrength); //fade strength is 0.5f
                    alpha = Mathf.Clamp(alpha, 0f, 1f);
                    if (alpha <= 0f)
                    { curIncrement_X += Xincrement; continue; }

                    // - - - P O S I T I O N
                    Vector3 objPos                  = posMapArr1D[arrInd];

                    // - - - R O T A T I O N
                    Quaternion objQRotation         = Quaternion.Euler(rotMapArr1D[arrInd]);

                    // - - - S C A L E
                    Vector3 objScale                = szeMapArr1D[arrInd];

                    // - - - M E S H   P R O P E R T Y   O B J E C T
                    MeshProperties meshProperty     = new MeshProperties();
                    meshProperty.matrix             = Matrix4x4.TRS(objPos, objQRotation, objScale);
                    meshProperty.worldPosition      = objPos;
                    meshProperty.color              = new Color(0.0f, 0.6f, 0.6f, 1.0f);
                    meshProperty.alpha              = alpha;

                    // - - - T O   M E S H   P R O P E R T I E S
                    meshProperties.Add(meshProperty);
  
                    curIncrement_X += Xincrement;
                }
                curIncrement_Z += Zincrement;
                curIncrement_X = 0;
            }

        }

        public bool isGrass(float x, float z)
        {
                int curSplatToCheck_X = (int)(x / bladesPerRow * splatMapTextureWidth);
                int curSplatToCheck_Z = (int)(z / bladesPerRow * splatMapTextureWidth);
                int indexToCheck = curSplatToCheck_X * splatMapTextureWidth + curSplatToCheck_Z;
                Color splat = splatMapArray1DNative[indexToCheck];
                if (splat.r > SplatMapGrassThreshold && splat.g < 0.2 && splat.b < 0.2 && splat.a == 0.0)
                { return true; }
                else { return false; }
        }

    } //END IJOB

        //[BurstCompile]
        public struct MeshPropertiesJobParallel : IJobParallelFor
        {

            //// - [ Mesh Properties Struct For Buffer ]
            public struct MeshProperties
            {
                public Matrix4x4 matrix;
                public Vector4 worldPosition;
                public Vector4 uv;
                public Vector4 color;
                public float alpha;
                public static int Size()
                {
                    return
                        sizeof(float) * 4 * 4 +     //matrix
                        sizeof(float) * 4 +         //worldPos
                        sizeof(float) * 4 +         //uv
                        sizeof(float) * 4 +         //color
                        sizeof(float);              //alpha
                }
            }

            public NativeArray<MeshProperties> values;
            public void Execute(int index)
            {
                MeshProperties mp = values[index];

                values[index] = mp;
            }

        } //END IPARALELLFOR JOB CLASS

    } //END CLASS

} //END NAMESPACE





//public float bladesPerRow;
//public Vector3 terPos;
//public float curIncrement_X;
//public float curIncrement_Z;
//public float Xincrement;
//public float Zincrement;
//public Vector3 mainCamPosition;
//public Vector3 meshFinalScale;
//public NativeList<MeshProperties> meshProperties;
//public NativeArray<Vector4> splatMapArray1DNative;
//public NativeArray<Vector3> posMapArr1D;
//public NativeArray<Vector3> rotMapArr1D;
//public NativeArray<Vector3> szeMapArr1D;
//public float SplatMapGrassThreshold;
//public float cullDistance;
//public int splatMapTextureWidth; //512 for example not 1293102391023123
//public float fadeStrength;
//public float randomPosOffset_X;
//public float randomPosOffset_Z;
//public float randomPosOffsetStrength;

//public void Execute()
//{

//    for (float z = 0; z < bladesPerRow; z++) //yes we do put in 'bladesPer 'X'' here, that's not a mistake!
//    {
//        for (float x = 0; x < bladesPerRow; x++)
//        {

//            // - - - H O W   T H I N G S   A R E   S T O R E D
//            // - > Pos: posMapArr1D[arrInd]
//            // - > Rot: rotMapArr1D[arrInd]
//            // - > Sze: rotMapArr1D[arrInd]

//            // - - - C U L L
//            int arrInd = (int)x * (int)bladesPerRow + (int)z;
//            float distFromCam = Vector3.Distance(mainCamPosition, posMapArr1D[arrInd]);
//            if (distFromCam > cullDistance)
//            {
//                curIncrement_X += Xincrement; continue;
//            }

//            // - - - O N L Y   O N   G R A S S
//            if (!isGrass(x, z))
//            {
//                curIncrement_X += Xincrement; continue;
//            }

//            //// - - - C U L L   B Y   A L P H A   F A D E 
//            float curDistP = distFromCam / cullDistance;
//            float curDistPInvert = (1.0f - curDistP) / 1.0f;
//            float alpha = Mathf.SmoothStep(0.0f, 1.0f, (curDistPInvert * Mathf.PI) / fadeStrength); //fade strength is 0.5f
//            alpha = Mathf.Clamp(alpha, 0f, 1f);
//            if (alpha <= 0f)
//            { curIncrement_X += Xincrement; continue; }

//            // - - - P O S I T I O N
//            Vector3 objPos = posMapArr1D[arrInd];

//            // - - - R O T A T I O N
//            Quaternion objQRotation = Quaternion.Euler(rotMapArr1D[arrInd]);

//            // - - - S C A L E
//            Vector3 objScale = szeMapArr1D[arrInd];

//            // - - - M E S H   P R O P E R T Y   O B J E C T
//            MeshProperties meshProperty = new MeshProperties();
//            meshProperty.matrix = Matrix4x4.TRS(objPos, objQRotation, objScale);
//            meshProperty.worldPosition = objPos;
//            meshProperty.color = new Color(0.0f, 0.6f, 0.6f, 1.0f);
//            meshProperty.alpha = alpha;

//            // - - - T O   M E S H   P R O P E R T I E S
//            meshProperties.Add(meshProperty);

//            curIncrement_X += Xincrement;
//        }
//        curIncrement_Z += Zincrement;
//        curIncrement_X = 0;
//    }

//}

//public bool isGrass(float x, float z)
//{
//    int curSplatToCheck_X = (int)(x / bladesPerRow * splatMapTextureWidth);
//    int curSplatToCheck_Z = (int)(z / bladesPerRow * splatMapTextureWidth);
//    int indexToCheck = curSplatToCheck_X * splatMapTextureWidth + curSplatToCheck_Z;
//    Color splat = splatMapArray1DNative[indexToCheck];
//    if (splat.r > SplatMapGrassThreshold && splat.g < 0.2 && splat.b < 0.2 && splat.a == 0.0)
//    { return true; }
//    else { return false; }
//}







// - - - C U L L   B Y   J U S T   D I S T A N C E (faster than alpha fade)
//float distanceFromCamera = Vector3.Distance(new Vector3(mainCamPosition.x,0f,mainCamPosition.z), new Vector3(spawnPoint.x, 0f, spawnPoint.z));
//if (distanceFromCamera > cullDistance)
//{ curIncrement_X += Xincrement; continue; }

// - - - H E I G H T   T O   T E R R A I N
//int arrInd = (int)x * (int)bladesPerRow + (int)z;
//float sampleHeight = hghtMapArr1D[arrInd];
//spawnPoint = terPos + new Vector3(curIncrement_X, sampleHeight, curIncrement_Z);
