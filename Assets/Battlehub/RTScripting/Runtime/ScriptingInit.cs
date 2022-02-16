#if UNITY_STANDALONE
using Battlehub.CodeAnalysis;
using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.RTSL;
using Battlehub.RTSL.Interface;
using Battlehub.UIControls;
using Battlehub.UIControls.MenuControl;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
#else
using UnityEngine;
using Battlehub.RTEditor;
#endif

namespace Battlehub.RTScripting
{
    [DefaultExecutionOrder(-1)]
    public class ScriptingInit : EditorExtension
    {
#pragma warning disable CS0414
        [SerializeField]
        private GameObject m_editRuntimeScriptDialog = null;

        [SerializeField]
        private ComponentEditor m_runtimeScriptEditor = null;
#pragma warning restore CS0414

#if UNITY_STANDALONE

        private IWindowManager m_wm;
        private IProjectFolder m_projectFolder;
        private IRuntimeScriptManager m_scriptManager;
        private ICompiler m_compiler;
        private IRTE m_editor;
        private const string Ext = ".cs";

        protected override void OnEditorExist()
        {
            base.OnEditorExist();

            m_editor = IOC.Resolve<IRTE>();

            m_compiler = new Complier();
            IOC.RegisterFallback(m_compiler);

            if (m_scriptManager == null)
            {
                m_scriptManager = gameObject.AddComponent<RuntimeScriptsManager>();
                m_scriptManager.AddReference(typeof(IRTE).Assembly.Location);
                m_scriptManager.AddReference(typeof(ILocalization).Assembly.Location);
                m_scriptManager.AddReference(typeof(IRuntimeEditor).Assembly.Location);
                m_scriptManager.AddReference(typeof(VirtualizingTreeView).Assembly.Location);
                m_scriptManager.AddReference(typeof(RTSLVersion).Assembly.Location);
                m_scriptManager.AddReference(typeof(TextMeshProUGUI).Assembly.Location);
            }

            Subscribe();

            IRTEAppearance appearance = IOC.Resolve<IRTEAppearance>();
            List<AssetIcon> icons = appearance.AssetIcons.ToList();
            icons.Add(new AssetIcon { AssetTypeName = typeof(RuntimeTextAsset).FullName + Ext, Icon = Resources.Load<Sprite>("RTE_Script") });
            appearance.AssetIcons = icons.ToArray();
            Register();
        }

        private void Register()
        {
            ILocalization lc = IOC.Resolve<ILocalization>();
            lc.LoadStringResources("RTScripting.StringResources");

            IWindowManager wm = IOC.Resolve<IWindowManager>();
            if (m_editRuntimeScriptDialog != null)
            {
                RegisterWindow(wm, "EditRuntimeScript", lc.GetString("ID_RTScripting_WM_Header_EditScript", "Edit Script"),
                    Resources.Load<Sprite>("RTE_Script"), m_editRuntimeScriptDialog, true);

                IRTEAppearance appearance = IOC.Resolve<IRTEAppearance>();
                appearance.ApplyColors(m_editRuntimeScriptDialog);
            }

            if (m_runtimeScriptEditor != null)
            {
                IEditorsMap map = IOC.Resolve<IEditorsMap>();
                map.RegisterEditor(m_runtimeScriptEditor);
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

        protected override void OnEditorClosed()
        {
            base.OnEditorClosed();
            IOC.UnregisterFallback(m_compiler);
            DestroyScriptManager();
            Unsubscribe();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            IOC.UnregisterFallback(m_compiler);
            DestroyScriptManager();
            Unsubscribe();
        }

        private void DestroyScriptManager()
        {
            if (m_scriptManager != null)
            {
                Destroy(m_scriptManager as MonoBehaviour);
                m_scriptManager = null;
            }
        }


        private void Subscribe()
        {
            m_wm = IOC.Resolve<IWindowManager>();
            m_wm.WindowCreated += OnWindowCreated;
            m_wm.AfterLayout += OnAfterLayout;
            m_wm.WindowDestroyed += OnWindowDestroyed;

            m_scriptManager.Loading += OnScriptManagerLoading;
            m_scriptManager.Loaded += OnScriptManagerLoaded;
            m_scriptManager.Compiling += OnScriptManagerCompiling;
            m_scriptManager.Complied += OnScriptManagerCompiled;

            StartCoroutine(m_coUpdateContextMenuHandler = CoUpdateContextMenuHandler());
        }


        private void Unsubscribe()
        {
            if (m_wm != null)
            {
                m_wm.WindowCreated -= OnWindowCreated;
                m_wm.AfterLayout -= OnAfterLayout;
                m_wm.WindowDestroyed -= OnWindowDestroyed;

                m_wm = null;
            }

            if (m_scriptManager != null)
            {
                m_scriptManager.Loading -= OnScriptManagerLoading;
                m_scriptManager.Loaded -= OnScriptManagerLoaded;
                m_scriptManager.Compiling -= OnScriptManagerCompiling;
                m_scriptManager.Complied -= OnScriptManagerCompiled;
            }

            if (m_projectFolder != null)
            {
                m_projectFolder.ItemOpen -= OnProjectFolderItemOpen;
                m_projectFolder.ValidateContextMenuOpenCommand -= OnProjectFolderValidateContextMenuOpenCommand;
                m_projectFolder.ContextMenu -= OnProjectFolderContextMenu;
                m_projectFolder = null;
            }

            if (m_coUpdateContextMenuHandler != null)
            {
                StopCoroutine(m_coUpdateContextMenuHandler);
                m_coUpdateContextMenuHandler = null;
            }
        }

        private void OnWindowCreated(Transform windowTransform)
        {
            RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
            if (window != null && window.WindowType == RuntimeWindowType.Project)
            {
                StartCoroutine(m_coUpdateContextMenuHandler = CoUpdateContextMenuHandler());
            }
        }
        private void OnAfterLayout(IWindowManager obj)
        {
            StartCoroutine(m_coUpdateContextMenuHandler = CoUpdateContextMenuHandler());
        }

        private void OnWindowDestroyed(Transform windowTransform)
        {
            if (m_coUpdateContextMenuHandler != null)
            {
                StopCoroutine(m_coUpdateContextMenuHandler);
                m_coUpdateContextMenuHandler = null;
            }
            UpdateContextMenuHandler();
        }

        private IEnumerator m_coUpdateContextMenuHandler;
        private IEnumerator CoUpdateContextMenuHandler()
        {
            //WindowInitialized event must be added to IWindowManager to replace this CoRoutine;
            yield return new WaitForEndOfFrame();
            UpdateContextMenuHandler();
        }

        private void UpdateContextMenuHandler()
        {
            IProjectFolder projectFolder = IOC.Resolve<IProjectFolder>();
            if (m_projectFolder != projectFolder)
            {
                if (m_projectFolder != null)
                {
                    m_projectFolder.ItemOpen -= OnProjectFolderItemOpen;
                    m_projectFolder.ValidateContextMenuOpenCommand -= OnProjectFolderValidateContextMenuOpenCommand;
                    m_projectFolder.ContextMenu -= OnProjectFolderContextMenu;
                }

                m_projectFolder = projectFolder;

                if (m_projectFolder != null)
                {
                    m_projectFolder.ItemOpen += OnProjectFolderItemOpen;
                    m_projectFolder.ValidateContextMenuOpenCommand += OnProjectFolderValidateContextMenuOpenCommand;
                    m_projectFolder.ContextMenu += OnProjectFolderContextMenu;
                }
            }
        }

        private void OnProjectFolderValidateContextMenuOpenCommand(object sender, ProjectTreeCancelEventArgs e)
        {
            if (e.ProjectItem is AssetItem)
            {
                AssetItem assetItem = (AssetItem)e.ProjectItem;
                ITypeMap typeMap = IOC.Resolve<ITypeMap>();
                if (typeMap.ToType(assetItem.TypeGuid) == typeof(RuntimeTextAsset) && e.ProjectItem.Ext == Ext)
                {
                    e.Cancel = false;
                }
            }
        }

        private void OnProjectFolderItemOpen(object sender, ProjectTreeEventArgs e)
        {
            if (e.ProjectItem is AssetItem)
            {
                AssetItem assetItem = (AssetItem)e.ProjectItem;
                ITypeMap typeMap = IOC.Resolve<ITypeMap>();
                if (typeMap.ToType(assetItem.TypeGuid) == typeof(RuntimeTextAsset) && e.ProjectItem.Ext == Ext)
                {
                    IWindowManager wm = IOC.Resolve<IWindowManager>();
                    wm.CreateDialogWindow("EditRuntimeScript", "Edit " + assetItem.Name, (s, okArgs) => { });
                    IEditRuntimeScriptDialog dialog = IOC.Resolve<IEditRuntimeScriptDialog>();
                    dialog.AssetItem = assetItem;
                }
            }
        }

        private void OnProjectFolderContextMenu(object sender, ProjectTreeContextMenuEventArgs e)
        {
            ILocalization lc = IOC.Resolve<ILocalization>();
            MenuItemInfo createAsset = new MenuItemInfo
            {
                Path = string.Format("{0}/{1}",
                        lc.GetString("ID_RTScripting_ProjectFolderView_Create", "Create"),
                        lc.GetString("ID_RTScripting_ProjectFolderView_Script", "Script"))
            };
            createAsset.Action = new MenuItemEvent();
            createAsset.Action.AddListener(arg =>
            {
                m_scriptManager.CreateScript(e.ProjectItem);
            });

            createAsset.Validate = new MenuItemValidationEvent();
            createAsset.Validate.AddListener(arg => arg.IsValid = e.ProjectItem == null || e.ProjectItem.IsFolder);
            e.MenuItems.Add(createAsset);
        }

        private void OnScriptManagerLoading()
        {
            m_editor.IsBusy = true;
        }

        private void OnScriptManagerLoaded()
        {
            m_editor.IsBusy = false;
        }

        private void OnScriptManagerCompiling()
        {
            m_editor.IsBusy = true;
        }

        private void OnScriptManagerCompiled(bool success)
        {
            m_editor.IsBusy = false;
        }
#endif
    }
}

