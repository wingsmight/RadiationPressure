using Battlehub.RTCommon;
using Battlehub.RTHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Battlehub.RTEditor
{
    public enum SystemOfMeasurement
    {
        Metric,
        Imperial
    }

    [Serializable]
    public struct InspectorSettings
    {
        [Serializable]
        public struct ComponentEditorSettings
        {
            public bool ShowResetButton;
            public bool ShowExpander;
            public bool ShowEnableButton;
            public bool ShowRemoveButton;

            public ComponentEditorSettings(bool showExpander, bool showResetButton, bool showEnableButton, bool showRemoveButton)
            {
                ShowResetButton = showResetButton;
                ShowExpander = showExpander;
                ShowEnableButton = showEnableButton;
                ShowRemoveButton = showRemoveButton;
            }
        }

        public ComponentEditorSettings ComponentEditor;
        public bool ShowAddComponentButton;

        public InspectorSettings(ComponentEditorSettings componentEditorSettings, bool showAddComponentButton)
        {
            ComponentEditor = componentEditorSettings;
            ShowAddComponentButton = showAddComponentButton;
        }

        public InspectorSettings(bool showAddComponentButton)
        {
            ComponentEditor = new ComponentEditorSettings(true, true, true, true);
            ShowAddComponentButton = showAddComponentButton;
        }
    }

    [Serializable]
    public class BuiltInWindowsSettings
    {
        public static readonly BuiltInWindowsSettings Default = new BuiltInWindowsSettings();

        public InspectorSettings Inspector;
        public BuiltInWindowsSettings()
        {
            Inspector = new InspectorSettings(true);
        }
    }

    public enum GraphicsQuality
    {
        High,
        Medium,
        Low
    }

    public interface ISettingsComponent
    {
        event EventHandler ResetToDefaultsEvent;

        bool IsGridVisible
        {
            get;
            set;
        }

        float GridOpacity
        {
            get;
            set;
        }

        bool IsGridEnabled
        {
            get;
            set;
        }

        bool GridZTest
        {
            get;
            set;
        }

        float GridSize
        {
            get;
            set;
        }

        bool UIAutoScale
        {
            get;
            set;
        }

        float UIScale
        {
            get;
            set;
        }

        void EndEditUIScale();

        float FreeRotationSmoothSpeed
        {
            get;
            set;
        }

        bool RotationInvertX
        {
            get;
            set;
        }

        bool RotationInvertY
        {
            get;
            set;
        }

        float FreeMovementSmoothSpeed
        {
            get;
            set;
        }
        
        float ZoomSpeed
        {
            get;
            set;
        }

        bool ConstantZoomSpeed
        {
            get;
            set;
        }

        event Action SystemOfMeasurementChanged;
        SystemOfMeasurement SystemOfMeasurement
        {
            get;
            set;
        }

        GraphicsQuality GraphicsQuality
        {
            get;
            set;
        }

        Color LightColor
        {
            get;
            set;
        }
             
        float LightIntensity
        {
            get;
            set;
        }
 
        LightShadows ShadowType
        {
            get;
            set;
        }
      
        float ShadowStrength
        {
            get;
            set;
        }


        int SelectedThemeIndex
        {
            get;
            set;
        }

        ThemeAsset[] Themes
        {
            get;
        }


        BuiltInWindowsSettings BuiltInWindowsSettings
        {
            get;
        }

        IList<GameObject> CustomSettings
        {
            get;
        }

 
        void ResetToDefaults();
        void RegisterCustomSettings(GameObject prefab);
        void UnregsiterCustomSettings(GameObject prefab);
    }

    [DefaultExecutionOrder(-1)]
    public class SettingsComponent : MonoBehaviour, ISettingsComponent
    {
        public event EventHandler ResetToDefaultsEvent;

        private IWindowManager m_wm;

        private Dictionary<Transform, IRuntimeSceneComponent> m_sceneComponents = new Dictionary<Transform, IRuntimeSceneComponent>();

        [SerializeField]
        private BuiltInWindowsSettings m_windowSettingsDefault = new BuiltInWindowsSettings();
        public BuiltInWindowsSettings BuiltInWindowsSettings
        {
            get { return m_windowSettingsDefault; }
        }

        [SerializeField]
        private bool m_isGridVisibleDefault = true;
        public bool IsGridVisible
        {
            get { return GetBool("IsGridVisible", m_isGridVisibleDefault); }
            set
            {
                SetBool("IsGridVisible", value);
                foreach(IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.IsGridVisible = value;
                }
            }
        }

        [SerializeField]
        private float m_gridOpacity = 0.33f;
        public float GridOpacity
        {
            get { return GetFloat("GridOpacity", m_gridOpacity); }
            set
            {
                SetFloat("GridOpacity", value);
                ApplyGridOpacity(value);
            }
        }

        private void ApplyGridOpacity(float value)
        {
            IRuntimeHandlesComponent handlesComponent = IOC.Resolve<IRuntimeHandlesComponent>();
            Color gridColor = handlesComponent.Colors.GridColor;
            gridColor.a = value;
            handlesComponent.Colors.GridColor = gridColor;
            foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
            {
                sceneComponent.GridZTest = !sceneComponent.GridZTest;
                sceneComponent.GridZTest = !sceneComponent.GridZTest;
            }
        }

        [SerializeField]
        private bool m_isGridEnabledDefault = false;
        public bool IsGridEnabled
        {
            get { return GetBool("IsGridEnabled", m_isGridEnabledDefault); }
            set
            {
                SetBool("IsGridEnabled", value);
                foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.IsGridEnabled = value;
                }
            }
        }

        [SerializeField]
        private bool m_gridZTest = true;
        public bool GridZTest
        {
            get { return GetBool("GridZTest", m_gridZTest); }
            set
            {
                SetBool("GridZTest", value);
                foreach(IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.GridZTest = value;
                }
            }
        }

        [SerializeField]
        private float m_gridSizeDefault = 0.5f;
        public float GridSize
        {
            get { return GetFloat("GridSize", m_gridSizeDefault); }
            set
            {
                SetFloat("GridSize", value);
                foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.SizeOfGrid = value;
                }
            }
        }

        [SerializeField]
        private bool m_uiAutoScaleDefault = true;
        public bool UIAutoScale
        {
            get { return GetBool("UIAutoScale", m_uiAutoScaleDefault); }
            set
            {                
                SetBool("UIAutoScale", value);
                EndEditUIScale();
            }
        }

        [SerializeField]
        private float m_uiScaleDefault = 1.0f;
        public float UIScale
        {
            get { return GetFloat("UIScale", m_uiScaleDefault); }
            set
            {
                if(value < 0)
                {
                    DeleteKey("UIScale");
                }
                else
                {
                    SetFloat("UIScale", value);
                }
            }
        }

        public void EndEditUIScale()
        {
            float scale = 1.0f;
            if (UIAutoScale)
            {
                if (!Application.isEditor)
                {
                    scale = Mathf.Clamp((float)System.Math.Round(Display.main.systemWidth / 1920.0f, 1), 0.5f, 4);
                }
            }
            else
            {
                scale = UIScale;
            }

            IRTEAppearance appearance = IOC.Resolve<IRTEAppearance>();
            appearance.UIScale = scale;

            IRuntimeHandlesComponent handles = IOC.Resolve<IRuntimeHandlesComponent>();
            handles.HandleScale = scale;
            handles.SceneGizmoScale = scale;
        }

        [SerializeField]
        private float m_freeMovementSmoothSpeed = 10.0f;
        public float FreeMovementSmoothSpeed
        {
            get { return GetFloat("FreeMovementSmoothSpeed", m_freeMovementSmoothSpeed); }
            set
            {
                SetFloat("FreeMovementSmoothSpeed", value);
                foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.FreeMovementSmoothSpeed = value;
                }
            }
        }

        [SerializeField]
        private float m_freeRotationSmoothSpeed = 10.0f;
        public float FreeRotationSmoothSpeed
        {
            get { return GetFloat("FreeRotationSmoothSpeed", m_freeRotationSmoothSpeed); }
            set
            {
                SetFloat("FreeRotationSmoothSpeed", value);
                foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.FreeRotationSmoothSpeed = value;
                }
            }
        }

        [SerializeField]
        private bool m_rotationInvertX = false;
        public bool RotationInvertX
        {
            get { return GetBool("RotationInvertX", m_rotationInvertX); }
            set
            {
                SetBool("RotationInvertX", value);
                foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.RotationInvertX = value;
                }
            }
        }

        [SerializeField]
        private bool m_rotationInvertY = false;
        public bool RotationInvertY
        {
            get { return GetBool("RotationInvertY", m_rotationInvertY); }
            set
            {
                SetBool("RotationInvertY", value);
                foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.RotationInvertY = value;
                }
            }
        }

        [SerializeField]
        private float m_zoomSpeedDefault = 5.0f;
        public float ZoomSpeed
        {
            get { return GetFloat("ZoomSpeed", m_zoomSpeedDefault); }
            set
            {
                SetFloat("ZoomSpeed", value);
                foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.ZoomSpeed = value;
                }
            }
        }

        [SerializeField]
        private bool m_constantZoomSpeedDefault = false;
        public bool ConstantZoomSpeed
        {
            get { return GetBool("ConstantZoomSpeed", m_constantZoomSpeedDefault); }
            set
            {
                SetBool("ConstantZoomSpeed", value);
                foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.ConstantZoomSpeed = value;
                }
            }
        }


        public event Action SystemOfMeasurementChanged;
        [SerializeField]
        private SystemOfMeasurement m_systemOfMeasurementDefault = SystemOfMeasurement.Metric;
        public SystemOfMeasurement SystemOfMeasurement
        {
            get { return (SystemOfMeasurement)GetInt("SystemOfMeasurement", (int)m_systemOfMeasurementDefault); }
            set
            {
                SetInt("SystemOfMeasurement", (int)value);
                foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
                {
                    sceneComponent.RectTool.Metric = SystemOfMeasurement == SystemOfMeasurement.Metric;
                }
                if(SystemOfMeasurementChanged != null)
                {
                    SystemOfMeasurementChanged();
                }
            }
        }

        private GraphicsQuality m_graphicsQualityDefault = GraphicsQuality.High;
        public GraphicsQuality GraphicsQuality
        {
            get { return (GraphicsQuality)GetInt("GraphicsQuality", (int)m_graphicsQualityDefault); }
            set
            {
                SetInt("GraphicsQuality", (int)value);

                if (RenderPipelineInfo.Type == RPType.URP)
                {
                    RenderPipelineAsset pipelineAsset = null;
                    int qualityLevel = 0;
                    switch (GraphicsQuality)
                    {
                        case GraphicsQuality.High:
                            pipelineAsset = Resources.Load<RenderPipelineAsset>("HighQuality_UniversalRenderPipelineAsset");
                            qualityLevel = QualitySettings.names.Length - 1;
                            break;
                        case GraphicsQuality.Medium:
                            pipelineAsset = Resources.Load<RenderPipelineAsset>("MidQuality_UniversalRenderPipelineAsset");
                            qualityLevel = (QualitySettings.names.Length - 1) / 2;
                            break;
                        case GraphicsQuality.Low:
                            pipelineAsset = Resources.Load<RenderPipelineAsset>("LowQuality_UniversalRenderPipelineAsset");
                            qualityLevel = 0;
                            break;
                    }

                    GraphicsSettings.renderPipelineAsset = pipelineAsset;
                    QualitySettings.SetQualityLevel(qualityLevel);
                    QualitySettings.renderPipeline = pipelineAsset;
                }

                foreach (RenderTextureCamera renderTextureCamera in FindObjectsOfType<RenderTextureCamera>())
                {
                    renderTextureCamera.ResizeRenderTexture();
                }

                StartCoroutine(CoUpdateGizmos());
            }
        }

        private Color m_lightColorDefault = Color.white;
        public Color LightColor
        {
            get { return GetColor("LightColor", m_lightColorDefault); }
            set
            {
                SetColor("LightColor", value);
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if(light.type != LightType.Directional)
                    {
                        continue;
                    }

                    light.color = value;
                }
            }
        }

        private float m_ligthIntensityDefault;
        public float LightIntensity
        {
            get { return GetFloat("LightIntensity", m_ligthIntensityDefault); }
            set
            {
                SetFloat("LightIntensity", value >= 0 ? value : 0);
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if (light.type != LightType.Directional)
                    {
                        continue;
                    }

                    light.intensity = value;
                }
            }
        }

        private LightShadows m_shadowTypeDefault = LightShadows.Soft;
        public LightShadows ShadowType
        {
            get { return (LightShadows)GetInt("ShadowType", (int)m_shadowTypeDefault); }
            set
            {
                SetInt("ShadowType", (int)value);
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if (light.type != LightType.Directional)
                    {
                        continue;
                    }

                    light.shadows = value;
                }
            }
        }

        private float m_shadowStrengthDefault;
        public float ShadowStrength
        {
            get { return GetFloat("ShadowStrength", m_shadowStrengthDefault); }
            set
            {
                SetFloat("ShadowStrength", value);
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if (light.type != LightType.Directional)
                    {
                        continue;
                    }

                    light.shadowStrength = value;
                }
            }
        }

        public int SelectedThemeIndex
        {
            get { return GetInt("SelectedThemeIndex", 0); }
            set
            {
                SetInt("SelectedThemeIndex", value);
                ApplySettings();
            }
        }

        public ThemeAsset[] Themes
        {
            get;
            private set;
        }

        private IEnumerator CoUpdateGizmos()
        {
            SceneGizmo[] gizmos = FindObjectsOfType<SceneGizmo>();
            foreach (SceneGizmo gizmo in gizmos)
            {
                gizmo.UpdateLayout();
                gizmo.DoSceneGizmo();
            }

            yield return new WaitForEndOfFrame();

            foreach (SceneGizmo gizmo in gizmos)
            {
                gizmo.DoSceneGizmo();
            }
        }

        private List<GameObject> m_customSettings = new List<GameObject>();
        public IList<GameObject> CustomSettings
        {
            get { return m_customSettings; }
        }

        private void Awake()
        {
            if(RenderPipelineInfo.Type == RPType.HDRP)
            {
                m_ligthIntensityDefault = 10000;
                m_shadowStrengthDefault = 1;
            }
            else if(RenderPipelineInfo.Type == RPType.URP)
            {
                m_ligthIntensityDefault = 2;
                m_shadowStrengthDefault = 0.75f;
            }
            else
            {
                m_ligthIntensityDefault = 1;
                m_shadowStrengthDefault = 1;
            }
            
            //simplified user's hardware configuration evaluation
            if (SystemInfo.graphicsMemorySize > 4096)
            {
                m_graphicsQualityDefault = GraphicsQuality.High;
            }
            else if(SystemInfo.graphicsMemorySize > 2048)
            {
                m_graphicsQualityDefault = GraphicsQuality.Medium;
            }
            else
            {
                m_graphicsQualityDefault = GraphicsQuality.Low;
            }

            Themes = Resources.LoadAll("Themes", typeof(ThemeAsset)).OfType<ThemeAsset>().OrderBy(a => a.name != "Dark").ToArray();

            IOC.RegisterFallback<ISettingsComponent>(this);
            m_wm = IOC.Resolve<IWindowManager>();
            m_wm.AfterLayout += OnAfterLayout;
            m_wm.WindowCreated += OnWindowCreated;
            m_wm.WindowDestroyed += OnWindowDestoryed;
        }

        private void OnDestroy()
        {
            IOC.UnregisterFallback<ISettingsComponent>(this);

            if(m_wm != null)
            {
                m_wm.AfterLayout -= OnAfterLayout;
                m_wm.WindowCreated -= OnWindowCreated;
                m_wm.WindowDestroyed -= OnWindowDestoryed;
            }
        }

        private void OnWindowCreated(Transform windowTransform)
        {
            RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
            if(window != null && window.WindowType == RuntimeWindowType.Scene)
            {
                IRuntimeSceneComponent sceneComponent = window.IOCContainer.Resolve<IRuntimeSceneComponent>();
                if(sceneComponent != null)
                {
                    m_sceneComponents.Add(windowTransform, sceneComponent);
                    ApplySettings(sceneComponent);
                }

            }
        }

        private void OnWindowDestoryed(Transform windowTransform)
        {
            RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
            if(window != null && window.WindowType == RuntimeWindowType.Scene)
            {
                m_sceneComponents.Remove(windowTransform);
            }
        }

        private void OnAfterLayout(IWindowManager obj)
        {
            Transform[] sceneWindows = m_wm.GetWindows(RuntimeWindowType.Scene.ToString());
            for (int i = 0; i < sceneWindows.Length; ++i)
            {
                Transform windowTransform = sceneWindows[i];
                RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
                if (window != null)
                {
                    IRuntimeSceneComponent sceneComponent = window.IOCContainer.Resolve<IRuntimeSceneComponent>();
                    if (sceneComponent != null)
                    {
                        m_sceneComponents.Add(windowTransform, sceneComponent);
                    }
                }
            }

            ApplySettings();
        }

        private void ApplySettings(IRuntimeSceneComponent sceneComponent)
        {
            sceneComponent.IsGridVisible = IsGridVisible;
            sceneComponent.IsGridEnabled = IsGridEnabled;
            sceneComponent.SizeOfGrid = GridSize;
            sceneComponent.GridZTest = GridZTest;
            sceneComponent.FreeRotationSmoothSpeed = FreeRotationSmoothSpeed;
            sceneComponent.RotationInvertX = RotationInvertX;
            sceneComponent.RotationInvertY = RotationInvertY;
            sceneComponent.FreeMovementSmoothSpeed = FreeMovementSmoothSpeed;
            sceneComponent.ZoomSpeed = ZoomSpeed;
            sceneComponent.ConstantZoomSpeed = ConstantZoomSpeed;
            sceneComponent.RectTool.Metric = SystemOfMeasurement == SystemOfMeasurement.Metric;
        }


        private void ApplySettings()
        {
            ApplyGridOpacity(GridOpacity);

            foreach (IRuntimeSceneComponent sceneComponent in m_sceneComponents.Values)
            {
                ApplySettings(sceneComponent);
            }

            GraphicsQuality = GraphicsQuality;

            Light[] lights = FindObjectsOfType<Light>();
            foreach(Light light in lights)
            {
                if(light.type != LightType.Directional)
                {
                    continue;
                }

                light.color = LightColor;
                light.intensity = LightIntensity;
                light.shadows = ShadowType;
                light.shadowStrength = ShadowStrength;
            }

            EndEditUIScale();

            int themeIndex = SelectedThemeIndex;
            if (Themes != null && 0 <= themeIndex && themeIndex < Themes.Length)
            {
                IRTEAppearance appearance = IOC.Resolve<IRTEAppearance>();
                appearance.Colors = Themes[themeIndex].Colors;
                appearance.CursorSettings = Themes[themeIndex].Cursors;
            }
        }

        public void ResetToDefaults()
        {
            DeleteKey("IsGridVisible");
            DeleteKey("IsGridEnabled");
            DeleteKey("GridSize");
            DeleteKey("GridOpacity");
            DeleteKey("GridZTest");
            DeleteKey("UIAutoScale");
            DeleteKey("UIScale");
            DeleteKey("FreeRotationSmoothSpeed");
            DeleteKey("RotationInvertX");
            DeleteKey("RotationInvertY");
            DeleteKey("FreeMovementSmoothSpeed");
            DeleteKey("ZoomSpeed");
            DeleteKey("ConstantZoomSpeed");
            DeleteKey("SystemOfMeasurement");
            DeleteKey("GraphicsQuality");
            DeleteKey("LightColor");
            DeleteKey("LightIntensity");
            DeleteKey("ShadowType");
            DeleteKey("ShadowStrength");
            DeleteKey("SelectedThemeIndex");

            if (ResetToDefaultsEvent != null)
            {
                ResetToDefaultsEvent(this, EventArgs.Empty);
            }

            ApplySettings();
        }

        public void RegisterCustomSettings(GameObject prefab)
        {
            m_customSettings.Add(prefab);
        }

        public void UnregsiterCustomSettings(GameObject prefab)
        {
            m_customSettings.Remove(prefab);
        }

        private const string KeyPrefix = "Battlehub.RTEditor.Settings.";

        private void DeleteKey(string propertyName)
        {
            PlayerPrefs.DeleteKey(KeyPrefix + propertyName);
        }

        private void SetColor(string propertyName, Color color)
        {
            SetString(propertyName, color.r + ":" + color.g + ":" + color.b + ":" + color.a);
        }

        private Color GetColor(string propertyName, Color color)
        {
            string colorStr = GetString(propertyName, color.r + ":" + color.g + ":" + color.b + ":" + color.a);
            string[] c = colorStr.Split(':');
            return new Color(float.Parse(c[0]), float.Parse(c[1]), float.Parse(c[2]), float.Parse(c[3]));
        }

        private void SetString(string propertyName, string value)
        {
            PlayerPrefs.SetString(KeyPrefix + propertyName, value);
        }

        private string GetString(string propertyName, string defaultValue)
        {
            return PlayerPrefs.GetString(KeyPrefix + propertyName, defaultValue);
        }

        private void SetFloat(string propertyName, float value)
        {
            PlayerPrefs.SetFloat(KeyPrefix + propertyName, value);
        }

        private float GetFloat(string propertyName, float defaultValue)
        {
            return PlayerPrefs.GetFloat(KeyPrefix + propertyName, defaultValue);
        }

        private void SetInt(string propertyName, int value)
        {
            PlayerPrefs.SetInt(KeyPrefix + propertyName, value);
        }

        private int GetInt(string propertyName, int defaultValue)
        {
            return PlayerPrefs.GetInt(KeyPrefix + propertyName, defaultValue);
        }

        private void SetBool(string propertyName, bool value)
        {
            PlayerPrefs.SetInt(KeyPrefix + propertyName, value ? 1 : 0);
        }

        private bool GetBool(string propertyName, bool defaultValue)
        {
            return PlayerPrefs.GetInt(KeyPrefix + propertyName, defaultValue ? 1 : 0) != 0;
        }
    }
}

