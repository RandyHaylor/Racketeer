using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundWhenCollide : NetworkBehaviour
{
    public AudioClip bounceSound;
    [Server]
    private void OnCollisionEnter(Collision collision)
    {
        CmdPlaySound();
    }

    [ClientRpc]
    private void CmdPlaySound()
    {
        SoundManager.PlaySound(bounceSound);
    }


}
