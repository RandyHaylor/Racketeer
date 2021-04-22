using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ignoreCollisionFromNonServerPlayer : NetworkBehaviour
{
    bool active = true;
    Collider myCollider;
    private void Awake()
    {
        myCollider = gameObject.GetComponent<Collider>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player" && !collision.gameObject.GetComponent<NetworkIdentity>().isServer)
        {
            Physics.IgnoreCollision(collision.collider, myCollider);
        }
    }

}
