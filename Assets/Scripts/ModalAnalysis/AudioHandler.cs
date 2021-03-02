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
        private AudioManager audioManager;
        private float[] auxillaryBuffer;
        private ConcurrentQueue<float[]> audioQueue;

        protected void Start()
        {
            audioManager = GameObject.Find("Manager")
                .GetComponent<AudioManager>();

            auxillaryBuffer = new float[AudioQueueBufferSize];
            audioQueue = new ConcurrentQueue<float[]>();
        }

        private float LogisticFunc(float x)
        {
            const float x0 = 0f;
            const float L = 2f;
            const float k = 0.65f;
            return L / (1.0f + Mathf.Exp(-k * (x - x0))) - 1.0f;
        }

        private float CompressLogistic(float sample)
        {
            //return LogisticFunc(sample);
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

            if (audioQueue.IsEmpty)
                return;

            for (int n = 0; n < UnityAudioBufferSize; n += AudioQueueBufferSize)
            {
                if (audioQueue.IsEmpty)
                    break;

                if (audioQueue.TryDequeue(out float[] samples))
                    CopyTo(samples, auxillaryBuffer);

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

        protected void Write(float[] samples)
        {
            if (audioQueue != null && samples != null)
            {
                float sampleSum = samples.Sum(Mathf.Abs);
                if (CompressLogistic(sampleSum) < 0.1f)
                    return;

                if (samples.Length == AudioQueueBufferSize)
                {
                    float[] aux = new float[samples.Length];
                    CopyTo(samples, aux);
                    audioQueue.Enqueue(aux);
                }
            }
        }
    }
}