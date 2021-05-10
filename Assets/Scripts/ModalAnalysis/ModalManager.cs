using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using B83.MeshHelper;

namespace ModalAnalysis
{
    public class ModalManager : MonoBehaviour
    {
        // Private
        private APIManager api;
        private Dictionary<string, ModalModel> modalModelsDict;

        void Awake()
        {
            api = new APIManager();
            modalModelsDict = new Dictionary<string, ModalModel>();
        }

        public ModalModel GetModalModel(SimpleMesh simpleMesh, int propertyIndex = 0)
        {
            try
            {
                // Get the (modal) material properties
                string mesh_id = simpleMesh.MeshName;
                if (ModelPresets.Instance.PresetsAvailableFor(mesh_id) <= 0)
                {
                    return null;
                }

                var presets = ModelPresets.Instance.Presets[mesh_id];
                ModalMaterialProperties props = presets[propertyIndex];

                // Call the API to fetch the modal model
                // Only fetch if we have not computed a model
                // for a similar mesh with same props
                if (!modalModelsDict.ContainsKey(props.MaterialName))
                {
                    var respTask = api.GetModalModelAsync(simpleMesh, props);
                    modalModelsDict.Add(props.MaterialName, respTask.Result.Data);
                }

                ModalModel model = modalModelsDict[props.MaterialName];
                return model;
            }
            catch (System.Exception e)
            {
                Debug.Log(e.ToString());
                return null;
            }
        }

    }

    [System.Serializable]
    public struct SonicObjectParameters
    {
        public bool Include;
        public GameObject SonicObject;
        public ModalMaterialProperties properties;
    }


}