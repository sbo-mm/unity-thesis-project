using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModalAnalysis
{
    public class FrictionForce : ForceProfile, IAudioForce
    {
        public FrictionForce(float sampleRate) : base(sampleRate)
        {
        }

        public void GetForce(float[] output, int nsamples)
        {
            throw new System.NotImplementedException();
        }
    }
}


