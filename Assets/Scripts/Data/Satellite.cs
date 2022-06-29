using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Satellite0", menuName = "Satellites/Satellite")]
public class Satellite : ScriptableObject
{
    [SerializeField] private new string name;
    [SerializeField] private GameObject model;


    public void Save()
    {
#if UNITY_EDITOR
        PrefabUtility.SaveAsPrefabAsset(model, name);
#endif
    }


    public string Name => name;
    public GameObject Model => model;
}
