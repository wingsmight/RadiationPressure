﻿using Battlehub.RTCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.RTEditor
{
    public class CustomTypeEditor : PropertyEditor<object>, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Transform Panel = null;
        [SerializeField]
        private Toggle Expander = null;

        public bool StartExpanded;

        private IEditorsMap m_editorsMap;

        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            m_editorsMap = IOC.Resolve<IEditorsMap>();
            Expander.onValueChanged.AddListener(OnExpanded);
        }

        protected override void StartOverride()
        {
            base.StartOverride();
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if (Expander != null)
            {
                Expander.onValueChanged.RemoveListener(OnExpanded);
            }

            if (m_coExpand != null)
            {
                StopCoroutine(m_coExpand);
                m_coExpand = null;
            }
        }

        protected override void SetIndent(float indent)
        {
            RectTransform rt = Expander.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.offsetMin = new Vector2(indent, rt.offsetMin.y);
            }
        }

        protected override void InitOverride(object[] targets, object[] accessors, MemberInfo memberInfo, Action<object, object> eraseTargetCallback, string label = null)
        {
            base.InitOverride(targets, accessors, memberInfo, eraseTargetCallback, label);
           
            FieldInfo[] serializableFields = Reflection.GetSerializableFields(memberInfo.GetType(), false);

            if (StartExpanded)
            {
                Expander.isOn = serializableFields.Length < 8;
            }
        }

        private void OnExpanded(bool isExpanded)
        {
            Panel.gameObject.SetActive(isExpanded);

            m_currentValue = GetValue();
            if (isExpanded)
            {
                CreateElementEditors(m_currentValue);
            }
            else
            {
                foreach (Transform c in Panel)
                {
                    Destroy(c.gameObject);
                }
            }
        }


        private void BuildEditor()
        {
            foreach (Transform c in Panel)
            {
                Destroy(c.gameObject);
            }

            CreateElementEditors(m_currentValue);
        }

        private void CreateElementEditors(object value)
        {
            Type memberInfoType = value != null ? value.GetType() : MemberInfoType;

            FieldInfo[] fields = Reflection.GetSerializableFields(memberInfoType, false);
            for (int i = 0; i < fields.Length; ++i)
            {
                MemberInfo memberInfo = fields[i];
                Type type = fields[i].FieldType;
                CreateElementEditor(memberInfo, type);
            }

            PropertyInfo[] properties = Reflection.GetSerializableProperties(memberInfoType);
            for(int i = 0; i < properties.Length; ++i)
            {
                PropertyInfo propertyInfo = properties[i];
                Type type = properties[i].PropertyType;
                CreateElementEditor(propertyInfo, type);
            }
        }

        private void CreateElementEditor(MemberInfo memberInfo, Type type)
        {
            if (!m_editorsMap.IsPropertyEditorEnabled(type))
            {
                return;
            }
            GameObject editorPrefab = m_editorsMap.GetPropertyEditor(type);
            if (editorPrefab == null)
            {
                return;
            }
         
            List<CustomTypeFieldAccessor> accessorsList = new List<CustomTypeFieldAccessor>();
            int targetsCount = Targets.Length;
            if(ChildDescriptors == null)
            {
                for(int i = 0; i < targetsCount; ++i)
                {
                    accessorsList.Add(new CustomTypeFieldAccessor(this, i, memberInfo, memberInfo.Name));
                }   
            }
            else
            {
                PropertyDescriptor childPropertyDescriptor;
                if(ChildDescriptors.TryGetValue(memberInfo, out childPropertyDescriptor))
                {
                    for (int i = 0; i < targetsCount; ++i)
                    {
                        accessorsList.Add(new CustomTypeFieldAccessor(
                            this, i, childPropertyDescriptor.MemberInfo, childPropertyDescriptor.Label));
                    }
                }
            }

            if (accessorsList.Count > 0)
            {
                PropertyEditor editor = Instantiate(editorPrefab).GetComponent<PropertyEditor>();
                if (editor == null)
                {
                    return;
                }

                editor.transform.SetParent(Panel, false);

                CustomTypeFieldAccessor[] accessors = accessorsList.ToArray();
                editor.Init(accessors, accessors, accessors[0].GetType().GetProperty("Value"), null, accessors[0].Name, OnValueChanging, OnValueChanged, null, false);
            }
        }

        private void OnValueChanging()
        {
            BeginEdit();
        }

        private void OnValueChanged()
        {
            EndEdit();
        }

        protected override void SetInputField(object value)
        {
            if (value == null)
            {
                if(MemberInfoType.IsArray)
                {
                    Array newArray = (Array)Activator.CreateInstance(MemberInfoType, 0);
                    SetValue(newArray);
                    return;
                }
            }
        }

        protected override void ReloadOverride(bool force)
        {
            base.ReloadOverride(force);
            if(force)
            {
                DoReload();
            }
            else
            {
                object value = GetValue();
                if (!EqualityComparer<object>.Default.Equals(m_currentValue, value))
                {
                    Type memberInfoType = value != null ? value.GetType() : MemberInfoType;
                    if (!Reflection.IsValueType(memberInfoType) || !Equals(memberInfoType, m_currentValue, value))
                    {
                        DoReload();
                    }
                }
            }
        }

        private void DoReload()
        {
            object value = Activator.CreateInstance(MemberInfoType);
            SetValue(value);
            m_currentValue = value;
            SetInputField(value);
            BuildEditor();
        }

        private bool Equals(Type memberInfoType, object currentValue, object value)
        {
            FieldInfo[] fields = Reflection.GetSerializableFields(memberInfoType, false);
            PropertyInfo[] properties = Reflection.GetSerializableProperties(memberInfoType);
            
            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo fieldInfo = fields[i];
                if (!m_editorsMap.IsPropertyEditorEnabled(fieldInfo.FieldType))
                {
                    continue;
                }

                if(ChildDescriptors != null && !ChildDescriptors.ContainsKey(fieldInfo))
                {
                    continue;
                }

                object c = fieldInfo.GetValue(currentValue);
                object v = fieldInfo.GetValue(value);
                if (c == null && v == null)
                {
                    continue;
                }
                if (c == null || v == null || !c.Equals(v))
                {
                    return false;
                }
            }

            for (int i = 0; i < properties.Length; ++i)
            {
                PropertyInfo propertyInfo = properties[i];
                if (!m_editorsMap.IsPropertyEditorEnabled(propertyInfo.PropertyType))
                {
                    continue;
                }

                if (ChildDescriptors != null && !ChildDescriptors.ContainsKey(propertyInfo))
                {
                    continue;
                }

                object c = propertyInfo.GetValue(currentValue);
                object v = propertyInfo.GetValue(value);
                if (c == null && v == null)
                {
                    continue;
                }
                if (c == null || v == null || !c.Equals(v))
                {
                    return false;
                }
            }

            return true;
        }


        private IEnumerator m_coExpand;
        private IEnumerator CoExpand()
        {
            yield return new WaitForSeconds(0.5f);
            Expander.isOn = true;
            m_coExpand = null;
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (Editor.DragDrop.InProgress)
            {
                if (Expander != null)
                {
                    m_coExpand = CoExpand();
                    StartCoroutine(m_coExpand);
                }
            }
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (m_coExpand != null)
            {
                StopCoroutine(m_coExpand);
                m_coExpand = null;
            }
        }
    }
}

