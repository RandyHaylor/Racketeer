using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetworkRigidbodyStreamer : NetworkBehaviour
{
    public bool debugWithThisObject;
    string debuggingTargetGameObjName = "Sphere";
    //used to provide a constant stream of data for a critical game object
    //  intended for a server-side physics sim that streams positions/physics info to all clients every fixed update
    [Header("Authority")]
    [Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
    public bool clientAuthority;

    public BufferType bufferType = 0;

    [Header("FrameBuffer")]
    [Tooltip("The number of frames to keep in the local FixedUpdate queue")]
    public byte numberOfFramesToKeepInBuffer = 2;
    public byte bufferLimit = 5;
    bool shiftingBufferCountBackToTarget = false;

    [Header("NetworkTimeBuffer")]
    [Tooltip("Uses synced network time to wait for processing")]
    public byte networkTimeFramesToBuffer = 2;
    bool factorPlayerPingInBufferTime; //not implemented yet
    public byte maxFrameBufferBeforeReset = 5;    
    
    //time syncing Authority > Observer
    bool recievedFirstUpdate = false;
    double localTimeOfLastAppliedUpdate;
    double bufferedTimeOffsetOwnerObserver;
    Queue<double> bufferedTimeOffsetHistory;


    [Header("Syncing")]
    [Tooltip("Data is sent whether checked or not (for live testing)")]
    public Rigidbody rigidBody;
    public bool syncRBVelocity;
    public bool syncRBAngularVelocity;
    public bool syncRBPosition;
    public bool syncRBRotation;


    bool updateRun; //not currently used, but identifies fixedUpdates that did not get a physics update from obj authority.  was for an abandoned interpolation script that isn't needed because local physics engine handles that...


    public enum BufferType
    {
        None = 0,
        FrameCountBuffer = 1,
        NetworkTime = 2
    }

    LinkedList<UpdateRibidbodyItem> updateBuffer;
    LinkedList<UpdateRibidbodyItem> updateHistory;
    double lastUpdateNetworktime;


    internal class UpdateRibidbodyItem
    {
        public Vector3 rigidBodyVelocity;
        public Vector3 rigidBodyAngularVelocity;
        public Vector3 rigidBodyPosition;
        public Quaternion rigidBodyRotation;
        public double networkTime;
        public double localRecievedTime;
    }

    // Start is called before the first frame update
    void Start()
    {
        lastUpdateNetworktime = 0;
        updateBuffer = new LinkedList<UpdateRibidbodyItem>();
        updateHistory = new LinkedList<UpdateRibidbodyItem>();
        debuggingTargetGameObjName = this.gameObject.name;
        bufferedTimeOffsetHistory = new Queue<double>();
    }



    // Update is called once per frame
    private void Update()
    {
        //if (gameObject.name == debuggingTargetGameObjName) Debug.Log("Update() check: UpdateRunLastFrame? " + updateRun + " Server? " + isServer);
    }
    void FixedUpdate()
    {
        if (updateBuffer == null) updateBuffer = new LinkedList<UpdateRibidbodyItem>();
        if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("UPDATEBUFFER.COUNT=============: " + updateBuffer.Count + " Time.time: " + Time.time);
        //recipient state
        if ((isServer && clientAuthority)||(!isServer && !clientAuthority))
        {
            if (bufferType != BufferType.None) 
                ApplyBufferedUpdates();
            else if (!updateRun) 
            { 
                if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("updateRun false and no buffer used in fixed update for recipient.");
            }

            if (localTimeOfLastAppliedUpdate < Time.time + networkTimeFramesToBuffer*Time.fixedDeltaTime - 1) //if we're more than a full second behind sync, reset
            {
                updateBuffer.Clear();
                recievedFirstUpdate = false;
                if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("OVER 1 SECOND WITHOUT UPDATE, CLEARING BUFFER AND RESETTING...");
            }
        }
        else //owner state
        {
            if (isServer) RpcUpdateRigidbodyOnClients(rigidBody.velocity, rigidBody.angularVelocity, rigidBody.position, rigidBody.rotation, NetworkTime.time);
            else CmdUpdateRigidbodyOnServer(rigidBody.velocity, rigidBody.angularVelocity, rigidBody.position, rigidBody.rotation, NetworkTime.time);
        }
    }

    [Command(channel = Channels.Unreliable)]
    void CmdUpdateRigidbodyOnServer(Vector3 rigidBodyVelocity, Vector3 rigidBodyAngularVelocity, Vector3 rigidBodyPosition, Quaternion rigidBodyRotation, double networkTime)
    {   
        //UNTESTED... needs to be tested with clientAuthority on
        if (NetworkServer.connections.Count > 0 && !isClient) //if a client is sending to a dedicated server, that should update it's own buffer
        {
            //Debug.Log("I think im' a dedicated server");
            if (networkTime < lastUpdateNetworktime) 
            { 
                //Debug.Log("Ignoring an out of order NetworkTime.time update"); 
                return; 
            } //don't apply out of order updates - ignore them
            lastUpdateNetworktime = networkTime;


            if (bufferType != BufferType.None)
                AddItemToUpdateBuffer(rigidBodyVelocity, rigidBodyAngularVelocity, rigidBodyPosition, rigidBodyRotation, networkTime);
            else
                ApplyRigidbodyUpdates(rigidBodyVelocity, rigidBodyAngularVelocity, rigidBodyPosition, rigidBodyRotation, networkTime);
        }
        RpcUpdateRigidbodyOnClients(rigidBodyVelocity, rigidBodyAngularVelocity, rigidBodyPosition, rigidBodyRotation, networkTime); //send updates to all clients
    }

    [ClientRpc(channel = Channels.Unreliable)]
    void RpcUpdateRigidbodyOnClients(Vector3 rigidBodyVelocity, Vector3 rigidBodyAngularVelocity, Vector3 rigidBodyPosition, Quaternion rigidBodyRotation, double networkTime)
    {
        if (NetworkServer.connections.Count > 0)
        {
            //Debug.Log("I'm a host+client and shouldn't update as client");
            return;
        }

        if (networkTime < lastUpdateNetworktime)
        { 
            if(debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("Ignoring an out of order NetworkTime.time update"); 
            return;  //don't apply out of order updates - ignore them
        }
        
        lastUpdateNetworktime = networkTime;


        if (bufferType != BufferType.None)
            AddItemToUpdateBuffer(rigidBodyVelocity, rigidBodyAngularVelocity, rigidBodyPosition, rigidBodyRotation, networkTime);
        else
        {
            AddItemToHistoryBuffer(rigidBodyVelocity, rigidBodyAngularVelocity, rigidBodyPosition, rigidBodyRotation, networkTime);
            ApplyRigidbodyUpdates(rigidBodyVelocity, rigidBodyAngularVelocity, rigidBodyPosition, rigidBodyRotation, networkTime);
        }


        //Debug.Log("I think I'm a client that's not the owner or a host");
    }

    void AddItemToHistoryBuffer(Vector3 rigidBodyVelocity, Vector3 rigidBodyAngularVelocity, Vector3 rigidBodyPosition, Quaternion rigidBodyRotation, double networkTime)
    {
        UpdateRibidbodyItem RBupdate = new UpdateRibidbodyItem();
        RBupdate.rigidBodyVelocity = rigidBodyVelocity;
        RBupdate.rigidBodyAngularVelocity = rigidBodyAngularVelocity;
        RBupdate.rigidBodyPosition = rigidBodyPosition;
        RBupdate.rigidBodyRotation = rigidBodyRotation;
        RBupdate.networkTime = networkTime;

        AddToUpdateHistory(RBupdate);
    }

    void AddItemToUpdateBuffer(Vector3 rigidBodyVelocity, Vector3 rigidBodyAngularVelocity, Vector3 rigidBodyPosition, Quaternion rigidBodyRotation, double networkTime)
    {
        if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("==========ADDING UPDATE TO BUFFER============= BUFFERCOUNT:" + updateBuffer.Count+1  + " Time.time: " + Time.time);
        UpdateRibidbodyItem RBupdate = new UpdateRibidbodyItem();
        RBupdate.rigidBodyVelocity = rigidBodyVelocity;
        RBupdate.rigidBodyAngularVelocity = rigidBodyAngularVelocity;
        RBupdate.rigidBodyPosition = rigidBodyPosition;
        RBupdate.rigidBodyRotation = rigidBodyRotation;
        RBupdate.networkTime = networkTime;
        RBupdate.localRecievedTime = Time.timeAsDouble;
        if (updateBuffer == null) updateBuffer = new LinkedList<UpdateRibidbodyItem>();
        updateBuffer.AddLast(RBupdate);
    }

    void ApplyBufferedUpdates() //called by FixedUpate
    {
        if (bufferType == BufferType.FrameCountBuffer)
        {
            if (updateBuffer.Count <= numberOfFramesToKeepInBuffer || updateBuffer.Count < 2)
                shiftingBufferCountBackToTarget = false;

            if (updateBuffer.Count < numberOfFramesToKeepInBuffer || updateBuffer.Count == 0)
            {
                if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("UpdateBuffer count was 0 or less than currentBufferFrames: " + updateBuffer.Count);
                return;
            }

            if (updateBuffer.Count > bufferLimit && !shiftingBufferCountBackToTarget) //if we detect buffer is over limit, reduce buffer by 1 each frame until ideal buffer size reached
            {
                if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("shifting buffer count back to target set to true. bufferCount: " + updateBuffer.Count + " bufferLimit: " + bufferLimit);
                shiftingBufferCountBackToTarget = true;
            }

            if (shiftingBufferCountBackToTarget)
            {
                if (updateBuffer.Count > 1)
                {
                    updateBuffer.RemoveFirst(); //causing this loop to skip a frame/update
                    if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("FrameCountBuffer: Removed update from buffer, new updateBuffer.Count: " + updateBuffer.Count);
                }
                if (updateBuffer.Count <= numberOfFramesToKeepInBuffer || updateBuffer.Count < 2)
                {
                    if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("FrameCountBuffer: last update removal caused buffercount to reach target");
                    shiftingBufferCountBackToTarget = false;
                }
            }
        }
        else if (bufferType == BufferType.NetworkTime) //Networktime.time is currently NOT syncing, so this is broken
        {
            if (updateBuffer == null) updateBuffer = new LinkedList<UpdateRibidbodyItem>();
            if (updateBuffer.Count == 0)
            {
                if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("networkTime buffer: updateBuffer.Count == 0" + " Time.time: " + Time.time);
                return;
            }

            if (!recievedFirstUpdate)
            {
                bufferedTimeOffsetHistory.Clear();
                localTimeOfLastAppliedUpdate = Time.timeAsDouble + (double)(networkTimeFramesToBuffer * Time.fixedDeltaTime);
                recievedFirstUpdate = true;
                if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("networkTime buffer: reset time sync");
            }

            UpdateTimeOffsetAverage(updateBuffer.Last.Value.networkTime, updateBuffer.Last.Value.localRecievedTime, networkTimeFramesToBuffer * Time.fixedDeltaTime); //after a reset, averages 30 updates of times and assigns average time delta from obj owner to observer to bufferedTimeOffsetOwnerObserver

            if (updateBuffer.First.Value.networkTime > Time.timeAsDouble + bufferedTimeOffsetOwnerObserver + (double)(0.5 *Time.fixedDeltaTime)) 
            {
                if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("networkTime buffer: too soon, waiting until next frame to apply" + " Time.time: " + Time.time);
                return;
            }

            while (updateBuffer.Count > 0 && updateBuffer.First.Value.networkTime < Time.timeAsDouble + bufferedTimeOffsetOwnerObserver - (double)(5.5*Time.fixedDeltaTime)) //discard updates more than two frames behind
            {
                if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("networkTime buffer: too late... dropping frame upate" + " Time.time: " + Time.time);
                updateBuffer.RemoveFirst();
                if (updateBuffer.Count == 0)
                {
                    if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("buffer cleared, ALL UPDATES WERE TOO OLD");
                }
                    
            }

            if (updateBuffer.Count > maxFrameBufferBeforeReset)
            {
                if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("max frame buffer exceeded, clearing buffer and resetting" + " Time.time: " + Time.time);
                updateBuffer.Clear();
                recievedFirstUpdate = false; //the next update will reset timing
                return;
            }
            //if neither previous case applies, continue to apply the buffered update

        }
        else
            Debug.Log("ERROR THIS SHOULDN'T BE RUNNING...  Review ApplyBufferedUpdates() in NetworkRigidbodyStreamer.cs");


        if (updateBuffer.Count > 0)
        {
            if (debugWithThisObject && gameObject.name == debuggingTargetGameObjName) Debug.Log("applying update" + " Time.time: " + Time.time);
            ApplyRigidbodyUpdates(updateBuffer.First.Value.rigidBodyVelocity, updateBuffer.First.Value.rigidBodyAngularVelocity, updateBuffer.First.Value.rigidBodyPosition, updateBuffer.First.Value.rigidBodyRotation, updateBuffer.First.Value.networkTime);
            AddToUpdateHistory(updateBuffer.First.Value);
            updateBuffer.RemoveFirst();
            updateRun = true;
        }
        else
            updateRun = false;
    }

    void UpdateTimeOffsetAverage(double authTime, double localReceivedTime, float bufferTime)
    {
        if (bufferedTimeOffsetHistory == null) bufferedTimeOffsetHistory = new Queue<double>();

        if (bufferedTimeOffsetHistory.Count < 30) bufferedTimeOffsetHistory.Enqueue(authTime - localReceivedTime - (double)bufferTime);
        
        bufferedTimeOffsetOwnerObserver = bufferedTimeOffsetHistory.Sum() / bufferedTimeOffsetHistory.Count;
    }

    void AddToUpdateHistory(UpdateRibidbodyItem newUpdate)
    {
        if (updateHistory == null) updateHistory = new LinkedList<UpdateRibidbodyItem>();
        updateHistory.AddFirst(newUpdate);
        while (updateHistory.Count > 2) updateHistory.RemoveLast();
    }

    void ApplyRigidbodyUpdates(Vector3 rigidBodyVelocity, Vector3 rigidBodyAngularVelocity, Vector3 rigidBodyPosition, Quaternion rigidBodyRotation, double networkTime)
    {
        localTimeOfLastAppliedUpdate = Time.timeAsDouble; 
        updateRun = true;
        //if (debugByGameObjectName && gameObject.name == debuggingTargetGameObjName) Debug.Log("just set updateRun to true for Sphere");
        if (syncRBVelocity) rigidBody.velocity = rigidBodyVelocity;
        if (syncRBAngularVelocity) rigidBody.angularVelocity = rigidBodyAngularVelocity;
        if (syncRBPosition) rigidBody.position = rigidBodyPosition;
        if (syncRBRotation) rigidBody.rotation = rigidBodyRotation;
    }
}

