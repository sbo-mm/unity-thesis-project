using UnityEngine;

namespace ModalAnalysis
{
    public interface IAudioForce
    {
        void GetForce(float[] output, int nsamples);
    }
}

namespace UnityEnginePhysicsUtil
{
    public interface IKinematicParameters
    {
        float GetMaxSpeed();
        float GetMaxAcceleration();
        void SetMaxSpeed(float value);
        void SetMaxAcceleration(float value);
        bool IsGrounded();
    }
}