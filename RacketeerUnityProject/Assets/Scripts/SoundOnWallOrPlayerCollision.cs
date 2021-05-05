using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundOnWallOrPlayerCollision : MonoBehaviour
{
    public string audioClipName;
    [Range(0,1)]
    public float audioClipVolume = 0.7f;
    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.other.CompareTag("Wall") || collision.other.CompareTag("Player") && rb.velocity.sqrMagnitude > 0.4f*GameManager.Instance.playerSpeedLimit* GameManager.Instance.playerSpeedLimit)
        {
            SoundManager.PlaySound(audioClipName, audioClipVolume*(0.2f + Mathf.Clamp(rb.velocity.magnitude /25f, 0f, 1)), 0.8f,true);
        }
    }
}
