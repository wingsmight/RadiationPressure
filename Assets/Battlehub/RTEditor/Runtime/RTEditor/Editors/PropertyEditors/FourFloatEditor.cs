using UnityEngine;
using TMPro;
using System.Globalization;

namespace Battlehub.RTEditor
{
    public abstract class FourFloatEditor<T> : PropertyEditor<T>
    {
        [SerializeField]
        private TMP_InputField m_xInput = null;
        [SerializeField]
        private TMP_InputField m_yInput = null;
        [SerializeField]
        private TMP_InputField m_zInput = null;
        [SerializeField]
        private TMP_InputField m_wInput = null;
        [SerializeField]
        private RectTransform m_expander = null;
        [SerializeField]
        private RectTransform m_xLabel = null;
        [SerializeField]
        private RectTransform m_yLabel = null;
        [SerializeField]
        private RectTransform m_zLabel = null;
        [SerializeField]
        private RectTransform m_wLabel = null;

        [SerializeField]
        protected DragField[] m_dragFields = null;

        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            m_xInput.onValueChanged.AddListener(OnXValueChanged);
            m_yInput.onValueChanged.AddListener(OnYValueChanged);
            m_zInput.onValueChanged.AddListener(OnZValueChanged);
            m_wInput.onValueChanged.AddListener(OnWValueChanged);

            m_xLabel.offsetMin = new Vector2(Indent, m_xLabel.offsetMin.y);
            m_yLabel.offsetMin = new Vector2(Indent, m_yLabel.offsetMin.y);
            m_zLabel.offsetMin = new Vector2(Indent, m_zLabel.offsetMin.y);
            m_wLabel.offsetMin = new Vector2(Indent, m_wLabel.offsetMin.y);

            m_xInput.onEndEdit.AddListener(OnEndEdit);
            m_yInput.onEndEdit.AddListener(OnEndEdit);
            m_zInput.onEndEdit.AddListener(OnEndEdit);
            m_wInput.onEndEdit.AddListener(OnEndEdit);

            for (int i = 0; i < m_dragFields.Length; ++i)
            {
                if (m_dragFields[i])
                {
                    m_dragFields[i].EndDrag.AddListener(OnEndDrag);
                }
            }
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if (m_xInput != null)
            {
                m_xInput.onValueChanged.RemoveListener(OnXValueChanged);
                m_xInput.onEndEdit.RemoveListener(OnEndEdit);
            }

            if (m_yInput != null)
            {
                m_yInput.onValueChanged.RemoveListener(OnYValueChanged);
                m_yInput.onEndEdit.RemoveListener(OnEndEdit);
            }

            if (m_zInput != null)
            {
                m_zInput.onValueChanged.RemoveListener(OnZValueChanged);
                m_zInput.onEndEdit.RemoveListener(OnEndEdit);
            }

            if (m_wInput != null)
            {
                m_wInput.onValueChanged.RemoveListener(OnWValueChanged);
                m_wInput.onEndEdit.RemoveListener(OnEndEdit);
            }

            for (int i = 0; i < m_dragFields.Length; ++i)
            {
                if (m_dragFields[i])
                {
                    m_dragFields[i].EndDrag.RemoveListener(OnEndDrag);
                }
            }
        }

        protected override void SetIndent(float indent)
        {
            m_expander.offsetMin = new Vector2(indent, m_expander.offsetMin.y);
            m_xLabel.offsetMin = new Vector2(indent + Indent, m_xLabel.offsetMin.y);
            m_yLabel.offsetMin = new Vector2(indent + Indent, m_yLabel.offsetMin.y);
            m_zLabel.offsetMin = new Vector2(indent + Indent, m_zLabel.offsetMin.y);
            m_wLabel.offsetMin = new Vector2(indent + Indent, m_wLabel.offsetMin.y);
        }

        protected override void SetInputField(T v)
        {
            if(HasMixedValues())
            {
                m_xInput.text = HasMixedValues((target, accessor) => GetX(GetValue(target, accessor)), (v1, v2) => v1.Equals(v2)) ? null : GetX(v).ToString(FormatProvider); 
                m_yInput.text = HasMixedValues((target, accessor) => GetY(GetValue(target, accessor)), (v1, v2) => v1.Equals(v2)) ? null : GetY(v).ToString(FormatProvider); 
                m_zInput.text = HasMixedValues((target, accessor) => GetZ(GetValue(target, accessor)), (v1, v2) => v1.Equals(v2)) ? null : GetZ(v).ToString(FormatProvider);
                m_wInput.text = HasMixedValues((target, accessor) => GetW(GetValue(target, accessor)), (v1, v2) => v1.Equals(v2)) ? null : GetW(v).ToString(FormatProvider);
            }
            else
            {
                m_xInput.text = GetX(v).ToString(FormatProvider);
                m_yInput.text = GetY(v).ToString(FormatProvider);
                m_zInput.text = GetZ(v).ToString(FormatProvider);
                m_wInput.text = GetW(v).ToString(FormatProvider);
            }
        }

        private void OnXValueChanged(string value)
        {
            float val;
            if (float.TryParse(value, NumberStyles.Any, FormatProvider, out val) && Target != null)
            {
                object[] targets = Targets;
                object[] accessors = Accessors;
                for (int i = 0; i < targets.Length; ++i)
                {
                    T v = SetX(GetValue(targets[i], accessors[i]), val);
                    SetValue(v, i);
                }
            }
        }

        private void OnYValueChanged(string value)
        {
            float val;
            if (float.TryParse(value, NumberStyles.Any, FormatProvider, out val) && Target != null)
            {
                object[] targets = Targets;
                object[] accessors = Accessors;
                for (int i = 0; i < targets.Length; ++i)
                {
                    T v = SetY(GetValue(targets[i], accessors[i]), val);
                    SetValue(v, i);
                }   
            }
        }

        private void OnZValueChanged(string value)
        {
            float val;
            if (float.TryParse(value, NumberStyles.Any, FormatProvider, out val) && Target != null)
            {
                object[] targets = Targets;
                object[] accessors = Accessors;
                for (int i = 0; i < targets.Length; ++i)
                {
                    T v = SetZ(GetValue(targets[i], accessors[i]), val);
                    SetValue(v, i);
                }
            }
        }

        private void OnWValueChanged(string value)
        {
            float val;
            if (float.TryParse(value, NumberStyles.Any, FormatProvider, out val) && Target != null)
            {
                object[] targets = Targets;
                object[] accessors = Accessors;
                for (int i = 0; i < targets.Length; ++i)
                {
                    T v = SetW(GetValue(targets[i], accessors[i]), val);
                    SetValue(v, i);
                }
            }
        }

        protected virtual void OnEndEdit(string value)
        {
            T v = GetValue();
            SetInputField(v);
            EndEdit();
        }


        protected void OnEndDrag()
        {
            EndEdit();
        }

        protected abstract T SetX(T v, float x);
        protected abstract T SetY(T v, float y);
        protected abstract T SetZ(T v, float z);
        protected abstract T SetW(T v, float w);
        protected abstract float GetX(T v);
        protected abstract float GetY(T v);
        protected abstract float GetZ(T v);
        protected abstract float GetW(T v);


    }
}

