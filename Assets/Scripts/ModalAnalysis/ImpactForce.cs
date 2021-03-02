using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModalAnalysis
{
    public class ImpactForce : ForceProfile, IAudioForce
    {
        // Private
        private int forceSampleLength; 
        private float currentDuration;
        private float currentImpactVelocity;

        private int currentPosition;
        private bool processEndedFlag;

        // Setup variables
        private const float MINDUR = 0.05f;
        private const float MAXDUR = 0.05f;
        private const float MINAMP = -1.0f;
        private const float MAXAMP =  1.0f;
        private ulong seed = 0;

        public ImpactForce(float sampleRate) : base(sampleRate)
        {
            var r = new System.Random();
            seed = (ulong)r.Next();

            processEndedFlag = true;
        }

        public void Hit(float impactVelocity)
        {
            currentPosition = 0;
            currentDuration = GetRandom(MINDUR, MAXDUR);
            forceSampleLength = (int)(currentDuration * sampleRate);
            currentImpactVelocity = impactVelocity;

            processEndedFlag = false;
        }

        public void GetForce(float[] output, int nsamples)
        {
            if (processEndedFlag)
                return;

            if (currentPosition < forceSampleLength)
            {
                int n = currentPosition;
                for (int i = 0; i < nsamples; i++, n++)
                {
                    float sample = GetRandom(MINAMP, MAXAMP);
                    float winval = HannSample(n, forceSampleLength);
                    output[i] = winval * sample * currentImpactVelocity;
                }
                currentPosition += nsamples;
            }
            else
            {
                Array.Clear(output, 0, nsamples);
                processEndedFlag = true;
            }
        }

        private float HannSample(int n, int N)
        {
            if (n < 0 || n > N)
                return 0.0f;

            float samp = n * Mathf.PI;
            float phi = Mathf.Sin(samp / N);
            return phi * phi;
        }

        private float GetRandom()
        {
            const float scale = 1.0f / (float)0x7FFFFFFF;
            seed = seed * 69069 + 1;
            return (((seed >> 16) ^ seed) & 0x7FFFFFFF) * scale;
        }

        private float GetRandom(float minVal, float maxVal)
        {
            return minVal + (maxVal - minVal) * GetRandom();
        }

    }
}