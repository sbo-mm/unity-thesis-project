//
//  Plugin_VariableModalFilter.cpp
//  AudioPluginModalSynth
//
//  Created by Sophus Bénée Olsen on 04/03/2021.
//  Copyright © 2021 Sophus Bénée Olsen. All rights reserved.
//

#include "AudioPluginUtil.h"

namespace VariableModalFilter
{
    const float TWO_PI = 2.0f * kPI;
    const int MAXMODELINSTANCES = 10;
    const int MAXRESONATORS = 256;
    
    enum Param
    {
        P_INSTANCE,
        P_GAIN,
        P_FSCALE,
        P_ASCALE,
        P_DSCALE,
        P_NUM
    };
    
    struct ModalModel
    {
        // Three dynamic memory pointers.
        // (Will eventually store arrays)
        float* f; // Pointer to array of resonant frequencies (will nmodes entries)
        float* d; // Pointer to array of decays (will have nmodes entries)
        float* a; // Pointer to flattened array of mode shapes (gains) ...
                  // ... will contain num_verts x num_modes entries
        
        float fscale;
        float dscale;
        float ascale;
        
        int nmodes;
        int nverts;
    };
    
    class Resonator
    {
    private:
        inline float getGainAt(int idx)
        {
            return model->a[idx * model->nmodes + filteridx] * model->ascale;
        }
        
        inline float sumGains(int idx)
        {
            int v0 = (idx + 1) * 3 - 3;
            int v1 = (idx + 1) * 3 - 2;
            int v2 = (idx + 1) * 3 - 1;
            return getGainAt(v0) + getGainAt(v1) + getGainAt(v2);
        }
        
    public:
        inline float Process(const float input)
        {
            float ynew = twoRcosTheta * yt_1 - R2 * yt_2 + ampR * input;
            yt_2 = yt_1;
            yt_1 = ynew;
            return ynew;
        }
        
        inline void ComputeFilterCoefs()
        {
            float omega = model->f[filteridx] * model->fscale * TWO_PI;
            float theta = omega / sampleRate;
            float R = exp((model->d[filteridx] * model->dscale) / sampleRate);
            twoRcosTheta = 2.0f * R * cos(theta);
            RSinTheta = R * sin(theta);
            R2 = R * R;
            yt_1 = 0;
            yt_2 = 0;
        }
        
        inline void SetupResonator(int ix)
        {
            ampR = 0;
            filteridx = ix;
            ComputeFilterCoefs();
        }
        
        inline void SetModel(ModalModel* inputmodel)
        {
            model = inputmodel;
        }
        
        inline void SetGain(int npoints, int* impactPoints, float *weights)
        {
            const int pw = 3;
            float g0, g1, g2, w0, w1, w2;
            
            ampR = 0;
            float oneOverN = 1.0f / (float)npoints;
            
            for (int i = 0; i < npoints; i += pw)
            {
                // Fetch the gains corresponding to the triangle vertices we have hit
                g0 = sumGains(impactPoints[i + 0]);
                g1 = sumGains(impactPoints[i + 1]);
                g2 = sumGains(impactPoints[i + 2]);
                
                // Fetch the weights (barycentric coordinates) of the point
                // within the triangle to interpolate the final gains
                w0 = weights[i + 0];
                w1 = weights[i + 1];
                w2 = weights[i + 2];
                
                // Compute weighted average of the three gains
                float avgGain = g0 * w0 + g1 * w1 + g2 * w2;
                ampR += oneOverN * avgGain * RSinTheta;
            }
        }
        
    public:
        float sampleRate;  // Reference to audio engines sample rate
                            
    private:
        ModalModel* model; // Pointer to a modal model
        int filteridx;     // Used internally to index into the model
        
        float twoRcosTheta; // reson filter coef
        float RSinTheta;    // reson filter coef
        float R2;           // reson filter coef
        float ampR;         // Set this value on impact (signal gain)
        
        float yt_1;         // History variables to store state of filter
        float yt_2;         // ^^
    };
    
    struct EffectData
    {
        struct Data
        {
            float p[P_NUM];
            Random random;
        };
        union
        {
            Data data;
            unsigned char pad[(sizeof(Data) + 15) & ~15];
        };
    };
    
    struct ModalFilterInstance
    {
        bool isSetup;
        ModalModel model;
        Resonator filters[MAXRESONATORS];
    };
    
    inline ModalFilterInstance* GetModalFilterInstance(int index)
    {
        static ModalFilterInstance instance[MAXMODELINSTANCES];
        if (index < 0 || index >= MAXMODELINSTANCES)
            return NULL;
    
        return &instance[index];
    }
    
    int InternalRegisterEffectDefinition(UnityAudioEffectDefinition& definition)
    {
        int numparams = P_NUM;
        definition.paramdefs = new UnityAudioParameterDefinition[numparams];
        RegisterParameter(definition, "Instance", "", 0.0f, MAXMODELINSTANCES - 1, 0.0f, 1.0f, 1.0f, P_INSTANCE, "Determines the modal model instance from which params are set");
        RegisterParameter(definition, "Gain", "dB", -120.0f, 50.0f, 0.0f, 1.0f, 1.0f, P_GAIN, "Overall gain");
        RegisterParameter(definition, "FreqScale", "", 0.1f, 100.0f, 1.0f, 1.0f, 1.0f, P_FSCALE, "Uniformly scale the center frequencies of all modes");
        RegisterParameter(definition, "DecayScale", "", 0.01f, 10.0f, 1.0f, 1.0f, 1.0f, P_DSCALE, "Uniformly scale the angular decays of all modes");
        RegisterParameter(definition, "ModeGainScale", "", 0.1f, 100.0f, 1.0f, 1.0f, 1.0f, P_ASCALE, "Uniformly scale the mode shape (mode gains) of all modes");
        return numparams;
    }
    
    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK CreateCallback(UnityAudioEffectState* state)
    {
        EffectData* effectdata = new EffectData;
        memset(effectdata, 0, sizeof(EffectData));
        state->effectdata = effectdata;
        InitParametersFromDefinitions(InternalRegisterEffectDefinition, effectdata->data.p);
        
        for (int i = 0; i < MAXMODELINSTANCES; i++)
        {
            ModalFilterInstance* mf = GetModalFilterInstance(i);
            
            mf->isSetup = false;
            mf->model.nmodes = 0;
            mf->model.nverts = 0;
            mf->model.fscale = 1.0f;
            mf->model.dscale = 1.0f;
            mf->model.ascale = 1.0f;
            
            for (int r = 0; r < MAXRESONATORS; r++)
            {
                mf->filters[r].sampleRate = (float)state->samplerate;
                mf->filters[r].SetModel(&mf->model);
            }
        }
        
        return UNITY_AUDIODSP_OK;
    }
    
    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK ReleaseCallback(UnityAudioEffectState* state)
    {
        EffectData* data = state->GetEffectData<EffectData>();
        delete data;
        
        for (int i = 0; i < MAXMODELINSTANCES; i++)
        {
            ModalFilterInstance* mf = GetModalFilterInstance(i);
            if (mf->model.f)
                delete[] mf->model.f;
            if (mf->model.d)
                delete[] mf->model.d;
            if (mf->model.a)
                delete[] mf->model.a;
        }

        return UNITY_AUDIODSP_OK;
    }
    
    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK SetFloatParameterCallback(UnityAudioEffectState* state, int index, float value)
    {
        EffectData* effectdata = state->GetEffectData<EffectData>();
        
        if (index >= P_NUM)
            return UNITY_AUDIODSP_ERR_UNSUPPORTED;
        
        effectdata->data.p[index] = value;
        
        if (index == P_FSCALE || index == P_DSCALE || index == P_ASCALE)
        {
            ModalFilterInstance* mf = GetModalFilterInstance((int)effectdata->data.p[P_INSTANCE]);
            mf->model.fscale = effectdata->data.p[P_FSCALE];
            mf->model.dscale = effectdata->data.p[P_DSCALE];
            mf->model.ascale = effectdata->data.p[P_ASCALE];
            for (int i = 0; i < mf->model.nmodes; i++)
                mf->filters[i].ComputeFilterCoefs();
        }
        
        return UNITY_AUDIODSP_OK;
    }
    
    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK GetFloatParameterCallback(UnityAudioEffectState* state, int index, float* value, char *valuestr)
    {
        EffectData* effectdata = state->GetEffectData<EffectData>();
        if (index >= P_NUM)
            return UNITY_AUDIODSP_ERR_UNSUPPORTED;
        if (value != NULL)
            *value = effectdata->data.p[index];
        if (valuestr != NULL)
            valuestr[0] = 0;
        return UNITY_AUDIODSP_OK;
    }
    
    int UNITY_AUDIODSP_CALLBACK GetFloatBufferCallback(UnityAudioEffectState* state, const char* name, float* buffer, int numsamples)
    {
        return UNITY_AUDIODSP_OK;
    }
    
    
    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK ProcessCallback(UnityAudioEffectState* state, float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int outchannels)
    {
        EffectData::Data* data = &state->GetEffectData<EffectData>()->data;
        
        // Clear output buffer
        memset(outbuffer, 0, sizeof(float) * length * outchannels);
        
        ModalFilterInstance* instance = GetModalFilterInstance((int)data->p[P_INSTANCE]);
        if (instance == NULL)
            return UNITY_AUDIODSP_OK;
        
        if (!instance->isSetup)
            return UNITY_AUDIODSP_OK;
        
        // Convert gain from db-scale to linear
        float gain = powf(10.0f, data->p[P_GAIN] * 0.05f);
        
        // Fetch number of resonators
        const int numResonators = instance->model.nmodes;
        gain /= (float)numResonators;
        
        // Fetch the reson filters
        Resonator* resonators = &instance->filters[0];
        
        for (int i = 0; i < inchannels; i++)
        {
            float denormalFix = data->random.GetFloat(-1.0f, 1.0f) * 1.0e-9f;
            for (int k = 0; k < numResonators; k++)
            {
                Resonator reson = resonators[k];
                float* src = inbuffer + i;
                float* dst = outbuffer + i;
                for (unsigned int n = 0; n < length; n++)
                {
                    *dst += reson.Process(*src + denormalFix) * gain;
                    src += inchannels;
                    dst += outchannels;
                }
            }
        }
        
        return UNITY_AUDIODSP_OK;
    }
    
    extern "C" UNITY_AUDIODSP_EXPORT_API int VariableModalFilter_SetModelParams(int index, int nmodes, int nverts, float* freqs, float* decays, float* gains)
    {
        ModalFilterInstance* instance = GetModalFilterInstance(index);
        if (instance == NULL)
            return -1;
        
        ModalModel* model = &instance->model;
        model->f = new float[nmodes];
        model->d = new float[nmodes];
        model->a = new float[nmodes * nverts];
        memcpy(model->f, freqs, nmodes * sizeof(float));
        memcpy(model->d, decays, nmodes * sizeof(float));
        memcpy(model->a, gains, nmodes * nverts * sizeof(float));
        model->nmodes = nmodes;
        model->nverts = nverts;
    
        for (int i = 0; i < nmodes; i++)
            instance->filters[i].SetupResonator(i);
        
        instance->isSetup = true;
        return 1;
    }
    
    extern "C" UNITY_AUDIODSP_EXPORT_API int VariableModalFilter_SetGains(int index, int npoints, int* impactPoints, float* weights)
    {
        ModalFilterInstance* instance = GetModalFilterInstance(index);
        if (instance == NULL)
            return -1;
        
        if (!instance->isSetup)
            return -2;
        
        int numModes = instance->model.nmodes;
        for (int i = 0; i < numModes; i++)
            instance->filters[i].SetGain(npoints, impactPoints, weights);
        
        return 1;
    }
}
