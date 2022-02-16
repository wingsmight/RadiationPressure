using UnityEngine;
using Battlehub.RTCommon;
namespace Battlehub.RTGizmos
{
    [DefaultExecutionOrder(-61)]
    public class BaseGizmoInput : MonoBehaviour
    {
        [SerializeField]
        protected BaseGizmo m_gizmo;
        protected IRTE m_editor;

        public BaseGizmo Gizmo
        {
            get { return m_gizmo; }
            set { m_gizmo = value; }
        }

        private void OnEnable()
        {
            if (m_gizmo == null)
            {
                m_gizmo = GetComponent<BaseGizmo>();
            }

            if(m_gizmo != null)
            {
                m_editor = m_gizmo.Editor;
                if (m_editor != null)
                {
                    if (BeginDragAction())
                    {
                        m_gizmo.BeginDrag();
                    }
                }
            }
        }

        protected virtual void Start()
        {
            if (m_gizmo == null)
            {
                m_gizmo = GetComponent<BaseGizmo>();
            }

            if (m_editor == null)
            {
                if(m_gizmo != null)
                {
                    m_editor = m_gizmo.Editor;
                }

            }
        }

        protected virtual void Update()
        {
            if(m_gizmo == null)
            {
                Destroy(this);
                return;
            }

            if (BeginDragAction())
            {
                m_gizmo.BeginDrag();
            }
            else if (EndDragAction())
            {
                m_gizmo.EndDrag();
            }
        }

        protected virtual bool BeginDragAction()
        {
            return m_editor.Input.GetPointerDown(0);
        }

        protected virtual bool EndDragAction()
        {
            return m_editor.Input.GetPointerUp(0);
        }
    }
}