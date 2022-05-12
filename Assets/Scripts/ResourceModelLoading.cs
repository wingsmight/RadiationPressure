using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceModelLoading : MonoBehaviour
{
    public static string lastSatelliteName;


    private void Start()
    {
        LoadModel();
    }


    private void LoadModel()
    {
        var model = Resources.Load<GameObject>($"Satellites/{lastSatelliteName}");
        if (model != null)
        {
        Instantiate(model, Vector3.zero, Quaternion.identity, this.transform);
        }
    }
}
