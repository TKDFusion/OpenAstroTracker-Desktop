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
