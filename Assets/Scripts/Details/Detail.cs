using UnityEngine;

public class Detail : MonoBehaviour
{
    [SerializeField] protected Coating coating;
    [SerializeField] protected MeshRenderer meshRenderer;


    // Эта функция вызывается, когда объект становится включенным и активным.
    private void OnEnable()
    {
        Coating = coating;
    }


    // Площадь
    public virtual float Area
    {
        get;
    }
    public Coating Coating
    {
        get
        {
            return coating;
        }
        set
        {
            coating = value;
            meshRenderer.material = coating.Material;
        }
    }
}
