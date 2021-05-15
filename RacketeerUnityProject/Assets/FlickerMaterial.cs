using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlickerMaterial : MonoBehaviour
{
    public MeshRenderer meshRenderer;

    public Material material1;

    public float minWaitTimeMaterial1;
    public float maxWaitTimeMaterial1;

    public Material material2;
    public float minWaitTimeMaterial2;
    public float maxWaitTimeMaterial2;

    public float minQuickFlicker = 0.01f;
    public float maxQuickFlicker = 0.05f;
    public bool enableQuickFlickerMaterial2 = true;
    bool quickFlickerActive;
    

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(FlickerBetweenMaterials());
    }

    IEnumerator FlickerBetweenMaterials()
    {
        while (true)
        {
            quickFlickerActive = false;
            meshRenderer.material = material1;
            yield return new WaitForSeconds(Random.Range(minWaitTimeMaterial1, maxWaitTimeMaterial1));

            quickFlickerActive = true;
            if (enableQuickFlickerMaterial2) StartCoroutine(QuickFlicker());
            else meshRenderer.material = material2;
            yield return new WaitForSeconds(Random.Range(minWaitTimeMaterial2, maxWaitTimeMaterial2));
        }
    }

    IEnumerator QuickFlicker()
    {
        while (quickFlickerActive)
        {
            if (quickFlickerActive) meshRenderer.material = material2;
            yield return new WaitForSeconds(Random.Range(minQuickFlicker, maxQuickFlicker));

            if (quickFlickerActive) meshRenderer.material = material1;
            yield return new WaitForSeconds(Random.Range(minQuickFlicker, maxQuickFlicker));
        }
    }
}
