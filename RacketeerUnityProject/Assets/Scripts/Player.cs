using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public float rotateMult = -10f;
    public float verticalMovementMult = 25f;
    public float horizontalMovementMult = 25f;

    private float moveHorizontal;
    private float moveVertical;
    private float rotate;

    private Vector3 movement;
    private Rigidbody rb;

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
            movement = new Vector3(moveHorizontal * horizontalMovementMult * Time.deltaTime, moveVertical * verticalMovementMult * Time.deltaTime, 0);
            //transform.position = transform.position + movement;
            rb.AddForce(movement);
            rotate = rotateMult * Input.GetAxis("RightStickHorizontal");
            rb.AddTorque(transform.forward * rotate * Time.deltaTime);
        }
    }
    void Update()
    {
        HandleMovement();
    }
    
}
