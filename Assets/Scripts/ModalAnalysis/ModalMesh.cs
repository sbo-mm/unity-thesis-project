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
        // Static
        private static readonly MeshWelder welder = new MeshWelder();

        // Const
        private const int GEOMDOF = 3;

        // Private
        private SimpleMesh meshData;
        private Collider objCollider;
        private Transform objTransform;
        private KDTree<float, int> triTree;

        public SimpleMesh InitMesh()
        {
            Mesh realMesh = GetComponent<MeshFilter>().mesh;
            welder.SetMesh(realMesh);

            int[] weldedTriangles; Vector3[] weldedVertices;
            welder.WeldAndGet(out weldedVertices, out weldedTriangles);
            meshData = new SimpleMesh()
            {
                Vertices = weldedVertices,
                Triangles = weldedTriangles
            };

            return meshData;
        }

        private void Start()
        {
            objTransform = GetComponent<Transform>();
            objCollider = GetComponent<Collider>();

            int ntris = meshData.Triangles.Length / 3;
            float[][] barycenters = new float[ntris][];
            int[] kdnodes = new int[ntris];

            for (int i = 0, k = 0; i < meshData.Triangles.Length; i+=3, k++)
            {
                Vector3 l = meshData.Vertices[meshData.Triangles[i]];
                Vector3 m = meshData.Vertices[meshData.Triangles[i + 1]];
                Vector3 n = meshData.Vertices[meshData.Triangles[i + 2]];

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

        public (int[,], float[,]) GetCollisionVertices(Vector3[] collisionPoints)
        {
            int[,] triangles = new int[collisionPoints.Length, GEOMDOF];
            float[,] barycentricWeights = new float[collisionPoints.Length, GEOMDOF];

            for (int i = 0; i < collisionPoints.Length; i++)
            {
                Vector3 colpoint = collisionPoints[i];
                colpoint = objCollider.ClosestPointOnBounds(colpoint);
                colpoint = objTransform.InverseTransformPoint(colpoint);
                float[] querypoint = { colpoint[0], colpoint[1], colpoint[2] };

                var kdsearch_result = triTree.NearestNeighbors(querypoint, neighbors: 1);

                int triangleOffset = kdsearch_result[0].Item2;
                Vector3 p = meshData.Vertices[meshData.Triangles[triangleOffset]];
                Vector3 q = meshData.Vertices[meshData.Triangles[triangleOffset + 1]];
                Vector3 r = meshData.Vertices[meshData.Triangles[triangleOffset + 2]];

                float wa, wb, wc;
                GetBarycentricWeights(colpoint, p, q, r, out wa, out wb, out wc);

                barycentricWeights[i, 0] = wa;
                barycentricWeights[i, 1] = wb;
                barycentricWeights[i, 2] = wc;
                triangles[i, 0] = meshData.Triangles[triangleOffset];
                triangles[i, 1] = meshData.Triangles[triangleOffset + 1];
                triangles[i, 2] = meshData.Triangles[triangleOffset + 2];

                Debug.Log($"{name}: {wa}, {wb}, {wc}, {triangles[i, 0]}, {triangles[i, 1]}, {triangles[i, 2]}");
            }

            return (triangles, barycentricWeights);
        }
    }
}

