using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelLoading : MonoBehaviour
{
    private const string PATH = "Satellites";
    private const string DEFAULT_SATELLITE_NAME = "Спутник 14Ф143";


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
            model = Instantiate(model, this.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            if (lastSatelliteName.Contains(DEFAULT_SATELLITE_NAME))
            {
                model.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }

            //TODO
            if (sunObjectEarthSystem != null)
            {
                sunObjectEarthSystem.Satellite = model;
            }
        }
    }
}
