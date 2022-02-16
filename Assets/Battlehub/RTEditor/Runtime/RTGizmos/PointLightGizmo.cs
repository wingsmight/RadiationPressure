﻿using UnityEngine;
using Battlehub.RTCommon;
using Battlehub.Utils;

namespace Battlehub.RTGizmos
{
    public class PointLightGizmo : SphereGizmo
    {
        [SerializeField]
        private Light m_light;

       
        protected override Vector3 Center
        {
            get { return Vector3.zero; }
            set {                      }
        }

        protected override float Radius
        {
            get
            {
                if (m_light == null)
                {
                    return 0;
                }

                return m_light.range;
            }
            set
            {
                if (m_light != null)
                {
                    m_light.range = value;
                }
            }
        }


        protected override void Awake()
        {
            if (m_light == null)
            {
                m_light = GetComponent<Light>();
            }

            if (m_light == null)
            {
                Debug.LogError("Set Light");
            }

            if(m_light.type != LightType.Point)
            {
                Debug.LogWarning("m_light.Type != LightType.Point");
            }

            base.Awake();
        }


        protected override void BeginRecord()
        {
            base.BeginRecord();
            Window.Editor.Undo.BeginRecordValue(m_light, Strong.PropertyInfo((Light x) => x.range, "range"));
        }

        protected override void EndRecord()
        {
            base.EndRecord();
            Window.Editor.Undo.EndRecordValue(m_light, Strong.PropertyInfo((Light x) => x.range, "range"));
        }

        //protected override Matrix4x4 HandlesTransform
        //{
        //    get
        //    {
        //        return Matrix4x4.TRS(Target.TransformPoint(Center), Quaternion.identity, Target.localScale * Radius);
        //    }
        //}

      

        //protected override void DrawOverride()
        //{
        //    if (Target == null)
        //    {
        //        return;
        //    }

        //    Vector3 scale = Target.localScale * Radius;
        //    RuntimeGizmos.DrawCubeHandles(Target.TransformPoint(Center), Quaternion.identity, scale, HandlesColor);
        //    RuntimeGizmos.DrawWireSphereGL(Target.TransformPoint(Center), Quaternion.identity, scale, LineColor);

        //    if (IsDragging)
        //    {
        //        Matrix4x4 m = Matrix4x4.TRS(Target.transform.position, Quaternion.identity, Target.transform.localScale);
        //        RuntimeGizmos.DrawSelection(m.MultiplyPoint(Center + Vector3.Scale(HandlesPositions[DragIndex], scale)), Quaternion.identity, scale, SelectionColor);
        //    }
        //}

        private void Reset()
        {
            LineColor = new Color(1, 1, 0.5f, 0.5f);
            HandlesColor = new Color(1, 1, 0.35f, 0.95f);
            SelectionColor = new Color(1.0f, 1.0f, 0, 1.0f);
        }
    }
}

