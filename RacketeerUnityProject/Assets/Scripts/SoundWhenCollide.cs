using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundWhenCollide : NetworkBehaviour
{
    public AudioClip bounceSound;
    Rigidbody rigidbody;
    private void Awake()
    {
        rigidbody = gameObject.GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isServer)
            CmdPlaySound(rigidbody.velocity.magnitude);
    }

    [ClientRpc]
    private void CmdPlaySound(float rigidbodySpeed)
    {
        SoundManager.PlaySound(bounceSound, 0.3f + Mathf.Clamp(rigidbodySpeed / 25f, 0f, 0.7f), 1f, false);        
    }


}
