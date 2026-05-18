# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Desktop software suite for the OpenAstroTracker (OAT) DIY equatorial telescope mount. Provides a WPF control application, an ASCOM telescope/focuser driver, a 3D simulation, and a protocol test harness. All communicate with OAT firmware via serial or WiFi using the LX200 Meade command protocol.

## Solutions and Build

Three independent Visual Studio solutions. All require Visual Studio 2019+ on Windows.

### ASCOM Driver (`ASCOM.Driver/OpenAstroTracker.sln`)
```bash
nuget restore ASCOM.Driver/OpenAstroTracker.sln
msbuild ASCOM.Driver/OpenAstroTracker.sln /p:Configuration=Debug /p:Platform="Any CPU"
```
Requires .NET Framework 4.0 targeting pack and ASCOM Platform 6.5+. Output: `ASCOM.Driver/OpenAstroTracker/bin/Debug/ASCOM.OpenAstroTracker.exe`.

### OATControl (`OATControl/OATControl.sln`)
```bash
nuget restore OATControl/OATControl.sln
msbuild OATControl/OATControl.sln /p:Configuration=Debug /p:Platform="Any CPU"
```
Requires .NET Framework 4.7.2. Includes OATCommunications, OATCommunications.WPF, OATCommunications.ASCOM, and OATTest projects. Current version is 1.2.0.0. Must be built in Visual Studio on Windows (not available in WSL).

### OATSimulation (`OATSimulation/OATSimulation.sln`)
```bash
nuget restore OATSimulation/OATSimulation.sln
msbuild OATSimulation/OATSimulation.sln /p:Configuration=Debug /p:Platform="Any CPU"
```
Requires .NET Framework 4.6. Uses HelixToolkit.Wpf.SharpDX for 3D rendering.

## Testing

No unit test projects. Validation uses:
- **ASCOM Conformance Tool** (external) — validates ASCOM driver compliance
- **OATTest** (`OATTest/`) — WPF app with XML-based test suites for protocol command verification
- Manual testing with OATControl and real/simulated hardware

## Architecture

```
Client Applications
├── OATControl (WPF)        — Main mount control UI, custom theming
├── OATTest (WPF)           — Protocol test harness
├── OATSimulation (WPF)     — 3D mount visualization (SharpDX)
└── ASCOM.Driver (WinForms) — ASCOM telescope/focuser driver

        ↓ all depend on

OATCommunications (.NET Standard 2.0)
├── ICommunicationHandler   — Core interface for all comm handlers
├── CommunicationHandler    — Base class with threaded job queue
├── SerialCommunicationHandler  — Serial port (OATCommunications.WPF)
├── TcpCommunicationHandler — WiFi/TCP with UDP discovery
└── ASCOMCommunicationHandler — Wraps ASCOM driver (OATCommunications.ASCOM)

        ↓ sends LX200 Meade protocol commands via serial/TCP

OAT Mount Firmware
```

### Communication Protocol

LX200 Meade ASCII commands terminated with `#`. Key commands:
- `:GR#` / `:GD#` — Get RA / Get DEC
- `:SrHH:MM:SS#` / `:SdDD*MM:SS#` — Set target RA / DEC
- `:MS#` — Start slew to target
- `:Qq#` — Stop all motion

Response types: `NoResponse`, `DigitResponse` (single char), `FullResponse` (`#`-terminated), `DoubleFullResponse` (two `#`-terminated strings, used by `:SC#`).

### Project Relationships

- **OATCommunications** is the shared core library (.NET Standard 2.0) used by all clients
- **OATCommunications.WPF** adds serial handler + WPF UI controls/converters, used by OATControl and OATTest
- **OATCommunications.ASCOM** wraps the ASCOM driver as a communication backend, allowing OATControl to connect via ASCOM
- **GuideCamCapture** contains only compiled output (no source code in repo)

## Key Files

- `OATCommunications/ICommunicationHandler.cs` — Communication contract
- `OATCommunications/CommunicationHandlers/CommunicationHandler.cs` — Job queue base implementation
- `ASCOM.Driver/TelescopeDriver/Driver.cs` — ASCOM ITelescopeV3 implementation (~1100 lines)
- `ASCOM.Driver/TelescopeDriver/FocuserDriver.cs` — ASCOM IFocuserV3 implementation
- `ASCOM.Driver/OpenAstroTracker/SharedResources.cs` — Profile management, logging, connection init
- `OATControl/ViewModels/MountVM.cs` — Main OATControl application logic

## Theming System

Custom theme engine (MahApps.Metro fully removed). Supports runtime switching, user themes, hot-reload, and a built-in theme editor. The `AppPrimaryColor`/`AppPrimaryBrush` keys have been removed — all references migrated to semantic keys (`AppForegroundBrush`, `AppButtonBorderBrush`, `AppButtonHoverBrush`, etc.).

### Theme file structure
- Theme XAML files contain **only `Color` resources** (no brushes)
- `SolidColorBrush` resources are generated at runtime by `ThemeManager.GenerateBrushes`
- `ThemeColorDefinitions.cs` defines all color keys, display names, groups, and defaults
- Brush key naming: `AppXxxColor` → `AppXxxBrush` (via `BrushKeyFromColorKey`)

### Key files
- `OATControl/Theming/ThemeManager.cs` — Runtime theme loading, brush generation, user theme CRUD
- `OATControl/Theming/ThemeColorDefinitions.cs` — Color key registry (used by editor, validation, import/export)
- `OATControl/Resources/Themes/Base.xaml` — Implicit control styles (templates, triggers using DynamicResource)
- `OATControl/Resources/Themes/DarkAstronomy.xaml` — Dark theme (colors only)
- `OATControl/Resources/Themes/Daylight.xaml` — Light theme (colors only)
- `OATControl/DlgThemeEditor.xaml/.cs` — Theme editor dialog (color picker, live preview, save/export)

### User themes
- Stored as XAML in `%AppData%\OpenAstroTracker\Themes\`
- Filename is file-safe (non-alphanumeric → `_`), metadata `ThemeName` stores display name
- ThemeManager scans user folder on startup; validates files contain at least one known color key

### DlgThemeEditor notes
- Editor chrome pinned to Daylight via local ResourceDictionary + GenerateBrushes in Window_Loaded
- `_editingTheme` = file-safe identifier for ThemeManager; `EditingThemeName` = display name only
- `_suppressSelectionChanged` prevents SelectionChanged handler during programmatic selection (e.g., "New Theme")
- ListBox uses `ThemeListEntry` objects with `DisplayMemberPath="DisplayName"` / `SelectedValuePath="Id"`
- `_updatingPicker` guards against re-entrant picker updates when color changes originate from the picker itself. `OnSelectedColorChanged` must still update `HexColorBox.Text` even when `_updatingPicker` is true, since the hex display is not bound to the model

## CI/CD

Azure Pipelines (`ASCOM.Driver/azure-pipelines.yml`) triggers on `master`. Builds ASCOM driver in Release, generates InnoSetup installer, publishes artifacts.

## Conventions

- Firmware protocol: LX200 Meade command set
- ASCOM driver ID: `ASCOM.OpenAstroTracker.Telescope`
- Driver registration: `ASCOM.OpenAstroTracker.exe /register` or `/unregister`
- CultureInfo-aware float parsing throughout (use `CultureInfo.InvariantCulture` for protocol values)
- See `ASCOM.Driver/CLAUDE.md` for ASCOM driver-specific details
