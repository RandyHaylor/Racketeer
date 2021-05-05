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
    public float pitchMultiplier = 1;
    public bool randomizePitch = false;

    Rigidbody rigidbody;
    private void Awake()
    {
        rigidbody = gameObject.GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isServer)
        {
            CmdPlaySound(rigidbody.velocity.magnitude, collision.transform.tag == "Player" ? playerBounceSoundName : wallBounceSoundName);
        }
            
    }

    [ClientRpc]
    private void CmdPlaySound(float rigidbodySpeed, string audioClipName)
    {
        SoundManager.PlaySound(audioClipName, (0.2f + Mathf.Clamp(rigidbodySpeed / 25f, 0f, 0.7f))* ballBounceVolume, pitchMultiplier, randomizePitch);        
    }


}
