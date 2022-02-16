using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.RTSL.Interface;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using System;

using UnityObject = UnityEngine.Object;
using System.Collections.Generic;
using Battlehub.Utils;
#if UNITY_STANDALONE
using Battlehub.CodeAnalysis;
#endif

namespace Battlehub.RTScripting
{
    public interface IRuntimeScriptManager
    {
        event Action Loading;
        event Action Loaded;
        event Action Compiling;
        event Action<bool> Complied;

        bool IsLoaded
        {
            get;
        }


        string Ext
        {
            get;
        }

        void AddReference(string assemblyLocation);
        void CreateScript(ProjectItem folder);
        ProjectAsyncOperation<RuntimeTextAsset> LoadScript(AssetItem assetItem);
        ProjectAsyncOperation SaveScript(AssetItem assetItem, RuntimeTextAsset script);
        ProjectAsyncOperation Compile();
    }

    [Serializable]
    public class RuntimeTypeGuid
    {
        public string FullName;
        public string Guid;
    }

    [Serializable]
    public class RuntimeTypeGuids
    {
        public RuntimeTypeGuid[] Guids;
    }


    [DefaultExecutionOrder(-1)]
    public class RuntimeScriptsManager : MonoBehaviour, IRuntimeScriptManager
    {
        public event Action Loading;
        public event Action Loaded;
        public event Action Compiling;
        public event Action<bool> Complied;

        public bool IsLoaded
        {
            get;
            private set;
        }

        private static object m_syncRoot = new object();
        private const string RuntimeAssemblyKey = "RuntimeAssembly";
        private const string RuntimeTypeGuids = "RuntimeTypeGuids";
        public string Ext
        {
            get { return ".cs"; }
        }

        private IProject m_project;
        private IEditorsMap m_editorsMap;
        private ITypeMap m_typeMap;
        private ILocalization m_localization;
        private Assembly m_runtimeAssembly;
        private Dictionary<string, Guid> m_typeNameToGuid;
        private RuntimeTextAsset m_runtimeTypeGuidsAsset;
        private HashSet<string> m_assemblyReferences = new HashSet<string>();
        
        private void Awake()
        {
            m_localization = IOC.Resolve<ILocalization>();
            m_project = IOC.Resolve<IProject>();
            m_project.OpenProjectCompleted += OnProjectOpened;
            m_project.DeleteCompleted += OnDeleteProjectItemCompleted;
            m_editorsMap = IOC.Resolve<IEditorsMap>();
            m_typeMap = IOC.Resolve<ITypeMap>();
            IOC.RegisterFallback<IRuntimeScriptManager>(this);

            if(m_project.IsOpened)
            {
                StartCoroutine(CoLoad());
            }
        }

        private void OnDestroy()
        {
            if (m_project != null)
            {
                m_project.OpenProjectCompleted -= OnProjectOpened;
                m_project.DeleteCompleted -= OnDeleteProjectItemCompleted;
            }
            IOC.UnregisterFallback<IRuntimeScriptManager>(this);
            UnloadTypes(false);
        }

        private void OnProjectOpened(Error error, ProjectInfo result)
        {
            StartCoroutine(CoLoad());
        }

        private IEnumerator CoLoad()
        {
            IsLoaded = false;

            if (Loading != null)
            {
                Loading();
            }

            ProjectAsyncOperation<RuntimeBinaryAsset> getAssemblyAo = m_project.GetValue<RuntimeBinaryAsset>(RuntimeAssemblyKey);
            yield return getAssemblyAo;
            if (getAssemblyAo.HasError)
            {
                if (getAssemblyAo.Error.ErrorCode != Error.E_NotFound)
                {
                    Debug.LogError(getAssemblyAo.Error);
                }
                else
                {
                    m_typeNameToGuid = new Dictionary<string, Guid>();
                    m_runtimeTypeGuidsAsset = ScriptableObject.CreateInstance<RuntimeTextAsset>();
                }
            }
            else
            {
                ProjectAsyncOperation<RuntimeTextAsset> getGuidsAo = m_project.GetValue<RuntimeTextAsset>(RuntimeTypeGuids);
                yield return getGuidsAo;
                if (getGuidsAo.HasError)
                {
                    Debug.LogError(getGuidsAo.Error);
                }
                else
                {
                    m_typeNameToGuid = new Dictionary<string, Guid>();
                    m_runtimeTypeGuidsAsset = getGuidsAo.Result;

                    string xml = m_runtimeTypeGuidsAsset.Text;
                    if (!string.IsNullOrEmpty(xml))
                    {
                        RuntimeTypeGuids typeGuids = XmlUtility.FromXml<RuntimeTypeGuids>(xml);
                        foreach (RuntimeTypeGuid typeGuid in typeGuids.Guids)
                        {
                            Guid guid;
                            if (!m_typeNameToGuid.ContainsKey(typeGuid.FullName) && Guid.TryParse(typeGuid.Guid, out guid))
                            {
                                m_typeNameToGuid.Add(typeGuid.FullName, guid);
                            }
                        }
                    }

                    LoadAssembly(getAssemblyAo.Result.Data);
                }
            }

            IsLoaded = true;

            if (Loaded != null)
            {
                Loaded();
            }
        }

        private void OnDeleteProjectItemCompleted(Error error, ProjectItem[] result)
        {
            for (int i = 0; i < result.Length; ++i)
            {
                if (result[i] is AssetItem)
                {
                    AssetItem assetItem = (AssetItem)result[i];
                    if (assetItem.Ext == Ext)
                    {
                        Compile();
                    }
                }
            }
        }

        public void AddReference(string assemblyLocation)
        {
            if(!m_assemblyReferences.Contains(assemblyLocation))
            {
                m_assemblyReferences.Add(assemblyLocation);
            }
        }

        public void CreateScript(ProjectItem folder)
        {
            string name = m_project.GetUniqueName(m_localization.GetString("ID_RTScripting_ScriptsManager_Script", "Script"), Ext, folder, true);

            string nl = Environment.NewLine;
            RuntimeTextAsset csFile = ScriptableObject.CreateInstance<RuntimeTextAsset>();
            csFile.name = name;
            csFile.Ext = Ext;
            csFile.Text =
                "using System.Collections;" + nl +
                "using System.Collections.Generic;" + nl +
                "using UnityEngine;" + nl + nl +

                "public class " + name + " : MonoBehaviour" + nl +
                "{" + nl +
                "    // Start is called before the first frame update" + nl +
                "    void Start()" + nl +
                "    {" + nl +
                "    }" + nl + nl +

                "    // Update is called once per frame" + nl +
                "    void Update()" + nl +
                "    {" + nl +
                "    }" + nl +
                "}";

            IProjectFolder projectFolder = IOC.Resolve<IProjectFolder>();
            projectFolder.CreateAsset(csFile, folder);
        }

        public ProjectAsyncOperation<RuntimeTextAsset> LoadScript(AssetItem assetItem)
        {
            ProjectAsyncOperation<RuntimeTextAsset> ao = new ProjectAsyncOperation<RuntimeTextAsset>();
            StartCoroutine(CoLoadScript(assetItem, ao));
            return ao;
        }

        private IEnumerator CoLoadScript(AssetItem assetItem, ProjectAsyncOperation<RuntimeTextAsset> ao)
        {
            ProjectAsyncOperation<UnityObject[]> loadAo = m_project.Load(new[] { assetItem });
            yield return loadAo;

            ao.Error = loadAo.Error;
            if (!ao.HasError)
            {
                ao.Result = (RuntimeTextAsset)loadAo.Result[0];
            }
            ao.IsCompleted = true;
        }

        public ProjectAsyncOperation SaveScript(AssetItem assetItem, RuntimeTextAsset script)
        {
            return m_project.Save(new[] { assetItem }, new[] { script });
        }

        private void RaiseCompiling()
        {
            if (Compiling != null)
            {
                Compiling();
            }

        }

        private void RaiseCompiled(bool completed)
        {
            if (Complied != null)
            {
                Complied(completed);
            }
        }

        public ProjectAsyncOperation Compile()
        {
            ProjectAsyncOperation ao = new ProjectAsyncOperation();
            StartCoroutine(CoCompile(ao));
            return ao;
        }

        private IEnumerator CoCompile(ProjectAsyncOperation ao)
        {
            RaiseCompiling();

            AssetItem[] assetItems = m_project.FindAssetItems(null, true, typeof(RuntimeTextAsset)).Where(assetItem => assetItem.Ext == Ext).ToArray();
            ProjectAsyncOperation<UnityObject[]> loadAo = m_project.Load(assetItems);
            yield return loadAo;
            if (loadAo.HasError)
            {
                RaiseCompiled(false);

                ao.Error = loadAo.Error;
                ao.IsCompleted = true;

                yield break;
            }

            RunCompilerAsync(loadAo.Result.OfType<RuntimeTextAsset>().Select(s => s.Text).ToArray(), ao);
        }

#pragma warning disable CS1998
        public async void RunCompilerAsync(string[] scripts, ProjectAsyncOperation ao)
        {
#if UNITY_STANDALONE
            ICompiler compiler = IOC.Resolve<ICompiler>();
#endif
            try
            {
                byte[] binData = null;
#if UNITY_STANDALONE
                binData = await Task.Run(() => compiler.Compile(scripts, m_assemblyReferences.ToArray()));
#endif
                if (binData == null)
                {
                    RaiseCompiled(false);

                    ao.Error = new Error(Error.E_Failed) { ErrorText = m_localization.GetString("ID_RTScripting_ScriptsManager_CompilationFailed", "Compilation failed") };
                    ao.IsCompleted = true;

                }
                else
                {
                    StartCoroutine(CoSaveAssembly(binData, ao));
                }
            }
            catch (Exception e)
            {
                RaiseCompiled(false);

                ao.Error = new Error(Error.E_Exception)
                {
                    ErrorText = e.ToString()
                };
                ao.IsCompleted = true;
            }
        }
#pragma warning restore CS1998

        private IEnumerator CoSaveAssembly(byte[] binData, ProjectAsyncOperation ao)
        {
            RuntimeBinaryAsset asmBinaryData = ScriptableObject.CreateInstance<RuntimeBinaryAsset>();
            asmBinaryData.Data = binData;

            ProjectAsyncOperation setValueAo = m_project.SetValue(RuntimeAssemblyKey, asmBinaryData);
            yield return setValueAo;
            if (setValueAo.HasError)
            {
                RaiseCompiled(false);

                ao.Error = setValueAo.Error;
                ao.IsCompleted = true;
                yield break;
            }

            LoadAssembly(binData);

            RuntimeTypeGuids guids = new RuntimeTypeGuids
            {
                Guids = m_typeNameToGuid.Select(kvp => new RuntimeTypeGuid { FullName = kvp.Key, Guid = kvp.Value.ToString() }).ToArray()
            };

            m_runtimeTypeGuidsAsset.Text = XmlUtility.ToXml(guids);
            ProjectAsyncOperation setGuidsAo = m_project.SetValue(RuntimeTypeGuids, m_runtimeTypeGuidsAsset);
            yield return setGuidsAo;
            if (setGuidsAo.HasError)
            {
                RaiseCompiled(false);

                Debug.LogError(setGuidsAo.Error);
                ao.Error = setGuidsAo.Error;
            }
            else
            {
                RaiseCompiled(true);

                ao.Error = Error.NoError;
            }
            ao.IsCompleted = true;
        }

        private void LoadAssembly(byte[] binData)
        {
            Dictionary<string, List<UnityObject>> typeToDestroyedObjects = UnloadTypes(true);

            Dictionary<string, Guid> typeNameToGuidNew = new Dictionary<string, Guid>();
            m_runtimeAssembly = Assembly.Load(binData);
            Type[] loadedTypes = m_runtimeAssembly.GetTypes().Where(t => typeof(MonoBehaviour).IsAssignableFrom(typeof(MonoBehaviour))).ToArray();
            foreach (Type type in loadedTypes)
            {
                Guid guid;
                string typeName = type.FullName;
                if (!m_typeNameToGuid.TryGetValue(typeName, out guid))
                {
                    guid = Guid.NewGuid();
                    m_typeNameToGuid.Add(typeName, guid);
                }

                if(m_editorsMap != null)
                {
                    m_editorsMap.AddMapping(type, typeof(RuntimeScriptEditor), true, false);
                }
                
                m_typeMap.RegisterRuntimeSerializableType(type, guid);
                typeNameToGuidNew.Add(typeName, guid);
            }

            m_typeNameToGuid = typeNameToGuidNew;
            EraseDestroyedObjects(typeToDestroyedObjects);
        }

        private void EraseDestroyedObjects(Dictionary<string, List<UnityObject>> typeToDestroyedObjects)
        {
            foreach (KeyValuePair<string, List<UnityObject>> kvp in typeToDestroyedObjects)
            {
                IRTE editor = IOC.Resolve<IRTE>();
                List<UnityObject> destroyedObjects = kvp.Value;
                Type type = m_runtimeAssembly.GetType(kvp.Key);
                if (!m_typeNameToGuid.ContainsKey(kvp.Key))
                {
                    for (int i = 0; i < destroyedObjects.Count; ++i)
                    {
                        editor.Undo.Erase(destroyedObjects[i], null);
                    }
                }
                else
                {
                    for (int i = 0; i < destroyedObjects.Count; ++i)
                    {
                        UnityObject destroyedObject = destroyedObjects[i];
                        UnityObject obj = null;
                        if (destroyedObject is Component)
                        {
                            obj = ((Component)destroyedObject).gameObject.AddComponent(type);
                        }
                        else if (destroyedObject is ScriptableObject)
                        {
                            obj = ScriptableObject.CreateInstance(type);
                        }
                        editor.Undo.Erase(destroyedObject, obj);
                    }
                }
            }
        }

        private Dictionary<string, List<UnityObject>> UnloadTypes(bool destroyObjects)
        {
            Dictionary<string, List<UnityObject>> typeToDestroyedObjects = new Dictionary<string, List<UnityObject>>();
            if (m_runtimeAssembly != null)
            {
                Type[] unloadedTypes = m_runtimeAssembly.GetTypes().Where(t => typeof(MonoBehaviour).IsAssignableFrom(typeof(MonoBehaviour))).ToArray();
                foreach (Type type in unloadedTypes)
                {
                    if (destroyObjects)
                    {
                        List<UnityObject> destroyedObjects = new List<UnityObject>();
                        UnityObject[] objectsOfType = Resources.FindObjectsOfTypeAll(type);
                        foreach (UnityObject obj in objectsOfType)
                        {
                            Destroy(obj);
                            destroyedObjects.Add(obj);
                            //m_editor.Undo.Erase(obj, null);
                        }
                        typeToDestroyedObjects.Add(type.FullName, destroyedObjects);
                    }

                    if(m_editorsMap != null)
                    {
                        m_editorsMap.RemoveMapping(type);
                    }
                    
                    m_typeMap.UnregisterRuntimeSerialzableType(type);
                }
            }

            return typeToDestroyedObjects;
        }
    }
}

