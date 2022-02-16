using Battlehub.RTCommon;
using Battlehub.RTHandles;
using UnityEngine;

namespace Battlehub.RTEditor.Demo
{
    public class CustomHandleExample : BaseHandle
    {
        private Vector3 m_prevPoint;

        protected override bool OnBeginDrag()
        {
            if (!base.OnBeginDrag())
            {
                return false;
            }

            DragPlane = new Plane(Vector3.up, 0);
            GetPointOnDragPlane(Window.Pointer, out m_prevPoint);

            RaycastHit[] hits = Physics.RaycastAll(Window.Pointer);
            foreach (RaycastHit hit in hits)
            {
                if (Editor.Selection.IsSelected(hit.collider.gameObject))
                {
                    return true;
                }
            }
            return false;
        }

        protected override void OnDrag()
        {
            if (!Window.IsPointerOver)
            {
                return;
            }

            Vector3 point;
            GetPointOnDragPlane(Window.Pointer, out point);

            Vector3 delta = point - m_prevPoint;
            m_prevPoint = point;

            transform.position += delta;
            foreach (Transform target in RealTargets)
            {
                target.transform.position += delta;
            }
        }
    }

}

