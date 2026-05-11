# Custom User Themes Design

## Goal

Allow all users (not just power users) to create, edit, import, and export custom color themes for OATControl. V1 is import/export files; online sharing is a roadmap item.

## Theme File Structure

**Built-in themes** stay as compiled XAML in `Resources/Themes/` loaded via `pack://` URIs (unchanged).

**User themes** live in `%AppData%\OpenAstroTracker\Themes\` as loose `.xaml` files. Each file is a `ResourceDictionary` containing **only Color definitions** — no brushes. The `ThemeManager` auto-generates matching `SolidColorBrush` entries when loading.

Optional metadata strings (`ThemeName`, `ThemeAuthor`) can be included. The file name minus `.xaml` is the fallback display name.

Example:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">
    <sys:String x:Key="ThemeName">Neon Purple</sys:String>
    <sys:String x:Key="ThemeAuthor">jsmith</sys:String>

    <Color x:Key="AppPrimaryColor">#FF9B30FF</Color>
    <Color x:Key="AppForegroundColor">#FFE0E0E0</Color>
    <!-- ... remaining color keys ... -->
</ResourceDictionary>
```

**Missing colors:** Not an error. `DynamicResource` falls back through the merged dictionary chain to the built-in theme values. The editor shows defaults pre-filled.

**Theme validation:** A `.xaml` file is only treated as a theme if it parses as a valid `ResourceDictionary` **and** contains at least one recognized color key (e.g. `AppPrimaryColor`, `AppForegroundColor`). Random XAML files in the folder (windows, user controls, etc.) are silently skipped. This prevents non-theme files from appearing in the theme list.

## ThemeManager Changes

### Theme scanning

On initialization, scan `%AppData%\OpenAstroTracker\Themes\*.xaml`. Add each file name (sans extension) to `AvailableThemes`. Track which themes are built-in vs user so the UI can distinguish them (user themes are editable/deletable; built-in are not). Create the folder on first use if it doesn't exist.

### Brush generation

New private method `GenerateBrushes(ResourceDictionary dict)`. Iterates a hardcoded list of known color-to-brush pairs (e.g. `AppPrimaryColor` → `AppPrimaryBrush`) and adds a `SolidColorBrush` for each color found in the dictionary. Colors absent from the user file are skipped — they fall through to the built-in theme.

### Load user theme

New code path in `ApplyTheme`. If the theme name is not in the built-in list, load from the AppData folder using a `file://` URI. After loading, call `GenerateBrushes` before inserting into merged dictionaries.

### Theme metadata

New helper `GetThemeMetadata(string themeName)` that loads the dictionary and extracts `ThemeName` and `ThemeAuthor` strings if present, for the UI to display. Falls back to file name for the name.

### Import / Export

- `ImportTheme(string sourceFilePath)` — copies the `.xaml` file into the user themes folder, validates it, adds to `AvailableThemes`.
- `ExportTheme(string themeName, string author, string destinationPath)` — writes the color keys as a clean user-format file. For built-in themes, extracts only the color keys (no brushes). Includes `ThemeName` and `ThemeAuthor` metadata.

### Delete

`DeleteTheme(string themeName)` — removes the file from AppData and removes from `AvailableThemes`. Only allowed for user themes. If the deleted theme is currently active, fall back to DarkAstronomy.

## Theme Editor Dialog (DlgThemeEditor)

A new standalone `ThemedWindow` opened from `DlgAppSettings`. The editor always renders using the Daylight theme for its own chrome, ensuring it stays readable regardless of what colors the user is editing. Only the live preview panel reflects the custom colors being configured.

### Layout — three panels

**Left panel: Theme list + actions.** `ListBox` showing all available themes. Built-in themes are labeled but not editable. User themes have Edit/Delete context actions. Bottom: "New Theme" and "Import Theme" buttons.

**Center panel: Color editor grid.** Scrollable grid of all ~25 color keys. Each row shows:
- Human-friendly semantic label (e.g. "Primary Accent", "Button Hover")
- Color preview swatch
- Color picker control
- "Reset to default" button (resets to DarkAstronomy value)

**Right panel: Live preview.** Small mockup area with sample controls (button, text block, checkbox, title bar, border, toggle) styled with the custom colors being configured, updating in real-time as the user edits. This is the only area that uses the custom colors — the editor chrome itself remains Daylight-themed.

**Bottom bar:** Save, Save As, Export, Cancel.

### Behavior

- Opening with a user theme selected pre-fills all colors from that theme.
- "New Theme" starts from a copy of the current theme's colors.
- Missing colors in a user theme show the built-in default value.
- Saving writes to AppData and calls `ThemeManager.ApplyTheme()` to apply the theme app-wide. Edits are not reflected outside the preview panel until Save is clicked.

## Settings Dialog Changes (DlgAppSettings)

The existing theme `ComboBox` continues to bind `ThemeManager.AvailableThemes`. Optionally visually distinguish built-in vs user themes.

New buttons below the ComboBox:
- **Edit Theme** — opens `DlgThemeEditor` with current theme. Disabled for built-in themes.
- **Create Theme** — opens `DlgThemeEditor` with new theme pre-filled from current colors.
- **Import Theme** — file dialog to pick `.xaml`, calls `ThemeManager.ImportTheme()`.

## Error Handling

- **Corrupt XAML on scan:** Skip the file, log warning, don't crash startup.
- **Missing color keys in user theme:** No error. Fall back to built-in values via `DynamicResource` chain. Editor pre-fills defaults.
- **File in use / permission denied on save:** Show message dialog, let user retry.
- **Import of invalid file:** Validate before copying. Show error if not a valid `ResourceDictionary` or contains no recognizable color keys.
