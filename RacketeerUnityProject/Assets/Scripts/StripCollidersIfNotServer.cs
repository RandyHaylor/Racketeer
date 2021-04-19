using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Mirror.Experimental;

public class StripCollidersIfNotServer : NetworkBehaviour
{
    public bool stripCollidersIfNotServer;
    // Start is called before the first frame update
    void Awake()
    {
        if (stripCollidersIfNotServer && NetworkServer.connections.Count == 0)
        {
            Collider[] gameObjectColliders = gameObject.GetComponents<Collider>();
            foreach (Collider collider in gameObjectColliders)
            {
                DestroyImmediate(collider);
            }

            Collider[] childColliders = gameObject.GetComponentsInChildren<Collider>();
            foreach (Collider collider in childColliders)
            {
                DestroyImmediate(collider);
            }
         
        }
    }

}
