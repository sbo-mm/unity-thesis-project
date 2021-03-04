using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ModalAnalysis
{
    public class ModalManager : MonoBehaviour
    {
    
#if UNITY_STANDALONE_OSX
        [DllImport("AudioPluginModalSynth")]
        private unsafe static extern int VariableModalFilter_SetModelParams(
            int index,
            int nmodes,
            int nverts,
            float* freqs,
            float* decays,
            float* gains
        );
#endif

        // Const
        private const string SOBTAG = "SonicObject";

        // Private
        private APIManager api;
        private GameObject[] sonicObjects;

        void Awake()
        {
            api = new APIManager();

            // Setup all sonic objects
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

                Debug.Log($"native call ret code: {ret}");
            }
        }
    }
}