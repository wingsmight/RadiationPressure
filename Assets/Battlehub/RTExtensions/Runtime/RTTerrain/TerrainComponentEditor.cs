﻿using Battlehub.RTEditor;
using System.Linq;
using UnityEngine;

namespace Battlehub.RTTerrain
{
    public class TerrainComponentEditor : ComponentEditor
    {
        [SerializeField]
        private TerrainEditor m_terrainEditor = null;

        [SerializeField]
        private GameObject m_multiComponentEditingProhibited = null;

        public override Component[] Components
        {
            get { return base.Components; }
            set
            {
                base.Components = value;

                if (value != null)
                {
                    m_terrainEditor.gameObject.SetActive(value.Length == 1);
                    m_terrainEditor.Terrain = value.OfType<Terrain>().FirstOrDefault();

                    if(m_multiComponentEditingProhibited != null)
                    {
                        m_multiComponentEditingProhibited.SetActive(value.Length > 1);
                    }
                }
            }
        }

        protected override void DestroyEditor()
        {
            DestroyGizmos();
        }

        protected override void BuildEditor(IComponentDescriptor componentDescriptor, PropertyDescriptor[] descriptors)
        {
            
        }
    }
}
