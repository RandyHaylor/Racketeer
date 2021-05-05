using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundThenDestroySelf : MonoBehaviour
{
    AudioSource audioSource;
    private void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
    }
    public void PlayThenDisable(AudioClip audioClip, float volume, float pitch)
    {
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.Play();
        StartCoroutine(DisableSelf(audioClip.length + 0.1f));
    }

    IEnumerator DisableSelf(float timeToDisableSelf)
    {
        yield return new WaitForSeconds(timeToDisableSelf);
        gameObject.SetActive(false);
    }
}
