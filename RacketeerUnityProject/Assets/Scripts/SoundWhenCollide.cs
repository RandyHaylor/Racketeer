using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundWhenCollide : NetworkBehaviour
{
    public string wallBounceSoundName;
    public string playerBounceSoundName;
    [Range(0,1)]
    public float ballBounceVolume;

    Rigidbody rigidbody;
    private void Awake()
    {
        rigidbody = gameObject.GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        //if (NetworkRigidbodyController.IsResimulating) return; //don't trigger sounds during resimulation of physics frames
        SoundManager.PlaySound
            (collision.transform.tag == "Player" ? playerBounceSoundName : wallBounceSoundName
            , transform.position, SoundManager.UsersToPlayFor.SelfOnly
            , (0.2f + Mathf.Clamp(rigidbody.velocity.magnitude / 25f, 0f, 0.7f)) * ballBounceVolume);
    }

}
