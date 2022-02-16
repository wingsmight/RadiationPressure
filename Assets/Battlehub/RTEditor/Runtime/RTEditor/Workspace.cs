using Battlehub.RTCommon;
using Battlehub.UIControls.Dialogs;
using Battlehub.UIControls.DockPanels;
using Battlehub.UIControls;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Battlehub.Utils;
using System.Collections;

namespace Battlehub.RTEditor
{
    public class Workspace : MonoBehaviour
    {
        public event Action<Workspace> AfterLayout;
        public event Action<Transform> WindowCreated;
        public event Action<Transform> WindowDestroyed;
        public event Action DeferUpdate;

        [SerializeField]
        private DialogManager m_dialogManager = null;
        public DialogManager DialogManager
        {
            get { return m_dialogManager; }
            set { m_dialogManager = value; }
        }
     
        [SerializeField]
        private DockPanel m_dockPanel = null;
        public DockPanel DockPanel
        {
            get { return m_dockPanel; }
            set { m_dockPanel = value; }
        }


        [SerializeField]
        private Transform m_componentsRoot = null;
        public Transform ComponentsRoot
        {
            get { return m_componentsRoot; }
            set { m_componentsRoot = value; }
        }

        [SerializeField]
        private RectTransform m_toolsRoot = null;
        public RectTransform ToolsRoot
        {
            get { return m_toolsRoot; }
            set { m_toolsRoot = value; }
        }

        [SerializeField]
        private RectTransform m_topBar = null;
        public RectTransform TopBar
        {
            get { return m_topBar; }
            set { m_topBar = value; }
        }

        [SerializeField]
        private RectTransform m_bottomBar = null;
        public RectTransform BottomBar
        {
            get { return m_bottomBar; }
            set { m_bottomBar = value; }
        }

        [SerializeField]
        private RectTransform m_leftBar = null;
        public RectTransform LeftBar
        {
            get { return m_leftBar; }
            set { m_leftBar = value; }
        }

        [SerializeField]
        private RectTransform m_rightBar = null;
        public RectTransform RightBar
        {
            get { return m_rightBar; }
            set { m_rightBar = value; }
        }

        private bool m_isPointerOverActiveWindow = true;
        public bool IsPointerOverActiveWindow
        {
            get { return m_isPointerOverActiveWindow; }
            set { m_isPointerOverActiveWindow = value; }
        }

        private RuntimeWindow[] Windows
        {
            get { return m_editor.Windows; }
        }

        private IInput Input
        {
            get { return m_editor.Input; }
        }

        private IUIRaycaster Raycaster
        {
            get { return m_editor.Raycaster; }
        }

        public readonly Dictionary<string, HashSet<Transform>> m_windows = new Dictionary<string, HashSet<Transform>>();
        public readonly Dictionary<Transform, List<Transform>> m_extraComponents = new Dictionary<Transform, List<Transform>>();

        public Func<IWindowManager, LayoutInfo> m_overrideLayoutCallback;
        public string m_activateWindowOfType;

        private IRTE m_editor;
        private ILocalization m_localization;
        private IWindowManager m_windowManager;
        private bool m_lockUpdateLayout;

        private void Awake()
        {
            m_windowManager = IOC.Resolve<IWindowManager>();
            m_editor = IOC.Resolve<IRTE>();
            m_localization = IOC.Resolve<ILocalization>();

            Init();
        }

        public void Init()
        {
            Unsubscribe();

            if(m_dockPanel != null)
            {
                m_dockPanel.CursorHelper = m_editor.CursorHelper;
                if (RenderPipelineInfo.UseRenderTextures)
                {
                    DepthMaskingBehavior depthMaskingBehavior = m_dockPanel.GetComponent<DepthMaskingBehavior>();
                    Destroy(depthMaskingBehavior);
                }
            }

            Subscribe();
        }


        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void SetTools(Transform tools)
        {
            Transform window = GetWindow(RuntimeWindowType.ToolsPanel.ToString().ToLower());
            if (window != null)
            {
                OnContentDestroyed(window);
            }

            SetContent(m_toolsRoot, tools);
        }

        public void SetLeftBar(Transform tools)
        {
            SetContent(m_leftBar, tools);
        }

        public void SetRightBar(Transform tools)
        {
            SetContent(m_rightBar, tools);
        }

        public void SetTopBar(Transform tools)
        {
            SetContent(m_topBar, tools);
        }

        public void SetBottomBar(Transform tools)
        {
            SetContent(m_bottomBar, tools);
        }

        private static void SetContent(Transform root, Transform content)
        {
            if (root != null)
            {
                foreach (Transform child in root)
                {
                    Destroy(child.gameObject);
                }
            }

            if (content != null)
            {
                content.SetParent(root, false);

                RectTransform rt = content as RectTransform;
                if (rt != null)
                {
                    rt.Stretch();
                }

                content.gameObject.SetActive(true);
            }
        }


        public LayoutInfo CreateLayoutInfo(Transform content, string header, Sprite icon)
        {
            Tab tab = Instantiate(DockPanel.TabPrefab);
            tab.Text = header;
            tab.Icon = icon;
            return new LayoutInfo(content, tab);
        }

        public bool ValidateLayout(LayoutInfo layoutInfo)
        {
            return DockPanel.RootRegion.Validate(layoutInfo);
        }

        public void OverrideDefaultLayout(Func<IWindowManager, LayoutInfo> buildLayoutCallback, string activateWindowOfType = null)
        {
            m_overrideLayoutCallback = buildLayoutCallback;
            m_activateWindowOfType = activateWindowOfType;
        }

        public void SetDefaultLayout()
        {
            if (m_overrideLayoutCallback != null)
            {
                SetLayout(m_overrideLayoutCallback, m_activateWindowOfType);
            }
            else
            {
                SetLayout(wm => IWindowManagerExt.GetBuiltInDefaultLayout(wm), RuntimeWindowType.Scene.ToString().ToLower());
            }
        }

        public void SetLayout(Func<IWindowManager, LayoutInfo> buildLayoutCallback, string activateWindowOfType = null)
        {
            Region rootRegion = DockPanel.RootRegion;
            if (rootRegion == null)
            {
                return;
            }
            if (m_editor == null)
            {
                return;
            }

            try
            {
                m_lockUpdateLayout = true;

                bool hasChildren = rootRegion.HasChildren;
                ClearRegion(rootRegion);
                foreach (Transform child in DockPanel.Free)
                {
                    Region region = child.GetComponent<Region>();
                    ClearRegion(region);
                }

                m_editor.StartCoroutine(CoSetLayout(hasChildren, buildLayoutCallback, activateWindowOfType));
            }
            catch
            {
                m_lockUpdateLayout = false;
            }
        }

        private IEnumerator CoSetLayout(bool waitForEndOfFrame, Func<IWindowManager, LayoutInfo> buildLayoutCallback, string activateWindowOfType = null)
        {
            if(waitForEndOfFrame)
            {
                //Wait for OnDestroy of destroyed windows 
                yield return new WaitForEndOfFrame();
            }
            
            try
            {
                m_lockUpdateLayout = true;                
                LayoutInfo layout = buildLayoutCallback(m_windowManager);
                if (layout.Content != null || layout.Child0 != null && layout.Child1 != null)
                {
                    DockPanel.RootRegion.Build(layout);
                }

                if (!string.IsNullOrEmpty(activateWindowOfType))
                {
                    ActivateWindow(activateWindowOfType);
                }
            }
            finally
            {
                m_lockUpdateLayout = false;
            }

            RuntimeWindow[] windows = Windows;
            if (windows != null)
            {
                for (int i = 0; i < windows.Length; ++i)
                {
                    windows[i].EnableRaycasts();
                    windows[i].HandleResize();
                }
            }

            if (AfterLayout != null)
            {
                AfterLayout(this);
            }
        }

        private void ClearRegion(Region rootRegion)
        {
            rootRegion.CloseAllTabs();
        }

        public Transform GetWindow(string windowTypeName)
        {
            HashSet<Transform> hs;
            if (m_windows.TryGetValue(windowTypeName.ToLower(), out hs))
            {
                return hs.FirstOrDefault();
            }
            return null;
        }

        public Transform[] GetWindows()
        {
            return m_windows.Values.SelectMany(w => w).ToArray();
        }

        public Transform[] GetWindows(string windowTypeName)
        {
            HashSet<Transform> hs;
            if (m_windows.TryGetValue(windowTypeName.ToLower(), out hs))
            {
                return hs.ToArray();
            }
            return new Transform[0];
        }

        public Transform[] GetComponents(Transform content)
        {
            List<Transform> extraComponents;
            if (m_extraComponents.TryGetValue(content, out extraComponents))
            {
                return extraComponents.ToArray();
            }
            return new Transform[0];
        }

        public bool IsActive(string windowTypeName)
        {
            HashSet<Transform> hs;
            if (m_windows.TryGetValue(windowTypeName.ToLower(), out hs))
            {
                foreach (Transform content in hs)
                {
                    Tab tab = Region.FindTab(content);
                    if (tab != null && tab.IsOn)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsActive(Transform content)
        {
            Tab tab = Region.FindTab(content);
            return tab != null && tab.IsOn;
        }

        public bool ActivateWindow(string windowTypeName)
        {
            Transform content = GetWindow(windowTypeName);
            if (content == null)
            {
                return false;
            }
            return ActivateWindow(content);
        }

        public bool ActivateWindow(Transform content)
        {
            if (content == null)
            {
                return false;
            }

            Region region = content.GetComponentInParent<Region>();
            if (region != null)
            {
                region.MoveRegionToForeground();
                IsPointerOverActiveWindow = m_editor != null && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)region.transform, Input.GetPointerXY(0), Raycaster.eventCamera);
                if (IsPointerOverActiveWindow)
                {
                    RuntimeWindow[] windows = Windows;
                    for (int i = 0; i < windows.Length; ++i)
                    {
                        windows[i].DisableRaycasts();
                    }
                }
            }

            Tab tab = Region.FindTab(content);
            if (tab == null)
            {
                return false;
            }

            tab.IsOn = true;
            return true;
        }

        public Transform CreateWindow(string windowTypeName, bool isFree = true, RegionSplitType splitType = RegionSplitType.None, float flexibleSize = 0.3f, Transform parentWindow = null)
        {
            Dialog dialog;
            return CreateWindow(windowTypeName, out dialog, isFree, splitType, flexibleSize, parentWindow);
        }

        private Transform CreateWindow(string windowTypeName, out Dialog dialog, bool isFree = true, RegionSplitType splitType = RegionSplitType.None, float flexibleSize = 0.3f, Transform parentWindow = null)
        {
            WindowDescriptor wd;
            GameObject content;
            bool isDialog;

            Transform window = CreateWindow(windowTypeName, out wd, out content, out isDialog);
            if (!window)
            {
                dialog = null;
                return window;
            }

            if (isDialog && isFree)
            {
                dialog = m_dialogManager.ShowDialog(wd.Icon, wd.Header, content.transform);
                dialog.IsCancelVisible = false;
                dialog.IsOkVisible = false;
            }
            else
            {
                dialog = null;

                Region targetRegion = null;
                if (parentWindow != null)
                {
                    targetRegion = parentWindow.GetComponentInParent<Region>();
                }

                if (targetRegion == null)
                {
                    targetRegion = DockPanel.RootRegion;
                }

                Tab tab = Instantiate(DockPanel.TabPrefab);
                tab.Text = wd.Header;
                tab.Icon = wd.Icon;

                targetRegion.Add(tab, content.transform, isFree, splitType, flexibleSize);

                if (!isFree)
                {
                    ForceLayoutUpdate();
                }

                RuntimeWindow region = window.GetComponentInParent<RuntimeWindow>();
                if (region != null)
                {
                    region.HandleResize();
                }

                targetRegion.RaiseDepthChanged();

            }

            ActivateContent(wd, content);

            if (WindowCreated != null)
            {
                WindowCreated(window);
            }

            return window;
        }

        public void DestroyWindow(Transform content)
        {
            Tab tab = Region.FindTab(content);
            if (tab != null)
            {
                DockPanel.RemoveRegion(content);
            }
            else
            {
                OnContentDestroyed(content);
            }
        }

        public Transform CreateDialogWindow(string windowTypeName, string header, DialogAction<DialogCancelArgs> okAction, DialogAction<DialogCancelArgs> cancelAction,
          float minWidth,
          float minHeight,
          float preferredWidth,
          float preferredHeight,
          bool canResize = true)
        {
            WindowDescriptor wd;
            GameObject content;
            bool isDialog;

            Transform window = CreateWindow(windowTypeName, out wd, out content, out isDialog);
            if (!window)
            {
                return window;
            }

            if (isDialog)
            {
                if (header == null)
                {
                    header = wd.Header;
                }
                Dialog dialog = m_dialogManager.ShowDialog(wd.Icon, header, content.transform,
                    okAction, m_localization.GetString("ID_RTEditor_WM_Dialog_OK", "OK"),
                    cancelAction, m_localization.GetString("ID_RTEditor_WM_Dialog_Cancel", "Cancel"),
                    minWidth, minHeight, preferredWidth, preferredHeight, canResize);
                dialog.IsCancelVisible = false;
                dialog.IsOkVisible = false;
            }
            else
            {
                throw new ArgumentException(windowTypeName + " is not a dialog");
            }

            ActivateContent(wd, content);

            return window;
        }

        public void DestroyDialogWindow()
        {
            m_dialogManager.CloseDialog();
        }

        public Transform CreateWindow(string windowTypeName, out WindowDescriptor wd, out GameObject content, out bool isDialog)
        {
            if (DockPanel == null)
            {
                Debug.LogError("Unable to create window. m_dockPanel == null. Set m_dockPanel field");
            }

            windowTypeName = windowTypeName.ToLower();

            content = null;
            wd = m_windowManager.GetWindowDescriptor(windowTypeName, out isDialog);
            if (wd == null)
            {
                Debug.LogWarningFormat("{0} window was not found", windowTypeName);
                return null;
            }

            if (wd.Created >= wd.MaxWindows)
            {
                return null;
            }
            wd.Created++;

            if (wd.ContentPrefab != null)
            {
                wd.ContentPrefab.SetActive(false);
                content = Instantiate(wd.ContentPrefab);
                content.name = windowTypeName;

                Transform[] children = content.transform.OfType<Transform>().ToArray();
                for (int i = 0; i < children.Length; ++i)
                {
                    Transform component = children[i];
                    if (!(component is RectTransform))
                    {
                        component.gameObject.SetActive(false);
                        component.transform.SetParent(m_componentsRoot, false);
                    }
                }

                List<Transform> extraComponents = new List<Transform>();
                for (int i = 0; i < children.Length; ++i)
                {
                    if (children[i].parent == m_componentsRoot)
                    {
                        extraComponents.Add(children[i]);
                    }
                }

                m_extraComponents.Add(content.transform, extraComponents);
            }
            else
            {
                content = new GameObject();
                content.AddComponent<RectTransform>();
                content.name = "Empty Content";

                m_extraComponents.Add(content.transform, new List<Transform>());
            }

            HashSet<Transform> windows;
            if (!m_windows.TryGetValue(windowTypeName, out windows))
            {
                windows = new HashSet<Transform>();
                m_windows.Add(windowTypeName, windows);
            }

            windows.Add(content.transform);
            return content.transform;
        }

        private void ActivateContent(WindowDescriptor wd, GameObject content)
        {
            List<Transform> extraComponentsList = new List<Transform>();
            m_extraComponents.TryGetValue(content.transform, out extraComponentsList);
            for (int i = 0; i < extraComponentsList.Count; ++i)
            {
                extraComponentsList[i].gameObject.SetActive(true);
            }

            wd.ContentPrefab.SetActive(true);
            content.SetActive(true);
        }

        public void MessageBox(string header, string text, DialogAction<DialogCancelArgs> ok = null)
        {
            m_dialogManager.ShowDialog(null, header, text,
                ok, m_localization.GetString("ID_RTEditor_WM_Dialog_OK", "OK"),
                null, m_localization.GetString("ID_RTEditor_WM_Dialog_Cancel", "Cancel"));
        }

        public void MessageBox(Sprite icon, string header, string text, DialogAction<DialogCancelArgs> ok = null)
        {
            m_dialogManager.ShowDialog(icon, header, text,
                ok, m_localization.GetString("ID_RTEditor_WM_Dialog_OK", "OK"),
                null, m_localization.GetString("ID_RTEditor_WM_Dialog_Cancel", "Cancel"));
        }

        public void Confirmation(string header, string text, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel")
        {
            m_dialogManager.ShowDialog(null, header, text, ok, okText, cancel, cancelText);

        }
        public void Confirmation(Sprite icon, string header, string text, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel")
        {
            m_dialogManager.ShowDialog(icon, header, text, ok, okText, cancel, cancelText);
        }

        public void Dialog(string header, Transform content, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel",
             float minWidth = 150,
             float minHeight = 150,
             float preferredWidth = 700,
             float preferredHeight = 400)
        {
            m_dialogManager.ShowDialog(null, header, content, ok, okText, cancel, cancelText, minWidth, minHeight, preferredWidth, preferredHeight);
        }

        public void Dialog(Sprite icon, string header, Transform content, DialogAction<DialogCancelArgs> ok, DialogAction<DialogCancelArgs> cancel, string okText = "OK", string cancelText = "Cancel",
            float minWidth = 150,
             float minHeight = 150,
             float preferredWidth = 700,
             float preferredHeight = 400)
        {
            m_dialogManager.ShowDialog(icon, header, content, ok, okText, cancel, cancelText, minWidth, minHeight, preferredWidth, preferredHeight);
        }

        public bool IsPointerOver(RuntimeWindow testWindow)
        {
            return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)testWindow.transform, Input.GetPointerXY(0), Raycaster.eventCamera);
        }

        public Transform FindPointerOverWindow(RuntimeWindow exceptWindow = null)
        {
            foreach (KeyValuePair<string, HashSet<Transform>> kvp in m_windows)
            {
                foreach (Transform content in kvp.Value)
                {
                    RuntimeWindow window = content.GetComponentInChildren<RuntimeWindow>();

                    if (window != null && window != exceptWindow && IsPointerOver(window) /* && !IsOverlapped(window, exceptWindow)*/)
                    {
                        Tab tab = Region.FindTab(window.transform);
                        if (tab != null && tab.IsOn)
                        {
                            return content;
                        }
                    }
                }
            }
            return null;
        }

        public bool LayoutExist(string name)
        {
            return PlayerPrefs.HasKey("Battlehub.RTEditor.Layout" + name);
        }

        public void SaveLayout(string name)
        {
            PersistentLayoutInfo layoutInfo = new PersistentLayoutInfo();
            ToPersistentLayout(DockPanel.RootRegion, layoutInfo);

            string serializedLayout = XmlUtility.ToXml(layoutInfo);
            PlayerPrefs.SetString("Battlehub.RTEditor.Layout" + name, serializedLayout);
            PlayerPrefs.Save();
        }

        private void ToPersistentLayout(Region region, PersistentLayoutInfo layoutInfo)
        {
            if (region.HasChildren)
            {
                Region childRegion0 = region.GetChild(0);
                Region childRegion1 = region.GetChild(1);

                RectTransform rt0 = (RectTransform)childRegion0.transform;
                RectTransform rt1 = (RectTransform)childRegion1.transform;

                Vector3 delta = rt0.localPosition - rt1.localPosition;
                layoutInfo.IsVertical = Mathf.Abs(delta.x) < Mathf.Abs(delta.y);

                if (layoutInfo.IsVertical)
                {
                    float y0 = Mathf.Max(0.000000001f, rt0.sizeDelta.y - childRegion0.MinHeight);
                    float y1 = Mathf.Max(0.000000001f, rt1.sizeDelta.y - childRegion1.MinHeight);

                    layoutInfo.Ratio = y0 / (y0 + y1);
                }
                else
                {
                    float x0 = Mathf.Max(0.000000001f, rt0.sizeDelta.x - childRegion0.MinWidth);
                    float x1 = Mathf.Max(0.000000001f, rt1.sizeDelta.x - childRegion1.MinWidth);

                    layoutInfo.Ratio = x0 / (x0 + x1);
                }

                layoutInfo.Child0 = new PersistentLayoutInfo();
                layoutInfo.Child1 = new PersistentLayoutInfo();

                ToPersistentLayout(childRegion0, layoutInfo.Child0);
                ToPersistentLayout(childRegion1, layoutInfo.Child1);
            }
            else
            {
                if (region.ContentPanel.childCount > 1)
                {
                    layoutInfo.TabGroup = new PersistentLayoutInfo[region.ContentPanel.childCount];
                    for (int i = 0; i < region.ContentPanel.childCount; ++i)
                    {
                        Transform content = region.ContentPanel.GetChild(i);
                        PersistentLayoutInfo tabLayout = new PersistentLayoutInfo();

                        ToPersistentLayout(region, content, tabLayout);
                        layoutInfo.TabGroup[i] = tabLayout;
                    }
                }
                else if (region.ContentPanel.childCount == 1)
                {
                    Transform content = region.ContentPanel.GetChild(0);
                    ToPersistentLayout(region, content, layoutInfo);
                }
            }
        }

        private void ToPersistentLayout(Region region, Transform content, PersistentLayoutInfo layoutInfo)
        {
            foreach (KeyValuePair<string, HashSet<Transform>> kvp in m_windows)
            {
                if (kvp.Value.Contains(content))
                {
                    layoutInfo.WindowType = kvp.Key;

                    Tab tab = Region.FindTab(content);
                    if (tab != null)
                    {
                        layoutInfo.CanDrag = tab.CanDrag;
                        layoutInfo.CanClose = tab.CanClose;
                        layoutInfo.IsOn = tab.IsOn;
                        layoutInfo.CanMaximize = tab.CanMaximize;
                    }
                    
                    layoutInfo.IsHeaderVisible = region.IsHeaderVisible;

                    break;
                }
            }
        }

        public LayoutInfo GetLayout(string name)
        {
            string serializedLayout = PlayerPrefs.GetString("Battlehub.RTEditor.Layout" + name);
            if (serializedLayout == null)
            {
                Debug.LogWarningFormat("Layout {0} does not exist ", name);
                return null;
            }

            PersistentLayoutInfo persistentLayoutInfo = XmlUtility.FromXml<PersistentLayoutInfo>(serializedLayout);
            LayoutInfo layoutInfo = new LayoutInfo();
            ToLayout(persistentLayoutInfo, layoutInfo);
            return layoutInfo;
        }

        public void LoadLayout(string name)
        {
            ClearRegion(DockPanel.RootRegion);
            foreach (Transform child in DockPanel.Free)
            {
                Region region = child.GetComponent<Region>();
                ClearRegion(region);
            }

            LayoutInfo layoutInfo = GetLayout(name);
            if (layoutInfo == null)
            {
                return;
            }

            SetLayout(wm => layoutInfo);

            RuntimeWindow[] windows = Windows;
            for (int i = 0; i < windows.Length; ++i)
            {
                windows[i].EnableRaycasts();
                windows[i].HandleResize();
            }
        }

        private void ToLayout(PersistentLayoutInfo persistentLayoutInfo, LayoutInfo layoutInfo)
        {
            if (!string.IsNullOrEmpty(persistentLayoutInfo.WindowType))
            {
                WindowDescriptor wd;
                GameObject content;
                bool isDialog;
                CreateWindow(persistentLayoutInfo.WindowType, out wd, out content, out isDialog);

                Tab tab = Instantiate(DockPanel.TabPrefab);
                if (content == null)
                {
                    tab.Text = "Empty";

                    layoutInfo.Content = new GameObject("Empty").AddComponent<RectTransform>();
                    layoutInfo.Tab = tab;
                }
                else
                {
                    tab.Text = wd.Header;
                    tab.Icon = wd.Icon;
                    layoutInfo.Content = content.transform;
                    layoutInfo.Tab = tab;
                    layoutInfo.CanDrag = persistentLayoutInfo.CanDrag;
                    layoutInfo.CanClose = persistentLayoutInfo.CanClose;
                    layoutInfo.CanMaximize = persistentLayoutInfo.CanMaximize;
                    layoutInfo.IsHeaderVisible = persistentLayoutInfo.IsHeaderVisible;
                    layoutInfo.IsOn = persistentLayoutInfo.IsOn;
                }
            }
            else
            {
                if (persistentLayoutInfo.TabGroup != null)
                {
                    layoutInfo.TabGroup = new LayoutInfo[persistentLayoutInfo.TabGroup.Length];
                    for (int i = 0; i < persistentLayoutInfo.TabGroup.Length; ++i)
                    {
                        LayoutInfo tabLayoutInfo = new LayoutInfo();
                        ToLayout(persistentLayoutInfo.TabGroup[i], tabLayoutInfo);
                        layoutInfo.TabGroup[i] = tabLayoutInfo;
                    }
                }
                else
                {
                    layoutInfo.IsVertical = persistentLayoutInfo.IsVertical;
                    if (persistentLayoutInfo.Child0 != null && persistentLayoutInfo.Child0 != null)
                    {
                        layoutInfo.Child0 = new LayoutInfo();
                        layoutInfo.Child1 = new LayoutInfo();
                        layoutInfo.Ratio = persistentLayoutInfo.Ratio;

                        ToLayout(persistentLayoutInfo.Child0, layoutInfo.Child0);
                        ToLayout(persistentLayoutInfo.Child1, layoutInfo.Child1);
                    }
                }
            }
        }

        public void DeleteLayout(string name)
        {
            PlayerPrefs.DeleteKey("Battlehub.RTEditor.Layout" + name);
        }

        public void ForceLayoutUpdate()
        {
            if(m_lockUpdateLayout)
            {
                return;
            }

            DockPanel.ForceUpdateLayout();
        }

        private void Subscribe()
        {
            if(m_dialogManager != null)
            {
                m_dialogManager.DialogDestroyed += OnDialogDestroyed;
            }
            
            if(m_dockPanel != null)
            {
                m_dockPanel.TabActivated += OnTabActivated;
                m_dockPanel.TabDeactivated += OnTabDeactivated;
                m_dockPanel.TabClosed += OnTabClosed;

                m_dockPanel.RegionBeforeDepthChanged += OnRegionBeforeDepthChanged;
                m_dockPanel.RegionDepthChanged += OnRegionDepthChanged;
                m_dockPanel.RegionSelected += OnRegionSelected;
                m_dockPanel.RegionUnselected += OnRegionUnselected;
                m_dockPanel.RegionEnabled += OnRegionEnabled;
                m_dockPanel.RegionDisabled += OnRegionDisabled;
                m_dockPanel.RegionMaximized += OnRegionMaximized;
                m_dockPanel.RegionBeforeBeginDrag += OnRegionBeforeBeginDrag;
                m_dockPanel.RegionBeginResize += OnBeginResize;
                m_dockPanel.RegionEndResize += OnRegionEndResize;
            }
        }

        private void Unsubscribe()
        {
            if (m_dockPanel != null)
            {
                m_dockPanel.TabActivated -= OnTabActivated;
                m_dockPanel.TabDeactivated -= OnTabDeactivated;
                m_dockPanel.TabClosed -= OnTabClosed;

                m_dockPanel.RegionBeforeDepthChanged -= OnRegionBeforeDepthChanged;
                m_dockPanel.RegionDepthChanged -= OnRegionDepthChanged;
                m_dockPanel.RegionSelected -= OnRegionSelected;
                m_dockPanel.RegionUnselected -= OnRegionUnselected;
                m_dockPanel.RegionEnabled -= OnRegionEnabled;
                m_dockPanel.RegionDisabled -= OnRegionDisabled;
                m_dockPanel.RegionMaximized -= OnRegionMaximized;
                m_dockPanel.RegionBeforeBeginDrag -= OnRegionBeforeBeginDrag;
                m_dockPanel.RegionBeginResize -= OnBeginResize;
                m_dockPanel.RegionEndResize -= OnRegionEndResize;
            }

            if (m_dialogManager != null)
            {
                m_dialogManager.DialogDestroyed -= OnDialogDestroyed;
            }
        }


        private void OnDialogDestroyed(Dialog dialog)
        {
            RuntimeWindow dialogWindow = dialog.Content.GetComponentInParent<RuntimeWindow>();

            OnContentDestroyed(dialog.Content);

            if (!m_dialogManager.IsDialogOpened)
            {
                Transform pointerOverWindow = dialog.Content != null ? FindPointerOverWindow(dialogWindow) : null;
                if (pointerOverWindow != null)
                {
                    RuntimeWindow window = pointerOverWindow.GetComponentInChildren<RuntimeWindow>();
                    if (window == null)
                    {
                        window = m_editor.GetWindow(RuntimeWindowType.Scene);
                    }
                    window.IsPointerOver = true;
                    m_editor.ActivateWindow(window);
                }
                else
                {
                    RuntimeWindow window = m_editor.GetWindow(RuntimeWindowType.Scene);
                    m_editor.ActivateWindow(window);
                }

                if(DeferUpdate != null)
                {
                    DeferUpdate();
                }
            }
        }

        private void OnRegionSelected(Region region)
        {
        }

        private void OnRegionUnselected(Region region)
        {

        }

        private void OnBeginResize(Resizer resizer, Region region)
        {

        }

        private void OnRegionEndResize(Resizer resizer, Region region)
        {
            if (DeferUpdate != null)
            {
                DeferUpdate();
            }
        }

        private void OnTabActivated(Region region, Transform content)
        {
            List<Transform> extraComponents;
            if (m_extraComponents.TryGetValue(content, out extraComponents))
            {
                for (int i = 0; i < extraComponents.Count; ++i)
                {
                    Transform extraComponent = extraComponents[i];
                    extraComponent.gameObject.SetActive(true);
                }
            }

            RuntimeWindow window = region.ContentPanel.GetComponentInChildren<RuntimeWindow>();
            if (window != null)
            {
                window.Editor.ActivateWindow(window);
            }
        }

        private void OnTabDeactivated(Region region, Transform content)
        {
            List<Transform> extraComponents;
            if (m_extraComponents.TryGetValue(content, out extraComponents))
            {
                for (int i = 0; i < extraComponents.Count; ++i)
                {
                    Transform extraComponent = extraComponents[i];
                    if (extraComponent)
                    {
                        extraComponent.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnTabClosed(Region region, Transform content)
        {
            OnContentDestroyed(content);
        }

        private void OnRegionDisabled(Region region)
        {
            if (region.ActiveContent != null)
            {
                List<Transform> extraComponents;
                if (m_extraComponents.TryGetValue(region.ActiveContent, out extraComponents))
                {
                    for (int i = 0; i < extraComponents.Count; ++i)
                    {
                        Transform extraComponent = extraComponents[i];
                        if (extraComponent)
                        {
                            extraComponent.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void OnRegionEnabled(Region region)
        {
            if (region.ActiveContent != null)
            {
                List<Transform> extraComponents;
                if (m_extraComponents.TryGetValue(region.ActiveContent, out extraComponents))
                {
                    for (int i = 0; i < extraComponents.Count; ++i)
                    {
                        Transform extraComponent = extraComponents[i];
                        extraComponent.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void OnRegionMaximized(Region region, bool maximized)
        {
            if (!maximized)
            {
                RuntimeWindow[] windows = m_dockPanel.RootRegion.GetComponentsInChildren<RuntimeWindow>();
                for (int i = 0; i < windows.Length; ++i)
                {
                    windows[i].HandleResize();
                }
            }
        }

        private void OnContentDestroyed(Transform content)
        {
            string windowTypeName = m_windows.Where(kvp => kvp.Value.Contains(content)).Select(kvp => kvp.Key).FirstOrDefault();
            if (!string.IsNullOrEmpty(windowTypeName))
            {
                HashSet<Transform> windowsOfType = m_windows[windowTypeName];
                windowsOfType.Remove(content);

                if (windowsOfType.Count == 0)
                {
                    m_windows.Remove(windowTypeName);
                }

                List<Transform> extraComponents = new List<Transform>();
                if (m_extraComponents.TryGetValue(content, out extraComponents))
                {
                    for (int i = 0; i < extraComponents.Count; ++i)
                    {
                        Destroy(extraComponents[i].gameObject);
                    }
                }

                bool isDialog;
                WindowDescriptor wd = m_windowManager.GetWindowDescriptor(windowTypeName, out isDialog);
                if (wd != null)
                {
                    wd.Created--;
                    Debug.Assert(wd.Created >= 0);

                    if (WindowDestroyed != null)
                    {
                        WindowDestroyed(content);
                    }
                }
            }

            RenderTextureCamera[] rtc = FindObjectsOfType<RenderTextureCamera>();
            for (int i = 0; i < rtc.Length; ++i)
            {
                if (rtc[i] != null)
                {
                    rtc[i].TryResizeRenderTexture();
                }
            }

            ForceLayoutUpdate();
            if(m_coForceUpdateLayout == null && !m_lockUpdateLayout)
            {
                m_coForceUpdateLayout = CoForceUpdateLayout();
                StartCoroutine(m_coForceUpdateLayout);
            }
        }

        private IEnumerator m_coForceUpdateLayout;
        private IEnumerator CoForceUpdateLayout()
        {
            yield return new WaitForEndOfFrame();
            m_coForceUpdateLayout = null;
            ForceLayoutUpdate();
        }

        private void CancelIfRegionIsNotActive(Region region, CancelArgs arg)
        {
            if (m_editor.ActiveWindow == null)
            {
                return;
            }

            Region activeRegion = m_editor.ActiveWindow.GetComponentInParent<Region>();
            if (activeRegion == null)
            {
                return;
            }

            if (!region.IsModal() && activeRegion.GetDragRegion() != region.GetDragRegion())
            {
                arg.Cancel = true;
            }
        }

        private void OnRegionBeforeBeginDrag(Region region, CancelArgs arg)
        {
            CancelIfRegionIsNotActive(region, arg);
        }

        private void OnRegionBeforeDepthChanged(Region region, CancelArgs arg)
        {
            CancelIfRegionIsNotActive(region, arg);
        }

        private void OnRegionDepthChanged(Region region, int depth)
        {
            RuntimeWindow[] windows = region.GetComponentsInChildren<RuntimeWindow>(true);
            for (int i = 0; i < windows.Length; ++i)
            {
                RuntimeWindow window = windows[i];
                if(window is RuntimeCameraWindow)
                {
                    RuntimeCameraWindow cameraWindow = (RuntimeCameraWindow)window;
                    cameraWindow.SetCameraDepth(10 + depth * 5);
                }
                
                window.Depth = (region.IsModal() ? 2048 + depth : depth) * 5;
                if (window.GetComponentsInChildren<RuntimeWindow>().Length > 1)
                {
                    window.Depth -= 1;
                }
            }
        }
    }


}
