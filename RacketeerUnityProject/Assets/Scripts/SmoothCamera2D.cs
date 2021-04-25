using UnityEngine;
using System.Collections;

public class SmoothCamera2D : MonoBehaviour
{
    Camera camera;
    public float dampTime = 0.15f;
    private Vector3 velocity = Vector3.zero;
    [HideInInspector]
    public Transform target;
    Vector3 cameraStartTransformPosition;
    float cameraZoomOutAmount;
    Transform zoomOutTransform;
    public float zoomOutBaseAmount = 3f;

    private void Start()
    {
        camera = Camera.main;
        cameraStartTransformPosition = camera.transform.position;
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (target)
        {
            if (!zoomOutTransform) zoomOutTransform = GameObject.FindGameObjectWithTag("Ball").transform;
            cameraZoomOutAmount = Vector3.Distance(target.position, zoomOutTransform.position) + zoomOutBaseAmount;
            Vector3 point = camera.WorldToViewportPoint(target.position);
            Vector3 delta = (target.position+zoomOutTransform.position)/2 - camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, (-1*cameraStartTransformPosition.z) + cameraZoomOutAmount)); //(new Vector3(0.5, 0.5, point.z));     - (targetHasRigidbody?targetRigidbody.velocity.magnitude:0)
            Vector3 destination = transform.position + delta;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, dampTime);
            
        }
        else
        {
            transform.position = cameraStartTransformPosition;
        }

    }
}
