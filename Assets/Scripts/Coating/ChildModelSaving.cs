using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildModelSaving : ModelSaving
{
    [SerializeField] private GameObject savedObjectParent;


    public override void Save()
    {
        var savedObject = savedObjectParent.transform.GetChild(0).gameObject;
        SaveToPrefab(savedObject, PATH);
    }
}
