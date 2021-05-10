using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public Transform Other;


    float sampleRate;
    float elapsed = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.RotateAround(Other.position, Vector3.up, 20.0f * Time.deltaTime);
        transform.LookAt(Other.position, Vector3.up);

    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        int sampleLen = data.Length / channels;
        for (int n = 0; n < sampleLen; n++)
        {
            float t = (elapsed + n) / sampleRate;
            float sample = Mathf.Sin(2.0f * Mathf.PI * 440.0f * t);
            for (int i = 0; i < channels; i++)
            {
                data[n * channels + i] += sample;
            }
        }

        elapsed += sampleLen;
    }

}
