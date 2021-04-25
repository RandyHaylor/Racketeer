using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessRotation : MonoBehaviour
{
    public Vector3 rotateSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(rotateSpeed.x * Time.fixedDeltaTime, rotateSpeed.y * Time.fixedDeltaTime, rotateSpeed.z * Time.deltaTime);
    }
}
