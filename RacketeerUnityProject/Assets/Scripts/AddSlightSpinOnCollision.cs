using UnityEngine;
using Mirror;

public class AddSlightSpinOnCollision : NetworkBehaviour
{
    // Start is called before the first frame update
    void OnCollisionEnter(Collision collision)
    {
        if (isServer)
            AddTorque(new Vector3(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f)));
    }

    void AddTorque(Vector3 torque)
    {        
        gameObject.GetComponent<Rigidbody>().AddTorque(torque);
    }
}
