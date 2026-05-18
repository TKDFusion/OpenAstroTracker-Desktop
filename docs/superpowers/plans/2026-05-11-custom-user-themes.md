# Custom User Themes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable users to create, edit, import, and export custom color themes stored as XAML files in AppData.

**Architecture:** Extend the existing `ThemeManager` to discover and load user theme files from `%AppData%\OpenAstroTracker\Themes\`. User themes contain only Color definitions; brushes are generated programmatically at load time. A new `DlgThemeEditor` dialog provides a visual editor with a live preview panel. The settings dialog gains buttons to create/edit/import themes.

**Tech Stack:** WPF (.NET Framework 4.7.2), System.Windows.Forms.ColorDialog (for color picking — already available via the framework, no NuGet needed), XAML ResourceDictionary.

---

## File Structure

| Action | Path | Purpose |
|--------|------|---------|
| Modify | `OATControl/Theming/ThemeManager.cs` | User theme scanning, loading, brush generation, import/export/delete |
| Create | `OATControl/Theming/ThemeColorDefinitions.cs` | Centralized list of known color keys, display names, and defaults |
| Create | `OATControl/DlgThemeEditor.xaml` | Theme editor dialog XAML |
| Create | `OATControl/DlgThemeEditor.xaml.cs` | Theme editor dialog code-behind |
| Modify | `OATControl/DlgAppSettings.xaml` | Add theme management buttons below ComboBox |
| Modify | `OATControl/DlgAppSettings.xaml.cs` | Wire up new theme management buttons |
| Modify | `OATControl/OATControl.csproj` | Add new files to compilation (if needed) |

---

### Task 1: Create ThemeColorDefinitions — the color key registry

**Files:**
- Create: `OATControl/Theming/ThemeColorDefinitions.cs`

This is a static class that defines all known theme color keys, their human-friendly display names, groupings, and default (DarkAstronomy) values. Both `ThemeManager` and `DlgThemeEditor` will reference this to avoid duplicating the key list.

- [ ] **Step 1: Create the file**

```csharp
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
            new ThemeColorDefinition("AppForegroundSubtleColor", "Foreground (Subtle)", "Text", (Color)ColorConverter.ConvertFromString("#FF800000")),

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
            new ThemeColorDefinition("AppTooltipForegroundColor", "Tooltip Foreground", "Tooltip", (Color)ColorConverter.ConvertFromString("#111")),

            // Disabled
            new ThemeColorDefinition("AppDisabledBackgroundColor", "Disabled Background", "Disabled", (Color)ColorConverter.ConvertFromString("#600000")),
            new ThemeColorDefinition("AppDisabledBorderColor", "Disabled Border", "Disabled", (Color)ColorConverter.ConvertFromString("#822")),
            new ThemeColorDefinition("AppDisabledForegroundColor", "Disabled Foreground", "Disabled", (Color)ColorConverter.ConvertFromString("#822")),

            // Toggle switch
            new ThemeColorDefinition("AppToggleOffColor", "Toggle Off", "Toggle", (Color)ColorConverter.ConvertFromString("#611")),
            new ThemeColorDefinition("AppToggleThumbColor", "Toggle Thumb", "Toggle", (Color)ColorConverter.ConvertFromString("#C11")),
            new ThemeColorDefinition("AppToggleOffBorderColor", "Toggle Off Border", "Toggle", (Color)ColorConverter.ConvertFromString("#D11")),
        };

        public static string BrushKeyFromColorKey(string colorKey)
        {
            // AppPrimaryColor -> AppPrimaryBrush, AppForegroundColor -> AppForegroundBrush
            return colorKey.Replace("Color", "Brush");
        }
    }
}
```

- [ ] **Step 2: Add the file to the .csproj if needed**

Check if the project uses auto-wildcard includes (SDK-style) or explicit `<Compile>` entries. If explicit, add:

```xml
<Compile Include="Theming\ThemeColorDefinitions.cs" />
```

- [ ] **Step 3: Commit**

```bash
git add OATControl/Theming/ThemeColorDefinitions.cs
git add OATControl/OATControl.csproj  # if modified
git commit -m "Add ThemeColorDefinitions — centralized theme color key registry"
```

---

### Task 2: Extend ThemeManager — user theme discovery, loading, and brush generation

**Files:**
- Modify: `OATControl/Theming/ThemeManager.cs`

- [ ] **Step 1: Add user theme infrastructure**

Add the following to `ThemeManager.cs`. Replace the existing `AvailableThemes` field with a tracking structure, and add the user themes folder path, scanning, brush generation, and metadata helpers.

Add these usings to the top of the file:
```csharp
using System.Linq;
using System.Xml;
using System.Windows.Media;
using OATControl.Properties;
```

Replace the `AvailableThemes` property and add new members:

```csharp
public static readonly string UserThemesFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "OpenAstroTracker", "Themes");

private static readonly List<string> BuiltinThemes = new List<string>
{
    "DarkAstronomy",
    "Daylight"
};

private readonly List<string> _userThemes = new List<string>();

public List<string> AvailableThemes { get; } = new List<string> { "DarkAstronomy", "Daylight" };

public bool IsUserTheme(string themeName) => _userThemes.Contains(themeName);
```

- [ ] **Step 2: Add ScanUserThemes method**

```csharp
public void ScanUserThemes()
{
    try
    {
        Directory.CreateDirectory(UserThemesFolder);
    }
    catch { return; }

    foreach (var file in Directory.GetFiles(UserThemesFolder, "*.xaml"))
    {
        var name = Path.GetFileNameWithoutExtension(file);
        if (string.IsNullOrEmpty(name) || AvailableThemes.Contains(name))
            continue;

        if (!IsValidThemeFile(file))
            continue;

        _userThemes.Add(name);
        AvailableThemes.Add(name);
    }
}
```

- [ ] **Step 3: Add IsValidThemeFile method**

```csharp
private bool IsValidThemeFile(string path)
{
    try
    {
        var dict = new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) };
        return ThemeColorDefinitions.Colors.Any(c => dict.Contains(c.Key));
    }
    catch
    {
        return false;
    }
}
```

- [ ] **Step 3b: Add ReloadUserThemes method**

```csharp
public void ReloadUserThemes()
{
    _userThemes.Clear();
    // Remove user themes from AvailableThemes, keeping built-in
    foreach (var name in AvailableThemes.ToList())
    {
        if (!BuiltinThemes.Contains(name))
            AvailableThemes.Remove(name);
    }
    ScanUserThemes();
}
```

- [ ] **Step 4: Add GenerateBrushes method**

```csharp
private void GenerateBrushes(ResourceDictionary dict)
{
    foreach (var def in ThemeColorDefinitions.Colors)
    {
        if (dict[def.Key] is Color color)
        {
            var brushKey = ThemeColorDefinitions.BrushKeyFromColorKey(def.Key);
            dict[brushKey] = new SolidColorBrush(color);
        }
    }
}
```

- [ ] **Step 5: Update ApplyTheme to handle user themes**

Replace the existing `ApplyTheme` method:

```csharp
public void ApplyTheme(string themeName)
{
    if (!AvailableThemes.Contains(themeName))
        throw new ArgumentException($"Theme '{themeName}' is not available.");

    var mergedDicts = Application.Current.Resources.MergedDictionaries;

    if (_currentThemeDictionary != null)
    {
        mergedDicts.Remove(_currentThemeDictionary);
    }

    ResourceDictionary themeDict;

    if (_userThemes.Contains(themeName))
    {
        var path = Path.Combine(UserThemesFolder, $"{themeName}.xaml");
        themeDict = new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) };
        GenerateBrushes(themeDict);
    }
    else
    {
        themeDict = new ResourceDictionary
        {
            Source = new Uri($"{ThemeDictionaryPrefix}{themeName}.xaml")
        };
    }

    mergedDicts.Insert(0, themeDict);

    _currentThemeDictionary = themeDict;
    CurrentTheme = themeName;

    SetupWatcher(themeName);
}
```

- [ ] **Step 6: Add GetThemeMetadata method**

```csharp
public (string Name, string Author) GetThemeMetadata(string themeName)
{
    string name = themeName;
    string author = "";

    try
    {
        ResourceDictionary dict;
        if (_userThemes.Contains(themeName))
        {
            var path = Path.Combine(UserThemesFolder, $"{themeName}.xaml");
            dict = new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) };
        }
        else
        {
            dict = new ResourceDictionary
            {
                Source = new Uri($"{ThemeDictionaryPrefix}{themeName}.xaml")
            };
        }

        if (dict["ThemeName"] is string themeNameValue)
            name = themeNameValue;
        if (dict["ThemeAuthor"] is string themeAuthorValue)
            author = themeAuthorValue;
    }
    catch { }

    return (name, author);
}
```

- [ ] **Step 7: Add GetThemeColors method (for the editor)**

```csharp
public Dictionary<string, Color> GetThemeColors(string themeName)
{
    var result = new Dictionary<string, Color>();

    ResourceDictionary dict;
    if (_userThemes.Contains(themeName))
    {
        var path = Path.Combine(UserThemesFolder, $"{themeName}.xaml");
        dict = new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) };
    }
    else
    {
        dict = new ResourceDictionary
        {
            Source = new Uri($"{ThemeDictionaryPrefix}{themeName}.xaml")
        };
    }

    foreach (var def in ThemeColorDefinitions.Colors)
    {
        if (dict[def.Key] is Color color)
            result[def.Key] = color;
    }

    return result;
}
```

- [ ] **Step 8: Add ImportTheme method**

```csharp
public void ImportTheme(string sourceFilePath)
{
    if (!IsValidThemeFile(sourceFilePath))
        throw new InvalidOperationException("Not a valid theme file.");

    var name = Path.GetFileNameWithoutExtension(sourceFilePath);
    var destPath = Path.Combine(UserThemesFolder, Path.GetFileName(sourceFilePath));

    Directory.CreateDirectory(UserThemesFolder);
    File.Copy(sourceFilePath, destPath, overwrite: true);

    if (!_userThemes.Contains(name))
    {
        _userThemes.Add(name);
        AvailableThemes.Add(name);
    }
}
```

- [ ] **Step 9: Add ExportTheme method**

```csharp
public void ExportTheme(string themeName, string author, string destinationPath)
{
    var colors = GetThemeColors(themeName);
    var (existingName, _) = GetThemeMetadata(themeName);

    using (var writer = XmlWriter.Create(destinationPath, new XmlWriterSettings
    {
        Indent = true,
        OmitXmlDeclaration = true
    }))
    {
        writer.WriteStartElement("ResourceDictionary");
        writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
        writer.WriteAttributeString("xmlns:x", "http://schemas.microsoft.com/winfx/2006/xaml");

        writer.WriteStartElement("sys", "String", "clr-namespace:System;assembly=mscorlib");
        writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", "ThemeName");
        writer.WriteString(existingName ?? themeName);
        writer.WriteEndElement();

        writer.WriteStartElement("sys", "String", "clr-namespace:System;assembly=mscorlib");
        writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", "ThemeAuthor");
        writer.WriteString(author ?? "");
        writer.WriteEndElement();

        foreach (var def in ThemeColorDefinitions.Colors)
        {
            if (colors.TryGetValue(def.Key, out var color))
            {
                writer.WriteStartElement("Color");
                writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", def.Key);
                writer.WriteString(color.ToString());
                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
    }
}
```

- [ ] **Step 10: Add DeleteTheme method**

```csharp
public void DeleteTheme(string themeName)
{
    if (!_userThemes.Contains(themeName))
        throw new InvalidOperationException("Cannot delete built-in themes.");

    var path = Path.Combine(UserThemesFolder, $"{themeName}.xaml");
    if (File.Exists(path))
        File.Delete(path);

    _userThemes.Remove(themeName);
    AvailableThemes.Remove(themeName);

    if (CurrentTheme == themeName)
        ApplyTheme("DarkAstronomy");
}
```

- [ ] **Step 11: Add SaveUserTheme method (for the editor)**

```csharp
public void SaveUserTheme(string themeName, string author, Dictionary<string, Color> colors)
{
    Directory.CreateDirectory(UserThemesFolder);
    var path = Path.Combine(UserThemesFolder, $"{themeName}.xaml");

    ExportTheme(
        ThemeColorDefinitions.Colors[0].Key, // use first key as dummy — ExportTheme reads the dict
        author,
        path);

    // Now rewrite with the editor's actual colors
    using (var writer = XmlWriter.Create(path, new XmlWriterSettings
    {
        Indent = true,
        OmitXmlDeclaration = true
    }))
    {
        writer.WriteStartElement("ResourceDictionary");
        writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
        writer.WriteAttributeString("xmlns:x", "http://schemas.microsoft.com/winfx/2006/xaml");

        // Use XmlnsDefinition for sys prefix
        writer.WriteAttributeString("xmlns:sys", "clr-namespace:System;assembly=mscorlib");

        writer.WriteStartElement("sys:String");
        writer.WriteAttributeString("x:Key", "ThemeName");
        writer.WriteString(themeName);
        writer.WriteEndElement();

        if (!string.IsNullOrEmpty(author))
        {
            writer.WriteStartElement("sys:String");
            writer.WriteAttributeString("x:Key", "ThemeAuthor");
            writer.WriteString(author);
            writer.WriteEndElement();
        }

        foreach (var def in ThemeColorDefinitions.Colors)
        {
            if (colors.TryGetValue(def.Key, out var color))
            {
                writer.WriteStartElement("Color");
                writer.WriteAttributeString("x:Key", def.Key);
                writer.WriteString(color.ToString());
                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
    }

    if (!_userThemes.Contains(themeName))
    {
        _userThemes.Add(themeName);
        AvailableThemes.Add(themeName);
    }
}
```

Note: The `SaveUserTheme` method uses XAML attribute syntax. The `x:Key` prefix resolution works because the `x` namespace is declared in the root element. For the `sys:String` elements, we declare `xmlns:sys` on the root element. The resulting XML will look exactly like the format in the spec.

- [ ] **Step 12: Wire ScanUserThemes into App.xaml.cs**

In `App.xaml.cs`, add the scan call after the existing theme application:

```csharp
ThemeManager.Instance.ScanUserThemes();
```

Insert this line **before** `ThemeManager.Instance.ApplyTheme(savedTheme);` so user themes are available when the saved theme name is resolved.

The startup block should read:
```csharp
AppSettings.Instance.Load();
var savedTheme = AppSettings.Instance.ThemeName;
#if DEBUG
ThemeManager.Instance.HotReloadEnabled = true;
#endif
ThemeManager.Instance.ScanUserThemes();
ThemeManager.Instance.ApplyTheme(savedTheme);
```

- [ ] **Step 13: Commit**

```bash
git add OATControl/Theming/ThemeManager.cs OATControl/App.xaml.cs
git commit -m "Extend ThemeManager with user theme scanning, loading, brush generation, import/export/delete"
```

---

### Task 3: Create the Theme Editor dialog

**Files:**
- Create: `OATControl/DlgThemeEditor.xaml`
- Create: `OATControl/DlgThemeEditor.xaml.cs`

This is the largest task. The editor has three panels: theme list (left), color grid (center), live preview (right).

- [ ] **Step 1: Create DlgThemeEditor.xaml**

```xml
<controls:ThemedWindow x:Class="OATControl.DlgThemeEditor"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="clr-namespace:OATControl.Controls"
        xmlns:converters="clr-namespace:OATControl.Converters"
        Title="Theme Editor" Width="900" Height="620"
        MinWidth="700" MinHeight="500"
        WindowStartupLocation="CenterOwner"
        Loaded="Window_Loaded">

    <controls:ThemedWindow.Resources>
        <!-- Force Daylight theme for the editor chrome -->
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/OATControl;component/Resources/Themes/Daylight.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </controls:ThemedWindow.Resources>

    <Grid Margin="8">
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Main three-panel area -->
        <Grid Grid.Row="0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="180"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="220"/>
            </Grid.ColumnDefinitions>

            <!-- Left panel: Theme list -->
            <Border Grid.Column="0" BorderBrush="#FFCCCCCC" BorderThickness="1" Margin="0,0,4,0">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Top" Text="Themes" FontWeight="Bold" FontSize="14"
                               Margin="6,6,6,4" Foreground="#FF333333"/>
                    <ListBox x:Name="ThemeListBox" Margin="4,0,4,4"
                             SelectionChanged="ThemeListBox_SelectionChanged"/>
                    <StackPanel DockPanel.Dock="Bottom" Margin="4,4,4,4" Orientation="Vertical">
                        <Button Content="New Theme" Click="OnNewTheme" Margin="0,0,0,4" Padding="4,2"/>
                        <Button Content="Import Theme" Click="OnImportTheme" Padding="4,2"/>
                    </StackPanel>
                </DockPanel>
            </Border>

            <!-- Center panel: Color editor grid -->
            <Border Grid.Column="1" BorderBrush="#FFCCCCCC" BorderThickness="1" Margin="4,0">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Top" FontWeight="Bold" FontSize="14"
                               Margin="6,6,6,4" Foreground="#FF333333">
                        <Run Text="Colors"/>
                        <Run Text=" (" x:Name="EditingLabel"/><Run x:Name="ThemeNameLabel"/></Run>
                    </TextBlock>
                    <ScrollViewer VerticalScrollBarVisibility="Auto" Margin="4,0,4,4">
                        <ItemsControl x:Name="ColorEditorGrid">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Grid Margin="0,2">
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="140"/>
                                            <ColumnDefinition Width="40"/>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="Auto"/>
                                        </Grid.ColumnDefinitions>
                                        <TextBlock Grid.Column="0" Text="{Binding DisplayName}"
                                                   VerticalAlignment="Center" FontSize="12" Foreground="#FF333333"/>
                                        <Border Grid.Column="1" Width="28" Height="20"
                                                BorderBrush="#FF999999" BorderThickness="1"
                                                CornerRadius="2" Margin="0,0,4,0">
                                            <Border.Background>
                                                <SolidColorBrush Color="{Binding Color}"/>
                                            </Border.Background>
                                        </Border>
                                        <TextBox Grid.Column="2" Text="{Binding HexValue, UpdateSourceTrigger=PropertyChanged}"
                                                 VerticalAlignment="Center" FontSize="11"
                                                 Margin="0,0,4,0" Width="80"
                                                 Background="#FFF0F0F0" Foreground="#FF333333"
                                                 BorderBrush="#FFCCCCCC"/>
                                        <StackPanel Grid.Column="3" Orientation="Horizontal">
                                            <Button Content="Pick" Click="OnPickColor" Tag="{Binding Key}"
                                                    Padding="6,1" Margin="0,0,2,0" FontSize="10"/>
                                            <Button Content="Reset" Click="OnResetColor" Tag="{Binding Key}"
                                                    Padding="6,1" FontSize="10"/>
                                        </StackPanel>
                                    </Grid>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </ScrollViewer>
                </DockPanel>
            </Border>

            <!-- Right panel: Live preview -->
            <Border Grid.Column="2" BorderBrush="#FFCCCCCC" BorderThickness="1" Margin="4,0,0,0">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Top" Text="Preview" FontWeight="Bold" FontSize="14"
                               Margin="6,6,6,4" Foreground="#FF333333"/>
                    <Border x:Name="PreviewPanel" Margin="4,0,4,4" CornerRadius="2">
                        <StackPanel Margin="8">
                            <!-- Preview uses DynamicResource bound to a local dict we swap -->
                            <TextBlock Text="Sample Title" FontSize="16" FontWeight="Bold"
                                       Foreground="{DynamicResource AppForegroundBrush}"/>
                            <TextBlock Text="Normal text" FontSize="12" Margin="0,4,0,8"
                                       Foreground="{DynamicResource AppForegroundSubtleBrush}"/>
                            <Border BorderThickness="1" CornerRadius="2" Padding="6,4" Margin="0,0,0,6"
                                    BorderBrush="{DynamicResource AppBorderBrush}"
                                    Background="{DynamicResource AppInputBackgroundBrush}">
                                <TextBlock Text="Input field" FontSize="12"
                                           Foreground="{DynamicResource AppForegroundBrush}"/>
                            </Border>
                            <Border BorderThickness="1" CornerRadius="2" Padding="8,4" Margin="0,0,0,6"
                                    BorderBrush="{DynamicResource AppButtonBorderBrush}"
                                    Background="{DynamicResource AppButtonBackgroundBrush}">
                                <TextBlock Text="Button" FontSize="12" HorizontalAlignment="Center"
                                           Foreground="{DynamicResource AppForegroundBrush}"/>
                            </Border>
                            <Border BorderThickness="1" CornerRadius="2" Padding="8,4" Margin="0,0,0,6"
                                    Background="{DynamicResource AppDisabledBackgroundBrush}"
                                    BorderBrush="{DynamicResource AppDisabledBorderBrush}">
                                <TextBlock Text="Disabled" FontSize="12" HorizontalAlignment="Center"
                                           Foreground="{DynamicResource AppDisabledForegroundBrush}"/>
                            </Border>
                            <CheckBox Content="Checkbox" IsChecked="True" Margin="0,0,0,6"
                                      Foreground="{DynamicResource AppForegroundBrush}"/>
                            <Border Background="{DynamicResource AppTooltipBackgroundBrush}"
                                    CornerRadius="2" Padding="6,4" Margin="0,0,0,4">
                                <TextBlock Text="Tooltip" FontSize="11"
                                           Foreground="{DynamicResource AppTooltipForegroundBrush}"/>
                            </Border>
                        </StackPanel>
                    </Border>
                </DockPanel>
            </Border>
        </Grid>

        <!-- Bottom bar: actions -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,8,0,0">
            <Button Content="Save" Click="OnSave" Width="70" Margin="0,0,8,0" Padding="0,4"/>
            <Button Content="Save As..." Click="OnSaveAs" Width="80" Margin="0,0,8,0" Padding="0,4"/>
            <Button Content="Export" Click="OnExport" Width="70" Margin="0,0,8,0" Padding="0,4"/>
            <Button Content="Cancel" Click="OnCancel" Width="70" Padding="0,4"/>
        </StackPanel>
    </Grid>
</controls:ThemedWindow>
```

- [ ] **Step 2: Create DlgThemeEditor.xaml.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using OATControl.Controls;
using OATControl.Theming;

namespace OATControl
{
    public class ColorEditItem : INotifyPropertyChanged
    {
        private Color _color;
        private string _hexValue;

        public string Key { get; }
        public string DisplayName { get; }
        public string Group { get; }
        public Color DefaultColor { get; }

        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    HexValue = value.ToString();
                    OnPropertyChanged(nameof(Color));
                }
            }
        }

        public string HexValue
        {
            get => _hexValue;
            set
            {
                if (_hexValue != value)
                {
                    _hexValue = value;
                    if (TryParseColor(value, out var parsed))
                    {
                        _color = parsed;
                        OnPropertyChanged(nameof(Color));
                    }
                    OnPropertyChanged(nameof(HexValue));
                }
            }
        }

        private static bool TryParseColor(string text, out Color color)
        {
            try
            {
                color = (Color)ColorConverter.ConvertFromString(text);
                return true;
            }
            catch
            {
                color = Colors.Transparent;
                return false;
            }
        }

        public ColorEditItem(string key, string displayName, string group, Color current, Color defaultColor)
        {
            Key = key;
            DisplayName = displayName;
            Group = group;
            DefaultColor = defaultColor;
            _color = current;
            _hexValue = current.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public partial class DlgThemeEditor : ThemedWindow, INotifyPropertyChanged
    {
        private ObservableCollection<ColorEditItem> _colorItems = new ObservableCollection<ColorEditItem>();
        private string _editingTheme;
        private string _themeAuthor = "";
        private bool _isNewTheme;
        private ResourceDictionary _previewDict;

        public DlgThemeEditor(string themeName = null, bool isNew = false)
        {
            _editingTheme = themeName;
            _isNewTheme = isNew;
            _previewDict = new ResourceDictionary();

            DataContext = this;
            InitializeComponent();
        }

        public ObservableCollection<ColorEditItem> ColorItems => _colorItems;

        public string EditingThemeName
        {
            get => _editingTheme;
            set { _editingTheme = value; OnPropertyChanged(); }
        }

        public string ThemeAuthor
        {
            get => _themeAuthor;
            set { _themeAuthor = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Populate theme list
            ThemeListBox.ItemsSource = ThemeManager.Instance.AvailableThemes.ToList();
            if (!_isNewTheme && !string.IsNullOrEmpty(_editingTheme))
                ThemeListBox.SelectedItem = _editingTheme;

            LoadThemeColors();
        }

        private void LoadThemeColors()
        {
            _colorItems.Clear();

            var defaults = ThemeColorDefinitions.Colors;
            var themeColors = new Dictionary<string, Color>();

            if (!string.IsNullOrEmpty(_editingTheme) && !_isNewTheme)
            {
                themeColors = ThemeManager.Instance.GetThemeColors(_editingTheme);
                var (name, author) = ThemeManager.Instance.GetThemeMetadata(_editingTheme);
                if (!string.IsNullOrEmpty(name)) EditingThemeName = name;
                ThemeAuthor = author;
            }
            else if (!string.IsNullOrEmpty(_editingTheme) && _isNewTheme)
            {
                // New theme starts from current theme's colors
                themeColors = ThemeManager.Instance.GetThemeColors(
                    ThemeManager.Instance.CurrentTheme);
                EditingThemeName = "New Theme";
            }

            foreach (var def in defaults)
            {
                var current = themeColors.TryGetValue(def.Key, out var c) ? c : def.DefaultColor;
                _colorItems.Add(new ColorEditItem(def.Key, def.DisplayName, def.Group, current, def.DefaultColor));
            }

            ColorEditorGrid.ItemsSource = _colorItems;
            UpdatePreview();
        }

        private void ThemeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeListBox.SelectedItem is string themeName)
            {
                _editingTheme = themeName;
                _isNewTheme = false;
                LoadThemeColors();
            }
        }

        private void OnPickColor(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
            {
                var item = _colorItems.FirstOrDefault(c => c.Key == key);
                if (item == null) return;

                using (var dlg = new ColorDialog())
                {
                    dlg.Color = System.Drawing.Color.FromArgb(item.Color.A, item.Color.R, item.Color.G, item.Color.B);
                    dlg.FullOpen = true;
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        item.Color = Color.FromArgb(dlg.Color.A, dlg.Color.R, dlg.Color.G, dlg.Color.B);
                        UpdatePreview();
                    }
                }
            }
        }

        private void OnResetColor(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
            {
                var item = _colorItems.FirstOrDefault(c => c.Key == key);
                if (item != null)
                {
                    item.Color = item.DefaultColor;
                    UpdatePreview();
                }
            }
        }

        private void UpdatePreview()
        {
            // Remove old preview dict from PreviewPanel
            if (_previewDict != null)
                PreviewPanel.Resources.MergedDictionaries.Remove(_previewDict);

            _previewDict = new ResourceDictionary();
            foreach (var item in _colorItems)
            {
                _previewDict[ThemeColorDefinitions.BrushKeyFromColorKey(item.Key)] = new SolidColorBrush(item.Color);
            }

            PreviewPanel.Resources.MergedDictionaries.Add(_previewDict);
        }

        private void OnNewTheme(object sender, RoutedEventArgs e)
        {
            _editingTheme = ThemeManager.Instance.CurrentTheme;
            _isNewTheme = true;
            LoadThemeColors();
        }

        private void OnImportTheme(object sender, RoutedEventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Theme files (*.xaml)|*.xaml|All files (*.*)|*.*";
                dlg.Title = "Import Theme";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        ThemeManager.Instance.ImportTheme(dlg.FileName);
                        var name = Path.GetFileNameWithoutExtension(dlg.FileName);
                        ThemeListBox.ItemsSource = ThemeManager.Instance.AvailableThemes.ToList();
                        _editingTheme = name;
                        _isNewTheme = false;
                        ThemeListBox.SelectedItem = name;
                        LoadThemeColors();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed to import theme: {ex.Message}", "Import Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            SaveCurrentTheme(_editingTheme ?? "MyTheme");
        }

        private void OnSaveAs(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Width = 320,
                Height = 180,
                Title = "Save Theme As",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stack = new StackPanel { Margin = new Thickness(12) };
            stack.Children.Add(new TextBlock { Text = "Theme Name:", Margin = new Thickness(0, 0, 0, 4) });
            var nameBox = new TextBox { Text = EditingThemeName ?? "MyTheme", Margin = new Thickness(0, 0, 0, 8) };
            stack.Children.Add(nameBox);
            stack.Children.Add(new TextBlock { Text = "Author:", Margin = new Thickness(0, 0, 0, 4) });
            var authorBox = new TextBox { Text = ThemeAuthor, Margin = new Thickness(0, 0, 0, 8) };
            stack.Children.Add(authorBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var saveBtn = new Button { Content = "Save", Width = 60, Margin = new Thickness(0, 0, 8, 0) };
            var cancelBtn = new Button { Content = "Cancel", Width = 60 };
            btnPanel.Children.Add(saveBtn);
            btnPanel.Children.Add(cancelBtn);
            stack.Children.Add(btnPanel);

            dialog.Content = stack;

            cancelBtn.Click += (s, args) => dialog.DialogResult = false;
            saveBtn.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    System.Windows.MessageBox.Show("Please enter a theme name.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                dialog.DialogResult = true;
            };

            if (dialog.ShowDialog() == true)
            {
                EditingThemeName = nameBox.Text.Trim();
                ThemeAuthor = authorBox.Text.Trim();

                // Sanitize file name: replace non-alphanumeric with underscore
                var fileName = new string(EditingThemeName.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
                SaveCurrentTheme(fileName);
            }
        }

        private void WriteThemeFile(string path, string themeName, string author, Dictionary<string, Color> colors)
        {
            using (var writer = System.Xml.XmlWriter.Create(path, new System.Xml.XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            }))
            {
                writer.WriteStartElement("ResourceDictionary");
                writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                writer.WriteAttributeString("xmlns:x", "http://schemas.microsoft.com/winfx/2006/xaml");
                writer.WriteAttributeString("xmlns:sys", "clr-namespace:System;assembly=mscorlib");

                writer.WriteStartElement("sys:String");
                writer.WriteAttributeString("x:Key", "ThemeName");
                writer.WriteString(themeName);
                writer.WriteEndElement();

                if (!string.IsNullOrEmpty(author))
                {
                    writer.WriteStartElement("sys:String");
                    writer.WriteAttributeString("x:Key", "ThemeAuthor");
                    writer.WriteString(author);
                    writer.WriteEndElement();
                }

                foreach (var def in ThemeColorDefinitions.Colors)
                {
                    if (colors.TryGetValue(def.Key, out var color))
                    {
                        writer.WriteStartElement("Color");
                        writer.WriteAttributeString("x:Key", def.Key);
                        writer.WriteString(color.ToString());
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }
        }

        private void SaveCurrentTheme(string fileSafeName)
        {
            try
            {
                var colors = _colorItems.ToDictionary(i => i.Key, i => i.Color);
                Directory.CreateDirectory(ThemeManager.UserThemesFolder);
                var path = Path.Combine(ThemeManager.UserThemesFolder, $"{fileSafeName}.xaml");
                WriteThemeFile(path, EditingThemeName ?? fileSafeName, ThemeAuthor, colors);

                if (!ThemeManager.Instance.IsUserTheme(fileSafeName))
                    ThemeManager.Instance.ReloadUserThemes();
                ThemeManager.Instance.ApplyTheme(fileSafeName);

                _editingTheme = fileSafeName;
                _isNewTheme = false;
                ThemeListBox.ItemsSource = ThemeManager.Instance.AvailableThemes.ToList();
                ThemeListBox.SelectedItem = fileSafeName;

                System.Windows.MessageBox.Show($"Theme '{EditingThemeName}' saved.", "Theme Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save theme: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnExport(object sender, RoutedEventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "Theme files (*.xaml)|*.xaml";
                dlg.Title = "Export Theme";
                dlg.FileName = (_editingTheme ?? "MyTheme") + ".xaml";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        // Write current editor colors directly to file
                        var colors = _colorItems.ToDictionary(i => i.Key, i => i.Color);
                        WriteThemeFile(dlg.FileName, EditingThemeName ?? _editingTheme ?? "MyTheme", ThemeAuthor, colors);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed to export theme: {ex.Message}", "Export Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
```

- [ ] **Step 3: Add files to .csproj if needed**

Check and add `DlgThemeEditor.xaml`, `DlgThemeEditor.xaml.cs` to the `.csproj` if it uses explicit includes.

- [ ] **Step 4: Add reference to System.Windows.Forms**

The editor uses `ColorDialog` and `OpenFileDialog/SaveFileDialog` from `System.Windows.Forms`. Add a reference in the `.csproj`:

```xml
<Reference Include="System.Windows.Forms" />
```

Also ensure `using System.Windows.Forms;` is at the top of `DlgThemeEditor.xaml.cs`.

- [ ] **Step 5: Commit**

```bash
git add OATControl/DlgThemeEditor.xaml OATControl/DlgThemeEditor.xaml.cs OATControl/OATControl.csproj
git commit -m "Add DlgThemeEditor — visual theme editor with live preview"
```

---

### Task 4: Update DlgAppSettings — theme management buttons

**Files:**
- Modify: `OATControl/DlgAppSettings.xaml`
- Modify: `OATControl/DlgAppSettings.xaml.cs`

- [ ] **Step 1: Add buttons below the Theme ComboBox in DlgAppSettings.xaml**

After the Theme ComboBox (around line 386), add a row of buttons. Add a new row to the grid and place the buttons there:

Find the Grid.RowDefinitions block in the "General" tab (line 329-337) and add one more row:

```xml
<RowDefinition Height="Auto"/>
```

Then after the ComboBox block (line 380-386), add:

```xml
<StackPanel Grid.Row="3" Grid.Column="1" Orientation="Horizontal" Margin="10,4,0,0">
    <Button Content="Edit Theme" Click="OnEditTheme" Width="80" Margin="0,0,4,0"
            Style="{StaticResource AccentedDialogSquareButton}" x:Name="EditThemeButton"/>
    <Button Content="Create Theme" Click="OnCreateTheme" Width="90" Margin="0,0,4,0"
            Style="{StaticResource AccentedDialogSquareButton}"/>
    <Button Content="Import Theme" Click="OnImportTheme" Width="90"
            Style="{StaticResource AccentedDialogSquareButton}"/>
</StackPanel>
```

- [ ] **Step 2: Add handler methods in DlgAppSettings.xaml.cs**

Add these methods to the `DlgAppSettings` class:

```csharp
private void OnEditTheme(object sender, RoutedEventArgs e)
{
    if (ThemeComboBox.SelectedItem is string themeName && ThemeManager.Instance.IsUserTheme(themeName))
    {
        var dlg = new DlgThemeEditor(themeName) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        dlg.ShowDialog();
        // Refresh the list in case themes were added/renamed
        ThemeComboBox.ItemsSource = ThemeManager.Instance.AvailableThemes;
        ThemeComboBox.SelectedItem = ThemeManager.Instance.CurrentTheme;
    }
}

private void OnCreateTheme(object sender, RoutedEventArgs e)
{
    var dlg = new DlgThemeEditor(null, isNew: true) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
    dlg.ShowDialog();
    ThemeComboBox.ItemsSource = ThemeManager.Instance.AvailableThemes;
    ThemeComboBox.SelectedItem = ThemeManager.Instance.CurrentTheme;
}

private void OnImportTheme(object sender, RoutedEventArgs e)
{
    using (var dlg = new System.Windows.Forms.OpenFileDialog())
    {
        dlg.Filter = "Theme files (*.xaml)|*.xaml|All files (*.*)|*.*";
        dlg.Title = "Import Theme";
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            try
            {
                ThemeManager.Instance.ImportTheme(dlg.FileName);
                ThemeComboBox.ItemsSource = ThemeManager.Instance.AvailableThemes;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to import theme: {ex.Message}", "Import Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
```

- [ ] **Step 3: Disable Edit button for built-in themes**

In the `ThemeComboBox_SelectionChanged` handler, add logic to enable/disable the Edit button:

```csharp
private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (ThemeComboBox.SelectedItem is string themeName)
    {
        ThemeManager.Instance.ApplyTheme(themeName);
        AppSettings.Instance.ThemeName = themeName;
        AppSettings.Instance.Save();
        EditThemeButton.IsEnabled = ThemeManager.Instance.IsUserTheme(themeName);
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add OATControl/DlgAppSettings.xaml OATControl/DlgAppSettings.xaml.cs
git commit -m "Add theme management buttons (edit/create/import) to settings dialog"
```

---

### Task 5: Manual testing and polish

- [ ] **Step 1: Build and verify compilation**

Run:
```bash
msbuild OATControl/OATControl.sln /p:Configuration=Debug /p:Platform="Any CPU"
```

- [ ] **Step 2: Test theme scanning**

- Place a valid theme `.xaml` file in `%AppData%\OpenAstroTracker\Themes\`
- Launch the app, verify it appears in the theme ComboBox
- Place a non-theme `.xaml` file, verify it does NOT appear
- Place a corrupt `.xaml` file, verify the app starts without error

- [ ] **Step 3: Test theme editor**

- Open Settings, click "Create Theme" — verify editor opens with current theme's colors
- Edit a color, verify the preview panel updates
- Click Save — verify the theme file is created in AppData
- Click Save As — verify the name/author dialog works
- Click Export — verify a file is saved at the chosen location
- Close and reopen settings — verify the custom theme appears in the ComboBox

- [ ] **Step 4: Test import/export**

- Export a built-in theme to a file
- Import that file as a new theme
- Verify the imported theme matches the original

- [ ] **Step 5: Test delete**

- Select a user theme in the editor, verify it can be deleted
- Verify built-in themes cannot be deleted
- Verify deleting the active theme falls back to DarkAstronomy

- [ ] **Step 6: Commit any fixes**

```bash
git add -A
git commit -m "Polish custom themes feature based on manual testing"
```
