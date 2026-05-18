using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OATControl.Controls
{
    public partial class ToggleSwitch : UserControl
    {
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            "IsChecked", typeof(bool), typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

        public ToggleSwitch()
        {
            InitializeComponent();
            UpdateVisual();
        }

        [Category("Common")]
        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            IsChecked = !IsChecked;
            base.OnMouseLeftButtonUp(e);
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toggle = (ToggleSwitch)d;
            toggle.UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (Thumb == null || SwitchTrack == null) return;

            if (IsChecked)
            {
                Thumb.HorizontalAlignment = HorizontalAlignment.Right;
                Thumb.Margin = new Thickness(0, 0, 2, 0);
                SwitchTrack.SetResourceReference(BackgroundProperty, "AppToggleOnBrush");
                if (Checkmark != null)
                {
                    var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                    Checkmark.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            }
            else
            {
                Thumb.HorizontalAlignment = HorizontalAlignment.Left;
                Thumb.Margin = new Thickness(2, 0, 0, 0);
                SwitchTrack.SetResourceReference(BackgroundProperty, "AppToggleOffBrush");
                if (Checkmark != null)
                {
                    var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
                    Checkmark.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            }
        }
    }
}
