using System;
using System.Collections;

using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Battlehub.RTCommon;
using TMPro;

namespace Battlehub.RTEditor
{
    public abstract class IListEditor : PropertyEditor<IList>, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        protected GameObject SizeEditor = null;
        [SerializeField]
        protected TMP_InputField SizeInput = null;
        [SerializeField]
        protected TextMeshProUGUI SizeLabel = null;

        [SerializeField]
        protected Transform Panel = null;
        [SerializeField]
        protected Toggle Expander = null;

        private PropertyEditor m_editorPrefab;

        private Type m_elementType;
        public Type ElementType
        {
            get { return m_elementType; }
        }

        private Type m_propertyType;
        public Type PropertyType
        {
            get { return m_propertyType; }
        }

        public bool StartExpanded;

        private IEditorsMap m_editorsMap;

        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            m_editorsMap = IOC.Resolve<IEditorsMap>();

            Expander.onValueChanged.AddListener(OnExpanded);
            SizeInput.onValueChanged.AddListener(OnSizeValueChanged);
            SizeInput.onEndEdit.AddListener(OnSizeEndEdit);

            RectTransform rt = SizeLabel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.offsetMin = new Vector2(Indent, rt.offsetMin.y);
            }
        }   

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if (Expander != null)
            {
                Expander.onValueChanged.RemoveListener(OnExpanded);
            }

            if (SizeInput != null)
            {
                SizeInput.onValueChanged.RemoveListener(OnSizeValueChanged);
                SizeInput.onEndEdit.RemoveListener(OnSizeEndEdit);
            }

            if (m_coExpand != null)
            {
                StopCoroutine(m_coExpand);
                m_coExpand = null;
            }
        }

        protected override void SetIndent(float indent)
        {
            RectTransform rt = SizeLabel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.offsetMin = new Vector2(indent + Indent, rt.offsetMin.y);
            }

            rt = Expander.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.offsetMin = new Vector2(indent, rt.offsetMin.y);
            }
        }

        public Type GetElementType(object accessor, MemberInfo memberInfo)
        {
            CustomTypeFieldAccessor fieldAccessor = accessor as CustomTypeFieldAccessor;
            if (fieldAccessor != null)
            {
                m_propertyType = fieldAccessor.Type;
                m_elementType = fieldAccessor.Type.GetElementType();
                if (m_elementType == null)
                {
                    if (m_propertyType.IsGenericType)
                    {
                        m_elementType = fieldAccessor.Type.GetGenericArguments()[0];
                    }
                }
            }
            else
            {
                if (memberInfo is PropertyInfo)
                {
                    m_propertyType = ((PropertyInfo)memberInfo).PropertyType;
                    m_elementType = m_propertyType.GetElementType();
                    if (m_elementType == null)
                    {
                        if (m_propertyType.IsGenericType)
                        {
                            m_elementType = m_propertyType.GetGenericArguments()[0];
                        }
                    }
                }
                else if (memberInfo is FieldInfo)
                {
                    m_propertyType = ((FieldInfo)memberInfo).FieldType;
                    m_elementType = m_propertyType.GetElementType();
                    if (m_elementType == null)
                    {
                        if (m_propertyType.IsGenericType)
                        {
                            m_elementType = m_propertyType.GetGenericArguments()[0];
                        }
                    }
                }
            }

            return m_elementType;
        }

        protected override void InitOverride(object[] targets, object[] accessors, MemberInfo memberInfo, Action<object, object> eraseTargetCallback, string label = null)
        {
            m_elementType = accessors != null && accessors.Length > 0 ? GetElementType(accessors[0], memberInfo) : null;

            if (m_elementType != null)
            {
                GameObject editor = m_editorsMap.GetPropertyEditor(m_elementType);
                if (m_editorsMap.IsPropertyEditorEnabled(m_elementType))
                {
                    m_editorPrefab = editor.GetComponent<PropertyEditor>();
                }

                if (m_editorPrefab == null)
                {
                    Debug.LogWarning("Editor for " + memberInfo.Name + " not found");
                    Destroy(gameObject);
                }

                base.InitOverride(targets, accessors, memberInfo, eraseTargetCallback, label);
            }
            else
            {
                if (m_elementType == null)
                {
                    Debug.LogWarning("Editor for " + memberInfo.Name + " not found");
                    Destroy(gameObject);
                }
            }

            if (StartExpanded)
            {
                IList value = GetValue();
                Expander.isOn = value == null || value.Count < 8;
            }
        }

        protected virtual void OnExpanded(bool isExpanded)
        {
            SizeEditor.SetActive(isExpanded);
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

        protected virtual IListElementAccessor CreateAccessor(int listIndex, int i)
        {
            return new IListElementAccessor(this, listIndex, i, "Element " + i);
        }

        private void CreateElementEditors(IList value)
        {
            if(value ==  null)
            {
                return;
            }

            int minCount = value.Count;
            int targetsCount = Targets.Length;
            if(targetsCount > 0)
            {
                for (int i = 0; i < targetsCount; ++i)
                {
                    IList list = GetValue(i);
                    if (list == null)
                    {
                        return;
                    }

                    if (list.Count < minCount)
                    {
                        minCount = list.Count;
                    }
                }
            }
            
            for (int i = 0; i < minCount; ++i)
            {
                PropertyEditor editor = Instantiate(m_editorPrefab);
                editor.transform.SetParent(Panel, false);

                IListElementAccessor[] accessors = new IListElementAccessor[targetsCount];
                for (int l = 0; l < targetsCount; ++l)
                {
                    accessors[l] = CreateAccessor(l, i);
                }

                editor.Init(accessors, accessors, accessors[0].GetType().GetProperty("Value"), null, accessors[0].Name, OnValueChanging, OnValueChanged, () =>
                {
                }, 
                true);
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

        private void OnSizeValueChanged(string value)
        {
            BeginEdit();
        }

        protected abstract IList Resize(IList list, int size);
        
        private void OnSizeEndEdit(string value)
        {
            int size;
            if (int.TryParse(value, out size) && size >= 0)
            {
                foreach (Transform c in Panel)
                {
                    Destroy(c.gameObject);
                }

                int targetsCount = Targets.Length;
                for (int l = 0; l < targetsCount; ++l)
                {
                    IList list = GetValue(l);
                    IList newList = Resize(list, size);
                    if (size > 0)
                    {
                        if (!m_elementType.IsSubclassOf(typeof(UnityEngine.Object)))
                        {
                            var constructor = m_elementType.GetConstructor(Type.EmptyTypes);
                            if (constructor != null)
                            {
                                for (int i = list != null ? list.Count : 0; i < newList.Count; ++i)
                                {
                                    newList[i] = Activator.CreateInstance(m_elementType);
                                }
                            }
                        }
                    }
                    SetValue(newList, l);
                }

                EndEdit();
                if (Expander.isOn)
                {
                    CreateElementEditors(GetValue());
                }
            }
            else
            {
                IList list = GetValue();
                SizeInput.text = list != null ? list.Count.ToString() : "0";
            }
        }

        public bool AreSizesEqual()
        {
            int targetsCount = Targets.Length;
            if (targetsCount > 0)
            {
                IList list = GetValue(0);
                int size = list != null ? list.Count : -1;
                for (int i = 1; i < targetsCount; ++i)
                {
                    list = GetValue(i);
                    if (size != (list != null ? list.Count : -1))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected override void SetInputField(IList value)
        {
            if(!AreSizesEqual())
            {
                SizeInput.text = null;
                return;
            }

            if (value == null)
            {
                IList newArray = (IList)Activator.CreateInstance(PropertyType);
                SetValue(newArray);
                SizeInput.text = "0";
                return;
            }
            SizeInput.text = value.Count.ToString();
        }

        protected override void ReloadOverride(bool force)
        {
            if (!Expander.isOn)
            {
                return;
            }

            IList value = GetValue();
            if (m_currentValue == null && value == null)
            {
                return;
            }

            if (force || m_currentValue == null || value == null || m_currentValue.Count != value.Count)
            {
                m_currentValue = value;
                SetInputField(m_currentValue);
                BuildEditor();
            }
            else
            {
                for (int i = 0; i < m_currentValue.Count; ++i)
                {
                    object c = m_currentValue[i];
                    object v = value[i];
                    if (c == null && v == null)
                    {
                        continue;
                    }
                    if (c == null || v == null || !c.Equals(v))
                    {
                        m_currentValue = value;
                        BuildEditor();
                    }
                }
            }
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
