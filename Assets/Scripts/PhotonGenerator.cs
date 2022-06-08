using System.Collections;
using UnityEngine;

public class PhotonGenerator : MonoBehaviour
{
    // Пул фотонов
    [SerializeField] private ObjectPooler photonPooler;
    [Space(12)]
    // Начальная энергия фотонов
    [SerializeField] private float startEnergy;
    [SerializeField] private PooledObject[] pooledPhotonObjects = new PooledObject[0];


    public static Vector3 radiatoinForce = Vector3.zero;


    public IEnumerator ThrowRoutine(bool isShader)
    {
        radiatoinForce = Vector3.zero;
        RaycastReflectionPhoton.caughtPhotonCount = 0;

        pooledPhotonObjects = new PooledObject[photonPooler.Capacity];
        var primaryPhotonCount = photonPooler.Capacity;

        int i = 0;

        print($"primaryPhotonCount = {primaryPhotonCount}");
        print($"photonPooler.Capacity = {photonPooler.Capacity}");
        for (; i < primaryPhotonCount; i++)
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
