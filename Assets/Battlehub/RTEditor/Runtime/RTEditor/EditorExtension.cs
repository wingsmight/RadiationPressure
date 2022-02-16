using Battlehub.RTCommon;
using Battlehub.RTHandles;
using System;
using System.Collections;
using UnityEngine;

namespace Battlehub.RTEditor
{
    //For backward compatibility
    public class EditorOverride : EditorExtension
    {

    }

    public class EditorExtension : MonoBehaviour
    {
        private IRTEState m_rteState;
        private IRTE m_editor;

        protected virtual void Awake()
        {
            m_rteState = IOC.Resolve<IRTEState>();
            if (m_rteState != null)
            {
                if (m_rteState.IsCreated)
                {
                    OnEditorExist();
                }
                else
                {
                    m_rteState.Created += OnEditorCreated;
                }
            }
            else
            {
                OnEditorExist();
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_rteState != null)
            {
                m_rteState.Created -= OnEditorCreated;
            }

            if(m_editor != null)
            {
                m_editor.IsOpenedChanged -= OnIsOpenedChanged;
            }
        }

        protected virtual void OnEditorExist()
        {
            m_editor = IOC.Resolve<IRTE>();
            m_editor.IsOpenedChanged += OnIsOpenedChanged;
            if (m_editor.IsOpened)
            {
                OnEditorOpened();
            }
        }

        private void OnIsOpenedChanged()
        {
            if (m_editor.IsOpened)
            {
                OnEditorOpened();
            }
            else
            {
                m_editor.IsOpenedChanged -= OnIsOpenedChanged;
                OnEditorClosed();
            }
        }

        protected virtual void OnEditorCreated(object obj)
        {
            OnEditorExist();
        }

        protected virtual void OnEditorOpened()
        {

        }

        protected virtual void OnEditorClosed()
        {

        }

        protected void RunNextFrame(Action action)
        {
            StartCoroutine(CoWaitForEndOfFrame(action));
        }

        private IEnumerator CoWaitForEndOfFrame(Action action)
        {
            yield return new WaitForEndOfFrame();
            action();
        }
    }

    public class SceneComponentExtension : EditorExtension
    {
        private IRTE m_editor;
        private IRuntimeSceneComponent m_sceneComponent;

        protected override void OnEditorExist()
        {
            base.OnEditorExist();
            m_editor = IOC.Resolve<IRTE>();
            m_editor.ActiveWindowChanged += OnActiveWindowChanged;
            OnActiveWindowChanged(m_editor.ActiveWindow);
        }

        protected override void OnEditorClosed()
        {
            base.OnEditorClosed();
            m_editor.ActiveWindowChanged -= OnActiveWindowChanged;
            if (m_sceneComponent != null)
            {
                OnSceneDeactivated(m_sceneComponent);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_editor.ActiveWindowChanged -= OnActiveWindowChanged;
            if (m_sceneComponent != null)
            {
                OnSceneDeactivated(m_sceneComponent);
            }
        }

        private void OnActiveWindowChanged(RuntimeWindow deactivatedWindow)
        {
            if (m_sceneComponent != null)
            {
                OnSceneDeactivated(m_sceneComponent);
            }

            if (m_editor.ActiveWindow != null)
            {
                m_sceneComponent = m_editor.ActiveWindow.IOCContainer.Resolve<IRuntimeSceneComponent>();
                if (m_sceneComponent != null)
                {
                    OnSceneActivated(m_sceneComponent);
                }

            }
        }

        protected virtual void OnSceneActivated(IRuntimeSceneComponent sceneComponent)
        {


        }

        protected virtual void OnSceneDeactivated(IRuntimeSceneComponent sceneComponent)
        {

        }
    }
}

