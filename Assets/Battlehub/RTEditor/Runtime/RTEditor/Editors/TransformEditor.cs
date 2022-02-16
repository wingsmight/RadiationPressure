using Battlehub.RTCommon;
using Battlehub.RTHandles;
using Battlehub.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class TransformEditor : ComponentEditor
    {
        protected override void InitEditor(PropertyEditor editor, PropertyDescriptor descriptor)
        {
            base.InitEditor(editor, descriptor);

            bool canTransform = true;
            if(Components != null && Components.Length > 0)
            {
                IEnumerable<ExposeToEditor> exposeToEditor = NotNullComponents.Select(component => component.GetComponentInParent<ExposeToEditor>());
                if(exposeToEditor.Any(o => o != null && !o.CanTransform))
                {
                    canTransform = false;
                }
            }

            if(Editor.Tools.LockAxes == null && canTransform)
            {
                return;
            }

            if (descriptor.ComponentMemberInfo == Strong.PropertyInfo((Transform x) => x.localPosition, "localPosition"))
            {
                Vector3Editor vector3Editor = (Vector3Editor)editor;
                if (!canTransform)
                {
                    vector3Editor.IsXInteractable = false;
                    vector3Editor.IsYInteractable = false;
                    vector3Editor.IsZInteractable = false;
                }
                else if (Editor.Tools.LockAxes != null)
                {
                    vector3Editor.IsXInteractable = !Editor.Tools.LockAxes.PositionX;
                    vector3Editor.IsYInteractable = !Editor.Tools.LockAxes.PositionY;
                    vector3Editor.IsZInteractable = !Editor.Tools.LockAxes.PositionZ;
                }
            }

            if (descriptor.ComponentMemberInfo == Strong.PropertyInfo((Transform x) => x.localRotation, "localRotation"))
            {
                Vector3Editor vector3Editor = (Vector3Editor)editor;
                if (!canTransform)
                {
                    vector3Editor.IsXInteractable = false;
                    vector3Editor.IsYInteractable = false;
                    vector3Editor.IsZInteractable = false;
                }
                else if(Editor.Tools.LockAxes != null)
                {
                    vector3Editor.IsXInteractable = !Editor.Tools.LockAxes.RotationX;
                    vector3Editor.IsYInteractable = !Editor.Tools.LockAxes.RotationY;
                    vector3Editor.IsZInteractable = !Editor.Tools.LockAxes.RotationZ;
                }
            }

            if (descriptor.ComponentMemberInfo == Strong.PropertyInfo((Transform x) => x.localScale, "localScale"))
            {
                Vector3Editor vector3Editor = (Vector3Editor)editor;
                if (!canTransform)
                {
                    vector3Editor.IsXInteractable = false;
                    vector3Editor.IsYInteractable = false;
                    vector3Editor.IsZInteractable = false;
                }
                else if (Editor.Tools.LockAxes != null)
                {
                    vector3Editor.IsXInteractable = !Editor.Tools.LockAxes.ScaleX;
                    vector3Editor.IsYInteractable = !Editor.Tools.LockAxes.ScaleY;
                    vector3Editor.IsZInteractable = !Editor.Tools.LockAxes.ScaleZ;
                }
            }
        }

        protected override void OnValueChanged()
        {
            base.OnValueChanged();
            RefreshTransformHandles();
        }

       
        protected override void OnEndEdit()
        {
            base.OnEndEdit();
            ResetTransformHandles();
        }

        protected override void OnResetClick()
        {
            base.OnResetClick();
            ResetTransformHandles();
        }

        private static void RefreshTransformHandles()
        {
            BaseHandle[] handles = FindObjectsOfType<BaseHandle>();
            foreach (BaseHandle handle in handles)
            {
                handle.Refresh();
            }
        }

        private static void ResetTransformHandles()
        {
            BaseHandle[] handles = FindObjectsOfType<BaseHandle>();
            foreach (BaseHandle handle in handles)
            {
                handle.Targets = handle.RealTargets;
            }
        }


    }
}

