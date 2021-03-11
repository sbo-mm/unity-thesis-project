using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ModalAnalysis
{
    public class FrictionForce : ForceProfile, IAudioForce
    {
        // Private
        private AudioClip clip;
        private FrictionTable ftable;

        private ConcurrentQueue<float> frequencyrb;
        private ConcurrentQueue<float> levelrb;

        // Const
        private float DBDIFF = 0.5f;
        private float FPCTDIFF = 0.1f;

        // RT Variables
        private float prevSetFreqPct;
        private float prevSetLevel;

        public FrictionForce(float sampleRate) : base(sampleRate)
        {
            clip = (AudioClip)Resources.Load("AudioFiles/SURFACE1");
            Debug.Assert(clip.channels == 1, "AudioClip contains >1 channels");

            ftable = new FrictionTable(clip, sampleRate);
            frequencyrb = new ConcurrentQueue<float>();
            levelrb = new ConcurrentQueue<float>();
        }

        public void SetFrequencyPct(float pct)
        {
            if (Mathf.Abs(prevSetFreqPct - pct) > FPCTDIFF)
            {
                frequencyrb.Enqueue(pct);
                prevSetFreqPct = pct;
            }
        }

        public void SetLevel(float levelDb)
        {
            if (Mathf.Abs(prevSetLevel - levelDb) > DBDIFF)
            {
                levelrb.Enqueue(levelDb);
                prevSetLevel = levelDb;
            }
        }

        public void GetForce(float[] output, int nsamples)
        {
            if (frequencyrb.TryDequeue(out float fpct))
                ftable.TargetFrequency = fpct;

            if (levelrb.TryDequeue(out float level))
                ftable.TargetLevel = level;

            ftable.GetNextAudioBlock(output, nsamples);
        }

        private class FrictionTable
        {
            // Unsafe 
            private readonly float[] waveTable;

            // Private
            private int numSamples;
            private float sampleRate;

            private float baseFrequency;
            private float currentFrequency;
            private float targetFrequency;

            private float baseLevel;
            private float currentLevel;
            private float targetLevel;

            private float currentIndex;
            private float tableDelta;
            private int tableSize;

            private const float DECAYT = 0.997f;
            private float decayConst;

            // Expose target frequency
            public float TargetFrequency
            {
                get { return targetFrequency;  }
                set
                {
                    float multiplier = Mathf.Clamp(value, 0.1f, 10.0f);
                    targetFrequency = multiplier * baseFrequency;
                }
            }

            // Expose desired level
            public float TargetLevel
            {
                get { return targetLevel; }
                set 
                {
                    float multiplier = Mathf.Pow(10.0f, value * 0.05f);
                    targetLevel = multiplier * baseLevel;
                    targetLevel = Mathf.Clamp(targetLevel, 0, short.MaxValue);
                    currentLevel = targetLevel;
                }
            }

            public FrictionTable(AudioClip src, float sampleRate)
            {
                this.sampleRate = sampleRate;

                tableSize = src.samples - 1;
                waveTable = new float[tableSize + 1];
                src.GetData(waveTable, 0);
                waveTable[tableSize] = waveTable[0];
                
                currentIndex = 0.0f;
                tableDelta   = 0.0f;

                baseLevel = 500.0f;
                currentLevel = targetLevel = baseLevel;

                baseFrequency = sampleRate / tableSize;
                currentFrequency = targetFrequency = baseFrequency;

                decayConst = -1.0f / sampleRate;
                decayConst = Mathf.Exp(decayConst / DECAYT);

                UpdateTableDelta();
            }

            private void UpdateTableDelta()
            {
                float tableSizeOverSamplerate = tableSize / sampleRate;
                tableDelta = currentFrequency * tableSizeOverSamplerate;
            }

            private float GetNextSample()
            {
                int index0 = (int)currentIndex;
                int index1 = index0 + 1;

                float frac = currentIndex - index0;
                float value0 = waveTable[index0];
                float value1 = waveTable[index1];

                float currentSample = value0 + frac * (value1 - value0);

                if ((currentIndex += tableDelta) >= tableSize)
                    currentIndex -= tableSize;

                return currentSample;
            }

            public void GetNextAudioBlock(float[] output, int nsamples)
            {
                if (currentLevel < 0.01f)
                    return;

                float localTargetFrequency = targetFrequency;
                //float localTargetLevel = targetLevel;

                float freqDiff  = localTargetFrequency - currentFrequency;
                //float levelDiff = localTargetLevel - currentLevel;

                if (Mathf.Abs(freqDiff) > 5e-03f)
                {
                    float freqDelta = (localTargetFrequency - currentFrequency) / nsamples;
                    for (int sample = 0; sample < nsamples; sample++)
                    {
                        float currentSample = GetNextSample();
                        currentFrequency += freqDelta;
                        UpdateTableDelta();
                        output[sample] += currentSample * currentLevel;
                    }
                    currentFrequency = localTargetFrequency;
                }
                /*else if (Mathf.Abs(levelDiff) > 5e-03f)
                {
                    float levelDelta = (localTargetLevel - currentLevel) / nsamples;
                    for (int sample = 0; sample < nsamples; sample++)
                    {
                        float gain = currentLevel;
                        currentLevel += levelDelta;
                        output[sample] += GetNextSample() * currentLevel;
                    }
                    currentLevel = localTargetLevel;
                }*/
                else
                {
                    for (int sample = 0; sample < nsamples; sample++)
                    {
                        currentLevel *= decayConst;
                        output[sample] += GetNextSample() * currentLevel;
                    }
                }
            }
        }
    }
}


