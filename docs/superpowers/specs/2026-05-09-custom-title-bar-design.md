# Custom Title Bar for ThemedWindow

## Problem

ThemedWindow sets `WindowStyle.None` and `GlassFrameThickness=0`, which removes all native window chrome. The migration from MahApps.Metro never replaced the title bar, leaving windows with no minimize/maximize/close buttons and content flush against the top edge.

Additionally, five windows in OATControl still inherit from plain `Window`. Three of them (SlewPointsWindow, DlgChecklist, MiniController) have `WindowStyle="None"` and show an unwanted white bar. Two others (DlgCustomActionSetup, DlgMessageBox) should also be themed.

## Design

### Architecture

A default `ControlTemplate` applied to `ThemedWindow` via a `Style` in `Base.xaml`. The template wraps the window's `Content` in a `DockPanel` with the title bar docked to the top. All ThemedWindow instances inherit it automatically.

### Title Bar Layout

```
[Icon 16x16] [Title text]                    [_] [square] [X]
```

- **Height**: 30px, matching existing `CaptionHeight`
- **Left side**: App icon 24x24 (bound to `Window.Icon`), title text (bound to `Window.Title`)
- **Right side**: Minimize, Maximize/Restore, Close buttons (visibility controlled by `TitleBarButtons`)
- **Background**: `AppTitleBarBackgroundBrush`
- **Foreground**: `AppTitleBarForegroundBrush`
- **Drag**: The title bar area (not the buttons) serves as the drag surface via WindowChrome

### ShowTitleBar Attached Property

- Defined on `ThemedWindow` as an attached property, defaults to `true`
- The `ControlTemplate` uses a trigger bound to `ShowTitleBar` to collapse the title bar
- Windows that need no chrome set `controls:ThemedWindow.ShowTitleBar="False"`

### TitleBarButtons Flags Enum

```csharp
[Flags]
public enum TitleBarButtons
{
    None = 0,
    Minimize = 1,
    Maximize = 2,
    Close = 4,
    All = Minimize | Maximize | Close
}
```

- Attached property on `ThemedWindow`, defaults to `All`
- XAML usage: `controls:ThemedWindow.TitleBarButtons="Close"` or `controls:ThemedWindow.TitleBarButtons="Minimize,Maximize,Close"`
- Each button's visibility is controlled by checking its flag
- Example: `DlgMessageBox` sets `TitleBarButtons="Close"` — only shows the close button

### Button Behavior

- **Minimize**: `SystemCommands.MinimizeWindowCommand`
- **Maximize/Restore**: `SystemCommands.MaximizeWindowCommand` / `SystemCommands.RestoreWindowCommand`, button glyph toggles based on `WindowState`
- **Close**: `SystemCommands.CloseWindowCommand`
- **Hover**: `AppTitleBarButtonHoverColor`
- **Pressed**: `AppTitleBarButtonPressedColor`
- **Close hover accent**: Red tint on hover (standard Windows pattern)

### Button Glyphs

Path-based vector icons (no font dependency):
- Minimize: horizontal line
- Maximize: square outline
- Restore: overlapping squares
- Close: X

### Files Changed

#### Core (2 files)

1. **`Controls/ThemedWindow.cs`** — Add `SystemCommands` command bindings, `ShowTitleBar` attached property, `TitleBarButtons` enum and attached property
2. **`Resources/Themes/Base.xaml`** — Add implicit `Style` for `ThemedWindow` with `ControlTemplate` containing the title bar, triggers for `ShowTitleBar` and `TitleBarButtons`

#### Window conversions — chromeless (6 files, 3 windows)

3. **`SlewPointsWindow.xaml`** — Change root to `controls:ThemedWindow`, add `ShowTitleBar="False"`
4. **`SlewPointsWindow.xaml.cs`** — Change base class to `ThemedWindow`
5. **`DlgChecklist.xaml`** — Same pattern as SlewPointsWindow
6. **`DlgChecklist.xaml.cs`** — Same pattern
7. **`MiniController.xaml`** — Same pattern
8. **`MiniController.xaml.cs`** — Same pattern

#### Window conversions — with title bar (4 files, 2 windows)

9. **`DlgCustomActionSetup.xaml`** — Change root to `controls:ThemedWindow`
10. **`DlgCustomActionSetup.xaml.cs`** — Change base class to `ThemedWindow`
11. **`DlgMessageBox.xaml`** — Change root to `controls:ThemedWindow`, add `TitleBarButtons="Close"`
12. **`DlgMessageBox.xaml.cs`** — Change base class to `ThemedWindow`

No changes to the existing 13 ThemedWindow files (they get `TitleBarButtons="All"` and `ShowTitleBar="True"` by default).

### Theme Resources (already exist)

Both `DarkAstronomy.xaml` and `Daylight.xaml` already define:
- `AppTitleBarBackgroundColor` / `AppTitleBarBackgroundBrush`
- `AppTitleBarForegroundColor` / `AppTitleBarForegroundBrush`
- `AppTitleBarButtonHoverColor` / `AppTitleBarButtonHoverBrush`
- `AppTitleBarButtonPressedColor` / `AppTitleBarButtonPressedBrush`
