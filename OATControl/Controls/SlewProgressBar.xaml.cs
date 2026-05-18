using System.Windows;
using System.Windows.Controls;

namespace OATControl.Controls
{
    public partial class SlewProgressBar : UserControl
    {
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(SlewProgressBar), new PropertyMetadata(0.0));

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(SlewProgressBar), new PropertyMetadata(false, OnActiveStateChanged));

        public static readonly DependencyProperty BarThicknessProperty =
            DependencyProperty.Register("BarThickness", typeof(double), typeof(SlewProgressBar), new PropertyMetadata(6.0, OnActiveStateChanged));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public double BarThickness
        {
            get => (double)GetValue(BarThicknessProperty);
            set => SetValue(BarThicknessProperty, value);
        }

        public SlewProgressBar()
        {
            InitializeComponent();
        }

        private static void OnActiveStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bar = (SlewProgressBar)d;
            bar.UpdateBorderThickness();
        }

        private void UpdateBorderThickness()
        {
            var thickness = IsActive ? BarThickness : 0;
            ProgressBar.BorderThickness = new Thickness(0, thickness, 0, 0);
        }
    }
}
