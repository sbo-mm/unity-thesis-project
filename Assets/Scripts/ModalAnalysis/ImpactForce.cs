using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

namespace ModalAnalysis
{
    public class ImpactForce : ForceProfile, IAudioForce
    {
        // Private nested structures
        class Impact
        {
            public float volume;
            public float decay;

            private float lpf = 0;
            private float bpf = 0;
            private readonly float cut;
            private readonly float bw;

            public Impact()
            {
                cut = 2.0f * Mathf.Sin(0.25f * Mathf.PI * CUTOFF);
                bw  = BANDWIDTH;
            }

            public float Process(FastRandom random)
            {
                volume *= decay;
                lpf += cut * bpf;
                bpf += cut * (random.GetRandom(MINAMP, MAXAMP) - lpf - bpf * bw);
                return bpf * volume;
            }
        }

        private const int MAXIMPACTS = 200;

        private int numimpacts;
        private Impact[] impacts;
        private ConcurrentQueue<Impact> impactrb;

        // Setup variables
        private const float CUTOFF = 0.5f;
        private const float BANDWIDTH = 0.9f;

        private const float MINAMP = -1.0f;
        private const float MAXAMP = 1.0f;

        private const float THR_DB = -80.0f;
        private const float DECAYT = 0.15f;

        private const float IGAIN = 0.5f * (1.0f - BANDWIDTH * BANDWIDTH);

        private readonly float thr;
        private float decayConst;

        private readonly FastRandom random;

        public ImpactForce(float sampleRate) : base(sampleRate)
        {
            random = new FastRandom();

            impacts = new Impact[MAXIMPACTS];
            impactrb = new ConcurrentQueue<Impact>();

            decayConst = -1.0f / sampleRate;
            decayConst = Mathf.Exp(decayConst / DECAYT);
            thr = Mathf.Pow(10.0f, THR_DB * 0.05f);
        }

        public void AddImpact(float volume)
        {
            Impact imp = new Impact
            {
                volume = volume * IGAIN,
                decay = decayConst
            };
            impactrb.Enqueue(imp);
        }

        public void GetForce(float[] output, int nsamples)
        {
            Array.Clear(output, 0, nsamples);

            while (impactrb.TryDequeue(out Impact imp))
            {
                if (numimpacts < MAXIMPACTS)
                    impacts[numimpacts++] = imp;
            }

            int i = 0;
            while (i < numimpacts)
            {
                Impact imp = impacts[i];
                for (uint n = 0; n < nsamples; n++)
                {
                    float impact = imp.Process(random);
                    output[n] += impact;
                }
                if (imp.volume < thr)
                {
                    imp = impacts[--numimpacts];
                }
                else
                    ++i;
            }
        }
    }

    public class FastRandom
    {
        private ulong seed = 0;

        public FastRandom()
        {
            seed = (ulong)(new System.Random()).Next();
        }

        private float GetRandom()
        {
            const float scale = 1.0f / (float)0x7FFFFFFF;
            seed = seed * 69069 + 1;
            return (((seed >> 16) ^ seed) & 0x7FFFFFFF) * scale;
        }

        public float GetRandom(float minVal, float maxVal)
        {
            return minVal + (maxVal - minVal) * GetRandom();
        }
    }

}