using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DissapearOnBallCollide : NetworkBehaviour
{
    public AudioClip collectedSound;
    bool beingDestroyed = false;
    public GameObject explosionPrefab;
 
    private void OnTriggerEnter(Collider other)
    {        
        if (isServer && GameManager.Instance.playerNumberOwningBall >= 0 && !beingDestroyed && other.gameObject.tag == "Ball") // ball only has collider on server, so this only runs on server
        {
            beingDestroyed = true;
            CmdPlaySound();
            CoinSpawner.SpawnNewCoin();
            StartCoroutine(DestroyAfterTime(0.05f, GameManager.Instance.playerNumberOwningBall));
            GameManager.AddPointForOwningPlayer();
        }
    }

    IEnumerator DestroyAfterTime(float time, int playerNumber)
    {
        yield return new WaitForSeconds(time);
        Debug.Log("loading xplosion for player: " + playerNumber);
        GameObject.Instantiate(GameManager.Instance.playerExplosions[playerNumber], gameObject.transform.position, Quaternion.identity);
        RpcLoadExplosion(playerNumber);
        Destroy(this.gameObject);
    }
    [ClientRpc]
    private void RpcLoadExplosion(int playerNumber)
    {
        GameObject.Instantiate(GameManager.Instance.playerExplosions[playerNumber], gameObject.transform.position, Quaternion.identity);
    }

    [ClientRpc]
    private void CmdPlaySound()
    {
        SoundManager.PlaySound(collectedSound);
    }

}
