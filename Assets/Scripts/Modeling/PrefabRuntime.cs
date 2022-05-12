using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class PrefabRuntime : MonoBehaviour
{
    [SerializeField] private GameObject savedObject;


    public static void CreatePrefab()
    {
        var gameObjects = new GameObject[10];
        for (int i = 0; i < gameObjects.Length; i++)
        {
            gameObjects[i] = GameObject.Find("Satellite" + i);
        }

        foreach (var gameObject in gameObjects)
        {
            if (gameObject != null)
            {
                // Create folder Prefabs and set the path as within the Prefabs folder,
                // and name it as the GameObject's name with the .Prefab format
                var rootPath = "Assets/Resources/Satellites/";
                string localPath = rootPath + gameObject.name + ".prefab";
                ResourceModelLoading.lastSatelliteName = gameObject.name;

                // Make sure the file name is unique, in case an existing Prefab has the same name.
                localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

                // Create the new Prefab and log whether Prefab was saved successfully.
                bool prefabSuccess;
                PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, localPath, InteractionMode.UserAction, out prefabSuccess);
                if (prefabSuccess == true)
                    Debug.Log("Prefab was saved successfully");
                else
                    Debug.Log("Prefab failed to save" + prefabSuccess);
            }
        }
    }
}
