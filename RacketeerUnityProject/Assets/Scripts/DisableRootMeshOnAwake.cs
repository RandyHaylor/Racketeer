using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableRootMeshOnAwake : MonoBehaviour
{
    // Start is called before the first frame update
    private void Awake()
    {
        StartCoroutine(DisableMeshRenderer());
    }
    IEnumerator DisableMeshRenderer()
    {
        yield return new WaitForEndOfFrame();
        gameObject.GetComponent<MeshRenderer>().enabled = false;
    }
}

