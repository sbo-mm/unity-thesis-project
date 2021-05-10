using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Supercluster.KDTree;
using B83.MeshHelper;

namespace ModalAnalysis
{
    [RequireComponent(typeof(MeshFilter))]
    public class ModalMesh : MonoBehaviour
    {
        static private Dictionary<string, SimpleMesh> simpleMeshDict =
            new Dictionary<string, SimpleMesh>();
        static private readonly MeshWelder welder = new MeshWelder();

        // Const
        private const int GEOMDOF = 3;

        // Public Properties
        public SimpleMesh MeshData { get; set; }

        // Private
        //private SimpleMesh meshData;
        private Collider objCollider;
        private Transform objTransform;
        private KDTree<float, int> triTree;

        private void Awake()
        {
            Mesh realMesh = 
                GetComponent<MeshFilter>().sharedMesh;

            if (!simpleMeshDict.ContainsKey(realMesh.name))
            {
                welder.SetMesh(realMesh);
                int[] weldedTriangles;
                Vector3[] weldedVertices;
                welder.WeldAndGet(out weldedVertices, out weldedTriangles);
                var meshData = new SimpleMesh
                {
                    Vertices = weldedVertices,
                    Triangles = weldedTriangles,
                    MeshName = realMesh.name
                };

                simpleMeshDict.Add(realMesh.name, meshData);
            }

            MeshData = simpleMeshDict[realMesh.name];
        }

        private void Start()
        {
            objTransform = GetComponent<Transform>();
            objCollider = GetComponent<Collider>();

            int ntris = MeshData.Triangles.Length / 3;
            float[][] barycenters = new float[ntris][];
            int[] kdnodes = new int[ntris];

            for (int i = 0, k = 0; i < MeshData.Triangles.Length; i+=3, k++)
            {
                Vector3 l = MeshData.Vertices[MeshData.Triangles[i]];
                Vector3 m = MeshData.Vertices[MeshData.Triangles[i + 1]];
                Vector3 n = MeshData.Vertices[MeshData.Triangles[i + 2]];

                barycenters[k] = getBarycenter(l, m, n);
                kdnodes[k] = i;
            }

            triTree = new KDTree<float, int>(3, barycenters, kdnodes, (x, y) =>
            {
                float dist = 0.0f;
                for (int i = 0; i < x.Length; i++)
                {
                    dist += (x[i] - y[i]) * (x[i] - y[i]);
                }
                return dist;
            });
        }

        private float[] getBarycenter(Vector3 l, Vector3 m, Vector3 n)
        {
            Vector3 barycenter = (l + m + n) * 0.333f;
            return new float[] { barycenter[0], barycenter[1], barycenter[2] };
        }

        private void GetBarycentricWeights(Vector3 query,
            Vector3 p, Vector3 q, Vector3 r,
            out float wa, out float wb, out float wc)
        {
            Vector3 u = q - p;
            Vector3 v = r - p;
            Vector3 w = query - p;
            Vector3 n = Vector3.Cross(u, v);

            float oneOverNormSqr = 1.0f / Vector3.Dot(n, n);
            wc = Vector3.Dot(Vector3.Cross(u, w), n) * oneOverNormSqr;
            wb = Vector3.Dot(Vector3.Cross(w, v), n) * oneOverNormSqr;
            wa = 1.0f - wc - wb;
        }

        public (int[], float[]) GetCollisionVertices(Vector3[] collisionPoints)
        {
            int[] triangles = new int[collisionPoints.Length * GEOMDOF];
            float[] barycentricWeights = new float[collisionPoints.Length * GEOMDOF];

            for (int i = 0; i < collisionPoints.Length; i++)
            {
                Vector3 colpoint = collisionPoints[i];
                colpoint = objCollider.ClosestPointOnBounds(colpoint);
                colpoint = objTransform.InverseTransformPoint(colpoint);
                float[] querypoint = { colpoint[0], colpoint[1], colpoint[2] };

                var kdsearch_result = triTree.NearestNeighbors(querypoint, neighbors: 1);

                int triangleOffset = kdsearch_result[0].Item2;
                Vector3 p = MeshData.Vertices[MeshData.Triangles[triangleOffset]];
                Vector3 q = MeshData.Vertices[MeshData.Triangles[triangleOffset + 1]];
                Vector3 r = MeshData.Vertices[MeshData.Triangles[triangleOffset + 2]];

                float wa, wb, wc;
                GetBarycentricWeights(colpoint, p, q, r, out wa, out wb, out wc);

                int os = i * 3;
                barycentricWeights[os + 0] = wa;
                barycentricWeights[os + 1] = wb;
                barycentricWeights[os + 2] = wc;
                triangles[os + 0] = MeshData.Triangles[triangleOffset];
                triangles[os + 1] = MeshData.Triangles[triangleOffset + 1];
                triangles[os + 2] = MeshData.Triangles[triangleOffset + 2];
            }

            return (triangles, barycentricWeights);
        }
    }
}

