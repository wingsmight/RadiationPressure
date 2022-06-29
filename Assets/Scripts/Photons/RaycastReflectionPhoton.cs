using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RaycastReflectionPhoton : MonoBehaviour, IPhoton
{
    // Максимальное количество переотражений фотона
    [SerializeField] private int maxReflectionsCount;
    // Максимальная длина полета фотона
    [SerializeField] private float maxLength;
    [Space(12)]
    // Начальная энергия фотона
    [SerializeField] private float startEnergy;
    // Минимальная энергия фотона, при котором он уничтожается
    [SerializeField] private float minEnergy;
    // Отображать фотон, не попавший на корпус КА
    [SerializeField] private bool isMissedPhotonShowing;


    private LineRenderer lineRenderer;
    private ObjectPooler photonPooler;
    private List<PooledObject> pulledPhotons = new List<PooledObject>();


    public static int caughtPhotonCount = 0;


    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            gameObject.AddComponent<LineRenderer>();
        }

        photonPooler = GameObject.Find("SecondaryPhotonPool").GetComponent<ObjectPooler>();
    }
    private void OnEnable()
    {
        pulledPhotons.Clear();
    }
    // Эта функция вызывается, когда поведение становится отключенным.
    private void OnDisable()
    {
        pulledPhotons.ForEach(pulledPhoton => photonPooler.Push(pulledPhoton));
    }


    // Запустить фотонов вперед
    public void ThrowForward()
    {
        Throw(transform.position, Vector3.forward, startEnergy);
    }
    // Запустить фотон по заданному направлению от указанной позиции
    public void Throw(Vector3 startPosition, Vector3 direction, float energy)
    {
        StartCoroutine(ThrowRoutine(startPosition, direction, energy));
    }

    private IEnumerator ThrowRoutine(Vector3 startPosition, Vector3 direction, float energy)
    {
        if (energy < minEnergy)
            yield break;

        Ray ray = new Ray(startPosition, direction);

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPosition);

        for (int i = 0; i < maxReflectionsCount; i++)
        {
            // Пуск луча
            if (Physics.Raycast(ray.origin, ray.direction, out var hit))
            {
                var hitDetail = hit.transform.gameObject.GetComponent<Detail>();
                // Если попали в деталь КА
                if (hitDetail != null)
                {
                    caughtPhotonCount++;

                    // Уменьшение энергии
                    energy /= 2.0f;

                    // Вычисление силы
                    PhotonGenerator.radiatoinForce += Formulas.RadiationForce(hit.normal, ray.direction, hitDetail.Coating.Coefficients);

                    // Первичный луч
                    lineRenderer.positionCount++;
                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, hit.point);
                    ray = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));

                    yield return new WaitForEndOfFrame();
                    if (energy < minEnergy)
                    {
                        yield break;
                    }
                    else
                    {
                        // Вторичный луч
                        float x = Random.Range(-1.0f, 1.0f);
                        float y = Random.Range(-1.0f, 1.0f);
                        float zLength = Random.Range(0.0f, 1.0f);
                        float z = (x * hit.normal.x + y * hit.normal.y) / hit.normal.z;
                        Vector3 perpendicularVector = new Vector3(x, y, z);
                        Vector3 diffuseDirection = perpendicularVector + (hit.normal * zLength);

                        var pulledPhoton = photonPooler.Pull();
                        pulledPhotons.Add(pulledPhoton);
                        var secondaryPhoton = pulledPhoton.GameObject.GetComponent<RaycastReflectionPhoton>();
                        secondaryPhoton.gameObject.SetActive(true);
                        secondaryPhoton.Throw(hit.point, diffuseDirection, energy);

                        yield return new WaitForEndOfFrame();
                    }
                }
            }
            else if (isMissedPhotonShowing)
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1,
                    lineRenderer.GetPosition(lineRenderer.positionCount - 1) + ray.direction * maxLength / 2.0f);

                yield break;
            }
        }
    }
}
