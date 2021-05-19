using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    Transform targetTransform;

    // Update is called once per frame
    void Update()
    {
        if (targetTransform) transform.position = targetTransform.position;
    }
    public void SetTargetTransform(Transform target)
    {
        targetTransform = target;
    }
}
