using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MasterVolumeChanger : MonoBehaviour
{

    public AudioMixer mixer;

    public void SetLevel()
    {        
        float sliderValue = gameObject.GetComponent<Slider>().value;
        Debug.Log("new slider level: " + sliderValue);
        mixer.SetFloat("MasterVolume", Mathf.Log10(sliderValue) * 20);
        GUI.FocusControl(null);
    }
}