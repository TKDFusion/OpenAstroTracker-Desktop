using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

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

        public static readonly string UserThemesFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenAstroTracker", "Themes");

        private static readonly List<string> BuiltinThemes = new List<string>
        {
            "Red Astronomy",
            "Daylight"
        };

        private readonly List<string> _userThemes = new List<string>();

        public static ThemeManager Instance => _instance ?? (_instance = new ThemeManager());

        public List<string> AvailableThemes { get; } = new List<string>
        {
            "Red Astronomy",
            "Daylight"
        };

        public string CurrentTheme { get; private set; }
        public bool HotReloadEnabled { get; set; }

        public bool IsUserTheme(string themeName) => _userThemes.Contains(themeName);

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

            ResourceDictionary themeDict;

            if (_userThemes.Contains(themeName))
            {
                var path = Path.Combine(UserThemesFolder, $"{themeName}.xaml");
                themeDict = new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) };
            }
            else
            {
                if (themeName=="Red Astronomy")
                {
                    themeName = "DarkAstronomy";
                }
                themeDict = new ResourceDictionary
                {
                    Source = new Uri($"{ThemeDictionaryPrefix}{themeName}.xaml")
                };
            }

            GenerateBrushes(themeDict);

            mergedDicts.Insert(0, themeDict);

            _currentThemeDictionary = themeDict;
            CurrentTheme = themeName;

            SetupWatcher(themeName);
        }

        public void ScanUserThemes()
        {
            try
            {
                Directory.CreateDirectory(UserThemesFolder);
            }
            catch { return; }

            foreach (var file in Directory.GetFiles(UserThemesFolder, "*.xaml"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(name) || AvailableThemes.Contains(name))
                    continue;

                if (!IsValidThemeFile(file))
                    continue;

                _userThemes.Add(name);
                AvailableThemes.Add(name);
            }
        }

        private bool IsValidThemeFile(string path)
        {
            try
            {
                var dict = new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) };
                return ThemeColorDefinitions.Colors.Any(c => dict.Contains(c.Key));
            }
            catch
            {
                return false;
            }
        }

        public void ReloadUserThemes()
        {
            _userThemes.Clear();
            foreach (var name in AvailableThemes.ToList())
            {
                if (!BuiltinThemes.Contains(name))
                    AvailableThemes.Remove(name);
            }
            ScanUserThemes();
        }

        private void GenerateBrushes(ResourceDictionary dict)
        {
            foreach (var def in ThemeColorDefinitions.Colors)
            {
                if (dict[def.Key] is Color color)
                {
                    var brushKey = ThemeColorDefinitions.BrushKeyFromColorKey(def.Key);
                    dict[brushKey] = new SolidColorBrush(color);
                }
            }

            // SystemColors overrides for ListView selection/hover
            if (dict["AppForegroundColor"] is Color fgColor)
            {
                dict[SystemColors.HighlightTextBrushKey] = new SolidColorBrush(fgColor);
                dict[SystemColors.ControlTextBrushKey] = new SolidColorBrush(fgColor);
            }
            if (dict["AppButtonHoverColor"] is Color hoverColor)
            {
                dict[SystemColors.InactiveSelectionHighlightBrushKey] = new SolidColorBrush(hoverColor);
            }
        }

        public (string Name, string Author) GetThemeMetadata(string themeName)
        {
            string name = themeName;
            string author = "";

            try
            {
                ResourceDictionary dict;
                if (_userThemes.Contains(themeName))
                {
                    var path = Path.Combine(UserThemesFolder, $"{themeName}.xaml");
                    dict = new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) };
                }
                else
                {
                    if (themeName == "Red Astronomy")
                    {
                        themeName = "DarkAstronomy";
                    }
                    dict = new ResourceDictionary
                    {
                        Source = new Uri($"{ThemeDictionaryPrefix}{themeName}.xaml")
                    };
                }
                if (dict["ThemeName"] is string themeNameValue)
                    name = themeNameValue;
                if (dict["ThemeAuthor"] is string themeAuthorValue)
                    author = themeAuthorValue;
            }
            catch { }

            return (name, author);
        }

        public Dictionary<string, Color> GetThemeColors(string themeName)
        {
            var result = new Dictionary<string, Color>();

            try
            {
                ResourceDictionary dict;
                if (_userThemes.Contains(themeName))
                {
                    var path = Path.Combine(UserThemesFolder, $"{themeName}.xaml");
                    dict = new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) };
                }
                else
                {
                    dict = new ResourceDictionary
                    {
                        Source = new Uri($"{ThemeDictionaryPrefix}{themeName}.xaml")
                    };
                }

                foreach (var def in ThemeColorDefinitions.Colors)
                {
                    if (dict[def.Key] is Color color)
                        result[def.Key] = color;
                }
            }
            catch { }

            return result;
        }

        public void ImportTheme(string sourceFilePath)
        {
            if (!IsValidThemeFile(sourceFilePath))
                throw new InvalidOperationException("Not a valid theme file.");

            var name = Path.GetFileNameWithoutExtension(sourceFilePath);
            var destPath = Path.Combine(UserThemesFolder, Path.GetFileName(sourceFilePath));

            Directory.CreateDirectory(UserThemesFolder);
            File.Copy(sourceFilePath, destPath, overwrite: true);

            if (!_userThemes.Contains(name))
            {
                _userThemes.Add(name);
                AvailableThemes.Add(name);
            }
        }

        public void ExportTheme(string themeName, string author, string destinationPath)
        {
            var colors = GetThemeColors(themeName);
            var (existingName, _) = GetThemeMetadata(themeName);

            using (var writer = System.Xml.XmlWriter.Create(destinationPath, new System.Xml.XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            }))
            {
                writer.WriteStartElement("ResourceDictionary");
                writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                writer.WriteAttributeString("xmlns:x", "http://schemas.microsoft.com/winfx/2006/xaml");
                writer.WriteAttributeString("xmlns:sys", "clr-namespace:System;assembly=mscorlib");

                writer.WriteStartElement("sys", "String", "clr-namespace:System;assembly=mscorlib");
                writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", "ThemeName");
                writer.WriteString(existingName ?? themeName);
                writer.WriteEndElement();

                writer.WriteStartElement("sys", "String", "clr-namespace:System;assembly=mscorlib");
                writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", "ThemeAuthor");
                writer.WriteString(author ?? "");
                writer.WriteEndElement();

                foreach (var def in ThemeColorDefinitions.Colors)
                {
                    if (colors.TryGetValue(def.Key, out var color))
                    {
                        writer.WriteStartElement("Color");
                        writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", def.Key);
                        writer.WriteString(color.ToString());
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }
        }

        public void DeleteTheme(string themeName)
        {
            if (!_userThemes.Contains(themeName))
                throw new InvalidOperationException("Cannot delete built-in themes.");

            var path = Path.Combine(UserThemesFolder, $"{themeName}.xaml");
            if (File.Exists(path))
                File.Delete(path);

            _userThemes.Remove(themeName);
            AvailableThemes.Remove(themeName);

            if (CurrentTheme == themeName)
                ApplyTheme("Red Astronomy");
        }

        public void SaveUserTheme(string themeName, string author, Dictionary<string, Color> colors)
        {
            Directory.CreateDirectory(UserThemesFolder);
            var path = Path.Combine(UserThemesFolder, $"{themeName}.xaml");

            using (var writer = System.Xml.XmlWriter.Create(path, new System.Xml.XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            }))
            {
                writer.WriteStartElement("ResourceDictionary");
                writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                writer.WriteAttributeString("xmlns:x", "http://schemas.microsoft.com/winfx/2006/xaml");
                writer.WriteAttributeString("xmlns:sys", "clr-namespace:System;assembly=mscorlib");

                writer.WriteStartElement("sys:String");
                writer.WriteAttributeString("x:Key", "ThemeName");
                writer.WriteString(themeName);
                writer.WriteEndElement();

                if (!string.IsNullOrEmpty(author))
                {
                    writer.WriteStartElement("sys:String");
                    writer.WriteAttributeString("x:Key", "ThemeAuthor");
                    writer.WriteString(author);
                    writer.WriteEndElement();
                }

                foreach (var def in ThemeColorDefinitions.Colors)
                {
                    if (colors.TryGetValue(def.Key, out var color))
                    {
                        writer.WriteStartElement("Color");
                        writer.WriteAttributeString("x:Key", def.Key);
                        writer.WriteString(color.ToString());
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }

            if (!_userThemes.Contains(themeName))
            {
                _userThemes.Add(themeName);
                AvailableThemes.Add(themeName);
            }
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

            GenerateBrushes(newDict);

            if (_currentThemeDictionary != null)
            {
                mergedDicts.Remove(_currentThemeDictionary);
            }

            mergedDicts.Insert(0, newDict);
            _currentThemeDictionary = newDict;
        }
    }
}
