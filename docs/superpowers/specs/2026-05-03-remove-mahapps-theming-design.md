# Remove MahApps.Metro — Custom Theming Design

**Goal:** Remove the MahApps.Metro dependency from OATControl and replace it with a custom theming system that supports switching between named themes at runtime.

**Motivation:** MahApps.Metro is on v1.6.5 (current is 2.x with breaking changes). Removing it eliminates an upgrade-or-migrate decision, reduces dependency count, and gives full control over theme palettes — important for an astronomy app where night-vision-preserving dark modes and high-contrast daylight modes matter.

## Hardcoded Color Extraction

The current XAML contains ~255 unique hardcoded color values across ~500+ instances. Analysis reveals these serve a small set of semantic purposes. The migration must extract them into named theme keys grouped by intent, not by value.

### Semantic Color Categories

The app is currently a monochromatic red palette. Different shades convey different meanings:

| Semantic Key | Current Red Value | Intent |
|---|---|---|
| `AppPrimaryBrush` | `#E00` | Primary accent — icons, active elements, toggle thumbs |
| `AppForegroundBrush` | `#B00` | Normal text content, data values |
| `AppForegroundStrongBrush` | `#F00` | Emphasized text — tooltips, warnings |
| `AppForegroundSubtleBrush` | `#200` / `#000` | Subdued text — secondary labels |
| `AppBorderBrush` | `#C00` | Borders, separators, column headers |
| `AppBorderSubtleBrush` | `#200` | Subtle separators, dividers |
| `AppBackgroundBrush` | `#800000` | Primary background (panels, icon buttons) |
| `AppBackgroundAltBrush` | `#600000` | Alternate/hover backgrounds |
| `AppSelectedBrush` | `#400` | Selected item backgrounds |
| `AppButtonBorderBrush` | `#401111` | Button outlines |
| `AppTooltipBackgroundBrush` | `#611` | Tooltip backgrounds |
| `AppWarningBrush` | `#FF8000` | Warning/attention (orange) |
| `AppSuccessBrush` | `#842` | Completed/checked items |
| `AppDangerBrush` | `#F86` / `#F40` | Unchecked/pending items |

### Interactive State Colors

Button and list item states follow a shade progression within the same hue:

| Semantic Key | Current Value | Context |
|---|---|---|
| `AppButtonBackgroundBrush` | `#800` | Normal button background |
| `AppButtonHoverBrush` | `#A22` | Mouse-over button background |
| `AppButtonPressedBrush` | `#C33` | Pressed button background |
| `AppItemHoverBrush` | `#600` | Mouse-over list item background |
| `AppItemSelectedBrush` | `#400` | Selected list item background |

### Strategy

1. **Define semantic keys in theme files** — each theme maps the same keys to different colors (e.g., a Daylight theme maps `AppPrimaryBrush` to a blue, `AppBackgroundBrush` to white)
2. **Replace hardcoded colors during Phase 4/5** — each inline `#E00` becomes `DynamicResource AppPrimaryBrush`, etc.
3. **Keep the set small** — the ~15 semantic keys above cover the vast majority of cases. Specialized colors (e.g., RangeSlider internals) can have a few extra keys scoped to their controls

## Current MahApps.Metro Usage

- **13 windows** inherit `MetroWindow`
- **Controls used:** `ToggleSwitchButton` (4 files), `ToggleSwitch` (1 file), `NumericUpDown` (style only, may be unused), `MetroTextBlock`/`MetroTextBox`/`MetroToolTip` styles
- **App.xaml** merges 3 MahApps resource dictionaries (Controls, Fonts, Red accent)
- **Resources/RedTheme.xaml** overrides ~30 MahApps brush keys for the current dark-red look
- **Resources/RedControls.xaml** defines additional slider/control colors
- **Custom controls** in `Controls/` reference MahApps brushes (`AccentBaseColorBrush`, `HighlightBrush`, `AccentColorBrush4`, etc.)
- **No advanced MahApps features** used (no Flyouts, MetroDialogs, ProgressRings)

## Theme Infrastructure

### ThemeManager

A singleton class responsible for runtime theme switching:

```
ThemeManager
├── AvailableThemes: List<string>          // e.g. "DarkAstronomy", "Daylight", "RedEye"
├── CurrentTheme: string                   // name of active theme
├── SwitchTheme(string name): void         // swaps theme dictionaries in Application.Current.Resources
└── GetThemeBrush(string key): Brush       // helper for code-behind brush lookups
```

`SwitchTheme` works by removing the old theme ResourceDictionary from `Application.Current.Resources.MergedDictionaries` and inserting the new one at the same position. All controls using `DynamicResource` references update automatically.

### Dual-Key Strategy

Theme XAML files define two sets of keys:

1. **Semantic keys** (new): `AppBackgroundBrush`, `AppTextBrush`, `AppAccentBrush`, `AppAccentBrush2`, etc.
2. **Legacy MahApps keys** (migration shim): `AccentBaseColorBrush`, `AccentColorBrush`, `HighlightBrush`, `BlackBrush`, `WhiteBrush`, etc.

Existing XAML that references `StaticResource AccentBaseColorBrush` continues to work during migration. New code and migrated code uses semantic keys with `DynamicResource` so theme switches take effect immediately.

The legacy keys can be removed once all references are migrated to semantic keys.

## ThemedWindow (replaces MetroWindow)

A custom class inheriting `Window`:

- `WindowChrome` provides borderless chrome with system-resize areas
- Custom title bar template with theme-styled minimize/maximize/close buttons
- Implicit `Style` targeting `ThemedWindow` applied via `Base.xaml`
- Window background, border, and title bar colors sourced from theme brushes

All 13 windows change their base class from `Controls:MetroWindow` to `local:ThemedWindow`.

## Control Replacements

| MahApps Control | Replacement | Notes |
|---|---|---|
| `MetroWindow` | `ThemedWindow` | Custom `Window` + `WindowChrome` |
| `ToggleSwitchButton` | Custom `ToggleSwitch` | UserControl with thumb/track, On/Off states |
| `ToggleSwitch` | Same custom `ToggleSwitch` | Merged into one control |
| `NumericUpDown` | Custom `NumericUpDown` | TextBox + RepeatButtons, if actually used |
| `MetroTextBlock` style | Implicit `TextBlock` style in `Base.xaml` | References `AppTextBrush` |
| `MetroTextBox` style | Implicit `TextBox` style in `Base.xaml` | References theme brushes |
| `MetroToolTip` style | Implicit `ToolTip` style in `Base.xaml` | References theme brushes |

All replacement controls use `DynamicResource` for theme-aware colors.

## Resource Dictionary Structure

```
Resources/
├── Themes/
│   ├── Base.xaml              — shared ControlTemplates, implicit styles for standard WPF controls
│   ├── DarkAstronomy.xaml     — dark theme with red accent (matches current look)
│   ├── Daylight.xaml          — light theme for daytime use
│   └── RedEye.xaml            — example alternate palette
├── RedControls.xaml           — contents migrated into Base.xaml or theme files, then deleted
└── RedTheme.xaml              — contents merged into DarkAstronomy.xaml, then deleted
```

**Base.xaml** contains:
- Implicit styles for Button, TextBox, CheckBox, RadioButton, TabControl/TabItem, ToolTip, ComboBox, Label, etc.
- All styles reference theme brushes via `DynamicResource`
- `ThemedWindow` default style and ControlTemplate
- Custom control styles (PushButton, StopButton, etc.)

**Theme files** (e.g. `DarkAstronomy.xaml`) contain only:
- `Color` definitions
- `SolidColorBrush` definitions (both semantic and legacy keys)
- No ControlTemplates or styles

## Migration Phases

### Phase 1: Theme Infrastructure
- Create `ThemeManager` class
- Create `Base.xaml` with implicit styles for all standard controls
- Create `DarkAstronomy.xaml` (reproducing the current dark-red look from RedTheme.xaml + RedControls.xaml + MahApps defaults)
- Wire `Base.xaml` + default theme into `App.xaml` alongside existing MahApps dictionaries

### Phase 2: ThemedWindow
- Create `ThemedWindow` class with `WindowChrome` template
- Create default style in `Base.xaml`

### Phase 3: Replacement Controls
- Create custom `ToggleSwitch` UserControl
- Verify whether `NumericUpDown` is actually used in any XAML (audit found only a style definition, no instances). If unused, skip creating a replacement and just remove the style.
- Add styles to `Base.xaml`

### Phase 4: Migrate Windows and Extract Hardcoded Colors
- For each of the 13 windows:
  - Change base class from `MetroWindow` to `ThemedWindow`
  - Remove MahApps namespace import
  - Replace MahApps controls with custom equivalents
  - Replace all hardcoded color values with `DynamicResource` semantic keys from the theme
  - Update `StaticResource` to `DynamicResource` where appropriate

### Phase 5: Migrate Custom Controls
- Update `PushButton`, `StopButton`, `RangeSlider`, `Joystick` in `Controls/`
- Replace MahApps brush references with semantic theme keys
- Replace hardcoded colors with `DynamicResource` semantic keys
- RangeSlider may need a few additional scoped keys for its internal palette

### Phase 6: Cleanup
- Remove MahApps.Metro NuGet package reference
- Remove MahApps resource dictionaries from `App.xaml`
- Remove legacy MahApps key definitions from theme files
- Delete `RedTheme.xaml` and `RedControls.xaml`

### Phase 7: Theme Picker UI
- Add theme selection to the Settings dialog (`DlgAppSettings.xaml`)
- Bind to `ThemeManager.CurrentTheme`
- Persist theme choice in application settings

## Files Changed (Estimated)

- **New files:** ~10 (ThemeManager, ThemedWindow, ToggleSwitch, NumericUpDown, Base.xaml, 3 theme files)
- **Modified XAML:** ~20 (13 windows + App.xaml + custom controls + resource dictionaries)
- **Modified C#:** ~15 (window code-behinds, ViewModels, App.xaml.cs)
- **Removed files:** 2 (RedTheme.xaml, RedControls.xaml)
- **Removed NuGet:** MahApps.Metro 1.6.5
