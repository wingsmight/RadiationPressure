using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orbit : MonoBehaviour
{
    [SerializeField] private GameObject satellite;
    [SerializeField] private Circle path;
    [Space]
    [SerializeField] private float radius;
    [SerializeField] private int circleStepCount = 100;


    private void Awake()
    {
        satellite.transform.localPosition = new Vector3(radius, 0, 0);
    }
    private void Update()
    {
        path.Draw(radius, circleStepCount);
    }


    public void PlaceSatellite(float satelliteEarthAngle)
    {
        satellite.transform.localPosition = new Vector3(radius * Mathf.Cos(satelliteEarthAngle),
                                                        0,
                                                        radius * Mathf.Sin(satelliteEarthAngle));
    }
}
