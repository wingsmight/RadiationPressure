using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RecursivelyLoadResources
{
    public T LoadRecursively<T>(string fileName, string path)
        where T : ScriptableObject
    {
        string[] foundFiles = Directory.GetFiles(path, fileName, SearchOption.AllDirectories);
        return Resources.Load<T>(path);
    }
}
