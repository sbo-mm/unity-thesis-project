using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModalAnalysis
{
    [RequireComponent(typeof(Rigidbody), typeof(ModalSonicObject), typeof(ModalMesh))]
    public class CollisionHandler : MonoBehaviour
    {
        // Private
        private AudioManager audioManager;
        private Rigidbody objRigidbody;
        private ModalSonicObject sonicObject;
        private ModalMesh modalMesh;

        private ImpactForce impact;
        private FrictionForce friction;

        // Variables
        private const int MAXCP = 10; 
        private ContactPoint[] contactPoints;

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

        private void OnCollisionEnter(Collision collision)
        {
            sonicObject.ForceModel = impact;

            int npoints;
            SolveContacts(collision, out npoints);

            var querypoints = GetQueryPointsArray(contactPoints, npoints);
            var collisionVerts = modalMesh.GetCollisionVertices(querypoints);
            sonicObject.SetGainsThreadsafe(npoints, collisionVerts.Item1, collisionVerts.Item2);

            float impactMag = collision.relativeVelocity.magnitude;
            float impactVelocity = objRigidbody.mass * impactMag * impactMag;
            //Debug.Log($"{name}: {impactVelocity}");
            impact.Hit(impactVelocity);
        }

        private void OnCollisionStay(Collision collision)
        {
            
        }

    }
}
