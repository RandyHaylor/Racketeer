using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;
using static FloatOscillator;

public class AnimatedCircle : MonoBehaviour
{

    public Color FillColor;

    public bool Border;
    public Color BorderColor;
    
    public float BorderWidth = 0.2f;

    public float Radius = 2f;


    public bool Sector;

    [Range(0f, 360f)]
    public float InitialAngle;
    [Range(0f, 360f)]
    public float ArcLength = 360f;

    [Header("Variables to Pulse Border Thickness")]
    public bool pulseBorderThickness;
    public bool alsoExpandRadius;
    public PulseType pulseType;
    public enum PulseType { sine, sawtooth }

    public float pulseBorderThicknessSpeed = 5;
    public float pulseBorderThicknessAmount = 0.2f;
    float borderThicknessDelta;
    float radiusDelta;

    private void Update()
    {
        if (pulseBorderThickness) PulseBorderThickness();
        if (alsoExpandRadius) AdjustRadiusToKeepBorderCentered();

        var circleInfo = new CircleInfo
        {
            center = transform.position,
            forward = transform.forward,
            radius = Radius + (pulseBorderThickness && alsoExpandRadius ? radiusDelta : 0f),
            fillColor = FillColor
        };


        if (Border)
        {
            circleInfo.bordered = true;
            circleInfo.borderColor = BorderColor;
            circleInfo.borderWidth = BorderWidth + (pulseBorderThickness?borderThicknessDelta:0f);
        }

        if (Sector)
        {
            circleInfo.isSector = true;
            circleInfo.sectorInitialAngleInDegrees = InitialAngle;
            circleInfo.sectorArcLengthInDegrees = ArcLength;
        }


        Circle.Draw(circleInfo);
    }

    void PulseBorderThickness()
    {
        if (pulseType == PulseType.sine)
        {
            borderThicknessDelta = GetPulseAmountSine();
        }
        else if (pulseType == PulseType.sawtooth)
        {
            borderThicknessDelta = GetPulseAmountSawtooth();
        }

        /*
        else if (pulseType == PulseType.sineAndSawtooth)
        {
            borderThicknessDelta = GetBlendedPulseAmountSineSawtooth();                
        }
        */
    }

    float GetPulseAmountSine()
    {
        return pulseBorderThicknessAmount * Oscillate(pulseBorderThicknessSpeed, OscillatorType.Sine);// (Mathf.Sin(Time.time * pulseBorderThicknessSpeed) + 1f) / 2f;
    }

    float GetPulseAmountSawtooth()
    {
        return pulseBorderThicknessAmount * Oscillate(pulseBorderThicknessSpeed, OscillatorType.Sawtooth);
    }

    void AdjustRadiusToKeepBorderCentered()
    {
        radiusDelta = borderThicknessDelta/2f;
    }
}
