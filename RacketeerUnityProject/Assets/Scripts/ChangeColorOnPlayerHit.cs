using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ChangeColorOnPlayerHit : NetworkBehaviour
{
    Vector3 colorCache; //for sending color info between server & client
    Color tempColor; // to avoid creating a new color object every time the color changes

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

                gameObject.GetComponent<MeshRenderer>().material.color = tempColor;

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
