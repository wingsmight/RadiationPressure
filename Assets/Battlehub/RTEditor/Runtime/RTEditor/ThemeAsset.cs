﻿using System.Reflection;
using UnityEngine;

namespace Battlehub.RTEditor
{
    [CreateAssetMenu(menuName = "Runtime Editor/Theme Asset")]
    public class ThemeAsset : ScriptableObject
    {
        public RTEColors Colors;
        public RTECursor[] Cursors;

        [UnityEngine.ContextMenu("Invert Colors")]
        private void InvertColors()
        {
            foreach(FieldInfo field in Colors.GetType().GetFields())
            {
                object val = field.GetValue(Colors);
                if(val is Color)
                {
                    Color color = (Color)val;
                    field.SetValue(Colors, Invert(color));
                }
                else if(val is RTESelectableColors)
                {
                    RTESelectableColors colors = (RTESelectableColors)val;
                    colors.Disabled = Invert(colors.Disabled);
                    colors.Highlight = Invert(colors.Highlight);
                    colors.Normal = Invert(colors.Normal);
                    colors.Pressed = Invert(colors.Pressed);
                    colors.Selected = Invert(colors.Selected);
                    field.SetValue(Colors, colors);
                }
                else if(val is RTEHierarchyColors)
                {
                    RTEHierarchyColors colors = (RTEHierarchyColors)val;
                    colors.DisabledItem = Invert(colors.DisabledItem);
                    colors.NormalItem = Invert(colors.NormalItem);
                    field.SetValue(Colors, colors);
                }
                else if(val is RTEMenuItemColors)
                {
                    RTEMenuItemColors colors = (RTEMenuItemColors)val;
                    colors.DisabledSelectionColor = Invert(colors.DisabledSelectionColor);
                    colors.DisabledTextColor = Invert(colors.DisabledTextColor);
                    colors.SelectionColor = Invert(colors.SelectionColor);
                    colors.TextColor = Invert(colors.TextColor);
                    field.SetValue(Colors, colors);
                }
                else
                {
                    Debug.LogWarning("Unknown type " + val.GetType());
                }
            }
        }

        private Color Invert(Color color)
        {
            Color32 color32 = color;
            
            color32.r = (byte)~color32.r;
            color32.g = (byte)~color32.g;
            color32.b = (byte)~color32.b;

            return color32;
        }
    }
}