using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using Battlehub.UIControls.Dialogs;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Battlehub.RTScripting
{
    public interface IEditRuntimeScriptDialog
    {
        AssetItem AssetItem
        {
            set;
        }
    }

    public class EditRuntimeScriptDialog : RuntimeWindow, IEditRuntimeScriptDialog
    {
        private Dialog m_parentDialog;
        private IProject m_project;
        private IRuntimeScriptManager m_scriptManager;

        [SerializeField]
        private TMP_InputField m_text = null;
        private RuntimeTextAsset m_textAsset;

        private AssetItem m_assetItem;
        public AssetItem AssetItem
        {
            set { m_assetItem = value; }
        }

        protected override void AwakeOverride()
        {
            IOC.RegisterFallback<IEditRuntimeScriptDialog>(this);
            WindowType = RuntimeWindowType.Custom;
            base.AwakeOverride();

            m_project = IOC.Resolve<IProject>();
            m_scriptManager = IOC.Resolve<IRuntimeScriptManager>();
        }

        protected virtual IEnumerator Start()
        {
            m_parentDialog = GetComponentInParent<Dialog>();
            if (m_parentDialog != null)
            {
                m_parentDialog.OkText = "Save";
                m_parentDialog.Ok += OnSave;
                m_parentDialog.IsOkVisible = true;
                m_parentDialog.IsCancelVisible = true;
            }

            ProjectAsyncOperation<RuntimeTextAsset> ao = m_scriptManager.LoadScript(m_assetItem);
            yield return ao;

            if (ao.HasError)
            {
                Debug.LogError(ao.Error);
                m_parentDialog.Close(false);
                yield break;
            }

            m_textAsset = ao.Result;
            m_text.text = m_textAsset.Text;

        }

        protected override void OnDestroyOverride()
        {
            IOC.UnregisterFallback<IEditRuntimeScriptDialog>(this);
            base.OnDestroyOverride();
            if (m_parentDialog != null)
            {
                m_parentDialog.Ok -= OnSave;
            }
        }

        private void OnSave(Dialog sender, DialogCancelArgs args)
        {
            Editor.StartCoroutine(CoSave());
        }

        private IEnumerator CoSave()
        {
            m_textAsset.Text = m_text.text;
            ProjectAsyncOperation ao = m_scriptManager.SaveScript(m_assetItem, m_textAsset);
            yield return ao;
            if (ao.HasError)
            {
                Debug.LogError(ao.Error);
            }
            else
            {
                ao = m_scriptManager.Compile();
                yield return ao;
                if (ao.HasError)
                {
                    Debug.LogError(ao.Error);
                }
                else
                {
                    ILocalization lc = IOC.Resolve<ILocalization>();
                    Debug.Log(lc.GetString("ID_RTScripting_ScriptsManager_CompilationSucceeded", "Compilation succeeded"));
                }
            }
        }
    }
}



