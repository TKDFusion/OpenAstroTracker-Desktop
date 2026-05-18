using System.Windows;
using System.Windows.Controls;

namespace OATControl.Controls
{
    public class IconButton : Button
    {
        public static readonly DependencyProperty IconDataProperty =
            DependencyProperty.Register("IconData", typeof(string), typeof(IconButton));

        public string IconData
        {
            get => (string)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }
    }
}
