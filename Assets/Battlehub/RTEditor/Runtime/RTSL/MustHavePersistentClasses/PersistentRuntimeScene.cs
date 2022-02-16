using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.Battlehub.SL2;
using UnityObject = UnityEngine.Object;
using UnityEngine;
using System.IO;

namespace Battlehub.RTSL.Battlehub.SL2
{
    [ProtoContract]
    public class PersistentRuntimeScene<TID> : PersistentRuntimePrefab<TID>, ICustomSerialization
    {
        [ProtoMember(1)]
        public PersistentObject<TID>[] Assets;

        [ProtoMember(2) /*, Obsolete*/] //For compatibility purposes
        public int[] AssetIdentifiers;

        [ProtoMember(3)]
        public TID[] AssetIds;

        protected override TID ToID(UnityObject uo)
        {
            TID persistentID;
            if (m_assetDB.IsMapped(uo))
            {
                persistentID = m_assetDB.ToID(uo);
            }
            else
            {
                persistentID = m_assetDB.CreateID(uo);
                m_assetDB.RegisterSceneObject(persistentID, uo);
            }

            return persistentID;
        }

        protected override void ReadFromImpl(object obj)
        {
            ClearReferencesCache();

            Scene scene = (Scene)obj;

            GameObject[] rootGameObjects;
            if (scene.IsValid())
            {
                rootGameObjects = scene.GetRootGameObjects();
            }
            else
            {
                rootGameObjects = new GameObject[0];
            }

            List<Tuple<PersistentObject<TID>, UnityObject>> data = new List<Tuple<PersistentObject<TID>, UnityObject>>();
            List<TID> identifiers = new List<TID>();
            List<PersistentDescriptor<TID>> descriptors = new List<PersistentDescriptor<TID>>(rootGameObjects.Length);
            GetDepsFromContext getSceneDepsCtx = new GetDepsFromContext();

            for (int i = 0; i < rootGameObjects.Length; ++i)
            {
                GameObject rootGO = rootGameObjects[i];
                PersistentDescriptor<TID> descriptor = CreateDescriptorAndData(rootGO, data, identifiers, getSceneDepsCtx);
                if (descriptor != null)
                {
                    descriptors.Add(descriptor);
                }
            }

            HashSet<object> allDeps = getSceneDepsCtx.Dependencies;
            Queue<UnityObject> depsQueue = new Queue<UnityObject>(allDeps.OfType<UnityObject>());
            List<Tuple<PersistentObject<TID>, UnityObject>> assets = new List<Tuple<PersistentObject<TID>, UnityObject>>();
            List<TID> assetIds = new List<TID>();

            GetDepsFromContext getDepsCtx = new GetDepsFromContext();
            while (depsQueue.Count > 0)
            {
                UnityObject uo = depsQueue.Dequeue();
                if (!uo)
                {
                    continue;
                }


                Type persistentType = m_typeMap.ToPersistentType(uo.GetType());
                if (persistentType != null)
                {
                    getDepsCtx.Clear();

                    try
                    {
                        PersistentObject<TID> persistentObject = (PersistentObject<TID>)Activator.CreateInstance(persistentType);
                        if (!(uo is GameObject) && !(uo is Component) && (uo.hideFlags & HideFlags.DontSave) == 0)
                        {
                            if (!m_assetDB.IsMapped(uo))
                            {
                                assets.Add(new Tuple<PersistentObject<TID>, UnityObject>(persistentObject, uo));
                                assetIds.Add(ToID(uo));
                                persistentObject.GetDepsFrom(uo, getDepsCtx);
                            }
                            else
                            {
                                persistentObject.GetDepsFrom(uo, getDepsCtx);
                            }
                        }
                        else
                        {
                            persistentObject.GetDepsFrom(uo, getDepsCtx);
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }

                    foreach (UnityObject dep in getDepsCtx.Dependencies)
                    {
                        if (!allDeps.Contains(dep))
                        {
                            allDeps.Add(dep);
                            depsQueue.Enqueue(dep);
                        }
                    }

                }
            }

            List<UnityObject> externalDeps = new List<UnityObject>(allDeps.OfType<UnityObject>());
            for (int i = externalDeps.Count - 1; i >= 0; i--)
            {
                if (!m_assetDB.IsMapped(externalDeps[i]))
                {
                    externalDeps.RemoveAt(i);
                }
            }

            Descriptors = descriptors.ToArray();
            Identifiers = identifiers.ToArray();
            Data = data.Select(t =>
            {
                PersistentObject<TID> persistentObject = t.Item1;
                persistentObject.ReadFrom(t.Item2);
                return persistentObject;
            }).ToArray();
            Dependencies = externalDeps.Select(uo => ToID(uo)).ToArray();

            Assets = assets.Select(t =>
            {
                PersistentObject<TID> persistentObject = t.Item1;
                persistentObject.ReadFrom(t.Item2);
                return persistentObject;
            }).ToArray();

            AssetIds = assetIds.ToArray();

            m_assetDB.UnregisterSceneObjects();
            ClearReferencesCache();
        }

        protected override object WriteToImpl(object obj)
        {
            ClearReferencesCache();

            Scene scene = (Scene)obj;
            if (Descriptors == null && Data == null)
            {
                DestroyGameObjects(scene);
                return obj;
            }

            if (Descriptors == null && Data != null || Data != null && Descriptors == null)
            {
                throw new ArgumentException("data is corrupted", "scene");
            }

            if (Descriptors.Length == 0)
            {
                DestroyGameObjects(scene);
                return obj;
            }

            if (Identifiers == null || Identifiers.Length != Data.Length)
            {
                throw new ArgumentException("data is corrupted", "scene");
            }

            DestroyGameObjects(scene);
            Dictionary<TID, UnityObject> idToUnityObj = new Dictionary<TID, UnityObject>();
            for (int i = 0; i < Descriptors.Length; ++i)
            {
                PersistentDescriptor<TID> descriptor = Descriptors[i];
                if (descriptor != null)
                {
                    CreateGameObjectWithComponents(m_typeMap, descriptor, idToUnityObj, null);
                }
            }

            UnityObject[] assetInstances = null;
            if (AssetIds != null)
            {
                IUnityObjectFactory factory = IOC.Resolve<IUnityObjectFactory>();
                assetInstances = new UnityObject[AssetIds.Length];
                for (int i = 0; i < AssetIds.Length; ++i)
                {
                    Type uoType;
                    PersistentObject<TID> asset = Assets[i];
                    if (asset is PersistentRuntimeSerializableObject<TID>)
                    {
                        PersistentRuntimeSerializableObject<TID> runtimeSerializableObject = (PersistentRuntimeSerializableObject<TID>)asset;
                        uoType = runtimeSerializableObject.ObjectType;
                    }
                    else
                    {
                        uoType = m_typeMap.ToUnityType(asset.GetType());
                    }

                    if (uoType != null)
                    {
                        if (factory.CanCreateInstance(uoType, asset))
                        {
                            UnityObject assetInstance = factory.CreateInstance(uoType, asset);
                            if (assetInstance != null)
                            {
                                assetInstances[i] = assetInstance;
                                idToUnityObj.Add(AssetIds[i], assetInstance);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Unable to create object of type " + uoType.ToString());
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Unable to resolve unity type for " + asset.GetType().FullName);
                    }
                }
            }

            m_assetDB.RegisterSceneObjects(idToUnityObj);

            if (assetInstances != null)
            {
                for (int i = 0; i < assetInstances.Length; ++i)
                {
                    UnityObject assetInstance = assetInstances[i];
                    if (assetInstance != null)
                    {
                        PersistentObject<TID> asset = Assets[i];
                        asset.WriteTo(assetInstance);
                    }
                }
            }

            RestoreDataAndResolveDependencies();
            m_assetDB.UnregisterSceneObjects();

            ClearReferencesCache();

            return scene;
        }

        protected override void GetDepsFromImpl(object obj, GetDepsFromContext context)
        {
            if (!(obj is Scene))
            {
                return;
            }

            Scene scene = (Scene)obj;
            GameObject[] gameObjects = scene.GetRootGameObjects();

            for (int i = 0; i < gameObjects.Length; ++i)
            {
                base.GetDepsFromImpl(gameObjects[i], context);
            }
        }

        protected override void GetDependenciesFrom(GameObject go, List<object> prefabParts, GetDepsFromContext context)
        {
            if ((go.hideFlags & HideFlags.DontSave) != 0)
            {
                //Do not save persistent ignore objects
                return;
            }
            base.GetDependenciesFrom(go, prefabParts, context);
        }

        protected override PersistentDescriptor<TID> CreateDescriptorAndData(GameObject go, List<Tuple<PersistentObject<TID>, UnityObject>> persistentData, List<TID> persistentIdentifiers, GetDepsFromContext getDepsFromCtx, PersistentDescriptor<TID> parentDescriptor = null)
        {
            if ((go.hideFlags & HideFlags.DontSave) != 0)
            {
                return null;
            }
            return base.CreateDescriptorAndData(go, persistentData, persistentIdentifiers, getDepsFromCtx, parentDescriptor);
        }

        private void DestroyGameObjects(Scene scene)
        {
            if (UnityObject.FindObjectOfType<RTSLAdditive>())
            {
                return;
            }

            GameObject[] rootGameObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; ++i)
            {
                GameObject rootGO = rootGameObjects[i];
                if (rootGO.GetComponent<RTSLIgnore>() || (rootGO.hideFlags & HideFlags.DontSave) != 0)
                {
                    continue;
                }

                UnityObject.DestroyImmediate(rootGO);
            }
        }


        public bool AllowStandardSerialization
        {
            get { return true; }
        }

        private List<ICustomSerialization> m_customSerializationAssets;
        private List<int> m_customSerializationAssetIndices;

        [ProtoBeforeSerialization]
        public void OnBeforeSerialization()
        {
            if (!RTSLSettings.IsCustomSerializationEnabled)
            {
                return;
            }
            m_customSerializationAssets = new List<ICustomSerialization>();
            m_customSerializationAssetIndices = new List<int>();

            for (int i = 0; i < Assets.Length; ++i)
            {
                ICustomSerialization asset = Assets[i] as ICustomSerialization;
                if (asset != null)
                {
                    m_customSerializationAssets.Add(asset);
                    m_customSerializationAssetIndices.Add(i);
                    if (!asset.AllowStandardSerialization)
                    {
                        Assets[i] = null;
                    }
                }
            }
        }

        public void Serialize(Stream stream, BinaryWriter writer)
        {
            writer.Write(m_customSerializationAssets.Count);
            for (int i = 0; i < m_customSerializationAssets.Count; ++i)
            {
                ICustomSerialization asset = m_customSerializationAssets[i];
                writer.Write(asset.AllowStandardSerialization);
                writer.Write(m_customSerializationAssetIndices[i]);
                writer.Write(m_typeMap.ToGuid(asset.GetType()).ToByteArray());
                asset.Serialize(stream, writer);
            }
            m_customSerializationAssets = null;
            m_customSerializationAssetIndices = null;
        }

        public void Deserialize(Stream stream, BinaryReader reader)
        {
            List<PersistentObject<TID>> assets = Assets != null ? Assets.ToList() : new List<PersistentObject<TID>>();

            int count = reader.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                bool allowStandardSerialization = reader.ReadBoolean();
                int index = reader.ReadInt32();
                Guid typeGuid = new Guid(reader.ReadBytes(16));
                Type type = m_typeMap.ToType(typeGuid);
                if (type == null)
                {
                    //Type removal is not allowed
                    throw new InvalidOperationException("Unknown type guid " + typeGuid);
                }

                ICustomSerialization customSerializationAsset;
                if (!allowStandardSerialization)
                {
                    PersistentObject<TID> asset = (PersistentObject<TID>)Activator.CreateInstance(type);
                    assets.Insert(index, asset);
                }

                customSerializationAsset = (ICustomSerialization)assets[index];
                customSerializationAsset.Deserialize(stream, reader);
            }

            Assets = assets.ToArray();
        }

        [ProtoAfterDeserialization]
        public void OnDeserialized()
        {
            if(AssetIdentifiers != null && AssetIds == null)
            {
                AssetIds = new TID[AssetIdentifiers.Length];
                for(int i = 0; i < AssetIdentifiers.Length; ++i)
                {
                    AssetIds[i] = Cast(AssetIdentifiers[i]);
                }
                AssetIdentifiers = null;
            }
        }

        private TID Cast(object value)
        {
            if (value is TID)
            {
                return (TID)value;
            }

            return (TID)Convert.ChangeType(value, typeof(TID));
        }
    }

    [Obsolete("Use generic version")]
    [ProtoContract]
    public class PersistentRuntimeScene : PersistentRuntimeScene<long>
    {
    }
}


