using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTTerrain
{
    public class TerrainView : RuntimeWindow
    {
        [SerializeField]
        private TerrainEditor m_terrainEditor = null;

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.Custom;
            base.AwakeOverride();

            TryRefreshTerrainEditor();
            Editor.Selection.SelectionChanged += OnSelectionChanged;
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            if(Editor != null)
            {
                Editor.Selection.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnSelectionChanged(Object[] unselectedObjects)
        {
            TryRefreshTerrainEditor();
        }

        private void TryRefreshTerrainEditor()
        {
            if (Editor.Selection.activeGameObject == null || Editor.Selection.objects.Length > 1)
            {
                m_terrainEditor.gameObject.SetActive(false);
                m_terrainEditor.Terrain = null;
            }
            else
            {
                Terrain terrain = Editor.Selection.activeGameObject.GetComponent<Terrain>();
                m_terrainEditor.Terrain = terrain;
                m_terrainEditor.gameObject.SetActive(m_terrainEditor.Terrain != null);
            }
        }
    }
}