# GitHub Release Update Checker — Design Spec

## Problem

OATControl has no mechanism to notify users when a new desktop app or mount firmware version is available. Users must manually check GitHub releases.

## Solution

Add automatic GitHub release checking for both the desktop app and the mount firmware. Desktop checks run at startup; firmware checks run after mount connection. All network calls are fire-and-forget with short timeouts — if there's no internet (common during field imaging sessions), the app behaves exactly as it does today.

## User-Facing Behavior

### Desktop App Update

1. On app startup, after the main window loads, an async check queries `https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Desktop/releases/latest`
2. If a newer version is found, a modal dialog (`DlgUpdateAvailable`) appears immediately, blocking the main window
3. The dialog shows:
   - "OATControl V{new} is now available (you have V{current})"
   - The release changelog (plain text from the GitHub release body)
   - **Skip** button — dismisses the dialog, user continues normally
   - **Upgrade** button — starts the download flow
4. When the user clicks Upgrade:
   - The changelog area is replaced with a progress bar
   - The installer (~2.2 MB) is downloaded from the release asset's `browser_download_url` to the system temp folder
   - On completion: the installer is launched via `Process.Start()` and the app exits via `Application.Current.Shutdown()`
   - On download failure: an error message is shown; the user can close the dialog and continue normally
5. If no update is available, or the check fails (no internet, timeout, parse error), nothing happens — no dialog, no notification

### Firmware Update

1. After a successful mount connection, once the firmware version has been parsed (in `MountVM.ConnectToOat`), an async check queries `https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Firmware/releases/latest`
2. If a newer firmware version is found, two UI updates occur:
   - A red badge indicator appears on the "Mount Settings" button in the main window
   - In the Mount Settings dialog (SettingsDialog), below the current firmware version, clickable text appears: "new V{x.y.z} available"
3. Clicking the firmware update text opens the GitHub release page in the default browser
4. If the check fails (no internet, timeout, parse error), no badge or inline text appears — the UI is unchanged

### Fault Tolerance

- All GitHub API calls use `HttpClient` with a 5-second timeout
- Every network call is wrapped in try/catch — failures silently return "no update available"
- No retries, no caching, no background polling
- The app must never block, hang, or show errors due to network issues

## Architecture

### New Files

| File | Purpose |
|---|---|
| `OATControl/UpdateChecker.cs` | GitHub API queries, version parsing, comparison |
| `OATControl/DlgUpdateAvailable.xaml` | Desktop update dialog XAML |
| `OATControl/DlgUpdateAvailable.xaml.cs` | Dialog code-behind (changelog display, download, progress bar, launch installer) |

### `UpdateChecker` class

Static class with two public methods:

```
CheckForDesktopUpdateAsync() → Task<UpdateCheckResult>
CheckForFirmwareUpdateAsync(string currentFirmwareVersion) → Task<UpdateCheckResult>
```

**`UpdateCheckResult`** is a simple data class:
- `bool UpdateAvailable`
- `string LatestVersion`
- `string CurrentVersion`
- `string Changelog` (release body markdown, displayed as plain text)
- `string DownloadUrl` (desktop only — first asset's `browser_download_url`)
- `string ReleasePageUrl` (`html_url` from the GitHub release)

**Version parsing and comparison:**

Desktop tags: `V1.2.0.0` — strip leading 'V', parse as 4-part version (`Version` class)
Firmware tags: `v1.13.9` — strip leading 'v', parse as 3-part version (`Version` class)
For the firmware, the app already parses the mount's firmware version into a comparable long integer. The checker should also try to parse both versions into `Version` objects and compare. If either side can't be parsed (custom build, unknown format), still attempt numeric comparison — if that also fails, return "no update".

**GitHub API calls:**

- Uses `HttpClient` with `User-Agent` header set (GitHub API requires it)
- 5-second timeout
- Deserializes JSON response to extract `tag_name`, `body`, `html_url`, and `assets[0].browser_download_url`
- Uses `Newtonsoft.Json` (already referenced in the project, v13.0.3) for JSON deserialization

### `DlgUpdateAvailable` dialog

- Follows existing dialog patterns: inherits `Controls.ThemedWindow`, sets `Owner` to `Application.Current.MainWindow`, `WindowStartupLocation.CenterOwner`
- Two display states controlled by a `IsDownloading` property:
  - **Info state**: Shows version comparison text, changelog in a scrollable `TextBox` (read-only, word-wrap), Skip and Upgrade buttons
  - **Download state**: Shows progress bar, download percentage text, Cancel button. Cancel aborts the download and returns to info state.
- Download uses `HttpClient.GetAsync` with `HttpCompletionOption.ResponseHeadersRead`, reads the stream to report progress
- Installer is saved to `Path.Combine(Path.GetTempPath(), "OATControlSetup.exe")`
- On successful download: `Process.Start(installerPath)` then `Application.Current.Shutdown()`

### Integration Points

**Desktop check (startup):**

In `MainWindow.xaml.cs` `OnLoaded` handler (or similar startup hook):
```
_ = Task.Run(() => UpdateChecker.CheckForDesktopUpdateAsync())
    .ContinueWith(t => {
        if (!t.IsFaulted && !t.IsCanceled && t.Result?.UpdateAvailable == true)
            Dispatcher.Invoke(() => ShowUpdateDialog(t.Result));
    });
```

**Firmware check (on connect):**

In `MountVM.cs`, after firmware version parsing (~line 2830), set new properties:
- `FirmwareUpdateAvailable` (bool, `INotifyPropertyChanged`)
- `LatestFirmwareVersion` (string)
- `LatestFirmwareReleaseUrl` (string)

Call:
```
_ = Task.Run(() => UpdateChecker.CheckForFirmwareUpdateAsync(ScopeVersion))
    .ContinueWith(t => {
        if (t.Result.UpdateAvailable) {
            FirmwareUpdateAvailable = true;
            LatestFirmwareVersion = t.Result.LatestVersion;
            LatestFirmwareReleaseUrl = t.Result.ReleasePageUrl;
        }
    }, TaskScheduler.FromCurrentSynchronizationContext());
```

### Firmware UI in SettingsDialog

In `SettingsDialog.xaml`, below the firmware version `TextBlock`:
- Add a `Hyperlink` inside a `TextBlock`, bound to `LatestFirmwareReleaseUrl`, visible only when `FirmwareUpdateAvailable` is true
- Text: "new {LatestFirmwareVersion} available"
- Click handler opens the URL via `Process.Start()`

### Badge on Mount Settings button

In `MainWindow.xaml`, the "Mount Settings" `Button`:
- Add a small red circle badge (could be a `Border` + `TextBlock` with "!" overlaid via a `Grid` or `Canvas`)
- Visibility bound to `FirmwareUpdateAvailable` via `BooleanToVisibilityConverter`

### No Changes to OATCommunications

All update-checking code lives in the OATControl project. The shared communications library is not modified.

## Dependencies

- `Newtonsoft.Json` (already in project, v13.0.3) — for JSON deserialization
- `HttpClient` (available in .NET Framework 4.7.2)
- `System.Diagnostics.Process` (for launching installer and opening browser)

## Open Questions

None — all clarified during brainstorming.
