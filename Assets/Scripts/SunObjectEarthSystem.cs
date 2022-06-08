using UnityEngine;

public class SunObjectEarthSystem : MonoBehaviour
{
    [SerializeField] private GameObject sun;
    [SerializeField] private GameObject satellite;
    [SerializeField] private GameObject earth;


    private float satelliteSunDistance;


    private void Awake()
    {
        satelliteSunDistance = Vector3.Distance(satellite.transform.position, sun.transform.position);
    }
    private void FixedUpdate()
    {
        RotateSatellite();
        sun.transform.LookAt(satellite.transform);
    }


    // Поворот спутника (его солнечных батарей) в сторону Солнца
    private void RotateSatellite()
    {
        satellite.transform.LookAt(sun.transform);
        satellite.transform.localEulerAngles += new Vector3(90, 0, 0);
    }


    public GameObject Satellite
    {
        get
        {
            return satellite;
        }
        set
        {
            satellite = value;

            Awake();
        }
    }
}
