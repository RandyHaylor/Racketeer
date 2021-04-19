using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class NetworkRigidbodyStreamer : NetworkBehaviour
{
    //used to provide a constant stream of data for a critical game object
    //  intended for a server-side physics sim that streams positions/physics info to all clients every fixed update
    [Header("Authority")]
    [Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
    public bool clientAuthority;

    public BufferType bufferType = 0;

    public bool interpolateNextFixedUpdate = false;

    [Header("FrameBuffer")]
    [Tooltip("The number of frames to keep in the local FixedUpdate queue")]
    public byte numberOfFramesToKeepInBuffer = 2;
    public byte bufferLimit = 5;
    bool shiftingBufferCountBackToTarget = false;

    [Header("NetworkTimeBuffer")]
    [Tooltip("Uses synced network time to wait for processing")]
    public double bufferTimeSeconds = 0.05f;

    [Header("Syncing")]
    [Tooltip("Data is sent whether checked or not (for live testing)")]
    public Rigidbody rigidBody;
    public bool syncRBVelocity;
    public bool syncRBAngularVelocity;
    public bool syncRBPosition;
    public bool syncRBRotation;

    //time syncing server => client
    bool recievedFirstUpdate = false;
    double ownerToObserverTimeDifference = 0;
    double localTimeOfLastAppliedUpdate;
    double networkTimeOfLastAppliedUpdate;
    double cachedTargetNextUpdateTime;

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
    }

    // Start is called before the first frame update
    void Start()
    {
        lastUpdateNetworktime = 0;
        updateBuffer = new LinkedList<UpdateRibidbodyItem>();
        updateHistory = new LinkedList<UpdateRibidbodyItem>();
    }
    bool updateRun;
    // Update is called once per frame
    private void Update()
    {
        //if (gameObject.name == "Sphere") Debug.Log("Update() check: UpdateRunLastFrame? " + updateRun + " Server? " + isServer);
    }
    void FixedUpdate()
    {
        //if (gameObject.name == "Sphere") Debug.Log("FixedUpdate() check: UpdateRunLastFrame? " + updateRun + " Server? " + isServer);
        if (gameObject.name == "Sphere") Debug.Log("updateBuffer.Count: " + updateBuffer.Count);
        //recipient state
        if ((isServer && clientAuthority)||(!isServer && !clientAuthority))
        {
            if (bufferType != BufferType.None) 
                ApplyBufferedUpdates();
            else if (!updateRun) 
            { 
                Debug.Log("updateRun false and no buffer used in fixed update for recipient. Running InterpolateNextState()");
                if (interpolateNextFixedUpdate) InterpolateNextState(); 
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
            Debug.Log("I think im' a dedicated server");
            if (networkTime < lastUpdateNetworktime) { Debug.Log("Ignoring an out of order NetworkTime.time update"); return; } //don't apply out of order updates - ignore them
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

        if (networkTime < lastUpdateNetworktime){ Debug.Log("Ignoring an out of order NetworkTime.time update"); return; } //don't apply out of order updates - ignore them
        
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
        UpdateRibidbodyItem RBupdate = new UpdateRibidbodyItem();
        RBupdate.rigidBodyVelocity = rigidBodyVelocity;
        RBupdate.rigidBodyAngularVelocity = rigidBodyAngularVelocity;
        RBupdate.rigidBodyPosition = rigidBodyPosition;
        RBupdate.rigidBodyRotation = rigidBodyRotation;
        RBupdate.networkTime = networkTime;
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
                if (gameObject.name == "Sphere") Debug.Log("UpdateBuffer count was 0 or less than currentBufferFrames: " + updateBuffer.Count);
                if (interpolateNextFixedUpdate) InterpolateNextState(); //uses previous velocity to calculate a new position
                return;
            }

            if (updateBuffer.Count > bufferLimit && !shiftingBufferCountBackToTarget) //if we detect buffer is over limit, reduce buffer by 1 each frame until ideal buffer size reached
            {
                if (gameObject.name == "Sphere") Debug.Log("shifting buffer count back to target set to true. bufferCount: " + updateBuffer.Count + " bufferLimit: " + bufferLimit);
                shiftingBufferCountBackToTarget = true;
            }

            if (shiftingBufferCountBackToTarget)
            {
                if (updateBuffer.Count > 1)
                {
                    updateBuffer.RemoveFirst(); //causing this loop to skip a frame/update
                    if (gameObject.name == "Sphere") Debug.Log("FrameCountBuffer: Removed update from buffer, new updateBuffer.Count: " + updateBuffer.Count);
                }
                if (updateBuffer.Count <= numberOfFramesToKeepInBuffer || updateBuffer.Count <2)
                {
                    if (gameObject.name == "Sphere") Debug.Log("FrameCountBuffer: last update removal caused buffercount to reach target");
                    shiftingBufferCountBackToTarget = false;
                }
            }
        }
        else if (bufferType == BufferType.NetworkTime) //Networktime.time is currently NOT syncing, so this is broken
        {
            if (updateBuffer.Count == 0)
            {
                if (updateBuffer.Count == 0)
                {
                    if (interpolateNextFixedUpdate) InterpolateNextState(); //uses previous velocity to calculate a new position
                    if (gameObject.name == "Sphere") Debug.Log("networkTime buffer: updateBuffer.Count == 0");
                }

                return;
            }

            if (!recievedFirstUpdate) 
            {
                networkTimeOfLastAppliedUpdate = updateBuffer.First.Value.networkTime;
                localTimeOfLastAppliedUpdate = Time.timeAsDouble + bufferTimeSeconds; //adding a frame as buffer   // + (double)Time.fixedDeltaTime
                recievedFirstUpdate = true;
                if (gameObject.name == "Sphere") Debug.Log("networkTime buffer: reset times (Time.fixedDeltaTime: " +Time.fixedDeltaTime);
            }

            cachedTargetNextUpdateTime = networkTimeOfLastAppliedUpdate + Time.timeAsDouble - localTimeOfLastAppliedUpdate;


            if (updateBuffer.First.Value.networkTime > cachedTargetNextUpdateTime + (double)Time.fixedDeltaTime) //too soon
            {
                if (gameObject.name == "Sphere") Debug.Log("networkTime buffer: too soon, waiting until next frame to apply");
                return;
            }
            while (updateBuffer.Count > 0 && updateBuffer.First.Value.networkTime < cachedTargetNextUpdateTime - (double)Time.fixedDeltaTime) //discard late updates until last update
            {
                if (gameObject.name == "Sphere") Debug.Log("networkTime buffer: too late... dropping frame upate");
                updateBuffer.RemoveFirst();
                if (updateBuffer.Count == 0)
                {
                    recievedFirstUpdate = false; //the next update will reset timing
                    if (gameObject.name == "Sphere") Debug.Log("buffer cleared, next update will reset timing");
                }
                    
            }
            //if neither previous case applies, continue to apply the buffered update

        }
        else
            Debug.Log("ERROR THIS SHOULDN'T BE RUNNING...");


        if (updateBuffer.Count > 0)
        {
            ApplyRigidbodyUpdates(updateBuffer.First.Value.rigidBodyVelocity, updateBuffer.First.Value.rigidBodyAngularVelocity, updateBuffer.First.Value.rigidBodyPosition, updateBuffer.First.Value.rigidBodyRotation, updateBuffer.First.Value.networkTime);
            AddToUpdateHistory(updateBuffer.First.Value);
            updateBuffer.RemoveFirst();
            updateRun = true;
        }
        else
            updateRun = false;
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
        networkTimeOfLastAppliedUpdate = networkTime;
        updateRun = true;
        //if (gameObject.name == "Sphere") Debug.Log("just set updateRun to true for Sphere");
        if (syncRBVelocity) rigidBody.velocity = rigidBodyVelocity;
        if (syncRBAngularVelocity) rigidBody.angularVelocity = rigidBodyAngularVelocity;
        if (syncRBPosition) rigidBody.position = rigidBodyPosition;
        if (syncRBRotation) rigidBody.rotation = rigidBodyRotation;
    }

    void InterpolateNextState()
    {        
        if (updateHistory == null) updateHistory = new LinkedList<UpdateRibidbodyItem>();
        if (gameObject.name == "Sphere") Debug.Log("InterpolateNextState() started, udpateHistory count: " + updateHistory.Count);
        if (updateHistory.Count < 1) return;
        if (gameObject.name == "Sphere") Debug.Log("Interpolating next position state");
        rigidBody.position += updateHistory.First.Value.rigidBodyVelocity * Time.deltaTime;
    }
}

