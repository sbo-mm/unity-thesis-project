using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModalAnalysis
{
    public class AudioManager : MonoBehaviour
    {
        // Unity Public
        [SerializeField]
        private int AQBufferSize = 128;

        // Properties
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

        // Static
        private static List<Thread> _synthesisThreads
            = new List<Thread>();

        private static CancellationTokenSource _audioThreadCancellationSrc
            = new CancellationTokenSource();

        public static Thread GetSynthesisThread(Action<object> synthesisCallback)
        {
            Thread _internalThread
                = new Thread(() => synthesisCallback(_audioThreadCancellationSrc.Token));
            _synthesisThreads.Add(_internalThread);
            return _internalThread;
        }

        private void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
            AudioQueueBufferSize = AQBufferSize;
            AudioSettings.GetDSPBufferSize(out unityDspBufferSize, out int _);
        }

        private void OnApplicationQuit()
        {
            _audioThreadCancellationSrc.Cancel();
            foreach (var _thread in _synthesisThreads)
            {
                if (!_thread.IsAlive)
                    continue;
                _thread.Join();
            }
            _audioThreadCancellationSrc.Dispose();
        }
    }
}