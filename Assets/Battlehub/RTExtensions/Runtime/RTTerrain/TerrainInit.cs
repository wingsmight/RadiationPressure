﻿using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.UIControls.MenuControl;
using UnityEngine;
namespace Battlehub.RTTerrain
{
    [MenuDefinition(-1)]
    [DefaultExecutionOrder(-1)]
    public class TerrainInit : EditorExtension
    {
        [SerializeField]
        private GameObject m_terrainView = null;

        [SerializeField]
        private TerrainComponentEditor m_terrainComponentEditor = null;

        [SerializeField]
        private GameObject[] m_prefabs = null;

        [SerializeField]
        private TerrainProjectorBase m_terrainProjectorPrefab = null;
        private TerrainProjectorBase InstantiateTerrainProjector()
        {
            return Instantiate(m_terrainProjectorPrefab);
        }

        [SerializeField]
        private TerrainAreaProjector m_terrainAreaProjectorPrefab = null;
        private TerrainAreaProjector m_terrainAreaProjector;
        private ITerrainAreaProjector InstantiateTerrainAreaProjector()
        {
            if(m_terrainAreaProjector == null)
            {
                m_terrainAreaProjector = Instantiate(m_terrainAreaProjectorPrefab);
            }
            return m_terrainAreaProjector;
        }

        protected override void OnEditorExist()
        {
            base.OnEditorExist();

            if(IOC.Resolve<ITerrainSettings>() == null && gameObject.GetComponent<TerrainSettings>() == null)
            {
                gameObject.AddComponent<TerrainSettings>();
            }
            if(IOC.Resolve<ITerrainCutoutMaskRenderer>() == null && gameObject.GetComponent<TerrainCutoutMaskRenderer>() == null)
            {
                gameObject.AddComponent<TerrainCutoutMaskRenderer>();
            }

            Register();
            IOC.RegisterFallback(InstantiateTerrainProjector);
            IOC.RegisterFallback(InstantiateTerrainAreaProjector);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            IOC.UnregisterFallback(InstantiateTerrainProjector);
            IOC.UnregisterFallback(InstantiateTerrainAreaProjector);
            if (m_terrainAreaProjector != null)
            {
                Destroy(m_terrainAreaProjector);
            }
        }

        protected override void OnEditorClosed()
        {
            base.OnEditorClosed();
            IOC.UnregisterFallback(InstantiateTerrainProjector);
            IOC.UnregisterFallback(InstantiateTerrainAreaProjector);
            if (m_terrainAreaProjector != null)
            {
                Destroy(m_terrainAreaProjector);
            }
        }

        private void Register()
        {
            ILocalization lc = IOC.Resolve<ILocalization>();
            lc.LoadStringResources("RTTerrain.StringResources");

            IRTEAppearance appearance = IOC.Resolve<IRTEAppearance>();
            IWindowManager wm = IOC.Resolve<IWindowManager>();
            if (m_terrainView != null)
            {
                RegisterWindow(wm, "TerrainEditor", lc.GetString("ID_RTTerrain_WM_Header_TerrainEditor", "Terrain Editor"),
                    Resources.Load<Sprite>("icons8-earth-element-24"), m_terrainView, false);

                if(appearance != null)
                {
                    appearance.RegisterPrefab(m_terrainView);
                }
            }

            if(m_terrainComponentEditor != null)
            {
                IEditorsMap editorsMap = IOC.Resolve<IEditorsMap>();
                if(!editorsMap.HasMapping(typeof(Terrain)))
                {
                    editorsMap.AddMapping(typeof(Terrain), m_terrainComponentEditor.gameObject, true, false);
                    if (appearance != null)
                    {
                        appearance.RegisterPrefab(m_terrainComponentEditor.gameObject);
                    }
                }
            }

            if (appearance != null)
            {
                foreach (GameObject prefab in m_prefabs)
                {
                    if (prefab != null)
                    {
                        appearance.RegisterPrefab(prefab);
                    }
                }
            }
        }

        private void RegisterWindow(IWindowManager wm, string typeName, string header, Sprite icon, GameObject prefab, bool isDialog)
        {
            wm.RegisterWindow(new CustomWindowDescriptor
            {
                IsDialog = isDialog,
                TypeName = typeName,
                Descriptor = new WindowDescriptor
                {
                    Header = header,
                    Icon = icon,
                    MaxWindows = 1,
                    ContentPrefab = prefab
                }
            });
        }

        //[MenuCommand("MenuWindow/ID_RTTerrain_WM_Header_TerrainEditor", "", true)]
        public static void OpenTerrainEditor()
        {
            IWindowManager wm = IOC.Resolve<IWindowManager>();
            wm.CreateWindow("TerrainEditor");
        }

        [MenuCommand("MenuGameObject/3D Object/ID_RTTerrain_MenuGameObject_Terrain", "", true)]
        public static void CreateTerrain()
        {
            IRTE editor = IOC.Resolve<IRTE>();
            TerrainData terrainData = TerrainDataExt.DefaultTerrainData();
            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            if (RenderPipelineInfo.Type != RPType.Standard)
            {
                terrainObject.GetComponent<Terrain>().materialTemplate = RenderPipelineInfo.DefaultTerrainMaterial;
            }
            terrainObject.isStatic = false;
            if (terrainObject != null)
            {
                editor.AddGameObjectToScene(terrainObject);
            }
        }
    }
}


