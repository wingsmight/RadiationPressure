using UnityEngine;

public class RectangleDetail : Detail
{
    [SerializeField] private float width;
    [SerializeField] private float height;


    private void Awake()
    {
        width = transform.localScale.x;
        height = transform.localScale.y;

        Scale();
    }


    // Перерасчет визуального отображения размеров относительно параметров
    private void Scale()
    {
        transform.localScale = new Vector3(width, height, transform.localScale.z);
    }


    public float Width
    {
        get => width;
        set
        {
            width = value;

            Scale();
        }
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
    public override float Area => width * height;
}
