using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RaycastReflectionPhoton : MonoBehaviour, IPhoton
{
    [SerializeField] private int maxReflectionsCount;
    [SerializeField] private float maxLength;
    [Space(12)]
    [SerializeField] private float startEnergy;
    [SerializeField] private float minEnergy;


    private LineRenderer lineRenderer;
    private ObjectPooler photonPooler;


    public static int caughtPhtotonCount = 0;


    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            gameObject.AddComponent<LineRenderer>();
        }

        photonPooler = GameObject.FindObjectOfType<ObjectPooler>();
    }


    public void ThrowForward()
    {
        Throw(transform.position, Vector3.forward, startEnergy);
    }
    public void Throw(Vector3 startPosition, Vector3 direction, float energy)
    {
        StartCoroutine(ThrowRoutine(startPosition, direction, energy));
    }

    private IEnumerator ThrowRoutine(Vector3 startPosition, Vector3 direction, float energy)
    {
        if (energy < minEnergy)
        {
            yield break;
        }

        Ray ray = new Ray(startPosition, direction);

        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPosition);

        float remainingLength = maxLength;

        for (int i = 0; i < maxReflectionsCount; i++)
        {
            if (Physics.Raycast(ray.origin, ray.direction, out var hit, remainingLength))
            {
                var hitDetail = hit.transform.gameObject.GetComponent<Detail>();
                if (hitDetail != null)
                {
                    caughtPhtotonCount++;

                    // reduce energy
                    energy /= 2.0f;

                    // calculate the force
                    PhotonGenerator.radiatoinForce += Formulas.RadiationForce(hit.normal, ray.direction, hitDetail.Coating.Coefficients);

                    // primary ray
                    lineRenderer.positionCount++;
                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, hit.point);
                    remainingLength -= Vector3.Distance(ray.origin, hit.point);
                    ray = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));

                    yield return new WaitForEndOfFrame();

                    // secondary ray
                    float x = Random.Range(-1.0f, 1.0f);
                    float y = Random.Range(-1.0f, 1.0f);
                    float zLength = Random.Range(0.0f, 1.0f);
                    float z = (x * hit.normal.x + y * hit.normal.y) / hit.normal.z;
                    Vector3 perpendicularVector = new Vector3(x, y, z);
                    Vector3 diffuseDirection = perpendicularVector + (hit.normal * zLength);

                    var secondaryPhoton = photonPooler.Pull().GameObject.GetComponent<RaycastReflectionPhoton>();
                    secondaryPhoton.gameObject.SetActive(true);
                    secondaryPhoton.Throw(hit.point, diffuseDirection, energy);
                }
            }
        }

        if (lineRenderer.positionCount == 1)
        {
            //photonPooler.Push(this.gameObject);
        }
    }
}
