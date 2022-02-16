using Battlehub.RTEditor;
using UnityEngine;

namespace Battlehub.UIControls
{
    public class UIEditorStyle : UIMenuStyle
    {
        public static void ApplyTimelineControlBackgroundColor(UIStyle style, Color background)
        {
            TimelineControl timelineControl = style.GetComponent<TimelineControl>();
            if(timelineControl != null)
            {
                timelineControl.BackgroundColor = background;
            }
        }

        public static void ApplyHierarchyColors(UIStyle style, Color enabledItem, Color disabledItem)
        {
            HierarchyView hierarchy = style.GetComponent<HierarchyView>();
            if (hierarchy != null)
            {
                hierarchy.EnabledItemColor = enabledItem;
                hierarchy.DisabledItemColor = disabledItem;
            }
        }

        public static void ApplyToolCmdItemColor(UIStyle style, Color normalColor, Color pointerOverColor, Color pressedColor)
        {
            ToolCmdItem cmdItem = style.GetComponent<ToolCmdItem>();
            if(cmdItem != null)
            {
                cmdItem.NormalColor = normalColor;
                cmdItem.PointerOverColor = pointerOverColor;
                cmdItem.PressedColor = pressedColor;
            }
        }
    }
}