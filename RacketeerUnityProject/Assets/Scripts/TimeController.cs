using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chronos;
using Mirror;
public class TimeController : NetworkBehaviour
{
    public static TimeController Instance;
    List<string> rewindingKeys;
    public bool smoothTimeShift = true;
    public float RewindDuration = 1;
    public float RewindTimeScale = -2;
    public float timeScaleLerpDurationStart = 0.2f;
    public float timeScaleLerpDurationEnd = 0.2f;
    public bool steadyLerpTimeScaleStart;
    public bool steadyLerpTimeScaleEnd;
    [SyncVar]
    public uint SyncVar_playerControllingRewind = 999999999;

    public bool IsRewinding   // property
    {
        get { return (rewindingKeys.Count >0); } 
    }

    // Start is called before the first frame update
    void Start()
    {
        rewindingKeys = new List<string>();
    }

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Get the Enemies global clock
        

        // Change its time scale on key press
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            //clock.localTimeScale = -1; // Rewind
            StartCoroutine(RewindTimeCoroutine(RewindTimeScale, RewindDuration, "BallAndPlayers", 999999999));
        }
        
        /*else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            clock.localTimeScale = 0; // Pause
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            clock.localTimeScale = 0.5f; // Slow
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            clock.localTimeScale = 1; // Normal
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            clock.localTimeScale = 2; // Accelerate
        }
        */
    }
    
    public void RewindTime(uint objectNetIdToExempt) //exempt here means don't rewind that object - the player in question is still going to have rewind run on their system if it's not already running
    {
        if (!isServer) //non-hosting client starts rewinding immediately locally if they're the triggering party
        {
            if (!IsRewinding)
                RewindTimeInternal("BallAndPlayers", objectNetIdToExempt);
        }
        else //server gets separate command from player about button press and handles it differently. if client is already rewinding, new rewinding won't start
        {
            RpcRewindTime("BallAndPlayers", objectNetIdToExempt); //rewind won't run if it's already running, calling client will get a command to rewind from server but will ignore it
            RewindTimeInternal("BallAndPlayers", objectNetIdToExempt);
        }           
        
    }

    [ClientRpc]
    void RpcRewindTime(string clockKeyName, uint objectNetIdToExempt)
    {
        if (!IsRewinding)  
            RewindTimeInternal(clockKeyName, objectNetIdToExempt);
    }

    void RewindTimeInternal(string clockKey) => RewindTimeInternal(clockKey, 999999999);
    void RewindTimeInternal(string clockKey, uint objectNetIdToExempt)
    {
        StartCoroutine(RewindTimeCoroutine(RewindTimeScale, RewindDuration, clockKey, objectNetIdToExempt));
    }

    IEnumerator RewindTimeCoroutine(float rewindTimeScale, float rewindDuration, string clockKey, uint objectNetIdToExempt)
    {
        if (rewindingKeys.Contains(clockKey)) yield break; //if there are any keys in rewindingKeys, IsRewinding returns true

        GameObject objectToExempt = null;
        if (objectNetIdToExempt != 999999999)
            objectToExempt = NetworkIdentity.spawned[objectNetIdToExempt].gameObject;

        Debug.Log("RewindTimeCoroutine objectToExempt name: " + (objectToExempt == null ? "null" : objectToExempt.name));
       
        rewindingKeys.Add(clockKey);

        //disable rewinding on exempted object (originally designed to be calling player)
        if (objectToExempt) objectToExempt.GetComponent<Timeline>().rewindable = false;
        
        //lerp to reverse time
        if (smoothTimeShift)
        {
            Timekeeper.instance.Clock(clockKey).LerpTimeScale(rewindTimeScale, timeScaleLerpDurationStart, steadyLerpTimeScaleStart);
            yield return new WaitForSeconds(timeScaleLerpDurationStart);
        }
        //ensure timescale is set and wait specified time    
        Timekeeper.instance.Clock(clockKey).localTimeScale = rewindTimeScale;
        float startTime = Time.time;
        while (startTime + rewindDuration > Time.time)
        {
            yield return new WaitForFixedUpdate();
            if (SyncVar_playerControllingRewind != objectNetIdToExempt) //detect a server/client discrepancy in who pressed rewind first during a rewind, then try messy mid-rewind fix...
            {
                NetworkIdentity.spawned[objectNetIdToExempt].gameObject.GetComponent<Timeline>().rewindable = true;
                if (SyncVar_playerControllingRewind != 999999999)
                    NetworkIdentity.spawned[SyncVar_playerControllingRewind].gameObject.GetComponent<Timeline>().rewindable = false;
                objectNetIdToExempt = SyncVar_playerControllingRewind;
                if (SyncVar_playerControllingRewind != 999999999)
                    objectToExempt = NetworkIdentity.spawned[objectNetIdToExempt].gameObject;
            }
        }
        
                
        //lerp back to normal time
        if (smoothTimeShift)
        {
            Timekeeper.instance.Clock(clockKey).LerpTimeScale(1, timeScaleLerpDurationStart, steadyLerpTimeScaleEnd);
            yield return new WaitForSeconds(timeScaleLerpDurationEnd);
        }
            
        Timekeeper.instance.Clock(clockKey).localTimeScale = 1;
        rewindingKeys.Remove(clockKey);
        if (objectToExempt)
        {
            yield return new WaitForEndOfFrame();
            objectToExempt.GetComponent<Timeline>().ResetRecording();
            objectToExempt.GetComponent<Timeline>().rewindable = true;
        }
    }
}
