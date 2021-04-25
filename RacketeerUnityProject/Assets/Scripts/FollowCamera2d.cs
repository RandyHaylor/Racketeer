using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera2d : MonoBehaviour
{
    public bool follow = true;

    Transform MainCameraTransform;
    Vector3 newPosition;  //reusable position vector3 so we're not creating a new one every fixedUpdate...

    private void Start()
    {
        MainCameraTransform = Camera.main.transform;
        newPosition.z = transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {
        if (follow)
        {
            newPosition.x = MainCameraTransform.position.x;
            newPosition.y = MainCameraTransform.position.y;
            transform.position = newPosition;
        }
    }
}
