using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : NetworkBehaviour
{
    float moveHorizontal;
    float horizontalMovementMult = 25f;
    float moveVertical;
    float verticalMovementMult = 25f;
    float rotate;
    float rotateMult = -10f;
    Vector3 movement;
    Rigidbody rb;
    private void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody>();
    }
    void HandleMovement()
    {
        if (isLocalPlayer)
        {
            moveHorizontal = Input.GetAxis("Horizontal");
            moveVertical = Input.GetAxis("Vertical");
            movement = new Vector3(moveHorizontal * horizontalMovementMult, moveVertical* verticalMovementMult, 0);
            //transform.position = transform.position + movement;
            rb.AddForce(movement);
            rotate = rotateMult * Input.GetAxis("RightStickHorizontal");
            rb.AddTorque(transform.forward * rotate);
        }
    }
    void Update()
    {
        HandleMovement();
    }
    
}
