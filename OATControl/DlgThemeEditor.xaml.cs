using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using OATControl.Controls;
using OATControl.Theming;

namespace OATControl
{
    public class ColorEditItem : INotifyPropertyChanged
    {
        private Color _color;
        private string _hexValue;

        public string Key { get; }
        public string DisplayName { get; }
        public string Group { get; }
        public Color DefaultColor { get; }

        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    HexValue = value.ToString();
                    OnPropertyChanged(nameof(Color));
                }
            }
        }

        public string HexValue
        {
            get => _hexValue;
            set
            {
                if (_hexValue != value)
                {
                    _hexValue = value;
                    if (TryParseColor(value, out var parsed))
                    {
                        _color = parsed;
                        OnPropertyChanged(nameof(Color));
                    }
                    OnPropertyChanged(nameof(HexValue));
                }
            }
        }

        private static bool TryParseColor(string text, out Color color)
        {
            try
            {
                color = (Color)ColorConverter.ConvertFromString(text);
                return true;
            }
            catch
            {
                color = Colors.Transparent;
                return false;
            }
        }

        public ColorEditItem(string key, string displayName, string group, Color current, Color defaultColor)
        {
            Key = key;
            DisplayName = displayName;
            Group = group;
            DefaultColor = defaultColor;
            _color = current;
            _hexValue = current.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public partial class DlgThemeEditor : ThemedWindow, INotifyPropertyChanged
    {
        private ObservableCollection<ColorEditItem> _colorItems = new ObservableCollection<ColorEditItem>();
        private string _editingTheme;
        private string _themeAuthor = "";
        private bool _isNewTheme;
        private ResourceDictionary _previewDict;

        public DlgThemeEditor(string themeName = null, bool isNew = false)
        {
            _editingTheme = themeName;
            _isNewTheme = isNew;
            _previewDict = new ResourceDictionary();

            DataContext = this;
            InitializeComponent();
        }

        public ObservableCollection<ColorEditItem> ColorItems => _colorItems;

        public string EditingThemeName
        {
            get => _editingTheme;
            set { _editingTheme = value; OnPropertyChanged(); }
        }

        public string ThemeAuthor
        {
            get => _themeAuthor;
            set { _themeAuthor = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ThemeListBox.ItemsSource = ThemeManager.Instance.AvailableThemes.ToList();
            if (!_isNewTheme && !string.IsNullOrEmpty(_editingTheme))
                ThemeListBox.SelectedItem = _editingTheme;

            LoadThemeColors();
        }

        private void LoadThemeColors()
        {
            _colorItems.Clear();

            var defaults = ThemeColorDefinitions.Colors;
            var themeColors = new Dictionary<string, Color>();

            if (!string.IsNullOrEmpty(_editingTheme) && !_isNewTheme)
            {
                themeColors = ThemeManager.Instance.GetThemeColors(_editingTheme);
                var (name, author) = ThemeManager.Instance.GetThemeMetadata(_editingTheme);
                if (!string.IsNullOrEmpty(name)) EditingThemeName = name;
                ThemeAuthor = author;
            }
            else if (!string.IsNullOrEmpty(_editingTheme) && _isNewTheme)
            {
                themeColors = ThemeManager.Instance.GetThemeColors(
                    ThemeManager.Instance.CurrentTheme);
                EditingThemeName = "New Theme";
            }

            foreach (var def in defaults)
            {
                var current = themeColors.TryGetValue(def.Key, out var c) ? c : def.DefaultColor;
                var item = new ColorEditItem(def.Key, def.DisplayName, def.Group, current, def.DefaultColor);
                item.PropertyChanged += (s, e) => { if (e.PropertyName == "Color") UpdatePreview(); };
                _colorItems.Add(item);
            }

            ColorEditorGrid.ItemsSource = _colorItems;
            ThemeNameLabel.Text = EditingThemeName ?? _editingTheme ?? "";
            UpdatePreview();
        }

        private void ThemeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeListBox.SelectedItem is string themeName)
            {
                _editingTheme = themeName;
                _isNewTheme = false;
                LoadThemeColors();
            }
        }

        private void OnResetColor(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
            {
                var item = _colorItems.FirstOrDefault(c => c.Key == key);
                if (item != null)
                {
                    item.Color = item.DefaultColor;
                    UpdatePreview();
                }
            }
        }

        private void UpdatePreview()
        {
            if (_previewDict != null)
                PreviewPanel.Resources.MergedDictionaries.Remove(_previewDict);

            _previewDict = new ResourceDictionary();
            foreach (var item in _colorItems)
            {
                _previewDict[ThemeColorDefinitions.BrushKeyFromColorKey(item.Key)] = new SolidColorBrush(item.Color);
            }

            PreviewPanel.Resources.MergedDictionaries.Add(_previewDict);
        }

        private void OnNewTheme(object sender, RoutedEventArgs e)
        {
            _editingTheme = ThemeManager.Instance.CurrentTheme;
            _isNewTheme = true;
            LoadThemeColors();
        }

        private void OnImportTheme(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Theme files (*.xaml)|*.xaml|All files (*.*)|*.*",
                Title = "Import Theme"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    ThemeManager.Instance.ImportTheme(dlg.FileName);
                    var name = Path.GetFileNameWithoutExtension(dlg.FileName);
                    ThemeListBox.ItemsSource = ThemeManager.Instance.AvailableThemes.ToList();
                    _editingTheme = name;
                    _isNewTheme = false;
                    ThemeListBox.SelectedItem = name;
                    LoadThemeColors();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to import theme: {ex.Message}", "Import Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_editingTheme) || !ThemeManager.Instance.IsUserTheme(_editingTheme))
            {
                OnSaveAs(sender, e);
                return;
            }
            SaveCurrentTheme(_editingTheme);
        }

        private void OnSaveAs(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Width = 320,
                Height = 180,
                Title = "Save Theme As",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stack = new StackPanel { Margin = new Thickness(12) };
            stack.Children.Add(new TextBlock { Text = "Theme Name:", Margin = new Thickness(0, 0, 0, 4) });
            var nameBox = new TextBox { Text = EditingThemeName ?? "MyTheme", Margin = new Thickness(0, 0, 0, 8) };
            stack.Children.Add(nameBox);
            stack.Children.Add(new TextBlock { Text = "Author:", Margin = new Thickness(0, 0, 0, 4) });
            var authorBox = new TextBox { Text = ThemeAuthor, Margin = new Thickness(0, 0, 0, 8) };
            stack.Children.Add(authorBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var saveBtn = new Button { Content = "Save", Width = 60, Margin = new Thickness(0, 0, 8, 0) };
            var cancelBtn = new Button { Content = "Cancel", Width = 60 };
            btnPanel.Children.Add(saveBtn);
            btnPanel.Children.Add(cancelBtn);
            stack.Children.Add(btnPanel);

            dialog.Content = stack;

            cancelBtn.Click += (s, args) => dialog.DialogResult = false;
            saveBtn.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    System.Windows.MessageBox.Show("Please enter a theme name.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                dialog.DialogResult = true;
            };

            if (dialog.ShowDialog() == true)
            {
                EditingThemeName = nameBox.Text.Trim();
                ThemeAuthor = authorBox.Text.Trim();

                var fileName = new string(EditingThemeName.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
                SaveCurrentTheme(fileName);
            }
        }

        private void WriteThemeFile(string path, string themeName, string author, Dictionary<string, Color> colors)
        {
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
        }

        private void SaveCurrentTheme(string fileSafeName)
        {
            try
            {
                var colors = _colorItems.ToDictionary(i => i.Key, i => i.Color);
                Directory.CreateDirectory(ThemeManager.UserThemesFolder);
                var path = Path.Combine(ThemeManager.UserThemesFolder, $"{fileSafeName}.xaml");
                WriteThemeFile(path, EditingThemeName ?? fileSafeName, ThemeAuthor, colors);

                if (!ThemeManager.Instance.IsUserTheme(fileSafeName))
                    ThemeManager.Instance.ReloadUserThemes();
                ThemeManager.Instance.ApplyTheme(fileSafeName);

                _editingTheme = fileSafeName;
                _isNewTheme = false;
                ThemeListBox.ItemsSource = ThemeManager.Instance.AvailableThemes.ToList();
                ThemeListBox.SelectedItem = fileSafeName;

                System.Windows.MessageBox.Show($"Theme '{EditingThemeName}' saved.", "Theme Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save theme: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnExport(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Theme files (*.xaml)|*.xaml",
                Title = "Export Theme",
                FileName = (_editingTheme ?? "MyTheme") + ".xaml"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var colors = _colorItems.ToDictionary(i => i.Key, i => i.Color);
                    WriteThemeFile(dlg.FileName, EditingThemeName ?? _editingTheme ?? "MyTheme", ThemeAuthor, colors);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to export theme: {ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
