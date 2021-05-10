using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Testing : MonoBehaviour
{
    public float freq = 1.0f;

    private float fs;
    private float currentAngle = 0.0f;

    private float maxAngleTilt = 20.0f;

    public float OscRecv {get; set; }

    void Start()
    {
        fs = 1.0f / Time.fixedDeltaTime;
    }

    private void Tilt()
    {
        float curFreq = 2.0f * Mathf.PI * freq;
        float timeDelta = curFreq / fs;

        float rad0 = Mathf.Sin(currentAngle);
        float rad1 = Mathf.Cos(currentAngle * 2.0f);
        float tilt0 = maxAngleTilt * rad0;
        float tilt1 = maxAngleTilt * rad1;

        transform.rotation = Quaternion.Euler(tilt0, 0, 0);

        currentAngle = (currentAngle + timeDelta) % (2.0f * Mathf.PI);
    }

    private void FixedUpdate()
    {
        Tilt();
    }
}
