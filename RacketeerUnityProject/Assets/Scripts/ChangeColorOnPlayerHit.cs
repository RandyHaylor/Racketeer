using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ChangeColorOnPlayerHit : NetworkBehaviour
{
    Vector3 colorCache; //for sending color info between server & client
    Color tempColor; // to avoid creating a new color object every time the color changes
    int newPlayerNumber;
    public string newPlayerGainedBallSoundName = "GainedBallSound";
    [Range(0, 1)]
    public float gainedBallSoundVolume = 0.7f;
    string _TintColor = "_TintColor";

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            if (isServer)
            {
                newPlayerNumber = collision.gameObject.GetComponent<NetworkIdentity>().playerNumber;
                if (GameManager.Instance.playerNumberOwningBall != newPlayerNumber)
                    SoundManager.PlaySound(newPlayerGainedBallSoundName, gainedBallSoundVolume, 1, false);
                GameManager.Instance.playerNumberOwningBall = newPlayerNumber;

                colorCache.x = GameManager.Instance.playerColors[GameManager.Instance.playerNumberOwningBall].r;//collision.gameObject.GetComponent<MeshRenderer>().material.color.r;
                colorCache.y = GameManager.Instance.playerColors[GameManager.Instance.playerNumberOwningBall].b;
                colorCache.z = GameManager.Instance.playerColors[GameManager.Instance.playerNumberOwningBall].g;

                gameObject.GetComponent<MeshRenderer>().material.SetColor(_TintColor, GameManager.Instance.playerColors[GameManager.Instance.playerNumberOwningBall]);

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
