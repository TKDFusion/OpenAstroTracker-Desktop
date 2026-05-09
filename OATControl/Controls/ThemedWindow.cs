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
