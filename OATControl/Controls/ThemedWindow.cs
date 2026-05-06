using System.Windows;
using System.Windows.Shell;

namespace OATControl.Controls
{
    public class ThemedWindow : Window
    {
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
                UseAeroCaptionButtons = true
            };
            WindowChrome.SetWindowChrome(this, chrome);
        }
    }
}
