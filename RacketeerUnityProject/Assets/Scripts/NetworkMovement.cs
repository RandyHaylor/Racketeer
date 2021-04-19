using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Mirror;

public class NetworkMovement : NetworkBehaviour
{
    // taken from forum user Xuzon on this thread: https://forum.unity.com/threads/network-transform-lags.334718/
    #region Properties
    [SerializeField]
    protected Transform target;

    public bool clientAuthority = false;
    public bool useOwnerSpeed = true;

    #region Setup
    [Header("Setup")]
    [Range(0, 10)] public int SendRate = 2;
    [Range(0, 2)] public float movementThreshold = 0.2f;
    [Range(0, 30)] public float angleThreshold = 5;
    [Range(0, 10)] public float distanceBeforeSnap = 4;
    [Range(0, 90)] public float angleBeforeSnap = 40;
    #endregion

    #region Interpolation
    [Header("Interpolation")]
    [Range(0, 1)] public float movementInterpolation = 0.1f;
    [Range(0, 1)] public float rotationInterpolation = 0.1f;
    #endregion

    #region Prediction
    public float thresholdMovementPrediction = 0.7f;
    public float thresholdRotationPrediction = 15;
    #endregion

    #region ProtectedProperties
    protected Vector3 lastDirectionPerFrame = Vector3.zero;
    protected Vector3 lastPositionSent = Vector3.zero;
    protected Quaternion lastRotationSent = Quaternion.identity;
    protected Quaternion lastRotationDirectionPerFrame = Quaternion.identity;
    protected bool send = false;
    protected bool sending = false;
    protected int count = 0;
    protected Vector3 lastFixedUpdatePosition;
    protected Vector3 latestLocalSpeed;
    #endregion

    #endregion

    private void Awake()
    {
        lastFixedUpdatePosition = transform.position;
    }

    #region Logic
    void FixedUpdate()
    {
        if ((clientAuthority && !isServer) || (!clientAuthority && isServer))
        {
            latestLocalSpeed = (transform.position - lastFixedUpdatePosition) *Time.deltaTime;
            sendInfo();
        }
        else 
        {
            reconciliation();
        }

    }

    protected void sendInfo()
    {
        if (send)
        {
            if (count == SendRate)
            {
                count = 0;
                send = false;
                Vector3 v = target.position;
                Quaternion q = target.rotation;
                if (isServer)
                    RpcReceivePosition(v, q, latestLocalSpeed);
                else
                    CmdSendPosition(v, q, latestLocalSpeed);
            }
            else
            {
                count++;
            }
        }
        else
        {
            checkIfSend();
        }
    }
    protected void checkIfSend()
    {
        if (sending)
        {
            send = true;
            sending = false;
            return;
        }
        Vector3 v = target.position;
        Quaternion q = target.rotation;
        float distance = Vector3.Distance(lastPositionSent, v);
        float angle = Quaternion.Angle(lastRotationSent, q); ;
        if (distance > movementThreshold || angle > angleThreshold)
        {
            send = true;
            sending = true;
        }
    }
    protected void reconciliation()
    {
        Vector3 v = target.position;
        Quaternion q = target.rotation;
        float distance = Vector3.Distance(lastPositionSent, v);
        float angle = Vector3.Angle(lastRotationSent.eulerAngles, q.eulerAngles);
        if (distance > distanceBeforeSnap)
        {
            target.position = lastPositionSent;
        }
        if (angle > angleBeforeSnap)
        {
            target.rotation = lastRotationSent;
        }
        //prediction
        v += lastDirectionPerFrame;
        q *= lastRotationDirectionPerFrame;
        //interpolation
        Vector3 vLerp = Vector3.Lerp(v, lastPositionSent, movementInterpolation);
        Quaternion qLerp = Quaternion.Lerp(q, lastRotationSent, rotationInterpolation);
        target.position = vLerp;
        target.rotation = qLerp;
    }
    #endregion

    #region OverNetwork
    [Command(channel = Channels.Unreliable)]
    protected void CmdSendPosition(Vector3 newPos, Quaternion newRot, Vector3 senderSpeed)
    {
        RpcReceivePosition(newPos, newRot, senderSpeed);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    protected void RpcReceivePosition(Vector3 newPos, Quaternion newRot, Vector3 senderSpeed)
    {
        int frames = (SendRate + 1);

        if (useOwnerSpeed)
            lastDirectionPerFrame = senderSpeed;
        else
        {
            lastDirectionPerFrame = newPos - lastPositionSent;
            //right now prediction is made with the new direction and amount of frames
            lastDirectionPerFrame /= frames;

            if (lastDirectionPerFrame.magnitude > thresholdMovementPrediction)
            {
                lastDirectionPerFrame = Vector3.zero;
            }
        }

        Vector3 lastEuler = lastRotationSent.eulerAngles;
        Vector3 newEuler = newRot.eulerAngles;
        if (Quaternion.Angle(lastRotationDirectionPerFrame, newRot) < thresholdRotationPrediction)
        {
            lastRotationDirectionPerFrame = Quaternion.Euler((newEuler - lastEuler) / frames);
        }
        else
        {
            lastRotationDirectionPerFrame = Quaternion.identity;
        }
        lastPositionSent = newPos;
        lastRotationSent = newRot;        
    }
    #endregion
}
