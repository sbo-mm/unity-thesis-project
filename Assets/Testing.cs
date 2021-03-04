using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Testing : MonoBehaviour
{
#if UNITY_STANDALONE_OSX
    [DllImport("AudioPluginModalSynth")]
    private unsafe static extern int VariableModalFilter_SetGains(
        int index, 
        int npoints, 
        int* impactPoints, 
        float* weights
        );
#endif

    bool set = false;

    void Start()
    {


    }

    private void Update()
    {
        // 5,960464E-08, 0,9999999, 0, 106, 8, 109
        if (!set && Input.GetKeyDown(KeyCode.Q))
        {
            unsafe
            {
                int[] ip = { 106, 8, 109 };
                float[] wp = { 5.960464E-08f, 0.9999999f, 0f };

                fixed(int* _ip = &ip[0])
                fixed(float* _wp = &wp[0])
                {
                    VariableModalFilter_SetGains(
                        index: 0,
                        npoints: 1,
                        impactPoints: _ip,
                        weights: _wp
                    );
                }
            }
            Debug.Log("Set Gains");
            set = true;
        }

    }

}
