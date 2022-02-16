using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using Battlehub.UIControls;
using Battlehub.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityObject = UnityEngine.Object;

namespace Battlehub.RTEditor
{
    public class GameObjectEditor : MonoBehaviour
    {
        [SerializeField]
        private BoolEditor IsActiveEditor = null;
        [SerializeField]
        private TMP_InputField InputName = null;
        [SerializeField]
        private OptionsEditor LayerEditor = null;
        [SerializeField]
        private Button EditLayersButton = null;
        [SerializeField]
        private Transform ComponentsPanel = null;

        private class GameObjectWrapper
        {
            private GameObject m_gameObject;

            public bool IsActive
            {
                get 
                {
                    if(!m_gameObject)
                    {
                        return false;
                    }

                    return m_gameObject.activeSelf; 
                }
                set { m_gameObject.SetActive(value); }
            }

            public GameObjectWrapper(GameObject gameObject)
            {
                m_gameObject = gameObject;
            }
        }

        private GameObjectWrapper[] m_selectedGameObjects;

        private IRuntimeEditor m_editor;
        private IEditorsMap m_editorsMap;

        private void Awake()
        {
            m_editorsMap = IOC.Resolve<IEditorsMap>();
            m_editor = IOC.Resolve<IRuntimeEditor>();
            m_editor.Object.ComponentAdded += OnComponentAdded;
            
            GameObject[] selectedObjects = m_editor.Selection.gameObjects;
            InputName.text = GetObjectName(selectedObjects);
            InputName.onEndEdit.AddListener(OnEndEditName);

            m_selectedGameObjects = m_editor.Selection.gameObjects.Select(go => new GameObjectWrapper(go)).ToArray();
            IsActiveEditor.Init(m_selectedGameObjects, Strong.PropertyInfo((GameObjectWrapper x) => x.IsActive), string.Empty);

   
            m_editor.IsBusy = true;
            LayersEditor.LoadLayers(layersInfo =>
            {
                m_editor.IsBusy = false;
                List<RangeOptions.Option> layers = new List<RangeOptions.Option>();

                foreach (LayersInfo.Layer layer in layersInfo.Layers)
                {
                    if(!string.IsNullOrEmpty(layer.Name))
                    {
                        layers.Add(new RangeOptions.Option(string.Format("{0}: {1}", layer.Index, layer.Name), layer.Index));
                    }
                }

                LayerEditor.Options = layers.ToArray();
                LayerEditor.Init(m_editor.Selection.gameObjects, Strong.PropertyInfo((GameObject x) => x.layer), string.Empty);

                List<List<Component>> groups = GetComponentGroups(selectedObjects);
                for (int i = 0; i < groups.Count; ++i)
                {
                    List<Component> group = groups[i];
                    CreateComponentEditor(group);
                }

                UnityEventHelper.AddListener(EditLayersButton, btn => btn.onClick, OnEditLayersClick);
            });
        }

        private void OnDestroy()
        {
            if (InputName != null)
            {
                InputName.onEndEdit.RemoveListener(OnEndEditName);
            }
            
            if (m_editor != null)
            {
                if(m_editor.Object != null)
                {
                    m_editor.Object.ComponentAdded -= OnComponentAdded;
                }
            }

            UnityEventHelper.RemoveListener(EditLayersButton, btn => btn.onClick, OnEditLayersClick);
        }

        private void Update()
        {
            GameObject go = m_editor.Selection.activeGameObject;
            if(go == null)
            {
                return;
            }

            UnityObject[] objects = m_editor.Selection.objects;
            if(objects[0] == null)
            {
                return;
            }

            if (InputName != null && !InputName.isFocused)
            {
                string objectName = GetObjectName(objects);
                if(InputName.text != objectName)
                {
                    InputName.text = objectName;
                }
            }
        }

        /// <summary>
        /// Get object name
        /// </summary>
        /// <param name="objects">objects</param>
        /// <returns>The name of the first object, if all objects have the same name. Otherwise returns null</returns>
        private static string GetObjectName(UnityObject[] objects)
        {
            string name = objects[0].name;
            for(int i = 1; i < objects.Length; ++i)
            {
                UnityObject go = objects[i];
                if(go == null)
                {
                    continue;
                }

                if(go.name != name)
                {
                    return null;
                }
            }
            return name;
        }

        /// <summary>
        /// Get object activeSelf value
        /// </summary>
        /// <param name="objects">objects</param>
        /// <returns>The activeSelf value of the first object, if all objects have the same value of activeSelf. Otherwise returns false</returns>
        private static bool GetActiveSelf(UnityObject[] objects)
        {
            bool activeSelf = ((GameObject)objects[0]).activeSelf;
            for(int i = 1; i < objects.Length; ++i)
            {
                GameObject go = (GameObject)objects[i];
                if(go == null)
                {
                    continue;
                }

                if(go.activeSelf != activeSelf)
                {
                    return false;
                }
            }
            return activeSelf;
        }


        /// <summary>
        /// Check if component valid to be represented by component editor
        /// </summary>
        /// <param name="component">component</param>
        /// <param name="ignoreComponents">list of components which must be ignored</param>
        /// <returns>true if component is valid</returns>
        private static bool IsComponentValid(Component component, HashSet<Component> ignoreComponents)
        {
            if(component == null)
            {
                return false;
            }

            return !ignoreComponents.Contains(component) && (component.hideFlags & HideFlags.HideInInspector) == 0;
        }

        /// <summary>
        /// Find intersection of components of game objects
        /// </summary>
        /// <param name="gameObjects">game objects</param>
        /// <returns>list of groups of components with the same type</returns>
        private static List<List<Component>> GetComponentGroups(GameObject[] gameObjects)
        {
            List<List<Component>> groups = new List<List<Component>>();
            List<List<Component>> allComponents = new List<List<Component>>();
            for(int i = 0; i < gameObjects.Length; ++i)
            {
                GameObject go = gameObjects[i];

                HashSet<Component> ignoreComponents = IgnoreComponents(go);
                allComponents.Add(go.GetComponents<Component>().Where(component => IsComponentValid(component, ignoreComponents)).ToList());
            }

            List<Component> primaryList = allComponents[0];
            for(int i = 0; i < primaryList.Count; ++i)
            {
                Component primary = primaryList[i];
                Type primaryType = primary.GetType();

                List<Component> group = new List<Component>();
                group.Add(primary);

                for(int j = 1; j < allComponents.Count; ++j)
                {
                    List<Component> secondaryList = allComponents[j];
                    if(secondaryList.Count == 0)
                    {
                        //one of the lists is exhausted -> break outer loop
                        i = primaryList.Count;
                        group = null;
                        break;
                    }

                    //find component of type
                    for(int k = 0; k < secondaryList.Count; k++)
                    {
                        Component secondary = secondaryList[k];
                        if(primaryType == secondary.GetType())
                        {
                            group.Add(secondary);
                            secondaryList.RemoveAt(k);
                            break;
                        }
                    }

                    if(group.Count != j + 1)
                    {
                        //not all game objects have a component with the same type
                        group = null;
                        break;
                    }
                }

                if(group != null)
                {
                    //all game objects have a component with the same type
                    groups.Add(group);
                }
            }

            return groups;
        }
        
        private static HashSet<Component> IgnoreComponents(GameObject go)
        {
            ExposeToEditor exposeToEditor = go.GetComponent<ExposeToEditor>();
            HashSet<Component> ignoreComponents = new HashSet<Component>();
            if (exposeToEditor != null)
            {
                if (exposeToEditor.Colliders != null)
                {
                    for (int i = 0; i < exposeToEditor.Colliders.Length; ++i)
                    {
                        Collider collider = exposeToEditor.Colliders[i];
                        if (!ignoreComponents.Contains(collider))
                        {
                            ignoreComponents.Add(collider);
                        }
                    }
                }

                ignoreComponents.Add(exposeToEditor);
            }

            return ignoreComponents;
        }
        
        private bool CreateComponentEditor(List<Component> components)
        {
            if(components.Count == 0)
            {
                return false;
            }

            Component component = components[0];
            Type componentType = component.GetType();
            if (m_editorsMap.IsObjectEditorEnabled(componentType))
            {
                GameObject editorPrefab = m_editorsMap.GetObjectEditor(componentType);
                if (editorPrefab != null)
                {
                    ComponentEditor componentEditorPrefab = editorPrefab.GetComponent<ComponentEditor>();
                    if (componentEditorPrefab != null)
                    {
                        ComponentEditor editor = Instantiate(componentEditorPrefab);
                        editor.EndEditCallback = () =>
                        {
                            UpdatePreviews(components);
                        };
                        editor.transform.SetParent(ComponentsPanel, false);
                        editor.Components = components.ToArray();
                        return true;
                    }
                    else
                    {
                        Debug.LogErrorFormat("editor prefab {0} does not have ComponentEditor script", editorPrefab.name);
                        return false;
                    }
                }
            }

            return false;
        }

        private void UpdatePreviews(List<Component> components)
        {
            for (int i = 0; i < components.Count; ++i)
            {
                Component component = components[i];
                if (component != null && component.gameObject != null)
                {
                    m_editor.UpdatePreview(component);
                }
            }
            m_editor.IsDirty = true;
        }

        private void OnEndEditName(string name)
        {
            GameObject[] gameObjects = m_editor.Selection.gameObjects;
            for(int i = 0; i < gameObjects.Length; ++i)
            {
                GameObject go = gameObjects[i];
                if(go == null)
                {
                    continue;
                }

                ExposeToEditor exposeToEditor = go.GetComponent<ExposeToEditor>();
                if (exposeToEditor != null)
                {
                    exposeToEditor.SetName(name, true);
                }
                else
                {
                    go.name = name;
                }
            }    
        }

        private void OnComponentAdded(ExposeToEditor obj, Component component)
        {
            if(component == null)
            {
                IWindowManager wnd = IOC.Resolve<IWindowManager>();
                wnd.MessageBox("Unable to add component", "Component was not added");
            }
            else
            {
                if (!m_editor.Selection.IsSelected(component.gameObject))
                {
                    return;
                }

                if (m_editor.Selection.activeGameObject == null)
                {
                    return;
                }

                HashSet<Component> ignoreComponents = IgnoreComponents(obj.gameObject);
                if(!IsComponentValid(component, ignoreComponents))
                {
                    return;
                }

                GameObject[] gameObjects = m_editor.Selection.gameObjects;
                if (gameObjects.Length == 1)
                {
                    CreateComponentEditor(new List<Component> { component });
                }
                else
                {
                    if (gameObjects[gameObjects.Length - 1] != component.gameObject)
                    {
                        return;
                    }

                    List<List<Component>> groups = GetComponentGroups(gameObjects);
                    for (int i = 0; i < groups.Count; ++i)
                    {
                        List<Component> group = groups[i];

                        //This is to handle case when AddComponent called on multiple objects. 
                        //See InspectorView.cs void OnAddComponent(Type type) method for details.
                        if (group[group.Count - 1] == component)
                        {
                            CreateComponentEditor(group);
                            break;
                        }
                    }
                }
            }
        }

        private void OnEditLayersClick()
        {
            LayersEditor.BeginEdit();
        }
    }
}

