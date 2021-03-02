using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModalAnalysis
{
    public class ModalSonicObject : SonicObject
    {
        // Unity Public (maybe temporary)
        [SerializeField]
        public ModalMaterialProperties MaterialProperties;

        // Properties
        public ModalModel Model { get; set; }
        public IAudioForce ForceModel { get; set; }

        // Private
        float[] scratchBuffer;

        private int nf;
        private float[] twoRCosTheta;
        private float[] RSinTheta;
        private float[] R2;
        private float[] ampR;
        private float[] yt_1;
        private float[] yt_2;

        private readonly object _gainLock = new object();

        new private void Start()
        {
            base.Start();
            scratchBuffer = new float[AudioQueueBufferSize];

            nf = Model.NumberOfModes;
            twoRCosTheta = new float[nf];
            RSinTheta = new float[nf];
            R2 = new float[nf];

            for (int i = 0; i < nf; i++)
            {
                float R = Mathf.Exp(Model.D[i] / SampleRate);
                float omega = Model.F[i] * 2.0f * Mathf.PI;
                float theta = omega / SampleRate;

                twoRCosTheta[i] = 2.0f * R * Mathf.Cos(theta);
                RSinTheta[i] = R * Mathf.Sin(theta);
                R2[i] = R * R;
            }

            ampR = new float[nf]; // <-- gain is set on impact(s)
            yt_1 = new float[nf];
            yt_2 = new float[nf];

            MarkReadyForAudioRendering();
        }

        private void SetGains(int npoints, int[,] impactPoints, float[,] weights)
        {
            float oneOverN = 1.0f / (float)npoints;
            for (int n = 0; n < npoints; n++)
            {
                for (int i = 0; i < nf; i++)
                {
                    int gainIdx0 = impactPoints[n, 0] * nf + i;
                    int gainIdx1 = impactPoints[n, 1] * nf + i;
                    int gainIdx2 = impactPoints[n, 2] * nf + i;
                    float w0 = weights[n, 0];
                    float w1 = weights[n, 1];
                    float w2 = weights[n, 2];

                    float gain = Model.A[gainIdx0] * w0 +
                        Model.A[gainIdx1] * w1 +
                        Model.A[gainIdx2] * w2;

                    ampR[i] += oneOverN * gain * RSinTheta[i];
                }
            }
        }

        public void SetGainsThreadsafe(int npoints, int[,] impactPoints, float[,] weights)
        {
            /*
            lock (_gainLock)
            {
                SetGains(npoints, impactPoints, weights);
            }
            */
            SetGains(npoints, impactPoints, weights);
        }

        private void ComputeSoundBuffer(float[] output, float[] force, int nsamples)
        {
            Array.Clear(output, 0, nsamples);

            for (int i = 0; i < nf; i++)
            {
                float tmp_twoRCosTheta = twoRCosTheta[i];
                float tmp_R2 = R2[i];
                float tmp_a = ampR[i];
                float tmp_yt_1 = yt_1[i];
                float tmp_yt_2 = yt_2[i];
                for (int k = 0; k < nsamples; k++)
                {
                    float ynew = tmp_twoRCosTheta * tmp_yt_1 -
                        tmp_R2 * tmp_yt_2 + tmp_a * force[k];

                    tmp_yt_2 = tmp_yt_1;
                    tmp_yt_1 = ynew;
                    output[k] += ynew;
                }
                yt_1[i] = tmp_yt_1;
                yt_2[i] = tmp_yt_2;
            }
        }

        /// <summary>
        /// Called repeatedly in a separate thread. 
        /// Consider threadsafe usage.
        /// </summary>
        public override void GetForce(float[] output, int nsamples)
        {
            if (ForceModel != null)
                ForceModel.GetForce(scratchBuffer, nsamples);

            ComputeSoundBuffer(output, scratchBuffer, nsamples);
        }
    }
}