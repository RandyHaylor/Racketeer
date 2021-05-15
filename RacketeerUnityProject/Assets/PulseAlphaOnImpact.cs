using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseAlphaOnImpact : MonoBehaviour
{
    public Material material;


    public float originalAlpha;

    public float alternateAlpha = 0.1f;

    public float timeToRestoreAlpha = 0.5f;

    string _TintColor = "_TintColor";

    float lerpSpeed;
    

    bool flickerActive;

    private void Start()
    {        
        material = GetComponent<MeshRenderer>().material;
        originalAlpha = material.GetColor(_TintColor).a;
        lerpSpeed = (originalAlpha - alternateAlpha) / timeToRestoreAlpha;
    }


    private void OnCollisionEnter(Collision collision)
    {
        material.SetColor(_TintColor, new Color(material.GetColor(_TintColor).r, material.GetColor(_TintColor).g, material.GetColor(_TintColor).b, alternateAlpha));
    }

    private void Update()
    {
        if (material.GetColor(_TintColor).a != originalAlpha)
            material.SetColor(_TintColor, new Color(material.GetColor(_TintColor).r, material.GetColor(_TintColor).g, material.GetColor(_TintColor).b, Mathf.Clamp(material.GetColor(_TintColor).a + lerpSpeed*Time.deltaTime, 0f, 1f)));
    }
}
