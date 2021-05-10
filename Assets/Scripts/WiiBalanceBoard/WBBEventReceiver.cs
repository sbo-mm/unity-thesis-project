using OscJack;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace WiiBalanceBoard
{
    [AddComponentMenu("OSC/WBB Event Receiver")]
    public sealed class WBBEventReceiver : MonoBehaviour
    {
        const int RECV_TIMEOUT = 1;
        const int MAX_OSC_PARAMS = 7;

        [System.Serializable]
        class FloatArrayEvent : UnityEvent<float[]> { }

        [SerializeField] int _udpPort = 9000;
        [SerializeField] string _oscAddress = "/wii/1/balance";
        [SerializeField] FloatArrayEvent _event;

        int _currentPort;
        string _currentAddress;

        float[] _floatBuffer;
        float[] _timeOutBuffer;
        Queue<float> _floatQueue;

        // Pseudo Connection timeout
        bool onFirstReceived;
        float beginTime;

        public bool BoardReceiving
        {
            get { return onFirstReceived; }
        }

        float[] DequeueFloatArray()
        {
            lock (_floatQueue)
            {
                for (int i = 0; i < MAX_OSC_PARAMS; i++)
                    _floatBuffer[i] = _floatQueue.Dequeue();
            }

            return _floatBuffer;
        }

        void OnEnable()
        {
            if (string.IsNullOrEmpty(_oscAddress))
            {
                _currentAddress = null;
                return;
            }

            var server = OscMaster.GetSharedServer(_udpPort);
            server.MessageDispatcher.AddCallback(_oscAddress, OnDataReceive);
            
            _currentPort = _udpPort;
            _currentAddress = _oscAddress;

            if (_floatQueue == null)
                _floatQueue = new Queue<float>(MAX_OSC_PARAMS);

            if (_floatBuffer == null)
                _floatBuffer = new float[MAX_OSC_PARAMS];

            if (_timeOutBuffer == null)
                _timeOutBuffer = new float[MAX_OSC_PARAMS];
        }

        void OnDisable()
        {
            if (string.IsNullOrEmpty(_currentAddress)) return;

            var server = OscMaster.GetSharedServer(_currentPort);
            server.MessageDispatcher.RemoveCallback(_currentAddress, OnDataReceive);

            _currentAddress = null;
        }

        void OnValidate()
        {
            if (Application.isPlaying)
            {
                OnDisable();
                OnEnable();
            }
        }

        void InvokeEvent()
        {
            beginTime = Time.time;
            while (_floatQueue.Count > 0)
                _event.Invoke(DequeueFloatArray());
        }

        void InvokeTimeOutEvent()
        {
            _event.Invoke(_timeOutBuffer);
        }

        void LateUpdate()
        {
            if (!onFirstReceived)
            {
                if (_floatQueue.Count > 0)
                {
                    InvokeEvent();
                    onFirstReceived = true;
                }
                return;
            }

            if (_floatQueue.Count > 0)
            {
                InvokeEvent();
                return;
            }

            float elapsedSinceLastCount = Time.time - beginTime;
            if (elapsedSinceLastCount > RECV_TIMEOUT)
            {
                InvokeTimeOutEvent();
                onFirstReceived = false;
            }
        }
        
        void OnDataReceive(string address, OscDataHandle data)
        {
            lock (_floatQueue)
            {
                for (int i = 0; i < MAX_OSC_PARAMS; i++)
                    _floatQueue.Enqueue(data.GetElementAsFloat(i));
            }
        }

    }
}