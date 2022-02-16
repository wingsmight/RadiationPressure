using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathfExt
{
    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            value = min;
        }
        else if (value > max)
        {
            value = max;
        }

        return value;
    }
    public static float EaseOut(float value)
    {
        return Mathf.Sin(value * Mathf.PI * 0.5f);
    }
    public static float EaseIn(float value)
    {
        return 1f - Mathf.Cos(value * Mathf.PI * 0.5f);
    }
    public static float Exp(float value)
    {
        return Mathf.Exp(value);
    }
    public static float Smoothstep2(float value)
    {
        return value * value * (3.0f - 2.0f * value);
    }
    public static float Smoothstep3(float value)
    {
        return value * value * value * (value * (6f * value - 15f) + 10f);
    }
}
