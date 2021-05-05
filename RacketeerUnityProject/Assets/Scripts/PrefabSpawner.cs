using System;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public class PrefabSpawner : NetworkBehaviour
{
    public List<InspectableKvp<string, GameObject>> spawnablePrefabList;
    
    private static PrefabSpawner _instance;
    public static PrefabSpawner Instance { get { if (_instance == null) { _instance = GameObject.FindObjectOfType<PrefabSpawner>(); } return _instance; } }
    void Awake() => _instance = gameObject.GetComponent<PrefabSpawner>();
    public static void SpawnNewObject(string prefabSpawnName, Vector3 position, Quaternion rotation) => _instance.SpawnObject(prefabSpawnName, position, rotation);
    void SpawnObject(string prefabSpawnName, Vector3 position, Quaternion rotation)
    {
        if (!isServer || !spawnablePrefabList.Any(x => x.Key == prefabSpawnName)) return;

        GameObject prefabReference = spawnablePrefabList.Find(x => x.Key == prefabSpawnName).Value;

        GameObject newObject = GameObject.Instantiate(prefabReference, position, rotation);
        NetworkServer.Spawn(newObject);
    }
}
