# Changelog

## V1.2.0.0

### MahApps.Metro Removal

- Removed MahApps.Metro (v1.6.5) and ControlzEx (v3.0.2.4) NuGet dependencies
- Removed MahApps.Metro, ControlzEx, and System.Windows.Interactivity assembly references from .csproj
- Removed MahApps.Metro DLLs from InnoSetup installer (`OATControl Setup.iss`)
- Deleted legacy theme resource files: `RedAccent.xaml`, `RedControls.xaml`, `RedTheme.xaml`, `GreyControls.xaml`

### Custom Theme Engine

- Created `ThemeManager` singleton (`Theming/ThemeManager.cs`, +377 lines) for runtime theme loading, brush generation, and theme switching
- Created `ThemeColorDefinitions.cs` — centralized registry of all color keys with display names, groups, and default values
- `ThemeManager.GenerateBrushes` creates `SolidColorBrush` resources at runtime from theme `Color` resources; also generates `SystemColors` override brushes (`HighlightTextBrushKey`, `ControlTextBrushKey`, `InactiveSelectionHighlightBrushKey`)
- Brush key naming convention: `AppXxxColor` (Color resource) → `AppXxxBrush` (generated SolidColorBrush)
- User themes stored as XAML in `%AppData%\OpenAstroTracker\Themes\`; scanned and validated on startup
- Theme selection persisted in `AppSettings.ThemeName` (defaults to DarkAstronomy)
- Theme picker added to `DlgAppSettings` General tab with live switching
- Added `ThemeManager.ImportTheme`/`ExportTheme`/`DeleteTheme` for user theme file management

### Theme Files

- `Resources/Themes/Base.xaml` (+652 lines) — implicit styles for all standard WPF controls: TextBlock, TextBox, Button, CheckBox, RadioButton, ComboBox, TabControl, TabItem, ScrollBar, ProgressBar, ListViewItem, ToggleButton. All use `DynamicResource` for theme-aware binding.
- `Resources/Themes/DarkAstronomy.xaml` — dark theme with red accent palette (49 color definitions)
- `Resources/Themes/Daylight.xaml` — light theme with blue accent palette (49 color definitions)
- Bundled user themes: Blue Planet, Dark Observatory, Deep Space, NINA (`Theming/*.xaml`)
- Theme XAML files contain only `Color` resources; brushes generated at runtime

### Custom Controls

- **ThemedWindow** (`Controls/ThemedWindow.cs`) — `Window` subclass with `WindowChrome` (`CaptionHeight=30`, `ResizeBorderThickness=4`, `GlassFrameThickness=0`). Replaces `MetroWindow`. Provides borderless window with custom chrome.
  - `ShowTitleBar` property controls title bar visibility
  - `TitleBarButtons` collection for custom title bar buttons
  - `ControlTemplate` in Base.xaml with `WindowCommands` (minimize, maximize, close)
- **ToggleSwitch** (`Controls/ToggleSwitch.xaml/.cs`) — replaces `MahApps.Metro.Controls.ToggleSwitchButton`. Pill-shaped toggle with sliding thumb. `IsChecked` DP with `ThumbIndicatorBrush` for theme integration.
- **IconButton** (`Controls/IconButton.xaml/.cs`) — icon-only button control using `ResourceDictionary` style pattern. Replaces MahApps icon button usage.
- **LabeledToggleSwitch** (`Controls/LabeledToggleSwitch.xaml/.cs`) — ToggleSwitch with label text, used in settings and theme editor.
- **SlewProgressBar** (`Controls/SlewProgressBar.xaml/.cs`) — reusable progress bar for slew/drift align. Three DPs: `Progress` (double 0-1), `IsActive` (bool), `BarThickness` (double, default 6). Renders as `Border` with `LinearGradientBrush` using theme accent/disabled colors. Replaces 3 inline progress bar implementations in MainWindow.

### Window/Dialog Migration

All 14 windows/dialogs migrated from `MetroWindow` to `ThemedWindow`:
- MainWindow, DlgAppSettings, DlgAxisCalibration, DlgChecklist, DlgChecklistEditor, DlgChooseOat, DlgCustomActionSetup, DlgEditPoint, DlgMessageBox, DlgNinaPolarAlignment, DlgRunPolarAlignment, DlgRunPolarAlignmentStep1, DlgSharpCapPolarAlignment, DlgWaitForGXState, MiniController, SettingsDialog, SlewPointsWindow, TargetChooser

Each dialog:
- Replaced `MetroWindow` base with `ThemedWindow`
- Removed MahApps namespace declarations
- Replaced `ToggleSwitchButton` instances with `ToggleSwitch`

### Color Migration

- Converted all `StaticResource` brush references to `DynamicResource` across 12+ XAML files for runtime theme switching
- Replaced all MahApps brush key references (`AccentBaseColorBrush`, `AccentColorBrush2/3/4`, `TextBrush`, `WhiteBrush`, `WindowBackgroundBrush`, `ControlBackgroundBrush`, `HighlightBrush`) with semantic theme keys (`AppForegroundBrush`, `AppPrimaryBrush`, `AppBackgroundBrush`, etc.)
- Replaced ~287 hardcoded hex color values across 22+ XAML files with `DynamicResource` semantic key bindings
- Removed `AppPrimaryColor`/`AppPrimaryBrush` — all references migrated to specific semantic keys (`AppForegroundBrush`, `AppButtonBorderBrush`, `AppButtonHoverBrush`, etc.)
- `ScopeCircles` and `ScopePointer` — converted `Foreground` from CLR property to `DependencyProperty` for `DynamicResource` binding support
- `MotorIndicator` — refactored to use theme brushes

### Theme Editor (DlgThemeEditor)

- New dialog (`DlgThemeEditor.xaml` 594 lines, `DlgThemeEditor.xaml.cs` 1048 lines) with:
  - Live preview as mini app window with real themed controls
  - HSL color picker with interactive saturation/lightness square and hue bar
  - RGB and HSL slider inputs with bidirectional sync
  - Hex color textbox (RRGGBB) with bidirectional sync to RGB/HSL sliders
  - Color key selection grouped by category (Text, Background, Buttons, etc.)
  - New Theme: clones current theme into editable user theme
  - Save/Save As: prompts for theme name + author, saves as user theme XAML
  - Import/Export: file dialogs for sharing theme files
  - Editor chrome pinned to Daylight theme (doesn't change when editing other themes)
  - `_updatingPicker` guard prevents re-entrant picker updates; `HexColorBox.Text` still updated during picker-driven changes
  - `_suppressSelectionChanged` prevents handler during programmatic list selection

### Style Consolidation

- Added semantic `TextBlock` styles to Base.xaml: header, subheader, body, caption styles
- Unified `ListViewItem` template with consistent selection/hover behavior
- Consolidated `ToggleButton` and `Button` styles into Base.xaml
- Renamed button styles to semantic names
- Added `MetroListBoxItem` named style for `ListBoxItem`
- Added implicit `ToggleButton` style with themed hover/pressed/checked/disabled states

### Other Changes

- Removed `System.Windows.Forms` dependency; replaced with WPF-native `Microsoft.Win32` file dialogs
- Removed `DlgStepCalibration.xaml/.cs` (789 lines) — unused dialog
- Removed unused style resources
- Fixed `MiniController` layout and styling
- Cleaned up dialog chrome and removed unused files
- Version bumped from 1.1.24.0 to 1.2.0.0 (`AssemblyInfo.cs`, `OATControl Setup.iss`)
- `MountVM.cs` — replaced MahApps `DialogManager.ShowMessageAsync()` with `MessageBox.Show()` for DEC limits dialog
- `PushButton.xaml`/`StopButton.xaml` — removed MahApps converter imports, migrated all brush references to DynamicResource
- `RangeSlider.xaml/.cs` — tick color properties (`MajorTickColor`, `MinorTickColor`, `TickLabelColor`) converted from CLR properties to `DependencyProperty`s for `DynamicResource` binding
- `RangeSlider.xaml` — replaced ~12 hardcoded `SolidColorBrush` resources with theme-derived bindings
- `MotorIndicator.cs` — added `DisabledForeground` DP; control visually dims when mount is disconnected
- `ThemedWindow.cs` — automatic `Background`/`Foreground` binding via `SetResourceReference`; tinted icon support in title bar; `OnShowTitleBarChanged` dynamically toggles `WindowChrome.CaptionHeight`
- `DlgChecklist.xaml` — replaced `BoolToBrushConverter` with `DataTrigger`-based Foreground bindings for correct `DynamicResource` resolution
- `App.xaml` status icons (`WaitingIcon`, `CompleteIcon`, `InProgressIcon`) changed from hardcoded `Brush="Red"` to `DynamicResource AppForegroundBrush`
- `TargetChooser.xaml` — fixed out-of-theme cyan hover border by inheriting implicit `ListViewItem` style
- Dialogs set `TitleBarButtons="Close"` where minimize/maximize are inappropriate (DlgAppSettings, DlgAxisCalibration, DlgChooseOat, DlgChecklistEditor, DlgMessageBox)
- Chromeless windows (`ShowTitleBar=False`): DlgChecklist, MiniController, SlewPointsWindow
- ThemeManager hot-reload: `FileSystemWatcher` monitors theme XAML in DEBUG builds for instant preview during development
- Installer: ships bundled user themes to `%AppData%\OpenAstroTracker\Themes\` with `onlyifdoesntexist` flag
