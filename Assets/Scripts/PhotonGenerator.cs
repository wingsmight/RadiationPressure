using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotonGenerator : MonoBehaviour
{
    [SerializeField] private ObjectPooler photonPooler;
    [SerializeField] private Transform satellite;
    [Space(12)]
    [SerializeField] private float startEnergy;
    [SerializeField] private PooledObject[] pooledPhotonObjects = new PooledObject[0];


    public static Vector3 radiatoinForce = Vector3.zero;


    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Throw();
        }
    }


    public IEnumerator Throw()
    {
        radiatoinForce = Vector3.zero;
        RaycastReflectionPhoton.caughtPhtotonCount = 0;

        pooledPhotonObjects = new PooledObject[photonPooler.Capacity];
        var primaryPhotonCount = photonPooler.Capacity;

        for (int i = 0; i < primaryPhotonCount; i++)
        {
            var pooledPhotonObject = photonPooler.Pull();
            pooledPhotonObjects[i] = pooledPhotonObject;
            pooledPhotonObject.GameObject.SetActive(true);
            RaycastReflectionPhoton photon = pooledPhotonObject.GameObject.GetComponent<RaycastReflectionPhoton>();
            photon.Throw(pooledPhotonObject.GameObject.transform.position, transform.forward, startEnergy);

            yield return new WaitForEndOfFrame();
        }
    }
    public void Clear()
    {
        for (int i = 0; i < pooledPhotonObjects.Length; i++)
        {
            photonPooler.Push(pooledPhotonObjects[i]);
        }

        pooledPhotonObjects = new PooledObject[0];

        transform.SetAllChildrenActive(false);
    }
}
