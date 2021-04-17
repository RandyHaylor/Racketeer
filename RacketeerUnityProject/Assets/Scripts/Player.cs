using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Player : NetworkBehaviour
{
    public ParticleSystem boostFireParticleSys;
    public AudioClip PlayerBoostSoundClip;

    float moveHorizontal;
    float horizontalMovementMult = 3000f;
    float moveVertical;
    float verticalMovementMult = 3000f;
    float rotateStickHorizontal;
    float rotate;
    float rotateMult = -600f;
    float triggerAxis;

    float rotateBoostDuration = 0.1f;
    bool rotateBoostActive = false;
    float rotateBoostCooldown = 1f;
    bool rotateBoostCoolingDown = false;
    float rotateBoostAmount;//for passing temporary amounts, normally 0

    bool velocityBoostActive = false;
    float velocityBoostDuration = 0.1f;
    float velocityBoostCooldown = 1f;
    bool velocityBoostCoolingDown = false;

    Vector3 movement;
    bool velocityBoostInput;
    Rigidbody rb;
    private void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody>();
        movement.z = 0f;
    }
    void Update()
    {
        HandleMovement();
    }
    void HandleMovement()
    {
        if (isLocalPlayer && isClient)
        {
            moveHorizontal = Mathf.Clamp(Input.GetAxis("LeftStickHorizontal") + Input.GetAxis("Horizontal"), -1, 1);
            moveVertical = Mathf.Clamp(Input.GetAxis("LeftStickVertical") + Input.GetAxis("Vertical"), -1, 1);
            if (Math.Abs(moveHorizontal) < 0.15f) moveHorizontal = 0f;
            if (Math.Abs(moveVertical) < 0.15f) moveVertical = 0f;
            //movement = new Vector3(moveHorizontal * horizontalMovementMult, moveVertical* verticalMovementMult, 0);
            //below code prevents creating a new Vector3 object every frame that will eventually be cleaned up by garbage collection, which can cause a lag spike
            //  movement and rotate are global objects for this reason - they get re-used each frame
            movement.x = moveHorizontal * horizontalMovementMult * Time.deltaTime;
            movement.y = moveVertical * verticalMovementMult * Time.deltaTime;

            //right stick slower rotation code   
            rotateStickHorizontal = Input.GetAxis("RightStickHorizontal"); 
            if (Math.Abs(rotateStickHorizontal) < 0.1f) rotateStickHorizontal = 0f;
            rotate = rotateMult * rotateStickHorizontal * Time.deltaTime;

            //velocity boost code (bumpers)
            /*
            if (Input.GetButton("LeftBumper") || Input.GetButton("RightBumper") || Input.GetButton("Submit")) //will be true for a couple hundred frames...
                velocityBoostInput = true;
            else
                velocityBoostInput = false;
            

            if (velocityBoostInput && !velocityBoostCoolingDown && !velocityBoostActive)
            {
                velocityBoostActive = true;
                StartCoroutine(VelocityBoostTimer());
            }*/

            //rotational boost code (triggers)
            triggerAxis = Input.GetAxis("triggerAxis");

            if (triggerAxis > 0.1f && !rotateBoostCoolingDown && !rotateBoostActive) //Math.Abs(triggerAxis)
            {
                if (triggerAxis>0f)
                {
                    rotateBoostAmount = rotateMult * 2;
                }
                else
                {
                    rotateBoostAmount = rotateMult * - 2;
                }
                rotateBoostActive = true;
                velocityBoostActive = true;
                StartCoroutine(VelocityBoostTimer());
                StartCoroutine(AngularBoostTimer());
                if (isLocalPlayer) SoundManager.PlaySound(PlayerBoostSoundClip, 0.25f, 0.5f);
                CmdPlayerBoostSoundOtherPlayers();
                CmdPlayerBoostParticles();
                
            }
            CmdApplyForceOnServer(movement, rotate, rotateBoostActive, velocityBoostActive);
            
        }
    }
    [Command(requiresAuthority = false)] void CmdPlayerBoostParticles() => RpcPlayerBoostParticles();
    [ClientRpc] void RpcPlayerBoostParticles() => boostFireParticleSys.Play();

    [Command(requiresAuthority = false)] void CmdPlayerBoostSoundOtherPlayers() => RpcPlayerBoostSound();
    [ClientRpc] void RpcPlayerBoostSound(){if (!isLocalPlayer) SoundManager.PlaySound(PlayerBoostSoundClip, 0.25f, 0.5f);}


    IEnumerator VelocityBoostTimer()
    {
        yield return new WaitForSeconds(velocityBoostDuration);
        velocityBoostCoolingDown = true;
        velocityBoostActive = false;
        yield return new WaitForSeconds(velocityBoostCooldown);
        velocityBoostCoolingDown = false;
    }
    IEnumerator AngularBoostTimer()
    {
        Debug.Log("Started AngularBoostTimer Coroutine");
        yield return new WaitForSeconds(rotateBoostDuration);
        rotateBoostCoolingDown = true;
        rotateBoostActive = false;
        rotateBoostAmount = 0f;
        yield return new WaitForSeconds(rotateBoostCooldown);
        rotateBoostCoolingDown = false;
    }


    [Command(requiresAuthority = false)] // function following this line is run only on server, and will therefore only affect server object (which has a rigidbody for physics sim)
    void CmdApplyForceOnServer(Vector3 directionalForce, float rotationalForce, bool rotationalBoostActive, bool velocityBoostActive)
    {
        if (Vector3.Dot(rb.velocity, directionalForce.normalized) < (GameManager.Instance.playerSpeedLimit * (velocityBoostActive? GameManager.Instance.speedLimitBoostMultiplier : 1f) ))
            rb.AddForce(directionalForce * (velocityBoostActive ? GameManager.Instance.VelocityBoostMultiplier : 1f));
        
        if (
                (rotationalForce > 0  && rb.angularVelocity.z < (velocityBoostActive ? GameManager.Instance.angularSpeedLimitBoost : GameManager.Instance.angularSpeedLimitNormal) )
                ||
                (rotationalForce < 0 && rb.angularVelocity.z > -1 * (velocityBoostActive ? GameManager.Instance.angularSpeedLimitBoost : GameManager.Instance.angularSpeedLimitNormal))
            )
            rb.AddTorque(transform.forward * rotationalForce * (velocityBoostActive ? GameManager.Instance.RotationalBoostMultiplier : 1f));
        /* old code provided a fixed rotational boost, new code just boosts whatever you're doing, rotation or velocity
        if (
                (rotationalBoostForce > 0 && rb.angularVelocity.z < angularSpeedLimitBoost)
                ||
                (rotationalBoostForce < 0 && rb.angularVelocity.z > -1 * angularSpeedLimitBoost)
            )
            rb.AddTorque(transform.forward * rotationalBoostForce);
        */
    }


    
}
