using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FloatOscillator
{

    /// <summary>
    /// Outputs a float between 0 and 1 based on Time.time and provided speed and oscillator type
    /// </summary>
    public enum OscillatorType
    {
        Sine,
        Cosine,
        Sawtooth
    }

    public static float Oscillate(float pulseSpeed, OscillatorType oscillatorType)
    {
        if (oscillatorType == OscillatorType.Sine) return GetPulseAmountSine(pulseSpeed);
        if (oscillatorType == OscillatorType.Cosine) return GetPulseAmountCosine(pulseSpeed);
        if (oscillatorType == OscillatorType.Sawtooth) return GetPulseAmountSawtooth(pulseSpeed);
        return 0f;
    }
    public static float GetPulseAmountSine(float pulseSpeed)
    {
        return (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
    }

    public static float GetPulseAmountCosine(float pulseSpeed)
    {
        return (Mathf.Cos(Time.time * pulseSpeed) + 1f) / 2f;
    }

    public static float GetPulseAmountSawtooth(float pulseSpeed)
    {
        return (Mathf.PingPong(Time.time * pulseSpeed / 3f, 1f));
    }
}
