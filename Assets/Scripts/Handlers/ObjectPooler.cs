using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ObjectPooler : MonoBehaviour
{
	[SerializeField] protected int capacity;
	[SerializeField] protected int poolAmount;
	[SerializeField] protected bool canExpand = true;
	[SerializeField] protected GameObject itemToPool;

	protected List<GameObject> pooledObjects = new List<GameObject>();


	protected virtual void Awake()
	{
		pooledObjects = new List<GameObject>(capacity);

		for (int i = 0; i < poolAmount; i++)
		{
			pooledObjects.Add(CreateObject());
		}
	}


	public GameObject GetObject()
	{
		for (int i = 0; i < poolAmount; i++)
		{
			if (!pooledObjects[i].activeInHierarchy)
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
	public void ReturnObject(GameObject gameObject)
	{
		gameObject.SetActive(false);
	}

	protected virtual GameObject CreateObject()
	{
		GameObject obj = Instantiate(itemToPool, this.transform);
		obj.SetActive(false);

		return obj;
	}


	public int PoolAmount {get => poolAmount; set => poolAmount = value;}
}