﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Linq;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Battlehub.RTEditor
{
    public class EditorDescriptor
    {
        public Type Type;
        public bool Enabled;
        public GameObject Editor;
        public bool IsPropertyEditor;

        public EditorDescriptor(Type type, bool enabled, GameObject editor, bool isPropertyEditor)
        {
            Type = type;
            Enabled = enabled;
            Editor = editor;
            IsPropertyEditor = isPropertyEditor;
        }
    }

    public class MaterialEditorDescriptor
    {
        public Shader Shader;
        public bool Enabled;
        public GameObject Editor;

        public MaterialEditorDescriptor(Shader shader, bool enabled, GameObject editor)
        {
            Shader = shader;
            Enabled = enabled;
            Editor = editor;
        }
    }


    public class EditorsMapWindow : EditorWindow
    {
        [MenuItem("Tools/Runtime Editor/Configuration")]
        public static void ShowMenuItem()
        {
            ShowWindow();
        }

        public static void ShowWindow()
        {
            EditorsMapWindow prevWindow = GetWindow<EditorsMapWindow>();
            if (prevWindow != null)
            {
                prevWindow.Close();
            }

            EditorsMapWindow window = CreateInstance<EditorsMapWindow>();
            window.titleContent = new GUIContent("RTE Config");
            window.Show();
            window.position = new Rect(200, 200, 600, 600);
        }

        private EditorsMapStorage m_map;
        private EditorDescriptor[] m_objectEditorDescriptors;
        private EditorDescriptor[] m_propertyEditorDescriptors;
        private EditorDescriptor[] m_stdComponentEditorDescriptors;
        private EditorDescriptor[] m_scriptEditorDescriptors;
        private MaterialEditorDescriptor[] m_materialDescriptors;
        
        private void Awake()
        {
            Init();
        }

        private void GetUOAssembliesAndTypes(out Assembly[] assemblies, out Type[] types)
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.FullName.Contains("UnityEditor") && !a.FullName.Contains("Assembly-CSharp-Editor")).OrderBy(a => a.FullName).ToArray();

            List<Type> allUOTypes = new List<Type>();
            List<Assembly> assembliesList = new List<Assembly>();

            for (int i = 0; i < assemblies.Length; ++i)
            {
                Assembly assembly = assemblies[i];
                Type[] uoTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(UnityEngine.Object)) && !t.IsGenericType).ToArray();
                if (uoTypes.Length > 0)
                {
                    assembliesList.Add(assembly);
                    allUOTypes.AddRange(uoTypes);
                }
            }

            types = allUOTypes.OrderByDescending(t => t.FullName.Contains("UnityEngine")).ToArray();
            assemblies = new Assembly[] { null }.Union(assembliesList.OrderBy(a => a.FullName)).ToArray();
        }


        private void Init()
        {
            m_map = Resources.Load<EditorsMapStorage>(EditorsMapStorage.EditorsMapPrefabName);
            if(m_map == null)
            {
                m_map = Resources.Load<EditorsMapStorage>(EditorsMapStorage.EditorsMapTemplateName);
            }

            GameObject editorsMapGo = new GameObject();
            EditorsMap editorsMap = editorsMapGo.AddComponent<EditorsMap>();
            editorsMap.LoadMap();

            Assembly[] allAssemblies;
            Type[] types;
            GetUOAssembliesAndTypes(out allAssemblies, out types);

            Assembly[] unityAssemblies = allAssemblies.Where(a => a != null && a.FullName != null && a.FullName.Contains("UnityEngine")).ToArray();
            Assembly[] otherAssemblies = allAssemblies.Where(a => a != null && a.FullName != null && !a.FullName.Contains("UnityEngine")).ToArray();

            m_objectEditorDescriptors = new[] { typeof(GameObject), typeof(LayersInfo) }
                .Where(t => t.IsPublic && !t.IsGenericType)
                .Select(t => new EditorDescriptor(t, m_map != null && editorsMap.IsObjectEditorEnabled(t), m_map != null ? editorsMap.GetObjectEditor(t, true) : null, false)).ToArray();
            m_propertyEditorDescriptors = new[] { typeof(object), typeof(UnityEngine.Object), typeof(bool), typeof(Enum), typeof(List<>), typeof(Array), typeof(string), typeof(int), typeof(float), typeof(Range), typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Quaternion), typeof(Color), typeof(Bounds), typeof(RangeInt), typeof(RangeOptions), typeof(HeaderText), typeof(MethodInfo), typeof(RangeFlags), typeof(LayerMask) }
                .Where(t =>  t.IsPublic)
                .Select(t => new EditorDescriptor(t, m_map != null && editorsMap.IsPropertyEditorEnabled(t), m_map != null ? editorsMap.GetPropertyEditor(t, true) : null, true)).ToArray();
            m_stdComponentEditorDescriptors = unityAssemblies.SelectMany(a => a.GetTypes())
                .Where(t => typeof(Component).IsAssignableFrom(t) && t.IsPublic && !t.IsGenericType)
                .OrderBy(t => (t == typeof(Component)) ? string.Empty : t.Name)
                .Select(t => new EditorDescriptor(t, m_map != null && editorsMap.IsObjectEditorEnabled(t), m_map != null ? editorsMap.GetObjectEditor(t, true) : null, false)).ToArray();
            m_scriptEditorDescriptors = otherAssemblies.SelectMany(a => a.GetTypes())
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) && t.IsPublic && !t.IsGenericType)
                .OrderBy(t => t.FullName)
                .Select(t => GetScriptEditorDescriptor(t, editorsMap)).ToArray();

            List<Material> materials = new List<Material>();
            string[] assets = AssetDatabase.GetAllAssetPaths();
            foreach(string asset in assets)
            {
                if(!asset.EndsWith(".mat"))
                {
                    continue;
                }
                Material material = AssetDatabase.LoadAssetAtPath<Material>(asset);

                if (material == null ||
                   (material.hideFlags & HideFlags.DontSaveInBuild) != 0 ||
                    material.shader == null ||
                   (material.shader.hideFlags & HideFlags.DontSaveInBuild) != 0)
                {
                    continue;
                }
                materials.Add(material);

            }
            MaterialEditorDescriptor[] defaultDescriptors = new[] { new MaterialEditorDescriptor(null, m_map != null && m_map.IsDefaultMaterialEditorEnabled, m_map != null ? m_map.DefaultMaterialEditor : null) };
            MaterialEditorDescriptor[] materialDescriptors = materials.Where(m => m.shader != null && !m.shader.name.StartsWith("Hidden/"))
                .Select(m => m.shader).Distinct()
                .OrderBy(s => s.name.StartsWith("Standard") ? string.Empty : s.name)
                .Select(s => new MaterialEditorDescriptor(s, m_map != null && editorsMap.IsMaterialEditorEnabled(s), m_map != null ? editorsMap.GetMaterialEditor(s, true) : null)).ToArray();

            m_materialDescriptors = defaultDescriptors.Union(materialDescriptors).ToArray();

            DestroyImmediate(editorsMapGo);
        }

        private EditorDescriptor GetScriptEditorDescriptor(Type t, EditorsMap editorsMap)
        {
            bool isPropertyEditorType = typeof(PropertyEditor).IsAssignableFrom(t);
            bool isEditorEnabled = false;
            GameObject editor = null;
            if(m_map != null)
            {
                if (isPropertyEditorType)
                {
                    isEditorEnabled = editorsMap.IsPropertyEditorEnabled(t, true);
                    editor = editorsMap.GetPropertyEditor(t, true);
                }
                else
                {
                    isEditorEnabled = editorsMap.IsObjectEditorEnabled(t);
                    editor = editorsMap.GetObjectEditor(t, true);
                }
            }
            return new EditorDescriptor(t, isEditorEnabled, editor, isPropertyEditorType);  
        }

        private bool m_objectsGroup = true;
        private bool m_propertiesGroup = true;
        private bool m_stdComponentsGroup = true;
        private bool m_scriptComponentsGroup = true;
        private bool m_materialsGroup = true;
        private Vector2 m_position;
        private void OnGUI()
        {
            if (m_objectEditorDescriptors == null || m_propertyEditorDescriptors == null || m_stdComponentEditorDescriptors == null || m_scriptEditorDescriptors == null)
            {
                Init();
            }

            EditorGUILayout.Separator();
            GUILayout.Label("This window allows you to manage mappings between object types and editors");
            EditorGUILayout.Separator();
            if (GUILayout.Button("Save Editors Map"))
            {
                if (Application.isPlaying)
                {
                    EditorUtility.DisplayDialog("Unable to create Editors Map", "Application.isPlaying == true", "OK");
                }
                else
                {
                    EditorsMapGen.Generate(m_objectEditorDescriptors
                        .Union(m_propertyEditorDescriptors)
                        .Union(m_stdComponentEditorDescriptors)
                        .Union(m_scriptEditorDescriptors).ToArray(), 
                        m_materialDescriptors);
                    Close();
                }
            }
            EditorGUILayout.Separator();
            m_position = EditorGUILayout.BeginScrollView(m_position);
            ShowGroup(ref m_objectsGroup, "Object Editors", m_objectEditorDescriptors, d => d.Type.Name);
            EditorGUILayout.Separator();
            ShowGroup(ref m_propertiesGroup, "Property Editors", m_propertyEditorDescriptors, d =>
            {
                if(d.Type == typeof(object))
                {
                    return "Custom Type";
                }
                return d.Type.Name;
            });
            EditorGUILayout.Separator();
            ShowGroup(ref m_materialsGroup, "Material Editors", m_materialDescriptors, d => d.Shader != null ? d.Shader.name : "Default");
            EditorGUILayout.Separator();
            ShowGroup(ref m_stdComponentsGroup, "Standard Component Editors", m_stdComponentEditorDescriptors, d => d.Type.Name);
            EditorGUILayout.Separator();
            ShowGroup(ref m_scriptComponentsGroup, "Script Editors", m_scriptEditorDescriptors, d => d.Type.FullName);
            EditorGUILayout.EndScrollView();
        }

        private void ShowGroup(ref bool isExpanded, string label, EditorDescriptor[] descriptors, Func<EditorDescriptor, string> getName)
        {
            EditorGUIUtility.labelWidth = 350;
            isExpanded = EditorGUILayout.Foldout(isExpanded, label);
            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                for (int i = 0; i < descriptors.Length; ++i)
                {
                    EditorDescriptor descriptor = descriptors[i];
                    descriptor.Enabled = EditorGUILayout.Toggle(getName(descriptor), descriptor.Enabled);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                for (int i = 0; i < descriptors.Length; ++i)
                {
                    EditorDescriptor descriptor = descriptors[i];
                    descriptor.Editor = (GameObject)EditorGUILayout.ObjectField(descriptor.Editor, typeof(GameObject), false);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            
            EditorGUIUtility.labelWidth = 0;
        }

        private void ShowGroup(ref bool isExpanded, string label, MaterialEditorDescriptor[] descriptors, Func<MaterialEditorDescriptor, string> getName)
        {
            EditorGUIUtility.labelWidth = 350;
            isExpanded = EditorGUILayout.Foldout(isExpanded, label);
            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                for (int i = 0; i < descriptors.Length; ++i)
                {
                    MaterialEditorDescriptor descriptor = descriptors[i];
                    descriptor.Enabled = EditorGUILayout.Toggle(getName(descriptor), descriptor.Enabled);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                for (int i = 0; i < descriptors.Length; ++i)
                {
                    MaterialEditorDescriptor descriptor = descriptors[i];
                    descriptor.Editor = (GameObject)EditorGUILayout.ObjectField(descriptor.Editor, typeof(GameObject), false);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }


            EditorGUIUtility.labelWidth = 0;
        }
    }
}
#endif
