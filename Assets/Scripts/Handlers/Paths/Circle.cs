using System;
using UnityEngine;

[Serializable]
public class Circle
{
    [SerializeField] private float radius;
    [SerializeField] private int stepCount;


    public Circle(float radius, int stepCount)
    {
        this.radius = radius;
        this.stepCount = stepCount;
    }


    public float Radius => radius;
    public int StepCount => stepCount;
}
