using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LevelManager : NetworkBehaviour
{
    [SerializeField]
    List<GameObject> disableOnLevelStart;
    [SerializeField]
    List<GameObject> enableOnLevelStart;
    [SerializeField]
    GameObject coinPrefab;

    List<GameObject> coinSpawnPoints;

    int coinsCollected = 0;
    int coinCount = 0;

    GameObject newCoin;

    private void Start()
    {
        coinSpawnPoints = GameObject.FindGameObjectsWithTag("LevelCoinSpawn").ToList<GameObject>();
    }

    public void StartLevel()
    {
        DisableLevelstartObjects();
        EnableLevelStartObjects();
        RpcStartLevel();
        SpawnCoins();
    }

    [ClientRpc]
    void RpcStartLevel()
    {
        DisableLevelstartObjects();
        EnableLevelStartObjects();
    }



    void SpawnCoins()
    {
        coinCount = coinSpawnPoints.Count;
        coinsCollected = 0;
        foreach (var coinSpawnPoint in coinSpawnPoints)
        {
            newCoin = GameObject.Instantiate(coinPrefab, coinSpawnPoint.transform.position, coinPrefab.transform.rotation);
            newCoin.GetComponent<DissapearOnBallCollide>().spanwNewCoinWhenCollected = false;
            NetworkServer.Spawn(newCoin);
        }
    }

    public void CoinCollected()
    {
        coinsCollected++;

        if (coinsCollected >= coinCount)
        {
            GameManager.EndLevel();
        }
    }

    public void ResetLevel()
    {
        RpcResetLevel();
        foreach (var item in disableOnLevelStart)
        {
            item.SetActive(true);
        }
        foreach (var item in enableOnLevelStart)
        {
            item.SetActive(false);
        }
    }

    [ClientRpc]
    void RpcResetLevel()
    {
        foreach (var item in disableOnLevelStart)
        {
            item.SetActive(true);
        }
        foreach (var item in enableOnLevelStart)
        {
            item.SetActive(false);
        }
    }


    void DisableLevelstartObjects()
    {
        foreach (var item in disableOnLevelStart)
        {
            item.SetActive(false);
        }
    }

    void EnableLevelStartObjects()
    {
        foreach (var item in enableOnLevelStart)
        {
            item.SetActive(true);
        }
    }
}
