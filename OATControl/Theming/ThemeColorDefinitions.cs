using System.Collections.Generic;
using System.Windows.Media;

namespace OATControl.Theming
{
    public class ThemeColorDefinition
    {
        public string Key { get; }
        public string DisplayName { get; }
        public string Group { get; }
        public Color DefaultColor { get; }

        public ThemeColorDefinition(string key, string displayName, string group, Color defaultColor)
        {
            Key = key;
            DisplayName = displayName;
            Group = group;
            DefaultColor = defaultColor;
        }
    }

    public static class ThemeColorDefinitions
    {
        public static readonly IReadOnlyList<ThemeColorDefinition> Colors = new List<ThemeColorDefinition>
        {
            // Primary accent
            new ThemeColorDefinition("AppPrimaryColor", "Primary Accent", "Accent", (Color)ColorConverter.ConvertFromString("#610")),

            // Foreground / text
            new ThemeColorDefinition("AppForegroundColor", "Foreground", "Text", (Color)ColorConverter.ConvertFromString("#D11")),
            new ThemeColorDefinition("AppForegroundStrongColor", "Foreground (Strong)", "Text", (Color)ColorConverter.ConvertFromString("#F22")),
            new ThemeColorDefinition("AppForegroundAccentColor", "Foreground (Accent)", "Text", (Color)ColorConverter.ConvertFromString("#B11")),

            // Backgrounds
            new ThemeColorDefinition("AppWindowBackgroundColor", "Window Background", "Backgrounds", (Color)ColorConverter.ConvertFromString("#500")),
            new ThemeColorDefinition("AppNestedBackgroundColor", "Nested Background", "Backgrounds", (Color)ColorConverter.ConvertFromString("#600")),
            new ThemeColorDefinition("AppInputBackgroundColor", "Input Background", "Backgrounds", (Color)ColorConverter.ConvertFromString("#700")),

            // Title bar
            new ThemeColorDefinition("AppTitleBarBackgroundColor", "Title Bar Background", "Title Bar", (Color)ColorConverter.ConvertFromString("#400")),
            new ThemeColorDefinition("AppTitleBarForegroundColor", "Title Bar Foreground", "Title Bar", (Color)ColorConverter.ConvertFromString("#D11")),
            new ThemeColorDefinition("AppTitleBarButtonHoverColor", "Title Bar Button Hover", "Title Bar", (Color)ColorConverter.ConvertFromString("#FF600000")),
            new ThemeColorDefinition("AppTitleBarButtonPressedColor", "Title Bar Button Pressed", "Title Bar", (Color)ColorConverter.ConvertFromString("#FF701010")),

            // Borders
            new ThemeColorDefinition("AppBorderColor", "Border", "Borders", (Color)ColorConverter.ConvertFromString("#910")),
            new ThemeColorDefinition("AppNestedBorderColor", "Nested Border", "Borders", (Color)ColorConverter.ConvertFromString("#910")),
            new ThemeColorDefinition("AppBorderSubtleColor", "Border (Subtle)", "Borders", (Color)ColorConverter.ConvertFromString("#311")),

            // Button interactive states
            new ThemeColorDefinition("AppButtonBackgroundColor", "Button Background", "Buttons", (Color)ColorConverter.ConvertFromString("#700")),
            new ThemeColorDefinition("AppButtonBorderColor", "Button Border", "Buttons", (Color)ColorConverter.ConvertFromString("#B11")),
            new ThemeColorDefinition("AppButtonPressedColor", "Button Pressed", "Buttons", (Color)ColorConverter.ConvertFromString("#600")),
            new ThemeColorDefinition("AppButtonHoverColor", "Button Hover", "Buttons", (Color)ColorConverter.ConvertFromString("#800")),

            // Tooltip
            new ThemeColorDefinition("AppTooltipBackgroundColor", "Tooltip Background", "Tooltip", (Color)ColorConverter.ConvertFromString("#600")),
            new ThemeColorDefinition("AppTooltipHeaderColor", "Tooltip Header", "Tooltip", (Color)ColorConverter.ConvertFromString("#700")),
            new ThemeColorDefinition("AppTooltipForegroundColor", "Tooltip Foreground", "Tooltip", (Color)ColorConverter.ConvertFromString("#A11")),

            // Disabled
            new ThemeColorDefinition("AppDisabledBackgroundColor", "Disabled Background", "Disabled", (Color)ColorConverter.ConvertFromString("#600000")),
            new ThemeColorDefinition("AppDisabledBorderColor", "Disabled Border", "Disabled", (Color)ColorConverter.ConvertFromString("#822")),
            new ThemeColorDefinition("AppDisabledForegroundColor", "Disabled Foreground", "Disabled", (Color)ColorConverter.ConvertFromString("#822")),

            // Toggle switch
            new ThemeColorDefinition("AppToggleOffColor", "Toggle Off", "Toggle", (Color)ColorConverter.ConvertFromString("#611")),
            new ThemeColorDefinition("AppToggleOnColor", "Toggle On", "Toggle", (Color)ColorConverter.ConvertFromString("#610")),
            new ThemeColorDefinition("AppToggleThumbColor", "Toggle Thumb", "Toggle", (Color)ColorConverter.ConvertFromString("#C11")),
            new ThemeColorDefinition("AppToggleBorderColor", "Toggle Border", "Toggle", (Color)ColorConverter.ConvertFromString("#D11")),
        };

        public static string BrushKeyFromColorKey(string colorKey)
        {
            return colorKey.Replace("Color", "Brush");
        }
    }
}
