using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ObjectPooler : MonoBehaviour
{
    [SerializeField] protected int capacity;
    [SerializeField] protected bool canExpand = true;
    [SerializeField] protected GameObject pooledObject;


    protected List<PooledObject> pooledObjects = new List<PooledObject>();


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
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (pooledObjects[i].IsFree)
            {
                pooledObjects[i].IsFree = false;
                return pooledObjects[i];
            }
        }

        if (canExpand)
        {
            var newObject = CreateObject();
            pooledObjects.Add(newObject);

            capacity++;

            newObject.IsFree = false;
            return newObject;
        }

        return null;
    }
    public void Push(PooledObject pooledObject)
    {
        pooledObject.GameObject.SetActive(false);
        pooledObject.IsFree = true;
    }
    public void Push(GameObject pooledObject)
    {
        var pushedPooledObject = pooledObjects.Find(x => x.GameObject == pooledObject);

        if (pushedPooledObject != null)
        {
            Push(pushedPooledObject);
        }
    }

    protected virtual PooledObject CreateObject()
    {
        GameObject obj = Instantiate(pooledObject, this.transform);
        obj.SetActive(false);

        return new PooledObject(obj);
    }
    protected void Clear()
    {
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            Destroy(pooledObjects[i].GameObject);
        }
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