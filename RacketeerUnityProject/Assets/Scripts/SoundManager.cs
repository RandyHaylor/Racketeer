using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : NetworkBehaviour
{
    public GameObject SoundPrefab;

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

    public static void PlaySound(AudioClip audioClip)
    {
        _instance.ClientRpcPlaySoundPrivate(audioClip);
    }
    void ClientRpcPlaySoundPrivate(AudioClip audioClip)
    {
        GameObject newSound = GameObject.Instantiate(SoundPrefab, Vector3.zero, Quaternion.identity);
        newSound.GetComponent<PlaySoundThenDestroySelf>().PlayThenDestroy(audioClip);
    }
}
