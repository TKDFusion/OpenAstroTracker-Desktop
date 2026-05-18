# Remove MahApps.Metro — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the MahApps.Metro dependency from OATControl and replace it with a custom theming system that supports switching between named themes at runtime.

**Architecture:** A `ThemeManager` singleton swaps `ResourceDictionary` instances at runtime. All controls reference theme brushes via `DynamicResource`. A `ThemedWindow` base class replaces `MetroWindow` using `WindowChrome`. Theme files define ~20 semantic brush keys each. Legacy MahApps brush keys are provided as a migration shim and removed in the final cleanup phase.

**Tech Stack:** WPF (.NET Framework 4.7.2), no new dependencies. `WindowChrome` from PresentationFramework (already available).

**Design spec:** `docs/superpowers/specs/2026-05-03-remove-mahapps-theming-design.md`

---

## Phase 1: Theme Infrastructure

### Task 1: Create ThemeManager

**Files:**
- Create: `OATControl/Theming/ThemeManager.cs`

- [ ] **Step 1: Create the ThemeManager class**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace OATControl.Theming
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private ResourceDictionary _currentThemeDictionary;
        private const string ThemeDictionaryPrefix = "pack://application:,,,/OATControl;component/Resources/Themes/";

        public static ThemeManager Instance => _instance ?? (_instance = new ThemeManager());

        public List<string> AvailableThemes { get; } = new List<string>
        {
            "DarkAstronomy",
            "Daylight"
        };

        public string CurrentTheme { get; private set; }

        private ThemeManager() { }

        public void ApplyTheme(string themeName)
        {
            if (!AvailableThemes.Contains(themeName))
                throw new ArgumentException($"Theme '{themeName}' is not available.");

            var mergedDicts = Application.Current.Resources.MergedDictionaries;

            // Remove previous theme dictionary
            if (_currentThemeDictionary != null)
            {
                mergedDicts.Remove(_currentThemeDictionary);
            }

            // Load new theme dictionary
            var themeDict = new ResourceDictionary
            {
                Source = new Uri($"{ThemeDictionaryPrefix}{themeName}.xaml")
            };

            // Insert at position 0 so Base.xaml styles can reference the theme brushes
            mergedDicts.Insert(0, themeDict);

            _currentThemeDictionary = themeDict;
            CurrentTheme = themeName;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add OATControl/Theming/ThemeManager.cs
git commit -m "feat: add ThemeManager for runtime theme switching"
```

---

### Task 2: Create DarkAstronomy theme (matching current look)

This is the most critical file — it must reproduce the exact visual appearance of the current app by consolidating all color values from `RedTheme.xaml`, `RedAccent.xaml`, `RedControls.xaml`, and MahApps defaults into a single theme file with semantic keys.

**Files:**
- Create: `OATControl/Resources/Themes/DarkAstronomy.xaml`

- [ ] **Step 1: Create the DarkAstronomy theme file**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">

    <!-- ===== Semantic Theme Keys ===== -->
    <!-- These are the primary keys used throughout the app. Each theme defines the same keys with different colors. -->

    <!-- Primary accent -->
    <Color x:Key="AppPrimaryColor">#FF951400</Color>
    <SolidColorBrush x:Key="AppPrimaryBrush" Color="{StaticResource AppPrimaryColor}" />

    <!-- Foreground / text -->
    <Color x:Key="AppForegroundColor">#FF200000</Color>
    <Color x:Key="AppForegroundStrongColor">#FFFF2020</Color>
    <Color x:Key="AppForegroundSubtleColor">#FF580000</Color>
    <SolidColorBrush x:Key="AppForegroundBrush" Color="{StaticResource AppForegroundColor}" />
    <SolidColorBrush x:Key="AppForegroundStrongBrush" Color="{StaticResource AppForegroundStrongColor}" />
    <SolidColorBrush x:Key="AppForegroundSubtleBrush" Color="{StaticResource AppForegroundSubtleColor}" />

    <!-- Backgrounds -->
    <Color x:Key="AppBackgroundColor">#FF500000</Color>
    <Color x:Key="AppBackgroundAltColor">#FF900000</Color>
    <Color x:Key="AppWindowBackgroundColor">#FF200000</Color>
    <SolidColorBrush x:Key="AppBackgroundBrush" Color="{StaticResource AppBackgroundColor}" />
    <SolidColorBrush x:Key="AppBackgroundAltBrush" Color="{StaticResource AppBackgroundAltColor}" />
    <SolidColorBrush x:Key="AppWindowBackgroundBrush" Color="{StaticResource AppWindowBackgroundColor}" />

    <!-- Borders -->
    <Color x:Key="AppBorderColor">#FF900E00</Color>
    <Color x:Key="AppBorderSubtleColor">#FF380808</Color>
    <SolidColorBrush x:Key="AppBorderBrush" Color="{StaticResource AppBorderColor}" />
    <SolidColorBrush x:Key="AppBorderSubtleBrush" Color="{StaticResource AppBorderSubtleColor}" />

    <!-- Interactive states -->
    <Color x:Key="AppButtonBackgroundColor">#FF401010</Color>
    <Color x:Key="AppButtonHoverColor">#FF501010</Color>
    <Color x:Key="AppButtonPressedColor">#FF701010</Color>
    <SolidColorBrush x:Key="AppButtonBackgroundBrush" Color="{StaticResource AppButtonBackgroundColor}" />
    <SolidColorBrush x:Key="AppButtonHoverBrush" Color="{StaticResource AppButtonHoverColor}" />
    <SolidColorBrush x:Key="AppButtonPressedBrush" Color="{StaticResource AppButtonPressedColor}" />

    <!-- Selection -->
    <Color x:Key="AppSelectedColor">#CC951400</Color>
    <Color x:Key="AppSelectedHoverColor">#99901400</Color>
    <SolidColorBrush x:Key="AppSelectedBrush" Color="{StaticResource AppSelectedColor}" />
    <SolidColorBrush x:Key="AppSelectedHoverBrush" Color="{StaticResource AppSelectedHoverColor}" />

    <!-- Tooltip -->
    <SolidColorBrush x:Key="AppTooltipBackgroundBrush" Color="#FF611" />
    <SolidColorBrush x:Key="AppTooltipForegroundBrush" Color="#FFF" />

    <!-- Semantic state -->
    <SolidColorBrush x:Key="AppWarningBrush" Color="#FFFF8000" />
    <SolidColorBrush x:Key="AppSuccessBrush" Color="#FF842" />
    <SolidColorBrush x:Key="AppDangerBrush" Color="#FFF86" />

    <!-- Disabled -->
    <SolidColorBrush x:Key="AppDisabledBrush" Color="#FF535353" />
    <SolidColorBrush x:Key="AppDisabledForegroundBrush" Color="#FF666666" />

    <!-- Title bar -->
    <SolidColorBrush x:Key="AppTitleBarBackgroundBrush" Color="{StaticResource AppWindowBackgroundColor}" />
    <SolidColorBrush x:Key="AppTitleBarForegroundBrush" Color="{StaticResource AppForegroundColor}" />
    <SolidColorBrush x:Key="AppTitleBarButtonHoverBrush" Color="{StaticResource AppButtonHoverColor}" />
    <SolidColorBrush x:Key="AppTitleBarButtonPressedBrush" Color="{StaticResource AppButtonPressedColor}" />

    <!-- Toggle switch -->
    <SolidColorBrush x:Key="AppToggleOnBrush" Color="{StaticResource AppPrimaryColor}" />
    <SolidColorBrush x:Key="AppToggleOffBrush" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="AppToggleThumbBrush" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="AppToggleOffBorderBrush" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="AppToggleDisabledBrush" Color="#FF444444" />

    <!-- Ideal foreground for text on accent backgrounds -->
    <Color x:Key="AppIdealForegroundColor">Red</Color>
    <SolidColorBrush x:Key="AppIdealForegroundBrush" Color="Red" />

    <!-- ===== Legacy MahApps Keys (migration shim) ===== -->
    <!-- These keys are defined so existing StaticResource references continue to work during migration. -->
    <!-- Remove these once all XAML files are updated to use semantic keys. -->

    <!-- From RedAccent.xaml -->
    <Color x:Key="BlackColor">#FF580000</Color>
    <Color x:Key="WhiteColor">#FF900000</Color>
    <Color x:Key="HighlightColor">#FF900E00</Color>
    <Color x:Key="AccentBaseColor">#FF951400</Color>
    <Color x:Key="AccentColor">#CC951400</Color>
    <Color x:Key="AccentColor2">#99901400</Color>
    <Color x:Key="AccentColor3">#66901400</Color>
    <Color x:Key="AccentColor4">#33901400</Color>
    <Color x:Key="IdealForegroundColor">Red</Color>

    <SolidColorBrush x:Key="HighlightBrush" Color="{StaticResource HighlightColor}" />
    <SolidColorBrush x:Key="AccentBaseColorBrush" Color="{StaticResource AccentBaseColor}" />
    <SolidColorBrush x:Key="AccentColorBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="AccentColorBrush2" Color="{StaticResource AccentColor2}" />
    <SolidColorBrush x:Key="AccentColorBrush3" Color="{StaticResource AccentColor3}" />
    <SolidColorBrush x:Key="AccentColorBrush4" Color="{StaticResource AccentColor4}" />
    <SolidColorBrush x:Key="BlackBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="WhiteBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="WindowTitleColorBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="CheckmarkFill" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="RightArrowFill" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="IdealForegroundColorBrush" Color="{StaticResource IdealForegroundColor}" />
    <SolidColorBrush x:Key="IdealForegroundDisabledBrush" Opacity="0.4" Color="{StaticResource IdealForegroundColor}" />
    <SolidColorBrush x:Key="AccentSelectedColorBrush" Color="{StaticResource IdealForegroundColor}" />
    <LinearGradientBrush x:Key="ProgressBrush" StartPoint="1.002,0.5" EndPoint="0.001,0.5">
        <GradientStop Offset="0" Color="{StaticResource HighlightColor}" />
        <GradientStop Offset="1" Color="{StaticResource AccentColor3}" />
    </LinearGradientBrush>

    <!-- From RedTheme.xaml -->
    <SolidColorBrush x:Key="TextBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="LabelTextBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="BlackColorBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="ControlBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="WhiteColorBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="DisabledWhiteBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="WindowBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="{x:Static SystemColors.WindowBrushKey}" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="{x:Static SystemColors.ControlTextBrushKey}" Color="{StaticResource BlackColor}" />
    <Color x:Key="Gray1">#FF700000</Color>
    <Color x:Key="Gray2">#FF300000</Color>
    <Color x:Key="Gray7">#FF2C0000</Color>
    <Color x:Key="Gray8">#FF700000</Color>
    <Color x:Key="Gray10">#FF100000</Color>
    <Color x:Key="GrayNormal">#FF280000</Color>
    <Color x:Key="GrayHover">#FF500000</Color>
    <SolidColorBrush x:Key="GrayBrush1" Color="{StaticResource Gray1}" />
    <SolidColorBrush x:Key="GrayBrush2" Color="{StaticResource Gray2}" />
    <SolidColorBrush x:Key="GrayBrush7" Color="{StaticResource Gray7}" />
    <SolidColorBrush x:Key="GrayBrush8" Color="{StaticResource Gray8}" />
    <SolidColorBrush x:Key="GrayBrush10" Color="{StaticResource Gray10}" />
    <SolidColorBrush x:Key="GrayNormalBrush" Color="{StaticResource GrayNormal}" />
    <SolidColorBrush x:Key="GrayHoverBrush" Color="{StaticResource GrayHover}" />
    <SolidColorBrush x:Key="SliderValueDisabled" Color="#FF535353" />
    <SolidColorBrush x:Key="SliderTrackDisabled" Color="#FF383838" />
    <SolidColorBrush x:Key="SliderThumbDisabled" Color="#FF7E7E7E" />
    <SolidColorBrush x:Key="SliderTrackHover" Color="#FF737373" />
    <SolidColorBrush x:Key="SliderTrackNormal" Color="#FF6C6C6C" />
    <SolidColorBrush x:Key="TextBoxMouseOverInnerBorderBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="TextBoxFocusBorderBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="ButtonMouseOverBorderBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="ButtonMouseOverInnerBorderBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="ComboBoxMouseOverBorderBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="ComboBoxMouseOverInnerBorderBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="FlyoutBackgroundBrush" Color="#FF0E0000" />
    <SolidColorBrush x:Key="FlyoutForegroundBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="FlatButtonPressedBackgroundBrush" Color="#440000" />
    <SolidColorBrush x:Key="MenuBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="ContextMenuBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="SubMenuBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="MenuItemBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="ContextMenuBorderBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="SubMenuBorderBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="MenuItemSelectionFill" Color="#313131" />
    <SolidColorBrush x:Key="MenuItemSelectionStroke" Color="#313131" />
    <SolidColorBrush x:Key="TopMenuItemPressedFill" Color="#313131" />
    <SolidColorBrush x:Key="TopMenuItemPressedStroke" Color="#E0717070" />
    <SolidColorBrush x:Key="TopMenuItemSelectionStroke" Color="#90717070" />
    <SolidColorBrush x:Key="DisabledMenuItemForeground" Color="{StaticResource Gray7}" />
    <SolidColorBrush x:Key="DisabledMenuItemGlyphPanel" Color="#848589" />
    <SolidColorBrush x:Key="{x:Static SystemColors.MenuTextBrushKey}" Color="{StaticResource BlackColor}" />
    <Color x:Key="MenuShadowColor">#FFFFFFFF</Color>
    <SolidColorBrush x:Key="MetroDataGrid.DisabledHighlightBrush" Color="{StaticResource Gray7}" />

    <!-- DataGrid legacy -->
    <SolidColorBrush x:Key="MetroDataGrid.HighlightBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="MetroDataGrid.HighlightTextBrush" Color="{StaticResource IdealForegroundColor}" />
    <SolidColorBrush x:Key="MetroDataGrid.MouseOverHighlightBrush" Color="{StaticResource AccentColor3}" />
    <SolidColorBrush x:Key="MetroDataGrid.FocusBorderBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="MetroDataGrid.InactiveSelectionHighlightBrush" Color="{StaticResource AccentColor2}" />
    <SolidColorBrush x:Key="MetroDataGrid.InactiveSelectionHighlightTextBrush" Color="{StaticResource IdealForegroundColor}" />

    <!-- ToggleSwitch legacy -->
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OnSwitchBrush.Win10" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OnSwitchMouseOverBrush.Win10" Color="{StaticResource AccentColor2}" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorCheckedBrush.Win10" Color="{StaticResource IdealForegroundColor}" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.PressedBrush.Win10" Color="#FF999999" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OffBorderBrush.Win10" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OffMouseOverBorderBrush.Win10" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OffDisabledBorderBrush.Win10" Color="#FF666666" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OnSwitchDisabledBrush.Win10" Color="#FF444444" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorBrush.Win10" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorMouseOverBrush.Win10" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorPressedBrush.Win10" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorDisabledBrush.Win10" Color="#FF666666" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.Badged.DisabledBackgroundBrush" Color="#FF666666" />

    <!-- From RedControls.xaml (InlineSlider colors) -->
    <Color x:Key="USBGTop">#FF280808</Color>
    <Color x:Key="USBGBottom">#FF401010</Color>
    <Color x:Key="SBGTop">#FF701010</Color>
    <Color x:Key="SBGBottom">#FF501010</Color>
    <Color x:Key="SBGActTop">#FF702020</Color>
    <Color x:Key="SBGActBottom">#FF501010</Color>
    <Color x:Key="ButtonBGTop">#FF902020</Color>
    <Color x:Key="ButtonBGBottom">#FF601010</Color>
    <Color x:Key="ButtonBGActTop">#FFB03030</Color>
    <Color x:Key="ButtonBGActBottom">#FF902020</Color>
    <Color x:Key="TextColor">#FFFF2020</Color>
    <Color x:Key="SliderBorderColor">#FF380808</Color>
    <Color x:Key="SliderPosColor">#FFFF1010</Color>
    <Color x:Key="PromptColor">#FFD01010</Color>
    <Color x:Key="PromptActColor">#FFFF1010</Color>

</ResourceDictionary>
```

- [ ] **Step 2: Commit**

```bash
git add OATControl/Resources/Themes/DarkAstronomy.xaml
git commit -m "feat: add DarkAstronomy theme consolidating all color definitions"
```

---

### Task 3: Create Base.xaml with implicit control styles

This replaces the MahApps `Controls.xaml` and `Fonts.xaml` resource dictionaries. It provides implicit styles for all standard WPF controls that reference theme brushes via `DynamicResource`.

**Files:**
- Create: `OATControl/Resources/Themes/Base.xaml`

- [ ] **Step 1: Create the Base.xaml resource dictionary**

This file provides the minimum implicit styles needed to replace MahApps.Metro's default control styling. It references brushes from the theme dictionary via `DynamicResource`.

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Default Font -->
    <FontFamily x:Key="AppFontFamily">Segoe UI</FontFamily>
    <sys:Double x:Key="AppFontSize">14</sys:Double>
    <sys:Double x:Key="AppFontSizeSmall">11</sys:Double>
    <sys:Double x:Key="AppFontSizeLarge">18</sys:Double>

    <Style x:Key="MetroTextBlock" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{DynamicResource AppFontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource AppFontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource TextBrush}" />
    </Style>

    <Style TargetType="TextBlock" BasedOn="{StaticResource MetroTextBlock}" />

    <Style x:Key="MetroTextBox" TargetType="TextBox">
        <Setter Property="FontFamily" Value="{DynamicResource AppFontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource AppFontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource TextBrush}" />
        <Setter Property="Background" Value="{DynamicResource ControlBackgroundBrush}" />
        <Setter Property="BorderBrush" Value="{DynamicResource AppBorderBrush}" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="Padding" Value="4,2" />
        <Setter Property="CaretBrush" Value="{DynamicResource TextBrush}" />
    </Style>

    <Style TargetType="TextBox" BasedOn="{StaticResource MetroTextBox}" />

    <Style x:Key="MetroToolTip" TargetType="ToolTip">
        <Setter Property="Background" Value="{DynamicResource AppTooltipBackgroundBrush}" />
        <Setter Property="Foreground" Value="{DynamicResource AppTooltipForegroundBrush}" />
        <Setter Property="Padding" Value="5,2,5,7" />
        <Setter Property="FontFamily" Value="{DynamicResource AppFontFamily}" />
    </Style>

    <Style TargetType="ToolTip" BasedOn="{StaticResource MetroToolTip}" />

    <Style x:Key="AccentedSquareButtonStyle" TargetType="Button">
        <Setter Property="FontFamily" Value="{DynamicResource AppFontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource AppFontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource AccentSelectedColorBrush}" />
        <Setter Property="Background" Value="{DynamicResource AccentColorBrush}" />
        <Setter Property="BorderBrush" Value="{DynamicResource AppBorderSubtleBrush}" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="Padding" Value="10,5" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            Padding="{TemplateBinding Padding}"
                            CornerRadius="2">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter Property="Background" Value="{DynamicResource AccentColorBrush2}" />
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter Property="Background" Value="{DynamicResource AccentColorBrush3}" />
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Background" Value="{DynamicResource AccentColorBrush4}" />
                            <Setter Property="Foreground" Value="{DynamicResource AppDisabledForegroundBrush}" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType="Button" BasedOn="{StaticResource AccentedSquareButtonStyle}" />

    <Style TargetType="CheckBox">
        <Setter Property="FontFamily" Value="{DynamicResource AppFontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource AppFontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource TextBrush}" />
        <Setter Property="Background" Value="Transparent" />
    </Style>

    <Style TargetType="RadioButton">
        <Setter Property="FontFamily" Value="{DynamicResource AppFontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource AppFontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource TextBrush}" />
    </Style>

    <Style TargetType="Label">
        <Setter Property="FontFamily" Value="{DynamicResource AppFontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource AppFontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource TextBrush}" />
    </Style>

    <Style TargetType="ComboBox">
        <Setter Property="FontFamily" Value="{DynamicResource AppFontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource AppFontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource TextBrush}" />
        <Setter Property="Background" Value="{DynamicResource ControlBackgroundBrush}" />
    </Style>

    <Style TargetType="TabControl">
        <Setter Property="FontFamily" Value="{DynamicResource AppFontFamily}" />
        <Setter Property="Background" Value="{DynamicResource WindowBackgroundBrush}" />
    </Style>

    <Style TargetType="TabItem">
        <Setter Property="FontFamily" Value="{DynamicResource AppFontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource AppFontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource TextBrush}" />
    </Style>

    <Style TargetType="ScrollBar">
        <Setter Property="Background" Value="{DynamicResource WindowBackgroundBrush}" />
    </Style>

    <Style TargetType="ProgressBar">
        <Setter Property="Foreground" Value="{DynamicResource AccentColorBrush}" />
        <Setter Property="Background" Value="{DynamicResource AccentColorBrush4}" />
    </Style>

</ResourceDictionary>
```

Note: this file uses `sys:Double` so the `<ResourceDictionary>` tag needs the sys namespace. The `FontFamily` and `Double` resources also need:

```xml
<ResourceDictionary ...
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">
```

- [ ] **Step 2: Commit**

```bash
git add OATControl/Resources/Themes/Base.xaml
git commit -m "feat: add Base.xaml with implicit control styles replacing MahApps defaults"
```

---

### Task 4: Wire theme infrastructure into App.xaml

Replace MahApps resource dictionaries with our own. Update `App.xaml.cs` to use our `ThemeManager` instead of MahApps `ThemeManager`.

**Files:**
- Modify: `OATControl/App.xaml`
- Modify: `OATControl/App.xaml.cs`

- [ ] **Step 1: Update App.xaml — replace MahApps resource dictionaries**

In `OATControl/App.xaml`, replace the `<ResourceDictionary.MergedDictionaries>` section (lines 8-15) with:

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="pack://application:,,,/OATControl;component/Resources/Themes/Base.xaml" />
</ResourceDictionary.MergedDictionaries>
```

The theme dictionary will be added at runtime by `ThemeManager.ApplyTheme()`.

- [ ] **Step 2: Update App.xaml.cs — replace MahApps ThemeManager usage**

In `OATControl/App.xaml.cs`:

Remove `using MahApps.Metro;` (line 1).

Replace lines 50-61 (the MahApps `ThemeManager.AddAccent` / `ThemeManager.AddAppTheme` / `ThemeManager.ChangeAppStyle` block) with:

```csharp
Theming.ThemeManager.Instance.ApplyTheme("DarkAstronomy");
```

- [ ] **Step 3: Verify the app builds and looks identical**

Run: `msbuild OATControl/OATControl.sln /p:Configuration=Debug /p:Platform="Any CPU"`

The app should compile and run with the same visual appearance, since `DarkAstronomy.xaml` provides all the same brush keys.

- [ ] **Step 4: Commit**

```bash
git add OATControl/App.xaml OATControl/App.xaml.cs
git commit -m "feat: wire custom theme infrastructure into App.xaml, replacing MahApps theme setup"
```

---

## Phase 2: ThemedWindow

### Task 5: Create ThemedWindow base class

**Files:**
- Create: `OATControl/Controls/ThemedWindow.cs`

- [ ] **Step 1: Create ThemedWindow class**

```csharp
using System.Windows;
using System.Windows.Shell;

namespace OATControl.Controls
{
    public class ThemedWindow : Window
    {
        static ThemedWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemedWindow),
                new FrameworkPropertyMetadata(typeof(ThemedWindow)));
        }

        public ThemedWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = false;
        }
    }
}
```

- [ ] **Step 2: Add ThemedWindow default style to Base.xaml**

Add this to `OATControl/Resources/Themes/Base.xaml`, before the closing `</ResourceDictionary>`:

```xml
<!-- ThemedWindow default style -->
<Style TargetType="{x:Type local:ThemedWindow}" xmlns:local="clr-namespace:OATControl.Controls">
    <Setter Property="WindowChrome.WindowChrome">
        <Setter.Value>
            <WindowChrome
                CaptionHeight="30"
                ResizeBorderThickness="4"
                GlassFrameThickness="0"
                CornerRadius="0" />
        </Setter.Value>
    </Setter>
    <Setter Property="Background" Value="{DynamicResource WindowBackgroundBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource AppBorderSubtleBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type local:ThemedWindow}" xmlns:local="clr-namespace:OATControl.Controls">
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="*" />
                        </Grid.RowDefinitions>
                        <!-- Title bar -->
                        <Grid Grid.Row="0" Height="30" Background="{DynamicResource AppTitleBarBackgroundBrush}">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*" />
                                <ColumnDefinition Width="Auto" />
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="0"
                                       Text="{TemplateBinding Title}"
                                       Foreground="{DynamicResource AppTitleBarForegroundBrush}"
                                       FontSize="13"
                                       VerticalAlignment="Center"
                                       Margin="10,0,0,0"
                                       WindowChrome.IsHitTestVisibleInChrome="False" />
                            <StackPanel Grid.Column="1" Orientation="Horizontal"
                                        WindowChrome.IsHitTestVisibleInChrome="True">
                                <Button x:Name="MinimizeButton" Content="&#xE921;"
                                        FontFamily="Segoe MDL2 Assets" FontSize="10"
                                        Width="40" Height="30"
                                        Background="Transparent"
                                        Foreground="{DynamicResource AppTitleBarForegroundBrush}"
                                        BorderThickness="0"
                                        Click="MinimizeButton_Click" />
                                <Button x:Name="MaximizeButton" Content="&#xE922;"
                                        FontFamily="Segoe MDL2 Assets" FontSize="10"
                                        Width="40" Height="30"
                                        Background="Transparent"
                                        Foreground="{DynamicResource AppTitleBarForegroundBrush}"
                                        BorderThickness="0"
                                        Click="MaximizeButton_Click" />
                                <Button x:Name="CloseButton" Content="&#xE8BB;"
                                        FontFamily="Segoe MDL2 Assets" FontSize="10"
                                        Width="40" Height="30"
                                        Background="Transparent"
                                        Foreground="{DynamicResource AppTitleBarForegroundBrush}"
                                        BorderThickness="0"
                                        Click="CloseButton_Click" />
                            </StackPanel>
                        </Grid>
                        <!-- Content -->
                        <ContentPresenter Grid.Row="1" />
                    </Grid>
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

Then update the `ThemedWindow.cs` to add the click handlers. The template uses named buttons with Click handlers, so we need a code-behind approach. Since WPF `Window` default styles don't support code-behind event handlers in templates, use a different approach: bind to commands or use `WindowChrome` with the built-in non-client area buttons. The simplest approach that works with .NET Framework 4.7.2 is to not use template buttons at all, but instead set `WindowStyle="None"` and use the `WindowChrome` non-client area for system buttons:

Update `ThemedWindow.cs`:

```csharp
using System.Windows;
using System.Windows.Shell;

namespace OATControl.Controls
{
    public class ThemedWindow : Window
    {
        static ThemedWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemedWindow),
                new FrameworkPropertyMetadata(typeof(ThemedWindow)));
        }

        public ThemedWindow()
        {
            // WindowChrome handles the non-client area — system minimize/maximize/close
            // buttons are provided by the OS via WindowChrome
            var chrome = new WindowChrome
            {
                CaptionHeight = 30,
                ResizeBorderThickness = new Thickness(4),
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                UseAeroCaptionButtons = true
            };
            WindowChrome.SetWindowChrome(this, chrome);
        }
    }
}
```

And use a simpler template in `Base.xaml` that just provides the title bar area:

```xml
<Style TargetType="{x:Type local:ThemedWindow}" xmlns:local="clr-namespace:OATControl.Controls">
    <Setter Property="WindowStyle" Value="None" />
    <Setter Property="Background" Value="{DynamicResource WindowBackgroundBrush}" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type local:ThemedWindow}" xmlns:local="clr-namespace:OATControl.Controls">
                <AdornerDecorator>
                    <ContentPresenter />
                </AdornerDecorator>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

This approach lets `WindowChrome` render the system caption buttons natively. The window content is just the `ContentPresenter`. The window will have the standard minimize/maximize/close buttons styled by the OS but with our theme colors for the background.

- [ ] **Step 3: Commit**

```bash
git add OATControl/Controls/ThemedWindow.cs OATControl/Resources/Themes/Base.xaml
git commit -m "feat: add ThemedWindow base class with WindowChrome"
```

---

## Phase 3: Replacement Controls

### Task 6: Create custom ToggleSwitch control

Replaces `MahApps.Metro.Controls.ToggleSwitchButton` and `MahApps.Metro.Controls.ToggleSwitch`.

**Files:**
- Create: `OATControl/Controls/ToggleSwitch.xaml`
- Create: `OATControl/Controls/ToggleSwitch.xaml.cs`

- [ ] **Step 1: Create ToggleSwitch.xaml**

```xml
<UserControl x:Class="OATControl.Controls.ToggleSwitch"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             mc:Ignorable="d"
             x:Name="ThisToggle"
             d:DesignWidth="60" d:DesignHeight="30">
    <Grid>
        <Border x:Name="SwitchTrack"
                Width="44" Height="22"
                CornerRadius="11"
                Background="{DynamicResource AppToggleOffBrush}"
                BorderBrush="{DynamicResource AppToggleOffBorderBrush}"
                BorderThickness="1">
            <Ellipse x:Name="Thumb"
                     Width="16" Height="16"
                     Fill="{DynamicResource AppToggleThumbBrush}"
                     HorizontalAlignment="Left"
                     Margin="2,0,0,0"
                     VerticalAlignment="Center" />
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create ToggleSwitch.xaml.cs**

```csharp
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OATControl.Controls
{
    public partial class ToggleSwitch : UserControl
    {
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            "IsChecked", typeof(bool), typeof(ToggleSwitch),
            new PropertyMetadata(false, OnIsCheckedChanged));

        public static readonly DependencyProperty ThumbIndicatorBrushProperty = DependencyProperty.Register(
            "ThumbIndicatorBrush", typeof(Brush), typeof(ToggleSwitch),
            new PropertyMetadata(null));

        public ToggleSwitch()
        {
            InitializeComponent();
            UpdateVisual(false);
        }

        [Category("Common")]
        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        [Category("Appearance")]
        public Brush ThumbIndicatorBrush
        {
            get => (Brush)GetValue(ThumbIndicatorBrushProperty);
            set => SetValue(ThumbIndicatorBrushProperty, value);
        }

        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            IsChecked = !IsChecked;
            base.OnMouseLeftButtonUp(e);
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toggle = (ToggleSwitch)d;
            toggle.UpdateVisual(true);
        }

        private void UpdateVisual(bool animate)
        {
            if (Thumb == null || SwitchTrack == null) return;

            if (IsChecked)
            {
                Thumb.HorizontalAlignment = HorizontalAlignment.Right;
                Thumb.Margin = new Thickness(0, 0, 2, 0);
                SwitchTrack.Background = ThumbIndicatorBrush ?? (Brush)TryFindResource("AppToggleOnBrush");
            }
            else
            {
                Thumb.HorizontalAlignment = HorizontalAlignment.Left;
                Thumb.Margin = new Thickness(2, 0, 0, 0);
                SwitchTrack.Background = (Brush)TryFindResource("AppToggleOffBrush");
            }
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add OATControl/Controls/ToggleSwitch.xaml OATControl/Controls/ToggleSwitch.xaml.cs
git commit -m "feat: add custom ToggleSwitch control replacing MahApps ToggleSwitchButton"
```

---

## Phase 4: Migrate Windows

### Task 7: Migrate MainWindow

The largest and most important window. Template for all subsequent window migrations.

**Files:**
- Modify: `OATControl/MainWindow.xaml`
- Modify: `OATControl/MainWindow.xaml.cs`

- [ ] **Step 1: Update MainWindow.xaml — change base class and namespaces**

Replace lines 1-13:
```xml
<Controls:MetroWindow x:Class="OATControl.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:Controls="clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:OATControl"
		xmlns:controls="clr-namespace:OATControl.Controls"
		xmlns:converters="clr-namespace:OATControl.Converters"
		mc:Ignorable="d"
		Title="{Binding Version, StringFormat={} OpenAstroTracker Control V{0}}" MinHeight="767" MinWidth="720" Height="767" Width="720">
```

With:
```xml
<controls:ThemedWindow x:Class="OATControl.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:OATControl"
        xmlns:controls="clr-namespace:OATControl.Controls"
        xmlns:converters="clr-namespace:OATControl.Converters"
        mc:Ignorable="d"
        Title="{Binding Version, StringFormat={} OpenAstroTracker Control V{0}}" MinHeight="767" MinWidth="720" Height="767" Width="720">
```

Also change the closing tag from `</Controls:MetroWindow>` to `</controls:ThemedWindow>`.

- [ ] **Step 2: Update MainWindow.xaml — replace MahApps controls**

In the Window.Resources section:
- Change `BasedOn="{StaticResource MetroTextBox}"` to `BasedOn="{StaticResource MetroTextBox}"` (this now resolves to our custom style in Base.xaml)
- Change `BasedOn="{StaticResource MetroToolTip}"` to `BasedOn="{StaticResource MetroToolTip}"` (same — resolves to Base.xaml)
- Replace `Controls:NumericUpDown` style target (if present) — just remove the style since NumericUpDown appears unused
- Replace `Controls:ToggleSwitchButton` with `controls:ToggleSwitch` (our custom control)
- The `StaticResource AccentBaseColorBrush`, `AccentColorBrush4`, etc. references continue to work via the legacy keys in DarkAstronomy.xaml

- [ ] **Step 3: Update MainWindow.xaml.cs — change base class**

Replace:
```csharp
using MahApps.Metro.Controls;
```
With:
```csharp
using OATControl.Controls;
```

Replace:
```csharp
public partial class MainWindow : MetroWindow
```
With:
```csharp
public partial class MainWindow : ThemedWindow
```

- [ ] **Step 4: Verify build**

Run: `msbuild OATControl/OATControl.sln /p:Configuration=Debug /p:Platform="Any CPU"`

- [ ] **Step 5: Commit**

```bash
git add OATControl/MainWindow.xaml OATControl/MainWindow.xaml.cs
git commit -m "feat: migrate MainWindow from MetroWindow to ThemedWindow"
```

---

### Task 8: Migrate remaining MetroWindow dialogs

Apply the same pattern from Task 7 to all remaining windows that inherit `MetroWindow`. Each follows the same steps: change base class, update namespaces, fix MahApps controls.

**Files to modify (12 windows):**
- `OATControl/DlgAppSettings.xaml` + `.cs`
- `OATControl/DlgAxisCalibration.xaml` + `.cs`
- `OATControl/DlgChecklistEditor.xaml` + `.cs`
- `OATControl/DlgChooseOat.xaml` + `.cs`
- `OATControl/DlgEditPoint.xaml` + `.cs`
- `OATControl/DlgNinaPolarAlignment.xaml` + `.cs`
- `OATControl/DlgRunPolarAlignment.xaml` + `.cs`
- `OATControl/DlgRunPolarAlignmentStep1.xaml` + `.cs`
- `OATControl/DlgSharpCapPolarAlignment.xaml` + `.cs`
- `OATControl/DlgStepCalibration.xaml` + `.cs`
- `OATControl/DlgWaitForGXState.xaml` + `.cs`
- `OATControl/TargetChooser.xaml` + `.cs`

For each window, apply these changes:

**XAML changes:**
1. Replace opening tag from `<Controls:MetroWindow` to `<controls:ThemedWindow`
2. Remove the MahApps namespace import: `xmlns:Controls="clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro"`
3. Add/ensure `xmlns:controls="clr-namespace:OATControl.Controls"` is present
4. Replace closing tag from `</Controls:MetroWindow>` to `</controls:ThemedWindow>`
5. Replace any `Controls:ToggleSwitchButton` with `controls:ToggleSwitch`
6. Replace any `Controls:ToggleSwitch` with `controls:ToggleSwitch`
7. Remove any `ThumbIndicatorBrush="#E00"` attribute (our ToggleSwitch uses theme brush by default) or keep it for explicit override

**Code-behind changes:**
1. Replace `using MahApps.Metro.Controls;` with `using OATControl.Controls;`
2. Replace base class from `: MetroWindow` to `: ThemedWindow`

**DlgMessageBox.xaml** is special — it inherits plain `Window`, not `MetroWindow`, but references MahApps styles. Changes:
1. Remove `xmlns:mah="http://metro.mahapps.com/winfx/xaml/controls"`
2. `BasedOn="{StaticResource MetroTextBlock}"` now resolves to our Base.xaml style — keep it
3. `Style="{StaticResource AccentedSquareButtonStyle}"` now resolves to our Base.xaml style — keep it

- [ ] **Step 1: Migrate DlgAppSettings**
- [ ] **Step 2: Migrate DlgAxisCalibration**
- [ ] **Step 3: Migrate DlgChecklistEditor**
- [ ] **Step 4: Migrate DlgChooseOat**
- [ ] **Step 5: Migrate DlgEditPoint**
- [ ] **Step 6: Migrate DlgMessageBox**
- [ ] **Step 7: Migrate DlgNinaPolarAlignment**
- [ ] **Step 8: Migrate DlgRunPolarAlignment**
- [ ] **Step 9: Migrate DlgRunPolarAlignmentStep1**
- [ ] **Step 10: Migrate DlgSharpCapPolarAlignment**
- [ ] **Step 11: Migrate DlgStepCalibration**
- [ ] **Step 12: Migrate DlgWaitForGXState**
- [ ] **Step 13: Migrate TargetChooser**
- [ ] **Step 14: Verify build**

Run: `msbuild OATControl/OATControl.sln /p:Configuration=Debug /p:Platform="Any CPU"`

- [ ] **Step 15: Commit**

```bash
git add -A OATControl/
git commit -m "feat: migrate all dialogs from MetroWindow to ThemedWindow"
```

---

## Phase 5: Migrate Custom Controls

### Task 9: Migrate PushButton, StopButton, Joystick

Replace MahApps brush references with theme brush keys.

**Files:**
- Modify: `OATControl/Controls/PushButton.xaml`
- Modify: `OATControl/Controls/PushButton.xaml.cs`
- Modify: `OATControl/Controls/StopButton.xaml`
- Modify: `OATControl/Controls/StopButton.xaml.cs`
- Modify: `OATControl/Controls/Joystick.xaml`

- [ ] **Step 1: Update PushButton.xaml**

The `StaticResource` references to `HighlightBrush`, `AccentColorBrush4`, `AccentColorBrush2`, `AccentBaseColorBrush` all continue to work via legacy keys. Change them to `DynamicResource` for theme-swap support:

Replace all `{StaticResource HighlightBrush}` with `{DynamicResource HighlightBrush}`
Replace all `{StaticResource AccentColorBrush4}` with `{DynamicResource AccentColorBrush4}`
Replace all `{StaticResource AccentColorBrush2}` with `{DynamicResource AccentColorBrush2}`
Replace all `{StaticResource AccentBaseColorBrush}` with `{DynamicResource AccentBaseColorBrush}`

- [ ] **Step 2: Update PushButton.xaml.cs**

Remove `using MahApps.Metro.Converters;` (line 1) — it's unused.

- [ ] **Step 3: Update StopButton.xaml**

Same pattern — change `StaticResource` to `DynamicResource` for:
- `HighlightBrush`
- `AccentColorBrush3`
- `AccentBaseColorBrush`
- `AccentColorBrush2`

- [ ] **Step 4: Update StopButton.xaml.cs**

Remove `using MahApps.Metro.Converters;` (line 1) — it's unused.

- [ ] **Step 5: Update Joystick.xaml**

Change `{StaticResource AccentColorBrush2}` to `{DynamicResource AccentColorBrush2}`
Change `{StaticResource HighlightBrush}` to `{DynamicResource HighlightBrush}`

- [ ] **Step 6: Commit**

```bash
git add OATControl/Controls/PushButton.xaml OATControl/Controls/PushButton.xaml.cs OATControl/Controls/StopButton.xaml OATControl/Controls/StopButton.xaml.cs OATControl/Controls/Joystick.xaml
git commit -m "feat: migrate custom controls to use DynamicResource theme brushes"
```

---

### Task 10: Migrate ViewModels (remove MahApps usings)

**Files:**
- Modify: `OATControl/ViewModels/MountVM.cs`
- Modify: `OATControl/ViewModels/PolarAlignLogProcessorBase.cs` (if it has MahApps usings)
- Modify: `OATControl/ViewModels/SharpCapPolarAlignLogProcessor.cs` (if it has MahApps usings)

- [ ] **Step 1: Update MountVM.cs**

Remove these two lines from the using block:
```csharp
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
```

Search the file for any actual usage of MahApps types. If `MountVM` uses `ShowMessageAsync` or similar MahApps dialog methods, replace with standard `MessageBox.Show()` or a simple dialog.

- [ ] **Step 2: Check and update other ViewModels**

Remove `using MahApps.Metro.Controls;` from `PolarAlignLogProcessorBase.cs` and `SharpCapPolarAlignLogProcessor.cs` if present.

- [ ] **Step 3: Commit**

```bash
git add OATControl/ViewModels/
git commit -m "feat: remove MahApps usings from ViewModels"
```

---

## Phase 6: Cleanup

### Task 11: Remove MahApps.Metro dependency

**Files:**
- Modify: `OATControl/OATControl.csproj`
- Modify: `OATControl/packages.config`

- [ ] **Step 1: Remove MahApps references from .csproj**

In `OATControl/OATControl.csproj`, remove these lines (lines 43-48):
```xml
<Reference Include="ControlzEx, Version=3.0.2.4, Culture=neutral, processorArchitecture=MSIL">
  <HintPath>packages\ControlzEx.3.0.2.4\lib\net462\ControlzEx.dll</HintPath>
</Reference>
<Reference Include="MahApps.Metro, Version=1.6.5.1, Culture=neutral, processorArchitecture=MSIL">
  <HintPath>packages\MahApps.Metro.1.6.5\lib\net47\MahApps.Metro.dll</HintPath>
</Reference>
```

Also remove (lines 55-57):
```xml
<Reference Include="System.Windows.Interactivity, Version=4.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL">
  <HintPath>packages\ControlzEx.3.0.2.4\lib\net462\System.Windows.Interactivity.dll</HintPath>
</Reference>
```

- [ ] **Step 2: Remove MahApps from packages.config**

In `OATControl/packages.config`, remove:
```xml
<package id="ControlzEx" version="3.0.2.4" targetFramework="net472" />
<package id="MahApps.Metro" version="1.6.5" targetFramework="net472" />
```

- [ ] **Step 3: Add new files to .csproj**

Add the new files to `OATControl/OATControl.csproj`:
- Add `Theming/ThemeManager.cs` as a `<Compile>` item
- Add `Controls/ThemedWindow.cs` as a `<Compile>` item
- Add `Controls/ToggleSwitch.xaml` as a `<Page>` item
- Add `Controls/ToggleSwitch.xaml.cs` as a `<Compile>` with `<DependentUpon>ToggleSwitch.xaml</DependentUpon>`
- Add `Resources/Themes/Base.xaml` as a `<Page>` item
- Add `Resources/Themes/DarkAstronomy.xaml` as a `<Page>` item

- [ ] **Step 4: Delete old resource files**

Remove from .csproj and delete from disk:
- `Resources/RedControls.xaml`
- `Resources/RedAccent.xaml`
- `Resources/RedTheme.xaml`
- `Resources/GreyControls.xaml`

Also remove the `<Page>` entries for these files from .csproj (lines 252-279).

- [ ] **Step 5: Verify build**

Run: `msbuild OATControl/OATControl.sln /p:Configuration=Debug /p:Platform="Any CPU"`

- [ ] **Step 6: Commit**

```bash
git add -A OATControl/
git commit -m "feat: remove MahApps.Metro dependency and old theme files"
```

---

## Phase 7: Theme Picker & Second Theme

### Task 12: Create Daylight theme

A light theme for daytime use to demonstrate the theme system works.

**Files:**
- Create: `OATControl/Resources/Themes/Daylight.xaml`

- [ ] **Step 1: Create Daylight.xaml**

Same key structure as `DarkAstronomy.xaml` but with a light color palette:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ===== Semantic Theme Keys (light palette) ===== -->
    <Color x:Key="AppPrimaryColor">#FF0050C8</Color>
    <SolidColorBrush x:Key="AppPrimaryBrush" Color="{StaticResource AppPrimaryColor}" />

    <Color x:Key="AppForegroundColor">#FF1A1A1A</Color>
    <Color x:Key="AppForegroundStrongColor">#FF000000</Color>
    <Color x:Key="AppForegroundSubtleColor">#FF666666</Color>
    <SolidColorBrush x:Key="AppForegroundBrush" Color="{StaticResource AppForegroundColor}" />
    <SolidColorBrush x:Key="AppForegroundStrongBrush" Color="{StaticResource AppForegroundStrongColor}" />
    <SolidColorBrush x:Key="AppForegroundSubtleBrush" Color="{StaticResource AppForegroundSubtleColor}" />

    <Color x:Key="AppBackgroundColor">#FFFFFFFF</Color>
    <Color x:Key="AppBackgroundAltColor">#FFF0F0F0</Color>
    <Color x:Key="AppWindowBackgroundColor">#FFF5F5F5</Color>
    <SolidColorBrush x:Key="AppBackgroundBrush" Color="{StaticResource AppBackgroundColor}" />
    <SolidColorBrush x:Key="AppBackgroundAltBrush" Color="{StaticResource AppBackgroundAltColor}" />
    <SolidColorBrush x:Key="AppWindowBackgroundBrush" Color="{StaticResource AppWindowBackgroundColor}" />

    <Color x:Key="AppBorderColor">#FFCCCCCC</Color>
    <Color x:Key="AppBorderSubtleColor">#FFE0E0E0</Color>
    <SolidColorBrush x:Key="AppBorderBrush" Color="{StaticResource AppBorderColor}" />
    <SolidColorBrush x:Key="AppBorderSubtleBrush" Color="{StaticResource AppBorderSubtleColor}" />

    <Color x:Key="AppButtonBackgroundColor">#FFD0D0D0</Color>
    <Color x:Key="AppButtonHoverColor">#FFC0C0C0</Color>
    <Color x:Key="AppButtonPressedColor">#FFB0B0B0</Color>
    <SolidColorBrush x:Key="AppButtonBackgroundBrush" Color="{StaticResource AppButtonBackgroundColor}" />
    <SolidColorBrush x:Key="AppButtonHoverBrush" Color="{StaticResource AppButtonHoverColor}" />
    <SolidColorBrush x:Key="AppButtonPressedBrush" Color="{StaticResource AppButtonPressedColor}" />

    <Color x:Key="AppSelectedColor">#CC0050C8</Color>
    <Color x:Key="AppSelectedHoverColor">#990050C8</Color>
    <SolidColorBrush x:Key="AppSelectedBrush" Color="{StaticResource AppSelectedColor}" />
    <SolidColorBrush x:Key="AppSelectedHoverBrush" Color="{StaticResource AppSelectedHoverColor}" />

    <SolidColorBrush x:Key="AppTooltipBackgroundBrush" Color="#FF333333" />
    <SolidColorBrush x:Key="AppTooltipForegroundBrush" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="AppWarningBrush" Color="#FFFF8000" />
    <SolidColorBrush x:Key="AppSuccessBrush" Color="#FF228B22" />
    <SolidColorBrush x:Key="AppDangerBrush" Color="#FFFF4444" />
    <SolidColorBrush x:Key="AppDisabledBrush" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="AppDisabledForegroundBrush" Color="#FF999999" />

    <SolidColorBrush x:Key="AppTitleBarBackgroundBrush" Color="#FFEEEEEE" />
    <SolidColorBrush x:Key="AppTitleBarForegroundBrush" Color="#FF333333" />
    <SolidColorBrush x:Key="AppTitleBarButtonHoverBrush" Color="#FFD0D0D0" />
    <SolidColorBrush x:Key="AppTitleBarButtonPressedBrush" Color="#FFB0B0B0" />

    <SolidColorBrush x:Key="AppToggleOnBrush" Color="{StaticResource AppPrimaryColor}" />
    <SolidColorBrush x:Key="AppToggleOffBrush" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="AppToggleThumbBrush" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="AppToggleOffBorderBrush" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="AppToggleDisabledBrush" Color="#FFE0E0E0" />

    <Color x:Key="AppIdealForegroundColor">White</Color>
    <SolidColorBrush x:Key="AppIdealForegroundBrush" Color="White" />

    <!-- ===== Legacy MahApps Keys (light palette) ===== -->
    <Color x:Key="BlackColor">#FF1A1A1A</Color>
    <Color x:Key="WhiteColor">#FFF5F5F5</Color>
    <Color x:Key="HighlightColor">#FF0050C8</Color>
    <Color x:Key="AccentBaseColor">#FF0050C8</Color>
    <Color x:Key="AccentColor">#CC0050C8</Color>
    <Color x:Key="AccentColor2">#990050C8</Color>
    <Color x:Key="AccentColor3">#660050C8</Color>
    <Color x:Key="AccentColor4">#330050C8</Color>
    <Color x:Key="IdealForegroundColor">White</Color>

    <SolidColorBrush x:Key="HighlightBrush" Color="{StaticResource HighlightColor}" />
    <SolidColorBrush x:Key="AccentBaseColorBrush" Color="{StaticResource AccentBaseColor}" />
    <SolidColorBrush x:Key="AccentColorBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="AccentColorBrush2" Color="{StaticResource AccentColor2}" />
    <SolidColorBrush x:Key="AccentColorBrush3" Color="{StaticResource AccentColor3}" />
    <SolidColorBrush x:Key="AccentColorBrush4" Color="{StaticResource AccentColor4}" />
    <SolidColorBrush x:Key="BlackBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="WhiteBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="WindowTitleColorBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="CheckmarkFill" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="RightArrowFill" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="IdealForegroundColorBrush" Color="{StaticResource IdealForegroundColor}" />
    <SolidColorBrush x:Key="IdealForegroundDisabledBrush" Opacity="0.4" Color="{StaticResource IdealForegroundColor}" />
    <SolidColorBrush x:Key="AccentSelectedColorBrush" Color="{StaticResource IdealForegroundColor}" />
    <LinearGradientBrush x:Key="ProgressBrush" StartPoint="1.002,0.5" EndPoint="0.001,0.5">
        <GradientStop Offset="0" Color="{StaticResource HighlightColor}" />
        <GradientStop Offset="1" Color="{StaticResource AccentColor3}" />
    </LinearGradientBrush>

    <SolidColorBrush x:Key="TextBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="LabelTextBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="BlackColorBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="ControlBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="WhiteColorBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="DisabledWhiteBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="WindowBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="{x:Static SystemColors.WindowBrushKey}" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="{x:Static SystemColors.ControlTextBrushKey}" Color="{StaticResource BlackColor}" />
    <Color x:Key="Gray1">#FFD0D0D0</Color>
    <Color x:Key="Gray2">#FFE0E0E0</Color>
    <Color x:Key="Gray7">#FFB0B0B0</Color>
    <Color x:Key="Gray8">#FFD0D0D0</Color>
    <Color x:Key="Gray10">#FFF0F0F0</Color>
    <Color x:Key="GrayNormal">#FFE0E0E0</Color>
    <Color x:Key="GrayHover">#FFD0D0D0</Color>
    <SolidColorBrush x:Key="GrayBrush1" Color="{StaticResource Gray1}" />
    <SolidColorBrush x:Key="GrayBrush2" Color="{StaticResource Gray2}" />
    <SolidColorBrush x:Key="GrayBrush7" Color="{StaticResource Gray7}" />
    <SolidColorBrush x:Key="GrayBrush8" Color="{StaticResource Gray8}" />
    <SolidColorBrush x:Key="GrayBrush10" Color="{StaticResource Gray10}" />
    <SolidColorBrush x:Key="GrayNormalBrush" Color="{StaticResource GrayNormal}" />
    <SolidColorBrush x:Key="GrayHoverBrush" Color="{StaticResource GrayHover}" />
    <SolidColorBrush x:Key="SliderValueDisabled" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="SliderTrackDisabled" Color="#FFE0E0E0" />
    <SolidColorBrush x:Key="SliderThumbDisabled" Color="#FFD0D0D0" />
    <SolidColorBrush x:Key="SliderTrackHover" Color="#FFC0C0C0" />
    <SolidColorBrush x:Key="SliderTrackNormal" Color="#FFD0D0D0" />
    <SolidColorBrush x:Key="TextBoxMouseOverInnerBorderBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="TextBoxFocusBorderBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="ButtonMouseOverBorderBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="ButtonMouseOverInnerBorderBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="ComboBoxMouseOverBorderBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="ComboBoxMouseOverInnerBorderBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="FlyoutBackgroundBrush" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="FlyoutForegroundBrush" Color="{StaticResource BlackColor}" />
    <SolidColorBrush x:Key="FlatButtonPressedBackgroundBrush" Color="#FFD0D0D0" />
    <SolidColorBrush x:Key="MenuBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="ContextMenuBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="SubMenuBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="MenuItemBackgroundBrush" Color="{StaticResource WhiteColor}" />
    <SolidColorBrush x:Key="ContextMenuBorderBrush" Color="{StaticResource AppBorderColor}" />
    <SolidColorBrush x:Key="SubMenuBorderBrush" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="MenuItemSelectionFill" Color="#FFE0E0E0" />
    <SolidColorBrush x:Key="MenuItemSelectionStroke" Color="#FFD0D0D0" />
    <SolidColorBrush x:Key="TopMenuItemPressedFill" Color="#FFD0D0D0" />
    <SolidColorBrush x:Key="TopMenuItemPressedStroke" Color="#FFC0C0C0" />
    <SolidColorBrush x:Key="TopMenuItemSelectionStroke" Color="#FFD0D0D0" />
    <SolidColorBrush x:Key="DisabledMenuItemForeground" Color="#FF999999" />
    <SolidColorBrush x:Key="DisabledMenuItemGlyphPanel" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="{x:Static SystemColors.MenuTextBrushKey}" Color="{StaticResource BlackColor}" />
    <Color x:Key="MenuShadowColor">#FF000000</Color>
    <SolidColorBrush x:Key="MetroDataGrid.DisabledHighlightBrush" Color="{StaticResource Gray7}" />
    <SolidColorBrush x:Key="MetroDataGrid.HighlightBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="MetroDataGrid.HighlightTextBrush" Color="{StaticResource IdealForegroundColor}" />
    <SolidColorBrush x:Key="MetroDataGrid.MouseOverHighlightBrush" Color="{StaticResource AccentColor3}" />
    <SolidColorBrush x:Key="MetroDataGrid.FocusBorderBrush" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="MetroDataGrid.InactiveSelectionHighlightBrush" Color="{StaticResource AccentColor2}" />
    <SolidColorBrush x:Key="MetroDataGrid.InactiveSelectionHighlightTextBrush" Color="{StaticResource IdealForegroundColor}" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OnSwitchBrush.Win10" Color="{StaticResource AccentColor}" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OnSwitchMouseOverBrush.Win10" Color="{StaticResource AccentColor2}" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorCheckedBrush.Win10" Color="{StaticResource IdealForegroundColor}" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.PressedBrush.Win10" Color="#FF999999" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OffBorderBrush.Win10" Color="#FFCCCCCC" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OffMouseOverBorderBrush.Win10" Color="#FF999999" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OffDisabledBorderBrush.Win10" Color="#FFE0E0E0" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.OnSwitchDisabledBrush.Win10" Color="#FFE0E0E0" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorBrush.Win10" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorMouseOverBrush.Win10" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorPressedBrush.Win10" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.ToggleSwitchButton.ThumbIndicatorDisabledBrush.Win10" Color="#FFE0E0E0" />
    <SolidColorBrush x:Key="MahApps.Metro.Brushes.Badged.DisabledBackgroundBrush" Color="#FFE0E0E0" />

    <Color x:Key="USBGTop">#FFE8E8E8</Color>
    <Color x:Key="USBGBottom">#FFD0D0D0</Color>
    <Color x:Key="SBGTop">#FFD0D0D0</Color>
    <Color x:Key="SBGBottom">#FFE0E0E0</Color>
    <Color x:Key="SBGActTop">#FFC0C0C0</Color>
    <Color x:Key="SBGActBottom">#FFD0D0D0</Color>
    <Color x:Key="ButtonBGTop">#FFC0C0C0</Color>
    <Color x:Key="ButtonBGBottom">#FFD0D0D0</Color>
    <Color x:Key="ButtonBGActTop">#FFB0B0B0</Color>
    <Color x:Key="ButtonBGActBottom">#FFC0C0C0</Color>
    <Color x:Key="TextColor">#FF333333</Color>
    <Color x:Key="SliderBorderColor">#FFCCCCCC</Color>
    <Color x:Key="SliderPosColor">#FF0050C8</Color>
    <Color x:Key="PromptColor">#FF0050C8</Color>
    <Color x:Key="PromptActColor">#FF003090</Color>

</ResourceDictionary>
```

- [ ] **Step 2: Commit**

```bash
git add OATControl/Resources/Themes/Daylight.xaml
git commit -m "feat: add Daylight theme for light mode"
```

---

### Task 13: Add theme setting to AppSettings

**Files:**
- Modify: `OATControl/ViewModels/AppSettings.cs`

- [ ] **Step 1: Add ThemeName property**

Add this property to the `AppSettings` class in `OATControl/ViewModels/AppSettings.cs`, alongside the other settings:

```csharp
[DefaultValueAttribute("DarkAstronomy")]
public string ThemeName
{
    get { return this["ThemeName"]; }
    set { this["ThemeName"] = value; }
}
```

- [ ] **Step 2: Apply saved theme on startup**

In `OATControl/App.xaml.cs`, update the `OnStartup` method to use the saved theme:

```csharp
var savedTheme = AppSettings.Instance.ThemeName;
Theming.ThemeManager.Instance.ApplyTheme(savedTheme);
```

Also add `AppSettings.Instance.Load();` before the theme apply call if it's not already there.

- [ ] **Step 3: Commit**

```bash
git add OATControl/ViewModels/AppSettings.cs OATControl/App.xaml.cs
git commit -m "feat: persist theme selection in AppSettings"
```

---

### Task 14: Add theme picker to Settings dialog

**Files:**
- Modify: `OATControl/DlgAppSettings.xaml`
- Modify: `OATControl/DlgAppSettings.xaml.cs`

- [ ] **Step 1: Add a ComboBox for theme selection**

Add a row to the settings grid in `DlgAppSettings.xaml` with a theme selector:

```xml
<TextBlock Grid.Row="N" Grid.Column="0" Text="Theme" VerticalAlignment="Center" />
<ComboBox Grid.Row="N" Grid.Column="1"
          ItemsSource="{Binding AvailableThemes}"
          SelectedItem="{Binding SelectedTheme}" />
```

Where `N` is the next available row. Wire the `SelectedTheme` property to call `Theming.ThemeManager.Instance.ApplyTheme(value)` on change.

- [ ] **Step 2: Commit**

```bash
git add OATControl/DlgAppSettings.xaml OATControl/DlgAppSettings.xaml.cs
git commit -m "feat: add theme picker to Settings dialog"
```

---

### Task 15: Final verification and cleanup

- [ ] **Step 1: Search for any remaining MahApps references**

```bash
grep -r "MahApps" OATControl/ --include="*.cs" --include="*.xaml" -l
grep -r "MetroWindow" OATControl/ --include="*.cs" --include="*.xaml" -l
```

Both should return no results.

- [ ] **Step 2: Search for remaining hardcoded colors that should use theme keys**

```bash
grep -rn '#[0-9A-Fa-f]\{3,8\}' OATControl/ --include="*.xaml" | grep -v "Resources/Themes/" | grep -v "Controls/RangeSlider.xaml"
```

Review results — colors in RangeSlider.xaml are acceptable (internal control colors). Other hits should be converted to `DynamicResource` references.

- [ ] **Step 3: Full build and visual test**

Run: `msbuild OATControl/OATControl.sln /p:Configuration=Debug /p:Platform="Any CPU"`

Launch the app and verify:
- Visual appearance matches the original dark-red theme
- Theme switch to Daylight works via Settings dialog
- Theme switch back to DarkAstronomy restores original look
- All dialogs open and display correctly
- Toggle switches work
- No missing resource errors in output

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "feat: complete MahApps.Metro removal with custom theming system"
```
