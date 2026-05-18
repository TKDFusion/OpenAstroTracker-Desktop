# OATControl V1.2.0 Release Notes

## Custom Theme Engine

OATControl now has a completely custom theming system. The MahApps.Metro dependency has been fully removed and replaced with a purpose-built theme engine that gives you full control over the application's appearance.

### Built-in Themes

- **Dark Astronomy** — the classic dark red theme, now the default
- **Daylight** — a new light theme with blue accents, ideal for daytime use
- **Blue Planet**, **Dark Observatory**, **Deep Space**, **NINA** — additional themes bundled with the installer

Switch themes instantly from **Settings > General > Theme**. Your selection is saved and restored on the next launch.

### Theme Editor

A full visual theme editor lets you create and customize your own themes:

- **Live preview** shows your changes on a miniature app window in real time
- **Interactive color picker** with HSL saturation/lightness square, hue bar, and RGB/HSL sliders
- **Hex input** for precise color entry
- **Grouped color keys** organized by category (text, backgrounds, buttons, borders, etc.)
- **Save and share** your themes as files — import themes from other users or export your creations

Open the theme editor from **Settings > General > Edit Themes**.

### Smaller Installer

The installer no longer includes the MahApps.Metro and ControlzEx libraries. This reduces the number of deployed DLLs and simplifies the application dependency chain.

## Other Improvements

- All dialogs and windows now use a unified custom window chrome with consistent styling
- Slew progress bars for RA, DEC, and drift alignment have been consolidated into a reusable control
- Toggle switches, buttons, and other controls have been restyled for consistent theme support
- Color adjustments and polish across the entire UI
- Removed the unused Step Calibration dialog
- Replaced System.Windows.Forms file dialogs with native WPF equivalents
