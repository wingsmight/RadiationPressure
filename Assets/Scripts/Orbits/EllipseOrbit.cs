using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EllipseOrbit : Orbit
{
    [SerializeField] private Ellipse path;


    public override void PlaceSatellite(float satelliteEarthAngle)
    {
        var orbitProgress = satelliteEarthAngle / 360.0f;
        Vector2 orbitPos = path.Evaluate(orbitProgress);
        satellite.transform.localPosition = new Vector3(orbitPos.x, 0, orbitPos.y);
    }


    public Ellipse Path => path;
}
