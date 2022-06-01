using Battlehub.RTCommon;
using Battlehub.RTHandles;
using Battlehub.UIControls.Dialogs;
using Battlehub.UIControls.DockPanels;
using Battlehub.UIControls.MenuControl;
using Battlehub.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.RTEditor
{
    public interface IWindowManager
    {
        bool IsDialogOpened
        {
            get;
        }

        Transform ComponentsRoot
        {
            get;
        }

        event Action<IWindowManager> AfterLayout;
        event Action<Transform> WindowCreated;
        event Action<Transform> WindowDestroyed;

        Workspace ActiveWorkspace
        {
            get;
            set;
        }

        LayoutInfo CreateLayoutInfo(Transform content, string header, Sprite icon);
        bool ValidateLayout(LayoutInfo layout);
        void OverrideDefaultLayout(Func<IWindowManager, LayoutInfo> callback, string activateWindowOfType = null);
        void SetDefaultLayout();
        void SetLayout(Func<IWindowManager, LayoutInfo> callback, string activateWindowOfType = null);

        void OverrideWindow(string windowTypeName, WindowDescriptor descriptor);
        void OverrideTools(Transform contentPrefab);
        void SetTools(Transform content);
        void SetLeftBar(Transform tools);
        void SetRightBar(Transform tools);
        void SetTopBar(Transform tools);
        void SetBottomBar(Transform tools);

        bool IsWindowRegistered(string windowTypeName);
        bool RegisterWindow(CustomWindowDescriptor desc);

        WindowDescriptor GetWindowDescriptor(string windowTypeName, out bool isDialog);
        Transform GetWindow(string windowTypeName);
        Transform[] GetWindows();
        Transform[] GetWindows(string windowTypeName);
        Transform[] GetComponents(Transform content);

        bool Exists(string windowTypeName);
        bool IsActive(string windowType);
        bool IsActive(Transform content);
        Transform FindPointerOverWindow(RuntimeWindow exceptWindow);

        bool ActivateWindow(string windowTypeName);
        bool ActivateWindow(Transform content);

        Transform CreateWindow(string windowTypeName, out WindowDescriptor wd, out GameObject content, out bool isDialog);
        Transform CreateWindow(string windowTypeName, bool isFree = true, RegionSplitType splitType = RegionSplitType.None, float flexibleSize = 0.3f, Transform parentWindow = null);
        void DestroyWindow(Transform conent);

        Transform CreateDialogWindow(string windowTypeName, string header, DialogAction<DialogCancelArgs> okAction, DialogAction<DialogCancelArgs> cancelAction = null,
             float minWidth = 250,
             float minHeight = 250,
             float preferredWidth = 700,
             float preferredHeight = 400,
             bool canResize = true);
        void DestroyDialogWindow();

        void MessageBox(string header, string text, DialogAction<DialogCancelArgs> ok = null);
        void MessageBox(Sprite icon, string header, string text, DialogAction<DialogCancelArgs> ok = null);
        void Confirmation(string header, string text, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel");
        void Confirmation(Sprite icon, string header, string text, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel");

        void Dialog(string header, Transform content, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel",
             float minWidth = 150,
             float minHeight = 150,
             float preferredWidth = 700,
             float preferredHeight = 400);

        void Dialog(Sprite icon, string header, Transform content, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel",
            float minWidth = 150,
             float minHeight = 150,
             float preferredWidth = 700,
             float preferredHeight = 400);

        string DefaultPersistentLayoutName
        {
            get;
        }

        bool LayoutExist(string name);
        void SaveLayout(string name);
        LayoutInfo GetLayout(string name);
        void LoadLayout(string name);
        void DeleteLayout(string name);
        void ForceLayoutUpdate();
    }

    public static class IWindowManagerExt
    {
        public static LayoutInfo GetBuiltInDefaultLayout(this IWindowManager wm)
        {
            WindowDescriptor sceneWd;
            GameObject sceneContent;
            bool isDialog;
            wm.CreateWindow(RuntimeWindowType.Scene.ToString(), out sceneWd, out sceneContent, out isDialog);

            WindowDescriptor inspectorWd;
            GameObject inspectorContent;
            wm.CreateWindow(RuntimeWindowType.Inspector.ToString(), out inspectorWd, out inspectorContent, out isDialog);

#if UNITY_EDITOR
            WindowDescriptor consoleWd;
            GameObject consoleContent;
            wm.CreateWindow(RuntimeWindowType.Console.ToString(), out consoleWd, out consoleContent, out isDialog);
#endif

            WindowDescriptor hierarchyWd;
            GameObject hierarchyContent;
            wm.CreateWindow(RuntimeWindowType.Hierarchy.ToString(), out hierarchyWd, out hierarchyContent, out isDialog);

            LayoutInfo layout = new LayoutInfo(false,
                new LayoutInfo(false,
                    new LayoutInfo(true,
                        wm.CreateLayoutInfo(inspectorContent.transform, inspectorWd.Header, inspectorWd.Icon),
                        wm.CreateLayoutInfo(consoleContent.transform, consoleWd.Header, consoleWd.Icon),
                        0.5f),
                        wm.CreateLayoutInfo(sceneContent.transform, sceneWd.Header, sceneWd.Icon),
                    0.25f),
                wm.CreateLayoutInfo(hierarchyContent.transform, hierarchyWd.Header, hierarchyWd.Icon),
                0.75f);

            return layout;
        }
    }

    [Serializable]
    public class WindowDescriptor
    {
        public Sprite Icon;
        public string Header;
        public GameObject ContentPrefab;

        public int MaxWindows = 1;
        [ReadOnly]
        public int Created = 0;
    }

    [Serializable]
    public class CustomWindowDescriptor
    {
        public string TypeName;
        public bool IsDialog;
        public WindowDescriptor Descriptor;
    }

    [DefaultExecutionOrder(-89)]
    public class WindowManager : MonoBehaviour, IWindowManager
    {
        public event Action<IWindowManager> AfterLayout;
        public event Action<Transform> WindowCreated;
        public event Action<Transform> WindowDestroyed;

        [SerializeField, Obsolete]
        private DialogManager m_dialogManager = null;

        [SerializeField]
        private WindowDescriptor m_sceneWindow = null;

        [SerializeField]
        private WindowDescriptor m_gameWindow = null;

        [SerializeField]
        private WindowDescriptor m_hierarchyWindow = null;

        [SerializeField]
        private WindowDescriptor m_inspectorWindow = null;

        [SerializeField]
        private WindowDescriptor m_projectWindow = null;

        [SerializeField]
        private WindowDescriptor m_consoleWindow = null;

        [SerializeField]
        private WindowDescriptor m_animationWindow = null;

        [SerializeField]
        private WindowDescriptor m_saveSceneDialog = null;

        [SerializeField]
        private WindowDescriptor m_saveAssetDialog = null;

        [SerializeField]
        private WindowDescriptor m_openProjectDialog = null;

        [SerializeField]
        private WindowDescriptor m_selectAssetLibraryDialog = null;

        [SerializeField]
        private WindowDescriptor m_toolsWindow = null;

        [SerializeField]
        private WindowDescriptor m_importAssetsDialog = null;

        [SerializeField]
        private WindowDescriptor m_aboutDialog = null;

        [SerializeField]
        private WindowDescriptor m_selectObjectDialog = null;

        [SerializeField]
        private WindowDescriptor m_selectColorDialog = null;

        [SerializeField]
        private WindowDescriptor m_selectAnimationPropertiesDialog = null;

        [SerializeField]
        private WindowDescriptor m_saveFileDialog = null;

        [SerializeField]
        private WindowDescriptor m_openFileDialog = null;

        [SerializeField]
        private CustomWindowDescriptor[] m_customWindows = null;

        [SerializeField, Obsolete, HideInInspector]
        private DockPanel m_dockPanels = null;

        [SerializeField, Obsolete]
        private Transform m_componentsRoot = null;

        [SerializeField, Obsolete]
        private RectTransform m_toolsRoot = null;

        [SerializeField, Obsolete]
        private RectTransform m_topBar = null;

        [SerializeField, Obsolete]
        private RectTransform m_bottomBar = null;

        [SerializeField, Obsolete]
        private RectTransform m_leftBar = null;

        [SerializeField, Obsolete]
        private RectTransform m_rightBar = null;

        [SerializeField]
        private Workspace m_activeWorkspace;
        public Workspace ActiveWorkspace
        {
            get { return m_activeWorkspace; }
            set
            {
                if (m_activeWorkspace != null)
                {
                    m_activeWorkspace.AfterLayout -= OnAfterLayout;
                    m_activeWorkspace.WindowCreated -= OnWindowCreated;
                    m_activeWorkspace.WindowDestroyed -= OnWindowDestroyed;
                    m_activeWorkspace.DeferUpdate -= OnDeferUpdate;
                }

                m_activeWorkspace = value;

                if (m_activeWorkspace != null)
                {
                    m_activeWorkspace.AfterLayout += OnAfterLayout;
                    m_activeWorkspace.WindowCreated += OnWindowCreated;
                    m_activeWorkspace.WindowDestroyed += OnWindowDestroyed;
                    m_activeWorkspace.DeferUpdate += OnDeferUpdate;
                }
            }
        }

        private IInput Input
        {
            get { return m_editor.Input; }
        }

        private RuntimeWindow ActiveWindow
        {
            get { return m_editor.ActiveWindow; }
        }

        private RuntimeWindow[] Windows
        {
            get { return m_editor.Windows; }
        }

        private IUIRaycaster Raycaster
        {
            get { return m_editor.Raycaster; }
        }

        private bool IsInputFieldFocused
        {
            get { return m_editor.IsInputFieldFocused; }
        }

        public bool IsDialogOpened
        {
            get { return ActiveWorkspace.DialogManager.IsDialogOpened; }
        }

        public Transform ComponentsRoot
        {
            get { return ActiveWorkspace.ComponentsRoot; }
        }

        private ILocalization m_localization;
        private IRTE m_editor;
        private float m_zAxis;
        private bool m_skipUpdate;
        public readonly Dictionary<string, CustomWindowDescriptor> m_typeToCustomWindow = new Dictionary<string, CustomWindowDescriptor>();

        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            m_localization = IOC.Resolve<ILocalization>();

#pragma warning disable CS0612
            if (m_componentsRoot == null)
            {
                m_componentsRoot = transform;
            }

            if (m_activeWorkspace == null)
            {
                if (m_dockPanels != null)
                {
                    if (m_dialogManager == null)
                    {
                        m_dialogManager = FindObjectOfType<DialogManager>();
                    }
                    InitActiveWorkspace_Internal();
                }
                else
                {
                    Debug.LogError("m_activeWorkspace is null");
                    return;
                }
            }
        }

        internal void InitActiveWorkspace_Internal()
        {
            Workspace activeWorkspace = gameObject.AddComponent<Workspace>();
            activeWorkspace.ComponentsRoot = m_componentsRoot;
            activeWorkspace.ToolsRoot = m_toolsRoot;
            activeWorkspace.TopBar = m_topBar;
            activeWorkspace.BottomBar = m_bottomBar;
            activeWorkspace.LeftBar = m_leftBar;
            activeWorkspace.RightBar = m_rightBar;
            activeWorkspace.DockPanel = m_dockPanels;
            activeWorkspace.DialogManager = m_dialogManager;
            activeWorkspace.Init();
            ActiveWorkspace = activeWorkspace;
        }
#pragma warning restore

        private void Start()
        {
            for (int i = 0; i < m_customWindows.Length; ++i)
            {
                CustomWindowDescriptor customWindow = m_customWindows[i];
                if (customWindow != null && customWindow.Descriptor != null && !m_typeToCustomWindow.ContainsKey(customWindow.TypeName))
                {
                    m_typeToCustomWindow.Add(customWindow.TypeName, customWindow);
                }
            }

            m_sceneWindow.MaxWindows = m_editor.CameraLayerSettings.MaxGraphicsLayers;

            SetDefaultLayout();

            WindowDescriptor wd;
            GameObject content;
            bool isDialog;

            Transform tools = CreateWindow(RuntimeWindowType.ToolsPanel.ToString().ToLower(), out wd, out content, out isDialog);
            if (tools != null)
            {
                SetTools(tools);
            }
        }

        private void OnDestroy()
        {
            if (m_activeWorkspace != null)
            {
                m_activeWorkspace.AfterLayout -= OnAfterLayout;
                m_activeWorkspace.WindowCreated -= OnWindowCreated;
                m_activeWorkspace.WindowDestroyed -= OnWindowDestroyed;
                m_activeWorkspace.DeferUpdate -= OnDeferUpdate;
            }
        }

        private void Update()
        {
            if (m_skipUpdate)
            {
                m_skipUpdate = false;
                return;
            }

            if (!m_editor.IsInputFieldActive)
            {
                if (IsDialogOpened)
                {
                    if (m_editor.Input.GetKeyDown(KeyCode.Escape))
                    {
                        ActiveWorkspace.DialogManager.CloseDialog();
                    }
                }
            }

            m_editor.UpdateCurrentInputField();
            EnableOrDisableRaycasts();

            bool mwheel = false;
            if (m_zAxis != Mathf.CeilToInt(Mathf.Abs(Input.GetAxis(InputAxis.Z))))
            {
                mwheel = m_zAxis == 0;
                m_zAxis = Mathf.CeilToInt(Mathf.Abs(Input.GetAxis(InputAxis.Z)));
            }

            bool pointerDownOrUp = Input.GetPointerDown(0) ||
                Input.GetPointerDown(1) ||
                Input.GetPointerDown(2) ||
                Input.GetPointerUp(0);

            bool canActivate = pointerDownOrUp ||
                mwheel ||
                Input.IsAnyKeyDown() && !IsInputFieldFocused;

            if (canActivate)
            {
                List<RaycastResult> results = new List<RaycastResult>();
                Raycaster.Raycast(results);

                RectTransform activeRectTransform = GetRegionTransform(ActiveWindow);
                bool activeWindowContainsScreenPoint = activeRectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(activeRectTransform, Input.GetPointerXY(0), Raycaster.eventCamera);

                if (!results.Any(r => r.gameObject.GetComponent<Menu>() || r.gameObject.GetComponent<WindowOverlay>()))
                {
                    var regions = results.Select(r => r.gameObject.GetComponentInParent<Region>()).Where(r => r != null);//.OrderBy(r => r.transform.localPosition.z);

                    foreach (Region region in regions)
                    {
                        RuntimeWindow window = region.ActiveContent != null ? region.ActiveContent.GetComponentInChildren<RuntimeWindow>() : region.ContentPanel.GetComponentInChildren<RuntimeWindow>();
                        if (window != null && (!activeWindowContainsScreenPoint || window.Depth >= ActiveWindow.Depth))
                        {
                            if (m_editor.Contains(window))
                            {
                                if (pointerDownOrUp || window.ActivateOnAnyKey)
                                {
                                    if (window != null)
                                    {
                                        IEnumerable<Selectable> selectables = results.Select(r => r.gameObject.GetComponent<Selectable>()).Where(s => s != null);
                                        int count = selectables.Count();
                                        if (count >= 1)
                                        {
                                            RuntimeSelectionComponentUI selectionComponentUI = selectables.First() as RuntimeSelectionComponentUI;
                                            if (selectionComponentUI != null)
                                            {
                                                selectionComponentUI.Select();
                                            }
                                        }

                                        IEnumerable<Resizer> resizer = results.Select(r => r.gameObject.GetComponent<Resizer>()).Where(r => r != null);
                                        if (resizer.Any())
                                        {
                                            break;
                                        }
                                    }

                                    if (window != ActiveWindow)
                                    {
                                        m_editor.ActivateWindow(window);
                                        region.MoveRegionToForeground();
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void OnAfterLayout(Workspace obj)
        {
            if (AfterLayout != null)
            {
                AfterLayout(this);
            }
        }

        private void OnWindowCreated(Transform window)
        {
            if (WindowCreated != null)
            {
                WindowCreated(window);
            }
        }

        private void OnWindowDestroyed(Transform window)
        {
            if (WindowDestroyed != null)
            {
                WindowDestroyed(window);
            }
        }

        private void OnDeferUpdate()
        {
            m_skipUpdate = true;
        }

        private RectTransform GetRegionTransform(RuntimeWindow window)
        {
            if (window == null)
            {
                return null;
            }

            Region region = window.GetComponentInParent<Region>();
            if (region == null)
            {
                return null;
            }

            return region.GetDragRegion() as RectTransform;
        }

        private void EnableOrDisableRaycasts()
        {
            if (ActiveWindow != null)
            {
                if (ActiveWorkspace.IsPointerOver(ActiveWindow) && !IsOverlapped(ActiveWindow))
                {
                    if (!ActiveWorkspace.IsPointerOverActiveWindow)
                    {
                        ActiveWorkspace.IsPointerOverActiveWindow = true;

                        RuntimeWindow[] windows = Windows;

                        for (int i = 0; i < windows.Length; ++i)
                        {
                            RuntimeWindow window = windows[i];
                            window.DisableRaycasts();
                        }
                    }
                }
                else
                {
                    if (ActiveWorkspace.IsPointerOverActiveWindow)
                    {
                        ActiveWorkspace.IsPointerOverActiveWindow = false;

                        RuntimeWindow[] windows = Windows;

                        for (int i = 0; i < windows.Length; ++i)
                        {
                            RuntimeWindow window = windows[i];
                            window.EnableRaycasts();
                        }
                    }
                }
            }
        }

        private bool IsOverlapped(RuntimeWindow testWindow, RuntimeWindow exceptWindow = null)
        {
            for (int i = 0; i < Windows.Length; ++i)
            {
                RuntimeWindow window = Windows[i];
                if (window == testWindow)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)window.transform, Input.GetPointerXY(0), Raycaster.eventCamera))
                {
                    if (testWindow.Depth < window.Depth && exceptWindow != window)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Transform FindPointerOverWindow(RuntimeWindow exceptWindow = null)
        {
            return ActiveWorkspace.FindPointerOverWindow(exceptWindow);
        }

        public bool IsWindowRegistered(string windowTypeName)
        {
            bool isDialog;
            return GetWindowDescriptor(windowTypeName, out isDialog) != null;
        }

        public bool RegisterWindow(CustomWindowDescriptor desc)
        {
            if (m_typeToCustomWindow.ContainsKey(desc.TypeName.ToLower()))
            {
                return false;
            }

            m_typeToCustomWindow.Add(desc.TypeName.ToLower(), desc);
            return true;
        }

        public LayoutInfo CreateLayoutInfo(Transform content, string header, Sprite icon)
        {
            return ActiveWorkspace.CreateLayoutInfo(content, header, icon);
        }

        public bool ValidateLayout(LayoutInfo layoutInfo)
        {
            return ActiveWorkspace.ValidateLayout(layoutInfo);
        }

        public void OverrideDefaultLayout(Func<IWindowManager, LayoutInfo> buildLayoutCallback, string activateWindowOfType = null)
        {
            if (ActiveWorkspace == null)
            {
                //QuickFix - will be removed 
                InitActiveWorkspace_Internal();
            }

            ActiveWorkspace.OverrideDefaultLayout(buildLayoutCallback, activateWindowOfType);
        }

        public void SetDefaultLayout()
        {
            ActiveWorkspace.SetDefaultLayout();
        }

        public void OverrideWindow(string windowTypeName, WindowDescriptor descriptor)
        {
            windowTypeName = windowTypeName.ToLower();

            if (windowTypeName == RuntimeWindowType.Scene.ToString().ToLower())
            {
                m_sceneWindow = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.Game.ToString().ToLower())
            {
                m_gameWindow = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.Hierarchy.ToString().ToLower())
            {
                m_hierarchyWindow = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.Inspector.ToString().ToLower())
            {
                m_inspectorWindow = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.Project.ToString().ToLower())
            {
                m_projectWindow = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.Console.ToString().ToLower())
            {
                m_consoleWindow = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.Animation.ToString().ToLower())
            {
                m_animationWindow = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.SaveScene.ToString().ToLower())
            {
                m_saveSceneDialog = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.SaveAsset.ToString().ToLower())
            {
                m_saveAssetDialog = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.OpenProject.ToString().ToLower())
            {
                m_openProjectDialog = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.ToolsPanel.ToString().ToLower())
            {
                m_toolsWindow = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.SelectAssetLibrary.ToString().ToLower())
            {
                m_selectAssetLibraryDialog = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.ImportAssets.ToString().ToLower())
            {
                m_importAssetsDialog = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.About.ToString().ToLower())
            {
                m_aboutDialog = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.SelectObject.ToString().ToLower())
            {
                m_selectObjectDialog = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.SelectColor.ToString().ToLower())
            {
                m_selectColorDialog = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.SelectAnimationProperties.ToString().ToLower())
            {
                m_selectAnimationPropertiesDialog = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.SaveFile.ToString().ToLower())
            {
                m_saveFileDialog = descriptor;
            }
            else if (windowTypeName == RuntimeWindowType.OpenFile.ToString().ToLower())
            {
                m_openFileDialog = descriptor;
            }
        }

        public void OverrideTools(Transform contentPrefab)
        {
            if (contentPrefab == null)
            {
                m_toolsWindow.ContentPrefab = null;
                return;
            }
            m_toolsWindow.ContentPrefab = contentPrefab.gameObject;
        }

        public void SetTools(Transform tools)
        {
            ActiveWorkspace.SetTools(tools);
        }

        public void SetLeftBar(Transform tools)
        {
            ActiveWorkspace.SetLeftBar(tools);
        }

        public void SetRightBar(Transform tools)
        {
            ActiveWorkspace.SetRightBar(tools);
        }

        public void SetTopBar(Transform tools)
        {
            ActiveWorkspace.SetTopBar(tools);
        }

        public void SetBottomBar(Transform tools)
        {
            ActiveWorkspace.SetBottomBar(tools);
        }

        public void SetLayout(Func<IWindowManager, LayoutInfo> buildLayoutCallback, string activateWindowOfType = null)
        {
            if (ActiveWorkspace == null)
            {
                //QuickFix - will be removed 
                InitActiveWorkspace_Internal();
            }
            ActiveWorkspace.SetLayout(buildLayoutCallback, activateWindowOfType);
        }

        public bool Exists(string windowTypeName)
        {
            return GetWindow(windowTypeName) != null;
        }

        public Transform GetWindow(string windowTypeName)
        {
            return ActiveWorkspace.GetWindow(windowTypeName);
        }

        public Transform[] GetWindows()
        {
            return ActiveWorkspace.GetWindows();
        }

        public Transform[] GetWindows(string windowTypeName)
        {
            return ActiveWorkspace.GetWindows(windowTypeName);
        }

        public Transform[] GetComponents(Transform content)
        {
            return ActiveWorkspace.GetComponents(content);
        }

        public bool IsActive(string windowTypeName)
        {
            return ActiveWorkspace.IsActive(windowTypeName);
        }

        public bool IsActive(Transform content)
        {
            return ActiveWorkspace.IsActive(content);
        }

        public bool ActivateWindow(string windowTypeName)
        {
            return ActiveWorkspace.ActivateWindow(windowTypeName);
        }

        public bool ActivateWindow(Transform content)
        {
            return ActiveWorkspace.ActivateWindow(content);
        }

        public Transform CreateWindow(string windowTypeName, bool isFree = true, RegionSplitType splitType = RegionSplitType.None, float flexibleSize = 0.3f, Transform parentWindow = null)
        {
            return ActiveWorkspace.CreateWindow(windowTypeName, isFree, splitType, flexibleSize, parentWindow);
        }

        public void DestroyWindow(Transform content)
        {
            ActiveWorkspace.DestroyWindow(content);
        }

        public Transform CreateDialogWindow(string windowTypeName, string header, DialogAction<DialogCancelArgs> okAction, DialogAction<DialogCancelArgs> cancelAction,
             float minWidth,
             float minHeight,
             float preferredWidth,
             float preferredHeight,
             bool canResize = true)
        {
            return ActiveWorkspace.CreateDialogWindow(windowTypeName, header, okAction, cancelAction, minWidth, minHeight, preferredWidth, preferredHeight, canResize);
        }

        public void DestroyDialogWindow()
        {
            ActiveWorkspace.DestroyDialogWindow();
        }

        public Transform CreateWindow(string windowTypeName, out WindowDescriptor wd, out GameObject content, out bool isDialog)
        {
            return ActiveWorkspace.CreateWindow(windowTypeName, out wd, out content, out isDialog);
        }

        public WindowDescriptor GetWindowDescriptor(string windowTypeName, out bool isDialog)
        {
            WindowDescriptor wd = null;
            isDialog = false;
            if (windowTypeName == null)
            {
                return null;
            }

            windowTypeName = windowTypeName.ToLower();
            if (windowTypeName == RuntimeWindowType.Scene.ToString().ToLower())
            {
                wd = m_sceneWindow;
            }
            else if (windowTypeName == RuntimeWindowType.Game.ToString().ToLower())
            {
                wd = m_gameWindow;
            }
            else if (windowTypeName == RuntimeWindowType.Hierarchy.ToString().ToLower())
            {
                wd = m_hierarchyWindow;
            }
            else if (windowTypeName == RuntimeWindowType.Inspector.ToString().ToLower())
            {
                wd = m_inspectorWindow;
            }
            else if (windowTypeName == RuntimeWindowType.Project.ToString().ToLower())
            {
                wd = m_projectWindow;
            }
            else if (windowTypeName == RuntimeWindowType.Console.ToString().ToLower())
            {
                wd = m_consoleWindow;
            }
            else if (windowTypeName == RuntimeWindowType.Animation.ToString().ToLower())
            {
                wd = m_animationWindow;
            }
            else if (windowTypeName == RuntimeWindowType.ToolsPanel.ToString().ToLower())
            {
                wd = m_toolsWindow;
            }
            else if (windowTypeName == RuntimeWindowType.SaveScene.ToString().ToLower())
            {
                wd = m_saveSceneDialog;
                isDialog = true;
            }
            else if (windowTypeName == RuntimeWindowType.SaveAsset.ToString().ToLower())
            {
                wd = m_saveAssetDialog;
                isDialog = true;
            }
            else if (windowTypeName == RuntimeWindowType.OpenProject.ToString().ToLower())
            {
                wd = m_openProjectDialog;
                isDialog = true;
            }
            else if (windowTypeName == RuntimeWindowType.SelectAssetLibrary.ToString().ToLower())
            {
                wd = m_selectAssetLibraryDialog;
                isDialog = true;
            }
            else if (windowTypeName == RuntimeWindowType.ImportAssets.ToString().ToLower())
            {
                wd = m_importAssetsDialog;
                isDialog = true;
            }
            else if (windowTypeName == RuntimeWindowType.About.ToString().ToLower())
            {
                wd = m_aboutDialog;
                isDialog = true;
            }
            else if (windowTypeName == RuntimeWindowType.SelectObject.ToString().ToLower())
            {
                wd = m_selectObjectDialog;
                isDialog = true;
            }
            else if (windowTypeName == RuntimeWindowType.SelectColor.ToString().ToLower())
            {
                wd = m_selectColorDialog;
                isDialog = true;
            }
            else if (windowTypeName == RuntimeWindowType.SelectAnimationProperties.ToString().ToLower())
            {
                wd = m_selectAnimationPropertiesDialog;
                isDialog = true;
            }
            else if (windowTypeName == RuntimeWindowType.SaveFile.ToString().ToLower())
            {
                wd = m_saveFileDialog;
                isDialog = true;
            }
            else if (windowTypeName == RuntimeWindowType.OpenFile.ToString().ToLower())
            {
                wd = m_openFileDialog;
                isDialog = true;
            }
            else
            {
                CustomWindowDescriptor cwd;
                if (m_typeToCustomWindow.TryGetValue(windowTypeName, out cwd))
                {
                    wd = cwd.Descriptor;
                    isDialog = cwd.IsDialog;
                }
            }

            return wd;
        }


        public void MessageBox(string header, string text, DialogAction<DialogCancelArgs> ok = null)
        {
            ActiveWorkspace.MessageBox(header, text, ok);
        }

        public void MessageBox(Sprite icon, string header, string text, DialogAction<DialogCancelArgs> ok = null)
        {
            ActiveWorkspace.MessageBox(icon, header, text, ok);
        }

        public void Confirmation(string header, string text, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel")
        {
            ActiveWorkspace.Confirmation(header, text, ok, cancel, okText, cancelText);
        }

        public void Confirmation(Sprite icon, string header, string text, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel")
        {
            ActiveWorkspace.Confirmation(icon, header, text, ok, cancel, okText, cancelText);
        }

        public void Dialog(string header, Transform content, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel",
             float minWidth = 150,
             float minHeight = 150,
             float preferredWidth = 700,
             float preferredHeight = 400)
        {
            ActiveWorkspace.Dialog(header, content, ok, cancel, okText, cancelText, minWidth, minHeight, preferredWidth, preferredHeight);
        }

        public void Dialog(Sprite icon, string header, Transform content, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel",
            float minWidth = 150,
             float minHeight = 150,
             float preferredWidth = 700,
             float preferredHeight = 400)
        {
            ActiveWorkspace.Dialog(icon, header, content, ok, cancel, okText, cancelText, minWidth, minHeight, preferredWidth, preferredHeight);
        }

        public string DefaultPersistentLayoutName
        {
            get
            {
                return "Persistent_Layout";
            }
        }

        public bool LayoutExist(string name)
        {
            return ActiveWorkspace.LayoutExist(name);
        }

        public void SaveLayout(string name)
        {
            ActiveWorkspace.SaveLayout(name);
        }

        public LayoutInfo GetLayout(string name)
        {
            return ActiveWorkspace.GetLayout(name);
        }

        public void LoadLayout(string name)
        {
            ActiveWorkspace.LoadLayout(name);
        }


        public void DeleteLayout(string name)
        {
            ActiveWorkspace.DeleteLayout(name);
        }

        public void ForceLayoutUpdate()
        {
            ActiveWorkspace.ForceLayoutUpdate();
        }
    }
}

