using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace ModalAnalysis
{
    [RequireComponent(typeof(AudioSource))]
    public abstract class AudioHandler : MonoBehaviour
    {
        // Const
        private const float LF_GROWTHRATE = 0.00015f;
        private const float LF_MIDPOINT = 0f;
        private const float LF_MAXIMUM = 2f;
        private const float LF_HALFMAX = LF_MAXIMUM * 0.5f;

        // Properties (protected)
        protected int UnityAudioBufferSize
        {
            get { return audioManager.UnityAudioBufferSize; }
        }

        protected int AudioQueueBufferSize
        {
            get { return audioManager.AudioQueueBufferSize; }
        }

        protected long AqByteSize
        {
            get { return audioManager.AudioQueueBytesLength; }
        }

        protected float SampleRate
        {
            get { return audioManager.SampleRate; }
        }

        // Protected
        protected bool AudioObjectReady { get; set; } = false;

        // Private
        private float[] auxillaryBuffer;
        private AudioManager audioManager;

        protected void Start()
        {
            audioManager = GameObject.Find("Manager")
                .GetComponent<AudioManager>();
            auxillaryBuffer = new float[AudioQueueBufferSize];
        }

        private float CompressLogistic(float sample)
        {
            return (LF_MAXIMUM / (1 + Mathf.Exp(-LF_GROWTHRATE * (sample - LF_MIDPOINT)))) - LF_HALFMAX;
        }

        private void CopyTo(float[] src, float[] dst)
        {
            unsafe
            {
                fixed (float* srcarray = &src[0])
                fixed (float* dstarray = &dst[0])
                {
                    Buffer.MemoryCopy(srcarray, dstarray, AqByteSize, AqByteSize);
                }
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!AudioObjectReady)
                return;
              
            for (int n = 0; n < UnityAudioBufferSize; n += AudioQueueBufferSize)
            {
                OnRequestSamples(auxillaryBuffer, AudioQueueBufferSize);
                for (int j = 0; j < AudioQueueBufferSize; j++)
                {
                    float sample = auxillaryBuffer[j];
                    sample = CompressLogistic(sample);
                    for (int i = 0; i < channels; i++)
                    {
                        data[(n + j) * channels + i] += sample;
                    }
                }
            }
        }

        protected abstract void OnRequestSamples(float[] output, int nsamples);
    }
}