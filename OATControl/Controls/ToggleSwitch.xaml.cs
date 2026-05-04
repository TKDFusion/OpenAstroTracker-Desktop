using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            UpdateVisual();
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
