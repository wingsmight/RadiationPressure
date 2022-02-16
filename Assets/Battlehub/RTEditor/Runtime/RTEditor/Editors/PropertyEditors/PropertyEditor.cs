using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Battlehub.RTCommon;

using UnityObject = UnityEngine.Object;
using System.Linq;
using System.Collections;
using TMPro;
using System.Globalization;

namespace Battlehub.RTEditor
{
    public class CustomTypeFieldAccessor
    {
        private int m_index;

        private MemberInfo m_memberInfo;
        private PropertyEditor<object> m_editor;

        public string Name
        {
            get;
            private set;
        }

        public Type Type
        {
            get;
            private set;
        }

        public object Value
        {
            get
            {
                object obj = m_editor.GetValue(m_index);
                if (obj == null)
                {
                    return null;
                }
                if(m_memberInfo is FieldInfo)
                {
                    return ((FieldInfo)m_memberInfo).GetValue(obj);
                }
                else if (m_memberInfo is PropertyInfo)
                {
                    return ((PropertyInfo)m_memberInfo).GetValue(obj, null);
                }
                return null;
            }
            set
            {
                int targetsCount = m_editor.Target != null ? m_editor.Targets.Length : 0;
                for(int i = 0; i < targetsCount; ++i)
                {
                    object obj = m_editor.GetValue(i);
                    if (m_memberInfo is FieldInfo)
                    {
                        ((FieldInfo)m_memberInfo).SetValue(obj, value);
                    }
                    else if (m_memberInfo is PropertyInfo)
                    {
                        ((PropertyInfo)m_memberInfo).SetValue(obj, value, null);
                    }
                    m_editor.SetValue(obj, i);
                }
            }
        }

        public CustomTypeFieldAccessor(PropertyEditor<object> editor, int index, MemberInfo fieldInfo, string name)
        {
            m_index = index;
            m_editor = editor;
            m_memberInfo = fieldInfo;
            Name = name;
            if (m_memberInfo is PropertyInfo)
            {
                PropertyInfo pInfo = (PropertyInfo)m_memberInfo;
                Type = pInfo.PropertyType;
            }
            else
            {
                FieldInfo fInfo = (FieldInfo)m_memberInfo;
                Type = fInfo.FieldType;
            }
        }
    }

    public class IListElementAccessor
    {
        private int m_listIndex;

        private int m_index;
        public int Index
        {
            get { return m_index; }
        }

        private IListEditor m_editor;
        public IListEditor Editor
        {
            get { return m_editor; }
        }

        public virtual string Name
        {
            get;
            private set;
        }

        public Type Type
        {
            get { return m_editor.ElementType; }
        }

        public object Value
        {
            get
            {
                IList list = GetList(m_listIndex);
                if(list == null)
                {
                    return null;
                }

                if(m_index < 0 || m_index >= list.Count)
                {
                    return null;
                }

                return list[m_index];
            }
            set
            {
                int targetsCount = m_editor.Targets.Length;
                for(int i = 0; i < targetsCount; ++i)
                {
                    IList list = GetList(i);
                    list[m_index] = value;
                    m_editor.SetValue(list, i);
                }
            }
        }

        private IList GetList(int index = -1)
        {
            return m_editor.GetValue(index);
        }

        [Obsolete("Use IListElementAccessor(IListEditor editor, int listIndex, int index, string name)")]
        public IListElementAccessor(IListEditor editor, int index, string name)
        {
            m_editor = editor;
            m_index = index;
            Name = name;
        }

        public IListElementAccessor(IListEditor editor, int listIndex, int index, string name)
        {
            m_editor = editor;
            m_listIndex = listIndex;
            m_index = index;
            Name = name;
        }
    }

    public delegate void PropertyEditorCallback();

    public class PropertyEditor : MonoBehaviour
    {
        protected Action<object, object> m_eraseTargetCallback;
        protected PropertyEditorCallback m_valueChangingCallback;
        protected PropertyEditorCallback m_valueChangedCallback;
        protected PropertyEditorCallback m_endEditCallback;
        protected PropertyEditorCallback m_beginRecordCallback;
        protected PropertyEditorCallback m_endRecordCallback;
        protected PropertyEditorCallback m_afterRedoCallback;
        protected PropertyEditorCallback m_afterUndoCallback;
        protected PropertyEditorCallback m_reloadCallback;

        protected virtual IFormatProvider FormatProvider
        {
            get { return CultureInfo.InvariantCulture; }
        }
        
        private Dictionary<MemberInfo, PropertyDescriptor> m_childDescriptors;
        protected Dictionary<MemberInfo, PropertyDescriptor> ChildDescriptors
        {
            get { return m_childDescriptors; }
        }

        [SerializeField]
        protected TextMeshProUGUI Label;

        [SerializeField]
        protected int Indent = 10;

        private int m_effectiveIndent;
        private bool m_enableUndo;
        private bool m_isEditing;

        private bool m_lockValue;
        protected bool LockValue
        {
            get { return m_lockValue; }
        }

        private object[] m_targets;
        public object[] Targets
        {
            get { return m_targets; }
        }

        private object[] m_accessors;
        public object[] Accessors
        {
            get { return m_accessors; }
        }

        public object Target
        {
            get { return m_targets != null && m_targets.Length > 0 ? m_targets[0] : null; }
        }

        public object Accessor
        {
            get { return m_accessors != null && m_accessors.Length > 0 ? m_accessors[0] : null; }
        }

        
        public MemberInfo MemberInfo
        {
            get;
            private set;
        }
        protected Type MemberInfoType
        {
            get;
            private set;
        }

        private IRTE m_editor;
        public IRTE Editor
        {
            get { return m_editor; }
        }
        
        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            AwakeOverride();
            Editor.Undo.BeforeUndo += OnBeforeUndo;
        }
        
        private void Start()
        {
            StartOverride();
        }

        private void OnTransformParentChanged()
        {
            if(transform.parent != null)
            {
                PropertyEditor parentEditor = transform.parent.GetComponentInParent<PropertyEditor>();
                if (parentEditor != null)
                {
                    m_effectiveIndent = parentEditor.m_effectiveIndent + Indent;
                    SetIndent(m_effectiveIndent);
                }
            }
        }

        protected virtual void SetIndent(float indent)
        {
            if (Label != null)
            {
                RectTransform rt = Label.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.offsetMin = new Vector2(indent, rt.offsetMin.y);
                }
            }
        }

        private void OnDestroy()
        {
            if(Editor != null)
            {
                Editor.Undo.BeforeUndo -= OnBeforeUndo;
            }

            EndEdit();
            OnDestroyOverride();
        }

        protected virtual void AwakeOverride()
        {
        }

        protected virtual void StartOverride()
        {
        }

        protected virtual void OnDestroyOverride()
        {
        }

        public void Init(object target, MemberInfo memberInfo, string label, bool enableUndo = true,
            PropertyEditorCallback beginRecordCallback = null,
            PropertyEditorCallback endRecordCallback = null,
            PropertyEditorCallback afterRedoCallback = null,
            PropertyEditorCallback afterUndoCallback = null)
        {
            Init(target, target, memberInfo, null, label, null, null, null, enableUndo, null, beginRecordCallback, endRecordCallback, afterRedoCallback, afterUndoCallback);
        }

        public void Init(object[] target, MemberInfo memberInfo, string label, bool enableUndo = true,
           PropertyEditorCallback beginRecordCallback = null,
           PropertyEditorCallback endRecordCallback = null,
           PropertyEditorCallback afterRedoCallback = null,
           PropertyEditorCallback afterUndoCallback = null)
        {
            Init(target, target, memberInfo, null, label, null, null, null, enableUndo, null, beginRecordCallback, endRecordCallback, afterRedoCallback, afterUndoCallback);
        }

        public void Init(object target, object accessor,
            MemberInfo memberInfo,
            Action<object, object> eraseTargetCallback = null,
            string label = null,
            PropertyEditorCallback valueChangingCallback = null,
            PropertyEditorCallback valueChangedCallback = null,
            PropertyEditorCallback endEditCallback = null,
            bool enableUndo = true,
            PropertyDescriptor[] childDescriptors = null,
            PropertyEditorCallback beginRecordCallback = null,
            PropertyEditorCallback endRecordCallback = null,
            PropertyEditorCallback afterRedoCallback = null,
            PropertyEditorCallback afterUndoCallback = null,
            PropertyEditorCallback reloadCallback = null)
        {
            Init(new[] { target }, new[] { accessor }, memberInfo,
                eraseTargetCallback,
                label,
                valueChangingCallback, valueChangedCallback, endEditCallback,
                enableUndo,
                childDescriptors,
                beginRecordCallback, endRecordCallback,
                afterRedoCallback, afterUndoCallback, reloadCallback);   
        }

        public void Init(object[] targets, object[] accessors,
           MemberInfo memberInfo,
           Action<object, object> eraseTargetCallback = null,
           string label = null,
           PropertyEditorCallback valueChangingCallback = null,
           PropertyEditorCallback valueChangedCallback = null,
           PropertyEditorCallback endEditCallback = null,
           bool enableUndo = true,
           PropertyDescriptor[] childDescriptors = null,
           PropertyEditorCallback beginRecordCallback = null,
           PropertyEditorCallback endRecordCallback = null,
           PropertyEditorCallback afterRedoCallback = null,
           PropertyEditorCallback afterUndoCallback = null,
           PropertyEditorCallback reloadCallback = null)
        {
            m_enableUndo = enableUndo;
            m_valueChangingCallback = valueChangingCallback;
            m_valueChangedCallback = valueChangedCallback;
            m_endEditCallback = endEditCallback;
            m_beginRecordCallback = beginRecordCallback;
            m_endRecordCallback = endRecordCallback;
            m_afterRedoCallback = afterRedoCallback;
            m_afterUndoCallback = afterUndoCallback;
            m_reloadCallback = reloadCallback;

            if (childDescriptors != null)
            {
                m_childDescriptors = childDescriptors.ToDictionary(d => d.MemberInfo);
            }
            m_lockValue = true;
            InitOverride(targets, accessors, memberInfo, eraseTargetCallback, label);

#pragma warning disable 0618
            if(targets != null && accessors != null)
            {
                InitOverride(targets[0], accessors[0], memberInfo, eraseTargetCallback, label);
            }
#pragma warning restore 0618

            m_lockValue = false;
        }

        protected virtual void InitOverride(object[] targets, object[] accessors, MemberInfo memberInfo, Action<object, object> eraseTargetCallback = null, string label = null)
        {
            m_targets = targets;
            m_accessors = accessors;

            if(Target == null)
            {
                if (Label != null)
                {
                    if (label != null)
                    {
                        Label.text = label;
                    }
                }
                return;
            }

            IListElementAccessor arrayElement = Target as IListElementAccessor;
            if (arrayElement == null)
            {
                if (memberInfo is PropertyInfo)
                {
                    Type propType = ((PropertyInfo)memberInfo).PropertyType;
                    MemberInfoType = propType;
                }
                else if(memberInfo is FieldInfo)
                {
                    Type fieldType = ((FieldInfo)memberInfo).FieldType;
                    MemberInfoType = fieldType;
                }

                if(Label != null)
                {
                    if (label != null)
                    {
                        Label.text = label;
                    }
                    else
                    {
                        Label.text = memberInfo.Name;
                    }
                }
            }
            else
            {
                if(Label != null)
                {
                    Label.text = arrayElement.Name;
                }
                
                MemberInfoType = arrayElement.Type;
            }

            MemberInfo = memberInfo;
            m_eraseTargetCallback = eraseTargetCallback;
        }

        public void Reload(bool force = false)
        {
            if(m_isEditing)
            {
                return;
            }

            m_lockValue = true;
            ReloadOverride(force);
            m_lockValue = false;
        }

        protected virtual void ReloadOverride(bool force)
        {

        }

        protected void BeginEdit(bool record = true)
        {
            if(!m_isEditing && !m_lockValue)
            {                
                if(record)
                {
                    BeginRecord();
                }
                              
                m_isEditing = true;
            }
        }

        protected void EndEdit(bool record = true)
        {
            if(m_isEditing)
            {
                if(record)
                {
                    EndRecord();
                }
                
                if(m_endEditCallback != null)
                {
                    m_endEditCallback();
                }
            }
            m_isEditing = false;
        }

        protected virtual void OnBeforeUndo()
        {
            if (m_isEditing)
            {
                EndRecord();
            }
            m_isEditing = false;
        }

        protected void BeginRecord()
        {
            if(m_enableUndo)
            {
                if(m_targets != null)
                {
                    for(int i = 0; i < m_targets.Length; ++i)
                    {
                        Editor.Undo.BeginRecordValue(m_targets[i], m_accessors[i], MemberInfo);
                    }
                }
            }
            else
            {
                if (m_beginRecordCallback != null)
                {
                    m_beginRecordCallback();
                }
            }
        }

        protected void EndRecord()
        {
            if(m_enableUndo)
            {
                if(m_targets != null && m_targets.Length > 0)
                {
                    Editor.Undo.BeginRecord();

                    if (m_afterRedoCallback != null && m_afterUndoCallback != null)
                    {
                        for (int i = 0; i < m_targets.Length; ++i)
                        {
                            Editor.Undo.EndRecordValue(m_targets[i], m_accessors[i], MemberInfo, m_eraseTargetCallback, () => m_afterRedoCallback(), () => m_afterUndoCallback());
                        }
                    }
                    else
                    {
                        for (int i = 0; i < m_targets.Length; ++i)
                        {
                            Editor.Undo.EndRecordValue(m_targets[i], m_accessors[i], MemberInfo, m_eraseTargetCallback);
                        }
                    }

                    Editor.Undo.EndRecord();
                }
              
            }
            else
            {
                if (m_endRecordCallback != null)
                {
                    m_endRecordCallback();
                }
            }
        }

        protected void RaiseValueChanging()
        {
            if(m_valueChangingCallback != null)
            {
                m_valueChangingCallback();
            }
        }

        protected void RaiseValueChanged()
        {
            if(m_valueChangedCallback != null)
            {
                m_valueChangedCallback();
            }
        }

        #region obsolete

        [Obsolete("Use  void InitOverride(object[] targets, object[] accessors, MemberInfo memberInfo, Action<object, object> eraseTargetCallback = null, string label = null)")]
        protected virtual void InitOverride(object target, object accessor, MemberInfo memberInfo, Action<object, object> eraseTargetCallback = null, string label = null)
        {
        }

        #endregion
    }
    public abstract class ConvertablePropertyEditor<T> : PropertyEditor<T>
    {
        [SerializeField]
        protected bool m_convertUnits = false;

        private ISettingsComponent m_settings;

        protected override void AwakeOverride()
        {
            m_settings = IOC.Resolve<ISettingsComponent>();
            base.AwakeOverride();
        }

        protected float FromMeters(float units)
        {
            if (m_convertUnits && m_settings != null && m_settings.SystemOfMeasurement == SystemOfMeasurement.Imperial)
            {
                return UnitsConverter.MetersToFeet(units);
            }
            return units;
        }

        protected float ToMeters(float units)
        {
            if (m_convertUnits && m_settings != null && m_settings.SystemOfMeasurement == SystemOfMeasurement.Imperial)
            {
                return UnitsConverter.FeetToMeters(units);
            }
            return units;
        }
    }

    public abstract class PropertyEditor<T> : PropertyEditor
    {
        protected T m_currentValue;
   
        protected override void InitOverride(object[] target, object[] accessor, MemberInfo memberInfo, Action<object, object> eraseTargetCallback = null, string label = null)
        {
            base.InitOverride(target, accessor, memberInfo, eraseTargetCallback, label);
            SetInputField(GetValue());
        }

        protected virtual void SetInputField(T value)
        {
        }

        /// <summary>
        /// Returns true if target properties has different (mixed) values
        /// </summary>
        public virtual bool HasMixedValues()
        {
            return HasMixedValues(_GetValue, Equals);
        }

        protected virtual bool HasMixedValues(Func<object, object, object> getValue, Func<object, object, bool> equals)
        {
            object[] targets = Targets;
            if (targets == null || targets.Length == 0)
            {
                return false;
            }

            object[] accessors = Accessors;
            object val0 = getValue(targets[0], accessors[0]);
            for (int i = 1; i < targets.Length; ++i)
            {
                object val1 = getValue(targets[i], accessors[i]);
                if (!equals(val0, val1))
                {
                    return true;
                }
            }
            return false;
        }

        public T GetValue(int index = -1)
        {
            if(index == -1)
            {
                return (T)_GetValue(Target, Accessor);
            }

            return (T)_GetValue(Targets[index], Accessors[index]);   
        }

        protected T GetValue(object target, object accessor)
        {
            return (T)_GetValue(target, accessor);
        }

        private object _GetValue(object target, object accessor)
        {
            if (target is UnityObject)
            {
                UnityObject obj = (UnityObject)target;
                if (obj == null)
                {
                    return default(T);
                }
            }

            if (accessor == null)
            {
                return default(T);
            }

            if (MemberInfo is PropertyInfo)
            {
                PropertyInfo prop = (PropertyInfo)MemberInfo;
                return prop.GetValue(accessor, null);
            }

            FieldInfo field = (FieldInfo)MemberInfo;
            return field.GetValue(accessor);
        }

        /// <summary>
        /// Set Target value
        /// </summary>
        /// <param name="value">value</param>
        /// <param name="index">accessor index. Use all accessors if index == -1</param>
        public void SetValue(T value, int index = -1)
        {
            if(LockValue)
            {
                return;
            }

            if(Target == null)
            {
                return;
            }

            if (Target is UnityObject)
            {
                UnityObject obj = (UnityObject)Target;
                if (obj == null)
                {
                    return;
                }
            }

            RaiseValueChanging();
            BeginEdit(); //where is EndEdit? is this mistake?
            if (MemberInfo is PropertyInfo)
            {
                PropertyInfo prop = (PropertyInfo)MemberInfo;
                object[] accessors = Accessors;
                if (index < 0)
                {
                    for (int i = 0; i < accessors.Length; ++i)
                    {
                        prop.SetValue(accessors[i], value, null);
                    }
                }
                else
                {
                    prop.SetValue(accessors[index], value, null);
                }  
            }
            else
            {
                FieldInfo field = (FieldInfo)MemberInfo;
                object[] accessors = Accessors;
                if (index < 0)
                {
                    for (int i = 0; i < accessors.Length; ++i)
                    {
                        field.SetValue(accessors[i], value);
                    }
                }
                else
                {
                    field.SetValue(accessors[index], value);
                }
            }

            m_currentValue = value;
            RaiseValueChanged();
        }

        private const float m_updateInterval = 0.25f;
        private float m_nextUpate;
        protected virtual void Update()
        {
            if(m_nextUpate > Time.time)
            {
                return;
            }

            m_nextUpate = Time.time + m_updateInterval;
            
            if (MemberInfo == null)
            {
                return;
            }

            if(Target == null)
            {
                return;
            }

            if(Target is UnityObject)
            {
                UnityObject uobj = (UnityObject)Target;
                if(uobj == null)
                {
                    return;
                }
            }

            Reload();
        }

        protected override void ReloadOverride(bool force)
        {
            base.ReloadOverride(force);

            T value = GetValue();
            if (force || !Equals(m_currentValue, value)) // || HasMixedValues ?
            {
                m_currentValue = value;
                SetInputField(value);
                if (m_reloadCallback != null)
                {
                    m_reloadCallback();
                }
            }
        }

        protected virtual bool Equals(T x, T y)
        {
            return EqualityComparer<T>.Default.Equals(x, y);
        }
    }
}
