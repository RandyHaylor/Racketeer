using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ChangeColorOnPlayerHit : NetworkBehaviour
{
    Vector3 colorCache;
    Color tempColor;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            if (isServer)
            {
                GameManager.Instance.playerNumberOwningBall = collision.gameObject.GetComponent<NetworkIdentity>().playerNumber;

                colorCache.x = collision.gameObject.GetComponent<MeshRenderer>().material.color.r;
                colorCache.y = collision.gameObject.GetComponent<MeshRenderer>().material.color.g;
                colorCache.z = collision.gameObject.GetComponent<MeshRenderer>().material.color.b;

                RpcUpdateSphereColor(colorCache);
            }

        }
    }

    [ClientRpc]
    void RpcUpdateSphereColor(Vector3 colorRGB)
    {
        tempColor.r = colorRGB.x;
        tempColor.g = colorRGB.y;
        tempColor.b = colorRGB.z;

        gameObject.GetComponent<MeshRenderer>().material.color = tempColor;
    }
}
