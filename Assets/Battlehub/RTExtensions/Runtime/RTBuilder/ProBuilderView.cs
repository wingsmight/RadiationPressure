﻿using Battlehub.ProBuilderIntegration;
using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.RTHandles;
using Battlehub.UIControls;
using Battlehub.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Battlehub.RTBuilder
{
    public class ProBuilderView : RuntimeWindow
    {
        [SerializeField]
        private VirtualizingTreeView m_commandsList = null;
        [SerializeField]
        private GameObject m_uvEditor = null;
        [SerializeField]
        private GameObject m_materialPaletteEditor = null;
        [SerializeField]
        private GameObject m_smoothGroupEditor = null;
        [SerializeField]
        private EnumEditor m_modeSelector = null;
        [SerializeField]
        private bool m_useSceneViewToolbar = true;
        [SerializeField]
        private ProBuilderToolbar m_sceneViewToolbarPrefab = null;
        [SerializeField]
        private bool m_useToolbar = false;
        [SerializeField]
        private ProBuilderToolbar m_toolbar = null;

        private ToolCmd[] m_commands;
        private IProBuilderTool m_proBuilderTool;

        private bool m_canProBuilderize = false;
        private bool m_canUnproBuilderize = false;
        private bool m_isProBuilderMeshSelected = false;
        private bool m_isNonProBuilderMeshSelected = false;
        private bool m_isPolyShapeSelected = false;
        
        private IWindowManager m_wm;
        private ILocalization m_localization;

        public enum UIModes
        {
            Mesh_Editing,
            UV_Editing,
            Materials_Editing,
            Smooth_Groups,
        }

        [SerializeField]
        private UIModes m_uiMode;
        public UIModes UIMode
        {
            get { return m_uiMode; }
            set
            {
                if (m_uiMode != value)
                {
                    m_uiMode = value;
                    OnUIModeChanged();
                }
            }
        }

        private void OnUIModeChanged()
        {
            if (m_commandsList != null)
            {
                m_commandsList.gameObject.SetActive(m_uiMode == UIModes.Mesh_Editing);
            }

            if (m_uvEditor != null)
            {
                m_uvEditor.SetActive(m_uiMode == UIModes.UV_Editing);
            }

            if (m_materialPaletteEditor != null)
            {
                m_materialPaletteEditor.SetActive(m_uiMode == UIModes.Materials_Editing);
            }

            if(m_smoothGroupEditor != null)
            {
                m_smoothGroupEditor.SetActive(m_uiMode == UIModes.Smooth_Groups);
            }
        }

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.Custom;
            base.AwakeOverride();

            m_localization = IOC.Resolve<ILocalization>();
            m_wm = IOC.Resolve<IWindowManager>();
            m_wm.WindowCreated += OnWindowCreated;
            m_wm.WindowDestroyed += OnWindowDestroyed;

            m_proBuilderTool = IOC.Resolve<IProBuilderTool>();
            if(m_proBuilderTool == null)
            {
                GameObject proBuilderToolGO = new GameObject("ProBuilderTool");
                proBuilderToolGO.transform.SetParent(Editor.Root, false);
                m_proBuilderTool = proBuilderToolGO.AddComponent<ProBuilderTool>();
                proBuilderToolGO.AddComponent<ManualUVEditor>();
            }

            m_proBuilderTool.ModeChanged += OnProBuilderToolModeChanged;
            m_proBuilderTool.SelectionChanged += UpdateFlagsAndDataBindVisible;
            
            Editor.Selection.SelectionChanged += OnSelectionChanged;
            Editor.Undo.UndoCompleted += OnUndoStateChanged;
            Editor.Undo.RedoCompleted += OnUndoStateChanged;
            Editor.Undo.StateChanged += OnUndoStateChanged;

            m_commandsList.ItemClick += OnItemClick;
            m_commandsList.ItemDataBinding += OnItemDataBinding;
            m_commandsList.ItemExpanding += OnItemExpanding;
            m_commandsList.ItemBeginDrag += OnItemBeginDrag;
            m_commandsList.ItemDrop += OnItemDrop;
            m_commandsList.ItemDragEnter += OnItemDragEnter;
            m_commandsList.ItemDragExit += OnItemDragExit;
            m_commandsList.ItemEndDrag += OnItemEndDrag;

            m_commandsList.CanEdit = false;
            m_commandsList.CanReorder = false;
            m_commandsList.CanReparent = false;
            m_commandsList.CanSelectAll = false;
            m_commandsList.CanUnselectAll = true;
            m_commandsList.CanRemove = false;

            if (m_modeSelector != null)
            {
                m_modeSelector.Init(this, this, Strong.PropertyInfo((ProBuilderView x) => x.UIMode), null, m_localization.GetString("ID_RTBuilder_View_Mode", "Mode:"), null, null, null, false);
            }

            OnSelectionChanged(null);
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            if (m_wm != null)
            {
                m_wm.WindowCreated -= OnWindowCreated;
                m_wm.WindowDestroyed -= OnWindowDestroyed;
                DestroyToolbar();
            }

            if (Editor != null)
            {
                Editor.Selection.SelectionChanged -= OnSelectionChanged;
                Editor.Undo.UndoCompleted -= OnUndoStateChanged;
                Editor.Undo.RedoCompleted -= OnUndoStateChanged;
                Editor.Undo.StateChanged -= OnUndoStateChanged;
            }

            if (m_commandsList != null)
            {
                m_commandsList.ItemClick -= OnItemClick;
                m_commandsList.ItemDataBinding -= OnItemDataBinding;
                m_commandsList.ItemExpanding -= OnItemExpanding;
                m_commandsList.ItemBeginDrag -= OnItemBeginDrag;
                m_commandsList.ItemDrop -= OnItemDrop;
                m_commandsList.ItemDragEnter -= OnItemDragEnter;
                m_commandsList.ItemDragExit -= OnItemDragExit;
                m_commandsList.ItemEndDrag -= OnItemEndDrag;
            }

            if (m_proBuilderTool != null)
            {
                m_proBuilderTool.ModeChanged -= OnProBuilderToolModeChanged;
                m_proBuilderTool.SelectionChanged -= UpdateFlagsAndDataBindVisible;
                m_proBuilderTool.Mode = ProBuilderToolMode.Object;
            }
        }

        protected virtual void Start()
        {
            CreateToolbar();
            m_toolbar.gameObject.SetActive(m_useToolbar);

            UpdateFlags();
            m_commands = GetCommands().ToArray();
            m_commandsList.Items = m_commands;
            OnUIModeChanged();
        }

        private void OnProBuilderToolModeChanged(ProBuilderToolMode mode)
        {
            m_commands = GetCommands().ToArray();
            m_commandsList.Items = m_commands;
        }

        private List<ToolCmd> GetCommands()
        {
            switch (m_proBuilderTool.Mode)
            {
                case ProBuilderToolMode.Object:
                    return GetObjectCommands();
                case ProBuilderToolMode.Face:
                    return GetFaceCommands();
                case ProBuilderToolMode.Edge:
                    return GetEdgeCommands();
                case ProBuilderToolMode.Vertex:
                    return GetVertexCommands();
            }
            return new List<ToolCmd>();
        }

        private List<ToolCmd> GetObjectCommands()
        {
            List<ToolCmd> commands = GetCommonCommands();
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_ProBuilderize", "ProBuilderize"), OnProBuilderize, CanProBuilderize));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_UnproBuilderize", "UnproBuilderize"), OnUnproBuilderize, CanUnproBuilderize));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Subdivide", "Subdivide"), () => m_proBuilderTool.Subdivide(), () => m_isProBuilderMeshSelected));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_CenterPivot", "Center Pivot"), OnCenterPivot, () => m_isProBuilderMeshSelected));

            return commands;
        }

        private List<ToolCmd> GetFaceCommands()
        {
            List<ToolCmd> commands = GetCommonCommands();
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_ExtrudeFace", "Extrude Face"), OnExtrudeFace, () => m_proBuilderTool.Mode == ProBuilderToolMode.Face && m_proBuilderTool.HasSelection));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_DeleteFace", "Delete Face"), OnDelete, () => m_proBuilderTool.Mode == ProBuilderToolMode.Face && m_proBuilderTool.HasSelection));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_SubdivideFaces", "Subdivide Faces"), OnSubdivideFaces, () => m_proBuilderTool.Mode == ProBuilderToolMode.Face && m_proBuilderTool.HasSelection));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_MergeFaces", "Merge Faces"), OnMergeFaces, () => m_proBuilderTool.Mode == ProBuilderToolMode.Face && m_proBuilderTool.HasSelection));
            return commands;
        }

        private List<ToolCmd> GetEdgeCommands()
        {
            List<ToolCmd> commands = GetCommonCommands();
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_FindHoles", "Find Holes"), () => m_proBuilderTool.SelectHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_FillHoles", "Fill Holes"), () => m_proBuilderTool.FillHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_DeleteEdge", "Delete Edge"), OnDelete, () => m_proBuilderTool.Mode == ProBuilderToolMode.Edge && m_proBuilderTool.HasSelection));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_SubdivideEdges", "Subdivide Edges"), OnSubdivideEdges, () => m_proBuilderTool.Mode == ProBuilderToolMode.Edge && m_proBuilderTool.HasSelection));
            return commands;
        }

        private List<ToolCmd> GetVertexCommands()
        {
            List<ToolCmd> commands = GetCommonCommands();
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_FindHoles", "Find Holes"), () => m_proBuilderTool.SelectHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_FillHoles", "Fill Holes"), () => m_proBuilderTool.FillHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_DeleteVertex", "Delete Vertex"), OnDelete, () => m_proBuilderTool.Mode == ProBuilderToolMode.Vertex && m_proBuilderTool.HasSelection));
            return commands;
        }

        private List<ToolCmd> GetCommonCommands()
        {
            List<ToolCmd> commands = new List<ToolCmd>();
            ToolCmd newShapeCmd = new ToolCmd(m_localization.GetString("ID_RTBuilder_View_NewShape", "New Shape"), OnNewShape, true) { Arg = PBShapeType.Cube };
            newShapeCmd.Children = new List<ToolCmd>
            {
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Arch", "Arch"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Arch },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Cone", "Cone"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Cone },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Cube", "Cube"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Cube },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_CurvedStair", "Curved Stair"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.CurvedStair },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Cylinder", "Cylinder"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Cylinder },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Door", "Door"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Door },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Pipe", "Pipe"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Pipe },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Plane", "Plane"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Plane },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Prism", "Prism"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Prism },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Sphere", "Sphere"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Sphere },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Sprite", "Sprite"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Sprite },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Stair", "Stair"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Stair },
                new ToolCmd(m_localization.GetString("ID_RTBuilder_View_Torus", "Torus"), OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Torus },
            };

            commands.Add(newShapeCmd);
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_NewPolyShape", "New Poly Shape"), OnNewPolyShape, true));
            commands.Add(new ToolCmd(m_localization.GetString("ID_RTBuilder_View_EditPolyShape", "Edit Poly Shape"), OnEditPolyShape, () => m_isPolyShapeSelected));
            return commands;
        }

        private void UpdateFlags()
        {
            GameObject[] selected = Editor.Selection.gameObjects;
            if (selected != null && selected.Length > 0)
            {
                m_canProBuilderize = selected.Where(go => go.GetComponentsInChildren<MeshFilter>(true).Any(f => (f.hideFlags & HideFlags.HideInHierarchy) == 0 && f.GetComponent<PBMesh>() == null)).Any();
                m_canUnproBuilderize = selected.Where(go => go.GetComponentsInChildren<MeshFilter>(true).Any(f => f.GetComponent<PBMesh>() != null)).Any();
                m_isProBuilderMeshSelected = selected.Where(go => go.GetComponent<PBMesh>() != null).Any();
                m_isNonProBuilderMeshSelected = selected.Where(go => go.GetComponent<PBMesh>() == null).Any();
                m_isPolyShapeSelected = selected.Where(go => go.GetComponent<PBPolyShape>() != null).Count() == 1;
            }
            else
            {
                m_canProBuilderize = false;
                m_canUnproBuilderize = false;
                m_isProBuilderMeshSelected = false;
                m_isNonProBuilderMeshSelected = false;
                m_isPolyShapeSelected = false;
            }
        }

        private void OnSelectionChanged(UnityEngine.Object[] unselectedObjects)
        {
            UpdateFlagsAndDataBindVisible();
        }

        private void OnUndoStateChanged()
        {
            UpdateFlagsAndDataBindVisible();
            if(m_isProBuilderMeshSelected)
            {
                if(gameObject.activeSelf)
                {
                    StartCoroutine(CoUpdateFlagsAndDataBindVisible());
                }
            }
        }

        private IEnumerator CoUpdateFlagsAndDataBindVisible()
        {
            yield return new WaitForEndOfFrame();
            UpdateFlagsAndDataBindVisible();
        }

        private void UpdateFlagsAndDataBindVisible()
        {
            UpdateFlags();
            m_commandsList.DataBindVisible();
        }

        private void OnItemDataBinding(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>();
            ToolCmd cmd = (ToolCmd)e.Item;
            text.text = cmd.Text;

            bool isValid = cmd.Validate();
            Color color = text.color;
            color.a = isValid ? 1 : 0.5f;
            text.color = color;
          
            e.CanDrag = cmd.CanDrag;
            e.HasChildren = cmd.HasChildren;
        }

        private void OnItemExpanding(object sender, VirtualizingItemExpandingArgs e)
        {
            ToolCmd cmd = (ToolCmd)e.Item;
            e.Children = cmd.Children;
        }

        private void OnItemClick(object sender, ItemArgs e)
        {
            ToolCmd cmd = (ToolCmd)e.Items[0];
            if(cmd.Validate())
            {
                cmd.Run();
            }
        }

        private void CreateNewShape(PBShapeType type, out GameObject go, out ExposeToEditor exposeToEditor)
        {
            go = PBShapeGenerator.CreateShape(type);
            go.AddComponent<PBMesh>();

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterials.Length == 1 && renderer.sharedMaterials[0] == PBBuiltinMaterials.DefaultMaterial)
            {
                IMaterialPaletteManager paletteManager = IOC.Resolve<IMaterialPaletteManager>();
                if (paletteManager.Palette.Materials.Count > 0)
                {
                    renderer.sharedMaterial = paletteManager.Palette.Materials[0];
                }
            }

            IRuntimeEditor rte = IOC.Resolve<IRuntimeEditor>();
            RuntimeWindow scene = rte.GetWindow(RuntimeWindowType.Scene);
            Vector3 position;
            Quaternion rotation;
            GetPositionAndRotation(scene, out position, out rotation);

            exposeToEditor = go.AddComponent<ExposeToEditor>();
            go.transform.position = position + rotation * Vector3.up * exposeToEditor.Bounds.extents.y;
            go.transform.rotation = rotation;
        }

        private object OnNewShape(object arg)
        {
            GameObject go;
            ExposeToEditor exposeToEditor;
            CreateNewShape((PBShapeType)arg, out go, out exposeToEditor);

            IRuntimeEditor rte = IOC.Resolve<IRuntimeEditor>();
            RuntimeWindow scene = rte.GetWindow(RuntimeWindowType.Scene);
            IRuntimeSelectionComponent selectionComponent = null;
            if (scene != null)
            {
                selectionComponent = scene.IOCContainer.Resolve<IRuntimeSelectionComponent>();
            }

            Editor.Undo.BeginRecord();
            if (selectionComponent == null || selectionComponent.CanSelect)
            {
                Editor.Selection.activeGameObject = go;
            }

            Editor.Undo.RegisterCreatedObjects(new[] { exposeToEditor });
            Editor.Undo.EndRecord();

            return go;
        }

        private object OnNewPolyShape(object arg)
        {
            GameObject go;
            ExposeToEditor exposeToEditor;
            CreateNewShape(PBShapeType.Cube, out go, out exposeToEditor);
            exposeToEditor.SetName("Poly Shape");
          
            IRuntimeEditor rte = IOC.Resolve<IRuntimeEditor>();
            RuntimeWindow scene = rte.GetWindow(RuntimeWindowType.Scene);
          
            Vector3 position;
            Quaternion rotation;
            GetPositionAndRotation(scene, out position, out rotation);
            go.transform.position = position;
            go.transform.rotation = rotation;

            PBMesh pbMesh = go.GetComponent<PBMesh>();
            pbMesh.Clear();
            
            PBPolyShape polyShape = go.AddComponent<PBPolyShape>();
            polyShape.IsEditing = true;

            Editor.Undo.BeginRecord();
            Editor.Selection.activeGameObject = go;
            m_proBuilderTool.Mode = ProBuilderToolMode.PolyShape;
            Editor.Undo.RegisterCreatedObjects(new[] { exposeToEditor });
            Editor.Undo.EndRecord();

            return go;
        }

        private void GetPositionAndRotation(RuntimeWindow window, out Vector3 position, out Quaternion rotation, bool rotateToTerrain = false)
        {
            Ray ray = window != null ? 
                new Ray(window.Camera.transform.position, window.Camera.transform.forward) : 
                new Ray(Vector3.up * 100000, Vector3.down);

            RaycastHit[] hits = Physics.RaycastAll(ray);
            for (int i = 0; i < hits.Length; ++i)
            {
                RaycastHit hit = hits[i];
                if (hit.collider is TerrainCollider)
                {
                    position = hit.point;
                    if(rotateToTerrain)
                    {
                        rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    }
                    else
                    {
                        rotation = Quaternion.identity;
                    }
                    return;
                }
            }

            Vector3 up = Vector3.up;
            Vector3 pivot = Vector3.zero;
            if (window != null)
            {
                IScenePivot scenePivot = window.IOCContainer.Resolve<IScenePivot>();
                if (Mathf.Abs(Vector3.Dot(window.Camera.transform.up, Vector3.up)) > Mathf.Cos(Mathf.Deg2Rad))
                {
                    up = Vector3.Cross(window.Camera.transform.right, Vector3.up);
                }

                pivot = scenePivot.SecondaryPivot;
            }

            Plane dragPlane = new Plane(up, pivot);
            rotation = Quaternion.identity;
            if (!GetPointOnDragPlane(ray, dragPlane, out position))
            {
                position = window.Camera.transform.position + window.Camera.transform.forward * 10.0f;
            }
        }

        private bool GetPointOnDragPlane(Ray ray, Plane dragPlane, out Vector3 point)
        {
            float distance;
            if (dragPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }
            point = Vector3.zero;
            return false;
        }

        private void OnEditPolyShape()
        {
            m_proBuilderTool.Mode = ProBuilderToolMode.PolyShape;
        }

        private void OnCenterPivot()
        {
            m_proBuilderTool.CenterPivot();           
        }

        private bool CanProBuilderize()
        {
            return m_canProBuilderize;
        }

        private bool CanUnproBuilderize()
        {
            return m_canUnproBuilderize;
        }

        private bool IsDescendant(Transform ancestor, Transform obj)
        {
            obj = obj.parent;
            while(obj != null)
            {
                if(obj == ancestor)
                {
                    return true;
                }

                obj = obj.parent;
            }

            return false;
        }

        private class ProBuilderizer
        {
            private GameObject[] m_gameObjects;
            private Dictionary<GameObject, Mesh> m_gameObjectToMesh;
            
            public ProBuilderizer(GameObject[] gameObjects)
            {
                MeshFilter[] filters = gameObjects.SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true)).ToArray();
                m_gameObjectToMesh = filters.ToDictionary(f => f.gameObject, f => f.sharedMesh);
                m_gameObjects = gameObjects;
            }

            public void Redo()
            {
                for (int i = 0; i < m_gameObjects.Length; ++i)
                {
                    GameObject go = m_gameObjects[i];
                    Vector3 scale = go.transform.localScale;
                    float minScale = Mathf.Min(scale.x, scale.y, scale.z);
                    ProBuilderize(go, true, new Vector2(minScale, minScale));
                }
                RaiseOnSelectionChanged();
            }

            public void Undo()
            {
                foreach(var kvp in m_gameObjectToMesh)
                {
                    GameObject go = kvp.Key;
                    if(go == null)
                    {
                        continue;
                    }
                    Mesh mesh = kvp.Value;

                    MeshFilter filter = go.GetComponent<MeshFilter>();
                    if(filter)
                    {
                        filter.sharedMesh = mesh;
                    }
                    
                    PBMesh pbMesh = go.GetComponent<PBMesh>();
                    if(pbMesh)
                    {
                        pbMesh.DestroyImmediate();
                    }
                }
                RaiseOnSelectionChanged();
            }

            private static void RaiseOnSelectionChanged()
            {
                IRTE rte = IOC.Resolve<IRTE>();
                foreach (RuntimeWindow window in rte.Windows)
                {
                    if (window is ProBuilderView)
                    {
                        ProBuilderView pb = (ProBuilderView)window;
                        pb.OnSelectionChanged(null);
                    }
                }
            }


            public PBMesh ProBuilderize(GameObject gameObject, bool hierarchy, Vector2 uvScale)
            {
                bool wasActive = false;
                if (uvScale != Vector2.one)
                {
                    wasActive = gameObject.activeSelf;
                    gameObject.SetActive(false);
                }

                if (hierarchy)
                {
                    MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(true);
                    for (int i = 0; i < meshFilters.Length; ++i)
                    {
                        if (meshFilters[i].GetComponent<PBMesh>() == null)
                        {
                            ExposeToEditor exposeToEditor = meshFilters[i].GetComponent<ExposeToEditor>();
                            if (exposeToEditor != null)
                            {
                                exposeToEditor.AddComponent(typeof(PBMesh));
                                PBMesh pbMesh = exposeToEditor.GetComponent<PBMesh>();
                                PBMesh.Init(pbMesh, uvScale);
                            }
                        }
                    }

                    if (uvScale != Vector2.one)
                    {
                        gameObject.SetActive(wasActive);
                    }

                    return gameObject.GetComponent<PBMesh>();
                }
                else
                {
                    PBMesh mesh = gameObject.GetComponent<PBMesh>();
                    if (mesh != null)
                    {
                        if (uvScale != Vector2.one)
                        {
                            gameObject.SetActive(wasActive);
                        }
                        return mesh;
                    }

                    mesh = gameObject.AddComponent<PBMesh>();
                    PBMesh.Init(mesh, uvScale);
                    if (uvScale != Vector2.one)
                    {
                        gameObject.SetActive(wasActive);
                    }
                    return mesh;
                }
            }
        }

        private object OnProBuilderize(object arg)
        {
            GameObject[] gameObjects = Editor.Selection.gameObjects;
            if(gameObjects == null)
            {
                return null;
            }

            Transform[] transforms = gameObjects.Select(g => g.transform).ToArray();
            gameObjects = gameObjects.Where(g => !transforms.Any(t => IsDescendant(t, g.transform))).ToArray();

            ProBuilderizer proBuilderizer = new ProBuilderizer(gameObjects);
            Editor.Undo.CreateRecord(proBuilderizer, null, null, RedoProbuilderize, UndoProbuilderize);
            proBuilderizer.Redo();
            return null;
        }

        private object OnUnproBuilderize(object arg)
        {
            GameObject[] gameObjects = Editor.Selection.gameObjects;
            if (gameObjects == null)
            {
                return null;
            }

            Transform[] transforms = gameObjects.Select(g => g.transform).ToArray();
            gameObjects = gameObjects.Where(g => !transforms.Any(t => IsDescendant(t, g.transform))).ToArray();

            ProBuilderizer proBuilderizer = new ProBuilderizer(gameObjects);
            Editor.Undo.CreateRecord(proBuilderizer, null, null, UndoProbuilderize, RedoProbuilderize);
            proBuilderizer.Undo();
            return null;
        }

        private static bool UndoProbuilderize(Record record)
        {
            ProBuilderizer proBuilderizer = (ProBuilderizer)record.Target;
            proBuilderizer.Undo();
            return true;
        }

        private static bool RedoProbuilderize(Record record)
        {
            ProBuilderizer proBuilderizer = (ProBuilderizer)record.Target;
            proBuilderizer.Redo();
            return true;
        }

        private object OnExtrudeFace(object arg)
        {
            m_proBuilderTool.Extrude(0.01f);
            return null;
        }

        private void OnDelete()
        {
            m_proBuilderTool.Delete();
        }

        private void OnSubdivideFaces()
        {
            m_proBuilderTool.SubdivideFaces();
        }

        private void OnMergeFaces()
        {
            m_proBuilderTool.MergeFaces();
        }

        private void OnSubdivideEdges()
        {
            m_proBuilderTool.SubdivideEdges();
        }

        private void OnItemBeginDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseBeginDrag(this, e.Items, e.PointerEventData);
        }

        private void OnItemDragEnter(object sender, ItemDropCancelArgs e)
        {
            Editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
            e.Cancel = true;
        }

        private void OnItemDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseDrag(e.PointerEventData);
        }

        private void OnItemDragExit(object sender, EventArgs e)
        {
            Editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
        }

        private void OnItemDrop(object sender, ItemDropArgs e)
        {
            Editor.DragDrop.RaiseDrop(e.PointerEventData);
        }

        private void OnItemEndDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseDrop(e.PointerEventData);
        }

        private void CreateToolbar()
        {
            Transform[] scenes = m_wm.GetWindows(RuntimeWindowType.Scene.ToString());
            for(int i = 0; i < scenes.Length; ++i)
            {
                RuntimeWindow window = scenes[i].GetComponent<RuntimeWindow>();
                CreateToolbar(scenes[i], window);
            }
        }

        private void DestroyToolbar()
        {
            Transform[] scenes = m_wm.GetWindows(RuntimeWindowType.Scene.ToString());
            for(int i = 0; i < scenes.Length; ++i)
            {
                RuntimeWindow window = scenes[i].GetComponent<RuntimeWindow>();
                DestroyToolbar(scenes[i], window);
            }
        }

        private void OnWindowCreated(Transform windowTransform)
        {
            RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
            CreateToolbar(windowTransform, window);
        }

        private void CreateToolbar(Transform windowTransform, RuntimeWindow window)
        {
            if(m_useSceneViewToolbar)
            {
                if (window != null && window.WindowType == RuntimeWindowType.Scene)
                {
                    if (m_sceneViewToolbarPrefab != null)
                    {
                        RectTransform rt = (RectTransform)Instantiate(m_sceneViewToolbarPrefab, windowTransform, false).transform;
                        rt.Stretch();
                    }
                }
            }
        }

        private void OnWindowDestroyed(Transform windowTransform)
        {
            if (m_useSceneViewToolbar)
            {
                RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
                DestroyToolbar(windowTransform, window);
            }
        }

        private void DestroyToolbar(Transform windowTransform, RuntimeWindow window)
        {
            if (window != null && window.WindowType == RuntimeWindowType.Scene)
            {
                if (m_sceneViewToolbarPrefab != null)
                {
                    ProBuilderToolbar toolbar = windowTransform.GetComponentInChildren<ProBuilderToolbar>();
                    if (toolbar != null)
                    {
                        Destroy(toolbar.gameObject);
                    }
                }
            }
        }
    }
}


