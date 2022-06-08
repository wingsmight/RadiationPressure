using UnityEngine;

public class SatelliteRotater : MonoBehaviour
{
    [SerializeField] private Orbit orbit;
    [Space]
    // Расстояние за кадр
    [SerializeField] private float speed;


    private float angle = 0.0f;


    private void Update()
    {
        angle += speed * Time.deltaTime;
        orbit.PlaceSatellite(angle);
    }
}
