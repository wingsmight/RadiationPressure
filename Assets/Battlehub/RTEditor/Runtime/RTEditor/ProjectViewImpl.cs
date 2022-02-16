using UnityEngine;
using Battlehub.RTCommon;
using System.Linq;
using Battlehub.RTSL.Interface;
using Battlehub.UIControls.DockPanels;
using System.Collections;
using TMPro;
using Battlehub.UIControls;

namespace Battlehub.RTEditor
{
    public class ProjectViewImpl : MonoBehaviour
    {
        private IProject m_project;
        protected IProject Project
        {
            get { return m_project; }
        }

        private IRuntimeEditor m_editor;
        protected IRuntimeEditor Editor
        {
            get { return m_editor; }
        }

        private ProjectView m_projectView;
        protected RuntimeWindow Window
        {
            get { return m_projectView; }
        }

        private ProjectTreeViewImpl m_projectTree;
        protected ProjectTreeViewImpl ProjectTree
        {
            get { return m_projectTree; }
        }

        private ProjectFolderViewImpl m_projectFolder;
        protected ProjectFolderViewImpl ProjectFolder
        {
            get { return m_projectFolder; }
        }

        private TMP_InputField m_filterInput;
        protected TMP_InputField FilterInput
        {
            get { return m_filterInput; }
        }

        private IWindowManager m_windowManager;
        private ILocalization m_localization;
        private IResourcePreviewUtility m_resourcePreview;

        private string m_filter = string.Empty;
        private bool m_tryToChangeSelectedFolder;
        private float m_applyFilterTime;

        protected virtual void Start()
        {
            m_projectView = GetComponent<ProjectView>();
            m_projectTree = m_projectView.ProjectTree.GetComponent<ProjectTreeViewImpl>();
            m_projectFolder = m_projectView.ProjectFolder.GetComponent<ProjectFolderViewImpl>();
            m_filterInput = m_projectView.FilterInput;

            m_editor = IOC.Resolve<IRuntimeEditor>();
            m_windowManager = IOC.Resolve<IWindowManager>();
            m_localization = IOC.Resolve<ILocalization>();
            m_project = IOC.Resolve<IProject>();
            if (m_project == null)
            {
                Debug.LogWarning("RTSLDeps.Get.Project is null");
                Destroy(gameObject);
                return;
            }

            m_resourcePreview = IOC.Resolve<IResourcePreviewUtility>();
            if(m_resourcePreview == null)
            {
                Debug.LogWarning("RTEDeps.Get.ResourcePreview is null");
            }

            DockPanel dockPanelsRoot = GetComponent<DockPanel>();
            if (dockPanelsRoot != null)
            {
                dockPanelsRoot.CursorHelper = Editor.CursorHelper;
            }

            UnityEventHelper.AddListener(m_filterInput, inputField => inputField.onValueChanged, OnFiltering);

            m_projectFolder.ItemDoubleClick += OnProjectFolderItemDoubleClick;
            m_projectFolder.ItemRenamed += OnProjectFolderItemRenamed;
            m_projectFolder.ItemsDeleted += OnProjectFolderItemDeleted;
            m_projectFolder.SelectionChanged += OnProjectFolderSelectionChanged;

            m_projectTree.SelectionChanged += OnProjectTreeSelectionChanged;
            m_projectTree.ItemRenamed += OnProjectTreeItemRenamed;
            m_projectTree.ItemsDeleted += OnProjectTreeItemDeleted;

            m_project.OpenProjectCompleted += OnProjectOpenCompleted;
            m_project.CloseProjectCompleted += OnCloseProjectCompleted;
            m_project.ImportCompleted += OnImportCompleted;
            m_project.BeforeDeleteCompleted += OnBeforeDeleteCompleted;
            m_project.DeleteCompleted += OnDeleteCompleted;
            m_project.RenameCompleted += OnRenameCompleted;
            m_project.CreateCompleted += OnCreateCompleted;
            m_project.MoveCompleted += OnMoveCompleted;
            m_project.SaveCompleted += OnSaveCompleted;
            m_project.DuplicateItemsCompleted += OnDuplicateCompleted;
         
            if (m_project.IsOpened)
            {
                m_projectTree.LoadProject(m_project.Root);
                m_projectTree.SelectedItem = m_project.Root;
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_projectFolder != null)
            {
                m_projectFolder.ItemDoubleClick -= OnProjectFolderItemDoubleClick;
                m_projectFolder.ItemRenamed -= OnProjectFolderItemRenamed;
                m_projectFolder.ItemsDeleted -= OnProjectFolderItemDeleted;
                m_projectFolder.SelectionChanged -= OnProjectFolderSelectionChanged;
            }

            if(m_projectTree != null)
            {
                m_projectTree.SelectionChanged -= OnProjectTreeSelectionChanged;
                m_projectTree.ItemsDeleted -= OnProjectTreeItemDeleted;
                m_projectTree.ItemRenamed -= OnProjectTreeItemRenamed;
            }

            if (m_project != null)
            {
                m_project.OpenProjectCompleted -= OnProjectOpenCompleted;
                m_project.ImportCompleted -= OnImportCompleted;
                m_project.BeforeDeleteCompleted -= OnBeforeDeleteCompleted;
                m_project.DeleteCompleted -= OnDeleteCompleted;
                m_project.RenameCompleted -= OnRenameCompleted;
                m_project.CreateCompleted -= OnCreateCompleted;
                m_project.MoveCompleted -= OnMoveCompleted;
                m_project.SaveCompleted -= OnSaveCompleted;
                m_project.DuplicateItemsCompleted -= OnDuplicateCompleted;
            }

            UnityEventHelper.RemoveListener(m_filterInput, inputField => inputField.onValueChanged, OnFiltering);
        }

        protected virtual void Update()
        {
            if (Time.time > m_applyFilterTime)
            {
                m_applyFilterTime = float.PositiveInfinity;
                ApplyFilter();
            }
        }

        private void OnProjectOpenCompleted(Error error, ProjectInfo projectInfo)
        {
            Editor.IsBusy = false;
            if (error.HasError)
            {
                m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectView_CantOpenProject", "Can't open project"), error.ToString());
                return;
            }

            m_projectTree.LoadProject(m_project.Root);
            m_projectTree.SelectedItem = null;
            m_projectTree.SelectedItem = m_project.Root;
        }

        private void OnCloseProjectCompleted(Error error)
        {
            if (error.HasError)
            {
                m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectView_CantCloseProject", "Can't close project"), error.ToString());
                return;
            }

            m_projectTree.LoadProject(null);
            m_projectTree.SelectedItem = null;
            m_projectFolder.SetItems(null, null, true);
        }

        private void OnImportCompleted(Error error, AssetItem[] result)
        {
            if (error.HasError)
            {
                m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectView_UnableToImportAssets", "Unable to import assets") , error.ErrorText);
            }

            string path = string.Empty;
            if (m_projectTree.SelectedItem != null)
            {
                path = m_projectTree.SelectedItem.ToString();
            }

            m_projectTree.LoadProject(m_project.Root);

            if (!string.IsNullOrEmpty(path))
            {
                if (m_projectTree.SelectedItem == m_project.Root)
                {
                    m_projectTree.SelectedItem = null;
                }

                m_projectTree.SelectedItem = m_project.Root.Get(path);
            }
            else
            {
                m_projectTree.SelectedItem = m_project.Root;
            }
        }

        private void OnBeforeDeleteCompleted(Error error, ProjectItem[] result)
        {
            Editor.IsBusy = false;
            if (error.HasError)
            {
                m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectView_UnableToRemove", "Unable to remove"), error.ErrorText);
            }  
        }

        private void OnDeleteCompleted(Error error, ProjectItem[] result)
        {
            m_projectTree.RemoveProjectItemsFromTree(result);
            m_projectTree.SelectRootIfNothingSelected();

            m_projectFolder.OnDeleted(result.OfType<AssetItem>().ToArray());

            if(Editor.Selection.activeObject != null)
            {
                object selectedObjectId = m_project.ToPersistentID(Editor.Selection.activeObject);
                if(result.Any(r => m_project.ToPersistentID(r).Equals(selectedObjectId)))
                {
                    bool wasEnabled = Editor.Undo.Enabled;
                    Editor.Undo.Enabled = false;
                    Editor.Selection.activeObject = null;
                    Editor.Undo.Enabled = wasEnabled;
                }
            }
        }

        private void OnRenameCompleted(Error error, ProjectItem result)
        {
            Editor.IsBusy = false;
            if (error.HasError)
            {
                m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectView_UnableToRenameAsset", "Unable to rename asset"), error.ToString());
            }
        }

        private void OnCreateCompleted(Error error, ProjectItem[] result)
        {
            Editor.IsBusy = false;
            if (error.HasError)
            {
                m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectView_UnableToCreateFolder", "Unable to create folder"), error.ToString());
                return;
            }

            foreach(ProjectItem item in result)
            {
                m_projectTree.AddItem(item.Parent, item);
            }
            m_projectTree.SelectedItem = result.Last();
        }

        private void OnMoveCompleted(Error error, ProjectItem[] projectItems, ProjectItem[] oldParents)
        {
            if (error.HasError)
            {
                m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectView_UnableToMoveAssets", "Unable to move assets"), error.ErrorText);
                return;
            }

            m_projectFolder.Remove(projectItems);

            for (int i = 0; i < projectItems.Length; ++i)
            {
                ProjectItem projectItem = projectItems[i];
                ProjectItem oldParent = oldParents[i];
                if (!(projectItem is AssetItem))
                {
                    m_projectTree.ChangeParent(projectItem, oldParent);
                }
            }
        }

        private void OnSaveCompleted(Error createPrefabError, AssetItem[] result, bool userAction)
        {
            Editor.IsBusy = false;
            m_projectFolder.InsertItems(result, userAction);
        }

        private void OnDuplicateCompleted(Error error, ProjectItem[] result)
        {
            Editor.IsBusy = false;
            if(!error.HasError)
            {
                m_projectFolder.InsertItems(result, true);
                foreach (ProjectItem item in result.Where(item => item.IsFolder))
                {
                    m_projectTree.AddItem(item.Parent, item, false, false);
                }

                m_projectTree.SelectedItem = result.FirstOrDefault();
            }
        }

        private void OnProjectTreeSelectionChanged(object sender, SelectionChangedArgs<ProjectItem> e)
        {
            if (m_filterInput != null)
            {
                m_filterInput.SetTextWithoutNotify(string.Empty);
            }
            m_filter = string.Empty;
            m_tryToChangeSelectedFolder = false;
            ApplyFilter();
        }

        private void DataBind(ProjectItem[] projectItems, string searchPattern)
        {
            m_project.GetAssetItems(projectItems, searchPattern, (error, assets) =>
            {
                if (error.HasError)
                {
                    m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectView_CantGetAssets", "Can't get assets"), error.ToString());
                    return;
                }

                StartCoroutine(ProjectItemView.CoCreatePreviews(assets, m_project, m_resourcePreview));
                StartCoroutine(CoSetItems(projectItems, assets));
            });
        }

        private void SetItems(ProjectItem[] projectItems, ProjectItem[] assets)
        {
            Editor.Selection.Enabled = false;
            m_projectFolder.SetItems(projectItems.ToArray(), assets, true);
            Editor.Selection.Enabled = true;
        }

        private IEnumerator CoSetItems(ProjectItem[] projectItems, ProjectItem[] assets)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            SetItems(projectItems, assets);
        }

        private void OnProjectFolderItemDeleted(object sender, ProjectTreeEventArgs e)
        {
            IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
            editor.DeleteAssets(e.ProjectItems);
        }

        private void OnProjectFolderItemDoubleClick(object sender, ProjectTreeEventArgs e)
        {
            if(e.ProjectItem == null)
            {
                return;
            }

            if(e.ProjectItem.IsFolder)
            {
                m_projectTree.SelectedItem = e.ProjectItem;
            }
            else
            {
                if(m_project.IsScene(e.ProjectItem))
                {
                    Editor.IsPlaying = false;
                    Editor.IsBusy = true;
                    m_project.Load(new[] { (AssetItem)e.ProjectItem }, (error, obj) =>
                    {
                        Editor.IsBusy = false;
                        if(error.HasError)
                        {
                            m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectView_UnableToLoadScene", "Unable to load scene") + " " + e.ProjectItem.ToString(), error.ToString());
                        }
                    });
                }
            }
        }

        private void OnProjectFolderItemRenamed(object sender, ProjectTreeRenamedEventArgs e)
        {
            m_projectTree.UpdateProjectItem(e.ProjectItem);            
            Editor.IsBusy = true;
            m_project.Rename(e.ProjectItem, e.OldName);
        }

        private void OnProjectFolderSelectionChanged(object sender, ProjectTreeEventArgs e)
        {
            if(m_projectFolder.SelectedItems == null)
            {
                Editor.Selection.activeObject = null;
            }
            else
            {
                AssetItem[] assetItems = m_projectFolder.SelectedItems.OfType<AssetItem>().Where(o => !m_project.IsScene(o)).ToArray();
                if(assetItems.Length == 0)
                {
                    Editor.Selection.activeObject = null;
                }
                else
                {
                    Editor.IsBusy = true;
                    m_project.Load(assetItems, (error, objects) =>
                    {
                        Editor.IsBusy = false;
                        m_projectFolder.HandleEditorSelectionChange = false;
                        Editor.Selection.objects = objects;
                        m_projectFolder.HandleEditorSelectionChange = true;
                    });
                }
            }
        }

        private void OnProjectTreeItemRenamed(object sender, ProjectTreeRenamedEventArgs e)
        {
            Editor.IsBusy = true;
            m_project.Rename(e.ProjectItem, e.OldName);
        }

        private void OnProjectTreeItemDeleted(object sender, ProjectTreeEventArgs e)
        {
            IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
            editor.DeleteAssets(e.ProjectItems);
        }

        private void OnFiltering(string value)
        {
            m_tryToChangeSelectedFolder = !string.IsNullOrWhiteSpace(m_filter) && string.IsNullOrWhiteSpace(value);
            m_filter = value;
            m_applyFilterTime = Time.time + 0.3f;
        }

        private void ApplyFilter()
        {
            if (!string.IsNullOrWhiteSpace(m_filter))
            {
                DataBind(m_project.Root.Flatten(false, true), m_filter);
            }
            else
            {
                if(m_tryToChangeSelectedFolder)
                {
                    ProjectItem selectedFolder = m_projectTree.SelectedItem;
                    ProjectItem[] selectedItems = m_projectFolder.SelectedItems;
                    if (selectedFolder != null && selectedItems != null && selectedItems.Length > 0)
                    {
                        if (!selectedItems.Any(item => item.Parent == selectedFolder))
                        {
                            m_projectTree.SelectedItem = selectedItems[0].Parent;
                            return;
                        }
                    }
                }

                DataBind(m_projectTree.SelectedFolders, m_filter);
            }
        }
    }
}
