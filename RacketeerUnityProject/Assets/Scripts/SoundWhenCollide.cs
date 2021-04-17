using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundWhenCollide : NetworkBehaviour
{
    public AudioClip bounceSound;
    
    private void OnCollisionEnter(Collision collision)
    {
        if (isServer)
            CmdPlaySound();
    }

    [ClientRpc]
    private void CmdPlaySound()
    {
        SoundManager.PlaySound(bounceSound);
    }


}
