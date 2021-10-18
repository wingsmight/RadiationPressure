using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleDetail : Detail
{
    [SerializeField] private float radius;


    public float Radius { get => radius; set => radius = value; }
    public override float Area => Mathf.PI * (radius * radius);
}
