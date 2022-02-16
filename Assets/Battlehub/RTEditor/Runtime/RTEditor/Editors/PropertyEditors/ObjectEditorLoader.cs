﻿using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using System;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Battlehub.RTEditor
{
    public class ObjectEditorLoader : MonoBehaviour, IObjectEditorLoader
    {
        private void Awake()
        {
            IOC.RegisterFallback<IObjectEditorLoader>(this);
        }

        private void OnDestroy()
        {
            IOC.UnregisterFallback<IObjectEditorLoader>(this);
        }

        private Type ToType(GameObject go, Type memberInfoType)
        {
            Type type;
            if (typeof(Component).IsAssignableFrom(memberInfoType) && go.GetComponent(memberInfoType) != null)
            {
                type = memberInfoType;
            }
            else
            {
                type = typeof(GameObject);
            }

            return type;
        }

        public Type GetObjectType(object dragObject, Type memberInfoType)
        {
            Type type = null;
            if (dragObject is ExposeToEditor)
            {
                ExposeToEditor exposeToEditor = (ExposeToEditor)dragObject;
                GameObject go = exposeToEditor.gameObject;
                type = ToType(go, memberInfoType);
            }
            else if (dragObject is GameObject)
            {
                type = ToType((GameObject)dragObject, memberInfoType);
            }
            else if (dragObject is AssetItem)
            {
                AssetItem assetItem = (AssetItem)dragObject;
                IProject project = IOC.Resolve<IProject>();
                type = project.ToType(assetItem);
            }
            return type;
        }

        public void Load(object dragObject, Type memberInfoType, Action<UnityObject> callback)
        {
            if (dragObject is AssetItem)
            {
                AssetItem assetItem = (AssetItem)dragObject;
                IProject project = IOC.Resolve<IProject>();
                project.Load(new[] { assetItem }, (error, loadedObjects) =>
                {
                    if (error.HasError)
                    {
                        IWindowManager wnd = IOC.Resolve<IWindowManager>();
                        wnd.MessageBox("Unable to load object", error.ErrorText);
                        return;
                    }

                    callback(loadedObjects[0]);
                });
            }
            else if (dragObject is GameObject)
            {
                UnityObject value = GetGameObjectOrComponent((GameObject)dragObject, memberInfoType);
                callback(value);
            }
            else if (dragObject is ExposeToEditor)
            {
                UnityObject value = GetGameObjectOrComponent(((ExposeToEditor)dragObject).gameObject, memberInfoType);
                callback(value);
            }
        }

        private UnityObject GetGameObjectOrComponent(GameObject go, Type memberInfoType)
        {
            if(memberInfoType == typeof(GameObject))
            {
                return go;
            }

            Component component = go.GetComponent(memberInfoType);
            if (component != null)
            {
                return component;
            }
            return go;
        }
    }
}

