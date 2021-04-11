using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DissapearOnBallCollide : NetworkBehaviour
{
    public AudioClip collectedSound;
    bool beingDestroyed = false;
 
    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (!beingDestroyed && other.gameObject.tag == "Ball") // ball only has collider on server, so this only runs on server
        {
            beingDestroyed = true;
            CmdPlaySound();
            CoinSpawner.SpawnNewCoin();
            StartCoroutine(DestroyAftertime(0.1f));
        }
    }

    IEnumerator DestroyAftertime(float time)
    {
        Debug.Log("Ran destroyaftertime");
        yield return new WaitForSeconds(time);
        Destroy(this.gameObject);
    }

    [ClientRpc]
    private void CmdPlaySound()
    {
        SoundManager.PlaySound(collectedSound);
    }

}
