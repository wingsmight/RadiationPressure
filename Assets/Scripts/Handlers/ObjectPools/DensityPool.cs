using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityPool : ObjectPooler
{
    [SerializeField] private int dimension = 3;
    [SerializeField] private float cubeWidth;
    [SerializeField] private Vector3 betweenOffset;


    private Vector3 objectSize;
    private float density = 0.1f;


    protected override void Awake()
    {
        var betweenOffset = this.betweenOffset;
        betweenOffset /= density;
        float realCubeWidth = 0.0f;

        this.capacity = 1;
        if (dimension >= 1)
        {
            this.capacity *= (int)((cubeWidth / betweenOffset.x));
            realCubeWidth = capacity;
        }
        if (dimension >= 2)
        {
            this.capacity *= (int)((cubeWidth / betweenOffset.y));
        }
        else if (dimension >= 3)
        {
            this.capacity *= (int)((cubeWidth / betweenOffset.z));
        }

        base.Awake();

        Vector3 objectSize = new Vector3(pooledObject.transform.localScale.x, pooledObject.transform.localScale.y, pooledObject.transform.localScale.z);
        objectSize += betweenOffset / 2.0f;
        Vector3 minPoint = (realCubeWidth / dimension) * new Vector3(objectSize.x, objectSize.y, objectSize.z) * -1;
        int objectIndex = 0;
        for (int z = 0; z < realCubeWidth; z++)
        {
            for (int y = 0; y < realCubeWidth; y++)
            {
                for (int x = 0; x < realCubeWidth; x++)
                {
                    if (objectIndex < pooledObjects.Count)
                    {
                        pooledObjects[objectIndex++].GameObject.transform.localPosition = new Vector3(x * objectSize.x, y * objectSize.y, z * objectSize.z) + minPoint;
                    }
                }
            }
        }
    }


    public void Init(float density)
    {
        this.density = density / 10.0f;

        Awake();
    }


    public Vector3 BetweenOffset => betweenOffset;
}
