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
        private Thread synthesisThread;

        protected new void Start()
        {
            base.Start();
            outputBuffer = new float[AudioQueueBufferSize];
            synthesisThread = AudioManager.GetSynthesisThread(Run);
        }

        private void Run(object token)
        {
            CancellationToken ctToken 
                = (CancellationToken)token;

            while (true)
            {
                GetForce(outputBuffer, AudioQueueBufferSize);
                Write(outputBuffer);

                if (ctToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        public void MarkReadyForAudioRendering()
        {
            synthesisThread.Start();
            AudioObjectReady = true;
        }

        public abstract void GetForce(float[] output, int nsamples);
    }
}