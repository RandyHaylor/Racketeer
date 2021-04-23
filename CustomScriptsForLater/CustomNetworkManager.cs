using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

    public class CustomNetworkManager : NetworkManager
    {
        new public List<GameObject> playerPrefabs;
        public GameObject defaultPlayerPrefab;


        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            Transform startPos = base.GetStartPosition();
            GameObject newPlayer;
            if (playerPrefabs != null && playerPrefabs.Count > 0)
            {
                newPlayer = playerPrefabs[0];
                playerPrefabs.RemoveAt(0);
            }
            else
                newPlayer = defaultPlayerPrefab;

            GameObject player = startPos != null
                ? Instantiate(newPlayer, startPos.position, startPos.rotation)
                : Instantiate(newPlayer);


            // instantiating a "Player" prefab gives it the name "Player(clone)"
            // => appending the connectionId is WAY more useful for debugging!
            player.name = $"{playerPrefabs[0].name} [connId={conn.connectionId}]";
            NetworkServer.AddPlayerForConnection(conn, player);
        }
    }

