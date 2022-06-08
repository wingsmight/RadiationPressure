using UnityEngine;

public class CircleDetail : Detail
{
    [SerializeField] private float diameter;


    private void Awake()
    {
        diameter = transform.localScale.x;

        Scale();
    }


    private void Scale()
    {
        transform.localScale = new Vector3(diameter, diameter, transform.localScale.z);
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
    public override float Area => Mathf.PI * (diameter / 4.0f);
}
