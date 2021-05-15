/*
Creates and Manages the SineWave
*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class SineWave : MonoBehaviour {

    #region Variables

    //this is the material that will store the SineWave Shader
    public Shader SineWaveShader;
    private Material mat;



    private bool _XAxis;
    public bool XAxis
    {
        get { return _XAxis; }
        set { 
            _XAxis=value;
            float X = (XAxis) ? 1f : 0f;
            mat.SetFloat("_XAxis",X);
        }
    }


    private float _HorizontalOffset;
    public float HorizontalOffset
    {
        get { return _HorizontalOffset; }
        set { 
            _HorizontalOffset=value;
            mat.SetFloat("_HorizontalOffset",HorizontalOffset);
        }
    }


    private float _VerticalOffset;
    public float VerticalOffset
    {
        get { return _VerticalOffset; }
        set { 
            _VerticalOffset=value;
            mat.SetFloat("_VerticalOffset",VerticalOffset);
        }
    }


    private float _Amplitude;
    public float Amplitude
    {
        get { return _Amplitude; }
        set { 
            _Amplitude=value;
            mat.SetFloat("_Amplitude",_Amplitude);
        }
    }


    private float _Frequency;
    public float Frequency
    {
        get { return _Frequency; }
        set { 
            _Frequency=value;
            mat.SetFloat("_Frequency",_Frequency);
        }
    }

    #endregion

    #region Methods
    void Start()
    {
        mat = new Material(SineWaveShader);

        //default values
        XAxis = false;
        HorizontalOffset = 0f;
        VerticalOffset = 0;
        Amplitude = 0.1f;
        Frequency = 60f;


    }



    void OnRenderImage(RenderTexture src, RenderTexture dest) 
    {
        if (mat == null)
        {
            return;
        }

        if(mat != null)
        {
            
        }

        Graphics.Blit(src, dest, mat);
    }

    #endregion
}
