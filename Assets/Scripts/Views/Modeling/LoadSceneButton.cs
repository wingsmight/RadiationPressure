using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneButton : UIButton
{
    [SerializeField] private string sceneName;


    protected override void OnClick()
    {
        SceneManager.LoadScene(sceneName);

        PrefabRuntime.CreatePrefab();
    }
}
