using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCoin : MonoBehaviour
{
    [SerializeField]
    private float rotateSpeed = 1.0f;
    void Start()
    {
        StartCoroutine(Spin());
    }
    private IEnumerator Spin()
    {
        while (true)
        {
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime * 100);
            yield return new WaitForEndOfFrame();
        }
    }
}
