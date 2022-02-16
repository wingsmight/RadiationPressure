using UnityEngine;

using Battlehub.ProBuilderIntegration;
using Battlehub.RTCommon;
using Battlehub.RTHandles;
using System.Linq;

namespace Battlehub.RTBuilder
{
    public interface IPolyShapeEditor { }

    [DefaultExecutionOrder(-89)]
    public class ProBuilderPolyShapeEditor : MonoBehaviour, IPolyShapeEditor
    {
        private ExposeToEditor m_exposeToEditor;
        private PBPolyShape m_polyShape;
        private PBPolyShape PolyShape
        {
            get { return m_polyShape; }
            set
            {
                m_polyShape = value;
                if(m_polyShape != null)
                {
                    m_exposeToEditor = m_polyShape.GetComponent<ExposeToEditor>();
                }
                else
                {
                    m_exposeToEditor = null;
                }
            }
        }

        private bool m_endEditOnPointerUp;
        private Transform m_pivot;

        private IRTE m_rte;
        private IProBuilderTool m_tool;
        private IRuntimeSelectionComponent m_selectionComponent;

        private void Awake()
        {
            m_pivot = new GameObject("PolyShapePivot").transform;
            LockAxes axes = m_pivot.gameObject.AddComponent<LockAxes>();
            axes.PositionY = true;
            axes.RotationFree = axes.RotationScreen = axes.RotationX = axes.RotationY = axes.RotationZ = true;
            axes.ScaleX = axes.ScaleY = axes.ScaleZ = true;
            m_pivot.transform.SetParent(transform);
        }

        private void Start()
        {
            m_rte = IOC.Resolve<IRTE>();
            m_tool = IOC.Resolve<IProBuilderTool>();
            m_tool.ModeChanged += OnModeChanged;

            if (m_rte != null)
            {
                m_rte.ActiveWindowChanged += OnActiveWindowChanged;

                if (m_rte.ActiveWindow != null && m_rte.ActiveWindow.WindowType == RuntimeWindowType.Scene)
                {
                    m_selectionComponent = m_rte.ActiveWindow.IOCContainer.Resolve<IRuntimeSelectionComponent>();
                    SubscribeToEvents();
                }
            }
        }

        private void OnDestroy()
        {
            if (m_tool != null)
            {
                m_tool.ModeChanged -= OnModeChanged;
            }

            if (m_rte != null)
            {
                m_rte.ActiveWindowChanged -= OnActiveWindowChanged;
            }

            UnsubscribeFromEvents();
        }

        private void OnActiveWindowChanged(RuntimeWindow window)
        {
            UnsubscribeFromEvents();

            if (m_rte.ActiveWindow != null && m_rte.ActiveWindow.WindowType == RuntimeWindowType.Scene)
            {
                m_selectionComponent = m_rte.ActiveWindow.IOCContainer.Resolve<IRuntimeSelectionComponent>();
            }
            else
            {
                m_selectionComponent = null;
            }

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
           
            if (m_selectionComponent != null)
            {
                if (m_selectionComponent.PositionHandle != null)
                {
                    m_selectionComponent.PositionHandle.BeforeDrag.AddListener(OnBeginMove);
                    m_selectionComponent.PositionHandle.Drop.AddListener(OnEndMove);
                }
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (m_selectionComponent != null)
            {
                if (m_selectionComponent.PositionHandle != null)
                {
                    m_selectionComponent.PositionHandle.BeforeDrag.RemoveListener(OnBeginMove);
                    m_selectionComponent.PositionHandle.Drop.RemoveListener(OnEndMove);
                }
            }
        }

        private void OnModeChanged(ProBuilderToolMode oldMode)
        {
            if(m_tool.Mode == ProBuilderToolMode.PolyShape)
            {
                PolyShape = m_rte.Selection.activeGameObject.GetComponent<PBPolyShape>();
                PolyShapeUpdatePivot(PolyShape);
                PolyShape.IsEditing = true;
                SetLayer(PolyShape.gameObject);
            }
            else if(oldMode == ProBuilderToolMode.PolyShape)
            {
                if(PolyShape != null)
                {
                    PolyShape.IsEditing = false;
                }
                
                if (m_exposeToEditor != null && !m_exposeToEditor.MarkAsDestroyed)
                {
                    bool wasEnabled = m_rte.Undo.Enabled;
                    m_rte.Undo.Enabled = false;

                    m_rte.Selection.activeObject = PolyShape.gameObject;
                    PolyShapeUpdatePivot(PolyShape);
                    m_rte.Undo.Enabled = wasEnabled;
                }
                
                PolyShape = null;
            }
        }

        private void SetLayer(GameObject go)
        {
            int layer = m_rte.CameraLayerSettings.AllScenesLayer;

            foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            {
                if (child.transform == go.transform)
                {
                    continue;
                }

                if(child.GetComponent<WireframeMesh>() != null)
                {
                    continue;
                }

                child.gameObject.layer = layer;
            }
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

            if(m_exposeToEditor != null && m_exposeToEditor.MarkAsDestroyed)
            {
                EndEditing(true);
                m_exposeToEditor = null;
                return;
            }

            if (PolyShape != null && PolyShape.IsEditing)
            {
                BaseHandle baseHandle = m_rte.Tools.ActiveTool as BaseHandle;
                if (baseHandle != null && baseHandle.IsDragging && m_rte.Selection.activeGameObject == m_pivot.gameObject)
                {
                    if(baseHandle is PositionHandle)
                    {
                        PolyShape.SelectedPosition = PolyShape.transform.InverseTransformPoint(m_pivot.position);
                        m_endEditOnPointerUp = false;
                    }
                }
                else
                {
                   
                    if (m_rte.Input.GetPointerDown(0))
                    {
                        m_endEditOnPointerUp = true;
                        if (m_rte.Tools.ActiveTool is BaseHandle)
                        {
                            BaseHandle handle = (BaseHandle)m_rte.Tools.ActiveTool;
                            if (handle.IsDragging)
                            {
                                m_endEditOnPointerUp = false;
                            }
                        }
                    }
                    else if (m_rte.Input.GetKeyDown(KeyCode.Return))
                    {
                        EndEditing(true);
                    }
                    else if (m_rte.Input.GetPointerUp(0))
                    {
                        if (!m_rte.ActiveWindow.IsPointerOver || m_rte.Tools.ActiveTool != null && !(m_rte.Tools.ActiveTool is BoxSelection))
                        {
                            return;
                        }

                        if (m_endEditOnPointerUp)
                        {
                            RuntimeWindow window = m_rte.ActiveWindow;
                            if (PolyShape.Click(window.Camera, m_rte.Input.GetPointerXY(0)))
                            {
                                EndEditing(false);
                            }
                        }
                    }
                } 
            }
        }

        private void EndEditing(bool forceEndEditing)
        {
            if (PolyShape == null || !PolyShape.IsEditing)
            {
                return;
            }

            if (PolyShape.Stage == 0)
            {
                if (PolyShape.VertexCount < 3)
                {
                    m_tool.Mode = ProBuilderToolMode.Object;
                    return;
                }
                else
                {
                    PolyShape.Stage++;
                }
            }

            if (PolyShape.Stage > 0)
            {
                if (forceEndEditing)
                {
                    m_tool.Mode = ProBuilderToolMode.Object;
                }
                else
                {

                    bool wasEnabled = m_rte.Undo.Enabled;
                    m_rte.Undo.Enabled = false;
                    m_rte.Selection.activeObject = m_pivot.gameObject;
                    PolyShapeUpdatePivot(PolyShape);
                    m_rte.Undo.Enabled = wasEnabled;
                }
            }
        }

        private void PolyShapeUpdatePivot(PBPolyShape polyShape)
        {
            if (polyShape.SelectedIndex >= 0)
            {
                m_pivot.position = PolyShape.transform.TransformPoint(polyShape.SelectedPosition);
                m_pivot.rotation = Quaternion.identity;
            }
        }

        public void RecordState(
            MeshEditorState oldState, Vector3[] oldPositions,
            MeshEditorState newState, Vector3[] newPositions,
            bool oldStateChanged = true, bool newStateChanged = true)
        {

            PBPolyShape polyShape = PolyShape;
            UndoRedoCallback redo = record =>
            {
                if (newState != null)
                {
                    m_tool.Mode = ProBuilderToolMode.Object;
                    polyShape.SetState(newState);
                    if(newPositions != null)
                    {
                        polyShape.Positions = newPositions.ToList();
                        PolyShapeUpdatePivot(polyShape);
                    }
                    return newStateChanged;
                }
                return false;
            };

            UndoRedoCallback undo = record =>
            {
                if (oldState != null)
                {
                    m_tool.Mode = ProBuilderToolMode.Object;
                    polyShape.SetState(oldState);
                    if(oldPositions != null)
                    {
                        polyShape.Positions = oldPositions.ToList();
                        PolyShapeUpdatePivot(polyShape);
                    }
                    return oldStateChanged;
                }
                return false;
            };

            IOC.Resolve<IRTE>().Undo.CreateRecord(redo, undo);
        }

        private MeshEditorState m_oldState;
        private Vector3[] m_oldPositions;
        private void OnBeginMove(BaseHandle positionHandle)
        {
            if (m_tool.Mode != ProBuilderToolMode.PolyShape)
            {
                return;
            }

            if(PolyShape.Stage == 0)
            {
                return;
            }

            if(m_rte.Selection.activeGameObject != m_pivot.gameObject)
            {
                return;
            }

            positionHandle.EnableUndo = false;
            m_oldPositions = PolyShape.Positions.ToArray();
            m_oldState = PolyShape.GetState(false);
        }

        private void OnEndMove(BaseHandle positionHandle)
        {
            if (m_tool.Mode != ProBuilderToolMode.PolyShape)
            {
                return;
            }
            if (PolyShape.Stage == 0)
            {
                return;
            }
            if (m_rte.Selection.activeGameObject != m_pivot.gameObject)
            {
                return;
            }

            positionHandle.EnableUndo = true;

            PolyShape.Refresh();
            MeshEditorState newState = PolyShape.GetState(false);
            RecordState(m_oldState, m_oldPositions, newState, PolyShape.Positions.ToArray()); 
        }
    }
}
