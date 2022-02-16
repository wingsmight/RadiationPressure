using Battlehub.RTCommon;
using Battlehub.RTHandles;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.RTEditor.Demo
{
    public class RegisterCustomHandleExample : EditorExtension
    {
        private IWindowManager m_wm;
        private readonly Dictionary<Transform, CustomHandleExample> m_windowToHandle = new Dictionary<Transform, CustomHandleExample>();

        protected override void OnEditorExist()
        {
            base.OnEditorExist();
            m_wm = IOC.Resolve<IWindowManager>();
            Subscribe();
        }

        protected override void OnEditorClosed()
        {
            base.OnEditorClosed();
            Unsubscribe();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Unsubscribe();
        }

        private void Subscribe()
        {
            m_wm.WindowCreated += OnWindowCreated;
            m_wm.AfterLayout += OnAfterLayout;
            m_wm.WindowDestroyed += OnWindowDestroyed;
        }

        private void Unsubscribe()
        {
            if (m_wm != null)
            {
                m_wm.WindowCreated -= OnWindowCreated;
                m_wm.AfterLayout -= OnAfterLayout;
                m_wm.WindowDestroyed -= OnWindowDestroyed;
            }
        }

        private void OnAfterLayout(IWindowManager wm)
        {
            foreach (Transform windowTransform in wm.GetWindows())
            {
                CreateHandle(windowTransform);
            }
        }

        private void OnWindowCreated(Transform windowTransform)
        {
            CreateHandle(windowTransform);
        }

        private void OnWindowDestroyed(Transform windowTransform)
        {
            CustomHandleExample handle;
            if (m_windowToHandle.TryGetValue(windowTransform, out handle))
            {
                Destroy(handle.gameObject);
                m_windowToHandle.Remove(windowTransform);
            }
        }

        private void CreateHandle(Transform windowTransform)
        {
            SceneView scene = windowTransform.GetComponentInChildren<SceneView>();
            if (scene != null)
            {
                GameObject go = new GameObject("CustomHandle");
                go.transform.SetParent(transform, false);
                go.gameObject.SetActive(false);

                CustomHandleExample handle = go.AddComponent<CustomHandleExample>();
                handle.Window = scene;

                m_windowToHandle.Add(windowTransform, handle);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                foreach (Transform windowTranform in m_windowToHandle.Keys)
                {
                    RuntimeWindow window = windowTranform.GetComponentInChildren<RuntimeWindow>();
                    IRuntimeSelectionComponent selectionComponent = window.IOCContainer.Resolve<IRuntimeSelectionComponent>();
                    selectionComponent.CustomHandle = m_windowToHandle[windowTranform];
                }

                IRTE rte = IOC.Resolve<IRTE>();
                rte.Tools.Current = RuntimeTool.Custom;

            }
        }
    }
}
