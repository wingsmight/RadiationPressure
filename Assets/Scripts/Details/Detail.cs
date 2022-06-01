using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detail : MonoBehaviour
{
    [SerializeField] protected Coating coating;
    [SerializeField] protected MeshRenderer meshRenderer;


    private void OnEnable()
    {
        Coating = coating;
    }


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
