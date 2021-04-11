using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColorOnPlayerHit : MonoBehaviour
{

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            gameObject.GetComponent<MeshRenderer>().material = collision.gameObject.GetComponent<MeshRenderer>().material;
        }
    }
}
