using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Player : NetworkBehaviour
{

    [Header("Player Specific Settings")] [Range(0, 1)]
    public float deadzone = 0.15f;
    
    
    private Camera mainCam;
    public bool attachedView;
    private Vector3 strafe;
    private Vector3 moveForward;
    public ParticleSystem boostFireParticleSys;
    public string PlayerBoostSoundClip;

    float moveHorizontal;
    float horizontalMovementMult = 3000f;
    float moveVertical;
    float verticalMovementMult = 3000f;
    float rotateStickHorizontal;
    float rotate;
    float rotateMult = -600f;
    float rightTriggerAxis;
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
    
    void FixedUpdate()
    {
        if (!isServer) return;
            //enforce maximum boosted speed limit on players
        if (rb.velocity.sqrMagnitude > GameManager.Instance.speedLimitBoostMultiplier * GameManager.Instance.playerSpeedLimit * GameManager.Instance.speedLimitBoostMultiplier * GameManager.Instance.playerSpeedLimit)
            rb.velocity = GameManager.Instance.speedLimitBoostMultiplier * GameManager.Instance.playerSpeedLimit * rb.velocity.normalized;
        if (rb.angularVelocity.sqrMagnitude > GameManager.Instance.angularSpeedLimitBoost * GameManager.Instance.angularSpeedLimitBoost)
            rb.angularVelocity = GameManager.Instance.angularSpeedLimitBoost * rb.velocity.normalized;            
    }
    void Update()
    {
        if (isLocalPlayer) HandleInput();
    }
    void HandleInput()
    {
        moveHorizontal = Mathf.Clamp(Input.GetAxis("LeftStickHorizontal") + Input.GetAxis("Horizontal"), -1, 1);
        moveVertical = Mathf.Clamp(Input.GetAxis("LeftStickVertical") + Input.GetAxis("Vertical"), -1, 1);
        if (Math.Abs(moveHorizontal) < deadzone) moveHorizontal = 0f;
        if (Math.Abs(moveVertical) < deadzone) moveVertical = 0f;
        //movement = new Vector3(moveHorizontal * horizontalMovementMult, moveVertical* verticalMovementMult, 0);

        //  movement and rotate are global objects because they get re-used each frame
        movement.x = moveHorizontal * horizontalMovementMult * Time.deltaTime;
        movement.y = moveVertical * verticalMovementMult * Time.deltaTime;

        //right stick slower rotation code   
        rotateStickHorizontal = Input.GetAxis("RightStickHorizontal"); 
        if (Math.Abs(rotateStickHorizontal) < deadzone) rotateStickHorizontal = 0f;
        rotate = rotateMult * rotateStickHorizontal * Time.deltaTime;

        //rotational boost code (triggers)
        rightTriggerAxis = Input.GetAxis("RightTriggerAxis");

        if (rightTriggerAxis > 0.2f && !rotateBoostCoolingDown && !rotateBoostActive) //Math.Abs(rightTriggerAxis)
        {
            if (rightTriggerAxis>0f)
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
            
            //play boost sound and particle system on self and other clients
            SoundManager.PlaySound(PlayerBoostSoundClip, 0.25f, 0.4f, true);
            
            boostFireParticleSys.Play();
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
        
        //only send movement updates to server if rewinding is not active
        if (!GameManager.Instance.syncVar_RewindingActive) CmdApplyInputOnServer(strafe+moveForward, rotate, rotateBoostActive, velocityBoostActive, abilityButtonDown);
    }
    [Command(requiresAuthority = false)] void CmdPlayerBoostParticles() => RpcPlayerBoostParticles();
    [ClientRpc] void RpcPlayerBoostParticles() { if (!isLocalPlayer) boostFireParticleSys.Play(); }

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
    void CmdApplyInputOnServer(Vector3 directionalForce, float rotationalForce, bool rotationalBoostActive, bool velocityBoostActiveClient, bool activateAbility)
    {
        if (GameManager.Instance.syncVar_RewindingActive) return; //discarding all movement input/force application during rewind
        //if speed in the direction the user is pointing is below the speed limit, apply the movement force
        if (Vector3.Dot(rb.velocity, directionalForce.normalized) < (GameManager.Instance.playerSpeedLimit * (velocityBoostActiveClient? GameManager.Instance.speedLimitBoostMultiplier : 1f) ))
            rb.AddForce(directionalForce * GameManager.Instance.playerBaseMovementForce * (velocityBoostActiveClient ? GameManager.Instance.velocityBoostMultiplier : 1f));
        
        //if the angular speed is below the angular speed limit, apply the angular force
        if (
                (rotationalForce > 0  && rb.angularVelocity.z < (velocityBoostActiveClient ? GameManager.Instance.angularSpeedLimitBoost : GameManager.Instance.angularSpeedLimitNormal) )
                ||
                (rotationalForce < 0 && rb.angularVelocity.z > -1 * (velocityBoostActiveClient ? GameManager.Instance.angularSpeedLimitBoost : GameManager.Instance.angularSpeedLimitNormal))
            )
            rb.AddTorque(transform.forward * rotationalForce* GameManager.Instance.playerBaseAngularMovementForce * (velocityBoostActiveClient ? GameManager.Instance.rotationalBoostMultiplier : 1f));

        //if user is pressing the ability button, look for an inactive ability and use it then remove it
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
