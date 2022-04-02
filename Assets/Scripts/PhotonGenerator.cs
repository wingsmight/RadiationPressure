﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotonGenerator : MonoBehaviour
{
    [SerializeField] private ObjectPooler photonPooler;
    [SerializeField] private ObjectPooler shaderPhotonPooler;
    [SerializeField] private Transform satellite;
    [Space(12)]
    [SerializeField] private float startEnergy;
    [SerializeField] private PooledObject[] pooledPhotonObjects = new PooledObject[0];


    public static Vector3 radiatoinForce = Vector3.zero;


    public IEnumerator ThrowRoutine(bool isShader)
    {
        radiatoinForce = Vector3.zero;
        RaycastReflectionPhoton.caughtPhtotonCount = 0;

        var photonPooler = isShader ? this.shaderPhotonPooler : this.photonPooler;

        pooledPhotonObjects = new PooledObject[photonPooler.Capacity];
        var primaryPhotonCount = photonPooler.Capacity;

        int i = 0;
        if (isShader)
        {
            if ((shaderPhotonPooler as DensityPool).Density * 10.0f >= 2.9f)
            {
                i = 800;
                primaryPhotonCount = 1150;
            }
            else if ((shaderPhotonPooler as DensityPool).Density * 10.0f >= 1.9f)
            {
                i = 380;
                primaryPhotonCount = 510;
            }

            for (int j = 0; j < i; j++)
            {
                var pooledPhotonObject = photonPooler.Pull();
            }
        }

        for (; i < primaryPhotonCount; i++)
        {
            var pooledPhotonObject = photonPooler.Pull();
            pooledPhotonObjects[i] = pooledPhotonObject;
            pooledPhotonObject.GameObject.SetActive(true);
            if (isShader)
            {
                RaycastReflectionPhoton1 photon = pooledPhotonObject.GameObject.GetComponent<RaycastReflectionPhoton1>();
                photon.Throw(pooledPhotonObject.GameObject.transform.position, transform.forward, startEnergy);
            }
            else
            {
                RaycastReflectionPhoton photon = pooledPhotonObject.GameObject.GetComponent<RaycastReflectionPhoton>();
                photon.Throw(pooledPhotonObject.GameObject.transform.position, transform.forward, startEnergy);
            }

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
