# Custom Title Bar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a custom title bar with minimize/maximize/close buttons to all ThemedWindow instances, converting 5 remaining plain `Window` classes to `ThemedWindow`.

**Architecture:** A `ControlTemplate` in `Base.xaml` wraps `ThemedWindow` content in a `DockPanel` with a 30px title bar docked at the top. An attached property `ShowTitleBar` (default `true`) hides it on chromeless windows. A `[Flags]` enum `TitleBarButtons` controls which buttons appear. `ThemedWindow.cs` provides the attached properties and wires up `SystemCommands`.

**Tech Stack:** WPF, C#, System.Windows.Shell.WindowChrome, SystemCommands

---

### Task 1: Add TitleBarButtons enum, ShowTitleBar and TitleBarButtons attached properties to ThemedWindow.cs

**Files:**
- Modify: `OATControl/Controls/ThemedWindow.cs`

- [ ] **Step 1: Add the TitleBarButtons enum and attached properties**

Replace the entire contents of `ThemedWindow.cs` with:

```csharp
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;

namespace OATControl.Controls
{
    [Flags]
    public enum TitleBarButtons
    {
        None = 0,
        Minimize = 1,
        Maximize = 2,
        Close = 4,
        All = Minimize | Maximize | Close
    }

    public class ThemedWindow : Window
    {
        public static readonly DependencyProperty ShowTitleBarProperty = DependencyProperty.RegisterAttached(
            "ShowTitleBar", typeof(bool), typeof(ThemedWindow),
            new PropertyMetadata(true));

        public static readonly DependencyProperty TitleBarButtonsProperty = DependencyProperty.RegisterAttached(
            "TitleBarButtons", typeof(TitleBarButtons), typeof(ThemedWindow),
            new PropertyMetadata(TitleBarButtons.All));

        public ThemedWindow()
        {
            WindowStyle = WindowStyle.None;
            SetResourceReference(BackgroundProperty, "AppWindowBackgroundBrush");
            SetResourceReference(ForegroundProperty, "AppForegroundBrush");
            var chrome = new WindowChrome
            {
                CaptionHeight = 30,
                ResizeBorderThickness = new Thickness(4),
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);

            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimize));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximize));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestore));
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnClose));
        }

        public static bool GetShowTitleBar(DependencyObject obj) => (bool)obj.GetValue(ShowTitleBarProperty);
        public static void SetShowTitleBar(DependencyObject obj, bool value) => obj.SetValue(ShowTitleBarProperty, value);

        public static TitleBarButtons GetTitleBarButtons(DependencyObject obj) => (TitleBarButtons)obj.GetValue(TitleBarButtonsProperty);
        public static void SetTitleBarButtons(DependencyObject obj, TitleBarButtons value) => obj.SetValue(TitleBarButtonsProperty, value);

        private void OnMinimize(object sender, ExecutedRoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
        private void OnMaximize(object sender, ExecutedRoutedEventArgs e) => SystemCommands.MaximizeWindow(this);
        private void OnRestore(object sender, ExecutedRoutedEventArgs e) => SystemCommands.RestoreWindow(this);
        private void OnClose(object sender, ExecutedRoutedEventArgs e) => SystemCommands.CloseWindow(this);
    }
}
```

Key changes from original:
- Added `TitleBarButtons` flags enum
- Added `ShowTitleBar` attached property (defaults `true`)
- Added `TitleBarButtons` attached property (defaults `All`)
- Added `SystemCommands` command bindings for min/max/restore/close
- Changed `UseAeroCaptionButtons` from `true` to `false`

- [ ] **Step 2: Commit**

```bash
git add OATControl/Controls/ThemedWindow.cs
git commit -m "feat: add ShowTitleBar and TitleBarButtons attached properties to ThemedWindow"
```

---

### Task 2: Add the ThemedWindow Style with ControlTemplate to Base.xaml

**Files:**
- Modify: `OATControl/Resources/Themes/Base.xaml`

This task adds the implicit `Style` for `ThemedWindow` at the end of `Base.xaml`, before the closing `</ResourceDictionary>` tag. The style provides a `ControlTemplate` that renders the title bar and wraps the window content.

- [ ] **Step 1: Add the xmlns for controls and the ThemedWindow Style**

First, add the controls namespace to the `ResourceDictionary` tag at line 1. Change:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">
```

to:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib"
                    xmlns:controls="clr-namespace:OATControl.Controls">
```

Then, add the following `Style` block immediately before the closing `</ResourceDictionary>` tag (after the `MetroListBoxItem` style):

```xml
    <!-- ThemedWindow Style -->
    <Style TargetType="controls:ThemedWindow">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="controls:ThemedWindow">
                    <AdornerDecorator>
                        <Grid Background="{TemplateBinding Background}">
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto" />
                                <RowDefinition Height="*" />
                            </Grid.RowDefinitions>

                            <!-- Title Bar -->
                            <Border x:Name="TitleBarBorder"
                                    Grid.Row="0"
                                    Height="30"
                                    Background="{DynamicResource AppTitleBarBackgroundBrush}"
                                    BorderBrush="{DynamicResource AppBorderSubtleBrush}"
                                    BorderThickness="0,0,0,1"
                                    WindowChrome.IsHitTestVisibleInChrome="False">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*" />
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="Auto" />
                                    </Grid.ColumnDefinitions>

                                    <!-- Icon + Title -->
                                    <StackPanel Orientation="Horizontal" Margin="8,0,0,0" VerticalAlignment="Center">
                                        <Image Source="{TemplateBinding Icon}"
                                               Width="24" Height="24"
                                               VerticalAlignment="Center"
                                               RenderOptions.BitmapScalingMode="HighQuality" />
                                        <TextBlock Text="{TemplateBinding Title}"
                                                   Foreground="{DynamicResource AppTitleBarForegroundBrush}"
                                                   FontSize="12"
                                                   VerticalAlignment="Center"
                                                   Margin="6,0,0,0"
                                                   TextTrimming="CharacterEllipsis" />
                                    </StackPanel>

                                    <!-- Minimize Button -->
                                    <Button x:Name="MinimizeButton"
                                            Grid.Column="1"
                                            Width="46" Height="30"
                                            WindowChrome.IsHitTestVisibleInChrome="True"
                                            Command="SystemCommands.MinimizeWindowCommand"
                                            Cursor="Hand"
                                            Background="Transparent"
                                            BorderThickness="0">
                                        <Path Data="M16,0 L32,0 32,2 16,2z"
                                              Stretch="Uniform"
                                              Width="10" Height="1"
                                              Fill="{DynamicResource AppTitleBarForegroundBrush}" />
                                    </Button>

                                    <!-- Maximize Button -->
                                    <Button x:Name="MaximizeButton"
                                            Grid.Column="2"
                                            Width="46" Height="30"
                                            WindowChrome.IsHitTestVisibleInChrome="True"
                                            Command="SystemCommands.MaximizeWindowCommand"
                                            Cursor="Hand"
                                            Background="Transparent"
                                            BorderThickness="0">
                                        <Path Data="M0,0 L12,0 12,12 0,12z M1,1 L1,11 11,11 11,1z"
                                              Stretch="Uniform"
                                              Width="10" Height="10"
                                              Fill="{DynamicResource AppTitleBarForegroundBrush}" />
                                    </Button>

                                    <!-- Restore Button -->
                                    <Button x:Name="RestoreButton"
                                            Grid.Column="2"
                                            Width="46" Height="30"
                                            WindowChrome.IsHitTestVisibleInChrome="True"
                                            Command="SystemCommands.RestoreWindowCommand"
                                            Cursor="Hand"
                                            Background="Transparent"
                                            BorderThickness="0"
                                            Visibility="Collapsed">
                                        <Path Data="M2,0 L10,0 10,2 12,2 12,10 10,10 10,12 0,12 0,4 2,4z M2,2 L2,4 10,4 10,10 12,10 12,2z M0,4 L8,4 8,12 0,12z"
                                              Stretch="Uniform"
                                              Width="10" Height="10"
                                              Fill="{DynamicResource AppTitleBarForegroundBrush}" />
                                    </Button>

                                    <!-- Close Button -->
                                    <Button x:Name="CloseButton"
                                            Grid.Column="3"
                                            Width="46" Height="30"
                                            WindowChrome.IsHitTestVisibleInChrome="True"
                                            Command="SystemCommands.CloseWindowCommand"
                                            Cursor="Hand"
                                            Background="Transparent"
                                            BorderThickness="0">
                                        <Path Data="M0,0 L2,0 8,6 14,0 16,0 16,2 10,8 16,14 16,16 14,16 8,10 2,16 0,16 0,14 6,8 0,2z"
                                              Stretch="Uniform"
                                              Width="10" Height="10"
                                              Fill="{DynamicResource AppTitleBarForegroundBrush}" />
                                    </Button>
                                </Grid>
                            </Border>

                            <!-- Window Content -->
                            <ContentPresenter Grid.Row="1"
                                              Content="{TemplateBinding Content}"
                                              ContentTemplate="{TemplateBinding ContentTemplate}" />
                        </Grid>
                    </AdornerDecorator>
                </ControlTemplate>
            </Setter.Value>
        </Setter>

        <Style.Triggers>
            <!-- Hide title bar when ShowTitleBar=False -->
            <Trigger Property="controls:ThemedWindow.ShowTitleBar" Value="False">
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="controls:ThemedWindow">
                            <AdornerDecorator>
                                <ContentPresenter Content="{TemplateBinding Content}"
                                                  ContentTemplate="{TemplateBinding ContentTemplate}" />
                            </AdornerDecorator>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- Title bar button styles -->
    <Style x:Key="TitleBarButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="BorderThickness" Value="0" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="ButtonBorder"
                            Background="{TemplateBinding Background}"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="ButtonBorder" Property="Background" Value="{DynamicResource AppTitleBarButtonHoverBrush}" />
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="ButtonBorder" Property="Background" Value="{DynamicResource AppTitleBarButtonPressedBrush}" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
```

Note: The `TitleBarButtonStyle` is provided as a reusable base but the title bar buttons in the template use inline `Background="Transparent"` and `BorderThickness="0"`. The hover/press behavior is handled by this style. To wire it up, each button in the template needs `Style="{StaticResource TitleBarButtonStyle}"`. This will be refined in the next step.

Actually, since the style is defined after the template, we cannot use `StaticResource`. The buttons need their hover/press built into the template directly. Here is the corrected approach — replace the title bar buttons section with self-contained buttons that include their own hover/press triggers.

Replace the entire ThemedWindow Style block above with the following corrected version that embeds hover/press triggers directly in each button template:

```xml
    <!-- ThemedWindow Style -->
    <Style TargetType="controls:ThemedWindow">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="controls:ThemedWindow">
                    <AdornerDecorator>
                        <Grid Background="{TemplateBinding Background}">
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto" />
                                <RowDefinition Height="*" />
                            </Grid.RowDefinitions>

                            <!-- Title Bar -->
                            <Border x:Name="TitleBarBorder"
                                    Grid.Row="0"
                                    Height="30"
                                    Background="{DynamicResource AppTitleBarBackgroundBrush}"
                                    BorderBrush="{DynamicResource AppBorderSubtleBrush}"
                                    BorderThickness="0,0,0,1"
                                    WindowChrome.IsHitTestVisibleInChrome="False">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*" />
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="Auto" />
                                    </Grid.ColumnDefinitions>

                                    <!-- Icon + Title -->
                                    <StackPanel Orientation="Horizontal" Margin="8,0,0,0" VerticalAlignment="Center">
                                        <Image Source="{TemplateBinding Icon}"
                                               Width="24" Height="24"
                                               VerticalAlignment="Center"
                                               RenderOptions.BitmapScalingMode="HighQuality" />
                                        <TextBlock Text="{TemplateBinding Title}"
                                                   Foreground="{DynamicResource AppTitleBarForegroundBrush}"
                                                   FontSize="12"
                                                   VerticalAlignment="Center"
                                                   Margin="6,0,0,0"
                                                   TextTrimming="CharacterEllipsis" />
                                    </StackPanel>

                                    <!-- Minimize Button -->
                                    <Button x:Name="MinimizeButton"
                                            Grid.Column="1"
                                            Width="46" Height="30"
                                            WindowChrome.IsHitTestVisibleInChrome="True"
                                            Command="SystemCommands.MinimizeWindowCommand"
                                            Cursor="Hand">
                                        <Button.Template>
                                            <ControlTemplate TargetType="Button">
                                                <Border x:Name="Bd" Background="Transparent">
                                                    <Path Data="M16,0 L32,0 32,2 16,2z"
                                                          Stretch="Uniform" Width="10" Height="1"
                                                          Fill="{DynamicResource AppTitleBarForegroundBrush}"
                                                          HorizontalAlignment="Center" VerticalAlignment="Center" />
                                                </Border>
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property="IsMouseOver" Value="True">
                                                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource AppTitleBarButtonHoverBrush}" />
                                                    </Trigger>
                                                    <Trigger Property="IsPressed" Value="True">
                                                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource AppTitleBarButtonPressedBrush}" />
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </Button.Template>
                                    </Button>

                                    <!-- Maximize Button -->
                                    <Button x:Name="MaximizeButton"
                                            Grid.Column="2"
                                            Width="46" Height="30"
                                            WindowChrome.IsHitTestVisibleInChrome="True"
                                            Command="SystemCommands.MaximizeWindowCommand"
                                            Cursor="Hand">
                                        <Button.Template>
                                            <ControlTemplate TargetType="Button">
                                                <Border x:Name="Bd" Background="Transparent">
                                                    <Path Data="M0,0 L12,0 12,12 0,12z M1,1 L1,11 11,11 11,1z"
                                                          Stretch="Uniform" Width="10" Height="10"
                                                          Fill="{DynamicResource AppTitleBarForegroundBrush}"
                                                          HorizontalAlignment="Center" VerticalAlignment="Center" />
                                                </Border>
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property="IsMouseOver" Value="True">
                                                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource AppTitleBarButtonHoverBrush}" />
                                                    </Trigger>
                                                    <Trigger Property="IsPressed" Value="True">
                                                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource AppTitleBarButtonPressedBrush}" />
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </Button.Template>
                                    </Button>

                                    <!-- Restore Button -->
                                    <Button x:Name="RestoreButton"
                                            Grid.Column="2"
                                            Width="46" Height="30"
                                            WindowChrome.IsHitTestVisibleInChrome="True"
                                            Command="SystemCommands.RestoreWindowCommand"
                                            Cursor="Hand"
                                            Visibility="Collapsed">
                                        <Button.Template>
                                            <ControlTemplate TargetType="Button">
                                                <Border x:Name="Bd" Background="Transparent">
                                                    <Path Data="M2,0 L10,0 10,2 12,2 12,10 10,10 10,12 0,12 0,4 2,4z M2,2 L2,4 10,4 10,10 12,10 12,2z M0,4 L8,4 8,12 0,12z"
                                                          Stretch="Uniform" Width="10" Height="10"
                                                          Fill="{DynamicResource AppTitleBarForegroundBrush}"
                                                          HorizontalAlignment="Center" VerticalAlignment="Center" />
                                                </Border>
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property="IsMouseOver" Value="True">
                                                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource AppTitleBarButtonHoverBrush}" />
                                                    </Trigger>
                                                    <Trigger Property="IsPressed" Value="True">
                                                        <Setter TargetName="Bd" Property="Background" Value="{DynamicResource AppTitleBarButtonPressedBrush}" />
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </Button.Template>
                                    </Button>

                                    <!-- Close Button -->
                                    <Button x:Name="CloseButton"
                                            Grid.Column="3"
                                            Width="46" Height="30"
                                            WindowChrome.IsHitTestVisibleInChrome="True"
                                            Command="SystemCommands.CloseWindowCommand"
                                            Cursor="Hand">
                                        <Button.Template>
                                            <ControlTemplate TargetType="Button">
                                                <Border x:Name="Bd" Background="Transparent">
                                                    <Path Data="M0,0 L2,0 8,6 14,0 16,0 16,2 10,8 16,14 16,16 14,16 8,10 2,16 0,16 0,14 6,8 0,2z"
                                                          Stretch="Uniform" Width="10" Height="10"
                                                          Fill="{DynamicResource AppTitleBarForegroundBrush}"
                                                          HorizontalAlignment="Center" VerticalAlignment="Center" />
                                                </Border>
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property="IsMouseOver" Value="True">
                                                        <Setter TargetName="Bd" Property="Background" Value="#E81123" />
                                                        <Setter TargetName="Bd" Property="Padding" Value="0" />
                                                    </Trigger>
                                                    <Trigger Property="IsPressed" Value="True">
                                                        <Setter TargetName="Bd" Property="Background" Value="#F1707A" />
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </Button.Template>
                                    </Button>
                                </Grid>
                            </Border>

                            <!-- Window Content -->
                            <ContentPresenter Grid.Row="1"
                                              Content="{TemplateBinding Content}"
                                              ContentTemplate="{TemplateBinding ContentTemplate}" />
                        </Grid>
                    </AdornerDecorator>

                    <ControlTemplate.Triggers>
                        <!-- Maximize/Restore toggle -->
                        <Trigger Property="WindowState" Value="Maximized">
                            <Setter TargetName="MaximizeButton" Property="Visibility" Value="Collapsed" />
                            <Setter TargetName="RestoreButton" Property="Visibility" Value="Visible" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>

        <Style.Triggers>
            <!-- Hide title bar when ShowTitleBar=False -->
            <Trigger Property="controls:ThemedWindow.ShowTitleBar" Value="False">
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="controls:ThemedWindow">
                            <AdornerDecorator>
                                <ContentPresenter Content="{TemplateBinding Content}"
                                                  ContentTemplate="{TemplateBinding ContentTemplate}" />
                            </AdornerDecorator>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Trigger>
        </Style.Triggers>
    </Style>
```

Note: The `TitleBarButtons` visibility triggers cannot be done purely in XAML with a `[Flags]` enum using `DataTrigger` comparisons (WPF doesn't support bitmask checks in triggers). The button visibility for `TitleBarButtons` will need to be handled in code-behind. For now, all three buttons are always visible when the title bar is shown. Task 3 adds the code-behind logic for `TitleBarButtons`.

- [ ] **Step 2: Commit**

```bash
git add OATControl/Resources/Themes/Base.xaml
git commit -m "feat: add ThemedWindow ControlTemplate with custom title bar to Base.xaml"
```

---

### Task 3: Add TitleBarButtons visibility logic to ThemedWindow.cs

**Files:**
- Modify: `OATControl/Controls/ThemedWindow.cs`

WPF triggers cannot do bitmask comparisons on `[Flags]` enums. We need code-behind to toggle button visibility when `TitleBarButtons` changes or the template is applied.

- [ ] **Step 1: Add OnApplyTemplate override and TitleBarButtons change callback**

Replace the entire contents of `ThemedWindow.cs` with:

```csharp
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shell;

namespace OATControl.Controls
{
    [Flags]
    public enum TitleBarButtons
    {
        None = 0,
        Minimize = 1,
        Maximize = 2,
        Close = 4,
        All = Minimize | Maximize | Close
    }

    public class ThemedWindow : Window
    {
        public static readonly DependencyProperty ShowTitleBarProperty = DependencyProperty.RegisterAttached(
            "ShowTitleBar", typeof(bool), typeof(ThemedWindow),
            new PropertyMetadata(true));

        public static readonly DependencyProperty TitleBarButtonsProperty = DependencyProperty.RegisterAttached(
            "TitleBarButtons", typeof(TitleBarButtons), typeof(ThemedWindow),
            new PropertyMetadata(TitleBarButtons.All, OnTitleBarButtonsChanged));

        public ThemedWindow()
        {
            WindowStyle = WindowStyle.None;
            SetResourceReference(BackgroundProperty, "AppWindowBackgroundBrush");
            SetResourceReference(ForegroundProperty, "AppForegroundBrush");
            var chrome = new WindowChrome
            {
                CaptionHeight = 30,
                ResizeBorderThickness = new Thickness(4),
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);

            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimize));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximize));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestore));
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnClose));
        }

        public static bool GetShowTitleBar(DependencyObject obj) => (bool)obj.GetValue(ShowTitleBarProperty);
        public static void SetShowTitleBar(DependencyObject obj, bool value) => obj.SetValue(ShowTitleBarProperty, value);

        public static TitleBarButtons GetTitleBarButtons(DependencyObject obj) => (TitleBarButtons)obj.GetValue(TitleBarButtonsProperty);
        public static void SetTitleBarButtons(DependencyObject obj, TitleBarButtons value) => obj.SetValue(TitleBarButtonsProperty, value);

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateTitleBarButtonVisibility();
        }

        private static void OnTitleBarButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ThemedWindow window)
                window.UpdateTitleBarButtonVisibility();
        }

        private void UpdateTitleBarButtonVisibility()
        {
            var flags = GetTitleBarButtons(this);
            GetTemplateChild("MinimizeButton")?.SetValue(VisibilityProperty,
                (flags & TitleBarButtons.Minimize) != 0 ? Visibility.Visible : Visibility.Collapsed);
            GetTemplateChild("MaximizeButton")?.SetValue(VisibilityProperty,
                (flags & TitleBarButtons.Maximize) != 0 ? Visibility.Visible : Visibility.Collapsed);
            GetTemplateChild("RestoreButton")?.SetValue(VisibilityProperty,
                (flags & TitleBarButtons.Maximize) != 0 && WindowState == WindowState.Maximized ? Visibility.Visible : Visibility.Collapsed);
            GetTemplateChild("CloseButton")?.SetValue(VisibilityProperty,
                (flags & TitleBarButtons.Close) != 0 ? Visibility.Visible : Visibility.Collapsed);
        }

        private void OnMinimize(object sender, ExecutedRoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
        private void OnMaximize(object sender, ExecutedRoutedEventArgs e) => SystemCommands.MaximizeWindow(this);
        private void OnRestore(object sender, ExecutedRoutedEventArgs e) => SystemCommands.RestoreWindow(this);
        private void OnClose(object sender, ExecutedRoutedEventArgs e) => SystemCommands.CloseWindow(this);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add OATControl/Controls/ThemedWindow.cs
git commit -m "feat: add TitleBarButtons visibility logic to ThemedWindow"
```

---

### Task 4: Convert SlewPointsWindow to ThemedWindow (chromeless)

**Files:**
- Modify: `OATControl/SlewPointsWindow.xaml` (lines 1, 11)
- Modify: `OATControl/SlewPointsWindow.xaml.cs` (line 109)

- [ ] **Step 1: Update XAML root element**

In `SlewPointsWindow.xaml`, change line 1 from:

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
```

to:

```xml
<controls:ThemedWindow xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
```

Change line 11 from:

```xml
        Title="Slew Positions" MinHeight="250" MinWidth="405" WindowStyle="None" SizeToContent="Width">
```

to:

```xml
        Title="Slew Positions" MinHeight="250" MinWidth="405" SizeToContent="Width"
        controls:ThemedWindow.ShowTitleBar="False">
```

Change the closing tag (last line) from `</Window>` to `</controls:ThemedWindow>`.

Also remove the `Background` and `Foreground` attributes (lines 9-10) since `ThemedWindow` sets those automatically:

Remove:
```xml
		Background="{DynamicResource AppWindowBackgroundBrush}"
		Foreground="{DynamicResource AppForegroundBrush}"
```

- [ ] **Step 2: Update code-behind base class**

In `SlewPointsWindow.xaml.cs`, change line 109 from:

```csharp
	public partial class SlewPointsWindow : Window
```

to:

```csharp
	public partial class SlewPointsWindow : Controls.ThemedWindow
```

- [ ] **Step 3: Remove DragMove workaround**

In `SlewPointsWindow.xaml.cs`, the `TitleTextBlock_MouseLeftButtonDown` method (lines 245-252) manually calls `DragMove()`. Since ThemedWindow with `WindowChrome` handles dragging natively via the caption area, remove the `DragMove()` call from this method. But since this window has `ShowTitleBar="False"`, there's no caption area. Keep the `DragMove()` call — it's still needed for chromeless windows.

No change needed for this step.

- [ ] **Step 4: Commit**

```bash
git add OATControl/SlewPointsWindow.xaml OATControl/SlewPointsWindow.xaml.cs
git commit -m "refactor: convert SlewPointsWindow to ThemedWindow (chromeless)"
```

---

### Task 5: Convert DlgChecklist to ThemedWindow (chromeless)

**Files:**
- Modify: `OATControl/DlgChecklist.xaml` (lines 1, 10, 15-16)
- Modify: `OATControl/DlgChecklist.xaml.cs` (line 17)

- [ ] **Step 1: Update XAML root element**

In `DlgChecklist.xaml`, change line 1 from:

```xml
<Window x:Class="OATControl.DlgChecklist"
```

to:

```xml
<controls:ThemedWindow x:Class="OATControl.DlgChecklist"
```

Change line 10 from:

```xml
        WindowStyle="None"
```

to (remove `WindowStyle="None"`, add `ShowTitleBar="False"`):

```xml
        controls:ThemedWindow.ShowTitleBar="False"
```

Remove lines 15-16 (Background and Foreground, since ThemedWindow sets those):

```xml
		Background="{DynamicResource AppWindowBackgroundBrush}"
		Foreground="{DynamicResource AppForegroundBrush}"
```

Change the closing tag from `</Window>` to `</controls:ThemedWindow>`.

- [ ] **Step 2: Update code-behind base class**

In `DlgChecklist.xaml.cs`, change line 17 from:

```csharp
	public partial class DlgChecklist : Window, INotifyPropertyChanged
```

to:

```csharp
	public partial class DlgChecklist : Controls.ThemedWindow, INotifyPropertyChanged
```

- [ ] **Step 3: Commit**

```bash
git add OATControl/DlgChecklist.xaml OATControl/DlgChecklist.xaml.cs
git commit -m "refactor: convert DlgChecklist to ThemedWindow (chromeless)"
```

---

### Task 6: Convert MiniController to ThemedWindow (chromeless)

**Files:**
- Modify: `OATControl/MiniController.xaml` (lines 1, 11)
- Modify: `OATControl/MiniController.xaml.cs` (line 24)

- [ ] **Step 1: Update XAML root element**

In `MiniController.xaml`, change line 1 from:

```xml
<Window x:Class="OATControl.MiniController"
```

to:

```xml
<controls:ThemedWindow x:Class="OATControl.MiniController"
```

Change line 11 from:

```xml
        Title="Mini OAT Control" Height="85" MinHeight="85" MaxHeight="85" Width="Auto" MaxWidth="660" MinWidth="250" WindowStyle="None" SizeToContent="Width">
```

to:

```xml
        Title="Mini OAT Control" Height="85" MinHeight="85" MaxHeight="85" Width="Auto" MaxWidth="660" MinWidth="250" SizeToContent="Width"
        controls:ThemedWindow.ShowTitleBar="False">
```

Remove the `Background` and `Foreground` attributes (lines 9-10).

Change the closing tag from `</Window>` to `</controls:ThemedWindow>`.

- [ ] **Step 2: Update code-behind base class**

In `MiniController.xaml.cs`, change line 24 from:

```csharp
	public partial class MiniController : Window
```

to:

```csharp
	public partial class MiniController : Controls.ThemedWindow
```

- [ ] **Step 3: Commit**

```bash
git add OATControl/MiniController.xaml OATControl/MiniController.xaml.cs
git commit -m "refactor: convert MiniController to ThemedWindow (chromeless)"
```

---

### Task 7: Convert DlgCustomActionSetup to ThemedWindow (with title bar)

**Files:**
- Modify: `OATControl/DlgCustomActionSetup.xaml` (lines 1, 11)
- Modify: `OATControl/DlgCustomActionSetup.xaml.cs`

- [ ] **Step 1: Update XAML root element**

In `DlgCustomActionSetup.xaml`, change line 1 from:

```xml
<Window x:Class="OATControl.DlgCustomActionSetup"
		Background="{DynamicResource AppWindowBackgroundBrush}"
		Foreground="{DynamicResource AppForegroundBrush}"
```

to:

```xml
<controls:ThemedWindow x:Class="OATControl.DlgCustomActionSetup"
```

Remove the `Background` and `Foreground` attributes (now set by ThemedWindow).

Change `WindowStyle="ToolWindow"` on line 11 — remove it (ThemedWindow uses `WindowStyle.None`). The line becomes:

```xml
        Title=" Custom Action Definition" MinHeight="250" Width="470" MinWidth="285" Height="200">
```

Change the closing tag from `</Window>` to `</controls:ThemedWindow>`.

- [ ] **Step 2: Update code-behind base class**

In `DlgCustomActionSetup.xaml.cs`, find the class declaration and change from `Window` to `Controls.ThemedWindow`:

```csharp
	public partial class DlgCustomActionSetup : Controls.ThemedWindow, INotifyPropertyChanged
```

- [ ] **Step 3: Commit**

```bash
git add OATControl/DlgCustomActionSetup.xaml OATControl/DlgCustomActionSetup.xaml.cs
git commit -m "refactor: convert DlgCustomActionSetup to ThemedWindow"
```

---

### Task 8: Convert DlgMessageBox to ThemedWindow (close button only)

**Files:**
- Modify: `OATControl/DlgMessageBox.xaml` (lines 1, 11)
- Modify: `OATControl/DlgMessageBox.xaml.cs`

- [ ] **Step 1: Update XAML root element**

In `DlgMessageBox.xaml`, change line 1 from:

```xml
<Window x:Class="OATControl.DlgMessageBox"
	      Background="{DynamicResource AppWindowBackgroundBrush}"
		Foreground="{DynamicResource AppForegroundBrush}"
```

to:

```xml
<controls:ThemedWindow x:Class="OATControl.DlgMessageBox"
```

Remove the `Background` and `Foreground` attributes.

Change line 11 from:

```xml
	      Title="{Binding Title}" WindowStyle="None">
```

to:

```xml
	      Title="{Binding Title}"
	      controls:ThemedWindow.TitleBarButtons="Close">
```

Change the closing tag from `</Window>` to `</controls:ThemedWindow>`.

- [ ] **Step 2: Update code-behind base class**

In `DlgMessageBox.xaml.cs`, find the class declaration and change from `Window` to `Controls.ThemedWindow`:

```csharp
	public partial class DlgMessageBox: Controls.ThemedWindow, INotifyPropertyChanged
```

- [ ] **Step 3: Commit**

```bash
git add OATControl/DlgMessageBox.xaml OATControl/DlgMessageBox.xaml.cs
git commit -m "refactor: convert DlgMessageBox to ThemedWindow (close-only title bar)"
```

---

### Task 9: Build and verify

- [ ] **Step 1: Build the solution**

```bash
cd /mnt/c/Users/Lutz.KRETZSCHMAR/Source/OpenAstroTracker-Desktop
nuget restore OATControl/OATControl.sln
msbuild OATControl/OATControl.sln /p:Configuration=Debug /p:Platform="Any CPU"
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 2: Fix any build errors**

Address any compilation issues from the conversions. Common issues:
- Missing `using OATControl.Controls;` in code-behind files
- XAML namespace resolution issues
- Closing tag mismatches

- [ ] **Step 3: Commit any fixes**

```bash
git add -A
git commit -m "fix: resolve build errors from ThemedWindow title bar migration"
```
