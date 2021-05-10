using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace ModalAnalysis
{
    public class ModalModel
    {
        //
        // Public Members that can be serialized
        //

        [DataMember(IsRequired = true, Name = "id")]
        public string ModelId { get; set; }

        [DataMember(IsRequired = true, Name = "modes")]
        public int NumberOfModes { get; set; }

        [DataMember(IsRequired = true, Name = "vertices")]
        public int NumberOfVertices { get; set; }

        [DataMember(IsRequired = true, Name = "freqs")]
        public IList<float> F { get; set; }

        [DataMember(IsRequired = true, Name = "decays")]
        public IList<float> D { get; set; }

        [DataMember(IsRequired = true, Name = "gains")]
        public IList<float> A { get; set; }

        //
        // Public properties
        //
        [IgnoreDataMember]
        public float Fscale { get; set; } = 1f;

        [IgnoreDataMember]
        public float Dscale { get; set; } = 1f;

        [IgnoreDataMember]
        public float Ascale { get; set; } = 1f;

        public override string ToString()
        {
            return $"[{ModelId}]: modes: {NumberOfModes}, verts: {NumberOfVertices}";
        }
    }
   
    public class ModelPresets
    {
        private static ModelPresets _instance = new ModelPresets();

        public static ModelPresets Instance
        {
            get { return _instance; }
        }

        private const string
            SMESH_SPHERE    = "Sphere",
            SMESH_CUBE      = "Cube",
            SMESH_CYLINDER  = "Cylinder"
        ;

        public Dictionary<string, List<ModalMaterialProperties>> Presets;
        private List<ModalMaterialProperties> _spherePresets;
        private List<ModalMaterialProperties> _cubePresets;

        private ModalMaterialProperties _sphere_wood =
            new ModalMaterialProperties
        {
            YoungsModulus = 1200000,
            Thickness = 100,
            Density = 10,
            ViscoElasticDamping = 4e-05,
            FluidElasticDamping = 5,
            MaterialName = "SphereWood"
        };

        private ModalMaterialProperties _cube_metal =
            new ModalMaterialProperties
        {
            YoungsModulus = 1200000,
            Thickness = 100,
            Density = 10,
            ViscoElasticDamping = 4e-08,
            FluidElasticDamping = 3,
            MaterialName = "CubeMetal"
        };

        private ModelPresets()
        {
            _spherePresets = new List<ModalMaterialProperties>
            {
                _sphere_wood
            };

            _cubePresets = new List<ModalMaterialProperties>
            {
                _cube_metal
            };

            Presets = new Dictionary<string, List<ModalMaterialProperties>>
            {
                [SMESH_SPHERE]      = _spherePresets,
                [SMESH_CUBE]        = _cubePresets,
                [SMESH_CYLINDER]    = null
            };
        }

        public int PresetsAvailableFor(string mesh_id)
        {
            if (!Presets.ContainsKey(mesh_id))
                return -1;

            if (Presets[mesh_id] == null)
                return 0;

            return Presets[mesh_id].Count;
        }

        /*
        public static Dictionary<string, List<ModalMaterialProperties>> Presets 
            = new Dictionary<string, List<ModalMaterialProperties>>
        {
            [SMESH_SPHERE]      = _spherePresets,
            [SMESH_CUBE]        = _cubePresets,
            [SMESH_CYLINDER]    = null
        };

        public static int PresetsAvailableFor(string mesh_id)
        {
            if (!Presets.ContainsKey(mesh_id))
                return -1;

            if (Presets[mesh_id] == null)
                return 0;

            return Presets[mesh_id].Count;
        }

        #region Sphere Properties
        private static List<ModalMaterialProperties> _spherePresets
            = new List<ModalMaterialProperties>
        {
            _sphere_wood, 
            _sphere_metal
        };

        private static ModalMaterialProperties _sphere_wood =
            new ModalMaterialProperties
        {
            YoungsModulus       = 1200000,
            Thickness           = 100,
            Density             = 10,
            ViscoElasticDamping = 4e-05,
            FluidElasticDamping = 5,
            MaterialName = "SphereWood"
        };

        private static ModalMaterialProperties _sphere_metal =
            new ModalMaterialProperties
        {
            YoungsModulus       = 100000,
            Thickness           = 100,
            Density             = 10,
            ViscoElasticDamping = 10e-15,
            FluidElasticDamping = 10,
            MaterialName = "SphereMetal"
        };
        #endregion

        #region Cube Properties
        private static List<ModalMaterialProperties> _cubePresets
            = new List<ModalMaterialProperties>
        {
            _cube_metal
        };

        private static ModalMaterialProperties _cube_metal =
            new ModalMaterialProperties
        {
            YoungsModulus = 1200000,
            Thickness = 100,
            Density = 10,
            ViscoElasticDamping = 4e-08,
            FluidElasticDamping = 3,
            MaterialName = "CubeMetal"
        };

        #endregion

        #region Cylinder Properties
        #endregion
        */
    }

    [System.Serializable]
    public struct ModalMaterialProperties
    {
        public float YoungsModulus;
        public float Thickness;
        public float Density;
        public double ViscoElasticDamping;
        public double FluidElasticDamping;

        public string MaterialName;
    }

    public struct SimpleMesh
    {
        public int[] Triangles;
        public Vector3[] Vertices;
        public string MeshName;
    }
}