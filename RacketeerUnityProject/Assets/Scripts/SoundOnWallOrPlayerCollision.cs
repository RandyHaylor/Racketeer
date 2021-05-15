using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SoundOnWallOrPlayerCollision : NetworkBehaviour
{
    public string audioClipName;
    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        //if (NetworkRigidbodyController.IsResimulating) return; //don't trigger sounds during resimulation of physics frames
          
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Player") && rb.velocity.sqrMagnitude > 0.4f*GameManager.Instance.playerSpeedLimit* GameManager.Instance.playerSpeedLimit)
        {
            //Debug.Log("Playing a collision sound");
            SoundManager.PlaySound(audioClipName, transform.position, SoundManager.UsersToPlayFor.SelfOnly, (0.2f + Mathf.Clamp(rb.velocity.magnitude /25f, 0f, 1)));
        }
    }
}
