using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunObjectEarthSystem : MonoBehaviour
{
    [SerializeField] private List<GameObject> suns;
    [SerializeField] private GameObject satellite;
    [SerializeField] private GameObject earth;


    private float satelliteSunDistance;
    private float satelliteEarthDistance;


    private void Awake()
    {
        satelliteSunDistance = Vector3.Distance(satellite.transform.position, suns[0].transform.position);
        satelliteEarthDistance = Vector3.Distance(satellite.transform.position, earth.transform.position);
    }
    private void FixedUpdate()
    {
        RotateSatellite();
        suns.ForEach(x => x.transform.LookAt(satellite.transform));
    }


    public void PlaceSatellite(float satelliteEarthAngle)
    {
        satellite.transform.position = new Vector3(earth.transform.position.x + satelliteEarthDistance * Mathf.Cos(satelliteEarthAngle),
                                                    earth.transform.position.y + satelliteEarthDistance * Mathf.Sin(satelliteEarthAngle),
                                                    satellite.transform.position.z);
    }

    private void RotateSatellite()
    {
        satellite.transform.LookAt(suns[0].transform);
        satellite.transform.localEulerAngles += new Vector3(90, 0, 0);
    }
}
