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
            StartCoroutine(RewindTimeCoroutine(RewindTimeScale, RewindDuration, "BallAndPlayers", null));
            StartCoroutine(RewindTimeCoroutine(RewindTimeScale, RewindDuration, "MusicAndCoins", null));
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
    
    public void RewindTime(GameObject objectToExempt)
    {
        if (!isServer) { Debug.Log("Can only run this from Server... "); return; }
        //rewind physics objects on server
        RewindTimeInternal("BallAndPlayers", objectToExempt);
        RewindTimeInternal("MusicAndCoins");
        //rewind music and coins on all clients
        RpcRewindTime("MusicAndCoins");
    }

    [ClientRpc] void RpcRewindTime(string clockKeyName) => RewindTimeInternal(clockKeyName);

    void RewindTimeInternal(string clockKey) => RewindTimeInternal(clockKey, null);
    void RewindTimeInternal(string clockKey, GameObject objectToExempt)
    {
        StartCoroutine(RewindTimeCoroutine(RewindTimeScale, RewindDuration, clockKey, objectToExempt));
    }

    IEnumerator RewindTimeCoroutine(float rewindTimeScale, float rewindDuration, string clockKey, GameObject objectToExempt)
    {
        if (rewindingKeys.Contains(clockKey)) yield break;
       
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
        yield return new WaitForSeconds(rewindDuration);
                
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
