using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinSpawner : NetworkBehaviour
{
    public GameObject CoinPrefab;

    private static CoinSpawner _instance;

    public static CoinSpawner Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<CoinSpawner>();
            }

            return _instance;
        }
    }

    void Awake()
    {
        _instance = gameObject.GetComponent<CoinSpawner>();
        //DontDestroyOnLoad(gameObject);
    }
 
   
    public static void SpawnNewCoin()
    {
        _instance.SpawnCoin();
    }
    void SpawnCoin()
    {
        if (!isServer) return;

        GameObject newCoin = GameObject.Instantiate(CoinPrefab, new Vector3(Random.Range(-8, 8), Random.Range(-4, 4), 0), CoinPrefab.transform.rotation);
        NetworkServer.Spawn(newCoin);
    }
}
