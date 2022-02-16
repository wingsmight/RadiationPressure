using System;
using System.Linq;
using UnityEngine;

using Battlehub.RTCommon;
using UnityObject = UnityEngine.Object;
using Battlehub.RTSL.Interface;

namespace Battlehub.RTEditor
{
    public class InspectorView : RuntimeWindow
    {
        [SerializeField]
        private Transform m_panel = null;

        [SerializeField]
        private GameObject m_addComponentRoot = null;

        [SerializeField]
        private AddComponentControl m_addComponentControl = null;

        private GameObject m_editor;

        private IEditorsMap m_editorsMap;

        private ISettingsComponent m_settingsComponent;

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.Inspector;
            base.AwakeOverride();

            m_editorsMap = IOC.Resolve<IEditorsMap>();
            m_settingsComponent = IOC.Resolve<ISettingsComponent>();

            Editor.Selection.SelectionChanged += OnRuntimeSelectionChanged;
            CreateEditor();
        }

        protected override void UpdateOverride()
        {
            base.UpdateOverride();
            UnityObject obj = Editor.Selection.activeObject;
            if(obj == null)
            {
                DestroyEditor();
            }
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if(Editor != null)
            {
                Editor.Selection.SelectionChanged -= OnRuntimeSelectionChanged;
            }
        }

        private void OnRuntimeSelectionChanged(UnityObject[] unselectedObjects)
        {
            if (m_editor != null &&  unselectedObjects != null && unselectedObjects.Length > 0)
            {
                IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
                if(editor.IsDirty)
                {
                    editor.IsDirty = false;
                    editor.SaveAssets(unselectedObjects, result =>
                    {
                        CreateEditor();
                    });
                }
                else
                {
                    CreateEditor();
                }
            }
            else
            {
                CreateEditor();
            }
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
            if (editor.IsDirty && editor.Selection.activeObject != null)
            {
                editor.IsDirty = false;
                editor.SaveAssets(editor.Selection.objects, result =>
                {
                });
            }
        }

        private void DestroyEditor()
        {
            if (m_editor != null)
            {
                Destroy(m_editor);
            }

            if(m_addComponentRoot != null)
            {
                m_addComponentRoot.SetActive(false);
            }

            if (m_addComponentControl != null)
            {
                m_addComponentControl.ComponentSelected -= OnAddComponent;
            }
        }

        private bool OfSameType(UnityObject[] objects, out Type type)
        {
            type = objects[0].GetType();
            for(int i = 1; i < objects.Length; ++i)
            {
                if(type != objects[i].GetType())
                {
                    return false;
                }
            }
            return true;
        }

        private void CreateEditor()
        {
            DestroyEditor();

            if (Editor.Selection.activeObject == null)
            {
                return;
            }

            if(Editor.Selection.objects[0] == null)
            {
                return;
            }


            UnityObject[] selectedObjects = Editor.Selection.objects.Where(o => o != null).ToArray();
            Type objType;
            if(!OfSameType(selectedObjects, out objType))
            {
                return;
            }

            ExposeToEditor exposeToEditor = null;
            if (objType == typeof(GameObject))
            {
                exposeToEditor = Editor.Selection.activeGameObject.GetComponent<ExposeToEditor>();
                if (exposeToEditor != null && !exposeToEditor.CanInspect)
                {
                    return;
                }
            }
                       
            GameObject editorPrefab;
            if (objType == typeof(Material))
            {
                Material mat = selectedObjects[0] as Material;
                if (mat.shader == null)
                {
                    return;
                }

                Shader shader = mat.shader;
                for(int i = 0; i < selectedObjects.Length; ++i)
                {
                    Material material = (Material)selectedObjects[i];
                    if(material.shader != shader)
                    {
                        return;
                    }
                }

                editorPrefab = m_editorsMap.GetMaterialEditor(mat.shader);
            }
            else
            {
                if (!m_editorsMap.IsObjectEditorEnabled(objType))
                {
                    return;
                }
                editorPrefab = m_editorsMap.GetObjectEditor(objType);
            }

            if (editorPrefab != null)
            {
                m_editor = Instantiate(editorPrefab);
                m_editor.transform.SetParent(m_panel, false);
                m_editor.transform.SetAsFirstSibling();
            }

            if (m_addComponentRoot != null && exposeToEditor && (m_settingsComponent == null || m_settingsComponent.BuiltInWindowsSettings.Inspector.ShowAddComponentButton))
            {
                IProject project = IOC.Resolve<IProject>();
                if(project == null || project.ToAssetItem(Editor.Selection.activeGameObject) == null)
                {
                    m_addComponentRoot.SetActive(true);
                    if (m_addComponentControl != null)
                    {
                        m_addComponentControl.ComponentSelected += OnAddComponent;
                    }
                }
            }
        }

        private void OnAddComponent(Type type)
        {
            IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
            editor.Undo.BeginRecord();

            GameObject[] gameObjects = editor.Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; ++i)
            {
                GameObject go = gameObjects[i];
                ExposeToEditor exposeToEditor = go.GetComponent<ExposeToEditor>();
                foreach (RequireComponent requirement in type.GetCustomAttributes(true).OfType<RequireComponent>())
                {
                    if(requirement.m_Type0 != null && !go.GetComponent(requirement.m_Type0))
                    {
                        editor.Undo.AddComponent(exposeToEditor, requirement.m_Type0);
                    }
                    if (requirement.m_Type1 != null && !go.GetComponent(requirement.m_Type1))
                    {
                        editor.Undo.AddComponent(exposeToEditor, requirement.m_Type1);
                    }
                    if (requirement.m_Type2 != null && !go.GetComponent(requirement.m_Type2))
                    {
                        editor.Undo.AddComponent(exposeToEditor, requirement.m_Type2);
                    }
                }
                
                editor.Undo.AddComponent(exposeToEditor, type);
            }

            editor.Undo.EndRecord();       
        }
    }
}
