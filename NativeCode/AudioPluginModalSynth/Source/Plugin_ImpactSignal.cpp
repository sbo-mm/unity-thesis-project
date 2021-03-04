//
//  Plugin_ImpactSignal.cpp
//  AudioPluginModalSynth
//
//  Created by Sophus Bénée Olsen on 04/03/2021.
//  Copyright © 2021 Sophus Bénée Olsen. All rights reserved.
//

#include "AudioPluginUtil.h"

namespace ImpactSignal
{
    // Use an enum for the plugin parameters
    // we want Unity to have access to
    // By default, Unity manipulates exposed
    // parameters with a slider in the editor
    enum Param
    {
        P_GAIN,
        P_NUM
        // Since enum values start at 0, the last value
        // gives us the total number of parameters in the enum
        // However, we don't use it as an actual parameter
    };
    
    // This is a callback we'll have the SDK invoke when initializing parameters
    // Instantiate the parameter details here
    int InternalRegisterEffectDefinition(UnityAudioEffectDefinition& definition)
    {
        int numparams = P_NUM;
        definition.paramdefs = new UnityAudioParameterDefinition [numparams];
        
        RegisterParameter(definition,        // EffectDefinition object passed to us
                          "Gain Multiplier", // The parameter label shown in the Unity editor
                          "",                // The units (ex. dB, Hz, cm, s, etc)
                          0.0f,              // Minimum parameter value
                          2.0f,              // Maximum parameter value
                          1.0f,              // Default parameter value
                          1.0f,              // Display scale, Unity editor shows actualValue*displayScale
                          1.0f,              // Display exponent, in case you want a slider operating on an exponential scale in the editor
                          P_GAIN);           // The index of the parameter in question; use the enum value
        
        return numparams;
    }
    
    // Define a struct that will hold the plugin's state
    // Our noise plugin is very simple, so we're only interested
    // in keeping track of the single parameter we have: gain
    struct EffectData
    {
        float p[P_NUM]; // Parameters
    };
    
    // UNITY_AUDIODSP_RESULT is defined as `int`
    // UNITY_AUDIODSP_CALLBACK is defined as nothing
    // So behind the scenes, the function signature is really `int CreateCallback(UnityAudioEffectState* state)`
    // This callback is invoked by Unity when the plugin is loaded
    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK CreateCallback(UnityAudioEffectState* state)
    {
        EffectData* effectdata = new EffectData;    // Create a new pointer to the struct defined earlier
        memset(effectdata, 0, sizeof(EffectData));  // Quickly fill memory location with zeros
        effectdata->p[P_GAIN] = 1.0f;               // Initialize effectdata with default parameter value(s)
        state->effectdata = effectdata;             // Add our effectdata pointer to the state so we can reach it in other callbacks
        // Use the callback we defined earlier to initialize the parameters
        InitParametersFromDefinitions(InternalRegisterEffectDefinition, effectdata->p);
        //srand(time(nullptr));                       // Seeds the random number generator
        return UNITY_AUDIODSP_OK;                   // All is well!
    }
    
    // This callback is invoked by Unity when the plugin is unloaded
    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK ReleaseCallback(UnityAudioEffectState* state)
    {
        // Grab the EffectData pointer we added earlier in CreateCallback
        EffectData *data = state->GetEffectData<EffectData>();
        delete data; // Cleanup
        return UNITY_AUDIODSP_OK;
    }
    
    // Called whenever a parameter is changed from the editor
    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK SetFloatParameterCallback(UnityAudioEffectState* state, int index, float value)
    {
        EffectData *data = state->GetEffectData<EffectData>(); // Grab EffectData
        if (index >= P_NUM) // index should never point to a parameter that we haven't defined
            return UNITY_AUDIODSP_ERR_UNSUPPORTED;
        data->p[index] = value; // Set the parameter to the value the editor gave us
        return UNITY_AUDIODSP_OK;
    }
    
    // Internally used by the SDK to query parameter values, not sure when this is called...
    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK GetFloatParameterCallback(UnityAudioEffectState* state, int index, float* value, char *valuestr)
    {
        EffectData *data = state->GetEffectData<EffectData>();
        if (index >= P_NUM)
            return UNITY_AUDIODSP_ERR_UNSUPPORTED;
        if (value != NULL)
            *value = data->p[index];
        if (valuestr != NULL)
            valuestr[0] = 0;
        return UNITY_AUDIODSP_OK;
    }
    
    // Also unsure where this is used, but it's required to be defined, so just return from the function as soon as it's called
    int UNITY_AUDIODSP_CALLBACK GetFloatBufferCallback(UnityAudioEffectState* state, const char* name, float* buffer, int numsamples)
    {
        return UNITY_AUDIODSP_OK;
    }
    
    // ProcessCallback gets called as long as the plugin is loaded
    // This includes when the editor is not in play mode!
    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK ProcessCallback(
      UnityAudioEffectState* state, // The state gets passed into all callbacks
      float* inbuffer,              // Plugins can be chained together; inbuffer holds the signal incoming from another plugin, or an AudioSource
      float* outbuffer,             // We fill outbuffer with the signal Unity should send to the speakers
      unsigned int length,          // The number of samples the buffers hold
      int inchannels,               // The number of channels the incoming signal uses
      int outchannels)              // The number of channels the outgoing signal uses
    {
        // Grab the EffectData struct we created in CreateCallback
        EffectData *data = state->GetEffectData<EffectData>();
        
        // A gain multiplier to silence the plugin when not in play mode or muted, since ProcessCallback is still invoked in those cases
        float wetTarget = ((state->flags & UnityAudioEffectStateFlags_IsPlaying) && !(state->flags & (UnityAudioEffectStateFlags_IsMuted | UnityAudioEffectStateFlags_IsPaused))) ? 1.0f : 0.0f;
        
        // For each sample going to the output buffer
        for(unsigned int n = 0; n < length; n++)
        {
            // For each channel of the sample
            for(int i = 0; i < outchannels; i++)
            {
                // Generate a random float in the range [-1.0, 1.0] (fixed-point arithmetic)
                int rInt = rand() % 20000;
                rInt -= 10000;
                float r = rInt / 10000.0f;
                
                // Write the sample to the buffer
                outbuffer[n * outchannels + i] = r * wetTarget * data->p[P_GAIN];
            }
        }
        
        return UNITY_AUDIODSP_OK;
    }
}
