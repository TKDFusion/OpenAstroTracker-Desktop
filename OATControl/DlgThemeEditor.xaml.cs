using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    public class ThemeListEntry
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
    }

    public partial class DlgThemeEditor : ThemedWindow, INotifyPropertyChanged
    {
        private ObservableCollection<ColorEditItem> _colorItems = new ObservableCollection<ColorEditItem>();
        private string _editingTheme;
        private string _editingThemeDisplayName;
        private string _themeAuthor = "";
        private bool _isNewTheme;
        private ResourceDictionary _previewDict;
        private Dictionary<string, Color> _originalColors = new Dictionary<string, Color>();
        private double _averageHue;
        private double _averageSaturation;
        private double _averageLightness;
        private bool _suppressSelectionChanged;
        private bool _updatingFromSlider;
        private bool _updatingGlobalSlider;
        private bool _globalSliderDragging;

        private ColorEditItem _selectedColorItem;
        private bool _updatingPicker;
        private bool _isDraggingSquare;
        private bool _isDraggingBar;
        private WriteableBitmap _hueSatBitmap;
        private WriteableBitmap _hueBarBitmap;
        private const int PickerSize = 256;
        private const int BarWidth = 24;

        private bool _isReadOnly;
        private bool _hasChanges;
        private string _cloneSourceTheme;

        public DlgThemeEditor(string themeName = null, bool isNew = false, bool readOnly = false, bool cloneAsNew = false)
        {
            _editingTheme = themeName;
            _isNewTheme = isNew || cloneAsNew;
            _isReadOnly = readOnly;
            _previewDict = new ResourceDictionary();

            DataContext = this;
            InitializeComponent();

            if (_isReadOnly)
            {
                PickerPanel.IsEnabled = false;
                HueSlider.IsEnabled = false;
                SaturationSlider.IsEnabled = false;
                LightnessSlider.IsEnabled = false;
                SaveButton.IsEnabled = false;
                SaveAsButton.IsEnabled = false;
                ExportButton.IsEnabled = false;
            }

            if (cloneAsNew && !string.IsNullOrEmpty(themeName))
            {
                EditingThemeName = themeName + " Copy";
            }
        }

        public ObservableCollection<ColorEditItem> ColorItems => _colorItems;

        public string EditingThemeName
        {
            get => _editingThemeDisplayName;
            set { _editingThemeDisplayName = value; OnPropertyChanged(); }
        }

        public string ThemeAuthor
        {
            get => _themeAuthor;
            set { _themeAuthor = value; OnPropertyChanged(); }
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set { _isReadOnly = value; OnPropertyChanged(); }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set { _hasChanges = value; OnPropertyChanged(); }
        }

        public ColorEditItem SelectedColorItem
        {
            get => _selectedColorItem;
            set
            {
                if (_selectedColorItem != value)
                {
                    if (_selectedColorItem != null)
                        _selectedColorItem.PropertyChanged -= OnSelectedColorChanged;
                    _selectedColorItem = value;
                    if (_selectedColorItem != null)
                        _selectedColorItem.PropertyChanged += OnSelectedColorChanged;
                    OnPropertyChanged();
                    UpdatePickerDisplay();
                }
            }
        }

        private void OnSelectedColorChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Color" && !_updatingPicker)
                UpdatePickerDisplay();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Pin the editor chrome to Daylight by generating brushes from the
            // locally-merged Daylight colors into this window's Resources.
            // This prevents DynamicResource lookups from falling through to
            // Application.Resources when themes change during editing.
            foreach (var def in ThemeColorDefinitions.Colors)
            {
                if (Resources[def.Key] is Color color)
                {
                    var brushKey = ThemeColorDefinitions.BrushKeyFromColorKey(def.Key);
                    Resources[brushKey] = new SolidColorBrush(color);
                }
            }

            SetThemeList(!_isNewTheme && !string.IsNullOrEmpty(_editingTheme) ? _editingTheme : null);

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
                    _cloneSourceTheme ?? ThemeManager.Instance.CurrentTheme);
                if (string.IsNullOrEmpty(EditingThemeName))
                    EditingThemeName = "New Theme";
            }

            foreach (var def in defaults)
            {
                var current = themeColors.TryGetValue(def.Key, out var c) ? c : def.DefaultColor;
                var item = new ColorEditItem(def.Key, def.DisplayName, def.Group, current, def.DefaultColor);
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "Color")
                    {
                        if (_updatingPicker)
                            _originalColors[item.Key] = item.Color;
                        if (!_updatingGlobalSlider)
                            UpdatePreview();
                    }
                };
                _colorItems.Add(item);
            }

            if (ColorSwatchList != null)
            {
                ColorSwatchList.ItemsSource = _colorItems;
                ColorSwatchList.SelectedIndex = _colorItems.Count > 0 ? 0 : -1;
            }
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
            HasChanges = false;
        }

        private void ThemeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionChanged) return;

            if (ThemeListBox.SelectedValue is string themeName)
            {
                _editingTheme = themeName;
                _isNewTheme = false;
                UpdateReadOnlyState(themeName);
                LoadThemeColors();
            }
        }

        private void UpdateReadOnlyState(string themeName)
        {
            _isReadOnly = !string.IsNullOrEmpty(themeName) && !ThemeManager.Instance.IsUserTheme(themeName);
            bool canEdit = !_isReadOnly;
            PickerPanel.IsEnabled = canEdit;
            HueSlider.IsEnabled = canEdit;
            SaturationSlider.IsEnabled = canEdit;
            LightnessSlider.IsEnabled = canEdit;
            SaveButton.IsEnabled = canEdit;
            SaveAsButton.IsEnabled = canEdit;
            ExportButton.IsEnabled = canEdit;
        }

        private List<ThemeListEntry> BuildThemeList(params (string id, string displayName)[] extras)
        {
            var list = ThemeManager.Instance.AvailableThemes.Select(id =>
            {
                var (name, _) = ThemeManager.Instance.GetThemeMetadata(id);
                return new ThemeListEntry { Id = id, DisplayName = string.IsNullOrEmpty(name) ? id : name };
            }).ToList();
            foreach (var (id, displayName) in extras)
                list.Add(new ThemeListEntry { Id = id, DisplayName = displayName });
            return list;
        }

        private void SetThemeList(string selectId, bool suppressSelection = false)
        {
            var list = BuildThemeList();
            if (suppressSelection) _suppressSelectionChanged = true;
            ThemeListBox.ItemsSource = list;
            ThemeListBox.SelectedValue = selectId;
            if (suppressSelection) _suppressSelectionChanged = false;
        }

        private bool _updatingPreview;

        private void UpdatePreview()
        {
            if (_updatingPreview) return;
            _updatingPreview = true;
            try
            {
                if (!_isReadOnly) HasChanges = true;

                if (_previewDict != null)
                    PreviewPanel.Resources.MergedDictionaries.Remove(_previewDict);

                _previewDict = new ResourceDictionary();
                foreach (var item in _colorItems)
                {
                    _previewDict[ThemeColorDefinitions.BrushKeyFromColorKey(item.Key)] = new SolidColorBrush(item.Color);
                }

                PreviewPanel.Resources.MergedDictionaries.Add(_previewDict);

                if (_selectedColorItem != null && !_updatingPicker)
                    UpdatePickerDisplay();
            }
            finally
            {
                _updatingPreview = false;
            }
        }

        private void OnNewTheme(object sender, RoutedEventArgs e)
        {
            var cloneSource = ThemeListBox.SelectedValue as string
                ?? ThemeManager.Instance.CurrentTheme;

            var themes = ThemeManager.Instance.AvailableThemes.ToList();
            var name = "Unnamed";
            int counter = 1;
            while (themes.Contains(name))
                name = $"Unnamed {++counter}";

            var list = BuildThemeList((name, name));
            _suppressSelectionChanged = true;
            ThemeListBox.ItemsSource = list;
            ThemeListBox.SelectedValue = name;
            _suppressSelectionChanged = false;

            // SelectionChanged fires and resets _isNewTheme; override it
            _isNewTheme = true;
            _editingTheme = name;
            _cloneSourceTheme = cloneSource;
            LoadThemeColors();
            EditingThemeName = name;
            ThemeNameLabel.Text = name;

            // New themes are always editable
            _isReadOnly = false;
            PickerPanel.IsEnabled = true;
            HueSlider.IsEnabled = true;
            SaturationSlider.IsEnabled = true;
            LightnessSlider.IsEnabled = true;
            SaveButton.IsEnabled = true;
            SaveAsButton.IsEnabled = true;
            ExportButton.IsEnabled = true;
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
                    _editingTheme = name;
                    _isNewTheme = false;
                    SetThemeList(name);
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
                Height = 200,
                Title = "Save Theme As",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0))
            };

            var labelBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
            var inputBg = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
            var inputBorder = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));

            var stack = new StackPanel { Margin = new Thickness(12) };
            stack.Children.Add(new TextBlock { Text = "Theme Name:", Foreground = labelBrush, Margin = new Thickness(0, 0, 0, 4) });
            var nameBox = new TextBox
            {
                Text = _isNewTheme ? "" : (EditingThemeName ?? "MyTheme"),
                Margin = new Thickness(0, 0, 0, 8),
                Background = inputBg,
                Foreground = labelBrush,
                BorderBrush = inputBorder
            };
            stack.Children.Add(nameBox);
            stack.Children.Add(new TextBlock { Text = "Author:", Foreground = labelBrush, Margin = new Thickness(0, 0, 0, 4) });
            var authorBox = new TextBox
            {
                Text = ThemeAuthor,
                Margin = new Thickness(0, 0, 0, 8),
                Background = inputBg,
                Foreground = labelBrush,
                BorderBrush = inputBorder
            };
            stack.Children.Add(authorBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var saveBtn = new Button
            {
                Content = "Save", Width = 60, Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)),
                Foreground = labelBrush,
                BorderBrush = inputBorder
            };
            var cancelBtn = new Button
            {
                Content = "Cancel", Width = 60,
                Background = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)),
                Foreground = labelBrush,
                BorderBrush = inputBorder
            };
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
                writer.WriteStartElement("ResourceDictionary", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                writer.WriteAttributeString("xmlns", "x", null, "http://schemas.microsoft.com/winfx/2006/xaml");
                writer.WriteAttributeString("xmlns", "sys", null, "clr-namespace:System;assembly=mscorlib");

                writer.WriteStartElement("String", "clr-namespace:System;assembly=mscorlib");
                writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", "ThemeName");
                writer.WriteString(themeName);
                writer.WriteEndElement();

                if (!string.IsNullOrEmpty(author))
                {
                    writer.WriteStartElement("String", "clr-namespace:System;assembly=mscorlib");
                    writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", "ThemeAuthor");
                    writer.WriteString(author);
                    writer.WriteEndElement();
                }

                foreach (var def in ThemeColorDefinitions.Colors)
                {
                    if (colors.TryGetValue(def.Key, out var color))
                    {
                        writer.WriteStartElement("Color", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                        writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", def.Key);
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
                _isReadOnly = false;
                HasChanges = false;
                UpdateReadOnlyState(fileSafeName);
                SetThemeList(fileSafeName);

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

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_hasChanges)
            {
                var result = System.Windows.MessageBox.Show(
                    "You have unsaved changes. Save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    OnSave(this, null);
                    if (_hasChanges) e.Cancel = true;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
            base.OnClosing(e);
        }

        private void OnHueSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingFromSlider || _originalColors.Count == 0)
                return;

            var delta = e.NewValue - _averageHue;
            if (delta > 180) delta -= 360;
            if (delta < -180) delta += 360;

            _updatingGlobalSlider = true;
            foreach (var item in _colorItems)
            {
                if (!_originalColors.TryGetValue(item.Key, out var original) || IsBlackOrWhite(original))
                    continue;
                var (h, s, l) = ColorToHsl(original);
                item.Color = HslToColor((h + delta + 360) % 360, s, l);
            }
            _updatingGlobalSlider = false;
            UpdatePreview();

            if (!_globalSliderDragging)
                ResnapshotColors();
        }

        private void OnSaturationSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingFromSlider || _originalColors.Count == 0)
                return;

            var delta = e.NewValue - _averageSaturation;

            _updatingGlobalSlider = true;
            foreach (var item in _colorItems)
            {
                if (!_originalColors.TryGetValue(item.Key, out var original) || IsBlackOrWhite(original))
                    continue;
                var (h, s, l) = ColorToHsl(original);
                item.Color = HslToColor(h, Math.Max(0, Math.Min(1, s + delta)), l);
            }
            _updatingGlobalSlider = false;
            UpdatePreview();

            if (!_globalSliderDragging)
                ResnapshotColors();
        }

        private void OnLightnessSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingFromSlider || _originalColors.Count == 0)
                return;

            var delta = e.NewValue - _averageLightness;

            _updatingGlobalSlider = true;
            foreach (var item in _colorItems)
            {
                if (!_originalColors.TryGetValue(item.Key, out var original) || IsBlackOrWhite(original))
                    continue;
                var (h, s, l) = ColorToHsl(original);
                item.Color = HslToColor(h, s, Math.Max(0, Math.Min(1, l + delta)));
            }
            _updatingGlobalSlider = false;
            UpdatePreview();

            if (!_globalSliderDragging)
                ResnapshotColors();
        }

        private void OnGlobalSliderDragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _globalSliderDragging = true;
        }

        private void OnGlobalSliderDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _globalSliderDragging = false;
            ResnapshotColors();
        }

        private void ResnapshotColors()
        {
            _originalColors = _colorItems.ToDictionary(i => i.Key, i => i.Color);

            // Use slider positions as source of truth to avoid circular-averaging issues with hue
            _averageHue = HueSlider.Value;
            _averageSaturation = SaturationSlider.Value;
            _averageLightness = LightnessSlider.Value;
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

        private void UpdatePickerDisplay()
        {
            if (HueSatImage == null) return;

            var item = _selectedColorItem;
            if (item == null) return;

            var (h, s, l) = ColorToHsl(item.Color);
            RenderSatLightnessSquare(h);
            RenderSatLightnessSquare(h);
            RenderHueBar();
            UpdatePickerMarkers(h, s, l);
            UpdateSliderValues(item.Color, h, s, l);

            if (HexColorBox != null)
            {
                _updatingPicker = true;
                HexColorBox.Text = $"{item.Color.R:X2}{item.Color.G:X2}{item.Color.B:X2}";
                _updatingPicker = false;
            }
        }

        private void RenderSatLightnessSquare(double currentH)
        {
            if (_hueSatBitmap == null)
            {
                _hueSatBitmap = new WriteableBitmap(PickerSize, PickerSize, 96, 96, PixelFormats.Bgr24, null);
                HueSatImage.Source = _hueSatBitmap;
            }

            byte[] pixels = new byte[PickerSize * PickerSize * 3];
            for (int y = 0; y < PickerSize; y++)
            {
                double sat = 1.0 - (double)y / (PickerSize - 1);
                for (int x = 0; x < PickerSize; x++)
                {
                    double l = (double)x / (PickerSize - 1);
                    var c = HslToColor(currentH, sat, l);
                    int idx = (y * PickerSize + x) * 3;
                    pixels[idx] = c.B;
                    pixels[idx + 1] = c.G;
                    pixels[idx + 2] = c.R;
                }
            }

            _hueSatBitmap.WritePixels(
                new Int32Rect(0, 0, PickerSize, PickerSize),
                pixels, PickerSize * 3, 0);
        }

        private void RenderHueBar()
        {
            if (_hueBarBitmap == null)
            {
                _hueBarBitmap = new WriteableBitmap(BarWidth, PickerSize, 96, 96, PixelFormats.Bgr24, null);
                HueBarImage.Source = _hueBarBitmap;
            }

            byte[] pixels = new byte[BarWidth * PickerSize * 3];
            for (int y = 0; y < PickerSize; y++)
            {
                double h = (1.0 - (double)y / (PickerSize - 1)) * 360.0;
                var c = HslToColor(h, 1.0, 0.5);
                for (int x = 0; x < BarWidth; x++)
                {
                    int idx = (y * BarWidth + x) * 3;
                    pixels[idx] = c.B;
                    pixels[idx + 1] = c.G;
                    pixels[idx + 2] = c.R;
                }
            }

            _hueBarBitmap.WritePixels(
                new Int32Rect(0, 0, BarWidth, PickerSize),
                pixels, BarWidth * 3, 0);
        }

        private void UpdatePickerMarkers(double h, double s, double l)
        {
            if (Crosshair == null || HueBarMarker == null) return;

            double x = l * (PickerSize - 1);
            double y = (1.0 - s) * (PickerSize - 1);
            Canvas.SetLeft(Crosshair, x - Crosshair.Width / 2);
            Canvas.SetTop(Crosshair, y - Crosshair.Height / 2);

            double barY = (1.0 - h / 360.0) * (PickerSize - 1);
            Canvas.SetLeft(HueBarMarker, 0);
            Canvas.SetTop(HueBarMarker, barY - HueBarMarker.Height / 2);
        }

        private void UpdateSliderValues(Color c, double h, double s, double l)
        {
            _updatingFromSlider = true;
            SliderR.Value = c.R;
            SliderG.Value = c.G;
            SliderB.Value = c.B;
            SliderH.Value = Math.Round(h);
            SliderS.Value = Math.Round(s * 100);
            SliderL.Value = Math.Round(l * 100);
            _updatingFromSlider = false;
        }

        private void OnSwatchListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedColorItem = ColorSwatchList.SelectedItem as ColorEditItem;
        }

        private void OnColorSquareMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_selectedColorItem == null) return;
            _isDraggingSquare = true;
            HueSatCanvas.CaptureMouse();
            UpdateFromSquareMouse(e.GetPosition(HueSatImage));
        }

        private void OnColorSquareMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingSquare) return;
            UpdateFromSquareMouse(e.GetPosition(HueSatImage));
        }

        private void OnColorSquareMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingSquare)
            {
                _isDraggingSquare = false;
                HueSatCanvas.ReleaseMouseCapture();
            }
        }

        private void UpdateFromSquareMouse(Point pos)
        {
            var (h, s, l) = ColorToHsl(_selectedColorItem.Color);
            double newL = Math.Max(0, Math.Min(PickerSize - 1, pos.X)) / (PickerSize - 1);
            double newS = 1.0 - Math.Max(0, Math.Min(PickerSize - 1, pos.Y)) / (PickerSize - 1);
            _updatingPicker = true;
            _selectedColorItem.Color = HslToColor(h, newS, newL);
            _updatingPicker = false;
            RenderHueBar();
            UpdatePickerMarkers(h, newS, newL);
            UpdateSliderValues(_selectedColorItem.Color, h, newS, newL);
        }

        private void OnHueBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_selectedColorItem == null) return;
            _isDraggingBar = true;
            HueBarCanvas.CaptureMouse();
            UpdateFromBarMouse(e.GetPosition(HueBarImage));
        }

        private void OnHueBarMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingBar) return;
            UpdateFromBarMouse(e.GetPosition(HueBarImage));
        }

        private void OnHueBarMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingBar)
            {
                _isDraggingBar = false;
                HueBarCanvas.ReleaseMouseCapture();
            }
        }

        private void UpdateFromBarMouse(Point pos)
        {
            var (h, s, l) = ColorToHsl(_selectedColorItem.Color);
            double newH = (1.0 - Math.Max(0, Math.Min(PickerSize - 1, pos.Y)) / (PickerSize - 1)) * 360.0;
            _updatingPicker = true;
            _selectedColorItem.Color = HslToColor(newH, s, l);
            _updatingPicker = false;
            RenderSatLightnessSquare(newH);
            UpdatePickerMarkers(newH, s, l);
            UpdateSliderValues(_selectedColorItem.Color, newH, s, l);
        }

        private void OnRgbSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingFromSlider || _updatingPicker || _selectedColorItem == null) return;
            var c = Color.FromRgb((byte)SliderR.Value, (byte)SliderG.Value, (byte)SliderB.Value);
            _updatingPicker = true;
            _selectedColorItem.Color = c;
            var (h, s, l) = ColorToHsl(c);
            RenderSatLightnessSquare(h);
            RenderHueBar();
            UpdatePickerMarkers(h, s, l);
            _updatingFromSlider = true;
            SliderH.Value = Math.Round(h);
            SliderS.Value = Math.Round(s * 100);
            SliderL.Value = Math.Round(l * 100);
            _updatingFromSlider = false;
            _updatingPicker = false;
        }

        private void OnHslSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingFromSlider || _updatingPicker || _selectedColorItem == null) return;
            double h = SliderH.Value;
            double s = SliderS.Value / 100.0;
            double l = SliderL.Value / 100.0;
            var c = HslToColor(h, s, l);
            _updatingPicker = true;
            _selectedColorItem.Color = c;
            RenderSatLightnessSquare(h);
            RenderHueBar();
            UpdatePickerMarkers(h, s, l);
            _updatingFromSlider = true;
            SliderR.Value = c.R;
            SliderG.Value = c.G;
            SliderB.Value = c.B;
            _updatingFromSlider = false;
            _updatingPicker = false;
        }

        private void OnHexColorChanged(object sender, TextChangedEventArgs e)
        {
            if (_updatingPicker || _selectedColorItem == null) return;
            var text = HexColorBox.Text?.Trim() ?? "";
            if (text.Length == 0 || text.Length > 6) return;

            // Allow pasting with leading #
            if (text.StartsWith("#")) text = text.Substring(1);
            if (text.Length != 6) return;

            try
            {
                byte r = (byte)int.Parse(text.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = (byte)int.Parse(text.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = (byte)int.Parse(text.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                var c = Color.FromRgb(r, g, b);

                _updatingPicker = true;
                _selectedColorItem.Color = c;
                var (h, s, l) = ColorToHsl(c);
                RenderSatLightnessSquare(h);
                RenderHueBar();
                UpdatePickerMarkers(h, s, l);
                _updatingFromSlider = true;
                SliderR.Value = r;
                SliderG.Value = g;
                SliderB.Value = b;
                SliderH.Value = Math.Round(h);
                SliderS.Value = Math.Round(s * 100);
                SliderL.Value = Math.Round(l * 100);
                _updatingFromSlider = false;
                _updatingPicker = false;
            }
            catch { }
        }
    }
}
