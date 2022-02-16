using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using System.Collections;
using UnityEngine;

namespace Battlehub.RTSL.Demo
{
    public class MyScriptabeObject : ScriptableObject
    {
        public string Data;
    }

    public class SetGetValueUsageExample : MonoBehaviour
    {
        IEnumerator Start()
        {
            IProject project = IOC.Resolve<IProject>();
            yield return project.OpenProject("SetGetValueUsageExample");

            MyScriptabeObject obj = ScriptableObject.CreateInstance<MyScriptabeObject>();
            obj.Data = "Data";
            yield return project.SetValue("MyMaterialKey", obj);
            Destroy(obj);

            ProjectAsyncOperation<MyScriptabeObject> getAo = project.GetValue<MyScriptabeObject>("MyMaterialKey");
            yield return getAo;
            if(getAo.HasError)
            {
                Debug.LogError(getAo.Error);
                yield break;
            }

            Debug.Log("Loaded Object: " + getAo.Result.Data);
        }
    }
}

