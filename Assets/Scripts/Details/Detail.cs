using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detail : MonoBehaviour
{
    [SerializeField] protected Coating coating;
    [SerializeField] protected MeshRenderer meshRenderer;
    // TEST
    [SerializeField] private List<Coating> coatings;
    // TEST


    // TEST
    private void Awake()
    {
        coating = coatings.FirstOrDefault(x => x.Material.name == meshRenderer.material.name);
        //coating.Material = meshRenderer.material;
    }
    // TEST


    public virtual float Area
    {
        get;
    }
    public Coating Coating
    {
        get
        {
            if (coating != null)
            {
                return coating;
            }
            else
            {
                return coatings.FirstOrDefault(x => x.Material.name == meshRenderer.material.name);
            }
        }
        set
        {
            coating = value;
            meshRenderer.material = coating.Material;
        }
    }
    public MeshRenderer MeshRenderer { get => meshRenderer; set => meshRenderer = value; }
}
