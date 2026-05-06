using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace OATControl.Theming
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private ResourceDictionary _currentThemeDictionary;
        private FileSystemWatcher _watcher;
        private DateTime _lastReload = DateTime.MinValue;
        private string _watchedThemePath;
        private const string ThemeDictionaryPrefix = "pack://application:,,,/OATControl;component/Resources/Themes/";

        public static ThemeManager Instance => _instance ?? (_instance = new ThemeManager());

        public List<string> AvailableThemes { get; } = new List<string>
        {
            "DarkAstronomy",
            "Daylight"
        };

        public string CurrentTheme { get; private set; }
        public bool HotReloadEnabled { get; set; }

        private ThemeManager() { }

        public void ApplyTheme(string themeName)
        {
            if (!AvailableThemes.Contains(themeName))
                throw new ArgumentException($"Theme '{themeName}' is not available.");

            var mergedDicts = Application.Current.Resources.MergedDictionaries;

            if (_currentThemeDictionary != null)
            {
                mergedDicts.Remove(_currentThemeDictionary);
            }

            var themeDict = new ResourceDictionary
            {
                Source = new Uri($"{ThemeDictionaryPrefix}{themeName}.xaml")
            };

            mergedDicts.Insert(0, themeDict);

            _currentThemeDictionary = themeDict;
            CurrentTheme = themeName;

            SetupWatcher(themeName);
        }

        private void SetupWatcher(string themeName)
        {
            _watcher?.Dispose();
            _watcher = null;

            if (!HotReloadEnabled)
                return;

            // Walk up from exe directory to find the source theme file
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            string themePath = null;
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "OATControl", "Resources", "Themes", $"{themeName}.xaml");
                if (File.Exists(candidate))
                {
                    themePath = candidate;
                    break;
                }
                dir = Directory.GetParent(dir)?.FullName;
            }

            if (themePath == null)
                return;

            _watchedThemePath = themePath;
            var themeDir = Path.GetDirectoryName(themePath);
            _watcher = new FileSystemWatcher(themeDir)
            {
                Filter = $"{themeName}.xaml",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnThemeFileChanged;
        }

        private void OnThemeFileChanged(object sender, FileSystemEventArgs e)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastReload).TotalMilliseconds < 500)
                return;

            _lastReload = now;

            try
            {
                System.Threading.Thread.Sleep(150);
                Application.Current.Dispatcher.Invoke(() => ReloadTheme());
            }
            catch { }
        }

        private void ReloadTheme()
        {
            var mergedDicts = Application.Current.Resources.MergedDictionaries;

            var newDict = new ResourceDictionary
            {
                Source = new Uri(_watchedThemePath, UriKind.Absolute)
            };

            if (_currentThemeDictionary != null)
            {
                mergedDicts.Remove(_currentThemeDictionary);
            }

            mergedDicts.Insert(0, newDict);
            _currentThemeDictionary = newDict;
        }
    }
}
