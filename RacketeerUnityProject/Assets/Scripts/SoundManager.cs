using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : NetworkBehaviour
{
    public AudioMixerGroup musicMixerGroup;
    public GameObject SoundPrefab;
    public AudioSource RoundMusicAudioSource;
    public AudioSource LevelMusicAudioSource;
    public float randomizePitchLowMult = 0.9f;
    public float randomizePitchHighMult = 1.1f;

    private static SoundManager _instance;
    /*
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.Find("SoundManager").GetComponent<SoundManager>();
            }

            return _instance;
        }
    }*/

    void Awake()
    {
        _instance = this;
    }
    public static void PlaySound(AudioClip audioClip, float volume, float pitch, bool randomizePitch)
    {
        _instance.ClientRpcPlaySoundPrivate(audioClip, volume, pitch, randomizePitch);
    }
    public static void PlaySound(AudioClip audioClip)
    {
        PlaySound(audioClip, 1, 1, false);
    }

    void ClientRpcPlaySoundPrivate(AudioClip audioClip, float volume, float pitch, bool randomizePitch)
    {
        if (randomizePitch)
            pitch = pitch * Random.Range(randomizePitchLowMult, randomizePitchHighMult);
        GameObject newSound = GameObject.Instantiate(SoundPrefab, Vector3.zero, Quaternion.identity);
        newSound.GetComponent<PlaySoundThenDestroySelf>().PlayThenDestroy(audioClip, volume, pitch);
    }


    public static void PlayLevelMusic()
    {
        _instance.PlayLevelMusicInt();
        _instance.RpcPlayLevelMusic(); //start on clients
    }

    [ClientRpc]
    void RpcPlayLevelMusic()
    {
        PlayLevelMusicInt();
    }
    void PlayLevelMusicInt()
    {
        LevelMusicAudioSource.time = 0;
        LevelMusicAudioSource.Play();
    }
    public static void StopLevelMusic()
    {
        _instance.FadeOutAudioSource(_instance.LevelMusicAudioSource);
        _instance.RpcStopLevelMusic(); //fade out on clients
    }

    [ClientRpc]
    void RpcStopLevelMusic()
    {
        StartCoroutine(FadeOutAudioSource(LevelMusicAudioSource));
    }




    public void ToggleMusic()
    {
        musicMixerGroup.audioMixer.GetFloat("MusicVolume", out float audioMixerLevel);
        Debug.Log("music level: " + audioMixerLevel);
        if (audioMixerLevel != 0)
            musicMixerGroup.audioMixer.ClearFloat("MusicVolume");
        else
            musicMixerGroup.audioMixer.SetFloat("MusicVolume", -80);
    }

    public static void PlayRoundMusic()
    {
        _instance.PlayRoundMusicInt();
        _instance.RpcPlayRoundMusic(); //start on clients
    }
    
    [ClientRpc] void RpcPlayRoundMusic()
    {
        PlayRoundMusicInt();
    }
    void PlayRoundMusicInt()
    {
        RoundMusicAudioSource.time = 0;
        RoundMusicAudioSource.Play();
    }

    public static void StopRoundMusic()
    {
        _instance.RpcStopRoundMusic(); //stop on clients
        _instance.StartCoroutine(_instance.FadeOutAudioSource(_instance.RoundMusicAudioSource)); //stop on server
    }
    [ClientRpc]
    void RpcStopRoundMusic()
    {
        StartCoroutine(_instance.FadeOutAudioSource(_instance.RoundMusicAudioSource));
    }

    IEnumerator FadeOutAudioSource(AudioSource audioSource)
    {
        float currentVolume = audioSource.volume;
        for (int i = 0; i < 10; i++)
        {
            audioSource.volume -= 0.05f;
            yield return new WaitForSeconds(0.1f);
        }
        audioSource.Stop();
        audioSource.volume = currentVolume;
    }


}
