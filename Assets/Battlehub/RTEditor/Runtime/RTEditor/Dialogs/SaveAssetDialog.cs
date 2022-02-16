using UnityEngine;
using UnityEngine.UI;
using System.Linq;

using Battlehub.UIControls;
using Battlehub.RTCommon;
using Battlehub.UIControls.Dialogs;
using Battlehub.RTSL.Interface;

using TMPro;

namespace Battlehub.RTEditor
{
    public interface ISaveAssetDialog
    {
        Sprite AssetIcon
        {
            get;
            set;
        }

        Object Asset
        {
            get;
            set;
        }

        bool SelectSavedAssets
        {
            get;
            set;
        }

   
        event System.Action<ISaveAssetDialog, Object> SaveCompleted;
    }

    public class SaveAssetDialog : RuntimeWindow, ISaveAssetDialog
    {
        public event System.Action<ISaveAssetDialog, Object> SaveCompleted;

        [SerializeField]
        private TMP_InputField Input = null;
        [SerializeField]
        private Sprite FolderIcon = null;
        [SerializeField]
        private Sprite DefaultAssetIcon = null;
    
        private Dialog m_parentDialog;
        private VirtualizingTreeView m_treeView = null;
        private IProject m_project;
        private IWindowManager m_windowManager;
        private ILocalization m_localization;

        public Sprite AssetIcon
        {
            get;
            set;
        }

        private Object m_asset;
        public Object Asset
        {
            get { return m_asset; }
            set
            {
                if(m_asset != value)
                {
                    m_asset = value;
                    if(Input != null && m_asset != null)
                    {
                        Input.text = m_asset.name;
                    }
                }
            }
        }

        public bool SelectSavedAssets
        {
            get;
            set;
        }   

        protected override void AwakeOverride()
        {
            IOC.RegisterFallback<ISaveAssetDialog>(this);
            WindowType = RuntimeWindowType.SaveAsset;
            base.AwakeOverride();

            m_localization = IOC.Resolve<ILocalization>();
        }
        private void Start()
        {
            m_parentDialog = GetComponentInParent<Dialog>();
            m_parentDialog.Ok += OnOk;
            m_parentDialog.OkText = m_localization.GetString("ID_RTEditor_SaveAssetsDialog_Save", "Save");
            m_parentDialog.IsOkVisible = true;
            m_parentDialog.CancelText = m_localization.GetString("ID_RTEditor_SaveAssetsDialog_Cancel", "Cancel");
            m_parentDialog.IsCancelVisible = true;

            m_treeView = GetComponentInChildren<VirtualizingTreeView>();
            m_windowManager = IOC.Resolve<IWindowManager>();

            m_treeView.ItemDataBinding += OnItemDataBinding;
            m_treeView.ItemExpanding += OnItemExpanding;
            m_treeView.SelectionChanged += OnSelectionChanged;
            m_treeView.ItemDoubleClick += OnItemDoubleClick;
            m_treeView.CanDrag = false;
            m_treeView.CanEdit = false;
            m_treeView.CanUnselectAll = false;
            m_treeView.CanRemove = false;

            m_project = IOC.Resolve<IProject>();
            if (m_project == null)
            {
                Debug.LogError("ProjectManager.Instance is null");
                return;
            }

            m_treeView.Items = new[] { m_project.Root };
            m_treeView.SelectedItem = m_project.Root;
            m_treeView.Expand(m_project.Root);
                      
            Input.ActivateInputField();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StartCoroutine(CoActivateInputField());
        }

        private System.Collections.IEnumerator CoActivateInputField()
        {
            yield return new WaitForEndOfFrame();
            if (Input != null)
            {
                Input.ActivateInputField();
            }
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
          
            if(m_parentDialog != null)
            {
                m_parentDialog.Ok -= OnOk; 
            }

            if (m_treeView != null)
            {
                m_treeView.ItemDataBinding -= OnItemDataBinding;
                m_treeView.ItemExpanding -= OnItemExpanding;
                m_treeView.SelectionChanged -= OnSelectionChanged;
                m_treeView.ItemDoubleClick -= OnItemDoubleClick;
            }

            IOC.UnregisterFallback<ISaveAssetDialog>(this);
        }

        private void OnOk(Dialog dialog, DialogCancelArgs args)
        {
            if (m_treeView.SelectedItem == null)
            {
                args.Cancel = true;
                return;
            }

            if(Editor.IsPlaying)
            {
                m_windowManager.MessageBox(
                    m_localization.GetString("ID_RTEditor_SaveAssetsDialog_UnableToSaveAsset", "Unable to save asset"),
                    m_localization.GetString("ID_RTEditor_SaveAssetsDialog_UnableToSaveAssetInPlayMode", "Unable to save asset in play mode"));
                return;
            }

            if (string.IsNullOrEmpty(Input.text))
            {
                args.Cancel = true;
                Input.ActivateInputField();
                return;
            }

            if (Input.text != null && Input.text.Length > 0 && (!char.IsLetter(Input.text[0]) || Input.text[0] == '-'))
            {
                m_windowManager.MessageBox(
                    m_localization.GetString("ID_RTEditor_SaveAssetsDialog_AssetNameIsInvalid", "Asset name is invalid"),
                    m_localization.GetString("ID_RTEditor_SaveAssetsDialog_AssetNameShouldStartWith", "Asset name should start with letter"));
                args.Cancel = true;
                return;
            }

            if (!ProjectItem.IsValidName(Input.text))
            {
                m_windowManager.MessageBox(
                    m_localization.GetString("ID_RTEditor_SaveAssetsDialog_AssetNameIsInvalid", "Asset name is invalid"),
                    m_localization.GetString("ID_RTEditor_SaveAssetsDialog_AssetNameInvalidCharacters", "Asset name contains invalid characters"));
                args.Cancel = true;
                return;
            }

            ProjectItem selectedItem = (ProjectItem)m_treeView.SelectedItem;
            if (!selectedItem.IsFolder)
            {
                if (Input.text.ToLower() == selectedItem.Name.ToLower())
                {
                    Overwrite((AssetItem)selectedItem);
                    args.Cancel = true;
                }
                else
                {
                    ProjectItem folder = selectedItem.Parent;
                    SaveAssetToFolder(args, folder);
                }
            }
            else
            {
                ProjectItem folder = selectedItem;
                SaveAssetToFolder(args, folder);
            }
        }

        private void Overwrite(AssetItem selectedItem)
        {
            m_windowManager.Confirmation(
                m_localization.GetString("ID_RTEditor_SaveAssetsDialog_AssetWithSameNameExists", "Asset with same name already exits"),
                m_localization.GetString("ID_RTEditor_SaveAssetsDialog_DoYouWantToOverwriteIt", "Do you want to overwrite it?"),
                (sender, yes) =>
            {
                m_parentDialog.Close(null);

                ProjectItem parent = selectedItem.Parent;

                Editor.IsBusy = true;
                m_project.Delete(new[] { selectedItem }, (deleteError, deleteResult) =>
                {
                    if(deleteError.HasError)
                    {
                        Editor.IsBusy = false;
                        m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_SaveAssetsDialog_UnableToDeleteAsset", "Unable to delete asset"), deleteError.ToString());
                        RaseSaveCompleted(null);
                    }
                    else
                    {
                        SaveAssetToFolder(parent);
                    }
                });

            },
            (sender, no) => Input.ActivateInputField(),
            m_localization.GetString("ID_RTEditor_SaveAssetsDialog_Yes", "Yes"),
            m_localization.GetString("ID_RTEditor_SaveAssetsDialog_No", "No"));
        }

        private void OnItemDataBinding(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            ProjectItem item = e.Item as ProjectItem;
            if (item != null)
            {
                TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>(true);
                text.text = item.Name;

                Image image = e.ItemPresenter.GetComponentInChildren<Image>(true);
                if (item.IsFolder)
                {
                    image.sprite = FolderIcon;
                }
                else
                {
                    if (AssetIcon == null)
                    {
                        image.sprite = DefaultAssetIcon;
                    }
                    else
                    {
                        image.sprite = AssetIcon;
                    }
                }

                System.Guid assetTypeGuid = m_project.ToGuid(Asset.GetType());

                image.gameObject.SetActive(true);
                e.HasChildren = item.Children != null && item.Children.Count(projectItem => projectItem.IsFolder || ((AssetItem)projectItem).TypeGuid == assetTypeGuid) > 0;
            }
        }

        private void OnItemExpanding(object sender, VirtualizingItemExpandingArgs e)
        {
            System.Guid assetTypeGuid = m_project.ToGuid(Asset.GetType());

            ProjectItem item = e.Item as ProjectItem;
            if (item != null)
            {
                e.Children = item.Children.Where(projectItem => projectItem.IsFolder).OrderBy(projectItem => projectItem.Name)
                    .Union(item.Children.Where(projectItem => !projectItem.IsFolder && ((AssetItem)projectItem).TypeGuid == assetTypeGuid).OrderBy(projectItem => projectItem.Name));
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs e)
        {
            ProjectItem selectedItem = (ProjectItem)e.NewItem;
            if (selectedItem == null)
            {
                return;
            }
            if (!selectedItem.IsFolder)
            {
                Input.text = selectedItem.Name;
            }

            Input.ActivateInputField();
        }

        private void OnItemDoubleClick(object sender, ItemArgs e)
        {
            VirtualizingTreeViewItem treeViewItem = m_treeView.GetTreeViewItem(e.Items[0]);
            if (treeViewItem != null)
            {
                treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
            }

            Input.ActivateInputField();
        }

        private void SaveAssetToFolder(DialogCancelArgs args, ProjectItem folder)
        {
            System.Guid assetTypeGuid = m_project.ToGuid(Asset.GetType());
            System.Func<ProjectItem, bool> hasSameNameAndType = p => !p.IsFolder && ((AssetItem)p).TypeGuid == assetTypeGuid && p.Name.ToLower() == Input.text.ToLower();
            if (folder.Children != null && folder.Children.Any(hasSameNameAndType))
            {
                Overwrite((AssetItem)folder.Children.Where(hasSameNameAndType).First());
                args.Cancel = true;
            }
            else
            {
                SaveAssetToFolder(folder);
            }
        }

        private void SaveAssetToFolder(ProjectItem folder)
        {
            Editor.IsBusy = true;

            IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();

            Asset.name = Input.text;

            m_project.Save(new[] { folder }, new[] { new byte[0] }, new[] { Asset }, null, SelectSavedAssets, (saveError, saveResult) =>
            {
                if (saveError.HasError)
                {
                    Editor.IsBusy = false;
                    m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_SaveAssetsDialog_UnableToSaveAsset", "Unable to save asset"), saveError.ToString());
                    RaseSaveCompleted(null);
                }
                else
                {
                    Editor.IsBusy = true;
                    m_project.Load(saveResult, (loadError, UnityObject) =>
                    {
                        Editor.IsBusy = false;
                        if (loadError.HasError)
                        {
                            m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_SaveAssetsDialog_UnableToSaveAsset", "Unable to save asset"), loadError.ToString());
                            RaseSaveCompleted(null);
                        }
                        else
                        {
                            if(UnityObject.Length > 0)
                            {
                                Destroy(Asset);
                                Asset = UnityObject[0];
                            }
                            
                            RaseSaveCompleted(Asset);
                        }
                    });
                }
            });
        }

        private void RaseSaveCompleted(Object result)
        {
            if (SaveCompleted != null)
            {
                SaveCompleted(this, result);
            }
        }
    }
}

