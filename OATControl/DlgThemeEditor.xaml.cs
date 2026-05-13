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
                    HexValue = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
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
        private Dictionary<string, Color> _originalColors = new Dictionary<string, Color>();
        private double _averageHue;
        private double _averageSaturation;
        private double _averageLightness;
        private bool _updatingFromSlider;

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

            // Snapshot original colors for HSL adjustment sliders
            _originalColors = _colorItems.ToDictionary(i => i.Key, i => i.Color);
            var hslValues = _colorItems
                .Select(i => i.Color)
                .Where(c => !IsBlackOrWhite(c))
                .Select(c => ColorToHsl(c))
                .ToList();
            _averageHue = hslValues.Count > 0 ? hslValues.Average(v => v.h) : 0;
            _averageSaturation = hslValues.Count > 0 ? hslValues.Average(v => v.s) : 0;
            _averageLightness = hslValues.Count > 0 ? hslValues.Average(v => v.l) : 0.5;

            if (HueSlider != null)
            {
                _updatingFromSlider = true;
                HueSlider.Value = _averageHue;
                SaturationSlider.Value = _averageSaturation;
                LightnessSlider.Value = _averageLightness;
                _updatingFromSlider = false;
            }

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

        private void OnHueSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingFromSlider || _originalColors.Count == 0)
                return;

            var delta = e.NewValue - _averageHue;
            if (delta > 180) delta -= 360;
            if (delta < -180) delta += 360;

            foreach (var item in _colorItems)
            {
                if (!_originalColors.TryGetValue(item.Key, out var original) || IsBlackOrWhite(original))
                    continue;
                var (h, s, l) = ColorToHsl(original);
                item.Color = HslToColor((h + delta + 360) % 360, s, l);
            }
            UpdatePreview();
            ResnapshotColors();
        }

        private void OnSaturationSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingFromSlider || _originalColors.Count == 0)
                return;

            var delta = e.NewValue - _averageSaturation;

            foreach (var item in _colorItems)
            {
                if (!_originalColors.TryGetValue(item.Key, out var original) || IsBlackOrWhite(original))
                    continue;
                var (h, s, l) = ColorToHsl(original);
                item.Color = HslToColor(h, Math.Max(0, Math.Min(1, s + delta)), l);
            }
            UpdatePreview();
            ResnapshotColors();
        }

        private void OnLightnessSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingFromSlider || _originalColors.Count == 0)
                return;

            var delta = e.NewValue - _averageLightness;

            foreach (var item in _colorItems)
            {
                if (!_originalColors.TryGetValue(item.Key, out var original) || IsBlackOrWhite(original))
                    continue;
                var (h, s, l) = ColorToHsl(original);
                item.Color = HslToColor(h, s, Math.Max(0, Math.Min(1, l + delta)));
            }
            UpdatePreview();
            ResnapshotColors();
        }

        private void ResnapshotColors()
        {
            _originalColors = _colorItems.ToDictionary(i => i.Key, i => i.Color);
            var hslValues = _colorItems
                .Select(i => i.Color)
                .Where(c => !IsBlackOrWhite(c))
                .Select(c => ColorToHsl(c))
                .ToList();
            _averageHue = hslValues.Count > 0 ? hslValues.Average(v => v.h) : 0;
            _averageSaturation = hslValues.Count > 0 ? hslValues.Average(v => v.s) : 0;
            _averageLightness = hslValues.Count > 0 ? hslValues.Average(v => v.l) : 0.5;

            _updatingFromSlider = true;
            HueSlider.Value = _averageHue;
            SaturationSlider.Value = _averageSaturation;
            LightnessSlider.Value = _averageLightness;
            _updatingFromSlider = false;
        }

        private static (double h, double s, double l) ColorToHsl(Color c)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double l = (max + min) / 2.0;
            double h = 0, s = 0;

            if (max != min)
            {
                double d = max - min;
                s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
                if (max == r) h = ((g - b) / d + (g < b ? 6 : 0)) * 60;
                else if (max == g) h = ((b - r) / d + 2) * 60;
                else h = ((r - g) / d + 4) * 60;
            }

            return (h, s, l);
        }

        private static Color HslToColor(double h, double s, double l)
        {
            if (s == 0)
                return Color.FromRgb((byte)(l * 255), (byte)(l * 255), (byte)(l * 255));

            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            double hk = h / 360.0;

            byte r = (byte)Math.Round(HueToRgb(p, q, hk + 1.0 / 3.0) * 255);
            byte g = (byte)Math.Round(HueToRgb(p, q, hk) * 255);
            byte b = (byte)Math.Round(HueToRgb(p, q, hk - 1.0 / 3.0) * 255);
            return Color.FromRgb(r, g, b);
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
            return p;
        }

        private static bool IsBlackOrWhite(Color c)
        {
            var (_, _, l) = ColorToHsl(c);
            return l < 0.05 || l > 0.95;
        }
    }
}
