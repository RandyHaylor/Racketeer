using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Player : NetworkBehaviour
{
    private Camera mainCam;
    public bool attachedView;
    private Vector3 strafe;
    private Vector3 moveForward;
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
    bool abilityButtonDown;

    float rotateBoostDuration = 0.1f;
    bool rotateBoostActive = false;
    float rotateBoostCooldown = 1f;
    bool rotateBoostCoolingDown = false;
    float rotateBoostAmount;//for passing temporary amounts, normally 0

    bool velocityBoostActive = false;
    float velocityBoostDuration = 0.1f;
    float velocityBoostCooldown = 1f;
    bool velocityBoostCoolingDown = false;

    PlayerAbility playerAbility;

    Vector3 movement;
    bool velocityBoostInput;
    Rigidbody rb;
    private void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody>();
        movement.z = 0f;
        mainCam = Camera.main;
    }
    void Update()
    {
        HandleMovement();
    }
    void HandleMovement()
    {
        if (isLocalPlayer)
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
            triggerAxis = Input.GetAxis("RightTriggerAxis");

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
                if (isLocalPlayer) SoundManager.PlaySound(PlayerBoostSoundClip, 0.25f, 0.4f, true);
                CmdPlayerBoostSoundOtherPlayers();
                CmdPlayerBoostParticles();
                
            }

            if (attachedView)
            {
                moveForward = movement.y * transform.up;
                strafe = movement.x * transform.right;
                rotate = rotate / 4;
            }
            else
            {
                strafe = movement.y * Vector3.up;
                moveForward = movement.x*Vector3.right;
            }
                
            abilityButtonDown = (Input.GetAxis("LeftTriggerAxis") > 0.1f);

            CmdApplyInputOnServer(strafe+moveForward, rotate, rotateBoostActive, velocityBoostActive, abilityButtonDown);
            
        }
    }
    [Command(requiresAuthority = false)] void CmdPlayerBoostParticles() => RpcPlayerBoostParticles();
    [ClientRpc] void RpcPlayerBoostParticles() => boostFireParticleSys.Play();

    [Command(requiresAuthority = false)] void CmdPlayerBoostSoundOtherPlayers() => RpcPlayerBoostSound();
    [ClientRpc] void RpcPlayerBoostSound(){if (!isLocalPlayer) SoundManager.PlaySound(PlayerBoostSoundClip, 0.25f, 0.4f, true);}


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
        //Debug.Log("Started AngularBoostTimer Coroutine");
        yield return new WaitForSeconds(rotateBoostDuration);
        rotateBoostCoolingDown = true;
        rotateBoostActive = false;
        rotateBoostAmount = 0f;
        yield return new WaitForSeconds(rotateBoostCooldown);
        rotateBoostCoolingDown = false;
    }


    [Command(requiresAuthority = false)] // function following this line is run only on server, and will therefore only affect server object (which has a rigidbody for physics sim)
    void CmdApplyInputOnServer(Vector3 directionalForce, float rotationalForce, bool rotationalBoostActive, bool velocityBoostActive, bool activateAbility)
    {
        if (Vector3.Dot(rb.velocity, directionalForce.normalized) < (GameManager.Instance.playerSpeedLimit * (velocityBoostActive? GameManager.Instance.speedLimitBoostMultiplier : 1f) ))
            rb.AddForce(directionalForce * (velocityBoostActive ? GameManager.Instance.velocityBoostMultiplier : 1f));
        
        if (
                (rotationalForce > 0  && rb.angularVelocity.z < (velocityBoostActive ? GameManager.Instance.angularSpeedLimitBoost : GameManager.Instance.angularSpeedLimitNormal) )
                ||
                (rotationalForce < 0 && rb.angularVelocity.z > -1 * (velocityBoostActive ? GameManager.Instance.angularSpeedLimitBoost : GameManager.Instance.angularSpeedLimitNormal))
            )
            rb.AddTorque(transform.forward * rotationalForce * (velocityBoostActive ? GameManager.Instance.rotationalBoostMultiplier : 1f));


        if (activateAbility)
        {
            playerAbility = GetComponentInChildren<PlayerAbility>();
            if (playerAbility != null && !playerAbility.activatingPlayerAbility)
            {
                if (playerAbility.ActivatePlayerAbility())
                    RemovePlayerAbilityClientAndServer();
            }
        }
    }

    void RemovePlayerAbilityClientAndServer()
    {
        RemovePlayerAbility();
        RpcRemovePlayerAbility();
    }

    [ClientRpc] void RpcRemovePlayerAbility() => RemovePlayerAbility();

    void RemovePlayerAbility()
    {
        if (GetComponentInChildren<PlayerAbility>() != null)
        {
            Destroy(GetComponentInChildren<PlayerAbility>().gameObject);
        }
    }
    
}
