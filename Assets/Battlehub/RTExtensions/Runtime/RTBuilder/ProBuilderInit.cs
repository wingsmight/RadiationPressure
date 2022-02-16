using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.UIControls.MenuControl;
using UnityEngine;
namespace Battlehub.RTBuilder
{
    [MenuDefinition(-1)]
    public class ProBuilderInit : EditorExtension
    {
        [SerializeField]
        private GameObject m_proBuilderWindow = null;

        [SerializeField]
        private GameObject[] m_prefabs = null;

        protected override void OnEditorExist()
        {
            base.OnEditorExist();
            Register();
        }

        private void Register()
        {
            ILocalization lc = IOC.Resolve<ILocalization>();
            lc.LoadStringResources("RTBuilder.StringResources");

            IWindowManager wm = IOC.Resolve<IWindowManager>();
            IRTEAppearance appearance = IOC.Resolve<IRTEAppearance>();
            if (m_proBuilderWindow != null)
            {
                RegisterWindow(wm, "ProBuilder", lc.GetString("ID_RTBuilder_WM_Header_Builder", "Builder"),
                    Resources.Load<Sprite>("hammer-24"), m_proBuilderWindow, false);

                appearance.RegisterPrefab(m_proBuilderWindow);
            }

            foreach(GameObject prefab in m_prefabs)
            {
                if(prefab != null)
                {
                    appearance.RegisterPrefab(prefab);
                }
            }
        }

        private void RegisterWindow(IWindowManager wm, string typeName, string header, Sprite icon, GameObject prefab, bool isDialog)
        {
            wm.RegisterWindow(new CustomWindowDescriptor
            {
                IsDialog = isDialog,
                TypeName = typeName,
                Descriptor = new WindowDescriptor
                {
                    Header = header,
                    Icon = icon,
                    MaxWindows = 1,
                    ContentPrefab = prefab
                }
            });
        }

        [MenuCommand("MenuWindow/ID_RTBuilder_WM_Header_Builder", "", true)]
        public static void OpenProBuilder()
        {
            IWindowManager wm = IOC.Resolve<IWindowManager>();
            wm.CreateWindow("ProBuilder");
        }
    }
}


