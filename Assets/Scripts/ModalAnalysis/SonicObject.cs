using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModalAnalysis
{
    public abstract class SonicObject : AudioHandler, IAudioForce
    {
        // Private
        private float[] outputBuffer;

        protected new void Start()
        {
            base.Start();
            outputBuffer = new float[AudioQueueBufferSize];
        }

        protected override void OnRequestSamples(float[] output, int nsamples)
        {
            GetForce(output, nsamples);
        }

        public void MarkReadyForAudioRendering()
        {
            AudioObjectReady = true;
        }

        public abstract void GetForce(float[] output, int nsamples);
    }
}