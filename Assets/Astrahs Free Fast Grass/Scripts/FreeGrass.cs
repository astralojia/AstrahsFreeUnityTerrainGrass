using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Threading.Tasks;
using System.Threading;


//Lots of static variables because Unity's job system likes that!!

namespace Astrah
{

    public class FreeGrass : MonoBehaviour
    {

            public static FreeGrass fg;
            public bool UpdBigArrays = true;

         // - [ Serialized Values ]
            // - [ Splat Map Grass Threshold ]
            [Range(0.6f,0.95f)]
            public float SplatMapGrassThreshold = 0.8f;
            // - [ Grass Blades Per Row ]
            public enum BladesPerRowSquare { SixtyFour, TwoHundredFiftySix, FiveHundredTwentyNine, NineHundred, OneFourFourFour, TwoThousandTwentyFive }
            public int bladesPerRow = 20;
            // - [ Cull Distance ]
            public float cullDistance = 7f;
            // - [ Fade Strength ]
            [Range(0.02f, 0.98f)]
            public float fadeStrength = 0.5f;
            // - [ Mesh Scale ]
            public float meshScale = 0.125f;
            // - [ Terrain ]
            public Terrain terrain;
            // - [ Shadowmap Values ]
            public Light mainLight;
            // - [ Main Cam ]
            public Camera mainCam;
            // - [ Random Pos Offset Strength ]
            [Range(0.01f, 10.00f)]
            public float randomPosOffsetStrength = 1.0f;
        // - [ Private Values ]
        [HideInInspector]
        public Mesh mesh;
        [HideInInspector]
        public Material material;
        [HideInInspector]
        public Bounds bounds;
        [HideInInspector]
        public ComputeBuffer cB_meshProperties;
        [HideInInspector]
        public ComputeBuffer cB_Args;
        [HideInInspector]
        public int ObjCount;
        [HideInInspector]
        public RenderTexture shadowMapRenderTexture;
        [HideInInspector]
        public CommandBuffer mainLightCommandBuffer;
        // public static Color[,] MP_splatMapArray; // for Unity Job system (can't to GetPixel() in other thread)
            //THIS IS A CUSTOM TERRAIN HEIGHT MAP NOT UNITY TERRAIN'S BUILT IN ONE!
        public static Color[] MP_splatMapArray1D;
        // public static float[,] MP_heightMapArray; // for Unity Job system (can't use SampleHeight() in other thread)

        public static float[] MP_heightMapArray1D; //array[x * xWidth + y]
        public static Texture2D splatMapTexture;
        // - [ Get 'MG_Camera' ]
        
        private bool SetMG_Camera()
        {
            if (mainCam.cameraType != CameraType.Game) { Debug.LogError("Camera is not a game camera! Please make it into a game camera!"); return false; };
            if (mainCam.orthographic == true) { Debug.LogError("Your camera is set to orthographic! This isn't supported!"); return false; };
            return true;
        }
        // - [ Get Main Light ]
        private bool SetMainLight()  {
            if (mainLight == null) { Debug.LogError("Couldn't get component of 'Light' from 'MainLight' game Object! Please make sure your main light game object has a light component on it!"); return false; }
            if (mainLight.type != LightType.Directional) { Debug.LogError("No directional light on main light component in 'MainLight' game object! Please make sure it's a directional light!");  }
            return true;
        }
        // - [ Null Value Check ]
        private bool AValueIsNullOrInvalid()  {
            if (mesh == null) { Debug.LogError("mesh = NULL!"); return true; }
            if (material == null) { Debug.LogError("material = NULL!"); return true; }
            if (bounds == null) { Debug.LogError("bounds = NULL!"); return true; }
            if (cB_Args == null) { Debug.LogError("cB_Args = NULL!"); return true; }
            return false;
        }
        // - [ Get Mesh ]
        private Mesh GetMesh() { return gameObject.transform.Find("MeshToInstance").GetComponent<MeshFilter>().mesh; }
        // - [ Get Material ]
        private Material GetMeshMaterial() { return gameObject.transform.Find("MeshToInstance").GetComponent<MeshRenderer>().material; }
        // - [ Get Bounds ]
        private Bounds GetBounds() { return new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f)); }
        // - [ Is Square Number ]
        private bool isSquareNumber(int number) {return Mathf.Sqrt(number) % 1 == 0; }
        // - [ Init Variables and Compute Buffers ]
        private IEnumerator Init() {
            if (SetMG_Camera() == false) { Debug.LogError("No main camera set! You need to set a main camera!"); yield break; }
            if (SetMainLight() == false) { Debug.LogError("Terminating operation."); yield break; }
            ObjCount        = bladesPerRow * bladesPerRow; //squared for box...

            // - Global texture can be grabbed from shaders!
                // The idea is that we grab the value from our shaders '_SunCascadedShadowMap'. 
                // We can use this to get a global world position of the shadow map. This allows
                // us to color the blades of grass individually by their worldPos in the fragment function of 
                // our grass shader!
            mainLightCommandBuffer = new CommandBuffer();
            RenderTargetIdentifier shadowMapRenderTextureIdentifier = BuiltinRenderTextureType.CurrentActive;
            mainLightCommandBuffer.SetGlobalTexture("_SunCascadedShadowMap", shadowMapRenderTextureIdentifier);
            mainLight.AddCommandBuffer(LightEvent.AfterShadowMap, mainLightCommandBuffer);
           
            mesh            = GetMesh();
            material        = GetMeshMaterial();
            bounds          = GetBounds();

            if (AValueIsNullOrInvalid() == false)
                Inited = true;

            Buffers.UpdateBuffers(true);

            FreeGrass.fg.Inited = true;
            yield break; 
        }
        // - [ Feed In Camera World Pos ]
        private void FeedInCameraWorldPosToMaterial() {
            material.SetVector("_CamWorldPos", new Vector4(mainCam.transform.position.x, mainCam.transform.position.y, mainCam.transform.position.z, 1));
        }

        // - [ Update ]
        public bool Inited = false; //set by UpdateBuffers() to true.
        private void Update() {
            if (Inited  == false) { return; }
            if (mainCam == null) { return; }

            bounds      = GetBounds();

            //Update material according to camera view here with that long ass loop...

            if (cB_Args == null)
                return;
            if (cB_Args.count == 0)
                return;

            // ******************** ******** ********** ********** ********** ************
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, cB_Args, 0, null,
                                                UnityEngine.Rendering.ShadowCastingMode.On, 
                                                true, 0, null); 
                    //    ************************************ * * * *   *   *   *     *  


            FeedInCameraWorldPosToMaterial();

            mainCam.transform.Rotate(0f, Mathf.Sin(Time.deltaTime*10f), 0f, Space.Self);

        }
        // - [ Release Compute Buffers ]
        private void OnDisable() {
            Inited = false;
            try {
                mainLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, mainLightCommandBuffer);
                mainLightCommandBuffer.Clear();
            } catch {
                Debug.LogWarning("Couldn't remove command buffer?");
            }
        }
        // - [ Entry Point ]
        private void OnEnable() { 
            fg = this;
            StartCoroutine(Init()); 
        }

        private void OnApplicationQuit()
        {
            Buffers.meshPropertiesJob.splatMapArray1DNative.Dispose();
            Buffers.meshPropertiesJob.posMapArr1D.Dispose();
            Buffers.meshPropertiesJob.rotMapArr1D.Dispose();
            Buffers.meshPropertiesJob.szeMapArr1D.Dispose();
            Buffers.posMapArr1D.Dispose();
            Buffers.rotMapArr1D.Dispose();
            Buffers.szeMapArr1D.Dispose();
            Buffers.splatMapArray1DNative.Dispose();
        }

    }

}
