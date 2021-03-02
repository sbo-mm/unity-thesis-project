using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Utf8Json;
using RestSharp;
using RestSharp.Serialization;
using UnityEngine;

namespace ModalAnalysis
{
    public class APIManager
    {
        // Const
        private const string LOCAL_URL  = "http://127.0.0.1:5000/";
        private const string REMOTE_URL = "";

        // Private
        private RestClient client;

        public APIManager()
        {
            SetupRestClient();
        }

        private void SetupRestClient()
        {
            client = new RestClient(LOCAL_URL);
            client.UseSerializer(() => Utf8JsonSerializerWrapper.Default);
            client.ThrowOnAnyError = true;
        }

        private IRestRequest MakePostRequest(object cont, string endpoint)
        {
            RestRequest req = new RestRequest(endpoint, Method.POST)
            {
                JsonSerializer = Utf8JsonSerializerWrapper.Default,
            };
            req.AddJsonBody(cont);
            return req;
        }

        public Task<IRestResponse<ModalModel>> GetModalModelAsync(SimpleMesh mesh, ModalMaterialProperties material)
        {
            var requestContent = GetRequestObject(mesh, material);
            var request = MakePostRequest(requestContent, "1");
            return client.ExecuteAsync<ModalModel>(request);
        }

        private ModalAPIRequestContent GetRequestObject(SimpleMesh mesh, ModalMaterialProperties material)
        {
            ModalAPIRequestContent cont = new ModalAPIRequestContent
            {
                Mesh = new ModalAPIMesh
                {
                    Triangles = mesh.Triangles,
                    Vertices  = mesh.Vertices
                },
                Material = new ModalAPIMaterial
                {
                    Youngs       = material.YoungsModulus,
                    Thickness    = material.Thickness,
                    Density      = material.Density,
                    ViscoDamping = material.ViscoElasticDamping,
                    FluidDamping = material.FluidElasticDamping
                }
            };

            return cont;
        }
    }

    public class ModalAPIRequestContent
    {
        [DataMember(Name = "mesh")]
        public ModalAPIMesh Mesh { get; set; }

        [DataMember(Name = "material")]
        public ModalAPIMaterial Material { get; set; }
    }

    public class ModalAPIMesh
    {
        [DataMember(Name = "triangles")]
        public IList<int> Triangles    { get; set; }

        [DataMember(Name = "vertices")]
        public IList<Vector3> Vertices { get; set; }
    }

    public class ModalAPIMaterial
    {
        [DataMember(Name = "youngs")]
        public float Youngs { get; set; }

        [DataMember(Name = "thickness")]
        public float Thickness { get; set; }

        [DataMember(Name = "density")]
        public float Density { get; set; }

        [DataMember(Name = "visco")]
        public double ViscoDamping { get; set; }

        [DataMember(Name = "fluid")]
        public double FluidDamping { get; set; }
    }

    public class Utf8JsonSerializerWrapper : IRestSerializer
    {
        // Singleton Instance
        private static Utf8JsonSerializerWrapper _default;
        public static Utf8JsonSerializerWrapper Default
        {
            get
            {
                if (_default == null)
                    _default = new Utf8JsonSerializerWrapper();

                return _default;
            }
        }

        // Interface Implementation
        public string ContentType { get; set; } = "application/json";
        public DataFormat DataFormat { get { return DataFormat.Json; } }
        public string[] SupportedContentTypes { get; } = { "application/json" };

        public string Serialize(Parameter parameter)
        {
            return Serialize(parameter.Value);
        }

        public T Deserialize<T>(IRestResponse response)
        {
            return JsonSerializer.Deserialize<T>(response.RawBytes);
        }

        public string Serialize(object obj)
        {
            byte[] ser = JsonSerializer.Serialize(obj);
            return System.Text.Encoding.UTF8.GetString(ser);
        }
    }

}