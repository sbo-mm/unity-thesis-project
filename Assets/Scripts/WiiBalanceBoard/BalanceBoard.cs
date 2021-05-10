using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WiiBalanceBoard
{
    public class BalanceBoard
    {
        // Static
        private static WBBEventReceiver _wbbReceiver = null;
        public static bool IsReceiving
        {
            get
            {
                if (_wbbReceiver == null)
                {
                    _wbbReceiver = GameObject.Find("OscReceiver")
                        .GetComponent<WBBEventReceiver>();
                }

                return _wbbReceiver.BoardReceiving;
            }
        }

        // Const
        private const int
            BOTTOM_LEFT = 0,
            BOTTOM_RIGHT = 1,
            TOP_LEFT = 2,
            TOP_RIGHT = 3,
            SUM = 4,
            VIRTUAL_X = 5,
            VIRTUAL_Y = 6,
            BATTERYLV = 7,
            NUMP = 8
        ;

        private const int SUMBUFFERSIZE = 15;
        private const float SUMSCALE    = 1.0f / SUMBUFFERSIZE;
        private const float SUM_THRESH  = 0.05f;

        // Properties
        public float BatteryLevel       { get { return battery_level; } }
        public float COPMagnitude       { get { return CenterOfPressure.magnitude; } }
        public Vector2 CenterOfPressure { get { return new Vector2(vx, vy); } }

        public float RotationX
        {
            get 
            {
                if (!IsReceiving)
                    return 0.0f;

                if (sum <= SUM_THRESH)
                    return 0.0f;

                return WBBGetRotationX(); 
            }
        }

        public float RotationZ
        {
            get 
            {
                if (!IsReceiving)
                    return 0.0f;

                if (sum <= SUM_THRESH)
                    return 0.0f;

                return WBBGetRotationZ(); 
            }
        }

        public float SmoothSum
        {
            get
            {
                float acc = 0.0f;
                for (int i = 0; i < SUMBUFFERSIZE; i++)
                    acc += sumBuffer[i];

                Debug.Log($"Sum: {acc * SUMSCALE}");
                return acc * SUMSCALE;
            }
        }

        // Private
        private float bl;
        private float br;
        private float tl;
        private float tr;
        private float vx;
        private float vy;
        private float sum;
        private float battery_level;

        private float xSensitivity0;
        private float xSensitivity1;
        private float zSensitivity0;
        private float zSensitivity1;

        private float[] sumBuffer;

        public BalanceBoard()
        {
            xSensitivity0 = 7.0f;
            xSensitivity1 = 0.333f;
            zSensitivity0 = 7.0f;   //5.0f;
            zSensitivity1 = 0.333f; //0.75f;

            battery_level = -1.0f;
            sumBuffer = new float[SUMBUFFERSIZE];
        }

        public void SetBoard(float[] wbbdata)
        {
            bl = wbbdata[BOTTOM_LEFT];
            br = wbbdata[BOTTOM_RIGHT];
            tl = wbbdata[TOP_LEFT];
            tr = wbbdata[TOP_RIGHT];
            sum = wbbdata[SUM];
            vx = wbbdata[VIRTUAL_X];
            vy = wbbdata[VIRTUAL_Y];

            battery_level = wbbdata.Length == NUMP
                ? wbbdata[BATTERYLV] : -1f;

            EnqueueBuffer(sum);
        }

        private void EnqueueBuffer(float value)
        {
            for (int i = 0; i < SUMBUFFERSIZE - 1; i++)
                sumBuffer[i] = sumBuffer[i + 1];
            sumBuffer[SUMBUFFERSIZE - 1] = value;
        }

        private float GetUserAngle(float theta, float c, float d)
        {
            float bias = 1.0f / (1.0f + Mathf.Exp(c * d));
            if (theta >= 0)
                return 1.0f / (1.0f + Mathf.Exp(-c * (theta - d))) - bias;

            return -(1.0f / (1.0f + Mathf.Exp(-c * (-theta - d)))) + bias;
        }

        private float GetForceDelta(float f0, float f1, float f2, float f3)
        {
            return ((f0 + f1) - (f2 + f3)) / sum;
        }

        private float WBBGetRotationX()
        {
            float delta = GetForceDelta(tr, br, tl, bl);
            return GetUserAngle(delta, xSensitivity0, xSensitivity1);
        }

        private float WBBGetRotationZ()
        {
            float delta = GetForceDelta(br, bl, tr, tl);
            return -GetUserAngle(delta, zSensitivity0, zSensitivity1);
        }

    }
}
