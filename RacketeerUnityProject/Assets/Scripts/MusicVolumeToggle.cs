using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicVolumeToggle : MonoBehaviour
{

    public AudioMixerGroup musicMixerGroup;
    public void ToggleMusic()
    {
        musicMixerGroup.audioMixer.GetFloat("MusicMuteGain", out float audioMixerLevel);
        Debug.Log("music level: " + audioMixerLevel);
        if (audioMixerLevel != 0)
            musicMixerGroup.audioMixer.ClearFloat("MusicMuteGain");
        else
            musicMixerGroup.audioMixer.SetFloat("MusicMuteGain", -80);
    }
}
