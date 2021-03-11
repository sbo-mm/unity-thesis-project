using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModalAnalysis
{
    [RequireComponent(typeof(Rigidbody), typeof(ModalMesh))]
    public class CollisionHandler : MonoBehaviour
    {
        [Range(0.1f, 10.0f)]
        public float SampleFrequency = 1.0f;

        [Range(-80.0f, 20.0f)]
        public float SampleLevel = 0.0f;

        //private float psamp, plevel;

        // Private
        private AudioManager audioManager;
        private Rigidbody objRigidbody;
        private ModalSonicObject sonicObject;
        private ModalMesh modalMesh;

        private ImpactForce impact;
        private FrictionForce friction;

        // Setup Variables
        private const int MAXCP = 10; 
        private ContactPoint[] contactPoints;
        private bool ready = false;

        private void Start()
        {
            audioManager = GameObject.Find("Manager")
                .GetComponent<AudioManager>();
            objRigidbody = GetComponent<Rigidbody>();
            sonicObject  = GetComponent<ModalSonicObject>();
            modalMesh    = GetComponent<ModalMesh>();

            impact = new ImpactForce(audioManager.SampleRate);
            friction = new FrictionForce(audioManager.SampleRate);
            contactPoints = new ContactPoint[MAXCP];

            sonicObject.ImpactForceModel = impact;
            sonicObject.FrictionForceModel = friction;

            friction.SetLevel(-80.0f);

            ready = true;


            float tvel = ((Physics.gravity.magnitude / objRigidbody.drag)
                - Time.fixedDeltaTime * Physics.gravity.magnitude) / objRigidbody.mass;
            Debug.Log($"{name}-> Terminal velocity: {tvel}");

        }

        private void Update()
        {
            friction.SetFrequencyPct(SampleFrequency);
            friction.SetLevel(SampleLevel);
        }

        private void SolveContacts(Collision collision, out int npoints)
        {
            if (collision.contactCount > MAXCP)
            {
                for (int i = 0; i < MAXCP; i++)
                {
                    contactPoints[i] = collision.GetContact(i);
                }
                npoints = MAXCP;
                return;
            }

            npoints = collision.contactCount;
            collision.GetContacts(contactPoints);
        }

        private Vector3[] GetQueryPointsArray(ContactPoint[] cps, int npoints)
        {
            Vector3[] queries = new Vector3[npoints];
            for (int i = 0; i < npoints; i++)
            {
                queries[i] = cps[i].point;
            }
            return queries;
        }

        private void SolveCollisionVertices(Collision collision)
        {
            // Solve collision contact points
            SolveContacts(collision, out int npoints);

            // Fetch every query point on the surface of the collider 
            var querypoints = GetQueryPointsArray(contactPoints, npoints);

            // Retrive the indeces of the vertices for each triangle
            // that has contact to the impact surface
            var collisionResult = modalMesh.GetCollisionVertices(querypoints);

            // Set the gains corresponding to the positions on the object we hit
            // Gains are set by attempting to call into the native audio plugin
            int[] points = collisionResult.Item1;
            float[] weights = collisionResult.Item2;
            sonicObject.SetGains(npoints, points, weights);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!ready)
                return;

            // Solve collision and set corresponding gains
            SolveCollisionVertices(collision);

            // Add an impact to to the impact generator
            float impactMag = collision.relativeVelocity.sqrMagnitude;
            float impactVelocity = objRigidbody.mass * impactMag;
            impact.AddImpact(impactVelocity);
        }
        
        private void OnCollisionStay(Collision collision)
        {
            if (!ready)
                return;
            //if (name == "SonicSphere")
            //    Debug.Log($"{name}: {objRigidbody.velocity.magnitude}, " +
            //    	$"{objRigidbody.velocity.sqrMagnitude}, " +
            //    	$"{objRigidbody.angularVelocity.magnitude}");



        }

    }
}
