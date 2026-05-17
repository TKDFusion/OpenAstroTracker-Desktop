using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace OATControl.Controls
{
    public partial class LabeledToggleSwitch : UserControl
    {
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            "IsChecked", typeof(bool), typeof(LabeledToggleSwitch),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

        public static readonly DependencyProperty OnLabelProperty = DependencyProperty.Register(
            "OnLabel", typeof(string), typeof(LabeledToggleSwitch),
            new PropertyMetadata("ON", OnLabelChanged));

        public static readonly DependencyProperty OffLabelProperty = DependencyProperty.Register(
            "OffLabel", typeof(string), typeof(LabeledToggleSwitch),
            new PropertyMetadata("OFF", OnLabelChanged));

        public LabeledToggleSwitch()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdateVisual();
        }

        [Category("Common")]
        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        [Category("Appearance")]
        public string OnLabel
        {
            get => (string)GetValue(OnLabelProperty);
            set => SetValue(OnLabelProperty, value);
        }

        [Category("Appearance")]
        public string OffLabel
        {
            get => (string)GetValue(OffLabelProperty);
            set => SetValue(OffLabelProperty, value);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            IsChecked = !IsChecked;
            base.OnMouseLeftButtonUp(e);
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LabeledToggleSwitch)d).UpdateVisual();
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LabeledToggleSwitch)d).UpdateTrackWidth();
        }

        private void UpdateTrackWidth()
        {
            if (SwitchTrack == null || OnLabelElement == null || OffLabelElement == null) return;

            OnLabelElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            OffLabelElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            double labelSpace = Math.Max(OnLabelElement.DesiredSize.Width, OffLabelElement.DesiredSize.Width) + 12;
            double minWidth = labelSpace + 16 + 4; // label + thumb + padding
            SwitchTrack.MinWidth = Math.Max(44, minWidth);
        }

        private void UpdateVisual()
        {
            if (Thumb == null || SwitchTrack == null) return;

            UpdateTrackWidth();

            if (IsChecked)
            {
                Thumb.HorizontalAlignment = HorizontalAlignment.Right;
                Thumb.Margin = new Thickness(0, 0, 2, 0);
                SwitchTrack.SetResourceReference(BackgroundProperty, "AppToggleOnBrush");

                if (OnLabelElement != null)
                {
                    var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                    OnLabelElement.BeginAnimation(UIElement.OpacityProperty, anim);
                }
                if (OffLabelElement != null)
                {
                    var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
                    OffLabelElement.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            }
            else
            {
                Thumb.HorizontalAlignment = HorizontalAlignment.Left;
                Thumb.Margin = new Thickness(2, 0, 0, 0);
                SwitchTrack.SetResourceReference(BackgroundProperty, "AppToggleOffBrush");

                if (OnLabelElement != null)
                {
                    var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
                    OnLabelElement.BeginAnimation(UIElement.OpacityProperty, anim);
                }
                if (OffLabelElement != null)
                {
                    var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                    OffLabelElement.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            }
        }
    }
}
