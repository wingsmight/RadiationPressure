using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SatelliteRotater : MonoBehaviour
{
    [SerializeField] private Orbit orbit;
    [Space]
    [SerializeField] private float speed;


    private float angle = 0.0f;


    private void Update()
    {
        angle += speed * Time.deltaTime;
        orbit.PlaceSatellite(angle);
    }
}
