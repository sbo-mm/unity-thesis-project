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
        public float Fscale { get; set; }

        [IgnoreDataMember]
        public float Dscale { get; set; }

        [IgnoreDataMember]
        public float Ascale { get; set; }

        public override string ToString()
        {
            return $"[{ModelId}]: modes: {NumberOfModes}, verts: {NumberOfVertices}";
        }
    }
   
    [System.Serializable]
    public struct ModalMaterialProperties
    {
        public float YoungsModulus;
        public float Thickness;
        public float Density;
        public double ViscoElasticDamping;
        public double FluidElasticDamping;
    }

    public struct SimpleMesh
    {
        public int[] Triangles;
        public Vector3[] Vertices;
    }
}