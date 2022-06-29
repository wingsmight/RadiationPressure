using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleOrbit : Orbit
{
    [SerializeField] private Circle path;


    public override void PlaceSatellite(float satelliteEarthAngle)
    {
        satellite.transform.localPosition = new Vector3(path.Radius * Mathf.Cos(satelliteEarthAngle),
                                                        0,
                                                        path.Radius * Mathf.Sin(satelliteEarthAngle));
    }


    public Circle Path => path;
}
