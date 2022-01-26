using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleDetail : Detail
{
    [SerializeField] private float width;
    [SerializeField] private float height;


    public float Width { get => width; set => width = value; }
    public float Height { get => height; set => height = value; }
    public override float Area => width * height / 2;
}
