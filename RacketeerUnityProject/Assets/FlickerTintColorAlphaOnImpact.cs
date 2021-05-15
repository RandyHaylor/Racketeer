using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlickerTintColorAlphaOnImpact : MonoBehaviour
{
    public Material material;


    public float originalAlpha;
    public float minWaitTimeOriginalAlpha;
    public float maxWaitTimeOriginalAlpha;

    public float alternateAlpha;
    public float minWaitTimeAlternateAlpha;
    public float maxWaitTimeAlternateAlpha;

    public float minFlickerTime = 0.01f;
    public float maxFlickerTime = 0.05f;

    string _TintColor = "_TintColor";

    bool flickerActive;

    private void Start()
    {
        material = GetComponent<MeshRenderer>().material;
        originalAlpha = material.GetColor(_TintColor).a;
    }

    // Start is called before the first frame update
    private void OnCollisionEnter(Collision collision)
    {
        flickerActive = true;
        StartCoroutine(FlickerBetweenMaterials());
        StartCoroutine(DisableFlickeringAfterRandomWait());
    }
    
    IEnumerator DisableFlickeringAfterRandomWait()
    {
        yield return new WaitForSeconds(Random.Range(minFlickerTime, minFlickerTime));
        flickerActive = false;
    }
    IEnumerator FlickerBetweenMaterials()
    {
        while (flickerActive)
        {
            material.SetColor(_TintColor, new Color(material.GetColor(_TintColor).r, material.GetColor(_TintColor).g, material.GetColor(_TintColor).b, alternateAlpha));
            yield return new WaitForSeconds(Random.Range(minWaitTimeAlternateAlpha, maxWaitTimeAlternateAlpha));

            material.SetColor(_TintColor, new Color(material.GetColor(_TintColor).r, material.GetColor(_TintColor).g, material.GetColor(_TintColor).b, originalAlpha));
            yield return new WaitForSeconds(Random.Range(minWaitTimeOriginalAlpha, maxWaitTimeOriginalAlpha));
        }
    }

}
