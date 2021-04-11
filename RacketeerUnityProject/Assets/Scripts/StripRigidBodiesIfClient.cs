using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class StripRigidBodiesIfClient : NetworkBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        if (isClient)
        {
            //Destroy(gameObject.GetComponent<Rigidbody>());
            Destroy(gameObject.GetComponent<BoxCollider>());
        }
            
    }

}
