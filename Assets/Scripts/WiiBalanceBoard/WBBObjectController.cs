using System.Collections;
using System.Collections.Generic;
using ModalAnalysis;
using UnityEngine;
using UnityEngine.Events;
using UnityEnginePhysicsUtil;

namespace WiiBalanceBoard
{
    public class WBBObjectController : MonoBehaviour, IKinematicParameters
    {
        // Unity Fields
        [SerializeField, Range(0f, 100f)]
        float maxSpeed = 10f, maxAcceleration = 10f;

        [SerializeField, Range(0f, 90f)]
        float maxGroundAngle = 25f;

        [SerializeField]
        bool enableKeyboardControl = true;

        // Events
        [System.Serializable]
        public class UserEvent : UnityEvent<GameObject, CollisionEvent> { }

        // Public Properties
        public float MaxSpeed
        {
            get { return maxSpeed; }
            set { maxSpeed = Mathf.Clamp(value, 0f, 100f); }
        }

        public float MaxAcceleration
        {
            get { return maxAcceleration; }
            set { maxAcceleration = Mathf.Clamp(value, 0f, 100f); }
        }

        private UserEvent _onUserEnter;
        public UserEvent OnUserEnter
        {
            get
            {
                if (_onUserEnter == null)
                    _onUserEnter = new UserEvent();

                return _onUserEnter;
            }
        }

        // Private Properties
        private BalanceBoard Board { get { return wbbManager?.Board; } }

        // Private
        private Rigidbody body;
        private Transform objTransform;
        private WBBManager wbbManager;

        private Vector2 boardInput;
        private Vector3 velocity, desiredVelocity;

        private bool OnGround => groundContactCount > 0;

        private int groundContactCount;
        private float minGroundDotProduct;
        private int stepsSinceLastGrounded;
        private Vector3 contactNormal;

        private bool objReady = false;

        void OnValidate()
        {
            minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        }

        private void Awake()
        {
            objTransform = this.transform;
            body = GetComponent<Rigidbody>();
            OnValidate();
        }

        void Start()
        {
            wbbManager = 
                GameObject.Find("Manager")
                .GetComponent<WBBManager>();

            objReady = true;
        }

        private void Update()
        {
            if (!objReady)
                return;

            if (BalanceBoard.IsReceiving)
            {
                boardInput.x = Board.RotationX;
                boardInput.y = Board.RotationZ;
            }
            else
            {
                if (enableKeyboardControl)
                {
                    boardInput.x = Input.GetAxis("Horizontal");
                    boardInput.y = Input.GetAxis("Vertical");
                }
                else
                {
                    boardInput = Vector2.zero;
                }
            }

            boardInput = Vector2.ClampMagnitude(boardInput, 1.0f);
        }

        private void LateUpdate()
        {
            if (!objReady)
                return;

            /*
            if (body.velocity.magnitude > 0.01f)
            {
                Vector3 fromRotation = body.velocity.normalized;
                Vector3 toRotation = objTransform.forward;
                Vector3 rotateDirection =
                    Vector3.RotateTowards(fromRotation, toRotation, 0.25f * Time.deltaTime, 0f);

                objTransform.localRotation = Quaternion.LookRotation(rotateDirection);
            }
            */
        }

        private void FixedUpdate()
        {
            if (!objReady)
                return;

            UpdateState();
            AdjustVelocity();

            body.velocity = velocity;
            ClearState();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!objReady)
                return;

            EvaluateCollision(collision);
            EvaluateCollisionEvent(collision, CollisionEvent.ENTER);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!objReady)
                return;

            EvaluateCollisionEvent(collision, CollisionEvent.EXIT);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!objReady)
                return;

            EvaluateCollision(collision);
        }

        private void EvaluateCollisionEvent(Collision collision, CollisionEvent eventType)
        {
            GameObject other = collision.gameObject;

            if (other.tag == "Untagged" || string.IsNullOrEmpty(other.tag))
                return;
            if (OnUserEnter == null)
                return;
                
            OnUserEnter?.Invoke(other, eventType);
        }

        private void EvaluateCollision(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (normal.y >= minGroundDotProduct)
                {
                    groundContactCount += 1;
                    contactNormal += normal;
                }
            }
        }

        bool SnapToGround()
        {
            if (stepsSinceLastGrounded > 1)
                return false;
            if (!Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, 1f))
                return false;
            if (hit.normal.y < minGroundDotProduct)
                return false;

            groundContactCount = 1;
            contactNormal = hit.normal;
            float speed = velocity.magnitude;
            float dot = Vector3.Dot(velocity, hit.normal);

            if (dot > 0f)
                velocity = (velocity - hit.normal * dot).normalized * speed;

            return true;
        }

        private void UpdateState()
        {
            stepsSinceLastGrounded += 1;
            velocity = body.velocity;
            if (OnGround || SnapToGround())
            {
                stepsSinceLastGrounded = 0;
                if (groundContactCount > 1)
                {
                    contactNormal.Normalize();
                }
            }
            else
            {
                contactNormal = Vector3.up;
            }
        }

        private void ClearState()
        {
            groundContactCount = 0;
            contactNormal = Vector3.zero;
        }

        private void AdjustVelocity()
        {
            Vector3 xAxiz = Vector3.right;
            Vector3 zAxis = Vector3.forward;
            Vector3 adjustment = Vector3.zero;

            float acceleration = OnGround ? maxAcceleration : 0f;
            float maxSpeedChange = OnGround ? maxSpeed : 0f;

            adjustment.x =
                boardInput.x * maxSpeedChange - Vector3.Dot(velocity, xAxiz);
            adjustment.z =
                boardInput.y * maxSpeedChange - Vector3.Dot(velocity, zAxis);
            adjustment =
                Vector3.ClampMagnitude(adjustment, acceleration * Time.deltaTime);
                
            velocity += xAxiz * adjustment.x + zAxis * adjustment.z;
        }

        public void ResetAll()
        {
            ClearState();
            body.velocity *= 0f;
        }

        public float GetMaxSpeed()
        {
            return MaxSpeed;
        }

        public float GetMaxAcceleration()
        {
            return MaxAcceleration;
        }

        public void SetMaxSpeed(float value)
        {
            MaxSpeed = value;
        }

        public void SetMaxAcceleration(float value)
        {
            MaxAcceleration = value;
        }

        public bool IsGrounded()
        {
            return OnGround || SnapToGround();
        }
    }
}