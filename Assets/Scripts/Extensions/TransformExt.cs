using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class TransformExt
{
    public static List<T> GetComponentsOnlyInChildren<T>(this Transform transform) where T : class
    {
        List<T> group = new List<T>();

        //collect only if its an interface or a Component
        if (typeof(T).IsInterface
         || typeof(T).IsSubclassOf(typeof(Component))
         || typeof(T) == typeof(Component))
        {
            foreach (Transform child in transform)
            {
                group.AddRange(child.GetComponentsInChildren<T>());
            }
        }

        return group;
    }
    public static List<Transform> GetChildren(this Transform transform)
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in transform)
        {
            children.Add(child);
        }

        return children;
    }
    public static Transform DestroyChildren(this Transform transform)
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        return transform;
    }
}
