﻿using UnityEngine;
using Battlehub.Utils;
using System.Linq;

namespace Battlehub.RTGizmos
{
    public class AudioReverbZoneGizmo : SphereGizmo
    {
        [SerializeField]
        private AudioReverbZone m_source;

        [SerializeField]
        [HideInInspector]
        private bool m_max = true;

        protected override Vector3 Center
        {
            get { return Vector3.zero; }
            set { }
        }

        protected override float Radius
        {
            get
            {
                if (m_source == null)
                {
                    return 0;
                }
                
                if(m_max)
                {
                    return m_source.maxDistance;
                }
                else
                {
                    return m_source.minDistance;
                }
            }
            set
            {
                if (m_source != null)
                {
                    if (m_max)
                    {
                        m_source.maxDistance = value;
                    }
                    else
                    {
                        m_source.minDistance = value;
                    }
                }
            }
        }

        protected override void Awake()
        {
            if (m_source == null)
            {
                m_source = GetComponent<AudioReverbZone>();
            }

            if (m_source == null)
            {
                Debug.LogError("Set AudioSource");
            }

            if (gameObject.GetComponents<AudioReverbZoneGizmo>().Count(a => a.m_source == m_source) == 1)
            {
                AudioReverbZoneGizmo gizmo = gameObject.AddComponent<AudioReverbZoneGizmo>();
                gizmo.LineColor = LineColor;
                gizmo.HandlesColor = HandlesColor;
                gizmo.SelectionColor = SelectionColor;
                gizmo.SelectionMargin = SelectionMargin;
                gizmo.EnableUndo = EnableUndo;
                gizmo.m_max = !m_max;
            }

            base.Awake();
        }

        protected override void BeginRecord()
        {
            base.BeginRecord();
            Window.Editor.Undo.BeginRecordValue(m_source, Strong.PropertyInfo((AudioReverbZone x) => x.minDistance, "minDistance"));
            Window.Editor.Undo.BeginRecordValue(m_source, Strong.PropertyInfo((AudioReverbZone x) => x.maxDistance, "maxDistance"));
        }

        protected override void EndRecord()
        {
            base.EndRecord();
            Window.Editor.Undo.EndRecordValue(m_source, Strong.PropertyInfo((AudioReverbZone x) => x.minDistance, "minDistance"));
            Window.Editor.Undo.EndRecordValue(m_source, Strong.PropertyInfo((AudioReverbZone x) => x.maxDistance, "maxDistance"));
        }

        private void Reset()
        {
            LineColor = new Color(0.375f, 0.75f, 1, 0.5f);
            HandlesColor = new Color(0.375f, 0.75f, 1, 0.5f);
            SelectionColor = new Color(1.0f, 1.0f, 0, 1.0f);
        }
    }
}

