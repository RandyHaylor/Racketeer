using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DissapearOnBallCollide : NetworkBehaviour
{
    public string collectedSound;
    [Range(0,1)]
    public float collectedSoundVolume = 0.7f;
    bool beingDestroyed = false;
    public bool spanwNewItemCollected = true;
    public string prefabNameToSpawn;


    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Coin triggerEnter detected, object: " + other.gameObject.tag + " isServer? " + isServer + " playerOwningBall: " + GameManager.Instance.playerNumberOwningBall);
        if (isServer && GameManager.Instance.playerNumberOwningBall >= 0 && !beingDestroyed && other.gameObject.tag == "Ball") // ball only has collider on server, so this only runs on server
        {
            //Debug.Log("Coin being removed");
            beingDestroyed = true;
            SoundManager.PlaySound(collectedSound, collectedSoundVolume, 1, false);
            if (spanwNewItemCollected) PrefabSpawner.SpawnNewObject(prefabNameToSpawn, new Vector3(Random.Range(-8f, 8f), Random.Range(-4f, 4f), 0f), Quaternion.identity*Quaternion.Euler(90, 0, 0));
            StartCoroutine(DestroyAfterTime(0.05f, GameManager.Instance.playerNumberOwningBall));
            GameManager.AddPointForOwningPlayer();
        }
    }

    IEnumerator DestroyAfterTime(float time, int playerNumber)
    {
        yield return new WaitForSeconds(time);
        //Debug.Log("loading xplosion for player: " + playerNumber);
        GameObject.Instantiate(GameManager.Instance.playerExplosions[playerNumber], gameObject.transform.position, Quaternion.identity);
        RpcLoadExplosion(playerNumber);
        Destroy(this.gameObject);
    }
    [ClientRpc]
    private void RpcLoadExplosion(int playerNumber)
    {
        GameObject.Instantiate(GameManager.Instance.playerExplosions[playerNumber], gameObject.transform.position, Quaternion.identity);
    }



}
