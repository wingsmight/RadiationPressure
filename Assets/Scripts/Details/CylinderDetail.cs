using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylinderDetail : Detail
{
    [SerializeField] private float height;
    [SerializeField] private float diameter;


    private void Awake()
    {
        height = transform.localScale.y;
        diameter = transform.localScale.x;

        Scale();
    }


    private void Scale()
    {
        transform.localScale = new Vector3(diameter, diameter, height);
    }


    public float Height
    {
        get => height;
        set
        {
            height = value;

            Scale();
        }
    }
    public float Diameter
    {
        get => diameter;
        set
        {
            diameter = value;

            Scale();
        }
    }
    public override float Area => Mathf.PI * diameter * height;
}
