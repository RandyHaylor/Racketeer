using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : NetworkBehaviour
{
    public GameObject SoundPrefab;
    public AudioSource RoundMusicAudiosource;

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
    public static void PlaySound(AudioClip audioClip, float volume, float pitch)
    {
        _instance.ClientRpcPlaySoundPrivate(audioClip, volume, pitch);
    }
    public static void PlaySound(AudioClip audioClip)
    {
        PlaySound(audioClip, 1, 1);
    }

    void ClientRpcPlaySoundPrivate(AudioClip audioClip, float volume, float pitch)
    {
        GameObject newSound = GameObject.Instantiate(SoundPrefab, Vector3.zero, Quaternion.identity);
        newSound.GetComponent<PlaySoundThenDestroySelf>().PlayThenDestroy(audioClip, volume, pitch);
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
        RoundMusicAudiosource.volume = 0.5f;
        RoundMusicAudiosource.Play();
    }

    public static void StopRoundMusic()
    {
        _instance.RpcStopRoundMusic(); //stop on clients
        _instance.StartCoroutine(_instance.FadeOutRoundMusic()); //stop on server
    }
    [ClientRpc]
    void RpcStopRoundMusic()
    {
        StartCoroutine(_instance.FadeOutRoundMusic());
    }

    IEnumerator FadeOutRoundMusic()
    {

        for (int i = 0; i < 10; i++)
        {
            RoundMusicAudiosource.volume -= 0.05f;
            yield return new WaitForSeconds(0.1f);
        }
        RoundMusicAudiosource.Stop();
    }


}
