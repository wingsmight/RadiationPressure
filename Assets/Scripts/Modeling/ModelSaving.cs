using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ModelSaving : MonoBehaviour
{
    protected const string PATH = "Assets/Resources/Satellites/";


    public virtual void Save()
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var savedObject = activeScene.GetRootGameObjects()[activeScene.rootCount - 1];

        if (IsModelExisted(savedObject))
        {
            SaveToPrefab(savedObject, PATH);
        }
    }

    protected void SaveToPrefab(GameObject savedObject, string path)
    {
        if (savedObject != null)
        {
            // Create folder Prefabs and set the path as within the Prefabs folder,
            // and name it as the GameObject's name with the .Prefab format
            string localPath = path + savedObject.name + ".prefab";
            ModelLoading.lastSatelliteName = savedObject.name;

            // Make sure the file name is unique, in case an existing Prefab has the same name.
            //localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

#if UNITY_EDITOR
            // Create the new Prefab and log whether Prefab was saved successfully.
            bool prefabSuccess;
            PrefabUtility.SaveAsPrefabAssetAndConnect(savedObject, localPath, InteractionMode.UserAction, out prefabSuccess);
            if (prefabSuccess == true)
                Debug.Log("Prefab was saved successfully");
            else
                Debug.Log("Prefab failed to save" + prefabSuccess);
#endif
        }
    }
    protected bool IsModelExisted(GameObject model)
    {
        return model != gameObject;
    }
}
