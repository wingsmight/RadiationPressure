using Battlehub.RTCommon;
using Battlehub.Utils;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTEditor
{
    
    public class BoundsAccessor
    {
        private int m_index;
        private PropertyEditor<Bounds> m_editor;

        public Vector3 Center
        {
            get { return GetBounds(m_index).center; }
            set
            {
                Bounds bounds = GetBounds(m_index);
                bounds.center = value;
                m_editor.SetValue(bounds, m_index);
            }
        }

        public Vector3 Extents
        {
            get { return GetBounds(m_index).extents; }
            set
            {
                Bounds bounds = GetBounds(m_index);
                bounds.extents = value;
                m_editor.SetValue(bounds, m_index);
            }
        }

        private Bounds GetBounds(int index = -1)
        {
            return m_editor.GetValue(index);
        }

        public BoundsAccessor(PropertyEditor<Bounds> editor, int index = -1)
        {
            m_editor = editor;
            m_index = index;
        }
    }

    public class BoundsEditor : PropertyEditor<Bounds>
    {
        [SerializeField]
        private Vector3Editor m_center = null;
        [SerializeField]
        private Vector3Editor m_extents = null;
        
        protected override void AwakeOverride()
        {
            base.AwakeOverride();
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
        }

        protected override void InitOverride(object[] targets, object[] accessors, MemberInfo memberInfo, Action<object, object> eraseTargetCallback, string label = null)
        {
            base.InitOverride(targets, accessors, memberInfo, eraseTargetCallback, label);

            ILocalization localization = IOC.Resolve<ILocalization>();

            int targetsCount = targets.Length;
            BoundsAccessor[] boundsAccessors = new BoundsAccessor[targetsCount];
            for(int i = 0; i < targets.Length; ++i)
            {
                boundsAccessors[i] = new BoundsAccessor(this, i); 
            }

            m_center.Init(boundsAccessors, boundsAccessors, Strong.PropertyInfo((BoundsAccessor x) => x.Center, "Center"), null, localization.GetString("ID_RTEditor_PE_BoundsEditor_Center", "Center"), OnValueChanging, null, OnEndEdit, false);
            m_extents.Init(boundsAccessors, boundsAccessors, Strong.PropertyInfo((BoundsAccessor x) => x.Extents, "Extents"), null, localization.GetString("ID_RTEditor_PE_BoundsEditor_Extents", "Extents"), OnValueChanging, null, OnEndEdit, false);
        }

        protected override void ReloadOverride(bool force)
        {
            base.ReloadOverride(force);
            m_center.Reload();
            m_extents.Reload();
        }

        private void OnValueChanging()
        {
            BeginEdit();
        }

        private void OnEndEdit()
        {
            EndEdit();
        }
    }
}

