using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Mirror.Experimental;

public class StripRigidBodiesIfClient : NetworkBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        if (isClient)
        {
            Destroy(gameObject.GetComponent<BoxCollider>());
        }
            
    }

}
