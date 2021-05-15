using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCoin : MonoBehaviour
{
    [SerializeField]
    public float rotateSpeed = 1.0f;
    Vector3 random3dRotation;
    void Start()
    {
        random3dRotation = Random.insideUnitSphere;
        var newRotation = transform.rotation * Quaternion.AngleAxis(60f, Random.insideUnitCircle);
        transform.rotation = newRotation;
        StartCoroutine(Spin(random3dRotation));
    }
    private IEnumerator Spin(Vector3 rotation)
    {
        while (true)
        {
            transform.Rotate(rotation.x * Time.deltaTime * rotateSpeed, rotation.y * Time.deltaTime * rotateSpeed, rotation.z * Time.deltaTime * rotateSpeed);
            yield return new WaitForEndOfFrame();
        }
    }
}
