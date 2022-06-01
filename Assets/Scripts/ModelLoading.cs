using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelLoading : MonoBehaviour
{
    private const string PATH = "Satellites";


    [SerializeField] private SunObjectEarthSystem sunObjectEarthSystem;


    public static string lastSatelliteName;


    private void Start()
    {
        LoadModel();
    }


    private void LoadModel()
    {
        var model = Resources.Load<GameObject>($"{PATH}/{lastSatelliteName}");
        if (model != null)
        {
            GameObject loadedeSatellite = Instantiate(model, Vector3.zero, Quaternion.identity, this.transform);
            if (lastSatelliteName.Contains("Спутник"))
            {
                loadedeSatellite.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }

            sunObjectEarthSystem.Satellite = model;
        }
    }
}
