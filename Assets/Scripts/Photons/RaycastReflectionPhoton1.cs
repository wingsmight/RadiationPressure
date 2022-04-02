using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastReflectionPhoton1 : MonoBehaviour, IPhoton
{
    [SerializeField] private int maxReflectionsCount;
    [SerializeField] private float maxLength;
    [Space(12)]
    [SerializeField] private float startEnergy;
    [SerializeField] private float minEnergy;
    [SerializeField] private bool isMissedPhotonsShowing;


    private ObjectPooler photonPooler;
    private PooledObject pulledPhoton;


    private void Awake()
    {
        photonPooler = GameObject.Find("TEST_ONLY").GetComponent<ObjectPooler>();
    }
    private void OnEnable()
    {
        pulledPhoton = null;
    }
    private void OnDisable()
    {
        if (pulledPhoton != null)
        {
            photonPooler.Push(pulledPhoton);
        }
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
            yield break;

        Ray ray = new Ray(startPosition, direction);

        for (int i = 0; i < maxReflectionsCount; i++)
        {
            if (Physics.Raycast(ray.origin, ray.direction, out var hit))
            {
                var hitDetail = hit.transform.gameObject.GetComponent<Detail>();
                if (hitDetail != null)
                {
                    RaycastReflectionPhoton.caughtPhtotonCount++;

                    // reduce energy
                    energy /= 2.0f;

                    // calculate the force
                    PhotonGenerator.radiatoinForce += Formulas.RadiationForce(hit.normal, ray.direction, hitDetail.Coating.Coefficients);

                    // primary ray
                    ray = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));

                    if (energy < minEnergy)
                    {
                        yield break;
                    }
                    else
                    {
                        // secondary ray
                        float x = Random.Range(-1.0f, 1.0f);
                        float y = Random.Range(-1.0f, 1.0f);
                        float zLength = Random.Range(0.0f, 1.0f);
                        float z = (x * hit.normal.x + y * hit.normal.y) / hit.normal.z;
                        Vector3 perpendicularVector = new Vector3(x, y, z);
                        Vector3 diffuseDirection = perpendicularVector + (hit.normal * zLength);

                        pulledPhoton = photonPooler.Pull();
                        var secondaryPhoton = pulledPhoton.GameObject.GetComponent<RaycastReflectionPhoton1>();
                        secondaryPhoton.gameObject.SetActive(true);
                        secondaryPhoton.Throw(hit.point, diffuseDirection, energy);
                    }
                }
            }
        }
    }
}
