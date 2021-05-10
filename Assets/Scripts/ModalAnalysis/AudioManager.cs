using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ModalAnalysis
{
    public class AudioManager : MonoBehaviour
    {
        // Unity Public
        [SerializeField]
        private int AQBufferSize = 128;

        [SerializeField]
        private AudioMixerGroup mixer;

        [SerializeField]
        private AudioListener mainListener;

        // Properties
        public AudioListener MainListener
        {
            get { return mainListener; }
        }

        public int UnityAudioBufferSize
        {
            get { return unityDspBufferSize; }
        }

        public int AudioQueueBufferSize
        {
            get { return audioQueueBufferSize; }

            private set
            {
                audioQueueBufferSize = value;
                aqBytesLength = audioQueueBufferSize * sizeof(float);
            }
        }

        public long AudioQueueBytesLength
        {
            get { return aqBytesLength; }
        }

        public float SampleRate
        {
            get { return sampleRate; }
        }

        // Private
        private float sampleRate;
        private long aqBytesLength;
        private int audioQueueBufferSize;
        private int unityDspBufferSize;

        // Utility Private
        private float mixerDefaultVolume;
        private readonly float[] NULLCLIP = new float[0];
        private Dictionary<AudioClip, float[]> loopSamplesDict;

        private void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
            AudioQueueBufferSize = AQBufferSize;
            AudioSettings.GetDSPBufferSize(out unityDspBufferSize, out int _);

            // misc setup
            mixer.audioMixer.GetFloat("Volume", out mixerDefaultVolume);
            loopSamplesDict = new Dictionary<AudioClip, float[]>();
        }

        public void GetLoopSamplesForAudioClip(AudioClip clip, out float[] samples)
        {
            if (clip == null)
            {
                samples = NULLCLIP;
                return;
            }

            if (!loopSamplesDict.ContainsKey(clip))
            {
                int nsamples = clip.samples;
                float[] sampleBuffer = new float[nsamples];
                clip.GetData(sampleBuffer, 0);
                loopSamplesDict.Add(clip, sampleBuffer);
            }

            if (!loopSamplesDict.TryGetValue(clip, out samples))
                samples = NULLCLIP;
        }

        public void OnPause()
        {
            if (mixer == null)
                return;

            mixer.audioMixer.SetFloat("Volume", -80f);
        }

        public void OnResume()
        {
            if (mixer == null)
                return;

            mixer.audioMixer.SetFloat("Volume", mixerDefaultVolume);
        }
    }
}