using ProtoBuf.Meta;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events.Battlehub.SL2;
using UnityEngine.Battlehub.SL2;
using Battlehub.RTSL.Battlehub.SL2;
using Battlehub.RTSL.Interface;
using Battlehub.RTCommon;
#if !UNITY_EDITOR
using System.Reflection;
using System.Linq;
#endif

namespace Battlehub.RTSL
{

    [ProtoBuf.ProtoContract]
    public class NilContainer { }

    public class ProtobufSerializer : ISerializer
    {
        private static TypeModel model;

        static ProtobufSerializer()
        {
#if !UNITY_EDITOR
            Assembly typeModelAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm.FullName.Contains("RTSLTypeModel")).FirstOrDefault();
            Type type = null;
            if (typeModelAssembly != null)
            {
                type = typeModelAssembly.GetTypes().Where(t => t.Name.Contains("RTSLTypeModel")).FirstOrDefault();
            }
            
            if(type != null)
            {
                model = Activator.CreateInstance(type) as TypeModel;
            }  

            if(model == null)
            {
                UnityEngine.Debug.LogError("RTSLTypeModel was not found. Please build type model using the Build All button available through the Tools->Runtime SaveLoad->Config menu item in Unity Editor.");
            }
#endif
            if (model == null)
            {
                model = CreateTypeModel();
            }

            model.DynamicTypeFormatting += (sender, args) =>
            {
                if (args.FormattedName == null)
                {
                    return;
                }

                if (Type.GetType(args.FormattedName) == null)
                {
                    args.Type = typeof(NilContainer);
                }
            };

#if UNITY_EDITOR
            RuntimeTypeModel runtimeTypeModel = model as RuntimeTypeModel;
            if (runtimeTypeModel != null)
            {
                runtimeTypeModel.CompileInPlace();
            }
#endif
        }


        public TData DeepClone<TData>(TData data)
        {
            return (TData)model.DeepClone(data);
        }

        public TData Deserialize<TData>(Stream stream)
        {
            TData deserialized = (TData)model.Deserialize(stream, null, typeof(TData));
            return deserialized;
        }

        public object Deserialize(byte[] b, Type type)
        {
            using (var stream = new MemoryStream(b))
            {
                return model.Deserialize(stream, null, type);
            }
        }

        public object Deserialize(Stream stream, Type type, long length = -1)
        {
            if (length <= 0)
            {
                return model.Deserialize(stream, null, type);
            }
            return model.Deserialize(stream, null, type, (int)length);
        }

        public TData Deserialize<TData>(byte[] b)
        {
            using (var stream = new MemoryStream(b))
            {
                TData deserialized = (TData)model.Deserialize(stream, null, typeof(TData));
                return deserialized;
            }
        }

        public TData Deserialize<TData>(byte[] b, TData obj)
        {
            using (var stream = new MemoryStream(b))
            {
                return (TData)model.Deserialize(stream, obj, typeof(TData));
            }
        }

        public void Serialize<TData>(TData data, Stream stream)
        {
            model.Serialize(stream, data);
        }

        public byte[] Serialize<TData>(TData data)
        {
            using (var stream = new MemoryStream())
            {
                model.Serialize(stream, data);
                stream.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        public static RuntimeTypeModel CreateTypeModel()
        {
            RuntimeTypeModel model = TypeModel.Create();

            model.Add(typeof(IntArray), true);
            model.Add(typeof(ProjectItem), true)
                .AddSubType(1025, typeof(AssetItem));
            model.Add(typeof(AssetItem), true);
            model.Add(typeof(AssetBundleItemInfo), true);
            model.Add(typeof(AssetBundleInfo), true);
            model.Add(typeof(ProjectInfo), true);
            model.Add(typeof(PrefabPart), true);
            model.Add(typeof(Preview), true);
            model.Add(typeof(PersistentDescriptor<long>), true);
            model.Add(typeof(PersistentPersistentCall<long>), true);
            model.Add(typeof(PersistentArgumentCache<long>), true);
            model.Add(typeof(PersistentBlendShapeFrame<long>), true);
            model.Add(typeof(PersistentBlendShapeFrame<Guid>), true);
            model.Add(typeof(PersistentDescriptor<Guid>), true);
            model.Add(typeof(PersistentPersistentCall<Guid>), true);
            model.Add(typeof(PersistentArgumentCache<Guid>), true);

            ITypeModelCreator typeModelCreator = IOC.Resolve<ITypeModelCreator>();
            if (typeModelCreator == null)
            {
                DefaultTypeModel(model);
            }
            else
            {
                typeModelCreator.Create(model);
            }

            MetaType primitiveContract = model.Add(typeof(PrimitiveContract), false);
            int fieldNumber = 16;

            //NOTE: Items should be added to TypeModel in exactly the same order!!!
            //It is allowed to append new types, but not to insert new types in the middle.

            Type[] types = new[] {
                typeof(bool),
                typeof(char),
                typeof(byte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(string),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(PersistentColor<long>),
                typeof(PersistentVector4<long>),
                typeof(Guid),
                typeof(PersistentColor<Guid>),
                typeof(PersistentVector4<Guid>)};

            foreach (Type type in types)
            {
                //if (type.IsGenericType())
                //{
                //    continue;
                //}

                Type derivedType = typeof(PrimitiveContract<>).MakeGenericType(type.MakeArrayType());
                primitiveContract.AddSubType(fieldNumber, derivedType);
                fieldNumber++;
                model.Add(derivedType, true);

                derivedType = typeof(PrimitiveContract<>).MakeGenericType(type);
                primitiveContract.AddSubType(fieldNumber, derivedType);
                fieldNumber++;
                model.Add(derivedType, true);

                model.Add(typeof(List<>).MakeGenericType(type), true);
            }

            //This is special kind of peristent object which can be used to serialize types using reflection. (This is required to serialize objects of type created at runtime for example)
            model.Add(typeof(PersistentRuntimeSerializableObject<long>), true);
            model[typeof(PersistentObject<long>)].AddSubType(1024, typeof(PersistentRuntimeSerializableObject<long>));

            model.Add(typeof(PersistentRuntimeSerializableObject<Guid>), true);
            model[typeof(PersistentObject<Guid>)].AddSubType(1024, typeof(PersistentRuntimeSerializableObject<Guid>));

            model.AutoAddMissingTypes = false;
            return model;
        }

        private static void DefaultTypeModel(RuntimeTypeModel model)
        {
            DefaultTypeModel<long>(model);
            DefaultTypeModel<Guid>(model);

            model.Add(typeof(PersistentColor<long>), true);
            model.Add(typeof(Color), false).SetSurrogate(typeof(PersistentColor<long>));
            model.Add(typeof(PersistentVector3<long>), true);
            model.Add(typeof(Vector3), false).SetSurrogate(typeof(PersistentVector3<long>));
            model.Add(typeof(PersistentVector4<long>), true);
            model.Add(typeof(Vector4), false).SetSurrogate(typeof(PersistentVector4<long>));
            model.Add(typeof(PersistentQuaternion<long>), true);
            model.Add(typeof(Quaternion), false).SetSurrogate(typeof(PersistentQuaternion<long>));

            model.Add(typeof(PersistentColor<Guid>), true);
            model.Add(typeof(Color), false).SetSurrogate(typeof(PersistentColor<Guid>));
            model.Add(typeof(PersistentVector3<Guid>), true);
            model.Add(typeof(Vector3), false).SetSurrogate(typeof(PersistentVector3<Guid>));
            model.Add(typeof(PersistentVector4<Guid>), true);
            model.Add(typeof(Vector4), false).SetSurrogate(typeof(PersistentVector4<Guid>));
            model.Add(typeof(PersistentQuaternion<Guid>), true);
            model.Add(typeof(Quaternion), false).SetSurrogate(typeof(PersistentQuaternion<Guid>));
        }

        private static void DefaultTypeModel<TID>(RuntimeTypeModel model)
        {
            model.Add(typeof(PersistentRuntimeScene<TID>), true);
            model.Add(typeof(PersistentRuntimePrefab<TID>), true)
                .AddSubType(1025, typeof(PersistentRuntimeScene<TID>));

            model.Add(typeof(PersistentGameObject<TID>), true);
            model.Add(typeof(PersistentTransform<TID>), true);

            model.Add(typeof(PersistentObject<TID>), true)
               .AddSubType(1025, typeof(PersistentGameObject<TID>))
               .AddSubType(1029, typeof(PersistentComponent<TID>))
               .AddSubType(1045, typeof(PersistentRuntimePrefab<TID>));

            model.Add(typeof(PersistentComponent<TID>), true)
                .AddSubType(1026, typeof(PersistentTransform<TID>));

            model.Add(typeof(PersistentUnityEvent<TID>), true);
            model.Add(typeof(PersistentUnityEventBase<TID>), true)
                .AddSubType(1025, typeof(PersistentUnityEvent<TID>));
        }
    }
}
