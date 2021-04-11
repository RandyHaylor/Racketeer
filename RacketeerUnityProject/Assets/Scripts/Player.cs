using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : NetworkBehaviour
{
    float moveHorizontal;
    float horizontalMovementMult = 1500f;
    float moveVertical;
    float verticalMovementMult = 1500f;
    float rotate;
    float rotateMult = -500f;
    Vector3 movement;
    Rigidbody rb;
    private void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody>();
        movement.z = 0f;
    }
    void HandleMovement()
    {
        if (isLocalPlayer)
        {
            moveHorizontal = Input.GetAxis("Horizontal");
            moveVertical = Input.GetAxis("Vertical");
            //movement = new Vector3(moveHorizontal * horizontalMovementMult, moveVertical* verticalMovementMult, 0);
            //below code prevents creating a new Vector3 object every frame that will eventually be cleaned up by garbage collection, which can cause a lag spike
            //  movement and rotate are global objects for this reason - they get re-used each frame
            movement.x = moveHorizontal * horizontalMovementMult * Time.deltaTime;
            movement.y = moveVertical * verticalMovementMult * Time.deltaTime;
            //transform.position = transform.position + movement;

            rotate = rotateMult * Input.GetAxis("RightStickHorizontal") * Time.deltaTime;

            CmdApplyForceOnServer(movement, rotate);
            
        }
    }
    void Update()
    {
        HandleMovement();
    }

    [Command] // function following this line is run only on server, and will therefore only affect server object (which has a rigidbody for physics sim)
    void CmdApplyForceOnServer(Vector3 directionalForce, float rotationalForce)
    {
        rb.AddForce(directionalForce);
        rb.AddTorque(transform.forward * rotationalForce);
    }
    
}
