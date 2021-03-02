using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModalAnalysis
{
    public class ModalManager : MonoBehaviour
    {
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
                msobj.Model = respTask.Result.Data;
            }
        }
    }
}