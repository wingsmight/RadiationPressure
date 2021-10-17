using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ObjectPooler : MonoBehaviour
{
    [SerializeField] protected int capacity;
    [SerializeField] protected bool canExpand = true;
    [SerializeField] protected GameObject pooledObject;


    protected List<PooledObject> pooledObjects = new List<PooledObject>();

    private int firstAccessibleObjectIndex;


    protected virtual void Awake()
    {
        pooledObjects = new List<PooledObject>(capacity);

        for (int i = 0; i < capacity; i++)
        {
            pooledObjects.Add(CreateObject());
        }
    }


    public PooledObject Pull()
    {
        if (firstAccessibleObjectIndex < capacity)
        {
            pooledObjects[firstAccessibleObjectIndex].IsFree = false;

            return pooledObjects[firstAccessibleObjectIndex++];
        }

        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].IsFree)
            {
                return pooledObjects[i];
            }
        }

        if (canExpand)
        {
            var newObject = CreateObject();
            pooledObjects.Add(newObject);

            return newObject;
        }

        return null;
    }
    public void Push(PooledObject pooledObject)
    {
        pooledObject.GameObject.SetActive(false);
        pooledObject.IsFree = false;
    }

    protected virtual PooledObject CreateObject()
    {
        GameObject obj = Instantiate(pooledObject, this.transform);
        obj.SetActive(false);

        return new PooledObject(obj);
    }
    protected void Clean()
    {
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            Destroy(pooledObjects[i].GameObject);
        }
        firstAccessibleObjectIndex = 0;
    }


    public int Capacity => capacity;
}

public class PooledObject
{
    private bool isFree;
    private GameObject gameObject;


    public PooledObject(GameObject gameObject)
    {
        this.isFree = true;
        this.gameObject = gameObject;
    }


    public bool IsFree { get => isFree; set => isFree = value; }
    public GameObject GameObject => gameObject;
}