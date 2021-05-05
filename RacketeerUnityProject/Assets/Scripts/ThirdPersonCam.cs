using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCam : MonoBehaviour
{
    public bool enabled;
    Camera camera;
    public float dampTime = 0.15f;
    private Vector3 velocity = Vector3.zero;
    //[HideInInspector] 
    public Transform target;
    Vector3 cameraStartTransformPosition;
    public float cameraHeight = 1f;
    public float cameraBack = 1f;
    Transform lookAtTransform;
    public float zoomOutBaseAmount = 3f;

    private void Start()
    {
        camera = Camera.main;
        cameraStartTransformPosition = camera.transform.position;

    }

    // Update is called once per frame
    private void Update()
    {
        if (enabled)
        {
            if (!lookAtTransform) lookAtTransform = GameObject.FindGameObjectWithTag("Ball").transform;




            transform.position = (target.position + Vector3.back*cameraHeight);

            transform.position += target.up * cameraBack;
            transform.rotation = Quaternion.LookRotation(target.up, Vector3.back);//

        }
        else
        {
            //transform.position = cameraStartTransformPosition;
        }

    }
}
