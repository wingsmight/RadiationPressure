using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotonGenerator : MonoBehaviour
{
    [SerializeField] [RequireInterface(typeof(IPhoton))] private Object photonPrefab;
    [SerializeField] private ObjectPooler photonPooler;
    [Space(12)]
    [SerializeField] private float startEnergy;


    public static Vector3 radiatoinForce = Vector3.zero;


    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Throw();
        }
    }


    public void Throw()
    {
        radiatoinForce = Vector3.zero;
        RaycastReflectionPhoton.caughtPhtotonCount = 0;

        StartCoroutine(ThrowRoutine());
    }

    private IEnumerator ThrowRoutine()
    {
        for (int i = 0; i < photonPooler.Capacity; i++)
        {
            var pooledPhotonObject = photonPooler.Pull();
            pooledPhotonObject.GameObject.SetActive(true);
            IPhoton photon = pooledPhotonObject.GameObject.GetComponent<IPhoton>();
            photon.Throw(pooledPhotonObject.GameObject.transform.position, transform.forward, startEnergy);

            yield return new WaitForEndOfFrame();
        }
    }


    private IPhoton Photon => photonPrefab as IPhoton;
}
