using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using Battlehub.Utils;
using System;
using UnityEngine;

namespace Battlehub.RTSL
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(RTSLIgnore))]
    public class RTSLDeps : RTSLDeps<long>
    {
        private IAssetDB m_assetDB;
        protected override IAssetDB<long> AssetDB
        {
            get
            {
                if (m_assetDB == null)
                {
                    m_assetDB = new AssetDB();
                }
                return m_assetDB;
            }
        }


        private Project m_project;
        protected override IProject Project
        {
            get
            {
                if (m_project == null)
                {
                    m_project = FindObjectOfType<Project>();
                    if (m_project == null)
                    {
                        m_project = gameObject.AddComponent<Project>();
                    }
                }
                return m_project;
            }
        }

        private Func<IAssetDB> m_registerAssetDB;
        private Func<IIDMap> m_registerIDMap;

        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            m_registerAssetDB = () => (IAssetDB)AssetDB;
            m_registerIDMap = () => (IIDMap)AssetDB;

            IOC.RegisterFallback(m_registerAssetDB);
            IOC.RegisterFallback(m_registerIDMap);
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            IOC.UnregisterFallback(m_registerAssetDB);
            IOC.UnregisterFallback(m_registerIDMap);

            Init();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            IProject project = null;
            IOC.RegisterFallback(() =>
            {
                if (project == null)
                {
                    RTSLDeps deps = FindObjectOfType<RTSLDeps>();
                    if (deps == null)
                    {
                        GameObject go = new GameObject("RTSL");
                        go.transform.SetSiblingIndex(0);
                        deps = go.AddComponent<RTSLDeps>();
                        project = deps.Project;
                    }
                }
                return project;
            });
        }
    }

    public abstract class RTSLDeps<TID> : MonoBehaviour
    {

        private IAssetBundleLoader m_assetBundleLoader;
        protected virtual IAssetBundleLoader AssetBundleLoader
        {
            get
            {
                if (m_assetBundleLoader == null)
                {
                    m_assetBundleLoader = new AssetBundleLoader();
                }
                return m_assetBundleLoader;

            }
        }

        private IMaterialUtil materialUtil;
        protected virtual IMaterialUtil MaterialUtil
        {
            get
            {
                if (materialUtil == null)
                {
                    materialUtil = new StandardMaterialUtils();
                }
                return materialUtil;
            }
        }

        private IRuntimeShaderUtil m_shaderUtil;
        protected virtual IRuntimeShaderUtil ShaderUtil
        {
            get
            {
                if (m_shaderUtil == null)
                {
                    m_shaderUtil = new RuntimeShaderUtil();
                }
                return m_shaderUtil;
            }
        }

        private ITypeMap m_typeMap;
        protected virtual ITypeMap TypeMap
        {
            get
            {
                if (m_typeMap == null)
                {
                    m_typeMap = new TypeMap<TID>();
                }

                return m_typeMap;
            }
        }

        private IUnityObjectFactory m_objectFactory;
        protected virtual IUnityObjectFactory ObjectFactory
        {
            get
            {
                if (m_objectFactory == null)
                {
                    m_objectFactory = new UnityObjectFactory();
                }

                return m_objectFactory;
            }
        }

        private ISerializer m_serializer;
        protected virtual ISerializer Serializer
        {
            get
            {
                if (m_serializer == null)
                {
                    m_serializer = new ProtobufSerializer();
                }
                return m_serializer;
            }
        }

        private IStorage<TID> m_storage;
        protected virtual IStorage<TID> Storage
        {
            get
            {
                if (m_storage == null)
                {
                    m_storage = new FileSystemStorage<TID>();
                }
                return m_storage;
            }
        }

        private IPlayerPrefsStorage m_playerPrefs;
        protected virtual IPlayerPrefsStorage PlayerPrefs
        {
            get
            {
                if (m_playerPrefs == null)
                {
                    m_playerPrefs = new PlayerPrefsStorage();
                }
                return m_playerPrefs;
            }
        }

        protected abstract IAssetDB<TID> AssetDB
        {
            get;
        }

        protected abstract IProject Project
        {
            get;
        }

        private Func<IAssetBundleLoader> m_registerBundleLoader;
        private Func<ITypeMap> m_registerTypeMap;
        private Func<IUnityObjectFactory> m_registerObjectFactory;
        private Func<ISerializer> m_registerSerializer;
        private Func<IStorage<TID>> m_registerStorage;
        private Func<IAssetDB<TID>> m_registerAssetDB;
        private Func<IIDMap<TID>> m_registerIDMap;
        private Func<IProject> m_registerProject;
        private Func<IRuntimeShaderUtil> m_registerShaderUtil;
        private Func<IMaterialUtil> m_registerMaterialUtil;
        private Func<IPlayerPrefsStorage> m_registerPlayerPrefs;

        protected virtual void Awake()
        {
            AwakeOverride();
        }

        protected virtual void AwakeOverride()
        {
            if (gameObject.GetComponent<Dispatcher>() == null)
            {
                gameObject.AddComponent<Dispatcher>();
            }

            m_registerBundleLoader = () => AssetBundleLoader;
            m_registerTypeMap = () => TypeMap;
            m_registerObjectFactory = () => ObjectFactory;
            m_registerSerializer = () => Serializer;
            m_registerStorage = () => Storage;
            m_registerAssetDB = () => AssetDB;
            m_registerIDMap = () => AssetDB;
            m_registerProject = () => Project;
            m_registerShaderUtil = () => ShaderUtil;
            m_registerMaterialUtil = () => MaterialUtil;
            m_registerPlayerPrefs = () => PlayerPrefs;

            IOC.UnregisterFallback<IProject>();

            IOC.RegisterFallback(m_registerBundleLoader);
            IOC.RegisterFallback(m_registerTypeMap);
            IOC.RegisterFallback(m_registerObjectFactory);
            IOC.RegisterFallback(m_registerSerializer);
            IOC.RegisterFallback(m_registerStorage);
            IOC.RegisterFallback(m_registerAssetDB);
            IOC.RegisterFallback(m_registerIDMap);

            IOC.RegisterFallback(m_registerProject);
            IOC.RegisterFallback(m_registerShaderUtil);
            IOC.RegisterFallback(m_registerMaterialUtil);
            IOC.RegisterFallback(m_registerPlayerPrefs);
        }

        protected virtual void OnDestroy()
        {
            OnDestroyOverride();
        }

        protected virtual void OnDestroyOverride()
        {
            IOC.UnregisterFallback(m_registerBundleLoader);
            IOC.UnregisterFallback(m_registerTypeMap);
            IOC.UnregisterFallback(m_registerObjectFactory);
            IOC.UnregisterFallback(m_registerSerializer);
            IOC.UnregisterFallback(m_registerStorage);
            IOC.UnregisterFallback(m_registerAssetDB);
            IOC.UnregisterFallback(m_registerIDMap);
            IOC.UnregisterFallback(m_registerProject);
            IOC.UnregisterFallback(m_registerShaderUtil);
            IOC.UnregisterFallback(m_registerMaterialUtil);
            IOC.UnregisterFallback(m_registerPlayerPrefs);
        }
    }
}

