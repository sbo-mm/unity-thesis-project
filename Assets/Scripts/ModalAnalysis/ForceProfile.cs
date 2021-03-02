using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModalAnalysis
{
    public abstract class ForceProfile
    {
        // Protected
        protected float sampleRate;

        protected ForceProfile(float sampleRate)
        {
            this.sampleRate = sampleRate;
        }

    }
}
