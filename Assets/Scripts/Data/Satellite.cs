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
        PrefabUtility.SaveAsPrefabAsset(model, name);
    }


    public string Name => name;
    public GameObject Model => model;
}
