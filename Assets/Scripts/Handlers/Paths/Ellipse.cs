using System;
using UnityEngine;

[Serializable]
public class Ellipse
{
    [Range(1.0f, 2.0f)]
    [SerializeField] private float majorAxis;
    [Range(0.00000001f, 0.99f)]
    [SerializeField] private float eccentricity;


    private float minorAxis;


    public Vector2 Evaluate(float t)
    {
        this.minorAxis = Mathf.Sqrt((majorAxis * majorAxis) / (1 - (eccentricity * eccentricity)));

        float angle = Mathf.Deg2Rad * 360f * t;
        float x = Mathf.Sin(angle) * minorAxis;
        float y = Mathf.Cos(angle) * majorAxis;
        return new Vector2(x, y);
    }
}
