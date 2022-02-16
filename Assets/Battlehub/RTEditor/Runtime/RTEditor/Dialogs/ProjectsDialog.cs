using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using Battlehub.UIControls;
using Battlehub.UIControls.Dialogs;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTEditor
{
    public class ProjectsDialog : RuntimeWindow
    {
        [SerializeField]
        private InputDialog m_inputDialogPrefab = null;

        [SerializeField]
        private VirtualizingTreeView m_treeView = null;
        
        [SerializeField]
        private Sprite ProjectIcon = null;

        private Dialog m_parentDialog;

        private IProject m_project;
        private IWindowManager m_windowManager;
        private ILocalization m_localization;

        [SerializeField]
        private Button m_btnNew = null;

        [SerializeField]
        private Button m_btnDelete = null;

        public ProjectInfo SelectedProject
        {
            get { return m_treeView.SelectedItem as ProjectInfo; }
        }

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.OpenProject;
            base.AwakeOverride();

            m_localization = IOC.Resolve<ILocalization>();
        }

        private void Start()
        {
            m_parentDialog = GetComponentInParent<Dialog>();
            if (m_parentDialog != null)
            {
                m_parentDialog.IsOkVisible = true;
                m_parentDialog.IsCancelVisible = true;
                m_parentDialog.OkText = m_localization.GetString("ID_RTEditor_ProjectsDialog_Btn_Open", "Open");
                m_parentDialog.CancelText = m_localization.GetString("ID_RTEditor_ProjectsDialog_Btn_Cancel", "Cancel");
                m_parentDialog.Ok += OnOk;
            }

            if (m_treeView == null)
            {
                Debug.LogError("m_builtInTreeView == null");
                return;
            }

            m_treeView.ItemDataBinding += OnItemDataBinding;
            m_treeView.ItemDoubleClick += OnItemDoubleClick;

            m_treeView.CanRemove = false;
            m_treeView.CanDrag = false;
            m_treeView.CanEdit = false;
            m_treeView.CanUnselectAll = false;

            m_project = IOC.Resolve<IProject>();
            m_windowManager = IOC.Resolve<IWindowManager>();

            IRTE editor = IOC.Resolve<IRTE>();

            m_parentDialog.IsInteractable = false;
            editor.IsBusy = true;

            m_project.GetProjects((error, projectInfo) =>
            {
                if(error.HasError)
                {
                    m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectsDialog_UnableToGetProjects", "Unable to get projects"), error.ToString());
                    return;
                }

                m_parentDialog.IsInteractable = true;
                editor.IsBusy = false;

                m_treeView.Items = projectInfo.OrderBy(p => p.Name).ToArray();
                if(projectInfo != null && projectInfo.Length > 0)
                {
                    if(m_project.ProjectInfo != null)
                    {
                        m_treeView.SelectedItem = m_treeView.Items.OfType<ProjectInfo>().Where(p => p.Name == m_project.ProjectInfo.Name).FirstOrDefault();
                    }
                    
                    if (m_treeView.SelectedItem == null)
                    {
                        m_treeView.SelectedIndex = 0;
                    }
                }

                if (m_btnNew != null)
                {
                    m_btnNew.onClick.AddListener(OnCreateProjectClick);
                }

                if (m_btnDelete != null)
                {
                    m_btnDelete.onClick.AddListener(OnDestroyProjectClick);
                }
            });
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
        
            if (m_parentDialog != null)
            {
                m_parentDialog.Ok -= OnOk;
            }

            if (m_treeView != null)
            {
                m_treeView.ItemDataBinding -= OnItemDataBinding;
                m_treeView.ItemDoubleClick -= OnItemDoubleClick;
            }

            if (m_btnNew != null)
            {
                m_btnNew.onClick.RemoveListener(OnCreateProjectClick);
            }

            if (m_btnDelete != null)
            {
                m_btnDelete.onClick.RemoveListener(OnDestroyProjectClick);
            }
        }

        private void OnItemDataBinding(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            ProjectInfo item = e.Item as ProjectInfo;
            if (item != null)
            {
                TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>(true);
                text.text = item.Name;

                Image image = e.ItemPresenter.GetComponentInChildren<Image>(true);
                image.sprite = ProjectIcon;
                image.gameObject.SetActive(true);

                e.HasChildren = false;
            }
        }

        private void OnItemDoubleClick(object sender, ItemArgs e)
        {
            m_parentDialog.Close(true);
        }

        private void OnOk(Dialog sender, DialogCancelArgs args)
        {
            if (m_treeView.SelectedItem == null)
            {
                args.Cancel = true;
                return;
            }
            else
            {
                ProjectInfo selectedProject = SelectedProject;
                if (selectedProject == null)
                {
                    args.Cancel = true;
                }
                else
                {
                    Editor.IsPlaying = false;
                    Editor.IsBusy = true;
                    m_project.OpenProject(selectedProject.Name, (error, result) =>
                    {
                        Editor.IsBusy = false;
                        if (error.HasError)
                        {
                            m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectsDialog_UnableToOpenProject", "Unable to open project"), error.ErrorText);
                        }                        
                    });
                }
            }
        }

        private void OnCreateProjectClick()
        {
            InputDialog input = Instantiate(m_inputDialogPrefab);
            input.transform.position = Vector3.zero;

            m_windowManager.Dialog(m_localization.GetString("ID_RTEditor_ProjectsDialog_CreateProject", "Create Project"), input.transform,
                (sender, args) =>
                {
                    string projectName = input.Text;
                    if(string.IsNullOrEmpty(projectName))
                    {
                        args.Cancel = true;
                        return;
                    }

                    if (m_treeView.Items != null && m_treeView.Items.OfType<ProjectInfo>().Any(p => p.Name == projectName))
                    {
                        m_windowManager.MessageBox(
                            m_localization.GetString("ID_RTEditor_ProjectsDialog_UnableToCreateProject", "Unable to create project"),
                            m_localization.GetString("ID_RTEditor_ProjectsDialog_ProjectWithSameNameExists", "Project with the same name already exists"));
                        args.Cancel = true;
                        return;
                    }

                    Editor.IsBusy = true;
                    m_project.CreateProject(projectName, (error, newProjectInfo) =>
                    {
                        Editor.IsBusy = false;
                        if(error.HasError)
                        {
                            m_windowManager.MessageBox(m_localization.GetString("ID_RTEditor_ProjectsDialog_UnableToCreateProject", "Unable to create project"), error.ErrorText);
                            args.Cancel = true;
                            return;
                        }

                        m_treeView.SelectedItem = null;
                        ProjectInfo[] projectInfo = m_treeView.Items.OfType<ProjectInfo>().Union(new[] { newProjectInfo }).OrderBy(p => p.Name).ToArray();
                        m_treeView.Insert(Array.IndexOf(projectInfo, newProjectInfo), newProjectInfo);
                        m_treeView.SelectedItem = newProjectInfo;
                        m_treeView.ScrollIntoView(newProjectInfo);
                    });
                },
                (sender, args) => { },
                m_localization.GetString("ID_RTEditor_ProjectsDialog_Btn_Create", "Create"),
                m_localization.GetString("ID_RTEditor_ProjectsDialog_Btn_Cancel", "Cancel"),
                120, 100, 350, 100);
        }

        private void OnDestroyProjectClick()
        {
            ProjectInfo selectedProject = (ProjectInfo)m_treeView.SelectedItem;
            if(selectedProject == null)
            {
                return;
            }

            m_windowManager.Confirmation(
                m_localization.GetString("ID_RTEditor_ProjectsDialog_DeleteProject", "Delete Project"),
                string.Format(m_localization.GetString("ID_RTEditor_ProjectsDialog_AreYouSureDeleteProject", "Delete {0} project?"), selectedProject.Name),
                (sender, args) =>
            {
                ProjectInfo[] projectInfo = m_treeView.Items.OfType<ProjectInfo>().ToArray();
                int index = Array.IndexOf(projectInfo, selectedProject);
                Editor.IsBusy = true;
                m_project.DeleteProject(selectedProject.Name, (error, deletedProject) =>
                {
                    Editor.IsBusy = false;
                    if (error.HasError)
                    {
                        m_windowManager.MessageBox("Unable to delete project", error.ErrorText);
                        args.Cancel = true;
                        return;
                    }

                    m_treeView.RemoveChild(null, selectedProject);

                    if ((projectInfo.Length - 1) == index)
                    {
                        m_treeView.SelectedIndex = (index - 1);
                    }
                    else
                    {
                        m_treeView.SelectedIndex = index;
                    }
                    
                });
            },
            (sender, args) => { },
            m_localization.GetString("ID_RTEditor_ProjectsDialog_Btn_Delete", "Delete"),
            m_localization.GetString("ID_RTEditor_ProjectsDialog_Btn_Cancel", "Cancel"));

        }
    }
}
