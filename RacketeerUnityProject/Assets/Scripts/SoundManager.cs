using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;



public class SoundManager : NetworkBehaviour
{
    [Serializable]
    public class AudioItemReference 
    {
        public string AudioClipName;
        [Range(0,1)]
        public float VolumeStrength;
        [HideInInspector]
        public bool playedThisRandomCycle;

        public AudioItemReference(string audioClipName, float volumeStrength)
        {
                AudioClipName = audioClipName;
                VolumeStrength = volumeStrength;
        }
    }

    [Serializable]
    public class SoundControlNode
    {
        public string AudioNodeName;
        [Range(0, 1)]
        public float VolumeMultiplier;
        [Header("Random: true random, Shuffle: non-repeating cycle")]
        public NodeAction nodeAction;
        [Header("Targets can be audioClipNames or other node names")]
        public List<AudioItemReference> TargetNames;
        private int currentTarget;

        public SoundControlNode(string audioNodeName, float volumeMultiplier, List<AudioItemReference> targetNames)
        {
            AudioNodeName = audioNodeName;
            VolumeMultiplier = volumeMultiplier;
            TargetNames = targetNames;
        }

        public enum NodeAction
        {
            playOne_LinearCycle,
            playOne_Random,
            playOne_Shuffle,
            playAllAtOnce
        }
    }

    [Serializable]
    public class AudioItem<K, T>
    {
        [Header("Name To Reference Sound In Method Calls")]
        public K AudioClipName;
        //[Header("Drag Sound File Here")]
        public T AudioClip;
        //[Header("1 is max volume")]
        [Range(0, 1)]
        public float ClipVolume = 1;
        //[Header("Use 0.1 - 0.3 to slow repeating")]
        public float DelayBetweenRepeat = 0.15f;
        //[Header("1 for original pitch")]
        public float Pitch = 1;
        //[Header("Randomize pitch to vary sound")]
        public bool RandomizePitch = false;
        //[Header("Use 0.02-0.05 for subtle effect")]
        public float MaxPitchShift = 0.04f;
        [Header("Only suggested for Music")]
        public bool LoopAudioClip;
        [Header("Optional: assign to a mixer group for using external features")]
        public UnityEngine.Audio.AudioMixerGroup optionalAudioMixerGroup;        
        [HideInInspector]
        public float LastPlayedTime = 0;
    }

    [Serializable]
    public class AudioClipList
    {
        [Range(0, 1)]
        public float groupMasterVolume = 1f; 
        [Header("Optional: assign an AudioSource prefab with additional settings")]
        public GameObject audioSourcePrefab;

        public List<AudioItem<string, AudioClip>> ClipList;

        public void SetDefaults(string audioClipName, float clipVolume, float delayBetweenRepeat, float pitch, bool randomizePitch, float maxPitchShift)
        {
            if (ClipList == null) ClipList = new List<AudioItem<string, AudioClip>>();

            AudioItem<string, AudioClip> defaultValuesAudioItem = new AudioItem<string, AudioClip>();
            defaultValuesAudioItem.AudioClipName = audioClipName;
            defaultValuesAudioItem.ClipVolume = clipVolume;
            defaultValuesAudioItem.DelayBetweenRepeat = delayBetweenRepeat;
            defaultValuesAudioItem.Pitch = pitch;
            defaultValuesAudioItem.RandomizePitch = randomizePitch;
            defaultValuesAudioItem.MaxPitchShift = maxPitchShift;

            ClipList[ClipList.Count-1] = defaultValuesAudioItem;
        }
    }

    public enum UsersToPlayFor
    {
        EveryoneViaServer,
        SelfLocallyThenEveryone,
        SelfOnly
    }

    public enum SoundGroup
    {
        Effects,
        Music
    }



    private AudioSource[] musicAudioSources;
    private List<Coroutine> crossfadeCoroutines;
    private int musicAudioSourcesSize = 2;
    private int currentMusicAudioSource = 0;

    [Header("Music System takes up 2 audio sources when crossfading.")]
    [Space(height: 1)]
    [Header("Set crossfade to negative for a delay between song changes")]
    public float crossfadeTime = 2;
    public float volumeChangesPerSecond = 10;

    
    public bool playSoundOnDedicatedServer = false;
    [Header("Sound Object Pool Options (32 is max simultaneous sounds...)")]
    public int soundObjectPoolSize = 28;

    public AudioClipList musicAudioClipList;
    private int previousMusicAudioClipListCount;

    public AudioClipList effectAudioClipList;
    private int previousEffectAudioClipListCount;

    private AudioClipList currentAudioClipList; //for internal caching
    
    
    [Header("Use sound nodes like audio references, you can randomize, loop through, or play multiple sounds at once")]
    [Space]
    [Header("Optional Sound Control Nodes For Further Grouping/Randomizing")]
    public List<SoundControlNode> soundControlNodes;
    private int previousSoundControlNodesCount;

    private void OnValidate() //populates an item if the lists are empty so unity inspector caches default item values & maintains one example item in list
    {        
        if (musicAudioClipList == null) musicAudioClipList = new AudioClipList();
        if (musicAudioClipList.ClipList.Count == 0)
        {
            musicAudioClipList.ClipList.Add(new AudioItem<string, AudioClip>());

            musicAudioClipList.SetDefaults("Name To Reference Sound", 0.4f, 0.15f, 1f, false, 0.04f);
            previousMusicAudioClipListCount = musicAudioClipList.ClipList.Count;
        }

        if (effectAudioClipList == null) effectAudioClipList = new AudioClipList();
        if (effectAudioClipList.ClipList.Count == 0)
        {
            effectAudioClipList.ClipList.Add(new AudioItem<string, AudioClip>());

            effectAudioClipList.SetDefaults("Name To Reference Sound", 0.6f, 0.15f, 1f, false, 0.04f);
            previousEffectAudioClipListCount = effectAudioClipList.ClipList.Count;
        }

        if (soundControlNodes == null) soundControlNodes = new List<SoundControlNode>();
        if (soundControlNodes.Count == 0)
        {
            soundControlNodes.Add(new SoundControlNode("Name to Reference Node", 1, new List<AudioItemReference>()));

            soundControlNodes[soundControlNodes.Count - 1].nodeAction = SoundControlNode.NodeAction.playOne_Random;
            soundControlNodes[soundControlNodes.Count - 1].TargetNames.Add(new AudioItemReference("Name Of Sound or Node", 1));

            previousSoundControlNodesCount = soundControlNodes.Count;
        }
    }


    private AudioSource[] soundObjectPool;
    private int currentSoundObject = 0; //keeps track of most recent sound object used so oldest sound object gets re-used (whether it's playing or not)

    private uint unreachableNetId = 4000000000;

    private AudioItem<string, AudioClip> currentSoundAudioItem;
    private float newPitchCache = 0f;

    private static SoundManager _instance;
    public static SoundManager Instance
    { get {
            if (_instance == null) _instance = GameObject.Find("SoundManager").GetComponent<SoundManager>();
            return _instance;
        } }

    void Awake()
    {
        gameObject.transform.position = Vector3.zero;
        _instance = this;
        InitializeSoundObjectPools();
        foreach (var item in musicAudioClipList.ClipList)  //unity serializer was sticking some weird numbers in this hidden value
        {
            item.LastPlayedTime = 0f;
        }
        foreach (var item in effectAudioClipList.ClipList)
        {
            item.LastPlayedTime = 0f;
        }
    }

    public static void StopMusic() => StopMusic(UsersToPlayFor.EveryoneViaServer);
    public static void StopMusic(UsersToPlayFor usersToPlayFor) => _instance.RouteStopMusicRequest(usersToPlayFor);

    public static void PlayMusic(string audioClipName) => PlayMusic(audioClipName, Vector3.zero);
    public static void PlayMusic(string audioClipName, Vector3 position) => PlayMusic(audioClipName, position, UsersToPlayFor.EveryoneViaServer, 1);
    public static void PlayMusic(string audioClipName, Vector3 position, UsersToPlayFor usersToPlayFor, float volumeStrength) => _instance.RoutePlayAudioRequest(audioClipName, position, usersToPlayFor, volumeStrength, SoundGroup.Music);
    public static void PlaySound(string audioClipName) => PlaySound(audioClipName, Vector3.zero);
    public static void PlaySound(string audioClipName, Vector3 position) => PlaySound(audioClipName, position, UsersToPlayFor.EveryoneViaServer);
    public static void PlaySound(string audioClipName, Vector3 position, UsersToPlayFor usersToPlayFor) => PlaySound(audioClipName, position, usersToPlayFor, 1);
    public static void PlaySound(string audioClipName, Vector3 position, UsersToPlayFor usersToPlayFor, float volumeStrength) => _instance.RoutePlayAudioRequest(audioClipName, position, usersToPlayFor, volumeStrength, SoundGroup.Effects);


    private void RoutePlayAudioRequest(string audioClipName, Vector3 position, UsersToPlayFor usersToPlayFor, float volumeStrength, SoundGroup soundGroup)
    {
        if (isServer && (usersToPlayFor == UsersToPlayFor.EveryoneViaServer || usersToPlayFor == UsersToPlayFor.SelfLocallyThenEveryone))
        {
            //call clientRpc to play sound and play locally on dedicated server if option selected
            ClientRpcPlaySound(audioClipName, position, volumeStrength, unreachableNetId, soundGroup);
            if (playSoundOnDedicatedServer && isServerOnly)
                PlaySoundPrivate(audioClipName, position, volumeStrength, unreachableNetId, soundGroup);

        }
        else if (!isServer && usersToPlayFor == UsersToPlayFor.SelfLocallyThenEveryone)
        {
            //play sound directly
            //call command that calls clientRpc, but supress playback on me
            PlaySoundPrivate(audioClipName, position, volumeStrength, _instance.unreachableNetId, soundGroup);
            CmdPlaySound(audioClipName, position, volumeStrength, _instance.GetComponent<NetworkBehaviour>().netId, soundGroup);
        }
        else if (usersToPlayFor == UsersToPlayFor.SelfOnly)
        {
            if (isServerOnly && !playSoundOnDedicatedServer) return;
            //call sound playing directly
            PlaySoundPrivate(audioClipName, position, volumeStrength, _instance.unreachableNetId, soundGroup);
        }
        else if (!isServer && usersToPlayFor == UsersToPlayFor.EveryoneViaServer)
        {
            //call Command that calls clientRpc to play sound
            CmdPlaySound(audioClipName, position, volumeStrength, _instance.unreachableNetId, soundGroup);
        }
    }

    //play sound on server and all clients except netIdToExclude
    [Command(requiresAuthority = false)] private void CmdPlaySound(string audioClipName, Vector3 position, float volumeStrength, uint netIdToExclude, SoundGroup soundGroup)
    {
        ClientRpcPlaySound(audioClipName, position, volumeStrength, netIdToExclude, soundGroup);
        if (playSoundOnDedicatedServer && isServerOnly)
            PlaySoundPrivate(audioClipName, position, volumeStrength, netIdToExclude, soundGroup);
    }

    //play a sound on all clients except netIdToExclude
    [ClientRpc] private void ClientRpcPlaySound(string audioClipName, Vector3 position, float volumeStrength, uint netIdToExclude, SoundGroup soundGroup)
        => PlaySoundPrivate(audioClipName, position, volumeStrength, netIdToExclude, soundGroup);

    void PlaySoundPrivate(string audioClipName, Vector3 position, float volumeStrength, uint netIdToExclude, SoundGroup soundGroup)
    {
        if (soundGroup == SoundGroup.Effects) currentAudioClipList = effectAudioClipList;
        else if (soundGroup == SoundGroup.Music) currentAudioClipList = musicAudioClipList;

        if (netId == netIdToExclude) return;

        if (!_instance.currentAudioClipList.ClipList.Any(x => x.AudioClipName == audioClipName))
        {
            Debug.Log("Sound name: " + audioClipName + " not found in SoundManager's Inspector list found in the unity editor.");
            return;
        }

        currentSoundAudioItem = currentAudioClipList.ClipList.Find(x => x.AudioClipName == audioClipName);

        if (currentSoundAudioItem.AudioClip == null)
        {
            Debug.Log("AudioClip not assigned for sound effect item named: " + audioClipName);
            return;
        }

        if (currentSoundAudioItem.DelayBetweenRepeat > Time.time - currentSoundAudioItem.LastPlayedTime)
        {
            //Debug.Log("audioClip supressed due to being played too soon: " + audioClipName);
            return;
        }            
        else
            currentSoundAudioItem.LastPlayedTime = Time.time;


        if (currentSoundAudioItem.RandomizePitch) newPitchCache = currentSoundAudioItem.Pitch + UnityEngine.Random.Range(-1 * currentSoundAudioItem.MaxPitchShift, currentSoundAudioItem.MaxPitchShift);
        else newPitchCache = currentSoundAudioItem.Pitch;

        if (soundGroup == SoundGroup.Music)
        {
            if (musicAudioSources[currentMusicAudioSource].clip != null) musicAudioSources[currentMusicAudioSource].Stop();

            StopCrossfadeCoroutines();

            crossfadeCoroutines.Add(StartCoroutine(FadeOutAudioSourceAndStop(musicAudioSources[currentMusicAudioSource])));

            currentMusicAudioSource++;
            if (currentMusicAudioSource >= musicAudioSources.Length)
                currentMusicAudioSource = 0;

            musicAudioSources[currentMusicAudioSource].loop = currentSoundAudioItem.LoopAudioClip;
            musicAudioSources[currentMusicAudioSource].clip = currentSoundAudioItem.AudioClip;
            musicAudioSources[currentMusicAudioSource].transform.position = position;
            musicAudioSources[currentMusicAudioSource].volume = 0;
            musicAudioSources[currentMusicAudioSource].time = 0; 
            musicAudioSources[currentMusicAudioSource].pitch = newPitchCache;

            musicAudioSources[currentMusicAudioSource].Play();

            crossfadeCoroutines.Add(StartCoroutine(
                FadeInAudioSource
                    (musicAudioSources[currentMusicAudioSource], currentSoundAudioItem.ClipVolume* volumeStrength * currentAudioClipList.groupMasterVolume)
                ));

        }
        else if (soundGroup == SoundGroup.Effects)
        {
            if (soundObjectPool[currentSoundObject].clip != null && soundObjectPool[currentSoundObject].isPlaying)
            {   // if the next AudioSource object is still playing, skip it so we don't cut off longer sounds. continue until we've looped the pool then stop something and play the new sound
                for (int i = 0; i < soundObjectPool.Length; i++)
                {
                    currentSoundObject++;
                    if (currentSoundObject >= soundObjectPool.Length) currentSoundObject = 0;
                    if (!soundObjectPool[currentSoundObject].isPlaying) break;
                }

                if (soundObjectPool[currentSoundObject].isPlaying)
                    soundObjectPool[currentSoundObject].Stop(); //we've looped the entire pool and not found a stopped audio source to grab, so we're stopping one and using it
            }            

            soundObjectPool[currentSoundObject].loop = currentSoundAudioItem.LoopAudioClip;
            soundObjectPool[currentSoundObject].clip = currentSoundAudioItem.AudioClip;
            soundObjectPool[currentSoundObject].pitch = newPitchCache;
            soundObjectPool[currentSoundObject].gameObject.transform.position = position;
            soundObjectPool[currentSoundObject].volume = currentSoundAudioItem.ClipVolume * volumeStrength * currentAudioClipList.groupMasterVolume;

            if (currentSoundAudioItem.optionalAudioMixerGroup != null)
                soundObjectPool[currentSoundObject].outputAudioMixerGroup = currentSoundAudioItem.optionalAudioMixerGroup;

            soundObjectPool[currentSoundObject].Play();

            currentSoundObject++;
            if (currentSoundObject >= soundObjectPool.Length) currentSoundObject = 0;
        }
    }

    private void RouteStopMusicRequest(UsersToPlayFor usersToPlayFor)
    {
        if (usersToPlayFor == UsersToPlayFor.EveryoneViaServer)
        {
            if (_instance.isServer)
            {
                _instance.RpcStopMusic(); //stop on clients
                if (isServerOnly) StopAllMusic();
            }                
            else
                _instance.CmdStopMusic();
        }
        else if (usersToPlayFor == UsersToPlayFor.SelfLocallyThenEveryone)
        {
            _instance.StopAllMusic();
            if (_instance.isServer)
                _instance.RpcStopMusic(); //stop on clients
            else
                _instance.CmdStopMusic();
        }
        else if (usersToPlayFor == UsersToPlayFor.SelfOnly)
        {
            _instance.StopAllMusic();
        }
           
        if (_instance.isServerOnly) _instance.StopAllMusic(); //stop on server
    }
    [Command] private void CmdStopMusic() => StopAllMusic();
    [ClientRpc] private void RpcStopMusic() => StopAllMusic();

    //better to use a separate system to lower overall volume    
    public static void PauseMusic()
    {   if (_instance.musicAudioSources[_instance.currentMusicAudioSource] != null) _instance.musicAudioSources[_instance.currentMusicAudioSource].Pause(); }
    public static void UnPauseMusic()
    {   if (_instance.musicAudioSources[_instance.currentMusicAudioSource] != null) _instance.musicAudioSources[_instance.currentMusicAudioSource].UnPause(); }

    private void StopCrossfadeCoroutines()
    {
        foreach (var fadeCoroutine in crossfadeCoroutines)
        {
            StopCoroutine(fadeCoroutine);            
        }

        crossfadeCoroutines.Clear();
    }

    private void StopAllMusic()
    {
        StopCrossfadeCoroutines();
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

    IEnumerator FadeInAudioSource(AudioSource audioSource, float targetVolume)
    {        
        if (crossfadeTime <= 0) 
            yield return new WaitForSeconds(Mathf.Abs(crossfadeTime));
        else
        {
            int Steps = (int)(volumeChangesPerSecond * crossfadeTime);
            float StepTime = crossfadeTime / Steps;
            float StepSize = (targetVolume - audioSource.volume) / Steps;

            //Fade now
            for (int i = 1; i < Steps; i++)
            {
                audioSource.volume += StepSize;
                yield return new WaitForSeconds(StepTime);
            }
        }

        audioSource.volume = targetVolume;

        yield break;
    }

    private void InitializeSoundObjectPools()
    {
        musicAudioSources = new AudioSource[2];
        crossfadeCoroutines = new List<Coroutine>(); 
        GameObject newGameObject;
        soundObjectPool = new AudioSource[soundObjectPoolSize];
        for (int i = 0; i < soundObjectPool.Length; i++)
        {
            if (effectAudioClipList.audioSourcePrefab == null || !effectAudioClipList.audioSourcePrefab.GetComponent<AudioSource>())
            {
                newGameObject = GameObject.Instantiate(new GameObject());
                newGameObject.name = "soundEffectPoolAudioSource";
                newGameObject.AddComponent<AudioSource>();
            }
            else
                newGameObject = GameObject.Instantiate(effectAudioClipList.audioSourcePrefab);

            newGameObject.transform.SetParent(gameObject.transform);
            soundObjectPool[i] = newGameObject.GetComponent<AudioSource>();
            soundObjectPool[i].GetComponent<AudioSource>().playOnAwake = false;
        }

        for (int i = 0; i < musicAudioSourcesSize; i++)
        {
            if (musicAudioClipList.audioSourcePrefab == null || !musicAudioClipList.audioSourcePrefab.GetComponent<AudioSource>())
            {
                newGameObject = GameObject.Instantiate(new GameObject());
                newGameObject.AddComponent<AudioSource>();
                newGameObject.name = "musicAudioSource";
            }
            else
                newGameObject = GameObject.Instantiate(musicAudioClipList.audioSourcePrefab);


            newGameObject.transform.SetParent(gameObject.transform);

            musicAudioSources[i] = newGameObject.GetComponent<AudioSource>();
            musicAudioSources[i].playOnAwake = false;
        }
    }
}
