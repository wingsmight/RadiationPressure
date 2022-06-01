using Battlehub.RTCommon;
using Battlehub.RTHandles;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public interface IGameObjectCmd
    {
        bool CanExec(string cmd);
        void Exec(string cmd);
    }

    public class GameObjectCmd : MonoBehaviour, IGameObjectCmd
    {
        private IRuntimeEditor m_editor;
        private ILocalization m_localization;

        [SerializeField]
        private Material m_defaultMaterial = null;


        private void Awake()
        {
            m_editor = IOC.Resolve<IRuntimeEditor>();
            m_localization = IOC.Resolve<ILocalization>();

            if (RenderPipelineInfo.Type != RPType.Standard)
            {
                m_defaultMaterial = RenderPipelineInfo.DefaultMaterial;
            }
        }

        public bool CanExec(string cmd)
        {
            return true;
        }

        public void Exec(string cmd)
        {
            cmd = cmd.ToLower();
            GameObject go = null;
            switch (cmd)
            {
                case "createempty":
                    go = new GameObject();
                    go.name = m_localization.GetString("ID_RTEditor_GameObjectCmd_Empty", "Empty");
                    break;
                case "createemptychild":
                    go = new GameObject();
                    go.name = "Empty";
                    IRuntimeSelection selection = m_editor.Selection;
                    go.transform.SetParent(selection.activeTransform, false);
                    break;
                case "cube":
                    go = DetailLoading.Load(cmd);
                    break;
                case "cylinder":
                    go = DetailLoading.Load(cmd);
                    break;
                case "prism":
                    go = DetailLoading.Load(cmd);
                    break;
                case "directionallight":
                    {
                        go = new GameObject();
                        go.name = m_localization.GetString("ID_RTEditor_GameObjectCmd_DirectionalLight", "Directional Light");
                        Light light = go.AddComponent<Light>();
                        light.type = LightType.Directional;
                    }
                    break;
                case "pointlight":
                    {
                        go = new GameObject();
                        go.name = m_localization.GetString("ID_RTEditor_GameObjectCmd_PointLight", "Point Light");
                        Light light = go.AddComponent<Light>();
                        light.type = LightType.Point;
                    }
                    break;
                case "spotlight":
                    {
                        go = new GameObject();
                        go.name = m_localization.GetString("ID_RTEditor_GameObjectCmd_SpotLight", "Spot Light");
                        Light light = go.AddComponent<Light>();
                        light.type = LightType.Spot;
                    }
                    break;
                case "camera":
                    {
                        go = new GameObject();
                        go.SetActive(false);
                        go.name = m_localization.GetString("ID_RTEditor_GameObjectCmd_Camera", "Camera");
                        go.AddComponent<Camera>();
                        go.AddComponent<GameViewCamera>();
                    }
                    break;
            }

            if (go != null)
            {
                m_editor.AddGameObjectToScene(go);
            }

        }
    }

    public static class DetailLoading
    {
        private const string PATH = "Details";


        public static GameObject Load(string name)
        {
            var detial = Resources.Load<GameObject>($"{PATH}/{name}");
            GameObject detailObject = GameObject.Instantiate(detial);
            detailObject.name = name;

            return detailObject;
        }
    }
}
