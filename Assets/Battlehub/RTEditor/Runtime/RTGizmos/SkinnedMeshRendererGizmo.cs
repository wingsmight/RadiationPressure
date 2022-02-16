﻿using UnityEngine;
using System.Collections;
using Battlehub.RTCommon;
using Battlehub.Utils;

namespace Battlehub.RTGizmos
{
    public class SkinnedMeshRendererGizmo : BoxGizmo
    {
        [SerializeField]
        private SkinnedMeshRenderer m_skinnedMeshRenderer;
        private static readonly Bounds m_zeroBounds = new Bounds();
        protected override Bounds Bounds
        {
            get
            {
                if (m_skinnedMeshRenderer == null)
                {
                    return m_zeroBounds;
                }
                return m_skinnedMeshRenderer.localBounds;
            }
            set
            {
                if (m_skinnedMeshRenderer != null)
                {
                    m_skinnedMeshRenderer.localBounds = value;
                }
            }
        }

        protected override void Awake()
        {
            if (m_skinnedMeshRenderer == null)
            {
                m_skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            }

            if (m_skinnedMeshRenderer == null)
            {
                Debug.LogError("Set SkinnedMeshRenderer");
            }
            else
            {
                Target = m_skinnedMeshRenderer.rootBone;
            }

            base.Awake();
        }

        protected override void BeginRecord()
        {
            base.BeginRecord();
            Window.Editor.Undo.BeginRecordValue(m_skinnedMeshRenderer, Strong.PropertyInfo((SkinnedMeshRenderer x) => x.localBounds, "localBounds"));
        }

        protected override void EndRecord()
        {
            base.EndRecord();
            Window.Editor.Undo.EndRecordValue(m_skinnedMeshRenderer, Strong.PropertyInfo((SkinnedMeshRenderer x) => x.localBounds, "localBounds"));
        }

        private void Reset()
        {
            LineColor = new Color(0.75f, 0.75f, 0.75f, 0.75f);
            HandlesColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
            SelectionColor = new Color(1.0f, 1.0f, 0, 1.0f);
        }
    }

}
