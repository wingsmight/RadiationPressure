using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DetailLoading
{
    private const string PATH = "Details";


    public static GameObject Load(string name)
    {
        var detial = Resources.Load<GameObject>($"{PATH}/{name}");
        return GameObject.Instantiate(detial, Vector3.zero, Quaternion.identity);
    }
}