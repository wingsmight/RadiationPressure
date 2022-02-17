using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunObjectEarthSystem : MonoBehaviour
{
    [SerializeField] private GameObject sun;
    [SerializeField] private GameObject satellite;
    [SerializeField] private GameObject earth;


    private float satelliteSunDistance;
    private float satelliteEarthDistance;


    private void Awake()
    {
        satelliteSunDistance = Vector3.Distance(satellite.transform.position, sun.transform.position);
        satelliteEarthDistance = Vector3.Distance(satellite.transform.position, earth.transform.position);
    }
    private void Start()
    {
        sun.transform.LookAt(satellite.transform);
    }
    private void FixedUpdate()
    {
        //PlaceSatellite(satelliteEarthAngle);
        RotateSatellite();
        sun.transform.LookAt(satellite.transform);
    }


    public void PlaceSatellite(float satelliteEarthAngle)
    {
        satellite.transform.position = new Vector3(earth.transform.position.x + satelliteEarthDistance * Mathf.Cos(satelliteEarthAngle),
                                                    earth.transform.position.y + satelliteEarthDistance * Mathf.Sin(satelliteEarthAngle),
                                                    satellite.transform.position.z);
    }
    public void RotateSatellite()
    {
        satellite.transform.LookAt(sun.transform);
        satellite.transform.localEulerAngles += new Vector3(90, 0, 0);
    }

    private Vector3 FindPosition(Vector3 sun, Vector3 earth, float satelliteSunDistance, float satelliteEarthDistance, float angle)
    {
        float sunDotMultiplyEarth = satelliteSunDistance * satelliteEarthDistance * Mathf.Cos(angle);
        // Vector3 satelliteEarthVector = ;
        // Vector3 satelliteSunVector;
        // float positionZ = earth.z;
        // float positionY;
        // float positionX;
        //positionX * earth.x + positionY * earth.y = aMuliplyB;
        //positionX * positionX + positionY * positionY + positionZ * positionZ;

        return Vector3.zero;
    }
}
