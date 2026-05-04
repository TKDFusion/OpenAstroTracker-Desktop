using System;
using System.Collections.Generic;
using System.Windows;

namespace OATControl.Theming
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private ResourceDictionary _currentThemeDictionary;
        private const string ThemeDictionaryPrefix = "pack://application:,,,/OATControl;component/Resources/Themes/";

        public static ThemeManager Instance => _instance ?? (_instance = new ThemeManager());

        public List<string> AvailableThemes { get; } = new List<string>
        {
            "DarkAstronomy",
            "Daylight"
        };

        public string CurrentTheme { get; private set; }

        private ThemeManager() { }

        public void ApplyTheme(string themeName)
        {
            if (!AvailableThemes.Contains(themeName))
                throw new ArgumentException($"Theme '{themeName}' is not available.");

            var mergedDicts = Application.Current.Resources.MergedDictionaries;

            // Remove previous theme dictionary
            if (_currentThemeDictionary != null)
            {
                mergedDicts.Remove(_currentThemeDictionary);
            }

            // Load new theme dictionary
            var themeDict = new ResourceDictionary
            {
                Source = new Uri($"{ThemeDictionaryPrefix}{themeName}.xaml")
            };

            // Insert at position 0 so Base.xaml styles can reference the theme brushes
            mergedDicts.Insert(0, themeDict);

            _currentThemeDictionary = themeDict;
            CurrentTheme = themeName;
        }
    }
}