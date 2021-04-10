using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundWhenCollide : MonoBehaviour
{
    AudioSource source;
    private void Start()
    {
        source = GetComponent<AudioSource>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        source.Play();
    }


}
