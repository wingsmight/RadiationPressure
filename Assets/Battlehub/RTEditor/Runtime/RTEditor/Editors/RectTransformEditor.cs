using Battlehub.RTCommon;
using Battlehub.RTHandles;
using Battlehub.Utils;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class RectTransformPropertyConverter
    {
        public Vector3 Pos
        {
            get
            {
                if(RectTransform == null)
                {
                    return Vector3.zero;
                }

                Vector3 aPos = RectTransform.anchoredPosition;

                Vector2 aMin = RectTransform.anchorMin;
                Vector2 aMax = RectTransform.anchorMax;

                Vector2 oMin = RectTransform.offsetMin;
                Vector2 oMax = RectTransform.offsetMax;

                return new Vector3(Approximately(aMin.x, aMax.x) ? aPos.x : oMin.x, Approximately(aMin.y, aMax.y) ? aPos.y : -oMax.y, RectTransform.localPosition.z);
            }
            set
            {
                if (RectTransform == null)
                {
                    return;
                }

                Vector2 aMin = RectTransform.anchorMin;
                Vector2 aMax = RectTransform.anchorMax;

                if(Approximately(aMin.x, aMax.x))
                {
                    Vector3 aPos = RectTransform.anchoredPosition;
                    aPos.x = value.x;
                    RectTransform.anchoredPosition = aPos;
                }
                else
                {
                    Vector2 oMin = RectTransform.offsetMin;
                    oMin.x = value.x;
                    RectTransform.offsetMin = oMin;
                }

                if(Approximately(aMin.y, aMax.y))
                {
                    Vector3 aPos = RectTransform.anchoredPosition;
                    aPos.y = value.y;
                    RectTransform.anchoredPosition = aPos;
                }
                else
                {
                    Vector2 oMax = RectTransform.offsetMax;
                    oMax.y = -value.y;
                    RectTransform.offsetMax = oMax;
                }

                Vector3 localPosition = RectTransform.localPosition;
                localPosition.z = value.z;
                RectTransform.localPosition = localPosition;

            }
        }

        public Vector3 Size
        {
            get
            {
                if(RectTransform == null)
                {
                    return Vector3.zero;
                }

                Vector2 aMin = RectTransform.anchorMin;
                Vector2 aMax = RectTransform.anchorMax;

                Vector2 oMin = RectTransform.offsetMin;
                Vector2 oMax = RectTransform.offsetMax;

                Vector3 size = RectTransform.sizeDelta;

                return new Vector3(Approximately(aMin.x, aMax.x) ? size.x : -oMax.x, Approximately(aMin.y, aMax.y) ? size.y : oMin.y, 0);
            }
            set
            {
                if (RectTransform == null)
                {
                    return;
                }

                Vector2 aMin = RectTransform.anchorMin;
                Vector2 aMax = RectTransform.anchorMax;

                if (Approximately(aMin.x, aMax.x))
                {
                    Vector3 size = RectTransform.sizeDelta;
                    size.x = value.x;
                    RectTransform.sizeDelta = size;
                }
                else
                {
                    Vector2 oMax = RectTransform.offsetMax;
                    oMax.x = -value.x;
                    RectTransform.offsetMax = oMax;
                }

                if (Approximately(aMin.y, aMax.y))
                {
                    Vector3 size = RectTransform.sizeDelta;
                    size.y = value.y;
                    RectTransform.sizeDelta = size;
                }
                else
                {
                    Vector2 oMin = RectTransform.offsetMin;
                    oMin.y = value.y;
                    RectTransform.offsetMin = oMin;
                }
            }
        }

        public Vector3 LocalEuler
        {
            get
            {
                if (ExposeToEditor == null)
                {
                    return Vector3.zero;
                }

                return ExposeToEditor.LocalEuler;
            }
            set
            {
                if (ExposeToEditor == null)
                {
                    return;
                }
                ExposeToEditor.LocalEuler = value;
            }
        }

        public Vector3 LocalScale
        {
            get
            {
                if (ExposeToEditor == null)
                {
                    return Vector3.zero;
                }

                return ExposeToEditor.LocalScale;
            }
            set
            {
                if (ExposeToEditor == null)
                {
                    return;
                }
                ExposeToEditor.LocalScale = value;
            }
        }


        public RectTransform RectTransform
        {
            get;
            set;
        }

        public ExposeToEditor ExposeToEditor
        {
            get;
            set;
        }

        public static bool Approximately(float a, float b)
        {
            return Mathf.Approximately(a, b);
        }
    }

    public class RectTransformEditor : ComponentEditor
    {
        [SerializeField]
        private AnchorPresetSelector m_presetSelector = null;
        [SerializeField]
        private Vector3Editor m_posEditor = null;
        [SerializeField]
        private Vector3Editor m_sizeEditor = null;
        [SerializeField]
        private Vector2Editor m_anchorMinEditor = null;
        [SerializeField]
        private Vector2Editor m_anchorMaxEditor = null;
        [SerializeField]
        private Vector2Editor m_pivotEditor = null;
        [SerializeField]
        private Vector3Editor m_rotationEditor = null;
        [SerializeField]
        private Vector3Editor m_scaleEditor = null;
        [SerializeField]
        private TextMeshProUGUI m_posXLabel = null;
        [SerializeField]
        private TextMeshProUGUI m_posYLabel = null;
        [SerializeField]
        private TextMeshProUGUI m_posZLabel = null;
        [SerializeField]
        private TextMeshProUGUI m_widthLabel = null;
        [SerializeField]
        private TextMeshProUGUI m_heightLabel = null;
        
        private ILocalization m_lc;

        protected override void Awake()
        {
            base.Awake();
            m_lc = IOC.Resolve<ILocalization>();
            m_presetSelector.Captions = new AnchorPresetSelector.AlignmentCaptions
            {
                Left = m_lc.GetString("ID_RTEditor_AnchorPreset_Left"),
                Center = m_lc.GetString("ID_RTEditor_AnchorPreset_Center"),
                Right = m_lc.GetString("ID_RTEditor_AnchorPreset_Right"),
                Top = m_lc.GetString("ID_RTEditor_AnchorPreset_Top"),
                Middle = m_lc.GetString("ID_RTEditor_AnchorPreset_Middle"),
                Bottom = m_lc.GetString("ID_RTEditor_AnchorPreset_Bottom"),
                Stretch = m_lc.GetString("ID_RTEditor_AnchorPreset_Stretch"),
                Custom = m_lc.GetString("ID_RTEditor_AnchorPreset_Custom"),
            };
            m_presetSelector.Selected += OnAnchorPresetSelected;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(m_presetSelector != null)
            {
                m_presetSelector.Selected -= OnAnchorPresetSelected;
            }
        }

        private object[] CreateConverters(ComponentEditor editor)
        {
            object[] converters = new object[editor.Components.Length];
            Component[] components = editor.Components;
            for (int i = 0; i < components.Length; ++i)
            {
                RectTransform rt = (RectTransform)components[i];
                if (rt != null)
                {
                    converters[i] = new RectTransformPropertyConverter
                    {
                        RectTransform = rt,
                        ExposeToEditor = rt.GetComponent<ExposeToEditor>()
                    };
                }
            }
            return converters;
        }

        protected override void BuildEditor(IComponentDescriptor componentDescriptor, PropertyDescriptor[] descriptors)
        {
            object[] converters = CreateConverters(this);

            PropertyDescriptor posDesc = new PropertyDescriptor("", converters, Strong.PropertyInfo((RectTransformPropertyConverter x) => x.Pos));
            PropertyDescriptor sizeDesc = new PropertyDescriptor("", converters, Strong.PropertyInfo((RectTransformPropertyConverter x) => x.Size));
            PropertyDescriptor anchorMinDesc = new PropertyDescriptor(m_lc.GetString("ID_RTEditor_CD_RectTransform_AnchorMin"), Components, Strong.PropertyInfo((RectTransform x) => x.anchorMin));
            PropertyDescriptor anchorMaxDesc = new PropertyDescriptor(m_lc.GetString("ID_RTEditor_CD_RectTransform_AnchorMax"), Components, Strong.PropertyInfo((RectTransform x) => x.anchorMax));
            PropertyDescriptor pivotDesc = new PropertyDescriptor(m_lc.GetString("ID_RTEditor_CD_RectTransform_Pivot"), Components, Strong.PropertyInfo((RectTransform x) => x.pivot));
            PropertyDescriptor rotationDesc = new PropertyDescriptor(m_lc.GetString("ID_RTEditor_CD_RectTransform_Rotation"), converters, Strong.PropertyInfo((RectTransformPropertyConverter x) => x.LocalEuler));
            PropertyDescriptor scaleDesc = new PropertyDescriptor(m_lc.GetString("ID_RTEditor_CD_RectTransform_Scale"), converters, Strong.PropertyInfo((RectTransformPropertyConverter x) => x.LocalScale));

            InitEditor(m_posEditor, posDesc);
            InitEditor(m_sizeEditor, sizeDesc);
            InitEditor(m_anchorMinEditor, anchorMinDesc);
            InitEditor(m_anchorMaxEditor, anchorMaxDesc);
            InitEditor(m_pivotEditor, pivotDesc);
            InitEditor(m_rotationEditor, rotationDesc);
            InitEditor(m_scaleEditor, scaleDesc);

            UpdatePreview();
            UpdatePositionAndSizeLabels();
        }

        protected override void DestroyEditor()
        {
            DestroyGizmos();
        }

        protected override void OnValueReloaded()
        {
            base.OnValueReloaded();

            UpdatePreview();
            UpdatePositionAndSizeLabels();
        }

        protected override void OnValueChanged()
        {
            base.OnValueChanged();
            RefreshTransformHandles();

            UpdatePreview();
            UpdatePositionAndSizeLabels();
        }

        protected override void OnEndEdit()
        {
            base.OnEndEdit();
            ResetTransformHandles();
        }

        protected override void OnResetClick()
        {
            base.OnResetClick();
            ResetTransformHandles();
        }

        private static void RefreshTransformHandles()
        {
            BaseHandle[] handles = FindObjectsOfType<BaseHandle>();
            foreach (BaseHandle handle in handles)
            {
                handle.Refresh();
            }
        }

        private static void ResetTransformHandles()
        {
            BaseHandle[] handles = FindObjectsOfType<BaseHandle>();
            foreach (BaseHandle handle in handles)
            {
                handle.Targets = handle.RealTargets;
            }
        }

        private void UpdatePreview()
        {
            RectTransform rt = (RectTransform)NotNullComponents.FirstOrDefault();
            if(rt != null)
            {
                m_presetSelector.Preview.CopyFrom(rt);
                m_presetSelector.UpdateCaptions();
            }
        }

        private void UpdatePositionAndSizeLabels()
        {
            Vector3 anchorMax = m_anchorMaxEditor.GetValue();
            Vector3 anchorMin = m_anchorMinEditor.GetValue();
            if (RectTransformPropertyConverter.Approximately(anchorMin.x, anchorMax.x))
            {
                m_posXLabel.text = m_lc.GetString("ID_RTEditor_CD_RectTransform_PosX");
                m_widthLabel.text = m_lc.GetString("ID_RTEditor_CD_RectTransform_Width");
            }
            else
            {
                m_posXLabel.text = m_lc.GetString("ID_RTEditor_CD_RectTransform_Left");
                m_widthLabel.text = m_lc.GetString("ID_RTEditor_CD_RectTransform_Right");
            }

            if (RectTransformPropertyConverter.Approximately(anchorMin.y, anchorMax.y))
            {
                m_posYLabel.text = m_lc.GetString("ID_RTEditor_CD_RectTransform_PosY");
                m_heightLabel.text = m_lc.GetString("ID_RTEditor_CD_RectTransform_Height");
            }
            else
            {
                m_posYLabel.text = m_lc.GetString("ID_RTEditor_CD_RectTransform_Top");
                m_heightLabel.text = m_lc.GetString("ID_RTEditor_CD_RectTransform_Bottom");
            }

            m_posZLabel.text = m_lc.GetString("ID_RTEditor_CD_RectTransform_PosZ");
        }

        private void OnAnchorPresetSelected(AnchorPreset preset)
        {
         
            Editor.Undo.BeginRecord();

            for(int i = 0; i < Components.Length; ++i)
            {
                RectTransform rt = Components[i] as RectTransform;
                if(rt == null)
                {
                    continue;
                }

                int index = i;

                Vector3 anchorMinOld = rt.anchorMin;
                Vector3 anchorMaxOld = rt.anchorMax;
                Vector3 anchoredPositionOld = rt.anchoredPosition;
                Vector3 pivotOld = rt.pivot;
                Vector3 sizeDeltaOld = rt.sizeDelta;
                Vector3 offsetMinOld = rt.offsetMin;
                Vector3 offsetMaxOld = rt.offsetMax;

                preset.CopyTo(rt, preset.IsPivotVisible, preset.IsPositionVisible);

                Vector3 anchorMinNew = rt.anchorMin;
                Vector3 anchorMaxNew = rt.anchorMax;
                Vector3 anchoredPositionNew = rt.anchoredPosition;
                Vector3 pivotNew = rt.pivot;
                Vector3 sizeDeltaNew = rt.sizeDelta;
                Vector3 offsetMinNew = rt.offsetMin;
                Vector3 offsetMaxNew = rt.offsetMax;

                Editor.Undo.CreateRecord(redo =>
                {
                    RectTransform rectTransform = Components[index] as RectTransform;
                    rectTransform.anchorMin = anchorMinNew;
                    rectTransform.anchorMax = anchorMaxNew;
                    rectTransform.anchoredPosition = anchoredPositionNew;
                    rectTransform.pivot = pivotNew;
                    rectTransform.sizeDelta = sizeDeltaNew;
                    rectTransform.offsetMin = offsetMinNew;
                    rectTransform.offsetMax = offsetMaxNew;

                    return true;
                }, undo =>
                {
                    RectTransform rectTransform = Components[index] as RectTransform;
                    rectTransform.anchorMin = anchorMinOld;
                    rectTransform.anchorMax = anchorMaxOld;
                    rectTransform.anchoredPosition = anchoredPositionOld;
                    rectTransform.pivot = pivotOld;
                    rectTransform.sizeDelta = sizeDeltaOld;
                    rectTransform.offsetMin = offsetMinOld;
                    rectTransform.offsetMax = offsetMaxOld;
                    return true;
                });
            }

            Editor.Undo.EndRecord();

            RefreshTransformHandles();

        }
    }
}

