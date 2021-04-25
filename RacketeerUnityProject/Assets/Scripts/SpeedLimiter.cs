using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpeedLimiter : NetworkBehaviour
{
    public float speedLimit = 20;

    Rigidbody rigidBody;

    void FixedUpdate()
    {
        if (isServer && rigidBody == null)
        {
            if (GetComponent<Rigidbody>() != null)
                rigidBody = GetComponent<Rigidbody>();
        }

        if (rigidBody != null)
        {
            if (rigidBody.velocity.sqrMagnitude > speedLimit * speedLimit)
            {
                rigidBody.velocity = speedLimit * rigidBody.velocity.normalized;
            }
        }
            
                
    }
}
