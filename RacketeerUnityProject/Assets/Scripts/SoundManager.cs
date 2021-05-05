using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoundManager : NetworkBehaviour
{
    public List<InspectableKvp<string, AudioClip>> effectAudioClipList;
    public List<InspectableKvp<string, AudioClip>> musicAudioClipList;

    private AudioSource[] musicAudioSources;
    private int musicAudioSourcesSize = 2;
    private int currentMusicAudioSource = 0;

    [Header("Music System takes up 2 audio sources when crossfading.")]
    [Space(height: 1)]
    [Header("Music Source Prefab Stores Settings And Location For Music Sources")]
    public GameObject musicSourcePrefab;
    [Range(0,1)]
    public float defaultMusicVolume = 0.8f;

    [Header("Set crossfade to negative for a delay between song changes")]
    public float crossfadeTime = 2;
    public float volumeChangesPerSecond = 10;


    public float randomizePitchLowMult = 0.9f;
    public float randomizePitchHighMult = 1.1f;
    public bool playSoundOnDedicatedServer = false;
    [Header("Sound Object Pool Options (32 is max simultaneous sounds...)")]
    public int soundObjectPoolSize = 28;
    public AudioSource[] soundObjectPool; 
    private int currentSoundObject = 0; //keeps track of most recent sound object used so oldest sound object gets re-used (whether it's playing or not)

    private uint unreachableNetId = 4000000000;

    private static SoundManager _instance;

    public static SoundManager Instance
    { get {
            if (_instance == null) _instance = GameObject.Find("SoundManager").GetComponent<SoundManager>();
            return _instance;
    }     }

    void Awake()
    {
        _instance = this;
        InitializeSoundObjectPools();
    }

    private void InitializeSoundObjectPools()
    {
        musicAudioSources = new AudioSource[2];
        GameObject newGameObject;
        soundObjectPool = new AudioSource[soundObjectPoolSize];
        for (int i = 0; i < soundObjectPool.Length; i++)
        {
            newGameObject = GameObject.Instantiate(new GameObject());
            newGameObject.name = "soundEffectPoolAudioSource";
            newGameObject.transform.SetParent(gameObject.transform);
            newGameObject.AddComponent<AudioSource>();
            soundObjectPool[i] = newGameObject.GetComponent<AudioSource>();
            soundObjectPool[i].GetComponent<AudioSource>().playOnAwake = false;
        }

        for (int i = 0; i < musicAudioSourcesSize; i++)
        {
            if (!musicSourcePrefab || musicSourcePrefab.GetComponent<AudioSource>() == null)
            {
                Debug.Log("MusicSourcePrefab is null or doesn't have an AudioSource attached");
                newGameObject = GameObject.Instantiate(new GameObject());
                newGameObject.AddComponent<AudioSource>();
            }
            else
                newGameObject = GameObject.Instantiate(musicSourcePrefab);

            newGameObject.name = "musicAudioSource";
            newGameObject.transform.SetParent(gameObject.transform);

            musicAudioSources[i] = newGameObject.GetComponent<AudioSource>();
            musicAudioSources[i].playOnAwake = false;
        }
    }

    //call these to play a sound on the client or server alone and send no commands/rpcs (useful to still use SoundManager for local sounds to prevent going over sound limit)
    public static void PlaySoundSelfOnly(string audioClipName) => PlaySoundSelfOnly(audioClipName, 1, 1, false, Vector3.zero);
    public static void PlaySoundSelfOnly(string audioClipName, float volume, float pitch, bool randomizePitch, Vector3 location)
    {   _instance.PlaySoundPrivate(audioClipName, volume, pitch, randomizePitch, location, _instance.unreachableNetId); }


    //call this from a client to play a sound on a client immediately (no network lag through server) and pass command to play on server and other clients
    [Client] public static void PlaySoundClientThenServerThenOtherClients(string audioClipName, float volume, float pitch, bool randomizePitch, Vector3 location)
    {
        _instance.PlaySoundPrivate(audioClipName, volume, pitch, randomizePitch, location, _instance.unreachableNetId);
        _instance.CmdPlaySoundPrivate(audioClipName, volume, pitch, randomizePitch, location, _instance.GetComponent<NetworkBehaviour>().netId);
    }
    //call this from a client to send a command to the server to play a sound on all clients (will play on calling client via server & will have network lag - sometimes desireable for sync)
    public static void PlaySoundServerThenClients(string audioClipName, float volume, float pitch, bool randomizePitch, Vector3 location)
    {   _instance.CmdPlaySoundPrivate(audioClipName, volume, pitch, randomizePitch, location, _instance.unreachableNetId); }

    [Command(requiresAuthority = false)] void CmdPlaySoundPrivate(string audioClipName, float volume, float pitch, bool randomizePitch, Vector3 location, uint netIdToExclude)
    { PlaySoundPrivate(audioClipName, volume, pitch, randomizePitch, location, netIdToExclude); }

    //call these from the server, they'll play on all clients
    [Server] public static void PlaySound(string audioClipName)                                                   => PlaySound(audioClipName, 1, 1, false);
    [Server] public static void PlaySound(string audioClipName, float volume, float pitch, bool randomizePitch)   => PlaySound(audioClipName, volume, pitch, randomizePitch, Vector3.zero);
    [Server] public static void PlaySound(string audioClipName, float volume, float pitch, bool randomizePitch, Vector3 location)
    {
        _instance.ClientRpcPlaySoundPrivate(audioClipName, volume, pitch, randomizePitch, location);
        if (_instance.playSoundOnDedicatedServer && _instance.isServerOnly) 
            _instance.PlaySoundPrivate(audioClipName, volume, pitch, randomizePitch, location, _instance.unreachableNetId);
    }
    [ClientRpc] void ClientRpcPlaySoundPrivate(string audioClipName, float volume, float pitch, bool randomizePitch, Vector3 location) 
        => PlaySoundPrivate(audioClipName, volume, pitch, randomizePitch, location, unreachableNetId);

    void PlaySoundPrivate(string audioClipName, float volume, float pitch, bool randomizePitch, Vector3 location, uint netIdToExclude)
    {
        if (netId == netIdToExclude) return;

        if (!_instance.effectAudioClipList.Any(x => x.Key == audioClipName))
        {
            Debug.Log("Sound name: " + audioClipName + " not found in SoundManager's Inspector list found in the unity editor.");
            return;
        }
        
        if (soundObjectPool[currentSoundObject] != null && soundObjectPool[currentSoundObject].clip != null) 
        soundObjectPool[currentSoundObject].Stop();
        soundObjectPool[currentSoundObject].clip = effectAudioClipList.Find(x => x.Key == audioClipName).Value;

        if (randomizePitch) pitch = pitch * Random.Range(randomizePitchLowMult, randomizePitchHighMult);

        soundObjectPool[currentSoundObject].gameObject.transform.position = location;

        soundObjectPool[currentSoundObject].volume = volume;
        soundObjectPool[currentSoundObject].pitch = pitch;
        soundObjectPool[currentSoundObject].Play();

        currentSoundObject++;
        if (currentSoundObject >= soundObjectPool.Length) currentSoundObject = 0;
    }


    //start new music on server (if enabled) and all clients from a client
    [Command(requiresAuthority = false)] public void CmdPlayMusic(string musicAudioClipName) => ServerPlayMusic(musicAudioClipName);

    //start new music on server (if enabled) and all clients from server
    [Server] public static void ServerPlayMusic(string musicAudioClipName)
    {
        if (_instance.playSoundOnDedicatedServer) _instance.PlayMusicPrivate(musicAudioClipName);
        _instance.RpcPlayMusic(musicAudioClipName); 
    }

    [ClientRpc]
    void RpcPlayMusic(string musicAudioClipName)
    {
        PlayMusicPrivate(musicAudioClipName);
    }
    void PlayMusicPrivate(string musicAudioClipName)
    {
        if (!musicAudioClipList.Any(x => x.Key == musicAudioClipName))
        {
            Debug.Log("Sound name: " + musicAudioClipName + " not found in SoundManager's Inspector musicAudioClipList found in the unity editor.");
            return;
        }

        if (musicAudioSources[currentMusicAudioSource].clip != null)
            musicAudioSources[currentMusicAudioSource].Stop();

        musicAudioSources[currentMusicAudioSource].clip = musicAudioClipList.Find(x => x.Key == musicAudioClipName).Value;

        StopCoroutine("FadeInAudioSource");
        StopCoroutine("FadeOutAudioSourceAndStop");
        musicAudioSources[currentMusicAudioSource].volume = 0;
        musicAudioSources[currentMusicAudioSource].time = 0;
        musicAudioSources[currentMusicAudioSource].Play();
        
        StartCoroutine(FadeInAudioSource(musicAudioSources[currentMusicAudioSource]));
        currentMusicAudioSource++;
        if (currentMusicAudioSource >= musicAudioSources.Length)
            currentMusicAudioSource = 0;
        StartCoroutine(FadeOutAudioSourceAndStop(musicAudioSources[currentMusicAudioSource]));
    }


    [Server] public static void StopMusic()
    {
        _instance.RpcStopMusic(); //stop on clients
        if (_instance.playSoundOnDedicatedServer) _instance.StopAllMusic(); //stop on server
    }

    [ClientRpc] private void RpcStopMusic() => _instance.StopAllMusic();


    //local commands for pausing/unpausing music, might expand later when needed. unused atm. Will cause de-sync of audio tracks on clients since it just happens locally atm
    public static void PauseMusic()
    {   if (_instance.musicAudioSources[_instance.currentMusicAudioSource] != null) _instance.musicAudioSources[_instance.currentMusicAudioSource].Pause(); }
    public static void UnPauseMusic()
    {   if (_instance.musicAudioSources[_instance.currentMusicAudioSource] != null) _instance.musicAudioSources[_instance.currentMusicAudioSource].UnPause(); }


    private void StopAllMusic()
    {
        foreach (var audioSource in _instance.musicAudioSources)
        {
            StartCoroutine(FadeOutAudioSourceAndStop(audioSource));
        }
    }

    IEnumerator FadeOutAudioSourceAndStop(AudioSource audioSource)
    {
        if (crossfadeTime <= 0)
            yield return new WaitForSeconds(Mathf.Abs(crossfadeTime));
        else
        {
            int Steps = (int)(volumeChangesPerSecond * crossfadeTime);
            float StepTime = crossfadeTime / Steps;
            float StepSize = (0 - audioSource.volume) / Steps;

            //Fade now
            for (int i = 1; i < Steps; i++)
            {
                audioSource.volume += StepSize;
                yield return new WaitForSeconds(StepTime);
            }
        }

        audioSource.volume = 0;
        audioSource.Stop();
    }

    IEnumerator FadeInAudioSource(AudioSource audioSource)
    {        
        if (crossfadeTime <= 0) 
            yield return new WaitForSeconds(Mathf.Abs(crossfadeTime));
        else
        {
            int Steps = (int)(volumeChangesPerSecond * crossfadeTime);
            float StepTime = crossfadeTime / Steps;
            float StepSize = (defaultMusicVolume - audioSource.volume) / Steps;

            //Fade now
            for (int i = 1; i < Steps; i++)
            {
                audioSource.volume += StepSize;
                yield return new WaitForSeconds(StepTime);
            }
        }

        audioSource.volume = defaultMusicVolume;

        yield break;
    }

}
