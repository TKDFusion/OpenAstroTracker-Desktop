# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASCOM driver for OpenAstroTracker (OAT) mounts. Translates ASCOM telescope/focuser standard interfaces into LX200 Meade protocol commands sent to OAT firmware via serial or WiFi.

## Build

Requires Visual Studio 2019+ with .NET Framework 4.0 targeting pack, and ASCOM Platform 6.5+ installed.

```bash
# Restore NuGet packages and build
nuget restore OpenAstroTracker.sln
msbuild OpenAstroTracker.sln /p:Configuration=Debug /p:Platform="Any CPU"

# Build installer (requires InnoSetup via NuGet package)
.\OpenAstroTracker\packages\Tools.InnoSetup.5.6.1\tools\ISCC.exe "OpenAstroTracker Setup.iss"
```

Output: `OpenAstroTracker\bin\Debug\ASCOM.OpenAstroTracker.exe` and `TelescopeDriver\bin\Debug\ASCOM.OpenAstroTracker.Telescope.dll`.

## Testing

No unit test project exists. Testing is done via:
- **ASCOM Conformance Tool** (external) — validates driver against ASCOM standards
- **OATTest** app (`../OATTest/`) — WPF test harness for protocol/command testing

## Architecture

```
ASCOM Client Software (NINA, KStars, etc.)
    ↓ COM/ASCOM interface
OpenAstroTracker LocalServer (OpenAstroTracker/ project)
    ↓
TelescopeDriver/ project
    ├── Driver.cs        — ITelescopeV3 (slew, park, sync, tracking)
    └── FocuserDriver.cs — IFocuserV3 (focus position, movement)
    ↓
SharedResources.cs — profile/settings, logging, connection state
    ↓
OATCommunications library (../OATCommunications/)
    ↓ Serial/TCP
OpenAstroTracker Mount Firmware
```

### Solution: Two Projects

- **OpenAstroTracker/** — WinForms local server executable. Handles COM registration/lifecycle (`LocalServer.cs`, `ClassFactory.cs`), setup UI, and shared resources.
- **TelescopeDriver/** — Class library DLL with the actual ASCOM driver implementations. Post-build copies DLL into the server's output directory.

### Key Files

- `TelescopeDriver/Driver.cs` (~1100 lines) — Core telescope driver, all ASCOM property/method implementations
- `TelescopeDriver/FocuserDriver.cs` (~300 lines) — Focuser driver
- `OpenAstroTracker/SharedResources.cs` — Profile management (COM port, IP, tracking mode, calibration), logging, communication handler init
- `OpenAstroTracker/LocalServer.cs` — COM object factory and registration
- `TelescopeDriver/SetupDialogForm.cs` — Configuration UI (connection settings, tracking, calibration, advanced options)

## CI/CD

Azure Pipelines (`azure-pipelines.yml`) triggers on `master`. Builds release, generates InnoSetup installer, runs VSTest, publishes artifacts.

## Conventions

- Driver registration: `ASCOM.OpenAstroTracker.exe /register` or `/unregister`
- Firmware protocol: LX200 Meade command set (`:GR#`, `:GD#`, `:Sr...#`, `:Sd...#`, `:MS#`, etc.)
- Profile storage uses ASCOM Profile class with driver ID `ASCOM.OpenAstroTracker.Telescope`
