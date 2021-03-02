using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour
{
    ulong seed = 0;

    // Start is called before the first frame update
    void Start()
    {
        var r = new System.Random();
        seed = (ulong)r.Next();


        for (int i = 0; i < 45; i++)
        {
            Debug.Log(GetRandom(-1f, 1f));
        }

    }

    float GetRandom()
    {
        const float scale = 1.0f / (float)0x7FFFFFFF;
        seed = seed * 69069 + 1;
        return (((seed >> 16) ^ seed) & 0x7FFFFFFF) * scale;
    }

    float GetRandom(float minVal, float maxVal)
    {
        return minVal + (maxVal - minVal) * GetRandom();
    }
}
