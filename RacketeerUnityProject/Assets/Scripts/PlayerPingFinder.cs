using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPingFinder : NetworkBehaviour
{
    public float playerPing = 0.01f;
    public int pingsToAverage = 5;
    public float pingSampleInterval = 1;
    Queue<float> pingResponseQueue;
    float newPing;

    void Start()
    {

        if (!isServer)
        {
            pingResponseQueue = new Queue<float>();
            //Debug.Log("starting updatemyping() for " + gameObject.name);
            StartCoroutine(UpdateMyPing());
        }
    }

    IEnumerator UpdateMyPing()
    {
        while (true)
        {
            //Debug.Log("Sending Ping request for GO: " + gameObject.name + " at client time: " + Time.time);
            CmdPingRequestFromClient(Time.time);
            yield return new WaitForSeconds(pingSampleInterval);
        }

    }

    void CalculateAveragePing()
    {
        if (pingResponseQueue != null && pingResponseQueue.Count > 0)
            playerPing = pingResponseQueue.Sum() / pingResponseQueue.Count;
    }

    [Command(requiresAuthority = false)]
    void CmdPingRequestFromClient(float pingSentTime)
    {
        //Debug.Log("Am I server?: " + isServer);
        TargetServerToClientPingResponse(connectionToClient, pingSentTime);
    }

    [TargetRpc]
    void TargetServerToClientPingResponse(NetworkConnection clientConnection, float pingSentTime)
    {
        pingResponseQueue.Enqueue(Time.time - pingSentTime);
        if (pingResponseQueue.Count > pingsToAverage)
            pingResponseQueue.Dequeue();

        CalculateAveragePing();
        //Debug.Log("ping response recieved for " + gameObject.name + " pingSentTime: " + pingSentTime + " curernt Time: " + Time.time + "new ping: " + playerPing);
    }

}
