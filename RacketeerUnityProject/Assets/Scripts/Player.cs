using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : NetworkBehaviour
{
    float speedLimit = 5f;
    float angularSpeedLimitNormal = 4f;
    float angularSpeedLimitBoost = 7f; //10 is the current global limit as well - have to increase in project settings as well to go higher with this
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
    float rotateBoostCooldown = 0.5f;
    bool rotateBoostCoolingDown = false;
    float rotateBoostAmount;
    Vector3 movement;
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
        if (isLocalPlayer)
        {
            moveHorizontal = Input.GetAxis("Horizontal");
            moveVertical = Input.GetAxis("Vertical");
            if (Math.Abs(moveHorizontal) < 0.1f) moveHorizontal = 0f;
            if (Math.Abs(moveVertical) < 0.1f) moveVertical = 0f;
            //movement = new Vector3(moveHorizontal * horizontalMovementMult, moveVertical* verticalMovementMult, 0);
            //below code prevents creating a new Vector3 object every frame that will eventually be cleaned up by garbage collection, which can cause a lag spike
            //  movement and rotate are global objects for this reason - they get re-used each frame
            movement.x = moveHorizontal * horizontalMovementMult * Time.deltaTime;
            movement.y = moveVertical * verticalMovementMult * Time.deltaTime;

            rotateStickHorizontal = Input.GetAxis("RightStickHorizontal");
            if (Math.Abs(rotateStickHorizontal) < 0.1f) rotateStickHorizontal = 0f;
            rotate = rotateMult * rotateStickHorizontal * Time.deltaTime;
            triggerAxis = Input.GetAxis("triggerAxis");

            if ( Math.Abs(triggerAxis) > 0.1f && !rotateBoostCoolingDown && !rotateBoostActive)
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
                StartCoroutine(AngularBoostTimer());
            }

            CmdApplyForceOnServer(movement, rotate, rotateBoostAmount);
            
        }
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


    [Command] // function following this line is run only on server, and will therefore only affect server object (which has a rigidbody for physics sim)
    void CmdApplyForceOnServer(Vector3 directionalForce, float rotationalForce, float rotationalBoostForce)
    {
        if (Vector3.Dot(rb.velocity, directionalForce.normalized) < speedLimit)
            rb.AddForce(directionalForce);
        
        if (
                (rotationalForce > 0  && rb.angularVelocity.z < angularSpeedLimitNormal) 
                ||
                (rotationalForce < 0 && rb.angularVelocity.z > -1 * angularSpeedLimitNormal)
            )
            rb.AddTorque(transform.forward * rotationalForce);

        if (
                (rotationalBoostForce > 0 && rb.angularVelocity.z < angularSpeedLimitBoost)
                ||
                (rotationalBoostForce < 0 && rb.angularVelocity.z > -1 * angularSpeedLimitBoost)
            )
            rb.AddTorque(transform.forward * rotationalBoostForce);
    }


    
}
