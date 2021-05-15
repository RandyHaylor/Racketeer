using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkRigidbodyControllerObject : NetworkBehaviour
{
    
    void Awake()
    {
        StartCoroutine(RegisterWithNetworkRigidbodyController());
    }

    IEnumerator RegisterWithNetworkRigidbodyController()
    {
        yield return new WaitForEndOfFrame();
        if (NetworkRigidbodyController.Instance != null)
            NetworkRigidbodyController.Instance.RegisterSphere(netId, gameObject.GetComponent<Rigidbody>());    
    }


}
