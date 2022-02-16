using Battlehub.RTCommon;
using Battlehub.RTHandles;
using Battlehub.RTSL;
using Battlehub.RTSL.Interface;
using Battlehub.UIControls;
using Battlehub.UIControls.MenuControl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace Battlehub.RTEditor
{
    public interface IRuntimeEditor : IRTE
    {
        event RTEEvent<CancelArgs> BeforeSceneSave;
        event RTEEvent SceneSaving;
        event RTEEvent SceneSaved;

        event RTEEvent SceneLoading;
        event RTEEvent SceneLoaded;

        void NewScene(bool confirm = true);
        void SaveScene();
        void SaveSceneAs();
        void OverwriteScene(AssetItem scene, Action<Error> callback = null);
        void SaveSceneToFolder(ProjectItem folder, string name, Action<Error> callback = null);

        void CreateWindow(string window);
        void CreateOrActivateWindow(string window);

        bool CmdGameObjectValidate(string cmd);
        void CmdGameObject(string cmd);
        bool CmdEditValidate(string cmd);
        void CmdEdit(string cmd);

        ProjectAsyncOperation<AssetItem[]> CreatePrefab(ProjectItem folder, ExposeToEditor obj, bool? includeDependencies = null, Action<AssetItem[]> done = null);
        ProjectAsyncOperation<AssetItem[]> SaveAssets(UnityObject[] assets, Action<AssetItem[]> done = null);
        ProjectAsyncOperation<ProjectItem[]> DeleteAssets(ProjectItem[] projectItems, Action<ProjectItem[]> done = null);
        ProjectAsyncOperation<AssetItem> UpdatePreview(UnityObject obj, Action<AssetItem> done = null);

        [Obsolete("Use SaveAssets")]
        ProjectAsyncOperation<AssetItem> SaveAsset(UnityObject obj, Action<AssetItem> done = null);
    }

    public static class IRuntimEditorExt
    {
        public static IRuntimeSelectionComponent GetScenePivot(this IRTE editor)
        {
            if (editor.ActiveWindow != null)
            {
                IRuntimeSceneComponent scenePivot = editor.ActiveWindow.IOCContainer.Resolve<IRuntimeSceneComponent>();
                if (scenePivot != null)
                {
                    return scenePivot;
                }
            }

            RuntimeWindow sceneWindow = editor.GetWindow(RuntimeWindowType.Scene);
            if (sceneWindow != null)
            {
                IRuntimeSelectionComponent scenePivot = sceneWindow.IOCContainer.Resolve<IRuntimeSelectionComponent>();
                if (scenePivot != null)
                {
                    return scenePivot;
                }
            }

            return null;
        }

        public static void AddGameObjectToScene(this IRTE editor, GameObject go)
        {
            Vector3 pivot = Vector3.zero;
            IRuntimeSelectionComponent selectionComponent = editor.GetScenePivot();
            if (selectionComponent != null)
            {
                pivot = selectionComponent.SecondaryPivot;
            }

            editor.AddGameObjectToHierarchy(go);

            go.transform.position = pivot;
            if(go.GetComponent<ExposeToEditor>() == null)
            {
                go.AddComponent<ExposeToEditor>();
            }

            go.SetActive(true);
            editor.RegisterCreatedObjects(new[] { go }, selectionComponent != null ? selectionComponent.CanSelect : true);
        }

    }

    [DefaultExecutionOrder(-90)]
    [RequireComponent(typeof(RuntimeObjects))]
    public class RuntimeEditor : RTEBase, IRuntimeEditor
    {
        public event RTEEvent<CancelArgs> BeforeSceneSave;
        public event RTEEvent SceneSaving;
        public event RTEEvent SceneSaved;

        public event RTEEvent SceneLoading;
        public event RTEEvent SceneLoaded;

        private IProject m_project;
        private IWindowManager m_wm;
        private ILocalization m_localization;

        [SerializeField]
        private GameObject m_progressIndicator = null;

        [Serializable]
        public class Settings
        {
            public bool OpenDefaultProject = true;
            public string DefaultProjectName = null;
            public bool CreateCamera = true;
            public bool CreateLight = true;
        }

        [SerializeField]
        private Settings m_extraSettings;

        public override bool IsBusy
        {
            get { return base.IsBusy; }
            set
            {
                if(m_progressIndicator != null)
                {
                    m_progressIndicator.gameObject.SetActive(value);
                }

                base.IsBusy = value;
            }
        }


        public override bool IsPlaying
        {
            get
            {
                return base.IsPlaying;
            }
            set
            {
                if(value != base.IsPlaying)
                {
                    if (!IsPlaying)
                    {
                        RuntimeWindow gameView = GetWindow(RuntimeWindowType.Game);
                        if (gameView != null)
                        {
                            ActivateWindow(gameView);
                        }
                    }

                    base.IsPlaying = value;

                    if (!IsPlaying)
                    {
                        if (ActiveWindow == null || ActiveWindow.WindowType != RuntimeWindowType.Scene)
                        {
                            RuntimeWindow sceneView = GetWindow(RuntimeWindowType.Scene);
                            if (sceneView != null)
                            {
                                ActivateWindow(sceneView);
                            }
                        }
                    }
                }
            }
        }

        protected override void Awake()
        {
            if (!RenderPipelineInfo.UseRenderTextures)
            {
                CameraLayerSettings layerSettings = CameraLayerSettings;
                Transform uiBgCameraTransform = transform.Find("UIBackgroundCamera");
                Transform uiCameraTransform = transform.Find("UICamera");
                Transform uiBgTransform = transform.Find("UIBackground");
                if(uiBgCameraTransform != null && uiCameraTransform != null && uiBgTransform != null)
                {
                    Camera uiBgCamera = uiBgCameraTransform.GetComponent<Camera>();
                    Camera uiCamera = uiCameraTransform.GetComponent<Camera>();
                    Canvas uiBg = uiBgTransform.GetComponent<Canvas>();
                    if(uiBgCamera != null && uiCamera != null && uiBg != null)
                    {
                        uiBgCamera.enabled = true;
                        uiBg.worldCamera = uiBgCamera;
                        uiBgCamera.gameObject.SetActive(true);

                        uiCamera.clearFlags = CameraClearFlags.Depth;
                        uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
                    }
                }
            }
            else
            {
                Transform uiBgCameraTransform = transform.Find("UIBackgroundCamera");
                if(uiBgCameraTransform != null)
                {
                    Destroy(uiBgCameraTransform.gameObject);
                }
            }

            base.Awake();
      
            IOC.Resolve<IRTEAppearance>();
            m_project = IOC.Resolve<IProject>();
            m_wm = IOC.Resolve<IWindowManager>();
            m_localization = IOC.Resolve<ILocalization>();

            m_project.NewSceneCreating += OnNewSceneCreating;
            m_project.NewSceneCreated += OnNewSceneCreated;
            m_project.BeginSave += OnBeginSave;
            m_project.BeginLoad += OnBeginLoad;
            m_project.SaveCompleted += OnSaveCompleted;
            m_project.LoadCompleted += OnLoadCompleted;
            m_project.OpenProjectCompleted += OnProjectOpened;
            m_project.DeleteProjectCompleted += OnProjectDeleted;

            if(m_extraSettings == null)
            {
                m_extraSettings = new Settings();
            }

            if(m_extraSettings.OpenDefaultProject)
            {
                if (string.IsNullOrEmpty(m_extraSettings.DefaultProjectName))
                {
                    m_extraSettings.DefaultProjectName = PlayerPrefs.GetString("RuntimeEditor.DefaultProject", "DefaultProject");
                }

                if(!m_project.IsOpened)
                {
                    IsBusy = true;
                    m_project.OpenProject(m_extraSettings.DefaultProjectName, (error, projectInfo) =>
                    {
                        IsBusy = false;
                    });
                }                
            }
        }

        protected override void Start()
        {
            if (GetComponent<RuntimeEditorInput>() == null)
            {
                gameObject.AddComponent<RuntimeEditorInput>();
            }
            base.Start();
            if (EventSystem != null)
            {
                if (!EventSystem.GetComponent<RTSLIgnore>() && EventSystem.transform.parent == null)
                {
                    EventSystem.gameObject.AddComponent<RTSLIgnore>();
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            StopAllCoroutines();

            if (m_project != null)
            {
                m_project.NewSceneCreating -= OnNewSceneCreating;
                m_project.NewSceneCreated -= OnNewSceneCreated;
                m_project.BeginSave -= OnBeginSave;
                m_project.BeginLoad -= OnBeginLoad;
                m_project.SaveCompleted -= OnSaveCompleted;
                m_project.LoadCompleted -= OnLoadCompleted;
                m_project.OpenProjectCompleted -= OnProjectOpened;
                m_project.DeleteProjectCompleted -= OnProjectDeleted;
            }
        }

        protected override void Update()
        {
            
        }

        public void SetDefaultLayout()
        {
            string persistentLayoutName = m_wm.DefaultPersistentLayoutName;
            if(m_wm.LayoutExist(persistentLayoutName))
            {
                m_wm.DeleteLayout(persistentLayoutName);
            }

            m_wm.SetDefaultLayout();
        }

        public void CmdCreateWindowValidate(MenuItemValidationArgs args)
        {
            #if !(UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA)
            if (args.Command.ToLower() == RuntimeWindowType.ImportFile.ToString().ToLower())
            {
                args.IsVisible = false;          
            }
            #endif
        }

        public virtual void CreateWindow(string windowTypeName)
        {
            m_wm.CreateWindow(windowTypeName);
        }

        public virtual void CreateOrActivateWindow(string windowTypeName)
        {
            if(!m_wm.CreateWindow(windowTypeName))
            {
                if (m_wm.Exists(windowTypeName))
                {
                    if(!m_wm.IsActive(windowTypeName))
                    {
                        m_wm.ActivateWindow(windowTypeName);

                        Transform windowTransform = m_wm.GetWindow(windowTypeName);

                        RuntimeWindow window = windowTransform.GetComponentInChildren<RuntimeWindow>();
                        if (window != null)
                        {
                            base.ActivateWindow(window);
                        }
                    }
                }
            }
        }

        public override void ActivateWindow(RuntimeWindow window)
        {
            base.ActivateWindow(window);
            if (window != null)
            {
                m_wm.ActivateWindow(window.transform);
            }
        }

        public virtual void NewScene(bool confirm)
        {
            if(confirm)
            {
                m_wm.Confirmation(m_localization.GetString("ID_RTEditor_CreateNewScene", "Create New Scene"),
                    m_localization.GetString("ID_RTEditor_DoYouWantCreateNewScene", "Do you want to create new scene?") + System.Environment.NewLine +
                    m_localization.GetString("ID_RTEditor_UnsavedChangesWillBeLost", "All unsaved changes will be lost"), (dialog, args) =>
                    {
                        m_project.CreateNewScene();
                    },
                    (dialog, args) => { },
                    m_localization.GetString("ID_RTEditor_Create", "Create"),
                    m_localization.GetString("ID_RTEditor_Cancel", "Cancel"));
            }
            else
            {
                m_project.CreateNewScene();
            }
        }

        public virtual void SaveScene()
        {
            if (m_project.LoadedScene == null)
            {
                m_wm.CreateWindow(RuntimeWindowType.SaveScene.ToString());
            }
            else
            {
                if(IsPlaying)
                {
                    m_wm.MessageBox(
                        m_localization.GetString("ID_RTEditor_UnableToSaveScene", "Unable to save scene"), 
                        m_localization.GetString("ID_RTEditor_UnableToSaveSceneInPlayMode", "Unable to save scene in play mode"));
                    return;
                }

                AssetItem scene = m_project.LoadedScene;
                OverwriteScene(scene, error =>
                {
                    if (error.HasError)
                    {
                        m_wm.MessageBox(m_localization.GetString("ID_RTEditor_UnableToSaveScene", "Unable to save scene"), error.ErrorText);
                    }
                });    
            }
        }

        public void OverwriteScene(AssetItem scene, Action<Error> callback)
        {
            if(BeforeSceneSave != null)
            {
                CancelArgs args = new CancelArgs();
                BeforeSceneSave(args);
                if(args.Cancel)
                {
                    if(callback != null)
                    {
                        callback(new Error(Error.OK));
                    }
                    return;
                }
            }

            Undo.Purge();
            IsBusy = true;
            m_project.Save(new[] { scene }, new[] { (object)SceneManager.GetActiveScene() }, (error, assetItem) =>
            {
                if(!error.HasError)
                {
                    m_project.LoadedScene = assetItem[0];
                }
                IsBusy = false;
                if(callback != null)
                {
                    callback(error);
                }
            });
        }

        public void SaveSceneToFolder(ProjectItem folder, string name, Action<Error> callback)
        {
            if (BeforeSceneSave != null)
            {
                CancelArgs args = new CancelArgs();
                BeforeSceneSave(args);
                if (args.Cancel)
                {
                    if (callback != null)
                    {
                        callback(new Error(Error.OK));
                    }
                    return;
                }
            }

            Undo.Purge();
            IsBusy = true;
            m_project.Save(new[] { folder }, new[] { new byte[0] }, new[] { (object)SceneManager.GetActiveScene() }, new[] { name }, (error, assetItem) =>
            {
                IsBusy = false;
                if (!error.HasError)
                {
                    if (assetItem.Length > 0)
                    {
                        m_project.LoadedScene = assetItem[0];
                    }
                }
                if(callback != null)
                {
                    callback(error);
                }
            });
        }

        public virtual void SaveSceneAs()
        {
            if (IsPlaying)
            {
                m_wm.MessageBox(
                    m_localization.GetString("ID_RTEditor_UnableToSaveScene", "Unable to save scene"),
                    m_localization.GetString("ID_RTEditor_UnableToSaveSceneInPlayMode", "Unable to save scene in play mode"));
                return;
            }

            if (m_project == null)
            {
                Debug.LogError("Project Manager is null");
                return;
            }

            CreateOrActivateWindow("SaveScene");
        }

        public void CmdGameObjectValidate(MenuItemValidationArgs args)
        {
            args.IsValid = CmdGameObjectValidate(args.Command);
        }

        public bool CmdGameObjectValidate(string cmd)
        {
            IGameObjectCmd goCmd = IOC.Resolve<IGameObjectCmd>();
            if(goCmd != null)
            {
                return goCmd.CanExec(cmd);
            }
            return false;
        }

        public void CmdGameObject(string cmd)
        {
            IGameObjectCmd goCmd = IOC.Resolve<IGameObjectCmd>();
            if(goCmd != null)
            {
                goCmd.Exec(cmd);
            }
        }

        public void CmdEditValidate(MenuItemValidationArgs args)
        {
            args.IsValid = CmdEditValidate(args.Command);
        }

        public bool CmdEditValidate(string cmd)
        {
            IEditCmd editCmd = IOC.Resolve<IEditCmd>();
            if (editCmd != null)
            {
                return editCmd.CanExec(cmd);
            }
            return false;
        }

        public void CmdEdit(string cmd)
        {
            IEditCmd editCmd = IOC.Resolve<IEditCmd>();
            if (editCmd != null)
            {
                editCmd.Exec(cmd);
            }
        }

        public ProjectAsyncOperation<AssetItem[]> CreatePrefab(ProjectItem dropTarget, ExposeToEditor dragObject, bool? includeDependencies, Action<AssetItem[]> done)
        {
            Action<AssetItem[]> callback = assetItems =>
            {
                if(done != null)
                {
                    done(assetItems);
                }
            };

            ProjectAsyncOperation<AssetItem[]> ao = new ProjectAsyncOperation<AssetItem[]>();
            if (!includeDependencies.HasValue)
            {
                m_wm.Confirmation(
                    m_localization.GetString("ID_RTEditor_CreatePrefab", "Create Prefab"),
                    m_localization.GetString("ID_RTEditor_IncludeDependencies", "Include dependencies?"),
                    (sender, args) =>
                    {
                        CreatePrefabWithDependencies(dropTarget, dragObject, result => OnCreatePrefabWithDepenenciesCompleted(result, ao, callback));
                    },
                    (sender, args) =>
                    {
                        CreatePrefabWithoutDependencies(dropTarget, dragObject, result => OnCreatePrefabWithDepenenciesCompleted(result, ao, callback));
                    },
                    m_localization.GetString("ID_RTEditor_Yes", "Yes"),
                    m_localization.GetString("ID_RTEditor_No", "No"));
            }
            else
            {
                if(includeDependencies.Value)
                {
                    CreatePrefabWithDependencies(dropTarget, dragObject, result => OnCreatePrefabWithDepenenciesCompleted(result, ao, callback));
                }
                else
                {
                    CreatePrefabWithoutDependencies(dropTarget, dragObject, result => OnCreatePrefabWithDepenenciesCompleted(result, ao, callback));
                }
            }

            return ao;
        }

        private void OnCreatePrefabWithDepenenciesCompleted(AssetItem[] result, ProjectAsyncOperation<AssetItem[]> ao, Action<AssetItem[]> callback)
        {
            if (callback != null)
            {
                callback(result);
            }

            ao.Error = new Error();
            ao.Result = result;
            ao.IsCompleted = true;
        }

        private void CreatePrefabWithoutDependencies(ProjectItem dropTarget, ExposeToEditor dragObject, Action<AssetItem[]> done)
        {
            IResourcePreviewUtility previewUtility = IOC.Resolve<IResourcePreviewUtility>();
            byte[] previewData = previewUtility.CreatePreviewData(dragObject.gameObject);
            CreatePrefab(dropTarget, new[] { previewData }, new[] { dragObject.gameObject }, done);
        }

        private void CreatePrefabWithDependencies(ProjectItem dropTarget, ExposeToEditor dragObject, Action<AssetItem[]> done)
        {
            IResourcePreviewUtility previewUtility = IOC.Resolve<IResourcePreviewUtility>();
            
            m_project.GetDependencies(dragObject.gameObject, true, (error, deps) =>
            {
                object[] objects;
                if (!deps.Contains(dragObject.gameObject))
                {
                    objects = new object[deps.Length + 1];
                    objects[deps.Length] = dragObject.gameObject;
                    for (int i = 0; i < deps.Length; ++i)
                    {
                        objects[i] = deps[i];
                    }
                }
                else
                {
                    objects = deps;
                }

                IUnityObjectFactory uoFactory = IOC.Resolve<IUnityObjectFactory>();
                objects = objects.Where(obj => uoFactory.CanCreateInstance(obj.GetType())).ToArray();
            
                byte[][] previewData = new byte[objects.Length][];
                for (int i = 0; i < objects.Length; ++i)
                {
                    if (objects[i] is UnityObject)
                    {
                        previewData[i] = previewUtility.CreatePreviewData((UnityObject)objects[i]);
                    }
                }

                CreatePrefab(dropTarget, previewData, objects, done);
            });
        }

        private void CreatePrefab(ProjectItem dropTarget, byte[][] previewData, object[] objects, Action<AssetItem[]> done)
        {
            StartCoroutine(CoCreatePrefab(dropTarget, previewData, objects, done));
        }

        private IEnumerator CoCreatePrefab(ProjectItem dropTarget, byte[][] previewData, object[] objects, Action<AssetItem[]> done)
        {            
            if(objects.Any(o => !(o is GameObject)))
            {
                IsBusy = true;

                if (dropTarget.Children == null || dropTarget.Get("DropTarget/Data") == null)
                {
                    ProjectAsyncOperation createFoldersAo = m_project.CreateFolder(dropTarget.Get("DropTarget/Data", true));
                    yield return createFoldersAo;
                    if (createFoldersAo.HasError)
                    {
                        m_wm.MessageBox("Unable to create data folder", createFoldersAo.Error.ToString());
                        IsBusy = false;
                        if (done != null)
                        {
                            done(null);
                        }
                        yield break;
                    }
                }

                IProjectTree projectTree = IOC.Resolve<IProjectTree>();
                if (projectTree != null)
                {
                    projectTree.SelectedItem = dropTarget;
                }
            }

            IsBusy = true;

            ProjectItem dataFolder = dropTarget.Get("DropTarget/Data");
            List<ProjectItem> parents = new List<ProjectItem>();
            for (int i = 0; i < objects.Length; ++i)
            {
                object obj = objects[i];
                if (obj is GameObject)
                {
                    parents.Add(dropTarget);
                }
                else
                {
                    parents.Add(dataFolder);
                }
            }

            ProjectAsyncOperation<AssetItem[]> saveAo = m_project.Save(parents.ToArray(), previewData, objects, null);
            yield return saveAo;
            if (saveAo.HasError)
            {
                m_wm.MessageBox(m_localization.GetString("ID_RTEditor_UnableToCreatePrefab", "Unable to create prefab"), saveAo.Error.ErrorText);
                IsBusy = false;
                if(done != null)
                {
                    done(null);
                }
                yield break;
            }

            IsBusy = false;
            if (done != null)
            {
                done(saveAo.Result.ToArray());
            }
        }

        public ProjectAsyncOperation<AssetItem[]> SaveAssets(UnityObject[] assets, Action<AssetItem[]> done)
        {
            ProjectAsyncOperation<AssetItem[]> ao = new ProjectAsyncOperation<AssetItem[]>();

            IProject project = IOC.Resolve<IProject>();

            List<UnityObject> assetsToSave = new List<UnityObject>();
            List<AssetItem> assetItems = new List<AssetItem>();

            for(int i = 0; i < assets.Length; ++i)
            {
                UnityObject asset = assets[i];
                AssetItem assetItem = project.ToAssetItem(asset);
                if (assetItem == null)
                {
                    continue;
                }

                assetsToSave.Add(asset);
                assetItems.Add(assetItem);
            }

            if(assetsToSave.Count == 0)
            {
                if(done != null)
                {
                    done(assetItems.ToArray());
                }
                ao.Error = Error.NoError;
                ao.IsCompleted = true;
                return ao;
            }

            IsBusy = true;
            m_project.Save(assetItems.ToArray(), assets.ToArray(), (saveError, saveResult) =>
            {
                if (saveError.HasError)
                {
                    IsBusy = false;
                    m_wm.MessageBox(m_localization.GetString("ID_RTEditor_UnableToSaveAsset", "Unable to save asset"), saveError.ErrorText);

                    if (done != null)
                    {
                        done(null);
                    }

                    ao.Error = saveError;
                    ao.IsCompleted = true;
                    return;
                }

                UpdateDependentAssetPreviews(saveResult, () =>
                {
                    IsBusy = false;
                    if (done != null)
                    {
                        done(saveResult);
                    }
                    ao.Error = Error.NoError;
                    ao.Result = saveResult;
                    ao.IsCompleted = true;
                });
            });

            return ao;
        }

        public ProjectAsyncOperation<ProjectItem[]> DeleteAssets(ProjectItem[] projectItems, Action<ProjectItem[]> done)
        {
            ProjectAsyncOperation<ProjectItem[]> ao = new ProjectAsyncOperation<ProjectItem[]> ();

            IProject project = IOC.Resolve<IProject>();
            AssetItem[] assetItems = projectItems.OfType<AssetItem>().ToArray();
            for(int i = 0; i < assetItems.Length; ++i)
            {
                AssetItem assetItem = assetItems[i];
                UnityObject obj = m_project.FromPersistentID<UnityObject>(assetItem);
                
                if (obj != null)
                {
                    if (obj is GameObject)
                    {
                        GameObject go = (GameObject)obj;
                        Component[] components = go.GetComponentsInChildren<Component>(true);
                        for(int j = 0; j < components.Length; ++j)
                        {
                            Component component = components[j];
                            Undo.Erase(component, null);
                            if(component is Transform)
                            {
                                Undo.Erase(component.gameObject, null);
                            }
                        }
                    }
                    else
                    {
                        Undo.Erase(obj, null);
                    }
                }
            }

            ProjectItem[] folders = projectItems.Where(pi => pi.IsFolder).ToArray();
            m_project.Delete(assetItems.Union(folders).ToArray(), (deleteError, deletedItems) =>
            {
                IsBusy = false;
                if (deleteError.HasError)
                {
                    m_wm.MessageBox(m_localization.GetString("ID_RTEditor_UnableToDeleteFolders", "Unable to delete folders"), deleteError.ErrorText);

                    if (done != null)
                    {
                        done(null);
                    }

                    ao.Error = deleteError;
                    ao.IsCompleted = true;
                    return;
                }

                StartCoroutine(CoUpdateDependantAssetPreview(assetItems, () =>
                {
                    if (done != null)
                    {
                        done(projectItems);
                    }

                    ao.Error = new Error();
                    ao.Result = projectItems;
                    ao.IsCompleted = true;
                }));
            });

            return ao;
        }

        private IEnumerator CoUpdateDependantAssetPreview(AssetItem[] assetItems, Action callback)
        {
            yield return new WaitForEndOfFrame();
            UpdateDependentAssetPreviews(assetItems, callback);
        }

        private void UpdateDependentAssetPreviews(AssetItem[] assetItems, Action callback)
        {
            IResourcePreviewUtility previewUtil = IOC.Resolve<IResourcePreviewUtility>();
            AssetItem[] dependentItems = m_project.GetDependantAssetItems(assetItems).Where(item => !m_project.IsScene(item)).ToArray();
            if(dependentItems.Length > 0)
            {
                m_project.Load(dependentItems, (loadError, loadedObjects) =>
                {
                    if (loadError.HasError)
                    {
                        IsBusy = false;
                        m_wm.MessageBox(m_localization.GetString("ID_RTEditor_UnableToLoadAssets", "Unable to load assets"), loadError.ErrorText);
                        return;
                    }

                    for (int i = 0; i < loadedObjects.Length; ++i)
                    {
                        UnityObject loadedObject = loadedObjects[i];
                        AssetItem dependentItem = dependentItems[i];
                        if (loadedObject != null)
                        {
                            byte[] previewData = previewUtil.CreatePreviewData(loadedObject);
                            dependentItem.Preview = new Preview { PreviewData = previewData };
                        }
                        else
                        {
                            dependentItem.Preview = new Preview();
                        }

                        m_project.SetPersistentID(dependentItem.Preview, m_project.ToPersistentID(dependentItem));
                    }

                    m_project.SavePreview(dependentItems, (savePreviewError, savedAssetItems) =>
                    {
                        if (savePreviewError.HasError)
                        {
                            IsBusy = false;
                            m_wm.MessageBox(m_localization.GetString("ID_RTEditor_UnableToLoadAssets", "Unable to load assets"), savePreviewError.ErrorText);
                            return;
                        }

                        callback();
                    });
                });
            }
            else
            {
                callback();
            }
        }

        public ProjectAsyncOperation<AssetItem> UpdatePreview(UnityObject obj, Action<AssetItem> done)
        {
            ProjectAsyncOperation<AssetItem> ao = new ProjectAsyncOperation<AssetItem>();

            IProject project = IOC.Resolve<IProject>();
            AssetItem assetItem = project.ToAssetItem(obj);
            if (assetItem != null)
            {
                IResourcePreviewUtility resourcePreviewUtility = IOC.Resolve<IResourcePreviewUtility>();
                byte[] preview = resourcePreviewUtility.CreatePreviewData(obj);
                assetItem.Preview = new Preview { PreviewData = preview };
                m_project.SetPersistentID(assetItem.Preview, m_project.ToPersistentID(assetItem));
            }

            if (done != null)
            {
                done(assetItem);
            }

            ao.Error = new Error();
            ao.Result = assetItem;
            ao.IsCompleted = true;
            return ao;
        }


        private void OnNewSceneCreating(Error error)
        {
            if (error.HasError)
            {
                return;
            }

            IsPlaying = false;

            if (SceneLoading != null)
            {
                SceneLoading();
            }
        }

        private void OnNewSceneCreated(Error error)
        {
            if(error.HasError)
            {
                return;
            }

            if (m_extraSettings.CreateLight)
            {
                if (m_project.ToGuid(typeof(Light)) != Guid.Empty)
                {
                    GameObject lightGO = new GameObject(m_localization.GetString("ID_RTEditor_DirectionalLight", "Directional Light"));
                    lightGO.transform.position = Vector3.up * 3;
                    lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

                    Light light = lightGO.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.shadows = LightShadows.Soft;
                    lightGO.AddComponent<ExposeToEditor>();

                    if(RenderPipelineInfo.Type == RPType.HDRP)
                    {
                        light.intensity = 10000;
                    }
                }
            }

            if(m_extraSettings.CreateCamera)
            { 
                if (m_project.ToGuid(typeof(Camera)) != Guid.Empty)
                {
                    GameObject cameraGO = new GameObject(m_localization.GetString("ID_RTEditor_Camera", "Camera"));
                    cameraGO.transform.position = new Vector3(0, 1, -10);

                    cameraGO.gameObject.SetActive(false);
                    cameraGO.AddComponent<Camera>();
                    cameraGO.AddComponent<ExposeToEditor>();
                    cameraGO.gameObject.SetActive(true);

                    cameraGO.AddComponent<GameViewCamera>();
                }
            }

            Selection.objects = null;
            Undo.Purge();

            StartCoroutine(CoNewSceneCreated());
        }

        private IEnumerator CoNewSceneCreated()
        {
            yield return new WaitForEndOfFrame();
            if (SceneLoaded != null)
            {
                SceneLoaded();
            }
        }

        private IEnumerator CoCallback(Action cb)
        {
            yield return new WaitForEndOfFrame();
            if (cb != null)
            {
                cb();
            }
        }

        private void RaiseIfIsScene(Error error, AssetItem[] assetItems, Action callback)
        {
            if (error.HasError)
            {
                return;
            }

            if (assetItems != null && assetItems.Length > 0)
            {
                AssetItem assetItem = assetItems[0];
                if (assetItem != null && m_project.IsScene(assetItem))
                {
                    callback();
                }
            }
        }

        private void OnBeginLoad(Error error, AssetItem[] result)
        {
            RaiseIfIsScene(error, result, () =>
            { 
                IsPlaying = false;

                Selection.objects = null;
                Undo.Purge();

                if (SceneLoading != null)
                {
                    SceneLoading();
                }
            });
        }

        private void OnBeginSave(Error error, object[] result)
        {
            if (error.HasError)
            {
                return;
            }
            if (result != null && result.Length > 0)
            {
                IsPlaying = false;

                object obj = result[0];
                if (obj != null && obj is Scene)
                {
                    if(SceneSaving != null)
                    {
                        SceneSaving();
                    }
                }
            }
        }

        private void OnLoadCompleted(Error error, AssetItem[] result, UnityObject[] objects)
        {
            RaiseIfIsScene(error, result, () =>
            {
                if (SceneLoaded != null)
                {
                    SceneLoaded();
                }
            });
        }

        private void OnSaveCompleted(Error error, AssetItem[] result, bool userAction)
        {
            RaiseIfIsScene(error, result, () =>
            {
                if (SceneSaved != null)
                {
                    SceneSaved();
                }
            });
        }

        private void OnProjectOpened(Error error, ProjectInfo result)
        {
            PlayerPrefs.SetString("RuntimeEditor.DefaultProject", result.Name);
        }

        private void OnProjectDeleted(Error error, string projectName)
        {
            if(projectName == PlayerPrefs.GetString("RuntimeEditor.DefaultProject"))
            {
                PlayerPrefs.DeleteKey("RuntimeEditor.DefaultProject");
            }
        }

        #region Obsolete
        [Obsolete("Use Save Assets instead")]
        public ProjectAsyncOperation<AssetItem> SaveAsset(UnityObject obj, Action<AssetItem> done)
        {
            ProjectAsyncOperation<AssetItem> ao = new ProjectAsyncOperation<AssetItem>();

            IProject project = IOC.Resolve<IProject>();
            AssetItem assetItem = project.ToAssetItem(obj);
            if (assetItem == null)
            {
                if (done != null)
                {
                    done(null);
                }

                ao.Error = new Error();
                ao.IsCompleted = true;
                return ao;
            }

            IsBusy = true;
            m_project.Save(new[] { assetItem }, new[] { obj }, (saveError, saveResult) =>
            {
                if (saveError.HasError)
                {
                    IsBusy = false;
                    m_wm.MessageBox(m_localization.GetString("ID_RTEditor_UnableToSaveAsset", "Unable to save asset"), saveError.ErrorText);

                    if (done != null)
                    {
                        done(null);
                    }

                    ao.Error = saveError;
                    ao.IsCompleted = true;
                    return;
                }

                UpdateDependentAssetPreviews(saveResult, () =>
                {
                    IsBusy = false;
                    if (done != null)
                    {
                        done(saveResult[0]);
                    }
                    ao.Error = new Error();
                    ao.Result = saveResult[0];
                    ao.IsCompleted = true;
                });
            });

            return ao;
        }

        #endregion
    }
}
