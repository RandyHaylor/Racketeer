using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PickupPowerup : NetworkBehaviour
{


    public GameObject powerupPrefab;
    bool grantingPowerup = false; //prevent giving to multiple players by accident or giving multiple times

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collision with powerup");
        if (isServer && other.gameObject.tag == "Player" && !grantingPowerup)
        {
            //prevent granting more than once
            grantingPowerup = true;
            //find and remove any other player abilities on server
            var currentPlayerAbilities = other.gameObject.GetComponentsInChildrenWithTag<Transform>("PlayerAbility");
            if (currentPlayerAbilities.Length > 0)
                for (int i = 0; i < currentPlayerAbilities.Length; i++)
                    Destroy(currentPlayerAbilities[i].gameObject);
            //attach powerup ability prefab to player on server
            GameObject.Instantiate(powerupPrefab, other.gameObject.transform);
            //create the powerup on non-server client player objects
            RpcCreatePowerupAbilityOnPlayer(other.gameObject.GetComponent<NetworkBehaviour>().netId);

            //remove the powerup item from play
            NetworkServer.Destroy(gameObject);
        }
    }


    [ClientRpc] void RpcCreatePowerupAbilityOnPlayer(uint playerNetId)
    {
        if (isServer) return; //already done on hosting client
        //remove any existing player abilities
        var currentPlayerAbilities = NetworkIdentity.spawned[playerNetId].gameObject.GetComponentsInChildrenWithTag<Transform>("PlayerAbility");
        if (currentPlayerAbilities.Length > 0)
            for (int i = 0; i < currentPlayerAbilities.Length; i++)
                Destroy(currentPlayerAbilities[i].gameObject);

        GameObject.Instantiate(powerupPrefab, NetworkIdentity.spawned[playerNetId].gameObject.transform);

    }

}
public static class Helper
{
    public static T[] GetComponentsInChildrenWithTag<T>(this GameObject gameObject, string tag)
            where T : Component
    {
        List<T> results = new List<T>();

        if (gameObject.CompareTag(tag))
            results.Add(gameObject.GetComponent<T>());

        foreach (Transform t in gameObject.transform)
            results.AddRange(t.gameObject.GetComponentsInChildrenWithTag<T>(tag));

        return results.ToArray();
    }
}