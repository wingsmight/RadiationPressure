#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;
using Battlehub.RTSL;

namespace Battlehub.RTEditor
{
    public static class RTEditorMenu
    {           
        [MenuItem("Tools/Runtime Editor/Create Editor", priority = 0)]
        public static void CreateRuntimeEditor()
        {
            Undo.RegisterCreatedObjectUndo(InstantiateRuntimeEditor(), "Battlehub.RTEditor.Create");

            EventSystem eventSystem = UnityObject.FindObjectOfType<EventSystem>();
            if (!eventSystem)
            {
                GameObject es = new GameObject();
                eventSystem = es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                es.name = "EventSystem";
            }

            eventSystem.gameObject.AddComponent<RTSLIgnore>();

            GameObject camera = GameObject.Find("Main Camera");
            if(camera != null)
            {
                if(camera.GetComponent<GameViewCamera>() == null)
                {
                    if(EditorUtility.DisplayDialog("Main Camera setup.", "Do you want to add Game View Camera script to Main Camera and render it to Runtime Editors's Game view?", "Yes", "No"))
                    {
                        Undo.AddComponent<GameViewCamera>(camera.gameObject);
                    }
                }
            }
        }

        public static GameObject InstantiateRuntimeEditor()
        {
            UnityObject prefab = AssetDatabase.LoadAssetAtPath(BHRoot.PackageRuntimeContentPath + "/RTEditor/Prefabs/RuntimeEditor.prefab" , typeof(GameObject));
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        [MenuItem("Tools/Runtime Editor/Custom Window/Create Prefab")]
        public static void CreateCustomWindowPrefab()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create View", "CustomWindow", "prefab", "Create Window");
            if(string.IsNullOrEmpty(path))
            {
                return;
            }
            AssetDatabase.CopyAsset(BHRoot.PackageRuntimeContentPath + "/RTEditor/Prefabs/Views/Resources/TemplateWindow.prefab", path);
        }

        [MenuItem("Tools/Runtime Editor/Custom Window/Create Script")]
        public static void CreateCustomWindowScript()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create View", "CustomWindow", "cs", "Create Window");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            string fileName = Path.GetFileNameWithoutExtension(path);

            CreateScriptFromTemplate action = ScriptableObject.CreateInstance<CreateScriptFromTemplate>();
            action.AssetTemplate = BHRoot.PackageRuntimeContentPath + "/RTEditor/Prefabs/Views/Resources/TemplateWindow.cs.txt";
            path = Path.Combine(Path.GetDirectoryName(path), fileName + ".cs").Replace('\\', '/');
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0, action, path,
                EditorGUIUtility.FindTexture("cs Script Icon"), null);

        }

        [MenuItem("Tools/Runtime Editor/Custom Window/Create Registration Script")]
        public static void CreateCustomRegistrationScript()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create View", "RegisterCustomWindow", "cs", "Create Window Registration Script");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            string fileName = Path.GetFileNameWithoutExtension(path);

            CreateScriptFromTemplate action = ScriptableObject.CreateInstance<CreateScriptFromTemplate>();
            action.AssetTemplate = BHRoot.PackageRuntimeContentPath + "/RTEditor/Prefabs/Views/Resources/RegisterTemplateWindow.cs.txt";
            action.Replacements.Add("#WINDOWNAME#", fileName.Replace("Register", ""));
            path = Path.Combine(Path.GetDirectoryName(path), fileName + ".cs").Replace('\\', '/');
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0, action, path,
                EditorGUIUtility.FindTexture("cs Script Icon"), null);
        }


        private class CreateScriptFromTemplate : EndNameEditAction
        {
            public string AssetTemplate;

            public readonly Dictionary<string, string> Replacements = new Dictionary<string, string>();

            public override void Action(int instanceId, string pathName,
                string resourceFile)
            {
                if (AssetDatabase.CopyAsset(AssetTemplate, pathName))
                {
                    string scriptName = Path.GetFileNameWithoutExtension(pathName);
                    string contents = File.ReadAllText(pathName);
                    contents = contents.Replace("#SCRIPTNAME#", scriptName);
                    foreach(var kvp in Replacements)
                    {
                        contents = contents.Replace(kvp.Key, kvp.Value);
                    }

                    File.WriteAllText(pathName, contents);
                }
            }
        }
    }
}
#endif