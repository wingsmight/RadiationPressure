using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetObjectPooler : ObjectPooler
{
    [SerializeField] private int dimension = 3;
    [SerializeField] private Vector3 betweenOffset;


    private Vector3 objectSize;


    protected override void Awake()
    {
        base.Awake();

        float cubeWidth = Mathf.CeilToInt(Mathf.Pow(capacity, 1.0f / dimension));
        Vector3 objectSize = new Vector3(pooledObject.transform.localScale.x, pooledObject.transform.localScale.y, pooledObject.transform.localScale.z);
        objectSize += betweenOffset / 2;
        Vector3 minPoint = (cubeWidth / 2) * new Vector3(objectSize.x, objectSize.y, objectSize.z) * -1;
        int objectIndex = 0;
        for (int z = 0; z < cubeWidth; z++)
        {
            for (int y = 0; y < cubeWidth; y++)
            {
                for (int x = 0; x < cubeWidth; x++)
                {
                    if (objectIndex < pooledObjects.Count)
                    {
                        pooledObjects[objectIndex++].GameObject.transform.localPosition = new Vector3(x * objectSize.x, y * objectSize.y, z * objectSize.z) + minPoint;
                    }
                }
            }
        }
    }


    public Vector3 BetweenOffset => betweenOffset;
}
