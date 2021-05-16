using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Player : NetworkBehaviour
{
    [Serializable]
    public class PlayerInput
    {
        public float moveHorizontal;
        public float moveVertical;
        public float spin;
        public bool boostButton;
        public bool abilityButton;        
    }
    [Serializable]
    public class PlayerInputForce
    {
        public Vector3 movement;
        public Vector3 spin;
        public PlayerInputForce()
        {
            movement = Vector3.zero;
            spin = Vector3.zero;
        }
    }

    public LinkedList<PlayerInputForce> playerInputForceBuffer;
    private int playerInputForceBufferSize = 130; //effectively sets maximum supported latency no cost in leaving high, doing so to reduce the chance of this causing failure
    private PlayerInputForce emptyPlayerInputForce;
    private PlayerInputForce newPlayerInputForce;
    public PlayerInputForce LastAppliedPlayerForce;


    [Header("Player Specific Settings")] [Range(0, 1)]
    public float stickDeadzone = 0.2f;
    public float triggerDeadzone = 0.2f;

    public ParticleSystem boostFireParticleSys;
    public string PlayerBoostSoundClip;

    private PlayerInput localPlayerInputCache;
    Vector3 directionalForce;
    float horizontalMovementMult = 10f;
    float verticalMovementMult = 10f;
    float rotateMult = -10f;

    float rotateBoostAmount;//for passing temporary amounts, normally 0

    bool velocityBoostActive = false;
    bool speedUpPowerupActive = false;
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
        
        directionalForce = Vector3.zero;
        localPlayerInputCache = new PlayerInput();
        emptyPlayerInputForce = new PlayerInputForce();
        newPlayerInputForce = new PlayerInputForce();
        LastAppliedPlayerForce = new PlayerInputForce();
        //create and fill input buffer
        playerInputForceBuffer = new LinkedList<PlayerInputForce>();
        for (int i = 0; i < playerInputForceBufferSize - 1; i++)
            playerInputForceBuffer.AddFirst(emptyPlayerInputForce);

        StartCoroutine(RegisterWithNetworkRigidbodyController());
    }
    IEnumerator RegisterWithNetworkRigidbodyController()
    {
        yield return new WaitForEndOfFrame();
        NetworkRigidbodyController.Instance.RegisterPlayer(netId, this, rb, isLocalPlayer, NetworkIdentity.spawned[netId].playerNumber);
    }

    void FixedUpdate()
    {
        //Debug.Log(gameObject.name + " isLocalPLayer? " + isLocalPlayer + " netId: " + netId);
        //if (!isServer) return;
            //enforce maximum boosted speed limit on players
        if (rb.velocity.sqrMagnitude > GameManager.Instance.speedLimitBoostMultiplier * GameManager.Instance.playerSpeedLimit * GameManager.Instance.speedLimitBoostMultiplier * GameManager.Instance.playerSpeedLimit)
            rb.velocity = GameManager.Instance.speedLimitBoostMultiplier * GameManager.Instance.playerSpeedLimit * rb.velocity.normalized;
        if (rb.angularVelocity.sqrMagnitude > GameManager.Instance.angularSpeedLimitBoost * GameManager.Instance.angularSpeedLimitBoost)
            rb.angularVelocity = GameManager.Instance.angularSpeedLimitBoost * rb.velocity.normalized;            
    }
    void Update()
    {
        
    }
    public void HandleInput()
    {
        //Debug.Log("Called HandleInput nettime: " + NetworkTime.time);
        localPlayerInputCache.moveHorizontal = Mathf.Clamp(Input.GetAxis("LeftStickHorizontal") + Input.GetAxis("Horizontal"), -1, 1);
        localPlayerInputCache.moveVertical = Mathf.Clamp(Input.GetAxis("LeftStickVertical") + Input.GetAxis("Vertical"), -1, 1);
        if (Math.Abs(localPlayerInputCache.moveHorizontal) < stickDeadzone) localPlayerInputCache.moveHorizontal = 0f;
        if (Math.Abs(localPlayerInputCache.moveVertical) < stickDeadzone) localPlayerInputCache.moveVertical = 0f;

        localPlayerInputCache.moveHorizontal = localPlayerInputCache.moveHorizontal * horizontalMovementMult * Time.deltaTime;
        localPlayerInputCache.moveVertical = localPlayerInputCache.moveVertical * verticalMovementMult * Time.deltaTime;

        //right stick slower rotation code   
        localPlayerInputCache.spin = Input.GetAxis("RightStickHorizontal"); 
        if (Math.Abs(localPlayerInputCache.spin) < stickDeadzone) localPlayerInputCache.spin = 0f;
        localPlayerInputCache.spin = rotateMult * localPlayerInputCache.spin * Time.deltaTime;


        localPlayerInputCache.boostButton = (Input.GetAxis("RightTriggerAxis") > triggerDeadzone);
        localPlayerInputCache.abilityButton = (Input.GetAxis("LeftTriggerAxis") > triggerDeadzone);


        ApplyPlayerInput(localPlayerInputCache);
        if (!isServer) CmdApplyInputOnServer(localPlayerInputCache);

    }

    [ClientRpc] void RpcPlayerBoostParticles() { if (!isLocalPlayer) boostFireParticleSys.Play(); }

    IEnumerator VelocityBoostTimer()
    {
        yield return new WaitForSeconds(velocityBoostDuration);
        velocityBoostCoolingDown = true;
        velocityBoostActive = false;
        yield return new WaitForSeconds(velocityBoostCooldown);
        velocityBoostCoolingDown = false;
    }


    [Command(requiresAuthority = false)] // function following this line is run only on server, and will therefore only affect server object (which has a rigidbody for physics sim)
    void CmdApplyInputOnServer(PlayerInput rawPlayerInput) => ApplyPlayerInput(rawPlayerInput);
    
    void ApplyPlayerInput(PlayerInput rawPlayerInput)
    {
        if (TimeController.Instance.IsRewinding)
        {
            if (isLocalPlayer && !isServer) //hosting player doesn't buffer input forces, they're the authority
            {
                playerInputForceBuffer.AddFirst(emptyPlayerInputForce); //recording the zero input force for resimulations
                while (playerInputForceBuffer.Count > playerInputForceBufferSize)
                    playerInputForceBuffer.RemoveLast();
            }

            LastAppliedPlayerForce = emptyPlayerInputForce;

            return;  //currently ignoring/discarding all player inputs during rewind
        }

        if (rawPlayerInput.boostButton && !velocityBoostCoolingDown && !velocityBoostActive) //Math.Abs(rightTriggerAxis)
        {
            velocityBoostActive = true;
            StartCoroutine(VelocityBoostTimer());

            //play boost sound and particle system on self and other clients
            if (isLocalPlayer) SoundManager.PlaySound(PlayerBoostSoundClip, transform.position, SoundManager.UsersToPlayFor.SelfLocallyThenEveryone);

            if (isLocalPlayer) boostFireParticleSys.Play();
            else RpcPlayerBoostParticles();
            if (isLocalPlayer && isServer) RpcPlayerBoostParticles();
        }



        directionalForce.x = rawPlayerInput.moveHorizontal;
        directionalForce.y = rawPlayerInput.moveVertical;


        //if speed in the direction the user is pointing is below the speed limit, apply the movement force
        if (Vector3.Dot(rb.velocity, directionalForce.normalized) < (GameManager.Instance.playerSpeedLimit * (velocityBoostActive || speedUpPowerupActive ? GameManager.Instance.speedLimitBoostMultiplier : 1f)))
            newPlayerInputForce.movement = directionalForce * GameManager.Instance.playerBaseMovementForce * (velocityBoostActive || speedUpPowerupActive ? GameManager.Instance.velocityBoostMultiplier : 1f);
        else
            newPlayerInputForce.movement = Vector3.zero;


        //if the angular speed is below the angular speed limit in the desired rotation direction, apply the angular force
        if (
                (rawPlayerInput.spin > 0 && rb.angularVelocity.z < (velocityBoostActive ? GameManager.Instance.angularSpeedLimitBoost : GameManager.Instance.angularSpeedLimitNormal))
                ||
                (rawPlayerInput.spin < 0 && rb.angularVelocity.z > -1 * (velocityBoostActive ? GameManager.Instance.angularSpeedLimitBoost : GameManager.Instance.angularSpeedLimitNormal))
            )
            newPlayerInputForce.spin = transform.forward * rawPlayerInput.spin * GameManager.Instance.playerBaseAngularMovementForce * (velocityBoostActive ? GameManager.Instance.rotationalBoostMultiplier : 1f);
        else
            newPlayerInputForce.spin = Vector3.zero;

        //apply forces on server
        if (isServer)
        {
            ApplyForcesLocally(newPlayerInputForce.movement, newPlayerInputForce.spin);
        }
        else if (isLocalPlayer)//add new force to local buffer if is local player && not server (hosting client doesn't buffer input forces, they're the authority)
        {
            playerInputForceBuffer.AddFirst(newPlayerInputForce);  //forces applied in rigidbodystreamer script
            while (playerInputForceBuffer.Count > playerInputForceBufferSize)
                playerInputForceBuffer.RemoveLast();
        }

        LastAppliedPlayerForce = newPlayerInputForce;

        //if user is pressing the ability button, look for an inactive ability and use it then remove it, happens locally and on server separately (server is auth tho, local is just a guess)
        if (rawPlayerInput.abilityButton)
        {
            playerAbility = GetComponentInChildren<PlayerAbility>();
            if (playerAbility != null && !playerAbility.activatingPlayerAbility)
            {
                if (playerAbility.ActivatePlayerAbility())
                {
                    if (isLocalPlayer)
                        playerAbility.PlayAbilitySoundLocalThenEveryoneElse(); // local player gets responsibility for triggering ability sound everywhere so they can play first
                    
                    if (isServer) RemovePlayerAbilityClientAndServer(); //server or hosting client removes powerup locally then removes on everyone
                    else RemovePlayerAbility(); //remove locally for visual responsiveness (will create a potential window where you can't pick up another powerup due to latency)
                }                    
            }
        }
    }

    public void ApplyForcesLocally(Vector3 movementForce, Vector3 angularForce)
    {
        if (rb == null) return;
        rb.AddForce(movementForce, ForceMode.Impulse);
        rb.AddTorque(angularForce, ForceMode.Impulse);
    }

    public void GrantBoostForSeconds(float boostTime) => StartCoroutine(GrantBoostForSecondsCoroutine(boostTime));
    IEnumerator GrantBoostForSecondsCoroutine(float boostTime)
    {
        speedUpPowerupActive = true;
        yield return new WaitForSeconds(boostTime);
        speedUpPowerupActive = false;
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
