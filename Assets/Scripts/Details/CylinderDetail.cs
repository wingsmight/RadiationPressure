using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylinderDetail : Detail
{
    [SerializeField] private float height;
    [SerializeField] private float radius;


    public float Height { get => height; set => height = value; }
    public float Radius { get => radius; set => radius = value; }
    public override float Area => 2 * Mathf.PI * radius * height;
}
