using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEnginePhysicsUtil;

namespace ModalAnalysis
{
    [RequireComponent(typeof(Rigidbody), typeof(ModalMesh), typeof(ModalSonicObject))]
    public class CollisionHandler : MonoBehaviour
    {
        // Unity Public
        [SerializeField]
        AudioClip frictionLoop;

        [SerializeField, Range(1f, 10f)]
        public float MaxFreqPct = 6.5f;

        [SerializeField, Range(-80f, 20f)]
        public float FrictionGainDb = 0f;

        [SerializeField, Range(0f, short.MaxValue)]
        public float ImpactGain = 350f;

        // Properties
        public float MaxSpeed
        {
            get { return kinematicUtil != null ? kinematicUtil.GetMaxSpeed() : 0; }
        }

        public float MaxAcceleration
        {
            get { return kinematicUtil != null ? kinematicUtil.GetMaxAcceleration(): 0; }
        }

        public bool OnGround
        {
            get { return kinematicUtil != null && kinematicUtil.IsGrounded(); }
        }

        public Vector3 CurrentVelocity
        {
            get { return objRigidbody.velocity; }
        }

        // Private
        private AudioManager audioManager;
        private Rigidbody objRigidbody;
        private ModalSonicObject sonicObject;
        private ModalMesh modalMesh;

        private SoftImpactForce softImpact;
        private FrictionForce friction;

        private IKinematicParameters kinematicUtil;

        // Setup Variables
        private const int MAXCP = 10; 
        private ContactPoint[] contactPoints;

        private const float MINIMPACTDURATION = 0.02f;
        private const float MAXIMPACTDURATION = 0.04f;

        private bool ready = false;

        private void Start()
        {
            audioManager = GameObject.Find("Manager")
                .GetComponent<AudioManager>();
            objRigidbody = GetComponent<Rigidbody>();
            sonicObject  = GetComponent<ModalSonicObject>();
            modalMesh    = GetComponent<ModalMesh>();

            kinematicUtil = GetComponent<IKinematicParameters>();

            audioManager.GetLoopSamplesForAudioClip(
                frictionLoop, out float[] audioLoop
            );

            softImpact = new SoftImpactForce(audioManager.SampleRate);
            friction = new FrictionForce(audioLoop, audioManager.SampleRate);

            contactPoints = new ContactPoint[MAXCP];

            sonicObject.ImpactForceModel = softImpact;
            sonicObject.FrictionForceModel = friction;

            // Basically turn of all audio from the 
            // friction generator
            friction.SetLevel(-80.0f);

            // Stupid flag to prevent unity
            // from executing the collision callbacks
            // before initialization is done
            ready = true;
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

            var other = collision.gameObject;

            if (other.GetComponent<Rigidbody>().isKinematic
                && objRigidbody.isKinematic)
            {
                return;
            }

            // Solve collision and set corresponding gains
            SolveCollisionVertices(collision);

            // Add an impact to to the impact generator
            float maxSpeed;
            if (objRigidbody.isKinematic)
            {
                CollisionHandler otherCollisionHandler =
                    other.GetComponent<CollisionHandler>();
                maxSpeed = otherCollisionHandler.MaxSpeed;
            }
            else
            {
                maxSpeed = MaxSpeed;
            }

            Vector3 relativeVelocity =
                collision.relativeVelocity;
            relativeVelocity =
                Vector3.ClampMagnitude(relativeVelocity, maxSpeed);

            float impactMag = relativeVelocity.magnitude;
            float linearImpactMag = impactMag / maxSpeed;
            float impactDuration =
                Mathf.Lerp(MINIMPACTDURATION, MAXIMPACTDURATION, linearImpactMag);

            softImpact.Hit(impactDuration, ImpactGain * impactMag);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!ready)
                return;

            var other = collision.gameObject;

            if (other.GetComponent<Rigidbody>().isKinematic
                && objRigidbody.isKinematic)
            {
                return;
            }

            // Solve collision and set corresponding gains
            SolveCollisionVertices(collision);

            float maxSpeed, currentSpeed;
            if (!objRigidbody.isKinematic)
            {
                // Check if only transient contanct
                if (!OnGround)
                    return;

                currentSpeed = CurrentVelocity.magnitude;
                maxSpeed = MaxSpeed;
            }
            else
            {
                CollisionHandler otherCollisionHandler =
                    other.GetComponent<CollisionHandler>();
                currentSpeed = 
                    otherCollisionHandler.CurrentVelocity.magnitude;
                maxSpeed =
                    otherCollisionHandler.MaxSpeed;
            }
            
            if (currentSpeed > 0.1f)
                friction.SetLevel(FrictionGainDb);

            float freqDelta =
                (currentSpeed / MaxSpeed) * Time.deltaTime;
            float freqScale =
                Mathf.MoveTowards(currentSpeed, MaxSpeed, freqDelta);
            float freqPct = 
                Mathf.Clamp01(freqScale / MaxSpeed);
            friction.SetFrequencyPct(freqPct * MaxFreqPct);
        }
    }
}
