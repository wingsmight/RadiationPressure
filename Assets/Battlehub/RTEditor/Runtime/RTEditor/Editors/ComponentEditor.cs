using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using UnityEngine.UI;
using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using TMPro;
using System.Collections.Generic;


namespace Battlehub.RTEditor
{
    public struct PropertyDescriptor
    {
        public string Label;
        public string AnimationPropertyName;
        public MemberInfo MemberInfo;
        public MemberInfo ComponentMemberInfo;
        public PropertyEditorCallback ValueChangedCallback;
        public PropertyEditorCallback EndEditCallback;
        public Range Range;
        public PropertyDescriptor[] ChildDesciptors;
     
        public Type MemberType
        {
            get
            {
                if(Range != null)
                {
                    return Range.GetType();
                }

                if (MemberInfo is PropertyInfo)
                {
                    PropertyInfo prop = (PropertyInfo)MemberInfo;
                    return prop.PropertyType;
                }
                else if (MemberInfo is FieldInfo)
                {
                    FieldInfo field = (FieldInfo)MemberInfo;
                    return field.FieldType;
                }

                return null;
            }
        }

        public Type ComponentMemberType
        {
            get
            {
                if (ComponentMemberInfo is PropertyInfo)
                {
                    PropertyInfo prop = (PropertyInfo)ComponentMemberInfo;
                    return prop.PropertyType;
                }
                else if (ComponentMemberInfo is FieldInfo)
                {
                    FieldInfo field = (FieldInfo)ComponentMemberInfo;
                    return field.FieldType;
                }

                return null;
            }
        }

        public object Target
        {
            get { return Targets != null && Targets.Length > 0 ? Targets[0] : null; }
            set
            {
                if (value == null)
                {
                    Targets = null;
                }
                else
                {
                    Targets = new[] { value };
                }
            }
        }

        public object[] Targets;
        
        public PropertyDescriptor(string label, object[] targets, MemberInfo memberInfo) : this(label, targets, memberInfo, memberInfo.Name) {}

        public PropertyDescriptor(string label, object[] targets, MemberInfo memberInfo, string animationPropertyName)
            : this(label, targets, memberInfo, memberInfo)
        {
            AnimationPropertyName = animationPropertyName;
        }

        public PropertyDescriptor(string label, object[] targets, MemberInfo memberInfo, MemberInfo componentMemberInfo) 
            : this(label, targets, memberInfo, componentMemberInfo, null)
        {
        }

        public PropertyDescriptor(string label, object[] targets, MemberInfo memberInfo, MemberInfo componentMemberInfo, PropertyEditorCallback valueChangedCallback)
            : this(label, targets, memberInfo, componentMemberInfo, valueChangedCallback, (PropertyEditorCallback)null)
        { 
        }

        public PropertyDescriptor(string label, object[] targets, MemberInfo memberInfo, MemberInfo componentMemberInfo, PropertyEditorCallback valueChangedCallback, Range range)
             : this(label, targets, memberInfo, componentMemberInfo, valueChangedCallback, (PropertyEditorCallback)null)
        {
            Range = range;
        }

        public PropertyDescriptor(string label, object[] targets, MemberInfo memberInfo, MemberInfo componentMemberInfo, PropertyEditorCallback valueChangedCallback, PropertyEditorCallback endEditCallback)
        {
            MemberInfo = memberInfo;
            ComponentMemberInfo = componentMemberInfo;
            Label = label;
            Targets = targets;
            ValueChangedCallback = valueChangedCallback;
            EndEditCallback = endEditCallback;
            Range = TryGetRange(memberInfo);
            ChildDesciptors = null;
            AnimationPropertyName = null;
        }

        public PropertyDescriptor(string label, object target, MemberInfo memberInfo) 
            : this(label, new[] { target }, memberInfo, memberInfo.Name) { }

        public PropertyDescriptor(string label, object target, MemberInfo memberInfo, string animationPropertyName)
            : this(label, new[] { target }, memberInfo, memberInfo) { }

        public PropertyDescriptor(string label, object target, MemberInfo memberInfo, MemberInfo componentMemberInfo)
            : this(label, new[] { target }, memberInfo, componentMemberInfo, null) { }

        public PropertyDescriptor(string label, object target, MemberInfo memberInfo, MemberInfo componentMemberInfo, PropertyEditorCallback valueChangedCallback)
            : this(label, new[] { target }, memberInfo, componentMemberInfo, valueChangedCallback, (PropertyEditorCallback)null) { }

        public PropertyDescriptor(string label, object target, MemberInfo memberInfo, MemberInfo componentMemberInfo, PropertyEditorCallback valueChangedCallback, Range range)
             : this(label, new[] { target }, memberInfo, componentMemberInfo, valueChangedCallback, (PropertyEditorCallback)null) { }

        public PropertyDescriptor(string label, object target, MemberInfo memberInfo, MemberInfo componentMemberInfo, PropertyEditorCallback valueChangedCallback, PropertyEditorCallback endEditCallback)
            : this(label, new[] { target }, memberInfo, componentMemberInfo, valueChangedCallback, endEditCallback) { }

        private static Range TryGetRange(MemberInfo memberInfo)
        {
            RangeAttribute range = memberInfo.GetCustomAttribute<RangeAttribute>();
            if (range != null)
            {
                if (memberInfo.GetUnderlyingType() == typeof(int))
                {
                    return new RangeInt((int)range.min, (int)range.max);
                }
                else if (memberInfo.GetUnderlyingType() == typeof(float))
                {
                    return new Range(range.min, range.max);
                }
            }
            return null;
        }
    }

    public class VoidComponentEditor : ComponentEditor
    {
        public override Component[] Components
        {
            get { return m_components; }
            set { m_components = value; }
        }

        protected override void Update()
        {
        }
    }

    public class ComponentEditor : MonoBehaviour
    {
        /// <summary>
        /// Used to update previews
        /// </summary>
        public PropertyEditorCallback EndEditCallback;

        [SerializeField]
        protected Transform EditorsPanel = null;
        [SerializeField]
        private BoolEditor EnabledEditor = null;
        [SerializeField]
        private TextMeshProUGUI Header = null;
        [SerializeField]
        private Toggle Expander = null;
        [SerializeField]
        private GameObject ExpanderGraphics = null;
        [SerializeField]
        private Button ResetButton = null;
        [SerializeField]
        private Button RemoveButton = null;

        private object m_converter;

        private Component[] m_gizmos;

        private bool IsComponentExpanded
        {
            get
            {
                string componentName = "BH_CE_EX_" + ComponentType.AssemblyQualifiedName;
                return PlayerPrefs.GetInt(componentName, 1) == 1;
            }
            set
            {
                string componentName = "BH_CE_EX_" + ComponentType.AssemblyQualifiedName;
                PlayerPrefs.SetInt(componentName, value ? 1 : 0);
            }
        }

        public virtual Component Component
        {
            get { return m_components != null && m_components.Length > 0 ? m_components[0] : null; }
            set
            {
                if(value == null)
                {
                    throw new ArgumentNullException("value");
                }
                Components = new[] { value };    
            }
        }

        protected Type ComponentType
        {
            get { return m_components[0].GetType(); }
        }

        public IEnumerable<Component> NotNullComponents
        {
            get
            {
                if(Components == null)
                {
                    yield break;
                }

                foreach(Component component in Components)
                {
                    if(component != null)
                    {
                        yield return component;
                    }
                }
            }
        }

        protected Component[] m_components;
        public virtual Component[] Components
        {
            get { return m_components; }
            set
            {
                m_components = value;
                if (m_components == null || m_components.Length == 0)
                {
                    throw new ArgumentNullException("value");
                }

                IComponentDescriptor componentDescriptor = GetComponentDescriptor();
                if (EnabledEditor != null)
                {
                    PropertyInfo enabledProperty = EnabledProperty;
                    if (enabledProperty != null && (componentDescriptor == null || componentDescriptor.GetHeaderDescriptor(m_editor).ShowEnableButton))
                    {
                        EnabledEditor.gameObject.SetActive(true);
                        EnabledEditor.Init(Components, Components, enabledProperty, null, string.Empty, () => { },
                            () => CreateOrDestroyGizmos(componentDescriptor),
                            () =>
                            {
                                if (EndEditCallback != null)
                                {
                                    EndEditCallback();
                                }
                            }, 
                            true, null, null, null,
                            () => CreateOrDestroyGizmos(componentDescriptor), () => CreateOrDestroyGizmos(componentDescriptor));
                    }
                    else
                    {
                        EnabledEditor.gameObject.SetActive(false);
                    }
                }

                if (Header != null)
                {
                    if (componentDescriptor != null)
                    {
                        Header.text = componentDescriptor.GetHeaderDescriptor(m_editor).DisplayName;
                    }
                    else
                    {
                        string typeName = ComponentType.Name;
                        ILocalization localization = IOC.Resolve<ILocalization>();
                        Header.text = localization.GetString("ID_RTEditor_CD_" + typeName, typeName);
                    }
                }

                if (Expander != null)
                {
                    Expander.isOn = IsComponentExpanded;
                }

                BuildEditor();
            }
        }

        private bool IsComponentEnabled
        {
            get
            {
                if (EnabledProperty == null)
                {
                    return true;
                }

                //TODO: Handle mixed values
                object v = EnabledProperty.GetValue(Components[0], null); 
                if (v is bool)
                {
                    bool isEnabled = (bool)v;
                    return isEnabled;
                }
                return true;
            }   
        }

        protected PropertyInfo EnabledProperty
        {
            get
            {
                Type type = ComponentType;

                while(type != typeof(UnityEngine.Object))
                {
                    PropertyInfo prop = type.GetProperty("enabled", BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                    if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead && prop.CanWrite)
                    {
                        return prop;
                    }
                    type = type.BaseType();
                }

                return null;
            }
        }

        private IRTE m_editor;
        public IRTE Editor
        {
            get { return m_editor; }
        }

        private IProject m_project;
        private IEditorsMap m_editorsMap;

        protected virtual void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            if(m_editor.Object != null)
            {
                m_editor.Object.ReloadComponentEditor += OnReloadComponentEditor;
            }
            m_project = IOC.Resolve<IProject>();
            m_editorsMap = IOC.Resolve<IEditorsMap>();
            

#pragma warning disable CS0612
            AwakeOverride();
#pragma warning restore CS0612
        }

        protected virtual void Start()
        {
            if (Components == null || Components.Length == 0)
            {
                return;
            }

            if(Expander != null)
            {
                Expander.onValueChanged.AddListener(OnExpanded);
            }
            
            if(ResetButton != null)
            {
                ResetButton.onClick.AddListener(OnResetClick);
            }

            if(RemoveButton != null)
            {
                RemoveButton.onClick.AddListener(OnRemove);
            }

            m_editor.Object.ReloadComponentEditor -= OnReloadComponentEditor;
            m_editor.Object.ReloadComponentEditor += OnReloadComponentEditor;
            m_editor.Undo.UndoCompleted += OnUndoCompleted;
            m_editor.Undo.RedoCompleted += OnRedoCompleted;
            m_editor.WindowRegistered += OnWindowRegistered;
            m_editor.WindowUnregistered += OnWindowUnregistered;
            m_editor.BeforePlaymodeStateChange += OnBeforePlayModeStateChange;
            
#pragma warning disable CS0612
            StartOverride();
#pragma warning restore CS0612
        }

        protected virtual void OnDestroy()
        {
            if(m_editor != null)
            {
                m_editor.Undo.UndoCompleted -= OnUndoCompleted;
                m_editor.Undo.RedoCompleted -= OnRedoCompleted;
                m_editor.WindowRegistered -= OnWindowRegistered;
                m_editor.WindowUnregistered -= OnWindowUnregistered;
                m_editor.BeforePlaymodeStateChange -= OnBeforePlayModeStateChange;

                if (m_editor.Object != null)
                {
                    m_editor.Object.ReloadComponentEditor -= OnReloadComponentEditor;
                }
            }

            if (Expander != null)
            {
                Expander.onValueChanged.RemoveListener(OnExpanded);
            }

            if(ResetButton != null)
            {
                ResetButton.onClick.RemoveListener(OnResetClick);
            }

            if (RemoveButton != null)
            {
                RemoveButton.onClick.RemoveListener(OnRemove);
            }

            if (m_gizmos != null)
            {
                for(int i = 0; i < m_gizmos.Length; ++i)
                {
                    Destroy(m_gizmos[i]);
                }
                m_gizmos = null;
            }
#pragma warning disable CS0612
            OnDestroyOverride();
#pragma warning restore CS0612
        }

        protected virtual void Update()
        {
            if (Components == null || Components.Length == 0 || Components[0] == null)
            {
                Destroy(gameObject);
            }

#pragma warning disable CS0612
            UpdateOverride();
#pragma warning restore CS0612
        }

        protected IComponentDescriptor GetComponentDescriptor()
        {
            IComponentDescriptor componentDescriptor;
            if (m_editorsMap.ComponentDescriptors.TryGetValue(ComponentType, out componentDescriptor))
            {
                return componentDescriptor;
            }
            return null;
        }

        public void BuildEditor()
        {
            IComponentDescriptor componentDescriptor = GetComponentDescriptor();
            if (componentDescriptor != null)
            {
                m_converter = componentDescriptor.CreateConverter(this);
            }

            PropertyDescriptor[] descriptors = m_editorsMap.GetPropertyDescriptors(ComponentType, this, m_converter);
            if (descriptors == null || descriptors.Length == 0)
            {
                if(ExpanderGraphics != null)
                {
                    ExpanderGraphics.SetActive(false);
                }
                
                return;
            }

            ISettingsComponent settingsComponent = IOC.Resolve<ISettingsComponent>();
            BuiltInWindowsSettings settings;
            if (settingsComponent == null)
            {
                settings = BuiltInWindowsSettings.Default;
            }
            else
            {
                settings = settingsComponent.BuiltInWindowsSettings;
            }

            if (ResetButton != null)
            {
                ResetButton.gameObject.SetActive(componentDescriptor != null ?
                    componentDescriptor.GetHeaderDescriptor(m_editor).ShowResetButton :
                    settings.Inspector.ComponentEditor.ShowResetButton);
            }

            if (RemoveButton != null)
            {
                bool showRemoveButton = componentDescriptor != null ?
                    componentDescriptor.GetHeaderDescriptor(m_editor).ShowRemoveButton :
                    settings.Inspector.ComponentEditor.ShowRemoveButton;
                if (showRemoveButton)
                {
                    bool canRemove = m_project == null || m_project.ToAssetItem(Components[0].gameObject) == null;
                    if (!canRemove)
                    {
                        showRemoveButton = false;
                    }
                }

                RemoveButton.gameObject.SetActive(showRemoveButton);
            }

            if (EnabledEditor != null && EnabledProperty != null)
            {
                EnabledEditor.gameObject.SetActive(componentDescriptor != null ?
                    componentDescriptor.GetHeaderDescriptor(m_editor).ShowEnableButton :
                    settings.Inspector.ComponentEditor.ShowEnableButton);
            }

            if (Expander == null)
            {
                BuildEditor(componentDescriptor, descriptors);
            }
            else
            {
                if (componentDescriptor != null ? !componentDescriptor.GetHeaderDescriptor(m_editor).ShowExpander : !settings.Inspector.ComponentEditor.ShowExpander)
                {
                    Expander.isOn = true;
                    Expander.enabled = false;
                }
                
                if (Expander.isOn)
                {
                    if (ExpanderGraphics != null)
                    {
                        ExpanderGraphics.SetActive(componentDescriptor != null ? componentDescriptor.GetHeaderDescriptor(m_editor).ShowExpander : settings.Inspector.ComponentEditor.ShowExpander);
                    }
                    BuildEditor(componentDescriptor, descriptors);
                }
            }
        }

        protected virtual void BuildEditor(IComponentDescriptor componentDescriptor, PropertyDescriptor[] descriptors)
        {
            DestroyEditor();
            TryCreateGizmos(componentDescriptor);

            for (int i = 0; i < descriptors.Length; ++i)
            {
                PropertyDescriptor descriptor = descriptors[i];
                if (descriptor.MemberInfo == EnabledProperty)
                {
                    continue;
                }
                BuildPropertyEditor(descriptor);
            }
        }

        private PropertyEditor InstantiatePropertyEditor(PropertyDescriptor descriptor)
        {
            if (descriptor.MemberInfo == null)
            {
                Debug.LogError("desciptor.MemberInfo is null");
                return null;
            }

            Type memberType;
            if (descriptor.MemberInfo is MethodInfo)
            {
                memberType = typeof(MethodInfo);
            }
            else
            {
                memberType = descriptor.MemberType;
            }

            if (memberType == null)
            {
                Debug.LogError("descriptor.MemberType is null");
                return null;
            }

            GameObject editorGo = m_editorsMap.GetPropertyEditor(memberType);
            if (editorGo == null)
            {
                return null;
            }

            if (!m_editorsMap.IsPropertyEditorEnabled(memberType))
            {
                return null;
            }
            PropertyEditor editor = editorGo.GetComponent<PropertyEditor>();
            if (editor == null)
            {
                Debug.LogErrorFormat("editor {0} is not PropertyEditor", editorGo);
                return null;
            }
            PropertyEditor instance = Instantiate(editor);
            instance.transform.SetParent(EditorsPanel, false);
            return instance;
        }

        protected virtual void BuildPropertyEditor(PropertyDescriptor descriptor)
        {
            PropertyEditor editor = InstantiatePropertyEditor(descriptor);
            if (editor == null)
            {
                return;
            }
            if (descriptor.Range != null)
            {
                if (descriptor.Range is RangeInt)
                {
                    RangeIntEditor rangeEditor = editor as RangeIntEditor;
                    rangeEditor.Min = (int)descriptor.Range.Min;
                    rangeEditor.Max = (int)descriptor.Range.Max;
                }
                else if (descriptor.Range is RangeFlags)
                {
                    RangeFlags range = (RangeFlags)descriptor.Range;
                    FlagsIntEditor flagsEditor = editor as FlagsIntEditor;
                    flagsEditor.Options = range.Options;
                }
                else if (descriptor.Range is RangeOptions)
                {
                    RangeOptions range = (RangeOptions)descriptor.Range;
                    OptionsEditor optionsEditor = editor as OptionsEditor;
                    optionsEditor.Options = range.Options;

                }
                else
                {
                    RangeEditor rangeEditor = editor as RangeEditor;
                    rangeEditor.Min = descriptor.Range.Min;
                    rangeEditor.Max = descriptor.Range.Max;
                }   
            }

            InitEditor(editor, descriptor);
        }

        //Better name for this is InitPropertyEditor
        protected virtual void InitEditor(PropertyEditor editor, PropertyDescriptor descriptor)
        {
            editor.Init(descriptor.Targets, descriptor.Targets, descriptor.MemberInfo, null, descriptor.Label, null, () => { descriptor.ValueChangedCallback?.Invoke(); OnValueChanged(); }, () => {  descriptor.EndEditCallback?.Invoke(); EndEditCallback?.Invoke(); OnEndEdit(); }, true, descriptor.ChildDesciptors, null, null, null, null, OnValueReloaded);
        }

        protected virtual void OnValueChanged()
        {
        }

        protected virtual void OnEndEdit()
        {
        }

        protected virtual void OnValueReloaded()
        {

        }

        protected virtual void DestroyEditor()
        {
            DestroyGizmos();
            foreach (Transform t in EditorsPanel)
            {
                Destroy(t.gameObject);
            }
        }

        protected virtual void OnWindowRegistered(RuntimeWindow window)
        {
            if (window.WindowType == RuntimeWindowType.Scene)
            {
                List<Component> gizmos = m_gizmos != null ? m_gizmos.ToList() : new List<Component>();
                TryCreateGizmos(GetComponentDescriptor(), gizmos, window);
                if(gizmos.Count > 0)
                {
                    m_gizmos = gizmos.ToArray();
                }
            }
        }

        protected virtual void OnWindowUnregistered(RuntimeWindow window)
        {
            if (window.WindowType == RuntimeWindowType.Scene && m_gizmos != null)
            {
                List<Component> gizmos = m_gizmos.ToList();

                for(int i = gizmos.Count - 1; i >= 0; i--)
                {
                    RTEComponent rteComponent = gizmos[i] as RTEComponent;
                    if(rteComponent != null && rteComponent.Window == window)
                    {
                        DestroyImmediate(rteComponent);
                        gizmos.RemoveAt(i);
                    }
                }
                
                m_gizmos = gizmos.ToArray();
            }
        }


        private void OnBeforePlayModeStateChange()
        {
            DestroyGizmos();
        }

        private void CreateOrDestroyGizmos(IComponentDescriptor componentDescriptor)
        {
            if (IsComponentEnabled)
            {
                TryCreateGizmos(componentDescriptor);
            }
            else
            {
                DestroyGizmos();
            }
        }

        protected virtual void TryCreateGizmos(IComponentDescriptor componentDescriptor)
        {
            if (componentDescriptor != null && componentDescriptor.GizmoType != null && IsComponentEnabled)
            {
                List<Component> gizmos = new List<Component>();
                RuntimeWindow[] windows = m_editor.Windows;
                for(int i = 0; i < windows.Length; ++i)
                {
                    RuntimeWindow window = windows[i];
                    if(window.WindowType == RuntimeWindowType.Scene)
                    {
                        TryCreateGizmos(componentDescriptor, gizmos, window);
                    }
                }

                m_gizmos = gizmos.ToArray();
            }
        }

        protected virtual void TryCreateGizmos(IComponentDescriptor componentDescriptor, List<Component> gizmos, RuntimeWindow window)
        {
            if (componentDescriptor != null && componentDescriptor.GizmoType != null && IsComponentEnabled && Expander.isOn)
            {
                for (int j = 0; j < Components.Length; ++j)
                {
                    Component component = Components[j];
                    if (component != null)
                    {
                        Component gizmo = component.gameObject.AddComponent(componentDescriptor.GizmoType);
                        if (gizmo is RTEComponent)
                        {
                            RTEComponent baseGizmo = (RTEComponent)gizmo;
                            baseGizmo.Window = window;
                        }

                        gizmo.SendMessageUpwards("Reset", SendMessageOptions.DontRequireReceiver);
                        gizmos.Add(gizmo);
                    }
                }
            }
        }

        protected virtual void DestroyGizmos()
        {
            if (m_gizmos != null)
            {
                for (int i = 0; i < m_gizmos.Length; ++i)
                {
                    Component gizmo = m_gizmos[i];
                    if(gizmo != null)
                    {
                        DestroyImmediate(gizmo);
                    }
                }
                m_gizmos = null;
            }
        }

        private void OnExpanded(bool expanded)
        {
            IsComponentExpanded = expanded;
            if (expanded)
            {
                IComponentDescriptor componentDescriptor = GetComponentDescriptor();
                PropertyDescriptor[] descriptors = m_editorsMap.GetPropertyDescriptors(ComponentType, this, m_converter);
                if(ExpanderGraphics != null)
                {
                    ExpanderGraphics.SetActive(true);
                }
                
                BuildEditor(componentDescriptor, descriptors);
            }
            else
            {
                DestroyEditor();
            }
        }

        private PropertyEditor GetPropertyEditor(MemberInfo memberInfo)
        {
            foreach(Transform t in EditorsPanel)
            {
                PropertyEditor propertyEditor = t.GetComponent<PropertyEditor>();
                if(propertyEditor != null && propertyEditor.MemberInfo == memberInfo)
                {
                    return propertyEditor;
                }
            }
            return null;
        }

        private void OnRedoCompleted()
        {
            ReloadEditors(false);
        }

        private void OnUndoCompleted()
        {
            ReloadEditors(false);
        }

        private void OnReloadComponentEditor(ExposeToEditor obj, Component component, bool force)
        {
            if(component == Component)
            {
                ReloadEditors(force);
            }
        }

        private void ReloadEditors(bool force)
        {
            foreach (Transform t in EditorsPanel)
            {
                PropertyEditor propertyEditor = t.GetComponent<PropertyEditor>();
                if (propertyEditor != null)
                {
                    propertyEditor.Reload(force);
                }
            }
        }

        protected virtual void OnResetClick()
        {
            GameObject go = new GameObject();
            go.SetActive(false);

            Component defaultComponent = go.GetComponent(ComponentType);
            if (defaultComponent == null)
            {
                defaultComponent = go.AddComponent(ComponentType);
            }
            bool isMonoBehavior = defaultComponent is MonoBehaviour;

            PropertyDescriptor[] descriptors = m_editorsMap.GetPropertyDescriptors(ComponentType, this, m_converter);
            for (int i = 0; i < descriptors.Length; ++i)
            {
                PropertyDescriptor descriptor = descriptors[i];
                MemberInfo memberInfo = descriptor.ComponentMemberInfo;
                if(memberInfo is PropertyInfo)
                {
                    PropertyInfo p = (PropertyInfo)memberInfo;
                    foreach(Component component in Components)
                    {
                        if(component == null)
                        {
                            continue;
                        }

                        object defaultValue = p.GetValue(defaultComponent, null);
                        m_editor.Undo.BeginRecordValue(component, memberInfo);
                        p.SetValue(component, defaultValue, null);
                    }
                }
                else
                {
                    if (isMonoBehavior)
                    {
                        if(memberInfo is FieldInfo)
                        {
                            foreach (Component component in Components)
                            {
                                if (component == null)
                                {
                                    continue;
                                }

                                FieldInfo f = (FieldInfo)memberInfo;
                                object defaultValue = f.GetValue(defaultComponent);
                                m_editor.Undo.BeginRecordValue(component, memberInfo);
                                f.SetValue(component, defaultValue);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < descriptors.Length; ++i)
            {
                PropertyDescriptor descriptor = descriptors[i];
                MemberInfo memberInfo = descriptor.MemberInfo;
                PropertyEditor propertyEditor = GetPropertyEditor(memberInfo);
                if (propertyEditor != null)
                {
                    propertyEditor.Reload(true);
                }
            }

            Destroy(go);

            m_editor.Undo.BeginRecord();
            for (int i = 0; i < descriptors.Length; ++i)
            {
                PropertyDescriptor descriptor = descriptors[i];
                MemberInfo memberInfo = descriptor.ComponentMemberInfo;
                if (memberInfo is PropertyInfo)
                {
                    foreach (Component component in Components)
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        m_editor.Undo.EndRecordValue(component, memberInfo);
                    }
                }
                else
                {
                    if(isMonoBehavior)
                    {
                        foreach (Component component in Components)
                        {
                            if (component == null)
                            {
                                continue;
                            }

                            m_editor.Undo.EndRecordValue(component, memberInfo);
                        }
                    }
                }
            }
            m_editor.Undo.EndRecord();
        }

        protected virtual void OnRemove()
        {
            PropertyDescriptor[] descriptors = m_editorsMap.GetPropertyDescriptors(ComponentType, this, m_converter);

            Editor.Undo.BeginRecord();
            Component[] components = Components;
            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                Editor.Undo.DestroyComponent(component, descriptors.Where(d => d.Targets[0] == (object)component).Select(d => d.ComponentMemberInfo).ToArray());
            }
           
            Editor.Undo.EndRecord();
        }

        [Obsolete]
        protected virtual void AwakeOverride()
        {
        }

        [Obsolete]
        protected virtual void StartOverride()
        {
        }

        [Obsolete]
        protected virtual void OnDestroyOverride()
        {
        }

        [Obsolete]
        protected virtual void UpdateOverride()
        {

        }
    }

}
