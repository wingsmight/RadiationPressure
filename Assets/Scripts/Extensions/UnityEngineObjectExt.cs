using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UnityEngineObjectExt
{
    public static List<T> FindObjectsOfInterface<T>()
    {
        var interfaces = new List<T>();
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var rootGameObjects = SceneManager.GetSceneAt(i).GetRootGameObjects();
            foreach (var rootGameObject in rootGameObjects)
            {
                interfaces.AddRange(rootGameObject.GetComponentsInChildren<T>(true));
            }
        }

        return interfaces;
    }
}
