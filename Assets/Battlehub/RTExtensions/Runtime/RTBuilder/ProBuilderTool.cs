using Battlehub.ProBuilderIntegration;
using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.RTHandles;
using Battlehub.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.RTBuilder
{
    public enum ProBuilderToolMode
    {
        Object = 0,
        Vertex = 1,
        Edge = 2,
        Face = 3,
        PolyShape = 4,
    }

    public interface IProBuilderTool
    {
        event Action SelectionChanging;
        event Action SelectionChanged;
        event Action<ProBuilderToolMode> ModeChanged;
        event Action MeshesChanged;

        ProBuilderToolMode Mode
        {
            get;
            set;
        }

        bool HasSelection
        {
            get;
        }

        bool HasSelectedFaces
        {
            get;
        }

        bool HasSelectedManualUVs
        {
            get;
        }

        bool HasSelectedAutoUVs
        {
            get;
        }

        PBAutoUnwrapSettings UV
        {
            get;
        }

        event Action<bool> UVEditingModeChanged;
        bool UVEditingMode
        {
            get;
            set;
        }

        IMeshEditor GetEditor();
        void TryUpdatePivotTransform();

        void ApplyMaterial(Material material);
        void ApplyMaterial(Material material, int submeshIndex);
        void ApplyMaterial(Material material, Camera camera, Vector3 mousePosition);
        void SelectFaces(Material material);
        void UnselectFaces(Material material);
        void Extrude(float value);
        void Delete();
        void SubdivideFaces();
        void MergeFaces();
        void SubdivideEdges();
        void SelectHoles();
        void FillHoles();
        void CenterPivot();
        void Subdivide();
        void GroupFaces();
        void UngroupFaces();
        void SelectFaceGroup();
        void ConvertUVs(bool auto);
        void ResetUVs();

        void RecordState(MeshEditorState oldState, MeshEditorState newState, bool raiseMeshChanged = false);
    }

    [DefaultExecutionOrder(-90)]
    public class ProBuilderTool : MonoBehaviour, IProBuilderTool
    {
        public event Action<ProBuilderToolMode> ModeChanged;
        public event Action<bool> UVEditingModeChanged;
        public event Action SelectionChanging;
        public event Action SelectionChanged;
        public event Action MeshesChanged;

        private bool m_modeChaning;
        private ProBuilderToolMode m_mode = ProBuilderToolMode.Object;
        public ProBuilderToolMode Mode
        {
            get { return m_mode; }
            set
            {
                try
                {
                    if(m_modeChaning)
                    {
                        return;
                    }

                    m_modeChaning = true;
                    ProBuilderToolMode oldMode = m_mode;
                    m_mode = value;

                    OnCurrentModeChanged(oldMode);
                    if (ModeChanged != null)
                    {
                        ModeChanged(oldMode);
                    }
                }
                finally
                {
                    m_modeChaning = false;
                }

              
            }
        }

        public bool HasSelection
        {
            get
            {
                IMeshEditor editor = GetEditor();
                return editor != null && editor.HasSelection;
            }
        }

        public bool HasSelectedFaces
        {
            get
            {
                IMeshEditor editor = GetEditor();
                if (editor == null || !editor.HasSelection)
                {
                    return false;
                }

                MeshSelection selection = editor.GetSelection();
                selection = selection.ToFaces(false, false);

                return selection.HasFaces;
            }
        }

        private PBAutoUnwrapSettings m_uv;
        public PBAutoUnwrapSettings UV
        {
            get { return m_uv; }
        }

        public bool HasSelectedManualUVs
        {
            get
            {
                IMeshEditor editor = GetEditor();
                if (editor == null || !editor.HasSelection)
                {
                    return false;
                }
                MeshSelection selection = editor.GetSelection();
                return m_autoUVEditor.HasAutoUV(selection, false);
            }
        }

        public bool HasSelectedAutoUVs
        {
            get
            {
                IMeshEditor editor = GetEditor();
                if (editor == null || !editor.HasSelection)
                {
                    return false;
                }
                MeshSelection selection = editor.GetSelection();
                return m_autoUVEditor.HasAutoUV(selection, true);
            }
        }


        private bool m_uvEditingMode = false;
        public bool UVEditingMode
        {
            get { return m_uvEditingMode; }
            set
            {
                if (m_uvEditingMode != value)
                {
                    bool oldMode = m_uvEditingMode;
                    m_uvEditingMode = value;

                    LockAxes lockAxes = m_pivot.gameObject.GetComponent<LockAxes>();
                    lockAxes.RotationX = m_uvEditingMode;
                    lockAxes.RotationY = m_uvEditingMode;
                    lockAxes.RotationScreen = m_uvEditingMode;
                    lockAxes.ScaleZ = m_uvEditingMode;
                    lockAxes.PositionZ = m_uvEditingMode;
                    lockAxes.PivotRotation = m_uvEditingMode;
                    lockAxes.PivotRotationValue = RuntimePivotRotation.Local;

                    if (m_rte.Selection.IsSelected(m_pivot.gameObject))
                    {
                        m_rte.Selection.Select(null, null);
                        m_rte.Selection.Select(m_pivot.gameObject, new[] { m_pivot.gameObject });
                    }

                    foreach (IMeshEditor editor in m_meshEditors)
                    {
                        if (editor == null)
                        {
                            continue;
                        }
                        editor.UVEditingMode = m_uvEditingMode;
                    }

                    IMeshEditor currentEditor = GetEditor();
                    if (currentEditor != null)
                    {
                        PivotPosition = currentEditor.Position;
                        PivotRotation = GetPivotRotation(currentEditor);
                    }

                    if(m_uvEditingMode)
                    {
                        CurrentSelection = CurrentSelection;
                    }

                    if (UVEditingModeChanged != null)
                    {
                        UVEditingModeChanged(oldMode);
                    }
                }
            }
        }

        private bool IsDepthTestEnabled(RuntimeWindow window)
        {
            return window.GetComponentInChildren<Wireframe>(true) == null;
        }

        private Vector3 m_pivotPosition;
        private Vector3 PivotPosition
        {
            get { return m_pivot.position; }
            set
            {
                m_pivot.position = value;
                m_pivotPosition = value;
            }
        }

        private Quaternion PivotRotation
        {
            get { return m_pivot.rotation; }
            set { m_pivot.rotation = value; }
        }

        private Vector3 PivotLocalScale
        {
            get { return m_pivot.localScale; }
            set { m_pivot.localScale = value; }
        }

        public IMeshEditor GetEditor()
        {
            return m_meshEditors[(int)m_mode];
        }

        private MeshSelection m_faceGroupSelection;
        private MeshSelection m_currentSelection;
        private MeshSelection CurrentSelection
        {
            get { return m_currentSelection; }
            set
            {
                m_currentSelection = value;
                if(m_currentSelection == null)
                {
                    m_faceGroupSelection = null;
                }
                else
                {
                    if(UVEditingMode)
                    {
                        m_faceGroupSelection = m_autoUVEditor.SelectFaceGroup(m_currentSelection);
                    }
                    else
                    {
                        m_faceGroupSelection = null;
                    }
                }
            }
        }

        private IRuntimeSelectionComponent m_selectionComponent;
        private IMeshEditor[] m_meshEditors;
        private IMaterialEditor m_materialEditor;
        private IAutoUVEditor m_autoUVEditor;
        private IWindowManager m_wm;
        private IRTE m_rte;
        private IRuntimeEditor m_runtimeEditor;
        private IBoxSelection m_boxSelection;
        private Transform m_pivot;
        private IPolyShapeEditor m_polyShapeEditor;

        private Vector2 m_initialUVOffset;
        private PBTextureMoveTool m_textureMoveTool = new PBTextureMoveTool();
        private Vector3 m_initialRight;
        private Quaternion m_initialRotation;
        private float m_initialUVRotation;
        private PBTextureRotateTool m_textureRotateTool = new PBTextureRotateTool();
        private Vector2 m_initialUVScale;
        private PBTextureScaleTool m_textureScaleTool = new PBTextureScaleTool();
        private bool m_hasManualUVs;
        private bool m_hasAutoUVs;

        private void Awake()
        {
            IOC.RegisterFallback<IProBuilderTool>(this);

            m_runtimeEditor = IOC.Resolve<IRuntimeEditor>();
            m_rte = IOC.Resolve<IRTE>();
            m_wm = IOC.Resolve<IWindowManager>();
            if(m_wm != null)
            {
                m_wm.WindowCreated += OnWindowCreated;
                m_wm.AfterLayout += OnAfterLayout;
            }
            
            InitCullingMask();

            gameObject.AddComponent<MaterialPaletteManager>();
            m_materialEditor = gameObject.AddComponent<PBMaterialEditor>();
            m_autoUVEditor = gameObject.AddComponent<PBAutoUVEditor>();
            m_polyShapeEditor = gameObject.AddComponent<ProBuilderPolyShapeEditor>();

            IOC.RegisterFallback<IMaterialEditor>(m_materialEditor);

            m_uv = new PBAutoUnwrapSettings();
            m_uv.Changed += OnUVChanged;

            bool wasActive = gameObject.activeSelf;
            gameObject.SetActive(false);
            PBVertexEditor vertexEditor = gameObject.AddComponent<PBVertexEditor>();
            PBEdgeEditor edgeEditor = gameObject.AddComponent<PBEdgeEditor>();
            PBFaceEditor faceEditor = gameObject.AddComponent<PBFaceEditor>();

            m_meshEditors = new IMeshEditor[5];
            m_meshEditors[(int)ProBuilderToolMode.Vertex] = vertexEditor;
            m_meshEditors[(int)ProBuilderToolMode.Edge] = edgeEditor;
            m_meshEditors[(int)ProBuilderToolMode.Face] = faceEditor;

            foreach (IMeshEditor editor in m_meshEditors)
            {
                if (editor == null)
                {
                    continue;
                }
                editor.GraphicsLayer = m_rte.CameraLayerSettings.AllScenesLayer;
                editor.CenterMode = m_rte.Tools.PivotMode == RuntimePivotMode.Center;
            }
            UpdateGlobalMode();

            m_pivot = new GameObject("Pivot").transform;
            m_pivot.gameObject.hideFlags = HideFlags.HideInHierarchy;

            LockAxes lockAxes = m_pivot.gameObject.AddComponent<LockAxes>();
            lockAxes.RotationFree = true;
            m_pivot.SetParent(transform, false);

            ExposeToEditor exposed = m_pivot.gameObject.AddComponent<ExposeToEditor>();
            exposed.CanDelete = false;
            exposed.CanDuplicate = false;
            exposed.CanInspect = false;

            gameObject.SetActive(wasActive);
        }

        private void Start()
        {
            SetCanSelect(Mode == ProBuilderToolMode.Object);

            if (m_rte != null)
            {
                if (m_rte.ActiveWindow != null && m_rte.ActiveWindow.WindowType == RuntimeWindowType.Scene)
                {
                    m_selectionComponent = m_rte.ActiveWindow.IOCContainer.Resolve<IRuntimeSelectionComponent>();
                    m_boxSelection = m_rte.ActiveWindow.IOCContainer.Resolve<IBoxSelection>();

                    SubscribeToEvents();
                }

                m_rte.Selection.SelectionChanged += OnEditorSelectionChanged;
                m_rte.Tools.ToolChanged += OnEditorToolChanged;
                m_rte.ActiveWindowChanged += OnActiveWindowChanged;
                m_rte.Tools.PivotModeChanging += OnPivotModeChanging;
                m_rte.Tools.PivotModeChanged += OnPivotModeChanged;
                m_rte.Tools.PivotRotationChanging += OnPivotRotationChanging;
                m_rte.Tools.PivotRotationChanged += OnPivotRotationChanged;

                if(m_runtimeEditor != null)
                {
                    m_runtimeEditor.SceneLoading += OnSceneLoading;
                }
            }
        }

        private void OnDestroy()
        {
            Mode = ProBuilderToolMode.Object;

            IOC.UnregisterFallback<IProBuilderTool>(this);
            IOC.UnregisterFallback<IMaterialEditor>(m_materialEditor);

            if (m_rte != null)
            {
                m_rte.Selection.SelectionChanged -= OnEditorSelectionChanged;
                m_rte.Tools.ToolChanged -= OnEditorToolChanged;
                m_rte.ActiveWindowChanged -= OnActiveWindowChanged;
                m_rte.Tools.PivotModeChanging -= OnPivotModeChanging;
                m_rte.Tools.PivotModeChanged -= OnPivotModeChanged;
                m_rte.Tools.PivotRotationChanging -= OnPivotRotationChanging;
                m_rte.Tools.PivotRotationChanged -= OnPivotRotationChanged;

                if (m_runtimeEditor != null)
                {
                    m_runtimeEditor.SceneLoading -= OnSceneLoading;
                }
               
            }

            UnsubscribeFromEvents();

            if (m_wm != null)
            {
                m_wm.WindowCreated -= OnWindowCreated;
                m_wm.AfterLayout -= OnAfterLayout;
            }

            for (int i = 0; i < m_meshEditors.Length; ++i)
            {
                MonoBehaviour meshEditor = m_meshEditors[i] as MonoBehaviour;
                if (meshEditor != null)
                {
                    Destroy(meshEditor);
                }
            }
            if (m_materialEditor != null)
            {
                Destroy(m_materialEditor as MonoBehaviour);
            }

            if (m_autoUVEditor != null)
            {
                Destroy(m_autoUVEditor as MonoBehaviour);
            }

            if (m_polyShapeEditor != null)
            {
                Destroy(m_polyShapeEditor as MonoBehaviour);
            }

            if (m_pivot != null)
            {
                Destroy(m_pivot.gameObject);
            }

            foreach (RuntimeWindow window in m_rte.Windows)
            {
                if (window.WindowType == RuntimeWindowType.Scene)
                {
                    ResetCullingMask(window);
                }
            }

            m_uv.Changed -= OnUVChanged;
        }

        private void LateUpdate()
        {
            if (m_rte.ActiveWindow == null)
            {
                return;
            }

            if (m_rte.ActiveWindow.WindowType != RuntimeWindowType.Scene)
            {
                return;
            }

            if (!m_rte.ActiveWindow.Camera)
            {
                return;
            }

            if (m_mode == ProBuilderToolMode.PolyShape)
            {
                return;
            }

            IMeshEditor meshEditor = GetEditor();
            if (meshEditor == null)
            {
                return;
            }

            if (m_rte.Tools.ActiveTool != null)
            {
                if (UVEditingMode)
                {
                    if (m_selectionComponent.PositionHandle != null && m_selectionComponent.PositionHandle.IsDragging)
                    {
                        if (m_hasAutoUVs)
                        {
                            Vector2 uv = (Quaternion.Inverse(PivotRotation) * (PivotPosition - meshEditor.Position));
                            if (!UV.flipU)
                            {
                                uv.x = -uv.x;
                            }

                            if (UV.flipV)
                            {
                                uv.x = -uv.x;
                            }

                            if (UV.swapUV)
                            {
                                uv.x = -uv.x;
                            }

                            UV.offset = m_initialUVOffset + (Vector2.one * Vector2.Scale(uv, UV.scale));
                        }
                        else if (m_hasManualUVs)
                        {
                            m_textureMoveTool.Drag(PivotPosition, PivotRotation, PivotLocalScale);
                        }
                    }
                    else if (m_selectionComponent.RotationHandle != null && m_selectionComponent.RotationHandle.IsDragging)
                    {
                        if (m_hasAutoUVs)
                        {
                            Vector3 fwd = m_pivot.forward;
                            if(Mathf.Sign(UV.scale.x) != Mathf.Sign(UV.scale.y))
                            {
                                fwd = -fwd;
                            }

                            UV.rotation = m_initialUVRotation - Vector3.SignedAngle(m_initialRight, m_pivot.right, fwd);
                        }
                        else if (m_hasManualUVs)
                        {
                            m_textureRotateTool.Drag(m_pivot);
                        }
                    }
                    else if (m_selectionComponent.ScaleHandle != null && m_selectionComponent.ScaleHandle.IsDragging)
                    {
                        if (m_hasAutoUVs)
                        {
                            Vector2 scale = PivotLocalScale;
                    
                            if (UV.swapUV)
                            {
                                float x = scale.x;
                                scale.x = scale.y;
                                scale.y = x;
                            }

                            if (Mathf.Approximately(scale.x, 0))
                            {
                                scale.x = Mathf.Epsilon;
                            }
                            if (Mathf.Approximately(scale.y, 0))
                            {
                                scale.y = Mathf.Epsilon;
                            }

                            Vector2 s = new Vector2(1 / scale.x, 1 / scale.y);
                            UV.scale = Vector2.Scale(m_initialUVScale, s);
                        }
                        else
                        {
                            m_textureScaleTool.Drag(PivotPosition, PivotRotation, PivotLocalScale);
                        }
                    }
                }
                else
                {
                    if (m_selectionComponent.PositionHandle != null && m_selectionComponent.PositionHandle.IsDragging)
                    {
                        meshEditor.Position = PivotPosition;
                    }
                    else if (m_selectionComponent.RotationHandle != null && m_selectionComponent.RotationHandle.IsDragging)
                    {
                        meshEditor.Rotate(PivotRotation);
                    }
                    else if (m_selectionComponent.ScaleHandle != null && m_selectionComponent.ScaleHandle.IsDragging)
                    {
                        Vector3 localScale = PivotLocalScale;
                        if (Mathf.Approximately(localScale.x, 0))
                        {
                            localScale.x = 0.00001f;
                        }
                        if (Mathf.Approximately(localScale.y, 0))
                        {
                            localScale.y = 0.000001f;
                        }
                        if (Mathf.Approximately(localScale.z, 0))
                        {
                            localScale.z = 0.000001f;
                        }
                        PivotLocalScale = localScale;
                        meshEditor.Scale(PivotLocalScale, PivotRotation);
                    }
                }

                if(MeshesChanged != null)
                {
                    MeshesChanged();
                }
            }
            else
            {
                if (m_pivotPosition != m_pivot.position)
                {
                    PivotPosition = m_pivot.position;
                    meshEditor.Position = PivotPosition;
                }

                if (!m_rte.ActiveWindow.IsPointerOver)
                {
                    return;
                }

                RuntimeWindow window = m_rte.ActiveWindow;
                meshEditor.Hover(window.Camera, m_rte.Input.GetPointerXY(0));

                if (m_rte.Input.GetPointerDown(0))
                {
                    bool ctrl = m_rte.Input.GetKey(KeyCode.LeftControl);
                    bool shift = m_rte.Input.GetKey(KeyCode.LeftShift);

                    bool depthTest = IsDepthTestEnabled(window);

                    m_rte.Undo.BeginRecord();

                    if(m_rte.Selection.activeGameObject != m_pivot.gameObject)
                    {
                        CurrentSelection = null;
                    }

                    MeshSelection oldSelection = CurrentSelection;
                    if (meshEditor.Select(window.Camera, m_rte.Input.GetPointerXY(0), shift, ctrl, depthTest) != null)
                    {
                        CurrentSelection = meshEditor.GetSelection();
                        
                        if (UVEditingMode && m_rte.Input.GetKey(KeyCode.S) && oldSelection != null && CurrentSelection != null)
                        {
                            PBMesh prevMesh = oldSelection.GetSelectedMeshes().FirstOrDefault();
                            PBMesh mesh = CurrentSelection.GetSelectedMeshes().FirstOrDefault();
                            IEnumerable<int> oldFaces = oldSelection.GetFaces(mesh);
                            IEnumerable<int> newFaces = CurrentSelection.GetFaces(mesh);
                            if (prevMesh == mesh && oldFaces.Count() == 1 && newFaces.Count() == 1 && oldFaces.First() != newFaces.First())
                            {
                                MeshEditorState oldState = meshEditor.GetState(true);
                                PBUVEditing.AutoStitch(mesh, oldFaces.First(), newFaces.First(), 0);
                                MeshEditorState newState = meshEditor.GetState(true);
                                RecordStateAndSelection(oldState, newState, oldSelection, CurrentSelection);
                            }
                            else
                            {
                                RecordSelection(oldSelection, CurrentSelection);
                            }
                        }
                        else
                        {
                            RecordSelection(oldSelection, CurrentSelection);
                        }
                    }

                    if (meshEditor.HasSelection)
                    {
                        TryUpdatePivotTransform();
                        TrySelectPivot(meshEditor);
                        TryUpdatePivotVisibility();
                    }
                    else
                    {
                        if (m_rte.Selection.activeGameObject == m_pivot.gameObject)
                        {
                            m_rte.Selection.activeGameObject = null;
                        }
                    }

                    m_rte.Undo.EndRecord();

                }
                else if (m_rte.Input.GetKeyDown(KeyCode.Delete))
                {
                    Delete();
                }
                else if (m_rte.Input.GetKeyDown(KeyCode.H))
                {
                    FillHoles();
                }
            }
        }


        private void InitCullingMask()
        {
            if(m_rte != null)
            {
                foreach (RuntimeWindow window in m_rte.Windows)
                {
                    if (window.WindowType == RuntimeWindowType.Scene)
                    {
                        SetCullingMask(window);
                    }
                }
            }
            
        }

        private void SetCullingMask(RuntimeWindow window)
        {
            //window.Camera.cullingMask |= 1 << m_rte.CameraLayerSettings.AllScenesLayer;
        }

        private void ResetCullingMask(RuntimeWindow window)
        {
            //window.Camera.cullingMask &= ~(1 << m_rte.CameraLayerSettings.AllScenesLayer);
        }

        private void OnEditorSelectionChanged(UnityEngine.Object[] unselectedObjects)
        {
            IMeshEditor editor = GetEditor();
            if (editor != null)
            {
                if (m_rte.Selection.IsSelected(m_pivot.gameObject))
                {
                    editor.SetSelection(CurrentSelection);
                }
                else
                {
                    editor.ClearSelection();
                }

                OnSelectionChanged();
            }
        }

        private void OnEditorToolChanged()
        {
            RuntimeTool current = m_rte.Tools.Current;
            if (current != RuntimeTool.Move && current != RuntimeTool.Rotate && current != RuntimeTool.Scale)
            {
                if(Mode != ProBuilderToolMode.Object)
                {
                    Mode = ProBuilderToolMode.Object;
                }
            }
        }

        private void OnCurrentModeChanged(ProBuilderToolMode oldMode)
        {
            if (m_mode != ProBuilderToolMode.Object)
            {
                RuntimeTool current = m_rte.Tools.Current;
                m_rte.Tools.Current = RuntimeTool.None; //This is required to notifiy other tools
                if (current != RuntimeTool.Move && current != RuntimeTool.Rotate && current != RuntimeTool.Scale)
                {
                    m_rte.Tools.Current = RuntimeTool.Move;
                }
                else
                {
                    m_rte.Tools.Current = current;
                }
            }

            IMeshEditor disabledEditor = m_meshEditors[(int)oldMode];
            IMeshEditor enabledEditor = m_meshEditors[(int)m_mode];

            if (disabledEditor != null)
            {
                disabledEditor.ClearSelection();
            }

            if (enabledEditor != null)
            {
                if (CurrentSelection != null)
                {
                    enabledEditor.SetSelection(CurrentSelection);

                    TryUpdatePivotTransform();
                    TrySelectPivot(enabledEditor, false);
                    OnSelectionChanged();
                }
            }

            if (Mode == ProBuilderToolMode.Object)
            {
                SetCanSelect(true);
            }
            else
            {
                SetCanSelect(false);
            }

            TryUpdatePivotVisibility();
        }


        private void SetCanSelect(bool value)
        {
            if(m_wm != null)
            {
                Transform[] windows = m_wm.GetWindows(RuntimeWindowType.Scene.ToString());
                for (int i = 0; i < windows.Length; ++i)
                {
                    RuntimeWindow window = windows[i].GetComponent<RuntimeWindow>();
                    ISelectionComponentState selectionComponent = window.IOCContainer.Resolve<ISelectionComponentState>();
                    if (selectionComponent != null)
                    {
                        selectionComponent.CanSelect(this, value);
                        selectionComponent.CanSelectAll(this, value);
                    }
                }
            }
            else
            {
                RuntimeWindow sceneWindow = m_rte.GetWindow(RuntimeWindowType.Scene);
                if(sceneWindow != null)
                {
                    IRuntimeSelectionComponent selectionComponent = sceneWindow.IOCContainer.Resolve<IRuntimeSelectionComponent>();
                    if (selectionComponent != null)
                    {
                        selectionComponent.CanSelect = value;
                        selectionComponent.CanSelectAll = value;
                    }
                }  
            }
        }

        private void OnPivotModeChanging()
        {
            m_rte.Undo.BeginRecordValue(m_rte.Tools, Strong.PropertyInfo((RuntimeTools x) => x.PivotMode));
        }

        private void OnPivotModeChanged()
        {
            m_rte.Undo.EndRecordValue(m_rte.Tools, Strong.PropertyInfo((RuntimeTools x) => x.PivotMode));
            UpdateCenterMode();
        }

        private void OnPivotRotationChanging()
        {
            m_rte.Undo.BeginRecordValue(m_rte.Tools, Strong.PropertyInfo((RuntimeTools x) => x.PivotRotation));
        }

        private void OnPivotRotationChanged()
        {
            m_rte.Undo.EndRecordValue(m_rte.Tools, Strong.PropertyInfo((RuntimeTools x) => x.PivotRotation));
            TryUpdatePivotTransform();
        }

        private void TryUpdatePivotVisibility()
        {
            ExposeToEditor exposeToEditor = m_pivot.GetComponent<ExposeToEditor>();
            if (Mode == ProBuilderToolMode.Object)
            {
                exposeToEditor.CanTransform = false;
            }
            else
            {
                IMeshEditor meshEditor = GetEditor();
                exposeToEditor.CanTransform = meshEditor == null || meshEditor.HasSelection;
                if(HasSelectedManualUVs && HasSelectedAutoUVs)
                {
                    exposeToEditor.CanTransform = false;
                }
            }

            if (m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                bool wasEnabled = m_rte.Undo.Enabled;
                m_rte.Undo.Enabled = false;
                m_rte.Selection.SelectionChanged -= OnEditorSelectionChanged;
                m_rte.Selection.Select(null, null);
                m_rte.Selection.Select(m_pivot.gameObject, new[] { m_pivot.gameObject });
                m_rte.Selection.SelectionChanged += OnEditorSelectionChanged;
                m_rte.Undo.Enabled = true;
            }
        }

        private void TrySelectPivot(IMeshEditor meshEditor, bool record = true)
        {
            bool wasEnabled = m_rte.Undo.Enabled;
            m_rte.Undo.Enabled = record;

            if (meshEditor != null && meshEditor.HasSelection)
            {
                m_rte.Selection.activeObject = m_pivot.gameObject;
            }
     
            m_rte.Undo.Enabled = wasEnabled;
        }


        public void TryUpdatePivotTransform()
        {
            UpdateCenterMode();
            UpdateGlobalMode();
        }

        private void UpdateCenterMode()
        {
            if(m_mode == ProBuilderToolMode.PolyShape)
            {
                return;
            }

            foreach(IMeshEditor editor in m_meshEditors)
            {
                if(editor == null)
                {
                    continue;
                }
                editor.CenterMode = m_rte.Tools.PivotMode == RuntimePivotMode.Center;
            }

            IMeshEditor meshEditor = m_meshEditors[(int)m_mode];
            if (meshEditor != null)
            {
                PivotPosition = meshEditor.Position;
                PivotRotation = GetPivotRotation(meshEditor);
            }
        }

        private void UpdateGlobalMode()
        {
            if (m_mode == ProBuilderToolMode.PolyShape)
            {
                return;
            }

            foreach (IMeshEditor editor in m_meshEditors)
            {
                if (editor == null)
                {
                    continue;
                }

                if(UVEditingMode)
                {
                    editor.GlobalMode = false;
                }
                else
                {
                    editor.GlobalMode = m_rte.Tools.PivotRotation == RuntimePivotRotation.Global;
                }
            }

            IMeshEditor currentEditor = GetEditor();
            if(currentEditor != null)
            {
                PivotRotation = GetPivotRotation(currentEditor);
            }
        }

        private Quaternion GetPivotRotation(IMeshEditor meshEditor)
        {
            if(UVEditingMode)
            {
                return meshEditor.Rotation;
            }

            return m_rte.Tools.PivotRotation == RuntimePivotRotation.Global ? Quaternion.identity : meshEditor.Rotation;
        }

        private void OnWindowCreated(Transform windowTransform)
        {
            if (m_mode == ProBuilderToolMode.Object)
            {
                return;
            }

            RuntimeWindow window = windowTransform.GetComponentInChildren<RuntimeWindow>(true);
            if (window.WindowType != RuntimeWindowType.Scene)
            {
                return;
            }

            SetCullingMask(window);

            ISelectionComponentState selectionComponent = window.IOCContainer.Resolve<ISelectionComponentState>();
            if (selectionComponent != null)
            {
                selectionComponent.CanSelect(this, false);
                selectionComponent.CanSelectAll(this, false);
            }
        }

        private void OnActiveWindowChanged(RuntimeWindow window)
        {
            m_hasAutoUVs = false;
            m_hasManualUVs = false;

            UnsubscribeFromEvents();

            if (m_rte.ActiveWindow != null && m_rte.ActiveWindow.WindowType == RuntimeWindowType.Scene)
            {
                m_selectionComponent = m_rte.ActiveWindow.IOCContainer.Resolve<IRuntimeSelectionComponent>();
                m_boxSelection = m_rte.ActiveWindow.IOCContainer.Resolve<IBoxSelection>();
            }
            else
            {
                m_selectionComponent = null;
                m_boxSelection = null;
            }

            SubscribeToEvents();
        }

        private void OnAfterLayout(IWindowManager obj)
        {
            InitCullingMask();
        }

        private void OnSceneLoading()
        {
            m_faceGroupSelection = null;
            m_currentSelection = null;

            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null)
            {
                meshEditor.ClearSelection();
            }
            Mode = ProBuilderToolMode.Object;
        }

        private void SubscribeToEvents()
        {
            if (m_boxSelection != null)
            {
                m_boxSelection.Begin += OnBeginBoxSelection;
                m_boxSelection.Selection += OnBoxSelection;
            }

            if (m_selectionComponent != null)
            {
                if (m_selectionComponent.PositionHandle != null)
                {
                    m_selectionComponent.PositionHandle.BeforeDrag.AddListener(OnBeginMove);
                    m_selectionComponent.PositionHandle.Drop.AddListener(OnEndMove);
                }

                if (m_selectionComponent.RotationHandle != null)
                {
                    m_selectionComponent.RotationHandle.BeforeDrag.AddListener(OnBeginRotate);
                    m_selectionComponent.RotationHandle.Drop.AddListener(OnEndRotate);
                }

                if (m_selectionComponent.ScaleHandle != null)
                {
                    m_selectionComponent.ScaleHandle.BeforeDrag.AddListener(OnBeginScale);
                    m_selectionComponent.ScaleHandle.Drop.AddListener(OnEndScale);
                }
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (m_boxSelection != null)
            {
                m_boxSelection.Begin -= OnBeginBoxSelection;
                m_boxSelection.Selection -= OnBoxSelection;
            }

            if (m_selectionComponent != null)
            {
                if (m_selectionComponent.PositionHandle != null)
                {
                    m_selectionComponent.PositionHandle.BeforeDrag.RemoveListener(OnBeginMove);
                    m_selectionComponent.PositionHandle.Drop.RemoveListener(OnEndMove);
                    if(m_selectionComponent.PositionHandle.IsDragging)
                    {
                        OnEndMove(m_selectionComponent.PositionHandle);
                    }
                }

                if (m_selectionComponent.RotationHandle != null)
                {
                    m_selectionComponent.RotationHandle.BeforeDrag.RemoveListener(OnBeginRotate);
                    m_selectionComponent.RotationHandle.Drop.RemoveListener(OnEndRotate);
                    if (m_selectionComponent.RotationHandle.IsDragging)
                    {
                        OnEndRotate(m_selectionComponent.RotationHandle);
                    }
                }

                if (m_selectionComponent.ScaleHandle != null)
                {
                    m_selectionComponent.ScaleHandle.BeforeDrag.RemoveListener(OnBeginScale);
                    m_selectionComponent.ScaleHandle.Drop.RemoveListener(OnEndScale);
                    if (m_selectionComponent.ScaleHandle.IsDragging)
                    {
                        OnEndScale(m_selectionComponent.ScaleHandle);
                    }
                }
            }
        }

        private void OnBeginBoxSelection(object sender, BeginBoxSelectionArgs e)
        {
            if(Mode != ProBuilderToolMode.Object)
            {
                RuntimeWindow window = m_rte.ActiveWindow;
                m_boxSelection.MethodOverride = BoxSelectionMethod.PixelPerfectDepthTest;
            }
        }

        private void OnBoxSelection(object sender, BoxSelectionArgs e)
        {
            m_boxSelection.MethodOverride = BoxSelectionMethod.Default;
            IMeshEditor meshEditor = m_meshEditors[(int)m_mode];
            if (meshEditor == null)
            {
                return;
            }

            RuntimeWindow window = m_rte.ActiveWindow;
            bool depthTest = IsDepthTestEnabled(window);

            Vector2 min = m_boxSelection.SelectionBounds.min;
            Vector2 max = m_boxSelection.SelectionBounds.max;

            Canvas canvas = window.GetComponentInParent<Canvas>();

            Rect rect;
            if(depthTest || Mode != ProBuilderToolMode.Face)
            {
                RectTransform sceneOutput = window.GetComponent<RectTransform>();
                if(sceneOutput.childCount > 0 )
                {
                    sceneOutput = (RectTransform)sceneOutput.GetChild(0);
                }
                

                Camera canvasCamera = canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(sceneOutput, min, canvasCamera, out min);
                min.y = sceneOutput.rect.height - min.y;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(sceneOutput, max, canvasCamera, out max);
                max.y = sceneOutput.rect.height - max.y;

                rect = new Rect(new Vector2(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y)), new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y)));
                rect.x += window.Camera.pixelRect.x;
                rect.y += canvas.pixelRect.height - (window.Camera.pixelRect.y + window.Camera.pixelRect.height);
            }
            else
            {
                RectTransform rt = window.GetComponent<RectTransform>();
                rect = new Rect(new Vector2(min.x, rt.rect.height - max.y), m_boxSelection.SelectionBounds.size);
            }

            m_rte.Undo.BeginRecord();

            MeshSelection oldSelection = CurrentSelection;
            if (meshEditor.Select(window.Camera, rect, canvas.pixelRect, e.GameObjects.Where(g => g.GetComponent<ExposeToEditor>() != null).ToArray(), depthTest, MeshEditorSelectionMode.Add) != null) 
            {
                CurrentSelection = meshEditor.GetSelection();
                RecordSelection(oldSelection, CurrentSelection);
            }

            TryUpdatePivotTransform();
            TrySelectPivot(meshEditor);
            TryUpdatePivotVisibility();

            m_rte.Undo.EndRecord();
        }

        private void OnBeginMove(BaseHandle positionHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if(meshEditor != null && CurrentSelection != null && m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                positionHandle.EnableUndo = false;

                if (UVEditingMode)
                {
                    m_hasAutoUVs = HasSelectedAutoUVs;
                    m_hasManualUVs = HasSelectedManualUVs;
                    if(m_hasAutoUVs && m_hasManualUVs)
                    {
                        m_hasAutoUVs = false;
                        m_hasManualUVs = false;
                    }

                    if(m_hasAutoUVs)
                    {
                        m_initialUVOffset = UV.offset;
                        m_rte.Undo.BeginRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.offset));
                    }
                    else if(m_hasManualUVs)
                    {
                        MeshEditorState oldState = meshEditor.GetState(true);
                        RecordState(oldState, null);

                        MeshSelection selection = CurrentSelection;
                        m_textureMoveTool.BeginDrag(selection, PivotPosition, PivotRotation);
                    }
                }
                else
                {
                    MeshEditorState oldState = meshEditor.GetState(false);
                    MeshSelection oldSelection = CurrentSelection;
                    bool control = m_rte.Input.GetKey(KeyCode.LeftControl);
                    if (control)
                    {
                        meshEditor.Extrude(0.01f);
                    }

                    RecordStateAndSelection(oldState, null, oldSelection, null);
                }
            
                meshEditor.BeginMove();
            }
        }

        private void OnEndMove(BaseHandle positionHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && CurrentSelection != null && m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                positionHandle.EnableUndo = true;

                if (UVEditingMode)
                {
                    if(m_hasAutoUVs)
                    {
                        m_rte.Undo.EndRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.offset));
                        TryUpdatePivotTransform();
                    }
                    else if(m_hasManualUVs)
                    {
                        MeshEditorState newState = meshEditor.GetState(true);
                        RecordState(null, newState);

                        m_textureMoveTool.EndDrag();
                    }
                    
                    PivotPosition = meshEditor.Position;
                }
                else
                {
                    MeshEditorState newState = meshEditor.GetState(false);
                    MeshSelection newSelection = meshEditor.GetSelection();
                    CurrentSelection = newSelection;
                    RecordStateAndSelection(null, newState, null, newSelection);
                }
                meshEditor.EndMove();
            }
        }

        
        private void OnBeginRotate(BaseHandle rotationHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if(meshEditor != null && m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                rotationHandle.EnableUndo = false;

                m_initialRotation = GetPivotRotation(meshEditor);
                PivotRotation = m_initialRotation;
                
                if(UVEditingMode)
                {
                    m_hasAutoUVs = HasSelectedAutoUVs;
                    m_hasManualUVs = HasSelectedManualUVs;
                    if (m_hasAutoUVs && m_hasManualUVs)
                    {
                        m_hasAutoUVs = false;
                        m_hasManualUVs = false;
                    }

                    if(m_hasAutoUVs)
                    {
                        m_initialRight = m_pivot.TransformDirection(Vector3.right);
                        m_initialUVRotation = UV.rotation;
                        m_rte.Undo.BeginRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.rotation));
                    }
                    else if(m_hasManualUVs)
                    {
                        MeshEditorState oldState = meshEditor.GetState(true);
                        RecordState(oldState, null);
                        m_textureRotateTool.BeginDrag(meshEditor.GetSelection(), PivotPosition, PivotRotation);
                    }
                }
                else
                {
                    MeshEditorState oldState = meshEditor.GetState(false);
                    RecordState(oldState, null);
                }
                meshEditor.BeginRotate(m_initialRotation);
                
            }
        }

        private void OnEndRotate(BaseHandle rotationHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if(meshEditor != null && m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                rotationHandle.EnableUndo = true;

                Quaternion initialRotation = m_initialRotation;
                Quaternion endRotation = PivotRotation;
                meshEditor.EndRotate();

                Quaternion newStartRotation = GetPivotRotation(meshEditor);
                PivotRotation = newStartRotation;

                if (UVEditingMode)
                {
                    if(m_hasAutoUVs)
                    {
                        m_rte.Undo.EndRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.rotation));
                        TryUpdatePivotTransform();
                    }
                    else if(m_hasManualUVs)
                    {
                        MeshEditorState newState = meshEditor.GetState(true);
                        RecordState(null, newState);
                        m_textureRotateTool.EndDrag(true);
                    }

                    TryUpdatePivotTransform();
                }
                else
                {
                    MeshEditorState newState = meshEditor.GetState(false);
                    RecordState(null, newState);
                }  
            }
        }

        private void OnBeginScale(BaseHandle scaleHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                scaleHandle.EnableUndo = false;
                PivotLocalScale = Vector3.one;
                if (UVEditingMode)
                {
                    m_hasAutoUVs = HasSelectedAutoUVs;
                    m_hasManualUVs = HasSelectedManualUVs;
                    if (m_hasAutoUVs && m_hasManualUVs)
                    {
                        m_hasAutoUVs = false;
                        m_hasManualUVs = false;
                    }

                    if(m_hasAutoUVs)
                    {
                        m_initialUVScale = UV.scale;
                        m_rte.Undo.BeginRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.scale));
                    }
                    else if(m_hasManualUVs)
                    {
                        MeshEditorState oldState = meshEditor.GetState(true);
                        RecordState(oldState, null);
                        m_textureScaleTool.BeginDrag(meshEditor.GetSelection(), PivotPosition, PivotRotation);
                    }
                }
                else
                {
                    MeshEditorState oldState = meshEditor.GetState(false);
                    RecordState(oldState, null);
                }
                meshEditor.BeginScale();
            }
        }

        private void OnEndScale(BaseHandle scaleHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                scaleHandle.EnableUndo = true;
                meshEditor.EndScale();

                Vector3 newScale = PivotLocalScale;
                Quaternion rotation = PivotRotation;
                PivotLocalScale = Vector3.one;

                if (UVEditingMode)
                {
                    if (m_hasAutoUVs)
                    {
                        m_rte.Undo.EndRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.scale));
                        TryUpdatePivotTransform();
                    }
                    else if(m_hasManualUVs)
                    {
                        MeshEditorState newState = meshEditor.GetState(true);
                        RecordState(null, newState);
                        m_textureScaleTool.EndDrag();
                    }                    
                }
                else
                {
                    MeshEditorState newState = meshEditor.GetState(false);
                    RecordState(null, newState);
                } 
            }
        }

        public void ApplyMaterial(Material material, Camera camera, Vector3 mousePosition)
        {
            IMeshEditor editor = GetEditor();
            if (editor != null)
            {
                MeshSelection selection = editor.GetSelection();
                ApplyMaterialResult result = m_materialEditor.ApplyMaterial(material, selection, camera, mousePosition);
                RecordApplyMaterialResult(result);
            }
            else
            {
                GameObject gameObject = PBUtility.PickObject(camera, mousePosition);
                if(gameObject != null)
                {
                    Renderer renderer;
                    int materialIndex = RaycastHelper.Raycast(camera.ScreenPointToRay(mousePosition), out renderer);
                    if (m_rte.Selection.IsSelected(gameObject))
                    {
                        ApplyMaterialToSelectedGameObjects(material, materialIndex);
                    }
                    else
                    {
                        ApplyMaterialResult result = m_materialEditor.ApplyMaterial(material, gameObject, materialIndex);
                        RecordApplyMaterialResult(result);
                    }
                }
                
            }
        }

        public void ApplyMaterial(Material material)
        {
            ApplyMaterial(material, -1);
        }

        public void ApplyMaterial(Material material, int submeshIndex)
        {
            IMeshEditor editor = GetEditor();
            if(editor != null)
            {
                MeshSelection selection = editor.GetSelection();
              
                ApplyMaterialResult result = m_materialEditor.ApplyMaterial(material, selection);
                RecordApplyMaterialResult(result);
            }
            else
            {
                ApplyMaterialToSelectedGameObjects(material, submeshIndex);
            }
        }

        private void ApplyMaterialToSelectedGameObjects(Material material, int submeshIndex)
        {
            m_rte.Undo.BeginRecord();

            GameObject[] gameObjects = m_rte.Selection.gameObjects;
            if (gameObjects != null)
            {
                
                for (int i = 0; i < gameObjects.Length; ++i)
                {
                    if (gameObjects[i] == null)
                    {
                        continue;
                    }
                    
                    ApplyMaterialResult result = m_materialEditor.ApplyMaterial(material, gameObjects[i], submeshIndex);
                    RecordApplyMaterialResult(result);
                }
            }

            m_rte.Undo.EndRecord();
        }

        public void SelectFaces(Material material)
        {
            IMeshEditor meshEditor = GetEditor();
            if(meshEditor != null && m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                m_rte.Undo.BeginRecord();

                MeshSelection oldSelection = CurrentSelection;
                if (meshEditor.Select(material) != null)
                {
                    CurrentSelection = meshEditor.GetSelection();
                    RecordSelection(oldSelection, CurrentSelection);
                }

                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor);
                TryUpdatePivotVisibility();

                m_rte.Undo.EndRecord();
            }
        }

        public void UnselectFaces(Material material)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                m_rte.Undo.BeginRecord();

                MeshSelection oldSelection = CurrentSelection;
                if (meshEditor.Unselect(material) != null)
                {
                    CurrentSelection = meshEditor.GetSelection();
                    RecordSelection(oldSelection, CurrentSelection);
                }

                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor);
                TryUpdatePivotVisibility();

                m_rte.Undo.EndRecord();
            }
        }

        public void CenterPivot()
        {
            RunPBMeshAction(pbMesh => pbMesh.CenterPivot(), true);
        }

        public void Extrude(float distance)
        {
            IMeshEditor meshEditor = GetEditor();
            MeshSelection oldSelection = CurrentSelection;
            MeshEditorState oldState = meshEditor.GetState(false);
            meshEditor.Extrude(distance);
            MeshSelection newSelection = meshEditor.GetSelection();
            MeshEditorState newState = meshEditor.GetState(false);
            TryUpdatePivotTransform();
            CurrentSelection = newSelection;
            RecordStateAndSelection(oldState, newState, oldSelection, newSelection);
        }

        public void Subdivide()
        {
            RunPBMeshAction(pbMesh => pbMesh.Subdivide(), false);
        }

        private void RunPBMeshAction(Action<PBMesh> action, bool recordPosition)
        {
            if (m_rte.Selection.activeGameObject != null)
            {
                m_rte.Undo.BeginRecord();
                foreach (GameObject go in m_rte.Selection.gameObjects)
                {
                    PBMesh pbMesh = go.GetComponent<PBMesh>();
                    if (pbMesh == null)
                    {
                        continue;
                    }

                    Vector3 oldPosition = pbMesh.transform.position;
                    MeshState oldState = pbMesh.GetState(true);
                    if (pbMesh != null)
                    {
                        action(pbMesh);
                    }

                    Vector3 newPosition = pbMesh.transform.position;
                    MeshState newState = pbMesh.GetState(true);
                    m_rte.Undo.CreateRecord(record =>
                    {
                        pbMesh.transform.position = newPosition;
                        pbMesh.SetState(newState);
                        return true;
                    },
                    record =>
                    {
                        pbMesh.transform.position = oldPosition;
                        pbMesh.SetState(oldState);
                        return true;
                    });
                }
                m_rte.Undo.EndRecord();
            }
        }

        public void Delete()
        {
            RunStateChangeAction(meshEditor => meshEditor.Delete(), true);
        }

        public void SubdivideFaces()
        {
            RunStateChangeAction(meshEditor => meshEditor.Subdivide(), true);
        }

        public void MergeFaces()
        {
            RunStateChangeAction(meshEditor => meshEditor.Merge(), true);
        }

        public void SubdivideEdges()
        {
            RunStateChangeAction(meshEditor => meshEditor.Subdivide(), true);
        }

        public void SelectHoles()
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                MeshSelection oldSelection = CurrentSelection;
                if(meshEditor.SelectHoles() != null)
                {
                    CurrentSelection = meshEditor.GetSelection();
                    RecordSelection(oldSelection, CurrentSelection);
                }
            }
        }

        public void FillHoles()
        {
            RunStateChangeAction(meshEditor => meshEditor.FillHoles(), false);
        }

        public void GroupFaces()
        {
            RunUVEditingAction(selection => m_autoUVEditor.GroupFaces(selection));
        }

        public void UngroupFaces()
        {
            RunUVEditingAction(selection => { m_autoUVEditor.UngroupFaces(selection); });
        }

        public void SelectFaceGroup()
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor == null)
            {
                return;
            }

            MeshSelection oldSelection = CurrentSelection;
            MeshSelection faceGroupSelection = m_autoUVEditor.SelectFaceGroup(oldSelection);
            if(faceGroupSelection != null)
            {
                meshEditor.SetSelection(faceGroupSelection);

                m_rte.Undo.BeginRecord();
                CurrentSelection = meshEditor.GetSelection();
                RecordSelection(oldSelection, CurrentSelection);

                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor);
                TryUpdatePivotVisibility();

                m_rte.Undo.EndRecord();
            }
        }

        public void ConvertUVs(bool auto)
        {
            RunUVEditingAction(selection => { m_autoUVEditor.SetAutoUV(selection, auto); });
        }

        public void ResetUVs()
        {
            RunUVEditingAction(selection => { m_autoUVEditor.ResetUV(selection); });
        }
            
        private void OnSelectionChanged()
        {
            if(SelectionChanging != null)
            {
                SelectionChanging();
            }

            IMeshEditor editor = GetEditor();
            if (editor != null)
            {
                MeshSelection selection = editor.GetSelection();
                PBAutoUnwrapSettings settings = m_autoUVEditor.GetSettings(selection);
                m_uv.CopyFrom(settings);
            }

            if (SelectionChanged != null)
            {
                SelectionChanged();
            }
        }

        private void OnUVChanged()
        {            
            if(m_faceGroupSelection != null)
            {
                m_autoUVEditor.ApplySettings(m_uv, m_faceGroupSelection);
            }

            if(m_selectionComponent == null ||
               (m_selectionComponent.PositionHandle == null || !m_selectionComponent.PositionHandle.IsDragging) &&
               (m_selectionComponent.RotationHandle == null || !m_selectionComponent.RotationHandle.IsDragging) &&
               (m_selectionComponent.ScaleHandle == null || !m_selectionComponent.ScaleHandle.IsDragging))
            {
                TryUpdatePivotTransform();
            }
        }

        private void RunUVEditingAction(Action<MeshSelection> action)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor == null || m_rte.Selection.activeGameObject != m_pivot.gameObject)
            {
                return;
            }

            m_rte.Undo.BeginRecord();
            m_rte.Undo.BeginRecordTransform(m_pivot);
            m_rte.Undo.RecordValue(meshEditor, Strong.PropertyInfo((IMeshEditor x) => x.Position));
            MeshEditorState oldState = meshEditor.GetState(true);
            MeshSelection selection = meshEditor.GetSelection();
            action(selection);
            MeshEditorState newState = meshEditor.GetState(true);
            RecordState(oldState, newState);
            TryUpdatePivotTransform();
            TrySelectPivot(meshEditor);
            TryUpdatePivotVisibility();
            m_rte.Undo.EndRecord();
        }

        private void RunStateChangeAction(Action<IMeshEditor> action, bool clearSelection)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && m_rte.Selection.activeGameObject == m_pivot.gameObject)
            {
                m_rte.Undo.BeginRecord();
                m_rte.Undo.BeginRecordTransform(m_pivot);
                m_rte.Undo.RecordValue(meshEditor, Strong.PropertyInfo((IMeshEditor x) => x.Position));

                MeshEditorState oldState = meshEditor.GetState(false);
                action(meshEditor);
                MeshEditorState newState = meshEditor.GetState(false);
                MeshSelection oldSelection = null;
                if (clearSelection)
                {
                    oldSelection = CurrentSelection;
                    meshEditor.ClearSelection();
                    CurrentSelection = meshEditor.GetSelection();
                    RecordStateAndSelection(oldState, newState, oldSelection, CurrentSelection);
                }
                else
                {
                    RecordState(oldState, newState, true);
                }

                TrySelectPivot(meshEditor);
                TryUpdatePivotVisibility();
                m_rte.Undo.EndRecord();
            }
        }


        public void RecordStateAndSelection(
           MeshEditorState oldState, MeshEditorState newState,
           MeshSelection oldSelection, MeshSelection newSelection,
           bool oldStateChanged = true,
           bool newStateChanged = true)
        {
            UndoRedoCallback redo = record =>
            {
                if (newState != null)
                {
                    CurrentSelection = newSelection;

                    IMeshEditor meshEditor = GetEditor();
                    if (meshEditor != null)
                    {
                        meshEditor.ClearSelection();
                        meshEditor.SetState(newState);
                        foreach (PBMesh mesh in newState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }

                        meshEditor.SetSelection(CurrentSelection);
                    }
                    else
                    {
                        newState.Apply();
                        foreach (PBMesh mesh in newState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }
                    }

                    TryUpdatePivotTransform();
                    TrySelectPivot(meshEditor, false);
                    TryUpdatePivotVisibility();
                    OnSelectionChanged();
                    return newStateChanged;
                }
                return false;
            };

            UndoRedoCallback undo = record =>
            {
                if (oldState != null)
                {
                    CurrentSelection = oldSelection;

                    IMeshEditor meshEditor = GetEditor();
                    if (meshEditor != null)
                    {
                        meshEditor.ClearSelection();
                        meshEditor.SetState(oldState);
                        foreach (PBMesh mesh in oldState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }

                        meshEditor.SetSelection(CurrentSelection);
                    }
                    else
                    {
                        oldState.Apply();
                        foreach (PBMesh mesh in oldState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }
                    }

                    TryUpdatePivotTransform();
                    TrySelectPivot(meshEditor, false);
                    TryUpdatePivotVisibility();
                    OnSelectionChanged();
                    return oldStateChanged;
                }
                return false;
            };

            m_rte.Undo.CreateRecord(redo, undo);
            OnSelectionChanged();
        }

        public void RecordState(MeshEditorState oldState, MeshEditorState newState, bool raiseMeshChanged = false)
        {
            UndoRedoCallback redo = record =>
            {
                if (newState != null)
                {
                    IMeshEditor meshEditor = GetEditor();
                    if(meshEditor != null)
                    {
                        meshEditor.SetState(newState);
                    }
                    else
                    {
                        newState.Apply();
                    }

                    if(raiseMeshChanged)
                    {
                        foreach (PBMesh mesh in newState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }
                    }
                    
                    TryUpdatePivotTransform();
                    TrySelectPivot(meshEditor, false);
                    TryUpdatePivotVisibility();
                    OnSelectionChanged();
                    return true;
                }
                return false;
            };

            UndoRedoCallback undo = record =>
            {
                if (oldState != null)
                {
                    IMeshEditor meshEditor = GetEditor();
                    if (meshEditor != null)
                    {
                        meshEditor.SetState(oldState);
                    }
                    else
                    {
                        oldState.Apply();
                    }

                    if(raiseMeshChanged)
                    {
                        foreach (PBMesh mesh in newState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }
                    }
                    
                    TryUpdatePivotTransform();
                    TrySelectPivot(meshEditor, false);
                    TryUpdatePivotVisibility();
                    OnSelectionChanged();
                    return true;
                }
                return false;
            };

            m_rte.Undo.CreateRecord(redo, undo);
            OnSelectionChanged();
        }

        private void RecordSelection(MeshSelection oldSelection, MeshSelection newSelection, bool oldStateChanged = true, bool newStateChanged = true)
        {
            UndoRedoCallback redo = record =>
            {
                CurrentSelection = newSelection;

                IMeshEditor meshEditor = GetEditor();
                if(meshEditor != null)
                {
                    meshEditor.SetSelection(CurrentSelection);
                }

                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor, false);
                TryUpdatePivotVisibility();
                OnSelectionChanged();
                return newStateChanged;
            };

            UndoRedoCallback undo = record =>
            {
                CurrentSelection = oldSelection;

                IMeshEditor meshEditor = GetEditor();
                if (meshEditor != null)
                {
                    meshEditor.SetSelection(CurrentSelection);
                }
                
                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor, false);
                TryUpdatePivotVisibility();
                OnSelectionChanged();
                return oldStateChanged;
            };

            m_rte.Undo.CreateRecord(redo, undo);
            OnSelectionChanged();
        }

        private void RecordApplyMaterialResult(ApplyMaterialResult result)
        {
            m_rte.Undo.CreateRecord(record =>
            {
                m_materialEditor.ApplyMaterials(result.NewState);
                return true;
            },
            record =>
            {
                m_materialEditor.ApplyMaterials(result.OldState);
                return true;
            },
            record => { },
            (record, oldReference, newReference) =>
            {
                result.OldState.Erase(oldReference, newReference);
                result.NewState.Erase(oldReference, newReference);
                return false;
            });
        }
    }
}