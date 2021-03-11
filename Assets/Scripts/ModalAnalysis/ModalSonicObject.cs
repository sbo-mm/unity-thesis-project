using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

namespace ModalAnalysis
{
    public class ModalSonicObject : SonicObject
    {
        // Properties
        public ModalModel Model { get; set; }
        public IAudioForce ImpactForceModel { get; set; }
        public IAudioForce FrictionForceModel { get; set; }

        // Private
        private FastRandom random;

        private float[] scratchBuffer;

        private int nf;
        private float[] twoRCosTheta;
        private float[] RSinTheta;
        private float[] R2;

        private float[] ampR;  // Filter gains
        private float[] pampR; // Previous gains

        private float[] yt_1; // Filter history
        private float[] yt_2; // ^^

        new private void Start()
        {
            base.Start();
            random = new FastRandom();

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

            ampR  = new float[nf]; // <-- gain is set on impact(s)
            pampR = new float[nf];

            yt_1 = new float[nf];
            yt_2 = new float[nf];
            
            MarkReadyForAudioRendering();
        }

        private float getGainAt(int idx)
        {
            int ix0 = (idx + 1) * 3 - 3;
            int ix1 = (idx + 1) * 3 - 2;
            int ix2 = (idx + 1) * 3 - 1;
            return Model.A[ix0] + Model.A[ix1] + Model.A[ix2];
        }

        private void SetGains(float[] gains, int npoints, int[] impactPoints, float[] weights)
        {
            //float oneOverN = 1.0f / (float)npoints;
            for (int n = 0; n < npoints * 3; n+=3)
            {
                for (int i = 0; i < nf; i++)
                {
                    int gainIdx0 = impactPoints[n + 0] * nf + i;
                    int gainIdx1 = impactPoints[n + 1] * nf + i;
                    int gainIdx2 = impactPoints[n + 2] * nf + i;
                    float g0 = getGainAt(gainIdx0);
                    float g1 = getGainAt(gainIdx1);
                    float g2 = getGainAt(gainIdx2);
                    float w0 = weights[n + 0];
                    float w1 = weights[n + 1];
                    float w2 = weights[n + 2];

                    float gain = g0 * w0 + g1 * w1 + g2 * w2;
                    gains[i] += gain * RSinTheta[i];
                }
            }
        }

        public void SetGains(int npoints, int[] impactPoints, float[] weights)
        {
            Array.Clear(ampR, 0, nf);
            SetGains(ampR, npoints, impactPoints, weights);
        }

        private void ComputeSoundBuffer(float[] output, float[] force, int nsamples)
        {
            Array.Clear(output, 0, nsamples);

            if (!pampR.SequenceEqual(ampR))
                Array.Copy(ampR, pampR, ampR.Length);

            float denormalFix = random.GetRandom(-1.0f, 1.0f) * 1.0e-9f;

            for (int i = 0; i < nf; i++)
            {
                float tmp_twoRCosTheta = twoRCosTheta[i];
                float tmp_R2 = R2[i];
                float tmp_a = pampR[i];
                float tmp_yt_1 = yt_1[i];
                float tmp_yt_2 = yt_2[i];
                for (int k = 0; k < nsamples; k++)
                {
                    float ynew = tmp_twoRCosTheta * tmp_yt_1 -
                        tmp_R2 * tmp_yt_2 + tmp_a * (force[k] + denormalFix);

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
            if (ImpactForceModel != null && FrictionForceModel != null)
            {
                ImpactForceModel.GetForce(scratchBuffer, nsamples);
                FrictionForceModel.GetForce(scratchBuffer, nsamples);
            }

            ComputeSoundBuffer(output, scratchBuffer, nsamples);
        }
    }
}