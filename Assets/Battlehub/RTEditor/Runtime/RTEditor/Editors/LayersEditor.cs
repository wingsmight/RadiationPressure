using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using Battlehub.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class LayersEditor : MonoBehaviour
    {
        [SerializeField]
        private Transform m_editorsPanel = null;
        [SerializeField]
        private GameObject m_editorPrefab = null;
        private LayersInfo m_layersInfo;
        private bool m_isDirty = false;
        private IRTE m_editor;
        
        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();

            m_layersInfo = (LayersInfo)m_editor.Selection.activeObject;

            foreach(LayersInfo.Layer layer in m_layersInfo.Layers)
            {
                GameObject editor = Instantiate(m_editorPrefab, m_editorsPanel, false);

                TextMeshProUGUI text = editor.GetComponentInChildren<TextMeshProUGUI>(true);
                if (text != null)
                {
                    text.text = layer.Index + ": ";
                }

                StringEditor stringEditor = editor.GetComponentInChildren<StringEditor>(true);
                if (stringEditor != null)
                {
                    if(layer.Index <= 5)
                    {
                        TMP_InputField inputField = stringEditor.GetComponentInChildren<TMP_InputField>(true);
                        inputField.selectionColor = new Color(0, 0, 0, 0);
                        inputField.readOnly = true;
                    }
                    
                    stringEditor.Init(layer, layer, Strong.MemberInfo((LayersInfo.Layer x) => x.Name), null, string.Empty, null, () => m_isDirty = true, null, false);
                }
            }
        }

        private void OnDestroy()
        {
            if(m_isDirty)
            {
                IRTE editor = IOC.Resolve<IRTE>();
                if(editor != null)
                {
                    editor.StartCoroutine(CoEndEdit());
                }
            }
        }

        private void OnApplicationQuit()
        {
            m_isDirty = false;
        }

        private static string m_currentProject;

        private static LayersInfo m_loadedLayers;
        public static LayersInfo LoadedLayers
        {
            get { return m_loadedLayers; }
        }

        public static void LoadLayers(Action<LayersInfo> callback)
        {
            IRTE editor = IOC.Resolve<IRTE>();
            editor.StartCoroutine(CoLoadLayer(callback));
        }

        public static void BeginEdit()
        {
            IRTE editor = IOC.Resolve<IRTE>();
            editor.StartCoroutine(CoLoadLayer(loadedLayers =>
            {
                editor.Selection.activeObject = loadedLayers;
            }));
        }

        private static IEnumerator CoLoadLayer(Action<LayersInfo> callback)
        {
            IProject project = IOC.Resolve<IProject>();

            if (m_loadedLayers == null || project.ProjectInfo.Name != m_currentProject)
            {
                m_currentProject = project.ProjectInfo.Name;

                ProjectAsyncOperation<RuntimeTextAsset> getLayersInfoAo = project.GetValue<RuntimeTextAsset>("Battlehub.RTEditor.LayersInfo_1");
                yield return getLayersInfoAo;
                if (getLayersInfoAo.HasError)
                {
                    if (getLayersInfoAo.Error.ErrorCode != Error.E_NotFound)
                    {
                        Debug.LogErrorFormat("Unable to load LayersInfo {0}", getLayersInfoAo.Error);
                        yield break;
                    }

                    m_loadedLayers = ScriptableObject.CreateInstance<LayersInfo>();
                    m_loadedLayers.Layers = new List<LayersInfo.Layer>
                    {
                        new LayersInfo.Layer("Default", 0),
                        new LayersInfo.Layer("Transparent FX", 1),
                        new LayersInfo.Layer("Ignore Raycast", 2),
                        new LayersInfo.Layer("Water", 4),
                        new LayersInfo.Layer("UI", 10)
                    };

                    for (int i = 11; i < 15; ++i)
                    {
                        m_loadedLayers.Layers.Add(new LayersInfo.Layer(LayerMask.LayerToName(i), i));
                    }

                    for (int i = 25; i <= 30; ++i)
                    {
                        m_loadedLayers.Layers.Add(new LayersInfo.Layer(LayerMask.LayerToName(i), i));
                    }

                    RuntimeTextAsset layersTextAsset = ScriptableObject.CreateInstance<RuntimeTextAsset>();
                    layersTextAsset.Text = JsonUtility.ToJson(m_loadedLayers);

                    ProjectAsyncOperation setLayersInfoAo = project.SetValue("Battlehub.RTEditor.LayersInfo", layersTextAsset);
                    yield return setLayersInfoAo;
                    if (setLayersInfoAo.HasError)
                    {
                        Debug.LogErrorFormat("Unable to set LayersInfo {0}", setLayersInfoAo.Error);
                        yield break;
                    }
                }
                else
                {
                    m_loadedLayers = ScriptableObject.CreateInstance<LayersInfo>();
                    JsonUtility.FromJsonOverwrite(getLayersInfoAo.Result.Text, m_loadedLayers);

                    foreach (LayersInfo.Layer layer in m_loadedLayers.Layers)
                    {
                        if(string.IsNullOrEmpty(layer.Name))
                        {
                            layer.Name = LayerMask.LayerToName(layer.Index);
                        }
                    }
                }
            }

            callback(m_loadedLayers);
        }

        private IEnumerator CoEndEdit()
        {
            IProject project = IOC.Resolve<IProject>();
            if(project == null)
            {
                yield break;
            }

            RuntimeTextAsset layersTextAsset = ScriptableObject.CreateInstance<RuntimeTextAsset>();
            layersTextAsset.Text = JsonUtility.ToJson(m_layersInfo);
            
            ProjectAsyncOperation setLayersInfoAo = project.SetValue("Battlehub.RTEditor.LayersInfo", layersTextAsset);
            yield return setLayersInfoAo;
            if (setLayersInfoAo.HasError)
            {
                Debug.LogErrorFormat("Unable to set LayersInfo {0}", setLayersInfoAo.Error);
                yield break;
            }
        }
    }
}
