using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ModalAnalysis
{
    public class ModalManager : MonoBehaviour
    {
        /*
#if UNITY_STANDALONE_OSX
        public const int 
            EXTFUN_NULL    = -1,
            EXTFUN_NOTINIT = -2,
            EXTFUN_SUCCES  =  1;

        [DllImport("AudioPluginModalSynth")]
        public unsafe static extern int VariableModalFilter_SetModelParams(
            int index,
            int nmodes,
            int nverts,
            float* freqs,
            float* decays,
            float* gains
        );

        [DllImport("AudioPluginModalSynth")]
        public unsafe static extern int VariableModalFilter_SetGains(
            int index,
            int npoints,
            int* impactPoints,
            float* weights
        );

        [DllImport("AudioPluginModalSynth")]
        public static extern int ImpactGenerator_AddImpact(
            int index, 
            float volume, 
            float decay, 
            float cut, 
            float bw
        );
#endif
        */
        // Unity public
        [SerializeField]
        public SonicObjectParameters[] Parameters;

        // Const
        private const string SOBTAG = "SonicObject";

        // Private
        private APIManager api;
        //private GameObject[] sonicObjects;

        void Awake()
        {
            api = new APIManager();

            // Setup all sonic objects
            for (int i = 0; i < Parameters.Length; i++)
            {
                // Set an index which is used by the 
                // sonic object to control the audio plugins
                int objectIdx = i;

                // Fetch the GameObject which represents 
                // our sonic object
                GameObject sobj = Parameters[i].SonicObject;

                // Get the modified (simplified) mesh which
                // holds info to pass to the API
                ModalMesh mesh = sobj.GetComponent<ModalMesh>();
                SimpleMesh simpleMesh = mesh.InitMesh();

                // Get the (modal) material properties
                ModalMaterialProperties props = Parameters[i].properties;

                // Call the API to fetch the modal model
                // TODO: make proper async
                var respTask = api.GetModalModelAsync(simpleMesh, props);
                ModalModel model = respTask.Result.Data;

                // Set the model on the corresponding object
                sobj.GetComponent<ModalSonicObject>().Model = model;

                // Attempt to call the native code and setup
                // properties for the modal filter plugin
                //NativeCall_SetupModalFilterParams(objectIdx, model);

                // (temporary) set the object index to the GameObject
                // sobj.GetComponent<CollisionHandler>().Native_ObjectIndex = objectIdx;
            }


            /*
            sonicObjects = GameObject.FindGameObjectsWithTag(SOBTAG);
            foreach (var sob in sonicObjects)
            {
                // Get the mesh
                ModalMesh mesh = sob.GetComponent<ModalMesh>();
                SimpleMesh simpleMesh = mesh.InitMesh();

                // Get the sonic object component
                ModalSonicObject msobj = sob.GetComponent<ModalSonicObject>();

                // Get the (modal) material properties
                ModalMaterialProperties props = msobj.MaterialProperties;

                // Call the API to fetch the modal model
                var respTask = api.GetModalModelAsync(simpleMesh, props);
                //msobj.Model = respTask.Result.Data;


                ModalModel model = respTask.Result.Data;

                int ret;
                unsafe
                {
                    float[] freqs = new float[model.F.Count];
                    model.F.CopyTo(freqs, 0);

                    float[] decays = new float[model.D.Count];
                    model.D.CopyTo(decays, 0);

                    float[] gains = new float[model.A.Count];
                    model.A.CopyTo(gains, 0);

                    fixed(float* _freqs = &freqs[0])
                    fixed(float* _decays = &decays[0])
                    fixed(float* _gains = &gains[0])
                    {
                        ret = VariableModalFilter_SetModelParams(
                            index: 0,
                            nmodes: model.NumberOfModes,
                            nverts: model.NumberOfVertices,
                            freqs: _freqs,
                            decays: _decays,
                            gains: _gains
                        );
                    }
                }
                */
            //Debug.Log($"native call ret code: {ret}");
        }

        /*
        private unsafe void NativeCall_SetupModalFilterParams(int index, ModalModel model)
        {
#if UNITY_STANDALONE_OSX

            // Variable to store native call return code
            int NRC;

            // Convert generic list to C# array
            IList2Array(model.F, out float[] freqs);
            IList2Array(model.D, out float[] decays);
            IList2Array(model.A, out float[] gains);

            // Fix the three arrays to a location in memory
            fixed (float* _freqs = &freqs[0])
            fixed (float* _decays = &decays[0])
            fixed (float* _gains = &gains[0])
            {
                // Call into the native plugin
                NRC = VariableModalFilter_SetModelParams(
                     index: index,
                     nmodes: model.NumberOfModes,
                     nverts: model.NumberOfVertices,
                     freqs: _freqs,
                     decays: _decays,
                     gains: _gains
                );
            }

            // Switch on the return code to ensure the call
            // was successful
            switch (NRC)
            {
                case EXTFUN_SUCCES:
                    return;
                case EXTFUN_NULL:
                    throw new System.NullReferenceException(
                        "Index of native object does not exist"
                        );
            }
#else
            return;
#endif
        }
        */
        private void IList2Array<T>(IList<T> src, out T[] dst)
        {
            dst = new T[src.Count];
            src.CopyTo(dst, 0);
        }

    }

    [System.Serializable]
    public struct SonicObjectParameters
    {
        public GameObject SonicObject;
        public ModalMaterialProperties properties;
    }


}