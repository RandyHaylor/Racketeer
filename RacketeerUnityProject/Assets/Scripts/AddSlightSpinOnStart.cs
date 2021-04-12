using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AddSlightSpinOnStart : NetworkBehaviour
{

    private void Start()
    {
        if (isServer)
        AddTorque(new Vector3(UnityEngine.Random.Range(-2000f, 2000f), UnityEngine.Random.Range(-2000f, 2000f), UnityEngine.Random.Range(-2000f, 2000f)));
    }
    void AddTorque(Vector3 torque)
    {
        Debug.Log("Adding initial torque, isServer: " + isServer);
        gameObject.GetComponent<Rigidbody>().AddTorque(torque);
    }


}
