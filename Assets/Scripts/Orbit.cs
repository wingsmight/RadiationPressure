using UnityEngine;

public class Orbit : MonoBehaviour
{
    // Физический объект: спутник
    [SerializeField] private GameObject satellite;
    // Круг, задающий орбиту
    [SerializeField] private Circle path;
    [Space]
    // Радиус орбиты
    [SerializeField] private float radius;
    // Плавность отрисовки орбиты
    [SerializeField] private int circleStepCount = 100;


    // Функция вызывается когда экземпляр скрипта будет загружен
    private void Awake()
    {
        satellite.transform.localPosition = new Vector3(radius, 0, 0);
    }
    // Функция, вызывающаяся каждый кадр
    private void Update()
    {
        path.Draw(radius, circleStepCount);
    }


    // Функция помещения спутника относительно угла между спутником и центром Земли
    public void PlaceSatellite(float satelliteEarthAngle)
    {
        satellite.transform.localPosition = new Vector3(radius * Mathf.Cos(satelliteEarthAngle),
                                                        0,
                                                        radius * Mathf.Sin(satelliteEarthAngle));
    }
}
