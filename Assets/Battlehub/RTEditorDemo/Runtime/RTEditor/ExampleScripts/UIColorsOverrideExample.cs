using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTEditor.Demo
{
    public class UIColorsOverrideExample : EditorExtension
    {
        protected override void OnEditorExist()
        {
            ISettingsComponent settings = IOC.Resolve<ISettingsComponent>();
            settings.SelectedThemeIndex = -1;

            RTEColors colors = new RTEColors();
            colors.Primary = Color.red;

            IRTEAppearance appearance = IOC.Resolve<IRTEAppearance>();
            appearance.Colors = colors; 
        }
    }
}
