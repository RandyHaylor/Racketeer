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
    public void PlayThenDestroy(AudioClip audioClip)
    {
        audioSource.clip = audioClip;
        audioSource.Play();
        StartCoroutine(DestroySelf(audioClip.length + 0.5f));
    }

    IEnumerator DestroySelf(float timeToSelfDestruct)
    {
        yield return new WaitForSeconds(timeToSelfDestruct);
        Destroy(gameObject);
    }
}
