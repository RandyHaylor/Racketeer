using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Linq;
using Chronos;

public class NetworkRigidbodyController : NetworkBehaviour
{
    private static NetworkRigidbodyController _instance;
    public static NetworkRigidbodyController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NetworkRigidbodyController>();
            }

            if (_instance == null)
                Debug.Log("Can't find NetworkRigidbodyController in scene...");

            return _instance;
        }
    }

    [HideInInspector]
    public static bool IsResimulating {   get { return _instance.isResimulating; }  }
    private bool isResimulating = false;

    public class NetworkRigidbodyPlayer 
    {
        public bool IsLocalPlayer;
        public Player PlayerComponent;
        public Rigidbody RigidbodyComponent;
        public PlayerRigidbodyUpdate PlayerRigidbodyUpdate;
        
        public NetworkRigidbodyPlayer(uint playerNetId, Player playerController, Rigidbody playerRigidbody, bool isLocalPlayer)
        {
            IsLocalPlayer = isLocalPlayer;
            PlayerComponent = playerController;
            RigidbodyComponent = playerRigidbody;
            PlayerRigidbodyUpdate = new PlayerRigidbodyUpdate();
            PlayerRigidbodyUpdate.PlayerNetId = playerNetId;
        }
    }

    [HideInInspector]
    public NetworkRigidbodyPlayer[] NetworkRigidbodyPlayers;
    [HideInInspector]
    public NetworkRigidbodyObject SphereRbObject;

    [Serializable]
    public class PlayerRigidbodyUpdate
    {
        public uint PlayerNetId;
        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;
        public Vector3 PlayerVelocity;
        public Vector3 PlayerAngularVelocity;
        public Vector3 LastAppliedForce;
        public Vector3 LastAppliedAngularForce;
        public PlayerRigidbodyUpdate()
        {
            PlayerNetId = 999999999;
        }
    }
    public class NetworkRigidbodyObject
    {
        public Rigidbody RigidbodyComponent;
        public ObjectRigidbodyUpdate ObjectRigidbodyUpdate;
        public NetworkRigidbodyObject(uint objectNetId, Rigidbody objectRigidbody)
        {
            RigidbodyComponent = objectRigidbody;
            ObjectRigidbodyUpdate = new ObjectRigidbodyUpdate();
            ObjectRigidbodyUpdate.ObjectNetId = objectNetId;
        }
    }

    [Serializable]
    public class ObjectRigidbodyUpdate
    {
        public uint ObjectNetId;
        public Vector3 ObjectPosition;
        public Quaternion ObjectRotation;
        public Vector3 ObjectVelocity;
        public Vector3 ObjectAngularVelocity;
        public ObjectRigidbodyUpdate()
        {
            ObjectNetId = 999999999;
            ObjectPosition = Vector3.zero;
            ObjectRotation = Quaternion.identity;
            ObjectVelocity = Vector3.zero;
            ObjectAngularVelocity = Vector3.zero;
        }
    }


    //server uses these to cache & send updates, client uses them to receive & read updates
    PlayerRigidbodyUpdate[] PlayerRigidbodyUpdates;
    ObjectRigidbodyUpdate RigidbodyUpdateSphere;


    bool incomingUpdatesAlreadyProcessed;
    double timeIncomingUpdatesSent;
    float lastFrameFraction;
    float timeToLookForward;

    int physicsFramesToPredict;

    Player currentLocalPlayer;

    private void Awake()
    {
        _instance = this;
        //references/info for local objects
        SphereRbObject = new NetworkRigidbodyObject(999999999, null);
        NetworkRigidbodyPlayers = new NetworkRigidbodyPlayer[4];
        for (int i = 0; i < 4; i++)
        {
            NetworkRigidbodyPlayers[i] = new NetworkRigidbodyPlayer(999999999, null, null, false);
        }

        //caches for collecting(server) or receiving(client) rigidbody info
        PlayerRigidbodyUpdates = new PlayerRigidbodyUpdate[4];
        for (int i = 0; i < 4; i++)
        {
            PlayerRigidbodyUpdates[i] = new PlayerRigidbodyUpdate();
        }
        RigidbodyUpdateSphere = new ObjectRigidbodyUpdate();        



        Physics.autoSimulation = false;
    }
    void Start()
    {

    }


    void FixedUpdate()
    {
        //Debug.Log("FixedUpdate in NetworkRbcont. Time.time: " + Time.time + " playerController == " + currentLocalPlayer);
        //--- ALL BUT DEDICATED SERVER - Handle input for localPlayer ---
        if (currentLocalPlayer != null)
        {
            //Debug.Log("Attempting to call HandleInput for localPlayer");
            currentLocalPlayer.HandleInput();
        }

        //---SERVER (CLIENT+HOST OR DEDICATED) ---
        //send rpc udpates of registered players and objects with a timestamp to clients - the rpc loads the input into a buffer that just keeps the last ordered update
        if (isServer)
        {
            Physics.Simulate(Time.fixedDeltaTime);
            GetSphereState();
            GetPlayerState();
            RpcSendRigidbodyUpdates(PlayerRigidbodyUpdates[0], PlayerRigidbodyUpdates[1], PlayerRigidbodyUpdates[2], PlayerRigidbodyUpdates[3], RigidbodyUpdateSphere, NetworkTime.time);
            //Debug.Log("Ran RpcSendRigidbodyUpdates at net time: " + NetworkTime.time);
            return;
        }

        //---NON-SERVER CLIENT-----        
        if (TimeController.Instance.IsRewinding) //needed for rewind-time effect. TODO: Decouple this (generic event that requires inputs to be ingored until another event?)
        {
            Physics.Simulate(Time.fixedDeltaTime); //don't resimulate anything or apply forces during rewind, just let the frame run as normal.
        }
        if (incomingUpdatesAlreadyProcessed) //no new update from server, apply last input for current frame & run as if local
        {
            ApplyLatestForces(); //does not run Physics.Simulate - applies latest input buffer and continues to predict
            Physics.Simulate(Time.fixedDeltaTime);
        }
        else //there's a new update from the server, reset object positions to that update, then roll time forward through physics frames & prediction
        {
            isResimulating = true;
            Timekeeper.instance.Clock("Root").paused = true;
            ResetPositionsAndRotations();
            PredictFromUpdatedServerState(); //runs Physics.Simulate 1 or more times, isResimulating is false for the last simulation            
        }
    }

    private void ResetPositionsAndRotations()
    {
        //set locations & velocities of all player objects
        if (SphereRbObject.RigidbodyComponent != null && NetworkIdentity.spawned.ContainsKey(SphereRbObject.ObjectRigidbodyUpdate.ObjectNetId))
        {
            SphereRbObject.RigidbodyComponent.position = RigidbodyUpdateSphere.ObjectPosition;
            SphereRbObject.RigidbodyComponent.rotation = RigidbodyUpdateSphere.ObjectRotation;
            SphereRbObject.RigidbodyComponent.velocity = RigidbodyUpdateSphere.ObjectVelocity;
            SphereRbObject.RigidbodyComponent.angularVelocity = RigidbodyUpdateSphere.ObjectAngularVelocity;
        }

        for (int i = 0; i < NetworkRigidbodyPlayers.Length; i++)
        {
            if (NetworkRigidbodyPlayers[i].RigidbodyComponent != null && NetworkIdentity.spawned.ContainsKey(NetworkRigidbodyPlayers[i].PlayerRigidbodyUpdate.PlayerNetId))
            {
                NetworkRigidbodyPlayers[i].RigidbodyComponent.position = PlayerRigidbodyUpdates[i].PlayerPosition;
                NetworkRigidbodyPlayers[i].RigidbodyComponent.rotation = PlayerRigidbodyUpdates[i].PlayerRotation;
                NetworkRigidbodyPlayers[i].RigidbodyComponent.velocity = PlayerRigidbodyUpdates[i].PlayerVelocity;
                NetworkRigidbodyPlayers[i].RigidbodyComponent.angularVelocity = PlayerRigidbodyUpdates[i].PlayerAngularVelocity;
            }
        }
        incomingUpdatesAlreadyProcessed = true;
    }
    private void PredictFromUpdatedServerState()
    {
        //from current estimated network time back to timestamp plus the 1/2 rtt lookahead time gives us perfect alignment for 0 latency
        //TODO set a max threshold for look-ahead time
        timeToLookForward = (float)(  (NetworkTime.time - timeIncomingUpdatesSent) + (NetworkTime.rtt / (float)2));
        timeToLookForward = Mathf.Clamp(timeToLookForward, 0, 2f); //input buffer doesn't support looking back more than 2 seconds (also, that's crazy, guessing 0.5s is max playable simulate time)
        //determine how many frames back we're going
        physicsFramesToPredict = (int)(timeToLookForward / Time.fixedDeltaTime);
        lastFrameFraction = ((timeToLookForward / Time.fixedDeltaTime) - (float)physicsFramesToPredict);
        //Debug.Log("timeToLookForward: " + timeToLookForward + " physicsFramesToPredict: " + physicsFramesToPredict + " lastFrameFraction: " + lastFrameFraction);

        for (int j = physicsFramesToPredict; j >= 0 ; j--)
        {
            //apply lastForce or buffered player input back correct number of frames, raising to consume first input in buffer
            for (int i = 0; i < NetworkRigidbodyPlayers.Length; i++)
            {
                if (NetworkRigidbodyPlayers[i].RigidbodyComponent != null && NetworkIdentity.spawned.ContainsKey(NetworkRigidbodyPlayers[i].PlayerRigidbodyUpdate.PlayerNetId))
                {
                    if (NetworkRigidbodyPlayers[i].IsLocalPlayer)
                    {
                        NetworkRigidbodyPlayers[i].RigidbodyComponent
                            .AddForce(NetworkRigidbodyPlayers[i].PlayerComponent.playerInputForceBuffer.Skip(j).First().movement * (j == 1 ? lastFrameFraction : 1));
                        NetworkRigidbodyPlayers[i].RigidbodyComponent
                            .AddTorque(NetworkRigidbodyPlayers[i].PlayerComponent.playerInputForceBuffer.Skip(j).First().spin * (j == 1 ? lastFrameFraction : 1));
                    }
                    else
                    {
                        NetworkRigidbodyPlayers[i].RigidbodyComponent.AddForce(NetworkRigidbodyPlayers[i].PlayerComponent.LastAppliedPlayerForce.movement * (j == 1 ? lastFrameFraction : 1), ForceMode.Impulse);
                        NetworkRigidbodyPlayers[i].RigidbodyComponent.AddTorque(NetworkRigidbodyPlayers[i].PlayerComponent.LastAppliedPlayerForce.spin * (j == 1 ? lastFrameFraction : 1), ForceMode.Impulse);
                    }
                }
            }
            if (j == 0)
            {
                isResimulating = false; //last full frame is a 'new frame' for any non-predictive physics items
                Timekeeper.instance.Clock("Root").paused = false;
            }
            Physics.Simulate(Time.fixedDeltaTime * (j == 1 ? lastFrameFraction : 1));
        }
    }

    private void ApplyLatestForces()
    {
        for (int i = 0; i < NetworkRigidbodyPlayers.Length; i++)
        {
            if (NetworkRigidbodyPlayers[i].RigidbodyComponent != null && NetworkIdentity.spawned.ContainsKey(NetworkRigidbodyPlayers[i].PlayerRigidbodyUpdate.PlayerNetId))
            {
                if (NetworkRigidbodyPlayers[i].IsLocalPlayer)
                {
                    NetworkRigidbodyPlayers[i].RigidbodyComponent
                        .AddForce(NetworkRigidbodyPlayers[i].PlayerComponent.playerInputForceBuffer.First().movement);
                    NetworkRigidbodyPlayers[i].RigidbodyComponent
                        .AddTorque(NetworkRigidbodyPlayers[i].PlayerComponent.playerInputForceBuffer.First().spin);
                }
                else
                {
                    NetworkRigidbodyPlayers[i].RigidbodyComponent.AddForce(NetworkRigidbodyPlayers[i].PlayerComponent.LastAppliedPlayerForce.movement, ForceMode.Impulse);
                    NetworkRigidbodyPlayers[i].RigidbodyComponent.AddTorque(NetworkRigidbodyPlayers[i].PlayerComponent.LastAppliedPlayerForce.spin, ForceMode.Impulse);
                }
            }
        }
    }


    private void GetPlayerState()
    {
        for (int i = 0; i < PlayerRigidbodyUpdates.Length; i++)
        {
            if (NetworkRigidbodyPlayers[i].RigidbodyComponent != null && NetworkIdentity.spawned.ContainsKey(NetworkRigidbodyPlayers[i].PlayerRigidbodyUpdate.PlayerNetId))
            {
                PlayerRigidbodyUpdates[i].PlayerPosition = NetworkRigidbodyPlayers[i].RigidbodyComponent.position;
                PlayerRigidbodyUpdates[i].PlayerRotation = NetworkRigidbodyPlayers[i].RigidbodyComponent.rotation;
                PlayerRigidbodyUpdates[i].PlayerVelocity = NetworkRigidbodyPlayers[i].RigidbodyComponent.velocity;
                PlayerRigidbodyUpdates[i].PlayerAngularVelocity = NetworkRigidbodyPlayers[i].RigidbodyComponent.angularVelocity;
                PlayerRigidbodyUpdates[i].LastAppliedForce = NetworkRigidbodyPlayers[i].PlayerComponent.LastAppliedPlayerForce.movement;
                PlayerRigidbodyUpdates[i].LastAppliedAngularForce = NetworkRigidbodyPlayers[i].PlayerComponent.LastAppliedPlayerForce.spin;
            }
        }
    }
    private void GetSphereState()
    {
        if (SphereRbObject.RigidbodyComponent != null && NetworkIdentity.spawned.ContainsKey(SphereRbObject.ObjectRigidbodyUpdate.ObjectNetId))
        RigidbodyUpdateSphere.ObjectPosition = SphereRbObject.RigidbodyComponent.position;
        RigidbodyUpdateSphere.ObjectRotation = SphereRbObject.RigidbodyComponent.rotation;
        RigidbodyUpdateSphere.ObjectVelocity = SphereRbObject.RigidbodyComponent.velocity;
        RigidbodyUpdateSphere.ObjectAngularVelocity = SphereRbObject.RigidbodyComponent.angularVelocity;
    }


    [ClientRpc]
    private void RpcSendRigidbodyUpdates
        (PlayerRigidbodyUpdate playerOneRbUpdate, PlayerRigidbodyUpdate playerTwoRbUpdate, PlayerRigidbodyUpdate playerThreeRbUpdate
        , PlayerRigidbodyUpdate playerFourRbUpdate, ObjectRigidbodyUpdate sphereRbUpdate, double timeUpdateSent)
    {
        if (isServer) return; //client host doesn't need this

        //ignore out of order packet - rather predict from current state without new input than go back in time.
        if (timeUpdateSent < timeIncomingUpdatesSent)  
        {
            Debug.Log("Ignoring an update that was sent out of order, current update is newer. TimeSent: " + timeUpdateSent + " timeSentForCurrentlyBufferedUpdate: " + timeIncomingUpdatesSent);
            return;
        }


        PlayerRigidbodyUpdates[0] = playerOneRbUpdate;
        PlayerRigidbodyUpdates[1] = playerTwoRbUpdate;
        PlayerRigidbodyUpdates[2] = playerThreeRbUpdate;
        PlayerRigidbodyUpdates[3] = playerFourRbUpdate;
        RigidbodyUpdateSphere = sphereRbUpdate;
        /*
        if (!incomingUpdatesAlreadyProcessed) Debug.Log("overwriting previously unread update at NetworkTime.time: " + NetworkTime.time + " NetworkTimeSent: " + timeUpdateSent);
        else Debug.Log("New Update Received at NetworkTime.time: " + NetworkTime.time + " NetworkTimeSent: " + timeUpdateSent);
        */
        incomingUpdatesAlreadyProcessed = false;
        timeIncomingUpdatesSent = timeUpdateSent;
    }

    public void RegisterPlayer(uint playerNetId, Player playerController, Rigidbody playerRigidbody, bool isLocalPlayer, int playerNumber)
    {
        if (!NetworkIdentity.spawned.ContainsKey(playerNetId)) 
        {   
            Debug.Log("playerNetId: " + playerNetId + " not found in spawned network objects, registration with NetworkRigidbodyController failed.");
            return;
        }

        NetworkRigidbodyPlayers[playerNumber] = new NetworkRigidbodyPlayer(playerNetId, playerController, playerRigidbody, isLocalPlayer);

        if (isLocalPlayer) 
        {
            Debug.Log("Assigning local playerController in NetworkRigidbodyController");
            currentLocalPlayer = playerController; 
        }
        Debug.Log("Player Number " + playerNumber + " Registered with NetworkRigidbodyController: " + playerNetId + " " + playerController.gameObject.name + " isLocalPlayer = " + isLocalPlayer);
    }

    public void RegisterSphere(uint objectNetId, Rigidbody objectRigidbody)
    {
        if (objectRigidbody == null)
        {
            Debug.Log("RegisterObject failed for netId: " + objectNetId + " referenced objectRigidbody was null");
            return;
        }
        if (!NetworkIdentity.spawned.ContainsKey(objectNetId))
        {
            Debug.Log("objectNetId: " + objectNetId + " not found in spawned network objects, registration with NetworkRigidbodyController failed.");
            return;
        }
        
        SphereRbObject =  new NetworkRigidbodyObject(objectNetId, objectRigidbody);

        Debug.Log("Object Registered with NetworkRigidbodyController   object:" + objectRigidbody.gameObject.name + "   netId: " + objectNetId);
    }

}
